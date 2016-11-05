using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("recordings")]
    public class Recordings
    {
        [XmlElement("rev")]
        public string Rev { get; set; }

        [XmlElement("serverURL")]
        public string ServerURL { get; set; }

        [XmlElement("imageURL")]
        public string ImageURL { get; set; }

        [XmlElement("recording")]
        public List<Recording> Recording { get; set; }

        [XmlAttribute("Ver")]
        public string Ver { get; set; }
    }

    public class Recording
    {
        [XmlElement("channel")]
        public string Channel { get; set; }

        [XmlElement("file")]
        public string File { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("info")]
        public string SubTitle { get; set; }

        [XmlElement("desc")]
        public string Description { get; set; }

        [XmlElement("series")]
        public string Series { get; set; }

        [XmlElement("image")]
        public string Image { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("charset")]
        public string Charset { get; set; }

        [XmlAttribute("start")]
        public string Start { get; set; }

        [XmlAttribute("duration")]
        public string Duration { get; set; }
    }
}