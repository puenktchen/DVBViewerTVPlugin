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

        public TVServiceProxy(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, StreamingServiceProxy wssProxy)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _wssProxy = wssProxy;
        }

        public Task<Channels> GetChannelList(string baseUrl, DVBViewerOptions configuration, CancellationToken cancellationToken)
        {
            return GetFromService<Channels>(baseUrl, configuration, cancellationToken, typeof(Channels), "api/getchannelsxml.html?logo=1");
        }

        public async Task<List<ChannelInfo>> GetChannels(string baseUrl, DVBViewerOptions configuration, CancellationToken cancellationToken)
        {
            var channels = await GetChannelList(baseUrl, configuration, cancellationToken).ConfigureAwait(false);

            return channels.Root.ChannelGroup.SelectMany(c => c.Channel).Select(c =>
            {
                var channelInfo = new ChannelInfo()
                {
                    Id = c.Id,
                    Name = c.Name,
                    ChannelType = (GeneralExtensions.HasVideoFlag(c.Flags)) ? ChannelType.TV : ChannelType.Radio,
                };

                if (!String.IsNullOrEmpty(c.Logo))
                {
                    channelInfo.ImageUrl = _wssProxy.GetChannelLogo(baseUrl, configuration, c);
                }

                return channelInfo;

            }).ToList();
        }

        public async Task<List<ProgramInfo>> GetPrograms(string baseUrl, DVBViewerOptions configuration, string channelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc, CancellationToken cancellationToken)
        {
            var channel = (await GetChannelList(baseUrl, configuration, cancellationToken).ConfigureAwait(false))
                .Root
                .ChannelGroup
                .SelectMany(c => c.Channel)
                .FirstOrDefault(c => string.Equals(c.Id, channelId, StringComparison.OrdinalIgnoreCase));

            var response = await GetFromService<Guide>(baseUrl, configuration, cancellationToken, typeof(Guide),
                "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                channel.EPGID,
                GeneralExtensions.FloatDateTimeOffset(startDateUtc),
                GeneralExtensions.FloatDateTimeOffset(endDateUtc)).ConfigureAwait(false);

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
                    ChannelId = channelId,
                    StartDate = GeneralExtensions.GetProgramTime(p.Start),
                    EndDate = GeneralExtensions.GetProgramTime(p.Stop),
                    Overview = p.Overview,
                    Etag = p.EitContent,
                };

                program.IsSeries = true;

                if (program.IsSeries && p.Name != p.EpisodeTitleRegEx)
                {
                    program.EpisodeTitle = p.EpisodeTitleRegEx;
                }

                return program;

            }).ToList();
        }
    }
}
