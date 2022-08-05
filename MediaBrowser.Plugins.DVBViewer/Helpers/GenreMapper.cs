using System.Collections.Generic;
using System.Linq;

using MediaBrowser.Controller.LiveTv;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public class GenreMapper
    {       
        private readonly List<string> EitEducationalContent;
        private readonly List<string> EitKidsContent;
        private readonly List<string> EitLiveContent;
        private readonly List<string> EitMovieContent;
        private readonly List<string> EitNewsContent;
        private readonly List<string> EitSportContent;

        public GenreMapper()
        {
            EitEducationalContent = new List<string>(new string[] { "128", "129", "130", "131", "144", "145", "146", "147", "148", "149", "150", "151" });
            EitKidsContent = new List<string>(new string[] { "80", "81", "82", "83", "84", "85" });
            EitLiveContent = new List<string>(new string[] { "179" });
            EitMovieContent = new List<string>(new string[] { "16", "17", "18", "19", "20", "21", "22", "23", "24" });
            EitNewsContent = new List<string>(new string[] { "32", "33", "34", "35", "36" });
            EitSportContent = new List<string>(new string[] { "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75" });
        }

        public void SetProgramCategories(ProgramInfo program)
        {
            program.IsEducational = EitEducationalContent.Any(c => program.Etag.Equals(c));
            program.IsKids = EitKidsContent.Any(c => program.Etag.Equals(c));
            program.IsLive = EitLiveContent.Any(c => program.Etag.Equals(c));
            program.IsMovie = EitMovieContent.Any(c => program.Etag.Equals(c)) && program.EpisodeNumber == null && ((program.EndDate - program.StartDate).TotalMinutes > 65);
            program.IsNews = EitNewsContent.Any(c => program.Etag.Equals(c));
            program.IsSports = EitSportContent.Any(c => program.Etag.Equals(c));

            if (!program.IsMovie)
            {
                program.IsSeries = true;
            }
        }
    }
}
