using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public static class GeneralExtensions
    {
        public static String ToUrlString(this String value)
        {
            return WebUtility.UrlEncode(value);
        }

        public static String ToDateString(this DateTime datetime)
        {
            datetime = datetime.ToLocalTime();
            var date = datetime.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            return date;
        }

        public static String ToTimeString(this DateTime datetime)
        {
            datetime = datetime.ToLocalTime();
            var time = ToUrlString(datetime.ToString("HH:mm", CultureInfo.InvariantCulture));
            return time;
        }

        public static String ToDelphiDate(this DateTime date)
        {
            date = date.ToLocalTime();
            var days = ((int)(date - (DateTime.ParseExact("30.12.1899", "dd.MM.yyyy", CultureInfo.InvariantCulture))).TotalDays).ToString();
            return days;
        }

        public static int ToDelphiTime(this DateTime time)
        {
            time = time.ToLocalTime();
            var minutes = time.Hour * 60 + time.Minute;
            return minutes;
        }

        public static String FloatDateTime(this DateTime value)
        {
            string totaldays = ToDelphiDate(value);
            double floattime = ToDelphiTime(value) / (24d * 60d);
            string totaltime = String.Format("{0:0.00000000}", floattime).Remove(0, 2);
            return String.Format("{0}.{1}", totaldays, totaltime);
        }

        public static DateTime GetProgramTime(this String value)
        {
            return DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).ToUniversalTime();
        }

        public static DateTime GetScheduleTime(this String date, String time)
        {
            var timerDate = DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.InvariantCulture).ToUniversalTime();
            var timerTime = time.Split(':');
            var totalSeconds = int.Parse(timerTime[0]) * 3600d + int.Parse(timerTime[1]) * 60d + int.Parse(timerTime[2]);
            var scheduleTime = timerDate.AddSeconds(totalSeconds);
            return scheduleTime;
        }

        public static DateTime GetSearchTime(this String time)
        {
            var searchTime = DateTime.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture).ToUniversalTime();
            return searchTime;
        }

        public static String SetChannelId(this String channelId)
        {
            return String.Format("TV-{0}", channelId);
        }

        public static String SetRecordingId(this String recordingId)
        {
            return String.Format("Recording-{0}", recordingId);
        }

        public static String SetEventId(String channel, String starttime, String endtime)
        {
            return String.Format("{0}|{1}|{2}", channel, starttime, endtime);
        }

        public static String SetSearchPhrase(this String searchname)
        {
            return String.Format("^{0}$", Regex.Replace(searchname, @"[^\w\.-@! ]", "?"));
        }

        public static String GetScheduleChannel(this String Id)
        {
            string[] channelId = Id.Split('|');
            return channelId[0];
        }

        public static String GetScheduleName(this String name)
        {
            if (name.EndsWith("[Cancelled]"))
                name = name.Remove(name.Length - 12, 12);
            if (name.EndsWith("[Conflict]"))
                name = name.Remove(name.Length - 11, 11);
            return name;
        }

        public static void GetSearchDays(int input, int power, ICollection<int> numbers)
        {
            if (input == 0)
                return;

            var digit = input % 2;
            if (digit == 1)
                numbers.Add((int)Math.Pow(2, power));

            GetSearchDays(input / 2, ++power, numbers);
        }

        public static String ChannelImageUrl(this String value)
        {
            var config = Plugin.Instance.Configuration;
            if (Plugin.Instance.Configuration.RequiresAuthentication)
                return String.Format("http://{0}:{1}@{2}:{3}/api/{4}", config.UserName, config.Password, config.ApiHostName, config.ApiPortNumber, value);
            else
                return String.Format("http://{0}:{1}/api/{2}", config.ApiHostName, config.ApiPortNumber, value);
        }

        public static String RecordingImageUrl(String baseurl, String value)
        {
            return String.Format("{0}{1}", baseurl, value);
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
