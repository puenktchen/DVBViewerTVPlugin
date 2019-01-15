using System;
using System.Collections.Generic;
using System.IO;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Configuration;
using MediaBrowser.Plugins.DVBViewer.Helpers;
using MediaBrowser.Plugins.DVBViewer.Interfaces;
using MediaBrowser.Plugins.DVBViewer.Services.Proxies;

namespace MediaBrowser.Plugins.DVBViewer
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public static TVServiceProxy TvProxy { get; private set; }
        public static StreamingServiceProxy StreamingProxy { get; private set; }
        public static IPluginLogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="logger">The logger.</param>
        public Plugin(
            IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IHttpClient httpClient, 
            IJsonSerializer jsonSerializer, INetworkManager networkManager, ILogger logger, TmdbLookup tmdbLookup)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            Logger = new PluginLogger(logger);

            // Create our shared service proxies
            StreamingProxy = new StreamingServiceProxy(httpClient, jsonSerializer, xmlSerializer, networkManager);
            TvProxy = new TVServiceProxy(httpClient, jsonSerializer, xmlSerializer, StreamingProxy, tmdbLookup);
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "DVBViewer TV Plugin"; }
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
            return type.Assembly.GetManifestResourceStream("MediaBrowser.Plugins.DVBViewer.Images.Plugin-thumb.png");
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

        /// <summary>
        /// Holds our registration information
        /// </summary>
        public MBRegistrationRecord Registration { get; set; }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var oldConfig = Configuration;

            base.UpdateConfiguration(configuration);

            ServerEntryPoint.Instance.OnConfigurationUpdated(oldConfig, (PluginConfiguration)configuration);
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "DVBViewer",
                    EmbeddedResourcePath = "MediaBrowser.Plugins.DVBViewer.Configuration.configPage.html"
                }
            };
        }
    }
}