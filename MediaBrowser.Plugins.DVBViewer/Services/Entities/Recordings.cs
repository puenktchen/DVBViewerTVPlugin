using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        [XmlElement("title"), DefaultValue("")]
        public string Name { get; set; }

        [XmlElement("info"), DefaultValue("")]
        public string EpisodeTitle { get; set; }


        int episodeNumber;
        public int? EpisodeNumber
        {
            get
            {
                if (!String.IsNullOrEmpty(EpisodeTitle))
                {
                    if (Int32.TryParse(Regex.Match(Regex.Match(EpisodeTitle, @"(?<=[s]?[0-9]+)[e|x|\.][0-9]+\s", RegexOptions.IgnoreCase).Value, @"\d+").Value, out episodeNumber))
                    {
                        return episodeNumber;
                    }
                }
                return null;
            }
            set
            {
                EpisodeNumber = value;
            }
        }

        int seasonNumber;
        public int? SeasonNumber
        {
            get
            {
                if (!String.IsNullOrEmpty(EpisodeTitle))
                {
                    if (Int32.TryParse(Regex.Match(Regex.Match(EpisodeTitle, @"[s]?[0-9]+(?=[e|x|\.][0-9]+\s)", RegexOptions.IgnoreCase).Value, @"\d+").Value, out seasonNumber))
                    {
                        return seasonNumber;
                    }
                }
                return null;
            }
            set
            {
                SeasonNumber = value;
            }
        }


        [XmlElement("desc"), DefaultValue("")]
        public string Overview { get; set; }

        [XmlElement("series"), DefaultValue("")]
        public string Series { get; set; }

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
                    return Plugin.TvProxy.GetChannelList(new CancellationToken()).Root.ChannelGroup.SelectMany(c => c.Channel)
                        .Where(x => x.Name.Equals(ChannelName, StringComparison.OrdinalIgnoreCase))
                        .First().Id;
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
}