using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("recordings")]
    public class Recordings
    {
        [XmlElement("serverURL")]
        public string ServerURL { get; set; }

        [XmlElement("imageURL")]
        public string ImageURL { get; set; }

        [XmlElement("recording")]
        public List<Recording> Recording { get; set; }
    }

    public class Recording
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        public string Name
        {
            get
            {
                if (!String.IsNullOrEmpty(Title))
                {
                    return Regex.Replace(Title, @"\s\W[a-zA-Z]?[0-9]{1,3}?\W$", String.Empty);
                }
                return null;
            }
        }

        public string MovieName
        {
            get
            {
                if (!String.IsNullOrEmpty(Title))
                {
                    return Regex.Replace(Title, @"(?<=\S)\s\W\d{4}\W(?=$)", String.Empty);
                }
                return null;
            }
        }

        private int year;
        public int? Year
        {
            get
            {
                if (!String.IsNullOrEmpty(Title))
                {
                    if (Int32.TryParse((Regex.Match(Title, @"(?<=\()\d{4}(?=\)$)").Value), out year))
                    {
                        return year;
                    }
                }
                return null;
            }
        }

        [XmlElement("info")]
        public string Info { get; set; }

        public string EpisodeTitle
        {
            get
            {
                if (!String.IsNullOrEmpty(Info))
                {
                    return Regex.Replace(Info, @"(^[(]?[s]?[0-9]*[e|x|\.][0-9]*[^\w]+)|(\s[(]?[s]?[0-9]*[e|x|\.][0-9]*[)]?$)", String.Empty, RegexOptions.IgnoreCase);
                }
                return null;
            }
        }

        private int episodeNumber;
        public int? EpisodeNumber
        {
            get
            {
                if (!String.IsNullOrEmpty(Info))
                {
                    if (Int32.TryParse(Regex.Match(Regex.Match(Info, @"(?<=[s]?[0-9]+)[e|x|\.][0-9]+[)]?\s|(?<=[s]?[0-9]+)[e|x|\.][0-9]+[)]?$", RegexOptions.IgnoreCase).Value, @"\d+").Value, out episodeNumber))
                    {
                        return episodeNumber;
                    }
                }
                return null;
            }
        }

        private int seasonNumber;
        public int? SeasonNumber
        {
            get
            {
                if (!String.IsNullOrEmpty(Info))
                {
                    if (Int32.TryParse(Regex.Match(Regex.Match(Info, @"[s]?[0-9]+(?=[e|x|\.][0-9]+[)]?\s)|[(]?[s]?[0-9]+(?=[e|x|\.][0-9]+[)]?$)", RegexOptions.IgnoreCase).Value, @"\d+").Value, out seasonNumber))
                    {
                        return seasonNumber;
                    }
                }
                return null;
            }
        }

        [XmlElement("desc")]
        public string Overview { get; set; }

        [XmlElement("series")]
        public string Series { get; set; }

        [XmlAttribute("content")]
        public string EitContent { get; set; }

        [XmlElement("image")]
        public string Image { get; set; }

        [XmlElement("file")]
        public string File { get; set; }

        [XmlAttribute("start")]
        public string Start { get; set; }

        [XmlAttribute("duration")]
        public string Duration { get; set; }

        [XmlElement("channel")]
        public string ChannelName { get; set; }

        public string ChannelId
        {
            get
            {
                if (ChannelName != null)
                {
                    try
                    {
                        return Plugin.TvProxy.GetChannelList(new CancellationToken(), "DefaultChannelGroup").Root.ChannelGroup.SelectMany(c => c.Channel)
                        .Where(x => x.Name.Equals(ChannelName, StringComparison.OrdinalIgnoreCase))
                        .First().Id;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                return null;
            }
        }
    }
}