using Formula_1_Media_Handler.Properties;
using QBittorrent.Client;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Formula_1_Media_Handler;

public class ScheduledTasks
{
    /// <summary>
    /// Searches for all episodes from monitored seasons.
    /// </summary>
    public static async Task MonitoredCheck()
    {
        //var downloadsXML = XDocument.Load(Globals.TORRENTS_XML_PATH);
        var monitoredXML = XDocument.Load(Globals.MONITORED_PATH);
        //var metaXML = XDocument.Load(Globals.METADATA_XML_PATH);

        var libraryFiles = InputOutput.FetchLibraryFiles();
        var monitoredSeasons = monitoredXML.Descendants("season").Select(x => (string)x.Value).ToList();
        //var episodeDownloads = downloadsXML.Root.Elements("episode");

        var episodeDownloads = XmlOps.Search(XmlOps.XmlType.Downloads);

        //IEnumerable<XElement> metadataMatches = new List<XElement>();

        //int tryTimes = 0;
        //while (tryTimes < 2)
        //{
        //    try {


        //        //metadataMatches = metaXML.Root.Elements("episode")
        //        //              .Where(x => DateTime.Compare(DateTime.Parse(x.Element("date").Value), DateTime.UtcNow.Date) < 0)
        //        //              .Where(x => monitoredSeasons.Any(y => y == x.Element("season").Value))
        //        //              .Where(x => !episodeDownloads.Any(y => (y.Element("season").Value == x.Element("season").Value) &&
        //        //                                                     (y.Element("episode").Value == x.Element("episode").Value)))
        //        //              .Where(x => !libraryFiles.Any(y => (y.Season == x.Element("season").Value) &&
        //        //                                                 (y.Episode == x.Element("episode").Value)));

        //        break;
        //    } catch (Exception ex) {
        //        if (tryTimes > 0) {
        //            LogWriter.Write("Failed to parse metadata file the second time! Ending task and retrying later...", LogWriter.Type.ERROR, ex);
        //            return;
        //        }

        //        LogWriter.Write("Failed to parse metadata file! Retrying in 5 seconds...", LogWriter.Type.ERROR, ex);
        //        await Task.Delay(TimeSpan.FromSeconds(5000));

        //    } finally {
        //        tryTimes++;
        //    }
        //}


        // LINQ query to generate list of episodes to search for.
        // Checks that the episode release date is same or equal to now.
        // Checks that the season matches 'monitoredSeasons'.
        // Checks that the episode isn't already in the download queue.
        // Finally checks that the file doesn't already exist locally.

        var metadataMatches = XmlOps.Search(XmlOps.XmlType.Metadata)
                                    .Where(x => DateTime.Compare(DateTime.Parse(x.Date), DateTime.UtcNow.Date) <= 0)
                                    .Where(x => monitoredSeasons.Any(y => y == x.SeasonNr))
                                    .Where(x => !episodeDownloads.Any(y => (y.SeasonNr == x.SeasonNr) &&
                                                                     (y.EpisodeNr == x.EpisodeNr)))
                                    .Where(x => !libraryFiles.Any(y => (y.Season == x.SeasonNr) &&
                                                                 (y.Episode == x.EpisodeNr)))
                                    .ToList();

        if (metadataMatches.Count == 0)
        {
            LogWriter.Logger.Info($"All monitored seasons are available. Skipping search...");
            Settings.Default.Cron_CheckMonitored_LastRun = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Settings.Default.Save();
            return;
        }

        var searches = new List<SearchObject>();
        LogWriter.Logger.Info($"Checking update rules for {metadataMatches.Count()} episodes...");

        for (int i = 0; i < metadataMatches.Count(); i++) {

            var dateParsed = DateTime.TryParse(metadataMatches[i].Date, out var airDate);
            var lastUpdated = Int64.TryParse(metadataMatches[i].Updated, out long outValue) ? DateTimeOffset.FromUnixTimeSeconds(outValue) : new DateTimeOffset();

            // skips the search if the episode release is more than 2 days old && was updated less than a day ago
            if (metadataMatches[i].Updated != "" && dateParsed &&
                DateTime.Today.Subtract(airDate) >= new TimeSpan(2,0,0,0) &&
                DateTimeOffset.UtcNow.Subtract(lastUpdated) < new TimeSpan(1,0,0,0)) {

                LogWriter.Logger.Trace($"S{metadataMatches[i].SeasonNr}E{metadataMatches[i].EpisodeNr} did not meet update criteria. Skipping...");
                continue;
            }

            int j = i;
            var searchObj = new SearchObject();
            searchObj.Title = metadataMatches[j].Title;
            searchObj.Season = metadataMatches[i].SeasonNr;
            searchObj.Episode = metadataMatches[i].EpisodeNr;
            searchObj.Pattern = $"formula 1 {searchObj.Season} {TorrentClient.CleanSearchPattern(searchObj.Title)} 1080p";
            LogWriter.Logger.Trace($"'{searchObj.Pattern}' -- S{searchObj.Season}E{searchObj.Episode} -- '{searchObj.Title}'");    //[REVIEW] remove later
            
            searches.Add(searchObj);
            XmlOps.EditUpdatedTimestamp(metadataMatches[i].SeasonNr, metadataMatches[i].EpisodeNr);
        }

        if (searches.Count() == 0) LogWriter.Logger.Info($"No monitored episodes match update criteria. Retrying later...");
        else
        {
            LogWriter.Logger.Info($"Starting torrent search for {searches.Count()} episodes...");
            var searchResults = await TorrentClient.SearchAsync(searches);

            if (searchResults.Count == 0) LogWriter.Logger.Info($"No search results returned. Retrying later...");
            else { 
                var totalCount = 0;

                foreach (var r in searchResults) {
                    if (r.SearchResult.Count == 0) {
                        LogWriter.Logger.Trace($"No search results found for S{r.Season}E{r.Episode}.");
                        continue;
                    }

                    LogWriter.Logger.Trace($"Torrent search for S{r.Season}E{r.Episode} returned {r.SearchResult.Count} results. Parsing results for matching torrents...");
                    var bestMatch = TorrentClient.FindBestMatch(r.SearchResult, r.Title, Int32.Parse(r.Season));

                    if (bestMatch != null) {
                        totalCount++;
                        await TorrentClient.AddAsync(bestMatch);

                        var hash = await TorrentClient.GetTorrentHashAsync(bestMatch.FileUrl) ?? "";

                        await XmlOps.UpdateTorrentQueue(r.Season, r.Episode, bestMatch.FileName, hash);
                        XmlOps.EditUpdatedTimestamp(r.Season, r.Episode, true);     // clears the updated timestamp value

                        LogWriter.Logger.Info($"Found a matching torrent for: S{r.Season}E{r.Episode} '{r.Title}'  =>  '{bestMatch.FileName}'");
                    }
                    //else {
                    //    LogWriter.Write($"Couldn't find a matching torrent for: S{r.Season}E{r.Episode} '{r.Title}'.", LogWriter.Type.INFO);
                    //}
                }

                if (totalCount == 0) LogWriter.Logger.Info($"No viable search matches found. Retrying later...");
                else LogWriter.Logger.Info($"Found matching torrents for {totalCount} episodes. Added torrents to download queue.");
            }
        }

        Settings.Default.Cron_CheckMonitored_LastRun = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Settings.Default.Save();
    }

    public static async Task TorrentCheck()
    {
        var episodes = XmlOps.Search(XmlOps.XmlType.Downloads);

        foreach (var ep in episodes) {
            //var torrents = await TorrentClient.Connection.Instance.Client.GetTorrentListAsync();
            var progress = await TorrentClient.GetTorrentProgressAsync(ep.TorrentHash);
            var sourceFile = await TorrentClient.FileMatch(ep.TorrentHash);

            if (!File.Exists(sourceFile) || progress == null || progress < 1) { continue; }

            var fileEnd = Regex.Match(
                Path.GetFileNameWithoutExtension(sourceFile),
                @$"(SkyF1HD|F1TV|1080p|720p).*",
                RegexOptions.IgnoreCase)
                .Value;

            var outputPath = InputOutput.GenerateOutputPath(ep.Title, ep.SeasonNr, ep.EpisodeNr, fileEnd, Path.GetExtension(sourceFile));

            var success = await TorrentClient.ImportTorrent(sourceFile, outputPath);
            if (success) {
                XmlOps.RemoveFromTorrentQueue(ep.SeasonNr, ep.EpisodeNr);
                await TorrentClient.DeleteAsync(ep.TorrentHash);
            }
        }

        Settings.Default.Cron_CheckTorrents_LastRun = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Settings.Default.Save();
    }

    public class SearchObject
    {
        public string Pattern { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;
        public string Episode { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public IReadOnlyList<SearchResult> SearchResult { get; set; } = new List<SearchResult>();
    }
}
