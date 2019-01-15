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

        private readonly List<String> _eitMovieContent;
        private readonly List<String> _eitSeriesContent;
        private readonly List<String> _eitSportContent;
        private readonly List<String> _eitNewsContent;
        private readonly List<String> _eitKidsContent;
        private readonly List<String> _eitLiveContent;

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

            _eitMovieContent = new List<string>(new string[] { "16", "17", "18", "19", "20", "22", "23", "24" });
            _eitSeriesContent = new List<string>(new string[] { "21" });
            _eitSportContent = new List<string>(new string[] { "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75" });
            _eitNewsContent = new List<string>(new string[] { "32", "33", "34", "35", "128", "129", "130", "131", "144", "145", "146", "147", "148", "149" });
            _eitKidsContent = new List<string>(new string[] { "80", "81", "82", "83", "84", "85" });
            _eitLiveContent = new List<string>(new string[] { "179" });
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
            if (program != null && program.Etag != null && _configuration.EitContent)
            {
                program.IsMovie = _eitMovieContent.Any(c => program.Etag.Equals(c));
                program.IsSports = _eitSportContent.Any(c => program.Etag.Equals(c));
                program.IsNews = _eitNewsContent.Any(c => program.Etag.Equals(c));
                program.IsKids = _eitKidsContent.Any(c => program.Etag.Equals(c));
                program.IsLive = _eitLiveContent.Any(c => program.Etag.Equals(c));
                program.IsSeries = _eitSeriesContent.Any(c => program.Etag.Equals(c));

                if (program.IsSeries)
                {
                    program.IsPremiere = true;
                }
                if (program.IsSports || program.IsNews || program.IsKids || program.IsLive)
                {
                    program.IsSeries = true;
                }
            }

            // Check there is a program and genres to map
            if (program != null && program.Overview != null && !_configuration.EitContent)
            {
                program.Genres = new List<String>();

                if (_movieGenres.All(g => !string.IsNullOrWhiteSpace(g)) && program.SeasonNumber == null && program.EpisodeNumber == null)
                {
                    var genre = _movieGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        program.Genres.Add(genre);
                    }

                    program.IsMovie = _movieGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_sportGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _sportGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        program.Genres.Add(genre);
                    }

                    program.IsSports = _sportGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_newsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _newsGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        program.Genres.Add(genre);
                    }

                    program.IsNews = _newsGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_kidsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _kidsGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        program.Genres.Add(genre);
                    }

                    program.IsKids = _kidsGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_liveGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _liveGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        program.Genres.Add(genre);
                    }

                    program.IsLive = _liveGenres.Any(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_seriesGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _seriesGenres.FirstOrDefault(g => program.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        program.Genres.Add(genre);
                    }

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
            if (recording != null && recording.EitContent != null && _configuration.EitContent)
            {
                recording.IsMovie = _eitMovieContent.Any(c => recording.EitContent.Contains(c));
                recording.IsSports = _eitSportContent.Any(c => recording.EitContent.Contains(c));
                recording.IsNews = _eitNewsContent.Any(c => recording.EitContent.Contains(c));
                recording.IsKids = _eitKidsContent.Any(c => recording.EitContent.Contains(c));
                recording.IsLive = _eitLiveContent.Any(c => recording.EitContent.Contains(c));
                recording.IsSeries = _eitSeriesContent.Any(c => recording.EitContent.Contains(c));

                if (recording.EpisodeNumber.HasValue)
                {
                    if (!recording.IsMovie && !recording.IsSports && !recording.IsNews && !recording.IsKids && !recording.IsLive)
                    {
                        recording.IsSeries = true;
                    }
                }
            }

            // Check there is a recording and genres to map
            if (recording != null && recording.Overview != null && !_configuration.EitContent)
            {
                recording.Genres = new List<String>();

                if (_movieGenres.All(g => !string.IsNullOrWhiteSpace(g)) && recording.SeasonNumber == null && recording.EpisodeNumber == null)
                {
                    var genre = _movieGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        recording.Genres.Add(genre);
                    }

                    recording.IsMovie = _movieGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_sportGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _sportGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        recording.Genres.Add(genre);
                    }

                    recording.IsSports = _sportGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_newsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _newsGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        recording.Genres.Add(genre);
                    }

                    recording.IsNews = _newsGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_kidsGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _kidsGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        recording.Genres.Add(genre);
                    }

                    recording.IsKids = _kidsGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_liveGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _liveGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        recording.Genres.Add(genre);
                    }

                    recording.IsLive = _liveGenres.Any(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);
                }

                if (_seriesGenres.All(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var genre = _seriesGenres.FirstOrDefault(g => recording.Overview.IndexOf(g, StringComparison.InvariantCulture) != -1);

                    if (!String.IsNullOrWhiteSpace(genre))
                    {
                        genre = Regex.Replace(genre, @"^\w+\W\s?|\s\(\w+\)$", String.Empty, RegexOptions.IgnoreCase);
                        recording.Genres.Add(genre);
                    }

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
    }
}