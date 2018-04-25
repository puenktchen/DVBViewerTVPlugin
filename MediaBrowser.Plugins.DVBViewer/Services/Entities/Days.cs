using System;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
        [Flags]
        public enum TimerDays
        {
            Monday = 1,
            Tuesday = 2,
            Wednesday = 4,
            Thursday = 8,
            Friday = 16,
            Saturday = 32,
            Sunday = 64,
        }
}