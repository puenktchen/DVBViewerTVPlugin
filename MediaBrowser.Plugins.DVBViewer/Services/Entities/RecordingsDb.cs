using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("table")]
    public class RecordingsDb
    {
        [XmlElement("row")]
        public List<RecordingEntry> RecordingEntry { get; set; }
    }

    public class RecordingEntry
    {
        [XmlElement("IDRECORD")]
        public string Id { get; set; }

        [XmlElement("TITLE")]
        public string Title { get; set; }

        [XmlElement("INFO")]
        public string Info { get; set; }

        public string EpisodeTitle
        {
            get
            {
                if (!String.IsNullOrEmpty(Info))
                {
                    return Regex.Replace(Info, @"(^[(]?[s]?[0-9]*[e|x|\.][0-9]*[^\w]+)|(\s[(]?[s]?[0-9]+[e|x|\.][0-9]+[)]?$)", String.Empty, RegexOptions.IgnoreCase);
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

        [XmlElement("DESCRIPTION")]
        public string Overview { get; set; }

        [XmlElement("SERIES")]
        public string Series { get; set; }

        [XmlElement("CONTENT")]
        public string EitContent { get; set; }

        [XmlElement("FILENAME")]
        public string File { get; set; }

        [XmlElement("start")]
        public string Start { get; set; }

        [XmlElement("DURATION")]
        public string Duration { get; set; }

        [XmlElement("CHANNEL")]
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

        [XmlElement("ENABLED")]
        public int Enabled { get; set; }

        [XmlElement("FOUND")]
        public int Found { get; set; }
    }
}