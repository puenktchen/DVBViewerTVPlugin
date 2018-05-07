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
            StreamPortNumber = 7522;
            MediaPortNumber = 8090;
            ChannelFavourites = true;
            ProgramImages = false;
            EnableProbing = true;
            TimerPrePadding = 5;
            TimerPostPadding = 10;
            EnableRecordingImport = true;
            CheckRecordingTitle = true;
            CheckRecordingSubTitle = true;
            CheckRemovedRecording = true;
            CheckTimerName = true;
            EnableTimerCache = true;

            // Initialise this
            GenreMappings = new SerializableDictionary<string, List<string>>();
        }

        /// <summary>
        /// The url / ip address that DVBViewer Media Server is hosted on
        /// </summary>
        public string ApiHostName { get; set; }

        /// <summary>
        /// The port number that DVBViewer Media Server is hosted on
        /// </summary>
        public Int32 ApiPortNumber { get; set; }

        /// <summary>
        /// The port number that DVBViewer Media Server is hosted on
        /// </summary>
        public Int32 StreamPortNumber { get; set; }

        /// <summary>
        /// The port number that DVBViewer Media Server is hosted on
        /// </summary>
        public Int32 MediaPortNumber { get; set; }

        /// <summary>
        /// Indicates whether DVBViewer Media Server requires authentication
        /// </summary>
        public bool RequiresAuthentication { get; set; }

        /// <summary>
        /// The user name for authenticating with DVBViewer Media Server
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password for authenticating with DVBViewer Media Server
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Use channel favourites instead of channel scan groups
        /// </summary>
        public bool ChannelFavourites { get; set; }

        /// <summary>
        /// The default channel group to use in Emby
        /// </summary>
        public String DefaultChannelGroup { get; set; }

        /// <summary>
        /// Use DVBViewer genre (EIT Content Categories)
        /// </summary>
        public bool EitContent { get; set; }

        /// <summary>
        /// The genre mappings, to map localised DVBViewer genres, to Emby categories.
        /// </summary>
        public SerializableDictionary<String, List<String>> GenreMappings { get; set; }

        /// <summary>
        /// Enable program images
        /// </summary>
        public bool ProgramImages { get; set; }

        /// <summary>
        /// Enables streaming probing for live tv
        /// </summary>
        public bool EnableProbing { get; set; }

        /// <summary>
        /// Timer default pre padding in minutes
        /// </summary>
        public Int32? TimerPrePadding { get; set; }

        /// <summary>
        /// Timer default post padding in minutes
        /// </summary>
        public Int32? TimerPostPadding { get; set; }

        /// <summary>
        /// The default task executed after recording ends
        /// </summary>
        public String TimerTask { get; set; }

        /// <summary>
        /// Checks the recording titel in AutoSearch to prevent recording repeats
        /// </summary>
        public bool CheckRecordingTitle { get; set; }

        /// <summary>
        /// Checks the recording subtitel in AutoSearch to prevent recording repeats
        /// </summary>
        public bool CheckRecordingSubTitle { get; set; }

        /// <summary>
        /// Checks the recording subtitel in AutoSearch to prevent recording repeats
        /// </summary>
        public bool CheckRemovedRecording { get; set; }

        /// <summary>
        /// Checks the timer name in AutoSearch to prevent recording repeats
        /// </summary>
        public bool CheckTimerName { get; set; }

        /// <summary>
        /// Skips timers if item is already in Emby library
        /// </summary>
        public bool SkipAlreadyInLibrary { get; set; }

        /// <summary>
        /// Skips timers method for items already in Emby library
        /// </summary>
        public String SkipAlreadyInLibraryProfile { get; set; }

        /// <summary>
        /// Autocreates timers based on missing episodes in Emby library
        /// </summary>
        public bool AutoCreateTimers { get; set; }

        /// <summary>
        /// Enable import of MediaPortal recordings
        /// </summary>
        public bool EnableRecordingImport { get; set; }

        /// <summary>
        /// Enable TMDB online lookup for recording posters
        /// </summary>
        public bool EnableTmdbLookup { get; set; }

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
        /// Enable custom image processing
        /// </summary>
        public bool EnableImageProcessing { get; set; }

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
                return new ValidationResult(false, "Please specify an API HostName (the box DVBViewer Media Server is installed on)");
            }

            if (ApiPortNumber < 1)
            {
                return new ValidationResult(false, "Please specify an API Port Number (usually 8089)");
            }

            if (StreamPortNumber < 1)
            {
                return new ValidationResult(false, "Please specify an Live Streamserver Port Number (usually 7522)");
            }

            if (MediaPortNumber < 1)
            {
                return new ValidationResult(false, "Please specify an Media Streamserver Port Number (usually 8090)");
            }

            if (RequiresAuthentication)
            {
                if (String.IsNullOrEmpty(UserName))
                {
                    return new ValidationResult(false, "Please specify a UserName (check DVBViewer Media Server - Authentication");
                }

                if (String.IsNullOrEmpty(Password))
                {
                    return new ValidationResult(false, "Please specify an Password (check DVBViewer Media Server - Authentication");
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
