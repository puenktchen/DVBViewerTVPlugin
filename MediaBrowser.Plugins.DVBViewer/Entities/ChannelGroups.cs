using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Entities
{
    [XmlRoot("channels")]
    public class ChannelGroups
    {
        [XmlElement("root")]
        public List<string> RootChannelGroups { get; set; }
    }
}