using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;

using MediaBrowser.Plugins.DVBViewer.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services
{
    public class StreamingService : ProxyService
    {
        public MediaSourceInfo GetLiveTvStream(DVBViewerOptions configuration, string baseUrl, string channelId)
        {
            var channelStreamId = channelId.Split('-')[0];
            var streamUrl = BuildRequestUrl(baseUrl, "upnp/channelstream/{0}.ts", channelStreamId);

            // need to change the port
            if (Uri.TryCreate(streamUrl, UriKind.Absolute, out Uri uri))
            {
                var builder = new UriBuilder(uri);
                builder.Port = configuration.StreamingPort;

                streamUrl = builder.Uri.ToString();
            }

            var mediaSourceInfo = new MediaSourceInfo
            {
                Id = "dvbviewer_" + channelStreamId,
                Container = "ts",
                Path = streamUrl,
                Protocol = MediaProtocol.Http,
                IsInfiniteStream = true,
                RequiresOpening = false,
                RequiresClosing = false,
                SupportsDirectPlay = false,
                SupportsDirectStream = true,
                SupportsTranscoding = true
            };

            if (!string.IsNullOrEmpty(configuration.UserName))
            {
                string authInfo = string.Format("{0}:{1}", configuration.UserName, configuration.Password ?? string.Empty);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

                mediaSourceInfo.RequiredHttpHeaders = new Dictionary<string, string> { { "Authentication", "Basic " + authInfo } };
            }

            mediaSourceInfo.InferTotalBitrate();

            return mediaSourceInfo;
        }

        public string GetChannelLogo(CancellationToken cancellationToken, DVBViewerOptions configuration, string baseUrl, Channel channel)
        {
            var cachePath = Path.Combine(Plugin.ConfigurationManager.CommonApplicationPaths.CachePath, "dvbviewer");

            var localImagePath = string.Empty;
            var localLogoPath = Path.Combine(cachePath, channel.Id + "-logo.png");
            var localLandscapePath = Path.Combine(cachePath, channel.Id + "-landscape.png");
            var localPosterPath = Path.Combine(cachePath, channel.Id + "-poster.png");

            if (Directory.Exists(cachePath))
            {
                localImagePath = Directory.EnumerateFiles(cachePath, string.Format(@"{0}.*", channel.Id), SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(localImagePath))
                {
                    var localImageSize = new FileInfo(localImagePath).Length;

                    var response = HeadHttpResponse(cancellationToken, configuration, baseUrl, "api/{0}", channel.Logo).Result;

                    var filetype = response.Content.Headers.ContentType.MediaType;

                    if (!filetype.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                    {
                        return localImagePath;
                    }

                    var remoteImageSize = response.Content.Headers.ContentLength.GetValueOrDefault();

                    if (localImageSize == remoteImageSize)
                    {
                        return localImagePath;
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(cachePath);
            }

            try
            {
                var response = GetHttpResponse(cancellationToken, configuration, baseUrl, "api/{0}", channel.Logo).Result;

                var filetype = response.Content.Headers.ContentType.MediaType;

                if (!filetype.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var fileExtension = filetype.Split('/')[1];

                localImagePath = Path.Combine(cachePath, channel.Id + "." + fileExtension);

                var imageArray = response.Content.ReadAsByteArrayAsync().Result;

                File.WriteAllBytes(localImagePath, imageArray);
            }
            catch (Exception)
            {
                return null;
            }

            Plugin.ImageCreator.CreateLogoImage(localImagePath, localLogoPath);
            Plugin.ImageCreator.CreateLandscapeImage(localImagePath, localLandscapePath);
            Plugin.ImageCreator.CreatePosterImage(localImagePath, localPosterPath);

            if (!File.Exists(localImagePath))
            {
                return null;
            }

            return localImagePath;
        }
    }
}