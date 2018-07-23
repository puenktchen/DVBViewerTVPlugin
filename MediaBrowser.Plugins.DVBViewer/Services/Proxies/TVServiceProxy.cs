using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Entities;
using MediaBrowser.Plugins.DVBViewer.Helpers;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services.Proxies
{
    /// <summary>
    /// Provides access to the DVBViewer Media Server tv functionality
    /// </summary>
    public class TVServiceProxy : ProxyBase
    {
        private readonly StreamingServiceProxy _wssProxy;
        private TmdbLookup _tmdbLookup;

        public TVServiceProxy(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, StreamingServiceProxy wssProxy, TmdbLookup tmdbLookup)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _wssProxy = wssProxy;
            _tmdbLookup = tmdbLookup;
        }

        #region Get Methods

        public Settings GetStatusInfo(CancellationToken cancellationToken)
        {
            return GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml");
        }

        public ChannelGroups GetChannelGroups(CancellationToken cancellationToken)
        {
            return GetFromService<ChannelGroups>(cancellationToken, typeof(ChannelGroups), "api/getchannelsxml.html?rootsonly=1");
        }

        private bool refreshChannels = false;
        private object _defaultChannelLock = new object();
        private object _timerChannelLock = new object();
        private Channels _defaultChannelCache = null;
        private Channels _timerChannelCache = null;
        private DateTime _defaultChannelExpirationTime;
        private DateTime _timerChannelExpirationTime;

        public Channels GetChannelList(CancellationToken cancellationToken, string channelGroup)
        {
            if (channelGroup == "DefaultChannelGroup")
            {
                lock (_defaultChannelLock)
                {
                    if (refreshChannels)
                    {
                        _defaultChannelCache = null;
                        refreshChannels = false;
                    }

                    if (_defaultChannelCache == null || _defaultChannelExpirationTime <= DateTime.UtcNow)
                    {
                        _defaultChannelExpirationTime = DateTime.UtcNow.AddSeconds(240);

                        if (Configuration.ChannelFavourites)
                        {
                            _defaultChannelCache = GetFromService<Channels>(cancellationToken, typeof(Channels), "api/getchannelsxml.html?favonly=1&logo=1");
                        }
                        else
                        {
                            _defaultChannelCache = GetFromService<Channels>(cancellationToken, typeof(Channels), "api/getchannelsxml.html?root={0}&logo=1", GeneralExtensions.ToUrlString(Configuration.DefaultChannelGroup));
                        }
                    }

                    return _defaultChannelCache;
                }
            }

            if (channelGroup == "TimerChannelGroup")
            {
                lock (_timerChannelLock)
                {
                    if (_timerChannelCache == null || _timerChannelExpirationTime <= DateTime.UtcNow)
                    {
                        _timerChannelExpirationTime = DateTime.UtcNow.AddSeconds(240);
                        Plugin.Logger.Info("AUTOCREATE DVBViewer TIMERS: Get channels for group \"{0}\" now", GeneralExtensions.ToUrlString(Configuration.TimerChannelGroup));
                        _timerChannelCache = GetFromService<Channels>(cancellationToken, typeof(Channels), "api/getchannelsxml.html?root={0}", GeneralExtensions.ToUrlString(Configuration.TimerChannelGroup));
                    }

                    return _timerChannelCache;
                }
            }

            throw new SystemException();
        }

        public IEnumerable<ChannelInfo> GetChannels(CancellationToken cancellationToken)
        {
            refreshChannels = true;
            var channels = GetChannelList(new CancellationToken(), "DefaultChannelGroup");
            IEnumerable<Channel> channel = channels.Root.ChannelGroup.SelectMany(c => c.Channel);

            Plugin.Logger.Info("Found overall channels: {0}", channel.Count());
            return channel.Select((c, index) =>    
            {
                var channelInfo = new ChannelInfo()
                {
                    Id = c.Id,
                    Name = c.Name,
                    Number = index.ToString(),
                    ChannelType = (GeneralExtensions.HasVideoFlag(c.Flags)) ? ChannelType.TV : ChannelType.Radio,
                };

                if (!String.IsNullOrEmpty(c.Logo))
                {
                    channelInfo.ImageUrl = _wssProxy.GetChannelLogo(c);
                }

                Plugin.Logger.Info("CHANNEL > Name: {0}, Nr: {1}, Id: {2}, EPGId: {3}, Type: {4}, Logo: {5}", c.Name, c.Nr, c.Id, c.EPGID, channelInfo.ChannelType, channelInfo.ImageUrl);
                return channelInfo;
            });
        }

        public IEnumerable<ProgramInfo> GetPrograms(CancellationToken cancellationToken, string channelId, DateTime startDateUtc, DateTime endDateUtc)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var channel = GetChannelList(new CancellationToken(), "DefaultChannelGroup").Root.ChannelGroup.SelectMany(c => c.Channel).Where(c => c.Id == channelId).FirstOrDefault();
            var response = GetFromService<Guide>(cancellationToken, typeof(Guide),
                "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                channel.EPGID,
                GeneralExtensions.FloatDateTime(startDateUtc),
                GeneralExtensions.FloatDateTime(endDateUtc));

            var genreMapper = new GenreMapper(Plugin.Instance.Configuration);

            Plugin.Logger.Info("Found Programs: {0}  for Channel: {1}", response.Program.Count(), channel.Name);
            return response.Program.Select(p =>
            {
                var program = new ProgramInfo()
                {
                    Name = p.Name,
                    EpisodeNumber = p.EpisodeNumber,
                    SeasonNumber = p.SeasonNumber,
                    ProductionYear = p.ProductionYear,
                    Id = GeneralExtensions.SetEventId(channelId, p.Start, p.Stop),
                    SeriesId = p.Name,
                    ChannelId = p.ChannelId,
                    StartDate = GeneralExtensions.GetProgramTime(p.Start),
                    EndDate = GeneralExtensions.GetProgramTime(p.Stop),
                    Overview = p.Overview,
                    Etag = p.EitContent,
                };

                if (!String.IsNullOrEmpty(p.EitContent) || !String.IsNullOrEmpty(p.Overview))
                {
                    genreMapper.PopulateProgramGenres(program);
                }

                if (program.IsSeries && p.Name != p.EpisodeTitleRegEx)
                {
                    program.EpisodeTitle = p.EpisodeTitleRegEx;
                }

                if (Configuration.ProgramImages)
                {
                    if (File.Exists(Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + ".png")))
                    {
                        program.ImageUrl = Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + ".png");

                        if (Configuration.EnableImageProcessing)
                        {
                            program.ImageUrl = Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + "-poster.png"); ;
                            program.ThumbImageUrl = Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + "-landscape.png");
                        }
                    }
                }

                Plugin.Logger.Info("PROGRAM > Title: {0}, SubTitle: {1}, SeasonNr: {2}, EpisodeNr: {3}, Channel: {4}, ChannelId: {5}, ChannelEPGId: {6}", program.Name, program.EpisodeTitle, program.SeasonNumber, program.EpisodeNumber, p.ChannelName, p.ChannelId, p.ChannelEPGID);
                return program; 
            });
        }

        public IEnumerable<MyRecordingInfo> GetRecordings(CancellationToken cancellationToken)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var localPath = String.Format("{0}", Configuration.LocalFilePath);
            var remotePath = String.Format("{0}", Configuration.RemoteFilePath);
            var genreMapper = new GenreMapper(Plugin.Instance.Configuration);
            var lastName = string.Empty;

            var schedules = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2");
            var response = GetFromService<Recordings>(cancellationToken, typeof(Recordings), "api/recordings.html?utf8=1&images=1");

            Plugin.Logger.Info("Found overall Recordings: {0} ", response.Recording.Count());
            return response.Recording.Select(r =>
            {
                var recording = new MyRecordingInfo()
                {
                    Id = r.Id,
                    Name = r.Name,
                    EpisodeTitle = r.EpisodeTitle,
                    EpisodeNumber = r.EpisodeNumber,
                    SeasonNumber = r.SeasonNumber,
                    Year = r.Year,
                    Overview = r.Overview,
                    EitContent = r.EitContent,
                    SeriesTimerId = r.Series,
                    ChannelId = r.ChannelId,
                    ChannelName = r.ChannelName,
                    ChannelType = ChannelType.TV,
                    StartDate = DateTime.ParseExact(r.Start, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).ToUniversalTime(),
                    EndDate =  DateTime.ParseExact(r.Start, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).Add(TimeSpan.ParseExact(r.Duration, "hhmmss", CultureInfo.InvariantCulture)).ToUniversalTime(),
                    Path = r.File,
                };

                if (!String.IsNullOrEmpty(r.EitContent) || !String.IsNullOrEmpty(r.Overview))
                {
                    genreMapper.PopulateRecordingGenres(recording);
                }

                if (recording.IsMovie)
                {
                    recording.Name = r.MovieName;
                }

                if ((recording.StartDate <= DateTime.Now.ToUniversalTime()) && (recording.EndDate >= DateTime.Now.ToUniversalTime()))
                {
                    var timers = schedules.Timer.Where(t => GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.PreEPG) == recording.StartDate && t.Recording == "-1").FirstOrDefault();

                    if (timers != null)
                    {
                        recording.Status = RecordingStatus.InProgress;
                    }
                }
                else
                {
                    recording.Status = RecordingStatus.Completed;

                    if (!String.IsNullOrEmpty(r.Image))
                    {
                        recording.ImageUrl = _wssProxy.GetRecordingImage(r.Image);
                    }
                }

                if (Configuration.RequiresPathSubstitution)
                {
                    recording.Path = r.File.Replace(localPath, remotePath);
                }

                if (Configuration.EnableTmdbLookup)
                {
                    if (recording.Name != lastName)
                    {
                        _tmdbLookup.GetTmdbPoster(cancellationToken, recording);
                    }
                    lastName = recording.Name;
                }

                if (File.Exists(Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + ".jpg")))
                {
                    recording.TmdbPoster = Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + ".jpg");
                }

                Plugin.Logger.Info("RECORDING > Title: {0}, SubTitle: {1}, Series: {2}, Id: {3}, status: {4}", r.Name, r.EpisodeTitle, r.Series, r.Id, recording.Status);
                return recording;
            });
        }

        private bool refreshTimers { get; set; }
        private object _timerLock = new object();
        private List<TimerInfo> _timerCache = null;

        public void RefreshSchedules(CancellationToken cancellationToken)
        {
            refreshSeriesTimers = true;
            refreshTimers = true;

            Plugin.Logger.Info("Refreshing schedules now.");

            GetSeriesSchedulesFromMemory(cancellationToken);
            GetSchedulesFromMemory(cancellationToken);
        }

        public IEnumerable<TimerInfo> GetSchedulesFromMemory(CancellationToken cancellationToken)
        {
            lock (_timerLock)
            {
                if (refreshTimers || _timerCache == null)
                {
                    Plugin.Logger.Info("Writing onetime schedules to memory cache");
                    _timerCache = GetSchedules(cancellationToken).ToList();
                    refreshTimers = false;
                }
                Plugin.Logger.Info("Return onetime schedules from memory cache");
                return _timerCache;
            }
        }

        public IEnumerable<TimerInfo> GetSchedules(CancellationToken cancellationToken)
        {
            var channels = GetChannelList(new CancellationToken(), "DefaultChannelGroup").Root.ChannelGroup.SelectMany(c => c.Channel);
            var response = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2");

            Plugin.Logger.Info("Found overall one time schedules: {0} ", response.Timer.Where(t => String.IsNullOrEmpty(t.Days)).Count());
            return response.Timer.Where(t => String.IsNullOrEmpty(t.Days)).Select(t =>
            {
                var timerInfo = new TimerInfo()
                {
                    Name = t.Description,
                    Id = t.Id,
                    ChannelId = t.ChannelId,
                    StartDate = GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.PreEPG),
                    EndDate = GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.Duration).AddMinutes(-t.PostEPG),
                    IsPrePaddingRequired = (t.PreEPG > 0),
                    IsPostPaddingRequired = (t.PostEPG > 0),
                    PrePaddingSeconds = t.PreEPG * 60,
                    PostPaddingSeconds = t.PostEPG * 60,
                    Priority = t.Priority,
                    Status = (t.Enabled == "0") ? RecordingStatus.Cancelled :
                             (t.Executeable == "0") ? RecordingStatus.ConflictedNotOk :
                             (t.Recording == "-1") ? RecordingStatus.InProgress :
                             RecordingStatus.New,
                };

                var seriesTimer = GetFromService<Searches>(cancellationToken, typeof(Searches), "api/searchlist.html").Search.OrderBy(s => s.Name).ToList();

                if (seriesTimer != null && t.Series != null)
                {
                    if (seriesTimer.Any(x => x.Series.Equals(t.Series)))
                    {
                        timerInfo.SeriesTimerId = t.Series;
                    }
                }

                var guide = GetFromService<Guide>(cancellationToken, typeof(Guide),
                    "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                    channels.Where(c => c.Id == timerInfo.ChannelId).Select(c => c.EPGID).FirstOrDefault(),
                    GeneralExtensions.FloatDateTime(timerInfo.StartDate),
                    GeneralExtensions.FloatDateTime(timerInfo.EndDate));

                var program = guide.Program.Where(p => GeneralExtensions.GetProgramTime(p.Start) == timerInfo.StartDate).FirstOrDefault();

                if (program != null)
                {
                    timerInfo.ProgramId = GeneralExtensions.SetEventId(program.ChannelId, program.Start, program.Stop);
                    timerInfo.Name = (program.EpisodeNumber.HasValue) ? program.Name + " - " + program.EpisodeTitle : program.Name;
                    timerInfo.EpisodeTitle = program.EpisodeTitle;
                    timerInfo.EpisodeNumber = program.EpisodeNumber;
                    timerInfo.SeasonNumber = program.SeasonNumber;
                    timerInfo.Overview = program.Overview;
                }

                Plugin.Logger.Info("SCHEDULE > Title: {0}, Series: {1}, ChannelId: {2}, StartDate: {3}, EndDate: {4}, IsEnabled: {5}", t.Description, t.Series, timerInfo.ChannelId, timerInfo.StartDate.ToLocalTime(), timerInfo.EndDate.ToLocalTime(), t.Enabled);
                return timerInfo; 
            });
        }

        private bool refreshSeriesTimers { get; set; }
        private object _seriesTimerLock = new object();
        private List<SeriesTimerInfo> _seriesTimerCache = null;

        public IEnumerable<SeriesTimerInfo> GetSeriesSchedulesFromMemory(CancellationToken cancellationToken)
        {
            lock (_seriesTimerLock)
            {
                if (refreshSeriesTimers || _seriesTimerCache == null)
                {
                    Plugin.Logger.Info("Writing series schedules to memory cache");
                    _seriesTimerCache = GetSeriesSchedules(cancellationToken).ToList();
                    refreshSeriesTimers = false;
                }
                Plugin.Logger.Info("Return series schedules from memory cache");
                return _seriesTimerCache;
            }
        }

        public IEnumerable<SeriesTimerInfo> GetSeriesSchedules(CancellationToken cancellationToken)
        {
            var response = GetFromService<Searches>(cancellationToken, typeof(Searches), "api/searchlist.html").Search.OrderBy(t => t.Name).ToList();

            Plugin.Logger.Info("Found overall AutoSearches: {0}", response.Count());
            return response.Select(t =>
            {
                var seriesTimerInfo = new SeriesTimerInfo()
                {
                    Name = t.Name,
                    Id = t.Name,
                    SeriesId = t.Name,
                    ChannelId = t.ChannelId,
                    IsPrePaddingRequired = (t.EPGBefore > 0),
                    IsPostPaddingRequired = (t.EPGAfter > 0),
                    PrePaddingSeconds = t.EPGBefore * 60,
                    PostPaddingSeconds = t.EPGAfter * 60,
                    StartDate = GeneralExtensions.GetSearchTime(t.StartTime),
                    EndDate = GeneralExtensions.GetSearchTime(t.EndTime),
                    Priority = t.Priority,
                };

                UpdateScheduling(seriesTimerInfo, t.StartTime, t.EndTime, t.Days);

                Plugin.Logger.Info("AUTOSEARCH > Title: {0}, Id: {1}", seriesTimerInfo.Name, seriesTimerInfo.Id);
                return seriesTimerInfo;
            });
        }

        private void UpdateScheduling(SeriesTimerInfo seriesTimerInfo, string starttime, string endtime, int timerdays)
        {
            seriesTimerInfo.Days = new List<DayOfWeek>();
            seriesTimerInfo.RecordAnyChannel = false;
            seriesTimerInfo.RecordAnyTime = false;
            seriesTimerInfo.RecordNewOnly = false;
            seriesTimerInfo.SkipEpisodesInLibrary = false;

            if (seriesTimerInfo.Priority == 49)
            {
                seriesTimerInfo.SkipEpisodesInLibrary = true;
            }

            if (seriesTimerInfo.ChannelId == null)
            {
                seriesTimerInfo.RecordAnyChannel = true;
            }

            if (starttime == "00:00" && endtime == "23:59")
            {
                seriesTimerInfo.RecordAnyTime = true;
                seriesTimerInfo.StartDate = DateTime.Now.AddHours(-1).ToUniversalTime();
                seriesTimerInfo.EndDate = DateTime.Now.AddHours(1).ToUniversalTime();
            }

            TimerDays days = (TimerDays)timerdays;

            if (timerdays != 127)
            {
                seriesTimerInfo.RecordNewOnly = true;

                if (days.HasFlag(TimerDays.Monday))
                    seriesTimerInfo.Days.Add(DayOfWeek.Monday);
                if (days.HasFlag(TimerDays.Tuesday))
                    seriesTimerInfo.Days.Add(DayOfWeek.Tuesday);
                if (days.HasFlag(TimerDays.Wednesday))
                    seriesTimerInfo.Days.Add(DayOfWeek.Wednesday);
                if (days.HasFlag(TimerDays.Thursday))
                    seriesTimerInfo.Days.Add(DayOfWeek.Thursday);
                if (days.HasFlag(TimerDays.Friday))
                    seriesTimerInfo.Days.Add(DayOfWeek.Friday);
                if (days.HasFlag(TimerDays.Saturday))
                    seriesTimerInfo.Days.Add(DayOfWeek.Saturday);
                if (days.HasFlag(TimerDays.Sunday))
                    seriesTimerInfo.Days.Add(DayOfWeek.Sunday);
            }
            else
            {
                seriesTimerInfo.Days.Add(DayOfWeek.Monday);
                seriesTimerInfo.Days.Add(DayOfWeek.Tuesday);
                seriesTimerInfo.Days.Add(DayOfWeek.Wednesday);
                seriesTimerInfo.Days.Add(DayOfWeek.Thursday);
                seriesTimerInfo.Days.Add(DayOfWeek.Friday);
                seriesTimerInfo.Days.Add(DayOfWeek.Saturday);
                seriesTimerInfo.Days.Add(DayOfWeek.Sunday);
            }
        }

        #endregion

        #region Create Methods

        public Task CreateSchedule(CancellationToken cancellationToken, TimerInfo timer)
        {
            var channels = GetChannelList(new CancellationToken(), "DefaultChannelGroup").Root.ChannelGroup.SelectMany(c => c.Channel);
            var guide = GetFromService<Guide>(cancellationToken, typeof(Guide),
                    "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                    channels.Where(c => c.Id == timer.ChannelId).Select(c => c.EPGID).FirstOrDefault(),
                    GeneralExtensions.FloatDateTime(timer.StartDate),
                    GeneralExtensions.FloatDateTime(timer.EndDate));

            var episodeTitle = guide.Program.Where(p => GeneralExtensions.GetProgramTime(p.Start) == timer.StartDate).FirstOrDefault().EpisodeTitle;

            var builder = new StringBuilder("api/timeradd.html?");

            builder.AppendFormat("title={0}&", (episodeTitle != null ? timer.Name + " - " + episodeTitle : timer.Name).ToUrlString());
            builder.AppendFormat("encoding={0}&", 255);
            builder.AppendFormat("ch={0}&", timer.ChannelId);
            builder.AppendFormat("dor={0}&", timer.StartDate.ToDelphiDate());
            builder.AppendFormat("start={0}&", timer.StartDate.AddSeconds(-timer.PrePaddingSeconds).ToLocalTime().TimeOfDay.TotalMinutes);
            builder.AppendFormat("stop={0}&", timer.EndDate.AddSeconds(timer.PostPaddingSeconds).ToLocalTime().TimeOfDay.TotalMinutes);
            builder.AppendFormat("pre={0}&", timer.PrePaddingSeconds / 60);
            builder.AppendFormat("post={0}&", timer.PostPaddingSeconds / 60);
            builder.AppendFormat("after={0}&", !String.IsNullOrWhiteSpace(Configuration.TimerTask) ? Configuration.TimerTask.ToUrlString() : String.Empty);

            builder.Remove(builder.Length - 1, 1);

            Plugin.Logger.Info("Create new schedule: {0}, StartTime: {1}, EndTime: {2}, ChannelId: {3}", timer.Name, timer.StartDate.ToLocalTime(), timer.EndDate.ToLocalTime(), timer.ChannelId);
            var result = Task.FromResult(GetToService(cancellationToken, builder.ToString()));

            if (result.IsCompleted)
                RefreshSchedules(cancellationToken);

            return result;
        }

        public Task ChangeSchedule(CancellationToken cancellationToken, TimerInfo timer)
        {
            var builder = new StringBuilder("api/timeredit.html?");

            builder.AppendFormat("id={0}&", timer.Id);
            builder.AppendFormat("dor={0}&", timer.StartDate.ToDelphiDate());
            builder.AppendFormat("start={0}&", timer.StartDate.AddSeconds(-timer.PrePaddingSeconds).ToLocalTime().TimeOfDay.TotalMinutes);
            builder.AppendFormat("stop={0}&", timer.EndDate.AddSeconds(timer.PostPaddingSeconds).ToLocalTime().TimeOfDay.TotalMinutes);
            builder.AppendFormat("pre={0}&", timer.PrePaddingSeconds / 60);
            builder.AppendFormat("post={0}&", timer.PostPaddingSeconds / 60);
            builder.AppendFormat("after={0}&", !String.IsNullOrWhiteSpace(Configuration.TimerTask) ? Configuration.TimerTask.ToUrlString() : String.Empty);

            builder.Remove(builder.Length - 1, 1);

            Plugin.Logger.Info("Change Schedule: {0}, StartTime: {1}, EndTime: {2}, ChannelId: {3}", timer.Name, timer.StartDate.ToLocalTime(), timer.EndDate.ToLocalTime(), timer.ChannelId);
            var result = Task.FromResult(GetToService(cancellationToken, builder.ToString()));

            if (result.IsCompleted)
                RefreshSchedules(cancellationToken);

            return result;
        }

        public Task CreateSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo timer)
        {
            var channels = GetChannelList(new CancellationToken(), "DefaultChannelGroup").Root.ChannelGroup.SelectMany(c => c.Channel);

            var builder = new StringBuilder("api/searchadd.html?");

            builder.AppendFormat("Name={0}&", timer.Name.ToUrlString());
            builder.AppendFormat("SearchPhrase={0}&", timer.Name.SetSearchPhrase());
            builder.AppendFormat("Series={0}&", timer.Name.ToUrlString());
            builder.AppendFormat("EPGBefore={0}&", timer.PrePaddingSeconds / 60);
            builder.AppendFormat("EPGAfter={0}&", timer.PostPaddingSeconds / 60);
            builder.AppendFormat("AutoRecording=1&");

            if (!String.IsNullOrWhiteSpace(Configuration.TimerTask))
                builder.AppendFormat("AfterProcessAction={0}&", Configuration.TimerTask.ToUrlString());

            if (Configuration.CheckRecordingTitle)
                builder.AppendFormat("CheckRecTitle=1&");
            else
                builder.AppendFormat("CheckRecTitle=0&");

            if (Configuration.CheckRecordingSubTitle)
                builder.AppendFormat("CheckRecSubtitle=1&");
            else
                builder.AppendFormat("CheckRecSubtitle=0&");

            if (Configuration.CheckRemovedRecording)
                builder.AppendFormat("IncRemoved=1&");
            else
                builder.AppendFormat("IncRemoved=0&");

            if (Configuration.CheckTimerName)
                builder.AppendFormat("CheckTimer=1&");
            else
                builder.AppendFormat("CheckTimer=0&");

            if (!timer.RecordAnyChannel)
            {
                builder.AppendFormat("Channels={0}&", channels.Where(c => c.Id.Equals(timer.ChannelId)).Select(c => c.EPGID).FirstOrDefault());
            }
            else
            {
                builder.AppendFormat("Channels=&");
            }

            if (!timer.RecordAnyTime)
            {
                builder.AppendFormat("StartTime={0}&", timer.StartDate.AddHours(-1).ToTimeString());
                builder.AppendFormat("EndTime={0}&", timer.EndDate.AddHours(1).ToTimeString());
            }
            else
            {
                builder.AppendFormat("StartTime=00:00&");
                builder.AppendFormat("EndTime=23:59&");
            }

            if (timer.RecordNewOnly)
            {
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Monday))
                    builder.AppendFormat("Days=1&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Tuesday))
                    builder.AppendFormat("Days=2&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Wednesday))
                    builder.AppendFormat("Days=4&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Thursday))
                    builder.AppendFormat("Days=8&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Friday))
                    builder.AppendFormat("Days=16&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Saturday))
                    builder.AppendFormat("Days=32&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Sunday))
                    builder.AppendFormat("Days=64&");
            }
            else
            {
                builder.AppendFormat("Days=127&");
            }

            if (timer.SkipEpisodesInLibrary)
            {
                builder.AppendFormat("Priority=49&");
            }

            builder.Remove(builder.Length - 1, 1);

            Plugin.Logger.Info("Create new AutoSearch: {0}", timer.Name);
            var result = Task.FromResult(GetToService(cancellationToken, builder.ToString()));

            result = Task.FromResult(GetToService(cancellationToken, "tasks.html?task=AutoTimer&aktion=tasks"));

            if (result.IsCompleted)
                Plugin.Logger.Info("HTTP STATUS CODE: {0}", result.ToString());
                RefreshSchedules(cancellationToken);

            return result;
        }

        public Task ChangeSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo timer)
        {
            var channels = GetChannelList(new CancellationToken(), "DefaultChannelGroup").Root.ChannelGroup.SelectMany(c => c.Channel);

            var builder = new StringBuilder("api/searchedit.html?");

            builder.AppendFormat("Name={0}&", timer.Name.ToUrlString());
            builder.AppendFormat("EPGBefore={0}&", timer.PrePaddingSeconds / 60);
            builder.AppendFormat("EPGAfter={0}&", timer.PostPaddingSeconds / 60);
            builder.AppendFormat("AutoRecording=1&");

            if (!String.IsNullOrWhiteSpace(Configuration.TimerTask))
                builder.AppendFormat("AfterProcessAction={0}&", Configuration.TimerTask.ToUrlString());

            if (!timer.RecordAnyChannel)
            {
                builder.AppendFormat("Channels={0}&", channels.Where(c => c.Id.Equals(timer.ChannelId)).Select(c => c.EPGID).FirstOrDefault());
            }
            else
            {
                builder.AppendFormat("Channels=&");
            }

            if (!timer.RecordAnyTime)
            {
                builder.AppendFormat("StartTime={0}&", timer.StartDate.ToTimeString());
                builder.AppendFormat("EndTime={0}&", timer.EndDate.ToTimeString());
            }
            else
            {
                builder.AppendFormat("StartTime=00:00&");
                builder.AppendFormat("EndTime=23:59&");
            }

            if (timer.RecordNewOnly)
            {
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Monday))
                    builder.AppendFormat("Days=1&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Tuesday))
                    builder.AppendFormat("Days=2&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Wednesday))
                    builder.AppendFormat("Days=4&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Thursday))
                    builder.AppendFormat("Days=8&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Friday))
                    builder.AppendFormat("Days=16&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Saturday))
                    builder.AppendFormat("Days=32&");
                if (timer.StartDate.ToLocalTime().DayOfWeek.Equals(DayOfWeek.Sunday))
                    builder.AppendFormat("Days=64&");
            }
            else
            {
                builder.AppendFormat("Days=127&");
            }

            if (timer.SkipEpisodesInLibrary)
            {
                builder.AppendFormat("Priority=49&");
            }
            else
            {
                builder.AppendFormat("Priority=50&");
            }

            builder.Remove(builder.Length - 1, 1);

            Plugin.Logger.Info("Changed AutoSearch: {0}", timer.Name);
            var result = Task.FromResult(GetToService(cancellationToken, builder.ToString()));

            GetToService(cancellationToken, "tasks.html?task=AutoTimer&aktion=tasks");

            if (result.IsCompleted)
                RefreshSchedules(cancellationToken);

            return result;
        }

        #endregion

        #region Delete Methods

        public Task DeleteSchedule(CancellationToken cancellationToken, string scheduleId)
        {
            var timer = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2").Timer.Where(t => t.Id == scheduleId).FirstOrDefault();

            if (timer.Enabled != "0")
            {
                Plugin.Logger.Info("Cancel Schedule: {0}, Date: {1}, StartTime: {2}, EndTime: {3}, ChannelId: {4}", timer.Description, timer.Date, timer.Start, timer.End, timer.ChannelId);
                var result = Task.FromResult(GetToService(cancellationToken, "api/timeredit.html?id={0}&enable=0", scheduleId));

                if (result.IsCompleted)
                    RefreshSchedules(cancellationToken);

                return result;
            }
            else
            {
                Plugin.Logger.Info("Delete Schedule with Id: {0}", scheduleId);
                var result = Task.FromResult(GetToService(cancellationToken, "api/timerdelete.html?id={0}", scheduleId));

                if (result.IsCompleted)
                    RefreshSchedules(cancellationToken);

                return result;
            }
        }

        public Task DeleteSeriesSchedule(CancellationToken cancellationToken, string scheduleId)
        {
            Plugin.Logger.Info("Delete AutoSearch with Id: {0}", scheduleId);
            var result = Task.FromResult(GetToService(cancellationToken, "api/searchdelete.html?name={0}", scheduleId.ToUrlString()));

            if (result.IsCompleted)
                RefreshSchedules(cancellationToken);

            return result;
        }

        public void DeleteRecording(string recordingId, CancellationToken cancellationToken)
        {
            var recording = GetFromService<Recordings>(cancellationToken, typeof(Recordings), "api/recordings.html?id=0{0}&utf8=1", recordingId).Recording.First();
            var schedules = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2");
            var activeRecording = schedules.Timer.Where(s => s.Description.StartsWith(recording.Name) && s.ChannelId.Equals(recording.ChannelId) && s.Recording.Equals("-1")).FirstOrDefault();

            if (activeRecording != null)
            {
                DeleteSchedule(cancellationToken, activeRecording.Id).Wait();
            }

            Plugin.Logger.Info("Delete Recording with Id: {0}", recordingId);
            GetToService(cancellationToken, "api/recdelete.html?recid={0}&delfile=1", recordingId);
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

        #endregion
    }
}