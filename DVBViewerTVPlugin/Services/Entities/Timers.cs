using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("Timers")]
    public class Timers
    {
        [XmlElement("Timer")]
        public List<Timer> Timer { get; set; }
    }

    public class Timer
    {
        [XmlElement("ID")]
        public string Id { get; set; }

        [XmlElement("Descr")]
        public string Description { get; set; }

        [XmlAttribute("Enabled")]
        public string Enabled { get; set; }

        [XmlElement("Executeable")]
        public string Executeable { get; set; }

        [XmlElement("Recording")]
        public string Recording { get; set; }

        [XmlAttribute("Days")]
        public string Days { get; set; }

        [XmlAttribute("Date")]
        public string Date { get; set; }

        [XmlAttribute("Start")]
        public string Start { get; set; }

        [XmlAttribute("Dur")]
        public int Duration { get; set; }

        [XmlAttribute("End")]
        public string End { get; set; }

        [XmlAttribute("PreEPG")]
        public int PreEPG { get; set; }

        [XmlAttribute("PostEPG")]
        public int PostEPG { get; set; }

        [XmlElement("Series")]
        public string Series { get; set; }

        //[XmlElement("Source")]
        //public string Source { get; set; }

        //[XmlAttribute("Priority")]
        //public string Priority { get; set; }

        //[XmlAttribute("Action")]
        //public string Action { get; set; }

        //[XmlAttribute("Type")]
        //public string Type { get; set; }

        //[XmlAttribute("EPGEventID")]
        //public string EPGEventID { get; set; }

        //[XmlElement("GUID")]
        //public string GUID { get; set; }

        //[XmlElement("Format")]
        //public string Format { get; set; }

        //[XmlElement("Folder")]
        //public string Folder { get; set; }

        //[XmlElement("NameScheme")]
        //public string NameScheme { get; set; }

        //[XmlElement("Options")]
        //public Options Options { get; set; }

        [XmlElement("Channel")]
        public TimerChannel Channel { private get; set; }

        public string ChannelId
        {
            get
            {
                if (Channel != null)
                {
                    return Channel.Id.Split('|').GetValue(0).ToString();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                ChannelId = value;
            }
        }
    }

    public class TimerChannel
    {
        [XmlAttribute("ID")]
        public string Id { get; set; }
    }

    //public class Options
    //{
    //    [XmlAttribute("AdjustPAT")]
    //    public string AdjustPAT { get; set; }

    //    [XmlAttribute("AllAudio")]
    //    public string AllAudio { get; set; }

    //    [XmlAttribute("DVBSubs")]
    //    public string DVBSubs { get; set; }

    //    [XmlAttribute("Teletext")]
    //    public string Teletext { get; set; }

    //    [XmlAttribute("EITEPG")]
    //    public string EITEPG { get; set; }

    //    [XmlAttribute("Dump")]
    //    public string Dump { get; set; }
    //}
}