using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.DVBViewer.Services.Proxies
{
    /// <summary>
    /// Provides base methods for proxy classes
    /// </summary>
    public abstract class ProxyBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyBase" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="serializer">The serializer.</param>
        protected ProxyBase(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer)
        {
            HttpClient = httpClient;
            JsonSerializer = jsonSerializer;
            XmlSerializer = xmlSerializer;
        }

        protected IHttpClient HttpClient { get; private set; }
        public IJsonSerializer JsonSerializer { get; private set; }
        public IXmlSerializer XmlSerializer { get; private set; }

        /// <summary>
        /// Retrieves a URL for a given action, allows the endpoint to be overriden
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        protected String GetUrl(string baseUrl, DVBViewerOptions configuration, String action, params object[] args)
        {
            // ensure it has a trailing /
            baseUrl = baseUrl.TrimEnd('/') + "/";

            return String.Concat(baseUrl, String.Format(action, args));
        }

        /// <summary>
        /// Retrieves data from the service for a given action
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        protected async Task<TResult> GetFromService<TResult>(string baseUrl, DVBViewerOptions configuration, CancellationToken cancellationToken, Type type, String action, params object[] args)
        {
            var request = new HttpRequestOptions()
            {
                Url = GetUrl(baseUrl, configuration, action, args),
                RequestContentType = "application/x-www-form-urlencoded",
                LogErrorResponseBody = true,
                LogRequest = true,
                CancellationToken = cancellationToken
            };

            if (!string.IsNullOrEmpty(configuration.UserName))
            {
                string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.RequestHeaders["Authorization"] = "Basic " + authInfo;
            }

            using (var stream = await HttpClient.Get(request).ConfigureAwait(false))
            {
                return (TResult)XmlSerializer.DeserializeFromStream(type, stream);
            }
        }
    }
}