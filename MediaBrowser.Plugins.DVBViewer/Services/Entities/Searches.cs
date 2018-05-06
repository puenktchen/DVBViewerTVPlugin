using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    [XmlRoot("Searches")]
    public class Searches
    {
        [XmlElement("Search")]
        public List<Search> Search { get; set; }
    }

    public class Search
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("AutoRecording")]
        public string AutoRecording { get; set; }

        [XmlAttribute("CheckRecTitle")]
        public string CheckRecTitle { get; set; }

        [XmlAttribute("CheckRecSubTitle")]
        public string CheckRecSubTitle { get; set; }

        [XmlAttribute("CheckTimer")]
        public string CheckTimer { get; set; }

        [XmlElement("Priority")]
        public int Priority { get; set; }

        [XmlElement("Series")]
        public string Series { get; set; }

        [XmlElement("SearchPhrase")]
        public string SearchPhrase { get; set; }

        [XmlElement("StartTime")]
        public string StartTime { get; set; }

        [XmlElement("EndTime")]
        public string EndTime { get; set; }

        [XmlElement("EPGBefore")]
        public int EPGBefore { get; set; }

        [XmlElement("EPGAfter")]
        public int EPGAfter { get; set; }

        [XmlElement("Days")]
        public int Days { get; set; }

        [XmlElement("Channels")]
        public SearchChannels Channels { get; set; }

        public string ChannelId
        {
            get
            {
                if (Channels != null)
                {
                    try
                    {
                        return Plugin.TvProxy.GetChannelList(new CancellationToken()).Root.ChannelGroup.SelectMany(c => c.Channel)
                        .Where(x => x.EPGID.Equals(Channels.Channel[0]))
                        .First().Id;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                return null;
            }
            set
            {
                ChannelId = value;
            }
        }
    }

    public class SearchChannels
    {
        [XmlElement("Channel")]
        public List<string> Channel { get; set; }
    }
}