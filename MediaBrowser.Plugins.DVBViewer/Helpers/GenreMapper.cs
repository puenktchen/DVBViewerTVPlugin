using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Plugins.DVBViewer.Configuration;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    /// <summary>
    /// Provides methods to map configure genres to MB programs
    /// </summary>
    public class GenreMapper
    {
        public const string GENRE_MOVIE = "GENREMOVIE";
        public const string GENRE_SERIES = "GENRESERIES";
        public const string GENRE_SPORT = "GENRESPORT";
        public const string GENRE_NEWS = "GENRENEWS";
        public const string GENRE_KIDS = "GENREKIDS";
        public const string GENRE_LIVE = "GENRELIVE";

        private readonly PluginConfiguration _configuration;
        private readonly List<String> _movieGenres;
        private readonly List<String> _seriesGenres;
        private readonly List<String> _sportGenres;
        private readonly List<String> _newsGenres;
        private readonly List<String> _kidsGenres;
        private readonly List<String> _liveGenres;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenreMapper"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public GenreMapper(PluginConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");

            _movieGenres = new List<string>();
            _seriesGenres = new List<string>();
            _sportGenres = new List<string>();
            _newsGenres = new List<string>();
            _kidsGenres = new List<string>();
            _liveGenres = new List<string>();

            LoadInternalLists(_configuration.GenreMappings);
        }

        private void LoadInternalLists(Dictionary<string, List<string>> genreMappings)
        {
            if (genreMappings != null)
            {
                if (_configuration.GenreMappings.ContainsKey(GENRE_MOVIE) && _configuration.GenreMappings[GENRE_MOVIE] != null)
                {
                    _movieGenres.AddRange(_configuration.GenreMappings[GENRE_MOVIE]);
                }

                if (_configuration.GenreMappings.ContainsKey(GENRE_SERIES) && _configuration.GenreMappings[GENRE_SERIES] != null)
                {
                    _seriesGenres.AddRange(_configuration.GenreMappings[GENRE_SERIES]);
                }

                if (_configuration.GenreMappings.ContainsKey(GENRE_SPORT) && _configuration.GenreMappings[GENRE_SPORT] != null)
                {
                    _sportGenres.AddRange(_configuration.GenreMappings[GENRE_SPORT]);
                }

                if (_configuration.GenreMappings.ContainsKey(GENRE_NEWS) && _configuration.GenreMappings[GENRE_NEWS] != null)
                {
                    _newsGenres.AddRange(_configuration.GenreMappings[GENRE_NEWS]);
                }

                if (_configuration.GenreMappings.ContainsKey(GENRE_KIDS) && _configuration.GenreMappings[GENRE_KIDS] != null)
                {
                    _kidsGenres.AddRange(_configuration.GenreMappings[GENRE_KIDS]);
                }

                if (_configuration.GenreMappings.ContainsKey(GENRE_LIVE) && _configuration.GenreMappings[GENRE_LIVE] != null)
                {
                    _liveGenres.AddRange(_configuration.GenreMappings[GENRE_LIVE]);
                }
            }
        }

        /// <summary>
        /// Populates the program genres.
        /// </summary>
        /// <param name="program">The program.</param>
        public void PopulateProgramGenres(ProgramInfo program)
        {
            // Check there is a program and genres to map
            if (program != null && program.Overview != null)
            {
                program.Genres = new List<String>();

                if (_movieGenres.All(g => !string.IsNullOrWhiteSpace(g)) && program.SeasonNumber == null && program.EpisodeNumber == null)
                {
                    var genre = _movieGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    program.Genres.Add(genre);
                    program.IsMovie = _movieGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_sportGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _sportGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    program.Genres.Add(genre);
                    program.IsSports = _sportGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_newsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _newsGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    program.Genres.Add(genre);
                    program.IsNews = _newsGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_kidsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _kidsGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    program.Genres.Add(genre);
                    program.IsKids = _kidsGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_liveGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _liveGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    program.Genres.Add(genre);
                    program.IsLive = _liveGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_seriesGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _seriesGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    program.Genres.Add(genre);
                    program.IsSeries = _seriesGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (program.IsSeries)
                {
                    program.IsPremiere = true;
                }
                if (program.IsSports || program.IsNews || program.IsKids || program.IsLive)
                {
                    program.IsSeries = true;
                }
                if (_seriesGenres.All(g => string.IsNullOrWhiteSpace(g)))
                {
                    program.IsSeries = true;
                }
            }
        }

        /// <summary>
        /// Populates the recording genres.
        /// </summary>
        /// <param name="recording">The recording.</param>
        public void PopulateRecordingGenres(MyRecordingInfo recording)
        {
            // Check there is a recording and genres to map
            if (recording != null && recording.Overview != null)
            {
                recording.Genres = new List<String>();

                if (_movieGenres.All(g => !string.IsNullOrWhiteSpace(g)) && recording.SeasonNumber == null && recording.EpisodeNumber == null)
                {
                    var genre = _movieGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    recording.Genres.Add(genre);
                    recording.IsMovie = _movieGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_sportGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _sportGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    recording.Genres.Add(genre);
                    recording.IsSports = _sportGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_newsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _newsGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    recording.Genres.Add(genre);
                    recording.IsNews = _newsGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_kidsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _kidsGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    recording.Genres.Add(genre);
                    recording.IsKids = _kidsGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_liveGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _liveGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    recording.Genres.Add(genre);
                    recording.IsLive = _liveGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_seriesGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _seriesGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    recording.Genres.Add(genre);
                    recording.IsSeries = _seriesGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_seriesGenres.All(g => string.IsNullOrWhiteSpace(g)) && recording.EpisodeNumber.HasValue)
                {
                    if (!recording.IsMovie && !recording.IsSports && !recording.IsNews && !recording.IsKids && !recording.IsLive)
                    {
                        recording.IsSeries = true;
                    }
                }
            }
        }

        /// <summary>
        /// Populates the timer genres.
        /// </summary>
        /// <param name="timer">The timer.</param>
        public void PopulateTimerGenres(TimerInfo timer)
        {
            // Check there is a timer and genres to map
            if (timer != null && timer.Overview != null)
            {
                timer.Genres = new List<String>();

                if (_movieGenres.All(g => !string.IsNullOrWhiteSpace(g)) && timer.SeasonNumber == null && timer.EpisodeNumber == null)
                {
                    var genre = _movieGenres.FirstOrDefault(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    timer.Genres.Add(genre);
                    timer.IsMovie = _movieGenres.Any(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_sportGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _sportGenres.FirstOrDefault(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    timer.Genres.Add(genre);
                    timer.IsSports = _sportGenres.Any(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_newsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _newsGenres.FirstOrDefault(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    timer.Genres.Add(genre);
                    timer.IsNews = _newsGenres.Any(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_kidsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _kidsGenres.FirstOrDefault(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    timer.Genres.Add(genre);
                    timer.IsKids = _kidsGenres.Any(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_seriesGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _seriesGenres.FirstOrDefault(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                    if (!String.IsNullOrWhiteSpace(genre))
                        genre = Regex.Replace(genre, @"^\w+\W\s?", String.Empty, RegexOptions.IgnoreCase);

                    timer.Genres.Add(genre);
                    timer.IsProgramSeries = _seriesGenres.Any(g => timer.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_seriesGenres.All(g => string.IsNullOrWhiteSpace(g)) && timer.SeriesTimerId != null)
                {
                    if (!timer.IsMovie && !timer.IsSports && !timer.IsNews && !timer.IsKids && !timer.IsLive)
                    {
                        timer.IsProgramSeries = true;
                    }
                }
            }
        }
    }
}