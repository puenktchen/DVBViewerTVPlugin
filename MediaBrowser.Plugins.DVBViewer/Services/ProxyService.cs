using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.DVBViewer.Services
{
    public abstract class ProxyService
    {
        private static HttpClient Client = new HttpClient();

        protected string BuildRequestUrl(string baseUrl, string action, params object[] args)
        {
            baseUrl = baseUrl.TrimEnd('/') + "/";

            return string.Concat(baseUrl, string.Format(action, args));
        }

        protected async Task<TResult> GetHttpContent<TResult>(CancellationToken cancellationToken, DVBViewerOptions configuration, Type type, string baseUrl, string action, params object[] args)
        {
            var response = await GetHttpResponse(cancellationToken, configuration, baseUrl, action, args);

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                return (TResult)Plugin.XmlSerializer.DeserializeFromStream(type, stream);
            }
        }

        protected async Task<HttpResponseMessage> GetHttpResponse(CancellationToken cancellationToken, DVBViewerOptions configuration, string baseUrl, string action, params object[] args)
        {
            var requestUrl = BuildRequestUrl(baseUrl, action, args);

            if (!string.IsNullOrWhiteSpace(configuration.UserName))
            {
                var authInfo = string.Format(@"{0}:{1}", configuration.UserName, configuration.Password ?? string.Empty);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authInfo);
            }

            return await Client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }

        protected async Task<HttpResponseMessage> HeadHttpResponse(CancellationToken cancellationToken, DVBViewerOptions configuration, string baseUrl, string action, params object[] args)
        {
            var requestUrl = BuildRequestUrl(baseUrl, action, args);

            if (!string.IsNullOrWhiteSpace(configuration.UserName))
            {
                var authInfo = string.Format(@"{0}:{1}", configuration.UserName, configuration.Password ?? string.Empty);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authInfo);
            }

            return await Client.SendAsync(new HttpRequestMessage { Method = HttpMethod.Head, RequestUri = new Uri(requestUrl) },cancellationToken).ConfigureAwait(false);
        }
    }
}