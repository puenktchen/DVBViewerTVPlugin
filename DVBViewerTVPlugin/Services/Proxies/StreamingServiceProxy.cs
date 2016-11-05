using System;
using System.Threading;
using System.Web;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services.Proxies
{
    /// <summary>
    /// Provides access to the DVBViewer Recording Service streaming functionality
    /// </summary>
    public class StreamingServiceProxy : ProxyBase
    {
        private readonly INetworkManager _networkManager;
        private readonly TVServiceProxy _tvProxy;

        public StreamingServiceProxy(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, INetworkManager networkManager, TVServiceProxy tvProxy)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _networkManager = networkManager;
            _tvProxy = tvProxy;
        }

        public StreamingDetails GetRecordingStream(CancellationToken cancellationToken, String recordingId, TimeSpan startPosition)
        {
            return GetStream(cancellationToken, recordingId, startPosition);
        }

        public StreamingDetails GetLiveTvStream(CancellationToken cancellationToken, String channelId)
        {
            return GetStream(cancellationToken, channelId, TimeSpan.Zero);
        }

        private String GetTVStreamingURL(CancellationToken cancellationToken, string channelId)
        {
            string port = GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml").StreamPort();
            string channelNr = channelId;
            string url = String.Format("http://{0}:{1}/upnp/channelstream/{2}.ts", Configuration.ApiHostName, port, channelNr);
            Plugin.Logger.Info("DVBViewer UPNP streaming url: {0}", url);
            return url;
        }

        private String GetRecStreamingURL(CancellationToken cancellationToken, string recordingId)
        {
            string port = GetFromService<Settings>(cancellationToken, typeof(Settings), "api/getconfigfile.html?file=config%5Cservice.xml").MediaPort();
            string recId = recordingId.Remove(0, 10);
            string url = String.Format("http://{0}:{1}/upnp/recordings/{2}.ts", Configuration.ApiHostName, port, recId);
            Plugin.Logger.Info("DVBViewer UPNP streaming url: {0}", url);
            return url;
        }

        private StreamingDetails GetStream(CancellationToken cancellationToken, string itemId, TimeSpan startPosition)
        {
            var identifier = HttpUtility.UrlEncode(String.Format("{0}-{1:yyyyMMddHHmmss}", itemId, DateTime.UtcNow));   
            var url = "";

            if (itemId.StartsWith("Recording"))
            {
                url = GetRecStreamingURL(cancellationToken, itemId);
            }
            else
            {
                url = GetTVStreamingURL(cancellationToken, itemId);
            }

            var streamingDetails = new StreamingDetails()
            {
                StreamIdentifier = identifier,
                SourceInfo = new MediaSourceInfo()
                {
                    Path = url,
                    Protocol = MediaProtocol.Http,
                    Id = identifier, //itemId,
                }
            };

            return streamingDetails;
        }
    }
}