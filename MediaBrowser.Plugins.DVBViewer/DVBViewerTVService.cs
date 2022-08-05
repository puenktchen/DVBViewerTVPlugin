using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Plugins.DVBViewer
{
    public class DVBViewerTvService : BaseTunerHost
    {
        public static DVBViewerTvService Instance { get; private set; }

        public DVBViewerTvService(IServerApplicationHost applicationHost) : base(applicationHost)
        {
            Instance = this;
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

            var channels = await Plugin.TVService.GetChannels(cancellationToken, config, tuner.Url).ConfigureAwait(false);

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

            var programs = await Plugin.TVService.GetPrograms(cancellationToken, config, tuner.Url, tunerChannelId, startDateUtc, endDateUtc).ConfigureAwait(false);

            foreach (var program in programs)
            {
                program.ChannelId = tunerChannelId;
                program.Id = GetProgramEntryId(program.ShowId, program.StartDate, program.ChannelId);
            }

            return programs;
        }

        protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, BaseItem dbChannnel, ChannelInfo tunerChannel, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<DVBViewerOptions>(tuner);

            var dvbViewerChannelId = GetTunerChannelIdFromEmbyChannelId(tuner, tunerChannel.Id);

            var mediaSourceInfo = Plugin.StreamingService.GetLiveTvStream(config, tuner.Url, dvbViewerChannelId);

            return Task.FromResult(new List<MediaSourceInfo> { mediaSourceInfo });
        }

        public DVBViewerOptions GetConfiguration(TunerHostInfo tuner)
        {
            return GetProviderOptions<DVBViewerOptions>(tuner);
        }
    }
}