using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Plugins.DVBViewer
{
    /// <summary>
    /// Provides DVBViewer Media Server integration for Emby
    /// </summary>
    public class DVBViewerTvService : BaseTunerHost
    {
        public DVBViewerTvService(IServerApplicationHost applicationHost)
            : base(applicationHost)
        {
        }

        public override string Name => Plugin.StaticName;

        public override string Type => "dvbviewer";

        public override string SetupUrl => Plugin.GetPluginPageUrl(Type);

        public override bool SupportsGuideData(TunerHostInfo tuner)
        {
            return true;
        }

        public override TunerHostInfo GetDefaultConfiguration()
        {
            var tuner = base.GetDefaultConfiguration();

            tuner.Url = "http://localhost:8089";

            SetCustomOptions(tuner, new DVBViewerOptions());

            return tuner;
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<DVBViewerOptions>(tuner);
            var baseUrl = tuner.Url;

            var channels = await Plugin.TvProxy.GetChannels(baseUrl, config, cancellationToken).ConfigureAwait(false);

            foreach (var channel in channels)
            {
                channel.TunerHostId = tuner.Id;
                channel.Id = CreateEmbyChannelId(tuner, channel.Id);
            }

            return channels;
        }

        protected override async Task<List<ProgramInfo>> GetProgramsInternal(TunerHostInfo tuner, string tunerChannelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<DVBViewerOptions>(tuner);
            var baseUrl = tuner.Url;

            var list = await Plugin.TvProxy.GetPrograms(baseUrl, config, tunerChannelId, startDateUtc, endDateUtc, cancellationToken).ConfigureAwait(false);

            foreach (var item in list)
            {
                item.ChannelId = tunerChannelId;
                item.Id = GetProgramEntryId(item.ShowId, item.StartDate, item.ChannelId);
            }

            return list;
        }

        protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, BaseItem dbChannnel, ChannelInfo tunerChannel, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<DVBViewerOptions>(tuner);
            var baseUrl = tuner.Url;

            var dvbChannelId = GetTunerChannelIdFromEmbyChannelId(tuner, tunerChannel.Id);

            var url = String.Format("{0}/upnp/channelstream/{1}.ts", baseUrl.TrimEnd('/'), dvbChannelId);

            // need to change the port
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                var builder = new UriBuilder(uri);
                builder.Port = config.StreamingPort;

                url = builder.Uri.ToString();
            }
            
            var mediaSource = new MediaSourceInfo
            {
                // Make sure that it is predictable and returns the same result each time
                Path = url,
                Protocol = MediaProtocol.Http,

                RequiresOpening = false,
                RequiresClosing = false,

                Container = "ts",
                Id = "native_" + dvbChannelId,

                // this needs review but I'm not sure these values matter at this earlier stage
                SupportsDirectPlay = false,
                SupportsDirectStream = true,
                SupportsTranscoding = true,

                IsInfiniteStream = true
            };

            mediaSource.InferTotalBitrate();

            return Task.FromResult(new List<MediaSourceInfo> { mediaSource });
        }
    }
}