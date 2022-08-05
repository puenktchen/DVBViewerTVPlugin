using System;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public static class GeneralExtensions
    {
        public static string ToDelphiDate(this DateTimeOffset date)
        {
            var localDate = date.ToLocalTime();
            var totalDays = ((int)(localDate - (DateTimeOffset.ParseExact("30.12.1899", "dd.MM.yyyy", CultureInfo.InvariantCulture))).TotalDays).ToString();

            return totalDays;
        }

        public static string ToDelphiTime(this DateTimeOffset time)
        {
            var localTime = time.ToLocalTime();
            var totalTime = string.Format("{0:0.00000000}", (localTime.Hour * 60 + localTime.Minute) / (24d * 60d)).Remove(0, 2);

            return totalTime;
        }

        public static string FloatDateTimeOffset(this DateTimeOffset value)
        {
            var totalDays = ToDelphiDate(value);
            var totalTime = ToDelphiTime(value);
            var dateTime = string.Format("{0}.{1}", totalDays, totalTime);

            return dateTime;
        }

        public static DateTimeOffset GetProgramTime(this string value)
        {
            var dateTime = DateTimeOffset.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).ToUniversalTime();

            return dateTime;
        }

        public static bool HasVideoFlag (this int flag)
        {
            var binary = Convert.ToString(flag, 2);

            try
            {
                var videoflag = binary.ElementAt(binary.Length - 4).ToString();
                var encrypted = binary.ElementAt(binary.Length - 1).ToString();

                if (videoflag == "1")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }
    }
}
