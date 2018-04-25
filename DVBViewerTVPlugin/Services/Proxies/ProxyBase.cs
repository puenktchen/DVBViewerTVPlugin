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
using MediaBrowser.Plugins.DVBViewer.Configuration;
using MediaBrowser.Plugins.DVBViewer.Interfaces;
using MediaBrowser.Plugins.DVBViewer.Services.Exceptions;

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

        /// <summary>
        /// Gets the plugin configuration.
        /// </summary>
        /// <value>
        /// The plugin configuration.
        /// </value>
        public PluginConfiguration Configuration { get { return Plugin.Instance.Configuration;  } }

        protected IHttpClient HttpClient { get; private set; }
        public IJsonSerializer JsonSerializer { get; private set; }
        public IXmlSerializer XmlSerializer { get; private set; }
        public IPluginLogger Logger { get; set; }

        /// <summary>
        /// Retrieves a URL for a given action, allows the endpoint to be overriden
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        protected String GetUrl(String action, params object[] args)
        {
            if (string.Equals(Configuration.ApiHostName, "localhost", StringComparison.CurrentCultureIgnoreCase) || Configuration.ApiHostName == "127.0.0.1")
            {
                Configuration.ApiHostName = LocalIPAddress().ToString();
            }
            var baseUrl = String.Format("http://{0}:{1}/", Configuration.ApiHostName, Configuration.ApiPortNumber);
            return String.Concat(baseUrl, String.Format(action, args));
        }

        /// <summary>
        /// Retrieves data from the service for a given action
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="action">The action.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="MediaBrowser.Plugins.DVBViewer.Services.Exceptions.ServiceAuthenticationException">There was a problem authenticating with the DVBViewer Media Server</exception>
        protected TResult GetFromService<TResult>(CancellationToken cancellationToken, Type type, String action, params object[] args)
        {
            var configuration = Plugin.Instance.Configuration;
            var request = new HttpRequestOptions()
            {
                Url = GetUrl(action, args),
                RequestContentType = "application/x-www-form-urlencoded",
                LogErrorResponseBody = true,
                LogRequest = true,
            };

            if (configuration.RequiresAuthentication)
            {
                string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.RequestHeaders["Authorization"] = "Basic " + authInfo;
            }

            try
            {
                var task = HttpClient.Get(request);
                using (var stream = task.Result)
                {
                    return (TResult)XmlSerializer.DeserializeFromStream(type, stream);
                }
                
            }
            catch (AggregateException aggregateException)
            {
                var exception = aggregateException.Flatten().InnerExceptions.OfType<MediaBrowser.Model.Net.HttpException>().FirstOrDefault();
                if (exception != null && exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ServiceAuthenticationException("There was a problem authenticating with the DVBViewer Media Server", exception);
                }

                throw;
            }
        }

        protected Task GetToService(CancellationToken cancellationToken, String action, params object[] args)
        {
            var configuration = Plugin.Instance.Configuration;
            var request = new HttpRequestOptions()
            {
                Url = GetUrl(action, args),
                LogErrorResponseBody = true,
                LogRequest = true,
            };

            if (configuration.RequiresAuthentication)
            {
                string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.RequestHeaders["Authorization"] = "Basic " + authInfo;
            }

            try
            {
                return Task.FromResult(HttpClient.GetResponse(request).Result.StatusCode);
            }
            catch (AggregateException aggregateException)
            {
                var exception = aggregateException.Flatten().InnerExceptions.OfType<Model.Net.HttpException>().FirstOrDefault();
                if (exception != null && exception.StatusCode ==  HttpStatusCode.Unauthorized)
                {
                    throw new ServiceAuthenticationException("There was a problem authenticating with the DVBViewer Media Server", exception);
                }

                throw;
            }
        }

        protected Task PostToService(CancellationToken cancellationToken, String action, Dictionary<string, string> header)
        {
            var configuration = Plugin.Instance.Configuration;
            var request = new HttpRequestOptions()
            {
                Url = GetUrl(action),
                RequestContentType = "application/xml",
                //RequestContentType = "application/x-www-form-urlencoded",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.87 Safari/537.36",
                EnableKeepAlive = true,
                LogErrorResponseBody = true,
                LogRequest = true,
            };

            request.SetPostData(header);

            if (configuration.RequiresAuthentication)
            {
                string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.RequestHeaders["Authorization"] = "Basic " + authInfo;
            }

            try
            {
                return Task.FromResult(HttpClient.Post(request));
            }
            catch (AggregateException aggregateException)
            {
                var exception = aggregateException.Flatten().InnerExceptions.OfType<MediaBrowser.Model.Net.HttpException>().FirstOrDefault();
                if (exception != null && exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ServiceAuthenticationException("There was a problem authenticating with the DVBViewer Media Server", exception);
                }

                throw;
            }
        }

        protected ImageStream GetImageFromService(CancellationToken cancellationToken, String type, String action, params object[] args)
        {
            var configuration = Plugin.Instance.Configuration;
            var request = new HttpRequestOptions()
            {
                Url = GetUrl(action, args),
                LogErrorResponseBody = true,
                LogRequest = true,
            };

            if (configuration.RequiresAuthentication)
            {
                string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.RequestHeaders["Authorization"] = "Basic " + authInfo;
            }

            ImageStream imageStream = new ImageStream();
            try
            {
                imageStream.Stream = HttpClient.GetResponse(request).Result.Content;

                if (String.Equals(type, "jpg", StringComparison.OrdinalIgnoreCase))
                    imageStream.Format = Model.Drawing.ImageFormat.Jpg;
                if (String.Equals(type, "png", StringComparison.OrdinalIgnoreCase))
                    imageStream.Format = Model.Drawing.ImageFormat.Png;
                if (String.Equals(type, "bmp", StringComparison.OrdinalIgnoreCase))
                    imageStream.Format = Model.Drawing.ImageFormat.Bmp;

                return imageStream;
            }
            catch (AggregateException aggregateException)
            {
                var exception = aggregateException.Flatten().InnerExceptions.OfType<MediaBrowser.Model.Net.HttpException>().FirstOrDefault();
                if (exception != null && exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ServiceAuthenticationException("There was a problem authenticating with the DVBViewer Media Server", exception);
                }

                throw;
            }
        }

        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}