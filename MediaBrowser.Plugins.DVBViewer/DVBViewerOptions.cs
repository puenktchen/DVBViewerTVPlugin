namespace MediaBrowser.Plugins.DVBViewer
{
    public class DVBViewerOptions
    {
        public int StreamingPort { get; set; } = 7522;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string RootChannelGroup { get; set; }
        public bool ImportRadioChannels { get; set; }
        public bool ImportFavoritesOnly { get; set; } = true;
        public bool RemapProgramEvents { get; set; }
    }
}
