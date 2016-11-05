using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("epg")]
    public class Guide
    {
        [XmlElement("programme")]
        public List<Programs> Programs { get; set; }

        [XmlAttribute("Ver")]
        public string Version { get; set; }
    }

    public class Programs
    {
        [XmlElement("titles", IsNullable = true)]
        public Titles Titles { get; set; }

        [XmlElement("title"), DefaultValue("")]
        public string Title { get; set; }

        public string GetTitle()
        {
            if (Titles != null)
            {
                return Titles.Title;
            }
            else
            {
                return Title;
            }
        }


        [XmlElement("events", IsNullable = true)]
        public SubTitles SubTitles { get; set; }

        [XmlElement("event"), DefaultValue("")]
        public string SubTitle { get; set; }

        public string GetSubTitle()
        {
            if (SubTitles != null)
            {
                return SubTitles.SubTitle;
            }
            else
            {
                return SubTitle;
            }
        }


        [XmlElement("descriptions", IsNullable = true)]
        public Descriptions Descriptions { get; set; }

        [XmlElement("description"), DefaultValue("")]
        public string Description { get; set; }

        public string GetDescription()
        {
            if (Descriptions != null)
            {
                return Descriptions.Description;
            }
            else
            {
                return Description;
            }
        }


        [XmlElement("eventid"), DefaultValue("")]
        public string EventID { get; set; }

        [XmlElement("content"), DefaultValue("")]
        public string Content { get; set; }

        [XmlElement("charset"), DefaultValue("")]
        public string Charset { get; set; }

        [XmlAttribute("start")]
        public string Start { get; set; }

        [XmlAttribute("stop")]
        public string Stop { get; set; }

        [XmlAttribute("channel")]
        public string Channel { get; set; }
    }

    public class Titles
    {
        [XmlElement("title")]
        public string Title { get; set; }
    }

    public class SubTitles
    {
        [XmlElement("event"), DefaultValue("")]
        public string SubTitle { get; set; }
    }

    public class Descriptions
    {
        [XmlElement("description"), DefaultValue("")]
        public string Description { get; set; }
    }
}