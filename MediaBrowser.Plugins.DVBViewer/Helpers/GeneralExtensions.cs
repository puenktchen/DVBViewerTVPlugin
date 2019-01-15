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
        public static String ConvertUtf8String(String name)
        {
            Encoding dst = Encoding.GetEncoding("iso-8859-1");
            Encoding src = Encoding.GetEncoding("utf-8");
            byte[] srcBytes = src.GetBytes(name);
            byte[] dstBytes = Encoding.Convert(src, dst, srcBytes);
            byte[] output = Encoding.Convert(src, dst, dstBytes);
            return Encoding.GetEncoding("iso-8859-1").GetString(output); ;
        }

        public static String ToUrlString(this String value)
        {
            return WebUtility.UrlEncode(value);
        }

        public static String ToDateString(this DateTimeOffset datetime)
        {
            datetime = datetime.ToLocalTime();
            var date = datetime.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            return date;
        }

        public static String ToTimeString(this DateTimeOffset datetime)
        {
            datetime = datetime.ToLocalTime();
            var time = ToUrlString(datetime.ToString("HH:mm", CultureInfo.InvariantCulture));
            return time;
        }

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

        public static DateTimeOffset GetScheduleTime(this String date, String time)
        {
            var timerDate = DateTimeOffset.ParseExact(date, "dd.MM.yyyy", CultureInfo.InvariantCulture).ToUniversalTime();
            var timerTime = time.Split(':');
            var totalSeconds = int.Parse(timerTime[0]) * 3600d + int.Parse(timerTime[1]) * 60d + int.Parse(timerTime[2]);
            var scheduleTime = timerDate.AddSeconds(totalSeconds);
            return scheduleTime;
        }

        public static DateTimeOffset GetSearchTime(this String time)
        {
            var searchTime = DateTimeOffset.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture).ToUniversalTime();
            return searchTime;
        }

        public static String SetEventId(String channel, String starttime, String endtime)
        {
            return String.Format("{0}|{1}|{2}", channel, starttime, endtime);
        }

        public static String SetSearchPhrase(this String searchname)
        {
            return String.Format("^{0}$", Regex.Replace(searchname, @"[^\w\.-@! ]", "?")).ToUrlString();
        }

        public static String GetScheduleName(this String name)
        {
            if (name.EndsWith("[Cancelled]"))
                name = name.Remove(name.Length - 12, 12);
            if (name.EndsWith("[Conflict]"))
                name = name.Remove(name.Length - 11, 11);
            return name;
        }

        public static String ProgramImageUrl(this String channellogo)
        {
            var localImage = Path.Combine(Plugin.Instance.DataFolderPath, "channelthumbs", WebUtility.UrlDecode(channellogo).Split('\\').Last());
            return localImage;
        }

        public static String ProgramImagePosterUrl(this String channellogo)
        {
            var localImage = Path.Combine(Plugin.Instance.DataFolderPath, "channelthumbs", WebUtility.UrlDecode(channellogo).Split('\\').Last());
            return Path.Combine(Plugin.Instance.DataFolderPath, "channelthumbs", Path.GetFileNameWithoutExtension(localImage) + "-poster.png");
        }

        public static String ProgramImageLandscapeUrl(this String channellogo)
        {
            var localImage = Path.Combine(Plugin.Instance.DataFolderPath, "channelthumbs", WebUtility.UrlDecode(channellogo).Split('\\').Last());
            return Path.Combine(Plugin.Instance.DataFolderPath, "channelthumbs", Path.GetFileNameWithoutExtension(localImage) + "-landscape.png");
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
