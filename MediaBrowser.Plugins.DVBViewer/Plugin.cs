using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Services.Proxies;

namespace MediaBrowser.Plugins.DVBViewer
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin, IHasWebPages, IHasThumbImage, IHasTranslations
    {
        public static TVServiceProxy TvProxy { get; private set; }
        public static StreamingServiceProxy StreamingProxy { get; private set; }
        public static ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        public Plugin(IXmlSerializer xmlSerializer, IHttpClient httpClient, 
            IJsonSerializer jsonSerializer, ILogger logger)
        {
            Instance = this;

            Logger = logger;

            // Create our shared service proxies
            StreamingProxy = new StreamingServiceProxy(httpClient, jsonSerializer, xmlSerializer);
            TvProxy = new TVServiceProxy(httpClient, jsonSerializer, xmlSerializer, StreamingProxy);
        }

        public static string StaticName = "DVBViewer";

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return StaticName; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "DVBViewer TV Plugin to enable Live TV streaming and scheduling.";
            }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }

        private Guid _id = new Guid("a697f993-d2de-45dc-b7ea-687363f7903e");

        public override Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
            {
                new PluginPageInfo
                {
                    Name = "dvbviewer",
                    EmbeddedResourcePath = GetType().Namespace + ".web.dvbviewer.html",
                    IsMainConfigPage = false
                },
                new PluginPageInfo
                {
                    Name = "dvbviewerjs",
                    EmbeddedResourcePath = GetType().Namespace + ".web.dvbviewer.js"
                }
            };
        }

        public TranslationInfo[] GetTranslations()
        {
            var basePath = GetType().Namespace + ".strings.";

            return GetType()
                .Assembly
                .GetManifestResourceNames()
                .Where(i => i.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                .Select(i => new TranslationInfo
                {
                    Locale = Path.GetFileNameWithoutExtension(i.Substring(basePath.Length)),
                    EmbeddedResourcePath = i

                }).ToArray();
        }
    }
}