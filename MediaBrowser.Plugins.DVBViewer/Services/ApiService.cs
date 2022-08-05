using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MediaBrowser.Model.Services;

namespace MediaBrowser.Plugins.DVBViewer.Services
{
    [Route("/DVBViewer/RootChannelGroups", "GET", Summary = "Gets a list of channel groups", IsHidden = true)]
    public class GetRootChannelGroups : IReturn<List<string>>
    {
    }

    public class DVBViewerServices : IService
    {
        public object Get(GetRootChannelGroups request)
        {
            var rootChannelGroups = new List<string>();

            try
            {
                var tuner = Plugin.LiveTvManager.GetTunerHostInfos("dvbviewer").FirstOrDefault();

                if (tuner == null)
                {
                    rootChannelGroups = Plugin.TVService.GetRootChannelGroups(new CancellationToken(), new DVBViewerOptions(), @"http://localhost:8089").Result.RootChannelGroups;
                }
                else
                {
                    var config = DVBViewerTvService.Instance.GetConfiguration(tuner);

                    rootChannelGroups = Plugin.TVService.GetRootChannelGroups(new CancellationToken(), config, tuner.Url).Result.RootChannelGroups;
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.ErrorException("There was an issue retrieving the channel groups", exception);
            }

            return rootChannelGroups;
        }
    }
}