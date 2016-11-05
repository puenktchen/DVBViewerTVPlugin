using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("settings")]
    public class Settings
    {
        [XmlElement(ElementName = "section")]
        public List<Section> Section { get; set; }

        public string Version() { return GetEntry(Section, "Version"); }
        public string ApiPort() { return GetEntry(Section, "Port"); }
        public string HttpPort() { return GetEntry(Section, "httpPort"); }
        public string RtspPort() { return GetEntry(Section, "RTSPPort"); }
        public string StreamPort() { return GetEntry(Section, "StreamPort"); }
        public string MediaPort() { return GetEntry(Section, "MediaPort"); }
        public string RecAllAudio() { return GetEntry(Section, "RecAllAudio"); }
        public string RecDVBSub() { return GetEntry(Section, "RecDVBSub"); }
        public string RecTeletext() { return GetEntry(Section, "RecTeletext"); }
        public string RecEITEpg() { return GetEntry(Section, "RecEITEPG"); }
        public string RecFormat() { return GetEntry(Section, "RecFormat"); }
        public string RecNameScheme() { return GetEntry(Section, "NameScheme"); }
        public string PATPMTAdjust() { return GetEntry(Section, "PATPMTAdjust"); }

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