using System;
using System.Collections.Generic;

using MediaBrowser.Model.Plugins;
using MediaBrowser.Plugins.DVBViewer.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            ApiHostName = "localhost";
            ApiPortNumber = 8089;
            TimerPrePadding = 5;
            TimerPostPadding = 10;
            CheckRecordingTitel = true;
            CheckRecordingInfo = true;
            CheckTimerName = true;
            EnableTimerCache = true;

            // Initialise this
            GenreMappings = new SerializableDictionary<string, List<string>>();
        }

        /// <summary>
        /// The url / ip address that DVBViewer Recording Service is hosted on
        /// </summary>
        public string ApiHostName { get; set; }

        /// <summary>
        /// The port number that DVBViewer Recording Service is hosted on
        /// </summary>
        public Int32 ApiPortNumber { get; set; }

        /// <summary>
        /// Indicates whether DVBViewer Recording Service requires authentication
        /// </summary>
        public bool RequiresAuthentication { get; set; }

        /// <summary>
        /// The user name for authenticating with DVBViewer Recording Service
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password for authenticating with DVBViewer Recording Service
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The default channel group to use in MB
        /// </summary>
        public String DefaultChannelGroup { get; set; }

        /// <summary>
        /// The genre mappings, to map localised MP genres, to MB genres.
        /// </summary>
        public SerializableDictionary<String, List<String>> GenreMappings { get; set; }

        /// <summary>
        /// Timer default pre padding in minutes
        /// </summary>
        public Int32? TimerPrePadding { get; set; }

        /// <summary>
        /// Timer default post padding in minutes
        /// </summary>
        public Int32? TimerPostPadding { get; set; }

        /// <summary>
        /// Checks the recording titel in AutoSearch to prevent recording repeats
        /// </summary>
        public bool CheckRecordingTitel { get; set; }

        /// <summary>
        /// Checks the recording info in AutoSearch to prevent recording repeats
        /// </summary>
        public bool CheckRecordingInfo { get; set; }

        /// <summary>
        /// Checks the timer name in AutoSearch to prevent recording repeats
        /// </summary>
        public bool CheckTimerName { get; set; }

        /// <summary>
        /// Enable direct access to recordings
        /// </summary>
        public bool EnableDirectAccess { get; set; }

        /// <summary>
        /// Enable Path Substitution
        /// </summary>
        public bool RequiresPathSubstitution { get; set; }

        /// <summary>
        /// The lokal recording folder of DVBViewer
        /// </summary>
        public string LocalFilePath { get; set; }

        /// <summary>
        /// The remote recording share of DVBViewer
        /// </summary>
        public string RemoteFilePath { get; set; }

        /// <summary>
        /// Enable one time schedules caching
        /// </summary>
        public bool EnableTimerCache { get; set; }

        /// <summary>
        /// Enable additional logging
        /// </summary>
        public bool EnableLogging { get; set; }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns></returns>
        public ValidationResult Validate()
        {
            if (String.IsNullOrEmpty(ApiHostName))
            {
                return new ValidationResult(false, "Please specify an API HostName (the box DVBViewer Recording Service is installed on)");
            }

            if (ApiPortNumber < 1)
            {
                return new ValidationResult(false, "Please specify an API Port Number (usually 4322)");
            }

            if (RequiresAuthentication)
            {
                if (String.IsNullOrEmpty(UserName))
                {
                    return new ValidationResult(false, "Please specify a UserName (check DVBViewer Recording Service - Authentication");
                }

                if (String.IsNullOrEmpty(Password))
                {
                    return new ValidationResult(false, "Please specify an Password (check DVBViewer Recording Service - Authentication");
                }
            }

            if (!TimerPrePadding.HasValue)
            {
                TimerPrePadding = 5;
            }

            if (!TimerPostPadding.HasValue)
            {
                TimerPostPadding = 10;
            }

            if (RequiresPathSubstitution)
            {
                if (String.IsNullOrEmpty(LocalFilePath))
                {
                    return new ValidationResult(false, "Please specify DVBViewers local recording folder");
                }

                if (String.IsNullOrEmpty(RemoteFilePath))
                {
                    return new ValidationResult(false, "Please specify DVBViewers remote recording share");
                }
            }

            return new ValidationResult(true, String.Empty);
        }
    }

    public class ValidationResult
    {
        public ValidationResult(Boolean isValid, String summary)
        {
            IsValid = isValid;
            Summary = summary;
        }

        public Boolean IsValid { get; set; }
        public String Summary { get; set; }
    }
}
