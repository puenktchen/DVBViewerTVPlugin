using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

using MediaBrowser.Plugins.DVBViewer.Helpers;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("channels")]
    public class Channels
    {
        [XmlElement("rtspURL"), DefaultValue("")]
        public string RtspURL { get; set; }

        [XmlElement("upnpURL"), DefaultValue("")]
        public string UpnpURL { get; set; }

        [XmlElement("root")]
        public Root Root { get; set; }
    }

    public class Root
    {
        [XmlElement("group")]
        public List<ChannelGroup> ChannelGroup { get; set; }

        [XmlAttribute("name")]
        public string RootName { get; set; }
    }

    public class ChannelGroup
    {
        [XmlElement("channel")]
        public List<Channel> Channel { get; set; }

        [XmlAttribute("name")]
        public string ChannelGroupName { get; set; }
    }

    public class Channel
    {
        [XmlElement("rtsp"), DefaultValue("")]
        public string Rtsp { get; set; }

        [XmlAttribute("nr")]
        public string Nr { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("EPGID")]
        public string EPGID { get; set; }

        [XmlAttribute("flags")]
        public int Flags { get; set; }

        [XmlAttribute("ID")]
        public string Id { get; set; }

        [XmlElement("logo"), DefaultValue("")]
        public string Logo { get; set; }
    }
}