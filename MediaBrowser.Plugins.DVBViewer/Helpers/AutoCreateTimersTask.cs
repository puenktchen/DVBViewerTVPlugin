using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;
using MediaBrowser.Plugins.DVBViewer.Services.Proxies;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public class AutoCreateTimersTask : ProxyBase, IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private TVServiceProxy _tvServiceProxy;

        public AutoCreateTimersTask(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, ILibraryManager libraryManager, TVServiceProxy tvServiceProxy)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _libraryManager = libraryManager;
            _tvServiceProxy = tvServiceProxy;
        }

        public string Category => "Live TV";
        public string Key => "DVBViewerAutoCreateTimersTask";
        public string Name => "Autocreate DVBViewer timers";
        public string Description => "Gets guide data for 72 hours and autocreates new DVBViewer timers, based on missing Emby library episodes" +
                                     Environment.NewLine +
                                     "(The action should be executed some minutes after the guide refresh task has finished, but only once a day)";

        public bool IsHidden => Plugin.Instance.Configuration.AutoCreateTimers ? false : true;
        public bool IsEnabled => Plugin.Instance.Configuration.AutoCreateTimers ? true : false;
        public bool IsLogged => false;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            TaskTriggerInfo startuptrigger = new TaskTriggerInfo();
            startuptrigger.Type = "StartupTrigger";

            List<TaskTriggerInfo> tasktrigger = new List<TaskTriggerInfo>();
            tasktrigger.Add(startuptrigger);

            return tasktrigger;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            SearchForTimers(cancellationToken);

            return Task.Delay(0, cancellationToken);
        }

        private void SearchForTimers(CancellationToken cancellationToken)
        {
            List<Program> programs = new List<Program>();

            Plugin.Logger.Info("AUTOCREATE DVBViewer TIMERS: Get missing episodes now");
            var missingEpisodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Episode).Name },
                IsMissing = true,

            }).Where(x => !x.Tags.Contains("NoDMS") && !x.Parent.Tags.Contains("NoDMS") && !x.Parent.Parent.Tags.Contains("NoDMS"));

            // Extra logging
            if (Plugin.Instance.Configuration.EnableLogging)
            {
                foreach (var item in missingEpisodes)
                {
                    Plugin.Logger.Info("MISSING EPISODES > Series: {0}; Number: {1}.{2}; Episode: {3}",
                        item.Parent.Parent.Name,
                        item.Parent.IndexNumber,
                        item.IndexNumber,
                        item.Name);
                }
            }

            Plugin.Logger.Info("AUTOCREATE DVBViewer TIMERS: Get timers from backend now");
            var timers = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2");

            var channels = Plugin.TvProxy.GetChannelList(cancellationToken, "TimerChannelGroup").Root.ChannelGroup.SelectMany(c => c.Channel);

            Plugin.Logger.Info("AUTOCREATE DVBViewer TIMERS: Get guide data from backend now");
            foreach (var channel in channels)
            {
                var program = GetFromService<Guide>(cancellationToken, typeof(Guide), "api/epg.html?lvl=2&channel={0}&start={1}&end={2}", channel.EPGID, GeneralExtensions.FloatDateTimeOffset(DateTimeOffset.Now), GeneralExtensions.FloatDateTimeOffset(DateTimeOffset.Now.AddHours(72))).Program;
                programs.AddRange(program.Where(p => p.ChannelEPGID == channel.EPGID));
            }

            bool refreshSchedules = false;

            Plugin.Logger.Info("AUTOCREATE DVBViewer TIMERS: Compare guide data with missing episodes now");
            foreach (var program in programs)
            {
                if (missingEpisodes != null && !String.IsNullOrEmpty(program.Name) && program.SeasonNumber.HasValue && program.EpisodeNumber.HasValue)
                {
                    // Extra logging
                    Plugin.Logger.Info("PROGRAM > Title: {0}; Number: {1}.{2}; Subtitle: {3}; Channel: {4}; ChannelId: {5}; ChannelEPGID: {6} Start: {7}",
                        program.Name,
                        program.SeasonNumber,
                        program.EpisodeNumber,
                        program.EpisodeTitleRegEx,
                        program.ChannelName,
                        program.ChannelId,
                        program.ChannelEPGID, GeneralExtensions.GetProgramTime(program.Start).ToLocalTime());

                    foreach (var episode in missingEpisodes.Where(x =>
                    Regex.Replace(x.Parent.Parent.Name, @"\s\W[a-zA-Z]?[0-9]{1,4}?\W$", String.Empty).Equals(Regex.Replace(program.Name, @"\s\W[a-zA-Z]?[0-9]{1,4}?\W$", String.Empty), StringComparison.OrdinalIgnoreCase) &&
                    x.IndexNumber.Equals(program.EpisodeNumber) &&
                    x.ParentIndexNumber.Equals(program.SeasonNumber)))
                    {
                        CreateTimer(cancellationToken, program, timers);
                        refreshSchedules = true;
                    }
                }
            }

            if (refreshSchedules)
            {
                Plugin.TvProxy.RefreshSchedules(cancellationToken);
            }
        }

        private Task CreateTimer(CancellationToken cancellationToken, Program program, Timers timers)
        {
            if (!timers.Timer.Any(t =>
            t.ChannelId == program.ChannelId &&
            GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.PreEPG) == GeneralExtensions.GetProgramTime(program.Start) &&
            GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.Duration).AddMinutes(-t.PostEPG) == GeneralExtensions.GetProgramTime(program.Stop)))
            {
                var builder = new StringBuilder("api/timeradd.html?");

                builder.AppendFormat("title={0}&", (program.EpisodeTitle != null ? program.Name + " - " + program.EpisodeTitle : program.Name).ToUrlString());
                builder.AppendFormat("series={0}&", program.Name.ToUrlString());
                builder.AppendFormat("encoding={0}&", 255);
                builder.AppendFormat("ch={0}&", program.ChannelId);
                builder.AppendFormat("dor={0}&", GeneralExtensions.GetProgramTime(program.Start).ToDelphiDate());
                builder.AppendFormat("start={0}&", GeneralExtensions.GetProgramTime(program.Start).AddMinutes(-(double)Configuration.TimerPrePadding).ToLocalTime().TimeOfDay.TotalMinutes);
                builder.AppendFormat("stop={0}&", GeneralExtensions.GetProgramTime(program.Stop).AddMinutes((double)Configuration.TimerPostPadding).ToLocalTime().TimeOfDay.TotalMinutes);
                builder.AppendFormat("pre={0}&", Configuration.TimerPrePadding);
                builder.AppendFormat("post={0}&", Configuration.TimerPostPadding);
                builder.AppendFormat("prio={0}&", 49);
                builder.AppendFormat("after={0}&", !String.IsNullOrWhiteSpace(Configuration.TimerTask) ? Configuration.TimerTask.ToUrlString() : String.Empty);

                builder.Remove(builder.Length - 1, 1);

                Plugin.Logger.Info("Create new schedule: {0} - {1}, StartTime: {2}, EndTime: {3}, Channel: {4}, ChannelId: {5}",
                    program.Name,
                    program.EpisodeTitle,
                    GeneralExtensions.GetProgramTime(program.Start).AddMinutes(-(double)Configuration.TimerPrePadding).ToLocalTime(),
                    GeneralExtensions.GetProgramTime(program.Stop).AddMinutes((double)Configuration.TimerPostPadding).ToLocalTime(),
                    program.ChannelName,
                    program.ChannelId);

                return Task.FromResult(GetToService(cancellationToken, builder.ToString()));
            }

            return Task.Delay(0);
        }
    }
}