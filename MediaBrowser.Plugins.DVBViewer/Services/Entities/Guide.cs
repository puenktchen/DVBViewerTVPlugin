using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("epg")]
    public class Guide
    {
        [XmlElement("programme")]
        public List<Program> Program { get; set; }

        [XmlAttribute("Ver")]
        public string Version { get; set; }
    }

    public class Program
    {
        [XmlElement("titles", IsNullable = true)]
        public Titles Titles { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        public string Name
        {
            get
            {
                if (Titles != null)
                {
                    return Titles.Title;
                }
                return Title;
            }
        }

        private int productionYear;
        public int? ProductionYear
        {
            get
            {
                if (!String.IsNullOrEmpty(Name))
                {
                    if (Int32.TryParse((Regex.Match(Name, @"(?<=\()\d{4}(?=\)$)").Value), out productionYear))
                    {
                        return productionYear;
                    }
                }
                return null;
            }
        }

        [XmlElement("events", IsNullable = true)]
        public Events Events { get; set; }

        [XmlElement("event")]
        public string Event { get; set; }

        public string EpisodeTitle
        {
            get
            {
                if (Events != null)
                {
                    return Events.Event;
                }
                return Event;
            }
        }

        public string EpisodeTitleRegEx
        {
            get
            {
                if (Events != null)
                {
                    if (!String.IsNullOrEmpty(Events.Event))
                    {
                        return Regex.Replace(Events.Event, @"(^[(]?[s]?[0-9]*[e|x|\.][0-9]*[^\w]+)|(\s[(]?[s]?[0-9]+[e|x|\.][0-9]+[)]?$)", String.Empty, RegexOptions.IgnoreCase);
                    }
                }
                if (Event != null)
                {
                    if (!String.IsNullOrEmpty(Event))
                    {
                        return Regex.Replace(Event, @"(^[(]?[s]?[0-9]*[e|x|\.][0-9]*[^\w]+)|(\s[(]?[s]?[0-9]+[e|x|\.][0-9]+[)]?$)", String.Empty, RegexOptions.IgnoreCase);
                    }
                }
                return null;
            }
        }

        private int episodeNumber;
        public int? EpisodeNumber
        {
            get
            {
                if (Events != null)
                {
                    if (!String.IsNullOrEmpty(Events.Event))
                    {
                        if (Int32.TryParse(Regex.Match(Regex.Match(Events.Event, @"(?<=[s]?[0-9]+)[e|x|\.][0-9]+[)]?\s|(?<=[s]?[0-9]+)[e|x|\.][0-9]+[)]?$", RegexOptions.IgnoreCase).Value, @"\d+").Value, out episodeNumber))
                        {
                            return episodeNumber;
                        }
                    }
                    return null;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Event))
                    {
                        if (Int32.TryParse(Regex.Match(Regex.Match(Event, @"(?<=[s]?[0-9]+)[e|x|\.][0-9]+[)]?\s|(?<=[s]?[0-9]+)[e|x|\.][0-9]+[)]?$", RegexOptions.IgnoreCase).Value, @"\d+").Value, out episodeNumber))
                        {
                            return episodeNumber;
                        }
                    }
                    return null;
                }
            }
        }

        private int seasonNumber;
        public int? SeasonNumber
        {
            get
            {
                if (Events != null)
                {
                    if (!String.IsNullOrEmpty(Events.Event))
                    {
                        if (Int32.TryParse(Regex.Match(Regex.Match(Events.Event, @"[s]?[0-9]+(?=[e|x|\.][0-9]+[)]?\s)|[(]?[s]?[0-9]+(?=[e|x|\.][0-9]+[)]?$)", RegexOptions.IgnoreCase).Value, @"\d+").Value, out seasonNumber))
                        {
                            return seasonNumber;
                        }
                    }
                    return null;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Event))
                    {
                        if (Int32.TryParse(Regex.Match(Regex.Match(Event, @"[s]?[0-9]+(?=[e|x|\.][0-9]+[)]?\s)|[(]?[s]?[0-9]+(?=[e|x|\.][0-9]+[)]?$)", RegexOptions.IgnoreCase).Value, @"\d+").Value, out seasonNumber))
                        {
                            return seasonNumber;
                        }
                    }
                    return null;
                }
            }
        }

        [XmlElement("descriptions", IsNullable = true)]
        public Descriptions Descriptions { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        public string Overview
        {
            get
            {
                if (Descriptions != null)
                {
                    return Descriptions.Description;
                }
                return Description;
            }
        }

        [XmlElement("eventid")]
        public string EventId { get; set; }

        [XmlElement("content")]
        public string EitContent { get; set; }

        [XmlElement("charset")]
        public string Charset { get; set; }

        [XmlAttribute("start")]
        public string Start { get; set; }

        [XmlAttribute("stop")]
        public string Stop { get; set; }

        [XmlAttribute("channel")]
        public string ChannelEPGID { get; set; }
    }

    public class Titles
    {
        [XmlElement("title")]
        public string Title { get; set; }
    }

    public class Events
    {
        [XmlElement("event")]
        public string Event { get; set; }
    }

    public class Descriptions
    {
        [XmlElement("description")]
        public string Description { get; set; }
    }
}