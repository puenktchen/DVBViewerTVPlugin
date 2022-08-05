using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Plugins.DVBViewer.Helpers;
using MediaBrowser.Plugins.DVBViewer.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services
{
    public class TVService : ProxyService
    {
        public Task<ChannelGroups> GetRootChannelGroups(CancellationToken cancellationToken, DVBViewerOptions configuration, string baseUrl)
        {
            return GetHttpContent<ChannelGroups>(cancellationToken, configuration, typeof(ChannelGroups), baseUrl, "api/getchannelsxml.html?rootsonly=1");
        }

        public Task<Channels> GetChannelList(CancellationToken cancellationToken, DVBViewerOptions configuration, string baseUrl)
        {
            if (configuration.ImportFavoritesOnly)
            {
                return GetHttpContent<Channels>(cancellationToken, configuration, typeof(Channels), baseUrl, "api/getchannelsxml.html?favonly=1&logo=1");
            }
            else
            {
                return GetHttpContent<Channels>(cancellationToken, configuration, typeof(Channels), baseUrl, "api/getchannelsxml.html?root={0}&logo=1", configuration.RootChannelGroup);
            }
        }

        public async Task<List<ChannelInfo>> GetChannels(CancellationToken cancellationToken, DVBViewerOptions configuration, string baseUrl)
        {
            var channelList = await GetChannelList(cancellationToken, configuration, baseUrl).ConfigureAwait(false);

            var channels = new List<Channel>();

            if (!configuration.ImportRadioChannels)
            {
                channels = channelList.Root.ChannelGroup.SelectMany(c => c.Channel).SkipWhile(c => !GeneralExtensions.HasVideoFlag(c.Flags)).ToList();
            }
            else
            {
                channels = channelList.Root.ChannelGroup.SelectMany(c => c.Channel).ToList();
            }

            return channels.Select((c, index) =>
            {
                var channelInfo = new ChannelInfo()
                {
                    Id = string.Format("{0}-{1}", c.Id, c.EPGID),
                    Name = c.Name,
                    Number = (index + 1).ToString(),
                    ChannelType = GeneralExtensions.HasVideoFlag(c.Flags) ? ChannelType.TV : ChannelType.Radio,
                    ImageUrl = Plugin.StreamingService.GetChannelLogo(cancellationToken, configuration, baseUrl, c)
                };

                return channelInfo;

            }).ToList();
        }

        public async Task<List<ProgramInfo>> GetPrograms(CancellationToken cancellationToken, DVBViewerOptions configuration, string baseUrl, string channelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc)
        {
            var genreMapper = new GenreMapper();
            var channelStreamId = channelId.Split('-')[0];
            var channelEpgId = channelId.Split('-')[1];

            var response = await GetHttpContent<Guide>(cancellationToken, configuration, typeof(Guide), baseUrl,
                "api/epg.html?lvl=2&channel={0}&start={1}&end={2}",
                channelEpgId,
                GeneralExtensions.FloatDateTimeOffset(startDateUtc),
                GeneralExtensions.FloatDateTimeOffset(endDateUtc)).ConfigureAwait(false);

            return response.Program.Select(p =>
            {
                var program = new ProgramInfo()
                {
                    Name = p.Name,
                    EpisodeNumber = p.EpisodeNumber,
                    SeasonNumber = p.SeasonNumber,
                    ProductionYear = p.ProductionYear,
                    Overview = p.Overview,
                    SeriesId = p.Name,
                    ShowId = p.Name,
                    Etag = p.EitContent,
                    StartDate = GeneralExtensions.GetProgramTime(p.Start),
                    EndDate = GeneralExtensions.GetProgramTime(p.Stop)
                };

                if (!string.IsNullOrWhiteSpace(program.Etag))
                {
                    genreMapper.SetProgramCategories(program);
                }

                if (program.IsSeries && p.Name != p.EpisodeTitleRegEx)
                {
                    program.EpisodeTitle = p.EpisodeTitleRegEx;
                }

                if (configuration.RemapProgramEvents)
                {
                    if (string.IsNullOrWhiteSpace(program.Overview))
                    {
                        program.Overview = program.EpisodeTitle;
                        program.EpisodeTitle = string.Empty;
                    }
                    else
                    {
                        program.EpisodeTitle = string.Empty;
                    }
                }

                if (p.Icon != null)
                {
                    program.ImageUrl = p.Icon.Src;
                }
                else
                {
                    var cachePath = Path.Combine(Plugin.ConfigurationManager.CommonApplicationPaths.CachePath, "dvbviewer");
                    var logoImage = Path.Combine(cachePath, channelStreamId + "-logo.png");
                    var landscapeImage = Path.Combine(cachePath, channelStreamId + "-landscape.png");
                    var posterImage = Path.Combine(cachePath, channelStreamId + "-poster.png");

                    if (File.Exists(logoImage))
                        program.LogoImageUrl = logoImage;
                    if (File.Exists(landscapeImage))
                        program.ThumbImageUrl = landscapeImage;
                    if (File.Exists(posterImage))
                        program.ImageUrl = posterImage;
                }

                return program;

            }).ToList();
        }
    }
}
