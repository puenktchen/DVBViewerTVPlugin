using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("channels")]
    public class ChannelGroups
    {
        [XmlElement("root")]
        public List<string> RootGroups { get; set; }
    }
}