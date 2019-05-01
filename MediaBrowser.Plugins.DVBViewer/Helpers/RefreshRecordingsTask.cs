using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;
using MediaBrowser.Plugins.DVBViewer.Services.Proxies;

using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public class RefreshRecordingsTask : ProxyBase, IScheduledTask, IConfigurableScheduledTask
    {
        private IChannelManager _channelManager;
        private ILibraryManager _libraryManager;

        public RefreshRecordingsTask(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, IChannelManager channelManager, ILibraryManager libraryManager)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _channelManager = channelManager;
            _libraryManager = libraryManager;
        }

        public string Category => "Live TV";
        public string Key => "DVBViewerRefreshRecordingsTask";
        public string Name => "Refresh DVBViewer recordings";
        public string Description => "Refreshes DVBViewer recordings at an internal interval of 90 seconds. Please do not delete the Startup Trigger!";

        public bool IsHidden => false;
        public bool IsEnabled => true;
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
            SetRecordingContentChanged(cancellationToken);

            return Task.Delay(0, cancellationToken);
        }

        private async Task SetRecordingContentChanged(CancellationToken cancellationToken)
        {
            while (true)
            {
                Task.Run(() =>
                {
                    Plugin.Logger.Info("Internal Trigger fired for Task: Refresh DVBViewer Recordings");

                    Plugin.TvProxy.refreshRecordings = true;
                    var allRecordings = Plugin.TvProxy.GetRecordingsFromMemory(cancellationToken);

                    _channelManager.GetChannel<RecordingsChannel>().OnContentChanged();

                    LockRecordings(allRecordings);
                });
                await Task.Delay(TimeSpan.FromSeconds(120));
            }
        }

        private void LockRecordings(IEnumerable<MyRecordingInfo> allRecordings)
        {
            var config = Plugin.Instance.Configuration;

            var recordingChannelId = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Channel).Name },
            }).Where(c => c.Name.Equals("DVBViewer Recordings")).Select(i => i.InternalId).ToArray();

            var recordingChannelItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Video).Name, typeof(Movie).Name, typeof(Episode).Name },
                IsLocked = false,
                TopParentIds = recordingChannelId,
            });

            if (recordingChannelItems != null)
            {
                foreach (var item in recordingChannelItems)
                {
                    var recording = allRecordings.Where(r => r.Id.Equals(item.ExternalId)).FirstOrDefault();

                    if (!String.IsNullOrEmpty(recording.TmdbImage))
                    {
                        if (config.RecGenreMapping && recording.IsMovie)
                            item.SetImage(new ItemImageInfo { Path = recording.TmdbImage, Type = Model.Entities.ImageType.Primary }, 0);

                        if (!config.RecGenreMapping)
                            item.SetImage(new ItemImageInfo { Path = recording.TmdbImage, Type = Model.Entities.ImageType.Thumb }, 0);
                    }

                    item.Overview = recording.Overview;
                    item.DateCreated = recording.StartDate;
                    item.DateModified = recording.EndDate;
                    item.IsLocked = true;

                    _libraryManager.UpdateItem(item, item.Parent, ItemUpdateType.None);
                }
            }
        }
    }
}
