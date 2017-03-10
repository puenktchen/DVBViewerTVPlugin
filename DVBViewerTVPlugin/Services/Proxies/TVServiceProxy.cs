using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Entities;
using MediaBrowser.Plugins.DVBViewer.Helpers;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services.Proxies
{
    /// <summary>
    /// Provides access to the DVBViewer Recording Service tv functionality
    /// </summary>
    public class TVServiceProxy : ProxyBase
    {
        public TVServiceProxy(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {

        }

        private bool refreshChannels = false;

        #region Get Methods

        public Settings GetStatusInfo(CancellationToken cancellationToken)
        {
            return GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml");
        }

        public ChannelGroups GetChannelGroups(CancellationToken cancellationToken)
        {
            return GetFromService<ChannelGroups>(cancellationToken, typeof(ChannelGroups), "api/getchannelsxml.html?rootsonly=1");
        }

        private Channels GetChannelList(CancellationToken cancellationToken)
        {
            if (Plugin.Instance.Configuration.EnableTimerCache)
            {
                var channelsCache = MemoryCache.Default;

                if (refreshChannels)
                {
                    channelsCache.Remove("channels");
                    refreshChannels = false;
                }

                if (!channelsCache.Contains("channels"))
                {
                    var expiration = DateTimeOffset.UtcNow.AddSeconds(60); ;
                    var results = GetFromService<Channels>(cancellationToken, typeof(Channels), "api/getchannelsxml.html?root={0}&logo=1", GeneralExtensions.ToUrlString(Configuration.DefaultChannelGroup));

                    channelsCache.Add("channels", results, expiration);
                }

                return (Channels)channelsCache.Get("channels", null);
            }
            else
            {
                return GetFromService<Channels>(cancellationToken, typeof(Channels), "api/getchannelsxml.html?root={0}&logo=1", GeneralExtensions.ToUrlString(Configuration.DefaultChannelGroup));
            }
        }

        public IEnumerable<ChannelInfo> GetChannels(CancellationToken cancellationToken)
        {
            refreshChannels = true;
            var channels = GetChannelList(cancellationToken);
            IEnumerable<Channel> channel = channels.Scanroot.Channelgroup.SelectMany(c => c.Channel);

            Plugin.Logger.Info("Found overall channels: {0}", channel.Count());
            return channel.Select((c, index) =>    
            {
                var channelInfo = new ChannelInfo()
                {
                    Name = c.Name,
                    Number = c.Nr,
                    Id = c.Nr,
                    ChannelType = (GeneralExtensions.HasVideoFlag(c.Flags)) ? ChannelType.TV : ChannelType.Radio,
                    //ImageUrl = GeneralExtensions.ChannelImageUrl(c.Logo),
                    HasImage = (String.IsNullOrEmpty(c.Logo)) ? false : true,
                };
                
                Plugin.Logger.Info("Found channel: {0}, Nr: {1}, Id: {2}, EPGId: {3} of type:{4}, channel logo: {5}", c.Name, c.Nr, c.ID, c.EPGID, channelInfo.ChannelType, channelInfo.HasImage);
                return channelInfo;
            });
        }

        public IEnumerable<ProgramInfo> GetPrograms(CancellationToken cancellationToken, string channelId, DateTime startDateUtc, DateTime endDateUtc)
        {
            var channels = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel);
            var response = GetFromService<Guide>(cancellationToken, typeof(Guide),
                "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                channels.Where(c => c.Nr == channelId).Select(c => c.EPGID).FirstOrDefault(),
                GeneralExtensions.FloatDateTime(startDateUtc),
                GeneralExtensions.FloatDateTime(endDateUtc));

            var genreMapper = new GenreMapper(Plugin.Instance.Configuration);

            Plugin.Logger.Info("Found programs: {0}  for channel nr: {1}", response.Programs.Count(), channelId);
            return response.Programs.Select(p =>
            {
                var program = new ProgramInfo()
                {
                    Name = p.GetTitle(),
                    EpisodeTitle = p.GetSubTitle(),
                    Overview = p.GetDescription(),
                    Id = GeneralExtensions.SetEventId(p.Channel, p.Start, p.Stop),
                    SeriesId = p.GetTitle(),
                    ChannelId = channels.Where(c => c.EPGID == p.Channel).Select(c => c.Nr).FirstOrDefault(),
                    StartDate = GeneralExtensions.GetProgramTime(p.Start),
                    EndDate = GeneralExtensions.GetProgramTime(p.Stop),
                };

                var channel = channels.Where(c => c.EPGID == p.Channel).FirstOrDefault();
                if (!String.IsNullOrEmpty(channel.Logo))
                    program.ImageUrl = GeneralExtensions.ChannelImageUrl(channel.Logo);

                if (!String.IsNullOrEmpty(p.GetDescription()))
                    genreMapper.PopulateProgramGenres(program);

                //Plugin.Logger.Info("Found program: {0}, subtitle: {1}, description: {2}", p.GetTitle(), p.GetSubTitle(), p.GetDescription());
                return program; 
            });
        }

        public IEnumerable<RecordingInfo> GetRecordings(CancellationToken cancellationToken)
        {
            var configuration = Plugin.Instance.Configuration;
            var localpath = String.Format("{0}", configuration.LocalFilePath);
            var remotepath = String.Format("{0}", configuration.RemoteFilePath);

            var channels = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel);
            var response = GetFromService<Recordings>(cancellationToken, typeof(Recordings), "api/recordings.html?utf8=1&images=1");
            
            return response.Recording.Select(r =>
            {
                Plugin.Logger.Info("Found overall recordings: {0} ", response.Recording.Count());
                var recording = new RecordingInfo()
                {
                    Name = r.Title,
                    EpisodeTitle = r.SubTitle,
                    Overview = r.Description,
                    Id = GeneralExtensions.SetRecordingId(r.Id),
                    SeriesTimerId = r.Series,
                    IsSeries = (!String.IsNullOrEmpty(r.Series)) ? true : false,
                    ChannelId = channels.Where(c => c.Name == r.Channel).Select(c => c.Nr).FirstOrDefault(),
                    StartDate = DateTime.ParseExact(r.Start, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                    EndDate =  DateTime.ParseExact(r.Start, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).Add(TimeSpan.ParseExact(r.Duration, "hhmmss", CultureInfo.InvariantCulture)),
                    HasImage = (!String.IsNullOrEmpty(r.Image)) ? true : false,
                    ImageUrl = GeneralExtensions.RecordingImageUrl(response.ImageURL, r.Image),
                };

                if (configuration.EnableDirectAccess && !configuration.RequiresPathSubstitution)
                {
                    recording.Path = r.File;
                }

                if (configuration.EnableDirectAccess && configuration.RequiresPathSubstitution)
                {
                    recording.Path = r.File.Replace(localpath, remotepath);
                }

                Plugin.Logger.Info("Found recording: {0} - {1}, Series: {2}, id: {3}, recording image: {4}", r.Title, r.SubTitle, r.Series, r.Id, recording.ImageUrl);
                return recording;
            });
        }

        public IEnumerable<TimerInfo> GetSchedules(CancellationToken cancellationToken)
        {
            var channels = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel);
            var response = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2");

            Plugin.Logger.Info("Found overall one time schedules: {0} ", response.Timer.Where(t => String.IsNullOrEmpty(t.Days)).Count());
            return response.Timer.Where(t => String.IsNullOrEmpty(t.Days)).Select(t =>
            {
                var timerInfo = new TimerInfo()
                {
                    Name = (t.Enabled == "0") ? t.Description + " [Cancelled]" :
                           (t.Executeable == "0") ? t.Description + " [Conflict]" :
                           (t.Recording == "-1") ? t.Description + " [Recording]" :
                           t.Description,
                    Id = t.TimerID,
                    SeriesTimerId = t.Series,
                    ChannelId = channels.Where(c => c.ID == GeneralExtensions.GetScheduleChannel(t.Channel.ChannelID)).Select(c => c.Nr).FirstOrDefault(),
                    StartDate = GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.PreEPG),
                    EndDate = GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.Dur).AddMinutes(-t.PostEPG),
                    IsPrePaddingRequired = (t.PreEPG > 0),
                    IsPostPaddingRequired = (t.PostEPG > 0),
                    PrePaddingSeconds = t.PreEPG * 60,
                    PostPaddingSeconds = t.PostEPG * 60,
                    Status = (t.Enabled == "0") ? RecordingStatus.Cancelled :
                             (t.Executeable == "0") ? RecordingStatus.ConflictedNotOk :
                             (t.Recording == "-1") ? RecordingStatus.InProgress :
                             RecordingStatus.New,
                };

                var guide = GetFromService<Guide>(cancellationToken, typeof(Guide),
                    "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                    channels.Where(c => c.Nr == timerInfo.ChannelId).Select(c => c.EPGID).FirstOrDefault(),
                    GeneralExtensions.FloatDateTime(timerInfo.StartDate),
                    GeneralExtensions.FloatDateTime(timerInfo.EndDate));
                //var program = guide.Programs.Where(p => GeneralExtensions.GetProgramTime(p.Start) == timerInfo.StartDate.ToLocalTime()).FirstOrDefault();
                var program = guide.Programs.Where(p => GeneralExtensions.GetProgramTime(p.Start) == timerInfo.StartDate).FirstOrDefault();

                if (program != null)
                {
                    timerInfo.ProgramId = GeneralExtensions.SetEventId(program.Channel, program.Start, program.Stop);
                    timerInfo.Overview = program.GetDescription();
                }

                Plugin.Logger.Info("Found schedule: {0}, series: {1}, channelNr: {2}, start: {3}, end: {4}, is enabled: {5}", t.Description, t.Series, timerInfo.ChannelId, timerInfo.StartDate.ToLocalTime(), timerInfo.EndDate.ToLocalTime(), t.Enabled);
                return timerInfo; 
            });
        }

        public IEnumerable<SeriesTimerInfo> GetSeriesSchedules(CancellationToken cancellationToken)
        {
            var channels = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel);
            var response = GetFromService<Searches>(cancellationToken, typeof(Searches), "api/getconfigfile.html?file=config%5Csearches.xml");
            var index = 0;

            Plugin.Logger.Info("Found overall AutoSearches: {0}", response.Search.Count());
            return response.Search.OrderBy(t => t.Name).Select(t =>
            {
                var seriesTimerInfo = new SeriesTimerInfo()
                {
                    Name = t.Name,
                    Id = String.Format("Autosearch-{0}", index++ ),
                    SeriesId = t.Series,
                    IsPrePaddingRequired = (t.EPGBefore > 0),
                    IsPostPaddingRequired = (t.EPGAfter > 0),
                    PrePaddingSeconds = t.EPGBefore * 60,
                    PostPaddingSeconds = t.EPGAfter * 60,
                };

                if (t.Channels != null)
                    seriesTimerInfo.ChannelId = channels.Where(c => c.EPGID == t.Channels.Channel[0]).Select(c => c.Nr).FirstOrDefault();
                else
                    seriesTimerInfo.RecordAnyChannel = true;

                if (t.CheckRecSubTitle == "-1" || t.CheckRecTitle == "-1" || t.CheckTimer == "-1")
                    seriesTimerInfo.RecordNewOnly = true;
                    
                UpdateScheduling(seriesTimerInfo, t.Starttime, t.EndTime, t.Days);

                Plugin.Logger.Info("Found AutoSearch: {0} with Id: {1}", t.Name, seriesTimerInfo.Id);
                return seriesTimerInfo;
            });
        }

        private void UpdateScheduling(SeriesTimerInfo seriesTimerInfo, string starttime, string endtime, int timerdays)
        {
            seriesTimerInfo.RecordAnyTime = false;

            if (starttime == "00:00" && endtime == "23:59")
                seriesTimerInfo.RecordAnyTime = true;
            else
            {
                seriesTimerInfo.StartDate = GeneralExtensions.GetSearchTime(starttime);
                seriesTimerInfo.EndDate = GeneralExtensions.GetSearchTime(endtime);
            }

            var days = new List<int>();
            GeneralExtensions.GetSearchDays(timerdays, 0, days);
            seriesTimerInfo.Days = new List<DayOfWeek>();

            if (days.Contains(1))
                seriesTimerInfo.Days.Add(DayOfWeek.Monday);
            if (days.Contains(2))
                seriesTimerInfo.Days.Add(DayOfWeek.Tuesday);
            if (days.Contains(4))
                seriesTimerInfo.Days.Add(DayOfWeek.Wednesday);
            if (days.Contains(8))
                seriesTimerInfo.Days.Add(DayOfWeek.Thursday);
            if (days.Contains(16))
                seriesTimerInfo.Days.Add(DayOfWeek.Friday);
            if (days.Contains(32))
                seriesTimerInfo.Days.Add(DayOfWeek.Saturday);
            if (days.Contains(64))
                seriesTimerInfo.Days.Add(DayOfWeek.Sunday);
        }

        #endregion

        #region Create Methods

        public void CreateSchedule(CancellationToken cancellationToken, TimerInfo timer)
        {
            var settings = GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml");

            var builder = new StringBuilder("timer_new.html?aktion=timer_add&source=timer_add&save=Speichern&active=active&");
            builder.AppendFormat("title={0}&", GeneralExtensions.ToUrlString(timer.Name));

            if (settings.Version().Contains("V1.32"))
                builder.AppendFormat("channel={0}&", timer.ChannelId);
            else
                builder.AppendFormat("chid={0}&", timer.ChannelId);

            builder.AppendFormat("dor={0}&", GeneralExtensions.ToDateString(timer.StartDate));
            builder.AppendFormat("starttime={0}&", GeneralExtensions.ToTimeString(timer.StartDate));
            builder.AppendFormat("endtime={0}&", GeneralExtensions.ToTimeString(timer.EndDate));
            builder.AppendFormat("epgbefore={0}&", timer.PrePaddingSeconds / 60);
            builder.AppendFormat("epgafter={0}&", timer.PostPaddingSeconds / 60);

            if (settings.RecAllAudio() == "1")
                builder.AppendFormat("RecAllAudio=1&");
            if (settings.RecDVBSub() == "1")
                builder.AppendFormat("RecDVBSub=1&");
            if (settings.RecTeletext() == "1")
                builder.AppendFormat("RecTeletext=1&");
            if (settings.RecEITEpg() == "1")
                builder.AppendFormat("RecEITEpg=1&");
            if (settings.PATPMTAdjust() == "1")
                builder.AppendFormat("PATPMTAdjust=1&");

            builder.Remove(builder.Length - 1, 1);

            GetToService(cancellationToken, builder.ToString());
            Plugin.Logger.Info("Created schedule: {0}, starttime: {1}, endtime: {2}, channelNr: {3}", timer.Name, timer.StartDate.ToLocalTime(), timer.EndDate.ToLocalTime(), timer.ChannelId);
        }

        public void ChangeSchedule(CancellationToken cancellationToken, TimerInfo timer)
        {
            var settings = GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml");

            var builder = new StringBuilder("timer_new.html?aktion=timer_add&source=timer_edit&save=Speichern&active=active&");
            builder.AppendFormat("timer_id={0}&", timer.Id);
            builder.AppendFormat("title={0}&", GeneralExtensions.ToUrlString(GeneralExtensions.GetScheduleName(timer.Name)));

            if (settings.Version().Contains("V1.32"))
                builder.AppendFormat("channel={0}&", timer.ChannelId);
            else
                builder.AppendFormat("chid={0}&", timer.ChannelId);

            builder.AppendFormat("dor={0}&", GeneralExtensions.ToDateString(timer.StartDate));
            builder.AppendFormat("starttime={0}&", GeneralExtensions.ToTimeString(timer.StartDate));
            builder.AppendFormat("endtime={0}&", GeneralExtensions.ToTimeString(timer.EndDate));
            builder.AppendFormat("epgbefore={0}&", timer.PrePaddingSeconds / 60);
            builder.AppendFormat("epgafter={0}&", timer.PostPaddingSeconds / 60);

            if (settings.RecAllAudio() == "1")
                builder.AppendFormat("RecAllAudio=1&");
            if (settings.RecDVBSub() == "1")
                builder.AppendFormat("RecDVBSub=1&");
            if (settings.RecTeletext() == "1")
                builder.AppendFormat("RecTeletext=1&");
            if (settings.RecEITEpg() == "1")
                builder.AppendFormat("RecEITEpg=1&");
            if (settings.PATPMTAdjust() == "1")
                builder.AppendFormat("PATPMTAdjust=1&");

            builder.Remove(builder.Length - 1, 1);

            GetToService(cancellationToken, builder.ToString());
            Plugin.Logger.Info("Changed schedule: {0}, starttime: {1}, endtime: {2}, channelNr: {3}", timer.Name, timer.StartDate.ToLocalTime(), timer.EndDate.ToLocalTime(), timer.ChannelId);
        }

        public void CreateSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo timer)
        {
            var channels = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel);
            var settings = GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml");
            var searchUrl = "epg_search.html";

            Dictionary<string, string> header = new Dictionary<string, string>()
            {
                { "search_id", "-1" },
                { "cbtitle", "on" },
                { "ignorecase", "on" },
                
                { "searchphrase", GeneralExtensions.SetSearchPhrase(timer.Name) },
                { "Savename", GeneralExtensions.ToUrlString(timer.Name) }, 
                { "Series", GeneralExtensions.ToUrlString(timer.Name) },

                { "chan_id", timer.RecordAnyChannel ? "" : channels.Where(c => c.Nr == timer.ChannelId).Select(c => c.EPGID).FirstOrDefault() },
                { "startH", timer.RecordAnyTime ? "00:00" : GeneralExtensions.ToTimeString(timer.StartDate.AddSeconds(-timer.PrePaddingSeconds)) },
                { "endH", timer.RecordAnyTime ? "23:59" : GeneralExtensions.ToTimeString(timer.EndDate.AddSeconds(timer.PostPaddingSeconds)) },
                { "epgbefore", (timer.PrePaddingSeconds / 60).ToString() },
                { "epgafter", (timer.PostPaddingSeconds / 60).ToString() },

                { "MonitorForStart", "1" },
                { "AutoRec", "on" },
                { "Save", ""},
            };

            if (timer.Days.Contains(DayOfWeek.Monday))
                header.Add("D0", "1");
            if (timer.Days.Contains(DayOfWeek.Tuesday))
                header.Add("D1", "1");
            if (timer.Days.Contains(DayOfWeek.Wednesday))
                header.Add("D2", "1");
            if (timer.Days.Contains(DayOfWeek.Thursday))
                header.Add("D3", "1");
            if (timer.Days.Contains(DayOfWeek.Friday))
                header.Add("D4", "1");
            if (timer.Days.Contains(DayOfWeek.Saturday))
                header.Add("D5", "1");
            if (timer.Days.Contains(DayOfWeek.Sunday))
                header.Add("D6", "1");

            if (timer.RecordNewOnly)
            {
                if (Plugin.Instance.Configuration.CheckRecordingTitel)
                    header.Add("chkrectitel", "on");
                if (Plugin.Instance.Configuration.CheckRecordingInfo)
                    header.Add("chkrecsubtitel", "on");
                if (Plugin.Instance.Configuration.CheckTimerName)
                    header.Add("chktimer", "on");
            }
            
            PostToService(cancellationToken, searchUrl, header);
            GetToService(cancellationToken, "tasks.html?task=AutoTimer&aktion=tasks");
            Plugin.Logger.Info("Created AutoSearch: {0}", timer.Name);
        }

        public void ChangeSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo timer)
        {
            var channels = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel);
            var settings = GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml");
            var searchUrl = "epg_search.html";

            Dictionary<string, string> header = new Dictionary<string, string>()
            {
                { "search_id", timer.Id.Remove(0, 11) },
                { "cbtitle", "on" },
                { "ignorecase", "on" },
                
                { "searchphrase", GeneralExtensions.SetSearchPhrase(timer.Name) },
                { "Savename", GeneralExtensions.ToUrlString(timer.Name) }, 
                { "Series", GeneralExtensions.ToUrlString(timer.Name) },

                { "chan_id", timer.RecordAnyChannel ? "" : channels.Where(c => c.Nr == timer.ChannelId).Select(c => c.EPGID).FirstOrDefault() },
                { "startH", timer.RecordAnyTime ? "00:00" : GeneralExtensions.ToTimeString(timer.StartDate) },
                { "endH", timer.RecordAnyTime ? "23:59" : GeneralExtensions.ToTimeString(timer.EndDate) },
                { "epgbefore", (timer.PrePaddingSeconds / 60).ToString() },
                { "epgafter", (timer.PostPaddingSeconds / 60).ToString() },

                { "MonitorForStart", "1" },
                { "AutoRec", "on" },
                { "Save", ""},
            };

            if (timer.Days.Contains(DayOfWeek.Monday))
                header.Add("D0", "1");
            if (timer.Days.Contains(DayOfWeek.Tuesday))
                header.Add("D1", "1");
            if (timer.Days.Contains(DayOfWeek.Wednesday))
                header.Add("D2", "1");
            if (timer.Days.Contains(DayOfWeek.Thursday))
                header.Add("D3", "1");
            if (timer.Days.Contains(DayOfWeek.Friday))
                header.Add("D4", "1");
            if (timer.Days.Contains(DayOfWeek.Saturday))
                header.Add("D5", "1");
            if (timer.Days.Contains(DayOfWeek.Sunday))
                header.Add("D6", "1");

            if (timer.RecordNewOnly)
            {
                if (Plugin.Instance.Configuration.CheckRecordingTitel)
                    header.Add("chkrectitel", "on");
                if (Plugin.Instance.Configuration.CheckRecordingInfo)
                    header.Add("chkrecsubtitel", "on");
                if (Plugin.Instance.Configuration.CheckTimerName)
                    header.Add("chktimer", "on");
            }

            PostToService(cancellationToken, searchUrl, header);
            GetToService(cancellationToken, "tasks.html?task=AutoTimer&aktion=tasks");
            Plugin.Logger.Info("Changed AutoSearch: {0} with Id: {1}", timer.Name, timer.Id.Remove(0, 11));
        }

        #endregion

        #region Delete Methods

        public void DeleteSchedule(CancellationToken cancellationToken, string scheduleId)
        {
            var timer = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2").Timer.Where(t => t.TimerID == scheduleId).FirstOrDefault();

            if (timer.Enabled != "0")
            {
                var channels = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel);
                var settings = GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml");

                var builder = new StringBuilder("timer_new.html?aktion=timer_add&source=timer_edit&save=Speichern&");
                builder.AppendFormat("timer_id={0}&", timer.TimerID);
                builder.AppendFormat("title={0}&", GeneralExtensions.ToUrlString(timer.Description));

                if (settings.Version().Contains("V1.32"))
                    builder.AppendFormat("channel={0}&", channels.Where(c => c.ID == GeneralExtensions.GetScheduleChannel(timer.Channel.ChannelID)).Select(c => c.Nr).FirstOrDefault());
                else
                    builder.AppendFormat("chid={0}&", channels.Where(c => c.ID == GeneralExtensions.GetScheduleChannel(timer.Channel.ChannelID)).Select(c => c.Nr).FirstOrDefault());

                builder.AppendFormat("dor={0}&", timer.Date);
                builder.AppendFormat("starttime={0}&", GeneralExtensions.ToTimeString(GeneralExtensions.GetScheduleTime(timer.Date, timer.Start).AddMinutes(timer.PreEPG)));
                builder.AppendFormat("endtime={0}&", GeneralExtensions.ToTimeString(GeneralExtensions.GetScheduleTime(timer.Date, timer.End).AddMinutes(-timer.PostEPG)));
                builder.AppendFormat("epgbefore={0}&", timer.PreEPG);
                builder.AppendFormat("epgafter={0}&", timer.PostEPG);

                if (settings.RecAllAudio() == "1")
                    builder.AppendFormat("RecAllAudio=1&");
                if (settings.RecDVBSub() == "1")
                    builder.AppendFormat("RecDVBSub=1&");
                if (settings.RecTeletext() == "1")
                    builder.AppendFormat("RecTeletext=1&");
                if (settings.RecEITEpg() == "1")
                    builder.AppendFormat("RecEITEpg=1&");
                if (settings.PATPMTAdjust() == "1")
                    builder.AppendFormat("PATPMTAdjust=1&");

                builder.Remove(builder.Length - 1, 1);

                GetToService(cancellationToken, builder.ToString());
                Plugin.Logger.Info("Cancelled schedule: {0} at date: {1} with starttime: {2}, endtime: {3} on channel nr: {4}", timer.Description, timer.Date, timer.Start, timer.End, timer.Channel);
            }
            else
            {
                GetToService(cancellationToken, "api/timerdelete.html?id={0}", scheduleId);
                Plugin.Logger.Info("Deleted schedule with id: {0}", scheduleId);
            }

        }

        public void DeleteSeriesSchedule(CancellationToken cancellationToken, string scheduleId)
        {
            var searchUrl = "epg_search.html";

            Dictionary<string, string> header = new Dictionary<string, string>()
            {
                { "search_id", scheduleId.Remove(0, 11) },
                { "Delete", ""},
            };

            PostToService(cancellationToken, searchUrl, header);
            Plugin.Logger.Info("Deleted AutoSearch with Id: {0}", scheduleId.Remove(0, 11));
        }

        public void DeleteRecording(string recordingId, CancellationToken cancellationToken)
        {
            GetToService(cancellationToken, "api/recdelete.html?recid={0}&delfile=1", recordingId.Remove(0, 10));
            Plugin.Logger.Info("Deleted recording with id: {0}", recordingId);
        }

        #endregion

        #region Other Methods

        public ScheduleDefaults GetScheduleDefaults(CancellationToken cancellationToken)
        {
            return new ScheduleDefaults()
            {
                PreRecordInterval = TimeSpan.FromMinutes(Plugin.Instance.Configuration.TimerPrePadding.Value),
                PostRecordInterval = TimeSpan.FromMinutes(Plugin.Instance.Configuration.TimerPostPadding.Value),
            };
        }

        public ImageStream GetChannelLogo(string channelId, CancellationToken cancellationToken)
        {
            try
            {
                var logo = GetChannelList(cancellationToken).Scanroot.Channelgroup.SelectMany(c => c.Channel).Where(c => c.Nr == channelId).Select(l => l.Logo).FirstOrDefault();
                var logotype = logo.Substring(logo.Length - 3);

                return GetImageFromService(cancellationToken, logotype, "api/{0}", logo);
            }
            catch
            {
                Plugin.Logger.Error("No channel logo found for channel nr: {0}", channelId);
                return null;
                throw;
            } 
        }

        #endregion
    }
}