using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

using MediaBrowser.Plugins.DVBViewer.Helpers;
using MediaBrowser.Plugins.DVBViewer.Services;

namespace MediaBrowser.Plugins.DVBViewer
{
    public class Plugin : BasePlugin, IHasWebPages, IHasThumbImage, IHasTranslations
    {
        public static Plugin Instance { get; private set; }
        public static IConfigurationManager ConfigurationManager { get; set; }
        public static IFfmpegManager FfmpegManager { get; set; }
        public static IImageProcessor ImageProcessor { get; set; }
        public static ILiveTvManager LiveTvManager { get; set; }
        public static ILogger Logger { get; set; }
        public static IXmlSerializer XmlSerializer { get; set; }
        public static ImageCreator ImageCreator { get; private set; }
        public static StreamingService StreamingService { get; private set; }
        public static TVService TVService { get; private set; }

        public Plugin
        (
            IConfigurationManager configurationManager,
            IFfmpegManager ffmpegManager,
            IImageProcessor imageProcessor,
            ILiveTvManager liveTvManager,
            ILogger logger,
            IXmlSerializer xmlSerializer
        )
            : base()
        {
            Instance = this;

            ConfigurationManager = configurationManager;
            FfmpegManager = ffmpegManager;
            ImageProcessor = imageProcessor;
            LiveTvManager = liveTvManager;
            Logger = logger;
            XmlSerializer = xmlSerializer;

            ImageCreator = new ImageCreator();
            StreamingService = new StreamingService();
            TVService = new TVService();
        }

        public static string StaticName = "DVBViewer";

        public override string Name
        {
            get { return StaticName; }
        }

        public override string Description
        {
            get
            {
                return "Live tv plugin to use DVBViewer Media Server as a tuner source for Emby";
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