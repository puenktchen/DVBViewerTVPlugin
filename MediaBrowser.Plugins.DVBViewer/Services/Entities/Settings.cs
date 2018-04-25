using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("settings")]
    public class Settings
    {
        private string _apiPort { get; set; }
        private string _httpPort { get; set; }
        private string _rtspPort { get; set; }
        private string _streamPort { get; set; }
        private string _mediaPort { get; set; }
        private string _recAllAudio { get; set; }
        private string _recDVBSub { get; set; }
        private string _recTeletext { get; set; }
        private string _recEITEpg { get; set; }
        private string _recFormat { get; set; }
        private string _recNameScheme { get; set; }
        private string _patPMTAdjust { get; set; }

        [XmlElement(ElementName = "section")]
        public List<Section> Section { get; set; }

        public string Version() { return GetEntry(Section, "Version"); }
        public string ApiPort()
        {
            try { _apiPort = GetEntry(Section, "Port"); }
            catch { return "8089"; }
            return _apiPort;
        }
        public string HttpPort()
        {
            try { _httpPort = GetEntry(Section, "httpPort"); }
            catch { return "8889"; }
            return _httpPort;
        }
        public string RtspPort()
        {
            try { _rtspPort = GetEntry(Section, "RTSPPort"); }
            catch { return "554"; }
            return _rtspPort;
        }
        public string StreamPort()
        {
            try { _streamPort = GetEntry(Section, "StreamPort"); }
            catch { return "7522"; }
            return _streamPort;
        }
        public string MediaPort()
        {
            try { _mediaPort = GetEntry(Section, "MediaPort"); }
            catch { return "8090"; }
            return _mediaPort;
        }
        public string RecAllAudio()
        {
            try { _recAllAudio = GetEntry(Section, "RecAllAudio"); }
            catch { return "1"; }
            return _recAllAudio;
        }
        public string RecDVBSub()
        {
            try { _recDVBSub = GetEntry(Section, "RecDVBSub"); }
            catch { return "0"; }
            return _recDVBSub;
        }
        public string RecTeletext()
        {
            try { _recTeletext = GetEntry(Section, "RecTeletext"); }
            catch { return "0"; }
            return _recTeletext;
        }
        public string RecEITEpg()
        {
            try { _recEITEpg = GetEntry(Section, "RecEITEPG"); }
            catch { return "0"; }
            return _recEITEpg;
        }
        public string RecFormat()
        {
            try { _recFormat = GetEntry(Section, "RecFormat"); }
            catch { return "2"; }
            return _recFormat;
        }
        public string RecNameScheme()
        {
            try { _recNameScheme = GetEntry(Section, "NameScheme"); }
            catch { return "%event - %title"; }
            return _recNameScheme;
        }
        public string PATPMTAdjust()
        {
            try { _patPMTAdjust = GetEntry(Section, "PATPMTAdjust"); }
            catch { return "1"; }
            return _patPMTAdjust;
        }

        public string GetEntry(List<Section> section, string key)
        {
            var val = section.SelectMany(s => s.Entry).FirstOrDefault(item => item.Name == key);
            if (val.Text != null)
                return val.Text ?? string.Empty;
            return string.Empty;
        }
    }

    public class Section
    {
        [XmlElement("entry")]
        public List<Entry> Entry { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class Entry
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
}