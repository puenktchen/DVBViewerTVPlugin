using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Plugins.DVBViewer.Services.Proxies;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public class RefreshTimersTask : ProxyBase, IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;

        public RefreshTimersTask(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _libraryManager = libraryManager;
        }

        public string Category => "Live TV";
        public string Key => "DVBViewerRefreshTimersTask";
        public string Name => "Refresh DVBViewer timers";
        public string Description => "Refresh DVBViewer timers and if enabled, check against Emby library items";

        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => false;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            TaskTriggerInfo startuptrigger = new TaskTriggerInfo();
            startuptrigger.Type = "StartupTrigger";

            TaskTriggerInfo intervaltrigger = new TaskTriggerInfo();
            intervaltrigger.Type = "IntervalTrigger";
            intervaltrigger.IntervalTicks = 9000000000;

            List<TaskTriggerInfo> tasktrigger = new List<TaskTriggerInfo>();
            tasktrigger.Add(startuptrigger);
            tasktrigger.Add(intervaltrigger);

            return tasktrigger;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            Plugin.TvProxy.RefreshSchedules(cancellationToken);

            SkipTimers(cancellationToken);

            return Task.Delay(0, cancellationToken);
        }

        private void SkipTimers(CancellationToken cancellationToken)
        {
            var timers = Plugin.TvProxy.GetSchedulesFromMemory(cancellationToken);

            foreach (var timer in timers.Where(x => x.Status.Equals(Model.LiveTv.RecordingStatus.New) && x.Priority.Equals(49)))
            {
                if (IsAlreadyInLibrary(timer))
                {
                    Plugin.Logger.Info("Cancel Schedule: \"{0}\" already exists as Emby library item, trying cancel now", timer.Name);
                    Task.FromResult(GetToService(cancellationToken, "api/timeredit.html?id={0}&enable=0", timer.Id));
                }
            }

            Plugin.TvProxy.RefreshSchedules(cancellationToken);
        }

        private bool IsAlreadyInLibrary(TimerInfo timer)
        {
            if (!String.IsNullOrEmpty(timer.Name) && !String.IsNullOrEmpty(timer.EpisodeTitle))
            {
                string seriesName = Regex.Replace(Regex.Split(timer.Name, @"\s\-\s").FirstOrDefault(), @"\s\W[a-zA-Z]?[0-9]{1,3}?\W$", String.Empty);
                string episodeName = Regex.Replace(timer.EpisodeTitle, @"(^[(]?[s]?[0-9]*[e|x|\.][0-9]*[^\w]+)|(\s[(]?[s]?[0-9]*[e|x|\.][0-9]*[)]?$)", String.Empty, RegexOptions.IgnoreCase);
                string movieName = Regex.Replace(timer.Name, @"\s\W[0-9]+\W$", String.Empty);

                if (Plugin.Instance.Configuration.SkipAlreadyInLibraryProfile == "Season and Episode Numbers" && timer.EpisodeNumber.HasValue && timer.SeasonNumber.HasValue)
                {
                    var seriesIds = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Series).Name },
                        Name = seriesName,
                        IsVirtualItem = false,

                    }).ToArray();

                    if (seriesIds.Length == 0)
                    {
                        return false;
                    }

                    var episode = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Episode).Name },
                        ParentIndexNumber = timer.SeasonNumber.Value,
                        IndexNumber = timer.EpisodeNumber.Value,
                        AncestorIds = seriesIds,
                        IsVirtualItem = false,
                        IsMissing = false,
                        Limit = 1
                    });

                    if (episode.Count > 0)
                    {
                        return true;
                    }
                }

                if (Plugin.Instance.Configuration.SkipAlreadyInLibraryProfile == "Episode Name" && !string.IsNullOrWhiteSpace(timer.EpisodeTitle))
                {
                    var seriesIds = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Series).Name },
                        Name = seriesName,
                        IsVirtualItem = false,

                    }).ToArray();

                    if (seriesIds.Length == 0)
                    {
                        return false;
                    }

                    var episodename = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Episode).Name },
                        NameContains = episodeName,
                        AncestorIds = seriesIds,
                        IsVirtualItem = false,
                        IsMissing = false,
                        Limit = 1
                    });

                    if (episodename.Count > 0)
                    {
                        return true;
                    }
                }

                if (timer.IsMovie && !timer.EpisodeNumber.HasValue && !timer.SeasonNumber.HasValue)
                {
                    var movie = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Movie).Name },
                        NameContains = movieName,
                        IsVirtualItem = false,

                    }).Select(i => i.ToString("N")).ToArray();

                    if (movie.Length > 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
    }
}
