using System;
using System.Collections.Generic;
using System.Threading;

using MediaBrowser.Model.Services;
using MediaBrowser.Plugins.DVBViewer.Services.Exceptions;

namespace MediaBrowser.Plugins.DVBViewer
{
    [Route("/DVBViewerPlugin/ChannelGroups", "GET", Summary = "Gets a list of channel groups")]
    public class GetChannelGroups : IReturn<List<String>>
    {
    }

    [Route("/DVBViewerPlugin/SkipAlreadyInLibraryProfiles", "GET", Summary = "Gets a list of profiles to skip timers for Emy library items")]
    public class GetSkipAlreadyInLibraryProfiles : IReturn<List<String>>
    {
    }

    [Route("/DVBViewerPlugin/TestConnection", "GET", Summary = "Tests the connection to DVBViewer Media Server")]
    public class GetConnection : IReturn<Boolean>
    {
    }

    public class ServerApiEndpoints : IService
    {
        public object Get(GetChannelGroups request)
        {
            var channelGroups = new List<string>();
            try
            {
                channelGroups = Plugin.TvProxy.GetChannelGroups(new CancellationToken()).RootGroups;
                foreach (var name in channelGroups)
                {
                    Plugin.Logger.Info("Kanalgruppenname: {0}", name);
                }
                              
            }
            catch (ServiceAuthenticationException)
            {
                // Do nothing, allow an empty list to be passed out
            }
            catch (Exception exception)
            {
                Plugin.Logger.ErrorException("There was an issue retrieving the channel groups", exception);
            }

            return channelGroups;
        }

        public object Get(GetSkipAlreadyInLibraryProfiles request)
        {
            return new List<string>(new string[] { "Season and Episode Numbers", "Episode Name" });
        }

        public object Get(GetConnection request)
        {
            try
            {
                var result = Plugin.TvProxy.GetStatusInfo(new CancellationToken()).Version();
                if (!String.IsNullOrEmpty(result))
                {
                    return true;
                }
            }
            catch
            {
                throw;
            }

            return false;
        }
    }
}
