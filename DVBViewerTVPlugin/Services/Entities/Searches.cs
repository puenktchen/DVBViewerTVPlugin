using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("Searches")]
    public class Searches
    {
        [XmlElement("Search")]
        public List<Search> Search { get; set; }
    }

    public class Search
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Autorecording")]
        public string Autorecording { get; set; }

        [XmlAttribute("CheckRecTitle")]
        public string CheckRecTitle { get; set; }

        [XmlAttribute("CheckRecSubTitle")]
        public string CheckRecSubTitle { get; set; }

        [XmlAttribute("CheckTimer")]
        public string CheckTimer { get; set; }

        [XmlElement("Series")]
        public string Series { get; set; }

        [XmlElement("searchphrase")]
        public string Searchphrase { get; set; }

        [XmlElement("Starttime")]
        public string Starttime { get; set; }

        [XmlElement("EndTime")]
        public string EndTime { get; set; }

        [XmlElement("EPGBefore")]
        public int EPGBefore { get; set; }

        [XmlElement("EPGAfter")]
        public int EPGAfter { get; set; }

        [XmlElement("Days")]
        public int Days { get; set; }

        [XmlElement("channels")]
        public SearchChannels Channels { get; set; }

        public string GetChannel()
        {
            if (Channels != null)
            {
                return Channels.Channel[0];
            }
            else
            {
                return null;
            }
        }
    }

    public class SearchChannels
    {
        [XmlElement("channel")]
        public List<string> Channel { get; set; }
    }
}