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
        public string Description => "Gets guide data for 24 hours and autocreates new DVBViewer timers, based on missing Emby library episodes" +
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
            var missingEpisodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Episode).Name },
                IsMissing = true,
            });

            var timers = GetFromService<Timers>(cancellationToken, typeof(Timers), "api/timerlist.html?utf8=2");

            var programs = GetFromService<Guide>(cancellationToken, typeof(Guide),
                "api/epg.html?lvl=2&start={0}&end={1}",
                GeneralExtensions.FloatDateTime(DateTime.Now),
                GeneralExtensions.FloatDateTime(DateTime.Now.AddHours(25))).Program;

            foreach (var program in programs)
            {
                if (!String.IsNullOrEmpty(program.Name))
                {
                    foreach (var episode in missingEpisodes.Where(x =>
                    x.Parent.Parent.Name.Contains(Regex.Replace(program.Name, @"\s\W[a-zA-Z]?[0-9]{1,3}?\W$", String.Empty)) &&
                    x.IndexNumber.Equals(Convert.ToInt32(program.EpisodeNumber)) &&
                    x.ParentIndexNumber.Equals(Convert.ToInt32(program.SeasonNumber))))
                    {
                        CreateTimer(cancellationToken, program, timers);
                    }
                }
            }

            Plugin.TvProxy.RefreshSchedules(cancellationToken);
        }

        private Task CreateTimer(CancellationToken cancellationToken, Program program, Timers timers)
        {
            var channels = _tvServiceProxy.GetChannelList(cancellationToken).Root.ChannelGroup.SelectMany(c => c.Channel);
            var guide = GetFromService<Guide>(cancellationToken, typeof(Guide),
                    "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                    program.ChannelEPGID,
                    GeneralExtensions.FloatDateTime(GeneralExtensions.GetProgramTime(program.Start)),
                    GeneralExtensions.FloatDateTime(GeneralExtensions.GetProgramTime(program.Stop)));

            var episodeTitle = program.EpisodeTitle;

            if (!timers.Timer.Any(t =>
            t.ChannelId == program.ChannelId &&
            GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.PreEPG) == GeneralExtensions.GetProgramTime(program.Start) &&
            GeneralExtensions.GetScheduleTime(t.Date, t.Start).AddMinutes(t.Duration).AddMinutes(-t.PostEPG) == GeneralExtensions.GetProgramTime(program.Stop)))
            {
                var builder = new StringBuilder("api/timeradd.html?");

                builder.AppendFormat("title={0}&", (episodeTitle != null ? program.Name + " - " + episodeTitle : program.Name).ToUrlString());
                builder.AppendFormat("series={0}&", program.Name.ToUrlString());
                builder.AppendFormat("encoding={0}&", 255);
                builder.AppendFormat("ch={0}&", program.ChannelId);
                builder.AppendFormat("dor={0}&", GeneralExtensions.GetProgramTime(program.Start).ToDelphiDate());
                builder.AppendFormat("start={0}&", GeneralExtensions.GetProgramTime(program.Start).AddMinutes(-(double)Configuration.TimerPrePadding).ToLocalTime().TimeOfDay.TotalMinutes);
                builder.AppendFormat("stop={0}&", GeneralExtensions.GetProgramTime(program.Stop).AddMinutes((double)Configuration.TimerPostPadding).ToLocalTime().TimeOfDay.TotalMinutes);
                builder.AppendFormat("pre={0}&", Configuration.TimerPrePadding);
                builder.AppendFormat("post={0}&", Configuration.TimerPostPadding);
                builder.AppendFormat("after={0}&", !String.IsNullOrWhiteSpace(Configuration.TimerTask) ? Configuration.TimerTask.ToUrlString() : String.Empty);

                builder.Remove(builder.Length - 1, 1);

                Plugin.Logger.Info("Create new schedule: {0}, StartTime: {1}, EndTime: {2}, ChannelId: {3}", program.Name, program.Start, program.Stop, program.ChannelId);
                return Task.FromResult(GetToService(cancellationToken, builder.ToString()));
            }

            return Task.Delay(0);
        }
    }
}