using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public static class GeneralExtensions
    {
        public static String ToDelphiDate(this DateTimeOffset date)
        {
            date = date.ToLocalTime();
            var totaldays = ((int)(date - (DateTimeOffset.ParseExact("30.12.1899", "dd.MM.yyyy", CultureInfo.InvariantCulture))).TotalDays).ToString();
            return totaldays;
        }

        public static String ToDelphiTime(this DateTimeOffset time)
        {
            time = time.ToLocalTime();
            var totaltime = String.Format("{0:0.00000000}", (time.Hour * 60 + time.Minute) / (24d * 60d)).Remove(0, 2);
            return totaltime;
        }

        public static String FloatDateTimeOffset(this DateTimeOffset value)
        {
            string totaldays = ToDelphiDate(value);
            string totaltime = ToDelphiTime(value);
            return String.Format("{0}.{1}", totaldays, totaltime);
        }

        public static DateTimeOffset GetProgramTime(this String value)
        {
            return DateTimeOffset.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).ToUniversalTime();
        }

        public static String SetEventId(String channel, String starttime, String endtime)
        {
            return String.Format("{0}|{1}|{2}", channel, starttime, endtime);
        }

        public static bool HasVideoFlag (this int flag)
        {
            var binary = Convert.ToString(flag, 2);
            try
            {
                var videoflag = binary.ElementAt(binary.Length - 4).ToString();
                var encrypted = binary.ElementAt(binary.Length - 1).ToString();
                //Plugin.Logger.Info("DVBViewer channel flag: {0}, binary: {1}, videoflag: {2}, encrypted: {3}", flag.ToString(), binary, videoflag, encrypted);
                if (videoflag == "1")
                    return true;
                else
                    return false;
            }
            catch
            {
                Plugin.Logger.Error("DVBViewer channel flag: {0}, binary: {1}, videoflag: unknown, encrypted: unknown", flag.ToString(), binary);
                return true;
                throw;
            }
        }
    }
}
