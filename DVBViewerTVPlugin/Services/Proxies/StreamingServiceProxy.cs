using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Helpers;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services.Proxies
{
    /// <summary>
    /// Provides access to the DVBViewer Recording Service streaming functionality
    /// </summary>
    public class StreamingServiceProxy : ProxyBase
    {
        private readonly INetworkManager _networkManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly TVServiceProxy _tvProxy;

        public StreamingServiceProxy(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, INetworkManager networkManager, IMediaEncoder mediaEncoder, TVServiceProxy tvProxy)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
            _networkManager = networkManager;
            _mediaEncoder = mediaEncoder;
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
            Plugin.Logger.Info("Streaming setting StreamDelay: {0}", Configuration.FFProbeAnalyzeDuration);
            Plugin.Logger.Info("Streaming setting EnableDirectPlay: {0}", Configuration.EnableDirectPlay);
            Plugin.Logger.Info("Streaming setting LimitDirectPlay to 720p: {0}", Configuration.LimitStreaming);

            var configuration = Plugin.Instance.Configuration;
            var identifier = HttpUtility.UrlEncode(String.Format("{0}-{1:yyyyMMddHHmmss}", itemId, DateTime.UtcNow));   
            var mediaInfo = new MediaInfo();
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

            if (configuration.EnableDirectPlay)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                mediaInfo = FFProbeStream(cancellationToken, url);
 
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;

                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                Plugin.Logger.Info("Probing RunTime = {0} for stream: {1}", elapsedTime, url);

                if (mediaInfo != null)
                {
                    var defaultVideoStream = mediaInfo.MediaStreams.FirstOrDefault(v => v.Type == Model.Entities.MediaStreamType.Video);
                    var defaultAudioStream = mediaInfo.MediaStreams.FirstOrDefault(a => a.Type == Model.Entities.MediaStreamType.Audio);
                    var defaultSubtitleStream = mediaInfo.MediaStreams.FirstOrDefault(s => s.Type == Model.Entities.MediaStreamType.Subtitle);
                    
                    if (defaultVideoStream != null)
                    {
                        if (!(configuration.LimitStreaming && defaultVideoStream.Height > 720))
                        {
                            streamingDetails.SourceInfo.Container = mediaInfo.Container;
                            streamingDetails.SourceInfo.Bitrate = mediaInfo.Bitrate;
                            streamingDetails.SourceInfo.MediaStreams = mediaInfo.MediaStreams;

                            if (defaultVideoStream.Height <= 576)
                            {
                                streamingDetails.SourceInfo.Bitrate = 4000000;
                            }
                            else if (defaultVideoStream.Height > 576 || defaultVideoStream.Height <= 720)
                            {
                                streamingDetails.SourceInfo.Bitrate = 7000000;
                            }
                            else if (defaultVideoStream.Height > 720)
                            {
                                streamingDetails.SourceInfo.Bitrate = 9000000;
                            }

                            if (itemId.StartsWith("Recording"))
                            {
                                streamingDetails.SourceInfo.RunTimeTicks = mediaInfo.RunTimeTicks;
                            }
                        }
                    }
                    else
                    {
                        streamingDetails.SourceInfo.Container = mediaInfo.Container;
                        streamingDetails.SourceInfo.Bitrate = defaultAudioStream.BitRate;
                        streamingDetails.SourceInfo.MediaStreams = mediaInfo.MediaStreams;
                    }
                    
                }
            }

            return streamingDetails;
        }

        private MediaInfo FFProbeStream(CancellationToken cancellationToken, String probeUrl)
        {
            string ffprobePath = _mediaEncoder.EncoderPath.Replace("ffmpeg.exe", "ffprobe.exe");
            string args = string.Format("-v quiet -print_format json -show_format -show_streams -analyzeduration {0}000 \"{1}\"", Configuration.FFProbeAnalyzeDuration, probeUrl);

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(ffprobePath);
            p.StartInfo.Arguments = args;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();
            p.Start();

            string ffprobeOutput = p.StandardOutput.ReadToEnd().Replace("\r\n", "\n");
            p.WaitForExit();

            FFProbeMediaInfo ffprobeMediaInfo = JsonSerializer.DeserializeFromString<FFProbeMediaInfo>(ffprobeOutput);

            var mediaInfo = new Model.MediaInfo.MediaInfo();
            var internalStreams = ffprobeMediaInfo.streams ?? new FFProbeMediaStreamInfo[] { };

            mediaInfo.Container = ffprobeMediaInfo.format.format_name;
            mediaInfo.Bitrate = ffprobeMediaInfo.format.bit_rate;
            mediaInfo.RunTimeTicks = (!string.IsNullOrEmpty(ffprobeMediaInfo.format.duration)) ? TimeSpan.FromSeconds(double.Parse(ffprobeMediaInfo.format.duration, CultureInfo.InvariantCulture)).Ticks : 9980000;
            mediaInfo.MediaStreams = internalStreams.Select(s => FFProbeHelper.GetMediaStream(s, ffprobeMediaInfo.format)).Where(i => i != null).ToList();

            return mediaInfo;
        }
    }
}