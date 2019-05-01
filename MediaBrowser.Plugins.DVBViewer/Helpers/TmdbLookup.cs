using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.DVBViewer.Services.Entities;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public class TmdbLookup
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        public TmdbLookup(IHttpClient httpClient, IJsonSerializer json, IServerConfigurationManager serverConfigurationManager)
        {
            _httpClient = httpClient;
            _json = json;
            _serverConfigurationManager = serverConfigurationManager;
        }

        public void GetTmdbImage(CancellationToken cancellationToken, MyRecordingInfo recording)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var config = Plugin.Instance.Configuration;
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var localImage = Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + ".jpg");
            var localImageMissing = Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + " [missing].jpg");

            if (!Directory.Exists(Path.Combine(pluginPath, "recordingposters")))
            {
                Directory.CreateDirectory(Path.Combine(pluginPath, "recordingposters"));
            }

            if ((recording.IsMovie || (!recording.EpisodeNumber.HasValue && (recording.EndDate - recording.StartDate) > TimeSpan.FromMinutes(70))) && !(File.Exists(localImage) || File.Exists(localImageMissing)))
            {
                try
                {
                    using (var tmdbMovieSearch = _httpClient.Get(new HttpRequestOptions()
                    {
                        Url = $"https://api.themoviedb.org/3/search/movie?api_key=9dbbec013a2d32baf38ccc58006cd991&query={recording.MovieName}" + $"&language={_serverConfigurationManager.Configuration.PreferredMetadataLanguage}",
                        CancellationToken = cancellationToken,
                        BufferContent = false,
                        EnableDefaultUserAgent = true,
                        AcceptHeader = "application/json",
                        EnableHttpCompression = true,
                        DecompressionMethod = CompressionMethod.Gzip
                    }).Result)
                    {
                        var movie = _json.DeserializeFromStream<TmdbMovieSearch>(tmdbMovieSearch);

                        if (movie.total_results > 0)
                        {
                            TmdbMovieResult tmdbMovieResult = movie.results.Find(x => x.title.Equals(recording.MovieName) || x.original_title.Contains(recording.EpisodeTitle)) ?? movie.results.First();

                            if (recording.MovieYear.HasValue)
                            {
                                tmdbMovieResult = movie.results.Find(x => x.release_date.StartsWith(recording.MovieYear.Value.ToString())) ?? movie.results.First();
                            }

                            var moviePoster = tmdbMovieResult.poster_path;
                            var movieBackdrop = tmdbMovieResult.backdrop_path;

                            if (config.RecGenreMapping)
                            {
                                if (!String.IsNullOrEmpty(moviePoster))
                                {
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile(new Uri($"https://image.tmdb.org/t/p/w500{moviePoster}"), localImage);
                                    }
                                }
                                else
                                {
                                    File.Create(localImageMissing);
                                }
                            }

                            if (!config.RecGenreMapping)
                            {
                                if (!String.IsNullOrEmpty(movieBackdrop))
                                {
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile(new Uri($"https://image.tmdb.org/t/p/w500{movieBackdrop}"), localImage);
                                    }
                                }
                                else
                                {
                                    File.Create(localImageMissing);
                                }
                            }
                        }
                        else
                        {
                            File.Create(localImageMissing);
                        }
                    }
                }
                catch (WebException)
                {
                    Plugin.Logger.Info("Could not download poster for Movie Recording: {0}", recording.Name);
                }
            }

            if ((recording.IsSeries || recording.EpisodeNumber.HasValue) && !(File.Exists(localImage) || File.Exists(localImageMissing)))
            {
                try
                {
                    using (var tmdbTvSearch = _httpClient.Get(new HttpRequestOptions()
                    {
                        Url = $"https://api.themoviedb.org/3/search/tv?api_key=9dbbec013a2d32baf38ccc58006cd991&query={recording.Name}" + $"&language={_serverConfigurationManager.Configuration.PreferredMetadataLanguage}",
                        CancellationToken = cancellationToken,
                        BufferContent = false,
                        EnableDefaultUserAgent = true,
                        AcceptHeader = "application/json",
                        EnableHttpCompression = true,
                        DecompressionMethod = CompressionMethod.Gzip
                    }).Result)
                    {
                        var tvshow = _json.DeserializeFromStream<TmdbTvSearch>(tmdbTvSearch);

                        if (tvshow.total_results > 0)
                        {
                            TmdbTvResult tmdbTvResult = tvshow.results.Find(x => x.name.Equals(recording.Name)) ?? tvshow.results.First();

                            var tvPoster = tmdbTvResult.poster_path;
                            var tvBackdrop = tmdbTvResult.backdrop_path;

                            if (config.RecGenreMapping)
                            {
                                if (!String.IsNullOrEmpty(tvPoster))
                                {
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile(new Uri($"https://image.tmdb.org/t/p/w500{tvPoster}"), localImage);
                                    }
                                }
                                else
                                {
                                    File.Create(localImageMissing);
                                }
                            }

                            if (!config.RecGenreMapping)
                            {
                                if (!String.IsNullOrEmpty(tvBackdrop))
                                {
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile(new Uri($"https://image.tmdb.org/t/p/w500{tvBackdrop}"), localImage);
                                    }
                                }
                                else
                                {
                                    File.Create(localImageMissing);
                                }
                            }
                        }
                        else
                        {
                            File.Create(localImageMissing);
                        }
                    }
                }
                catch (WebException)
                {
                    Plugin.Logger.Info("Could not download poster for TV Show Recording: {0}", recording.Name);
                }
            }
        }

        private class TmdbMovieSearch
        {
            public int total_results { get; set; }
            public int total_pages { get; set; }
            public List<TmdbMovieResult> results { get; set; }
        }

        private class TmdbMovieResult
        {
            public int id { get; set; }
            public string title { get; set; }
            public string original_title { get; set; }
            public string original_language { get; set; }
            public string release_date { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
        }

        private class TmdbTvSearch
        {
            public int total_results { get; set; }
            public List<TmdbTvResult> results { get; set; }
        }

        private class TmdbTvResult
        {
            public int id { get; set; }
            public string name { get; set; }
            public string original_title { get; set; }
            public string original_language { get; set; }
            public string first_air_date { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
        }

        private class TmdbEpisodeResult
        {
            public string name { get; set; }
        }
    }
}