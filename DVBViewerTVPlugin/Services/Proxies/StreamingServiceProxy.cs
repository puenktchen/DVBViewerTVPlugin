using System;
using System.IO;
using System.Net;
using System.Threading;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Helpers;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services.Proxies
{
    /// <summary>
    /// Provides access to the DVBViewer Media Server streaming functionality
    /// </summary>
    public class StreamingServiceProxy : ProxyBase
    {
        private readonly INetworkManager _networkManager;

        public StreamingServiceProxy(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, INetworkManager networkManager)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _networkManager = networkManager;
        }

        /// <summary>
        /// Gets a live tv stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="channelId">The channel to stream.</param>
        /// <returns></returns>
        public StreamingDetails GetLiveTvStream(CancellationToken cancellationToken, String channelId)
        {
            var identifier = WebUtility.UrlEncode(String.Format("{0}-{1:yyyyMMddHHmmss}", channelId, DateTime.UtcNow));

            var streamingDetails = new StreamingDetails()
            {
                SourceInfo = new MediaSourceInfo()
            };

            streamingDetails.StreamIdentifier = identifier;
            streamingDetails.SourceInfo.Id = identifier;
            streamingDetails.SourceInfo.Protocol = MediaProtocol.Http;
            streamingDetails.SourceInfo.ReadAtNativeFramerate = true;
            streamingDetails.SourceInfo.IsInfiniteStream = true;
            streamingDetails.SourceInfo.SupportsProbing = true;
            streamingDetails.SourceInfo.Path = String.Format("http://{0}:{1}/upnp/channelstream/{2}.ts", Configuration.ApiHostName, Configuration.StreamPortNumber, channelId);

            return streamingDetails;
        }

        /// <summary>
        /// Gets the video stream for an existing recording
        /// </summary>
        /// <param name="recordingId">The recording id.</param>
        /// <returns></returns>
        public String GetRecordingStream(String recordingId)
        {
            return String.Format("http://{0}:{1}/upnp/recordings/{2}.ts", Configuration.ApiHostName, Configuration.MediaPortNumber, recordingId);
        }

        /// <summary>
        /// Gets the recording image.
        /// </summary>
        /// <param name="recordingImage">The recording image.</param>
        /// <returns></returns>
        public String GetRecordingImage(String recordingImage)
        {
            if (Configuration.RequiresAuthentication)
            {
                return String.Format("http://{0}:{1}@{2}:{3}/upnp/thumbnails/video/{2}", Configuration.UserName, Configuration.Password, Configuration.ApiHostName, Configuration.ApiPortNumber, recordingImage);
            }

            return String.Format("http://{0}:{1}/upnp/thumbnails/video/{2}", Configuration.ApiHostName, Configuration.ApiPortNumber, recordingImage);
        }

        /// <summary>
        /// Gets the channel logo.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public String GetChannelLogo(Channel channel)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var remoteUrl = String.Format("http://{0}:{1}/api/{2}", Configuration.ApiHostName, Configuration.ApiPortNumber, channel.Logo);
            var localImagePath = Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + ".png");
            var localLandscapePath = Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + "-landscape.png");
            var localPosterPath = Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + "-poster.png");
            var localLogoPath = Path.Combine(pluginPath, "channellogos", String.Join("", channel.Name.Split(Path.GetInvalidFileNameChars())) + "-logo.png");

            if (Configuration.ProgramImages || Configuration.RequiresAuthentication)
            {

                if (!Directory.Exists(Path.Combine(pluginPath, "channellogos")))
                {
                    Directory.CreateDirectory(Path.Combine(pluginPath, "channellogos"));
                }

                try
                {
                    using (WebClient client = new WebClient())
                    {
                        if (Configuration.RequiresAuthentication)
                            client.Credentials = new NetworkCredential(Configuration.UserName, Configuration.Password);
                        client.DownloadFile(new Uri(remoteUrl), localImagePath);
                    }
                }
                catch (WebException)
                {
                    Plugin.Logger.Info("Could not download logo for Channel: {0}", channel.Name);
                    return null;
                }

                if (Configuration.EnableImageProcessing)
                {
                    ImageHelper.CreateLandscapeImage(localImagePath, localLandscapePath);
                    ImageHelper.CreatePosterImage(localImagePath, localPosterPath);
                    ImageHelper.CreateLogoImage(localImagePath, localLogoPath);

                    return localLogoPath;
                }

                return localImagePath;
            }

            return remoteUrl;
        }
    }
}