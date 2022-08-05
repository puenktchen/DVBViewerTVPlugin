using System;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Services.Proxies
{
    /// <summary>
    /// Provides access to the DVBViewer Media Server streaming functionality
    /// </summary>
    public class StreamingServiceProxy : ProxyBase
    {
        public StreamingServiceProxy(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer)
            : base(httpClient, jsonSerializer, xmlSerializer)
        {
        }

        /// <summary>
        /// Gets the channel logo.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public String GetChannelLogo(string baseUrl, DVBViewerOptions configuration, Channel channel)
        {
            var url = String.Format("{0}/api/{1}", baseUrl.TrimEnd('/'), channel.Logo);

            if (!string.IsNullOrEmpty(configuration.UserName))
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    var builder = new UriBuilder(uri);
                    builder.UserName = configuration.UserName;
                    builder.Password = configuration.Password;

                    url = builder.Uri.ToString();
                }
            }

            return url;
        }
    }
}