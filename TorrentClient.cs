using Formula_1_Media_Handler.Properties;
using QBittorrent.Client;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Formula_1_Media_Handler;

public class TorrentClient
{
    public sealed class Connection
    {
        private QBittorrentClient? _Client;
        public QBittorrentClient? Client
        {
            get => CheckConnection();
            private set => _Client = value;
        }

        private static readonly Lazy<Connection> _instance =
            new Lazy<Connection>(() => new Connection());

        public static Connection Instance { get { return _instance.Value; } }

        private Connection()
        {
            this._Client = CheckConnection();

            //if (!_instance.IsValueCreated)
            //{
            //    var connectionCount = 0;

            //    while (connectionCount < 5)
            //    {
            //        try
            //        {
            //            var client = new QBittorrentClient(new Uri(Settings.Default.QBWebURI));
            //            client.LoginAsync(Settings.Default.QBLogin, Settings.Default.QBPassword).Wait();
            //            this.Client = client;

            //            LogWriter.Logger.Info("Successfully established a connection to qBittorrent!");
            //            break;
            //        }
            //        catch (Exception ex)
            //        {
            //            if (++connectionCount == 5)
            //            {
            //                LogWriter.Logger.Fatal("Failed to establish a connection to qBittorrent!", ex);
            //                throw;
            //            }
            //        }
            //    }
            //}
        }

        private QBittorrentClient? CheckConnection()
        {
            try
            {
                if (!_instance.IsValueCreated) throw new SystemException();
                var connectionStatus = Instance._Client!.GetQBittorrentVersionAsync().Result;
                return _Client;
            }
            catch
            {
                if (_instance.IsValueCreated) LogWriter.Logger.Info("Connection to qBittorrent timed out. Reconnecting...");

                var connectionCount = 0;

                while (connectionCount < 5)
                {
                    try
                    {
                        var client = new QBittorrentClient(new Uri(Settings.Default.QBWebURI));
                        client.LoginAsync(Settings.Default.QBLogin, Settings.Default.QBPassword).Wait();
                        this._Client = client;

                        if (!_instance.IsValueCreated) LogWriter.Logger.Info("Successfully established a connection to qBittorrent!");
                        return _Client;
                    }
                    catch (Exception ex)
                    {
                        if (++connectionCount == 5)
                        {
                            LogWriter.Logger.Fatal("Failed to establish a connection to qBittorrent!", ex);
                            throw;
                        }
                    }
                }
            }

            return _Client;     // "not all code paths return a value" error if this line is omitted?
        }
    }

    /// <summary>Returns the hash of the first matching torrent found. Returns <see langword="null"/> if no match is found.</summary>
    /// <param name="torrentName">Full name of the torrent. No partial strings allowed (exact matches only).</param>
    /// <returns></returns>
    public static async Task<string?> GetTorrentHashAsync(string torrentName)
    {
        //var client = new QBittorrentClient(new Uri(Settings.Default.QBWebURI));
        //await client.LoginAsync(Settings.Default.QBLogin, Settings.Default.QBPassword);

        var resultsTask = await Connection.Instance.Client.GetTorrentListAsync();
        var result = resultsTask.Where(x => String.Equals(x.Name, torrentName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        if (result == null) {
            //LogWriter.Write($"Could not find a torrent with the name: '{torrentName}'", LogWriter.Type.DEBUG);
            return null;
        }
        else {
            return result.Hash;
        }
    }

    public static async Task<double?> GetTorrentProgressAsync(string torrentHash)
    {
        var resultsTask = await Connection.Instance.Client.GetTorrentListAsync();
        var result = resultsTask.Where(x => String.Equals(x.Hash, torrentHash, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        return result?.Progress;
    }

    public static async Task<IReadOnlyList<ScheduledTasks.SearchObject>> SearchAsync(List<ScheduledTasks.SearchObject> searchObj)
    {
        //var client = new QBittorrentClient(new Uri(Globals.SETTINGS.QBWebURI));
        //await client.LoginAsync(Globals.SETTINGS.QBLogin, Globals.SETTINGS.QBPassword);

        //var resultsList = new List<SearchResults>();

        foreach (var p in searchObj) {
            var resultID = await Connection.Instance.Client.StartSearchAsync(p.Pattern);
            var results = await Connection.Instance.Client.GetSearchResultsAsync(resultID);

            while (results.Status != SearchJobStatus.Stopped) {
                new ManualResetEvent(false).WaitOne(100);
                results = await Connection.Instance.Client.GetSearchResultsAsync(resultID);
            }

            p.SearchResult = results.Results;

            //resultsList.Add(results);
            //LogWriter.Write($"[INFO] Finished searching for torrents with the pattern '{p.Pattern}' -- {results.Results.Count()} results found");
        }

        //client.Dispose();
        return searchObj;
        //return results.Results;
    }

    public static async Task<bool> ImportTorrent(string fileSource, string fileDestination)
    {
        fileSource = Path.GetFullPath(fileSource);

        try {
            FileInfo fi = new FileInfo(fileSource);
            fi.MoveTo(fileDestination);
            LogWriter.Logger.Info($"Successfully imported torrent: '{fileSource}'  =>  '{fileDestination}'");
            return true;
        } catch (Exception ex) {
            LogWriter.Logger.Info($"Could not import torrent: '{fileSource}'  =>  '{fileDestination}'", ex);
            return false;
        }
    }

    //Retrieve the redirect url using HttpClient from the Headers of the Response
    //https://gist.github.com/Delaire/3f1283dc6365d705dfd6ba24498d4993
    public static async Task<string?> GetTorrentHashAsync(Uri magnetUri)
    {
        if (!magnetUri.ToString().Contains("magnet:?", StringComparison.OrdinalIgnoreCase)) {
            var handler = new HttpClientHandler() {
                AllowAutoRedirect = false
            };
            Uri? redirectedUrl = null;

            using (HttpClient client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(magnetUri))
            using (HttpContent content = response.Content) {
                if (response.StatusCode == System.Net.HttpStatusCode.Found) {
                    HttpResponseHeaders headers = response.Headers;
                    if (headers != null && headers.Location != null) {
                        redirectedUrl = headers.Location;
                    }
                }
            }

            if (redirectedUrl == null) return null;
            magnetUri = redirectedUrl;
        }

        return Regex.Match(magnetUri.ToString(), @"xt=urn:btih:(.*)&dn").Groups[1].Value;
    }

    public static async Task<string?> GetTorrentNameAsync(string torrentHash)
    {
        var resultsTask = await TorrentClient.Connection.Instance.Client.GetTorrentListAsync();
        var result = resultsTask.Where(x => String.Equals(x.Hash, torrentHash, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        return result?.Name;
    }

    public static async Task SaveFile(string fileUrl, string pathToSave)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(fileUrl);

        using (var stream = await response.Content.ReadAsStreamAsync()) {
            var fileInfo = new FileInfo(pathToSave);
            using (var fileStream = fileInfo.OpenWrite()) {
                await stream.CopyToAsync(fileStream);
            }
        }
    }

    public static async Task AddAsync(SearchResult result)
    {
        var request = new AddTorrentUrlsRequest(result.FileUrl);
        await TorrentClient.Connection.Instance.Client.AddTorrentsAsync(request);
        //var client = new QBittorrentClient(new Uri(Globals.SETTINGS.QBWebURI));
        //await client.LoginAsync(Globals.SETTINGS.QBLogin, Globals.SETTINGS.QBPassword);
        //LogWriter.Write($"[INFO] Added new torrent to client: {result.FileName}");
        //client.Dispose();
    }

    /// <summary>
    /// Deletes the torrent from the client and any remnant files.
    /// </summary>
    /// <param name="torrentHash">Hash value of the torrent.</param>
    /// <returns></returns>
    public static async Task DeleteAsync(string torrentHash)
    {
        var torrentName = await GetTorrentNameAsync(torrentHash);
        await TorrentClient.Connection.Instance.Client.DeleteAsync(torrentHash, true);
        LogWriter.Logger.Info($"Deleted the torrent and remnant files: '{torrentHash}'  =>  '{torrentName}'");
    }

    public static SearchResult? FindBestMatch(IReadOnlyList<SearchResult> searchResults, string title, int season)
    {
        SearchResult? bestMatch = null;
        long? bestSeeds = null;
        var bestLD = 1000;

        foreach (var r in searchResults) {
            var name = InputOutput.ReturnCleanTitle(r.FileName);
            
            if (name == null || name == "") {
                continue;
            }

            name = Generic.RemoveNonOccurringWords(name, season);
            var nameNum = Regex.Match(name, @"\w (\d)", RegexOptions.RightToLeft).Groups[1].Value;
            var titleNum = Regex.Match(title, @"\w (\d)", RegexOptions.RightToLeft).Groups[1].Value;
            
            //Checks that the name and title numbers match: ex 'Japan (Practice 1)' =/= 'Japan (Practice 2)'
            if (!String.Equals(nameNum, titleNum, StringComparison.OrdinalIgnoreCase)) { continue; }

            var ld = Generic.LevenshteinDistance(name, title, true);

            if ((ld < bestLD && r.Seeds > 0) ||
                (ld == bestLD && r.Seeds > bestSeeds)) {

                bestMatch = r;
                bestLD = ld;
                bestSeeds = r.Seeds;
            }
        }

        return bestMatch;
    }

    public static string CleanSearchPattern(string searchPattern)
    {
        searchPattern = searchPattern.Replace("-", "")
                         .Replace("_", "")
                         .Replace("(", "")
                         .Replace(")", "");

        searchPattern = Regex.Replace(searchPattern, @"\d+", "");
        searchPattern = Regex.Replace(searchPattern.Trim(), @"\s+", " ");
        
        return searchPattern;
    }

    public static async Task<string?> FileMatch(string torrentHash)
    {
        //var client = new QBittorrentClient(new Uri(Settings.Default.QBWebURI));
        //await client.LoginAsync(Settings.Default.QBLogin, Settings.Default.QBPassword);

        //var resultsTask = await client.GetTorrentListAsync();
        var resultsTask = await TorrentClient.Connection.Instance.Client.GetTorrentListAsync();
        var result = resultsTask.Where(x => String.Equals(x.Hash, torrentHash, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        if (result == null) { return null; }

        var allowedExtensions = new[] { ".mkv", ".mp4", ".avi" };
        var files = Directory
            .GetFiles(result.ContentPath)
            .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
            .ToList();

        //client.Dispose();

        if (files.Count() == 1) {
            return files.First();
        }
        else {
            foreach (var f in files) {
                var fileName = new DirectoryInfo(f).Name;
                if (Regex.IsMatch(fileName, @$"02\..*\.Session", RegexOptions.IgnoreCase)) {
                    return f;
                }
            }
        }

        return null;
    }
}
