using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer
{
    /// <summary>
    /// Provides DVBViewer Media Server integration for Emby
    /// </summary>
    public class DVBViewerTvService : ILiveTvService
    {
        private static StreamingDetails _currentStreamDetails;

        public DateTime LastRecordingChange = DateTime.MinValue;

        public string HomePageUrl
        {
            get { return "https://github.com/puenktchen/DVBViewerTVPlugin"; }
        }

        public string Name
        {
            get { return "DVBViewer (DMS)"; }
        }

    #region General

        public Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            LiveTvServiceStatusInfo result;

            var configurationValidationResult = Plugin.Instance.Configuration.Validate();
            var pluginVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Validate configuration first
            if (!configurationValidationResult.IsValid)
            {
                result = new LiveTvServiceStatusInfo()
                {
                    HasUpdateAvailable = false,
                    Status = LiveTvServiceStatus.Unavailable,
                    StatusMessage = "Cannot connect to DVBViewer Media Server - check your settings",
                    Version = String.Format("DVBViewer Live TV Plugin V{0}", pluginVersion)
                };
            }
            else
            {
                try
                {
                    var serviceVersion = Plugin.TvProxy.GetStatusInfo(cancellationToken).Version();

                    result = new LiveTvServiceStatusInfo()
                    {
                        HasUpdateAvailable = false,
                        Status = LiveTvServiceStatus.Ok,
                        StatusMessage = "Successfully connected to DVBViewer Media Server API",
                        Version = String.Format("DVBViewer Live TV Plugin V{0} - {1}", pluginVersion, serviceVersion)
                    };

                }
                catch (Exception ex)
                {
                    Plugin.Logger.Error(ex, "Exception occured getting the DVBViewer Media Server status");

                    result = new LiveTvServiceStatusInfo()
                    {
                        HasUpdateAvailable = false,
                        Status = LiveTvServiceStatus.Unavailable,
                        StatusMessage = "Cannot connect to DVBViewer Media Server - check your settings",
                        Version = String.Format("DVBViewer Live TV Plugin V{0}", pluginVersion)
                    };
                }
            }

            return Task.FromResult(result);
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    #endregion

    #region Channels

        public Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetChannels(cancellationToken));
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetPrograms(cancellationToken, channelId, startDateUtc, endDateUtc));
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    #endregion

    #region Recordings

        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            return new List<RecordingInfo>();
        }

        public Task<IEnumerable<MyRecordingInfo>> GetAllRecordingsAsync(CancellationToken cancellationToken)
        {
            if (Plugin.Instance.Configuration.EnableRecordingImport)
            {
                return Task.FromResult(Plugin.TvProxy.GetRecordings(cancellationToken));
            }
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            Plugin.TvProxy.DeleteRecording(recordingId, cancellationToken);
            LastRecordingChange = DateTime.UtcNow;
            return Task.Delay(0, cancellationToken);
        }

    #endregion

    #region Timers

        public Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program)
        {
            var scheduleDefaults = Plugin.TvProxy.GetScheduleDefaults(cancellationToken);
            var scheduleDayOfWeek = new List<DayOfWeek>();

            if (program != null)
                scheduleDayOfWeek.Add(program.StartDate.ToLocalTime().DayOfWeek);

            return Task.FromResult(new SeriesTimerInfo()
            {
                IsPostPaddingRequired = scheduleDefaults.PostRecordInterval.Ticks > 0,
                IsPrePaddingRequired = scheduleDefaults.PreRecordInterval.Ticks > 0,
                PostPaddingSeconds = (Int32)scheduleDefaults.PostRecordInterval.TotalSeconds,
                PrePaddingSeconds = (Int32)scheduleDefaults.PreRecordInterval.TotalSeconds,
                RecordNewOnly = true,
                RecordAnyChannel = false,
                RecordAnyTime = false,
                Days = scheduleDayOfWeek,
                SkipEpisodesInLibrary = Plugin.Instance.Configuration.SkipAlreadyInLibrary ? true : false,
            });
        }

        public Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetSchedulesFromMemory(cancellationToken));
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            var result = Task.FromResult(Plugin.TvProxy.CreateSchedule(cancellationToken, info));
            LastRecordingChange = DateTime.UtcNow;
            return result;
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            var result = Task.FromResult(Plugin.TvProxy.ChangeSchedule(cancellationToken, info));
            LastRecordingChange = DateTime.UtcNow;
            return result;
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var result = Task.FromResult(Plugin.TvProxy.DeleteSchedule(cancellationToken, timerId));
            LastRecordingChange = DateTime.UtcNow;
            return result;
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetSeriesSchedulesFromMemory(cancellationToken));
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.CreateSeriesSchedule(cancellationToken, info));
        }

        public Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.ChangeSeriesSchedule(cancellationToken, info));
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.DeleteSeriesSchedule(cancellationToken, timerId));
        }

    #endregion

    #region Streaming

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _currentStreamDetails = Plugin.StreamingProxy.GetLiveTvStream(cancellationToken, channelId);
            return Task.FromResult(_currentStreamDetails.SourceInfo);
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MediaSourceInfo> GetRecordingStream(string recordingId, string streamId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    #endregion

    #region Events

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public event EventHandler DataSourceChanged;

    #endregion

    }
}