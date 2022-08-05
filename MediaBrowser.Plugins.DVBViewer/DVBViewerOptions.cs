using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Plugins.DVBViewer
{
    public class DVBViewerOptions
    {
        /// <summary>
        /// The port number that DVBViewer Media Server is hosted on
        /// </summary>
        public Int32 StreamingPort { get; set; } = 7522;

        /// <summary>
        /// The user name for authenticating with DVBViewer Media Server
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password for authenticating with DVBViewer Media Server
        /// </summary>
        public string Password { get; set; }
    }
}
