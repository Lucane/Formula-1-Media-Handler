using Formula_1_Media_Handler.Properties;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Formula_1_Media_Handler;

public class XmlOps
{
    public enum XmlType
    {
        Metadata,
        Downloads
    }

    public class Episode
    {
        public string Title { get; set; } = String.Empty;
        public string SeasonNr { get; set; } = String.Empty;
        public string EpisodeNr { get; set; } = String.Empty;
        public string Date { get; set; } = String.Empty;
        public string Updated { get; set; } = String.Empty;
        public string TorrentName { get; set; } = String.Empty;
        public string TorrentHash { get; set; } = String.Empty;
    }

    //public static IEnumerable<MetadataElement> SearchMetadata(MetadataFilter filter, bool exactMatch = true, bool enableRegex = false)
    //{

    //}

    public static void EditUpdatedTimestamps(List<Episode> episodes)
    {
        var metaXML = XDocument.Load(Globals.METADATA_XML_PATH);
        var matches = metaXML.Root?.Elements("episode")
                      .Where(x => episodes.Any(y => x.Element("season")?.Value == y.SeasonNr &&
                                                    x.Element("episode")?.Value == y.EpisodeNr)).ToList();

        var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        //var metaXML = Search(XmlType.Metadata).ToList();
        //var matches = metaXML.Where(x => episodes.Any(y => x.SeasonNr == y.SeasonNr &&
        //                                                   x.EpisodeNr == y.EpisodeNr));

        foreach (var m in matches)
        {

            m.Element("updated")?.SetValue(dateTime);
        }

        metaXML.Save(Globals.METADATA_XML_PATH);
    }


    public static void EditUpdatedTimestamp(string season, string episode, bool clearTimestamp = false)
    {
        var dateTime = clearTimestamp ? "" : DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var metaXML = XDocument.Load(Globals.METADATA_XML_PATH);
        var match = metaXML.Root?.Elements("episode")
                      .Where(x => x.Element("season")?.Value == season &&
                                  x.Element("episode")?.Value == episode);

        match?.FirstOrDefault()?.SetElementValue("updated", dateTime);
        metaXML.Save(Globals.METADATA_XML_PATH);
    }

    /// <summary>
    /// Searches the specified XML document for matching episodes and returns the results. Returns an empty <see cref="IEnumerable{XElement}"/> if no matches are found.
    /// </summary>
    /// <param name="filter">You can filter by title, season and episode. Pass an empty <see cref="MetadataFilter"/> object to return all values in the file.</param>
    /// <param name="type">XML document type of either <see cref="XmlType.Metadata"/> or <see cref="XmlType.Downloads"/>.</param>
    /// <param name="exactMatch">Whether to only return exact matches. If <paramref name="enableRegex"/> is set to <see langword="true"/> then this parameter is considered to be <see langword="false"/>.</param>
    /// <param name="enableRegex">Enables matching with regular expressions instead. Settings this to <see langword="true"/> will ignore the <paramref name="exactMatch"/> parameter.</param>
    /// <returns></returns>
    public static IEnumerable<Episode> Search(XmlType type, Episode? filter = null, bool exactMatch = true, bool enableRegex = false)
    {
        if (filter is not null)
        {
            foreach (var prop in filter.GetType().GetProperties())
            {
                //if () {
                //    var ex = new ArgumentNullException(nameof(prop));
                //    LogWriter.Write("The program encountered an unexpected error!", LogWriter.Type.FATAL, ex, true);
                //    throw ex;
                //}

                if (!enableRegex)
                {
                    prop.SetValue(filter, Regex.Escape(prop.GetValue(filter, null)?.ToString() ?? ""));
                }

                if (exactMatch)
                {
                    //prop.SetValue(filter, 
                    //    String.IsNullOrWhiteSpace(prop.GetValue(filter, null)!.ToString()) ? "" : $"^{prop.GetValue(filter, null)}$");

                    prop.SetValue(filter, $"^{prop.GetValue(filter, null)!}$");
                }
            }
        }
        
        var xmlPath = "";

        switch (type) { 
            case XmlType.Metadata:
                xmlPath = Globals.METADATA_XML_PATH;
                break;

            case XmlType.Downloads:
                xmlPath = Globals.TORRENTS_XML_PATH;
                break;
        }

        if (!File.Exists(xmlPath)) { return new List<Episode>(); }

        var metaXML = XDocument.Load(xmlPath);

        var matches = metaXML.Root?.Elements("episode")
                      .Where(x => Regex.IsMatch(x.Element("title")?.Value ?? "", filter?.Title ?? ""))
                      .Where(x => Regex.IsMatch(x.Element("season")?.Value ?? "", filter?.SeasonNr ?? ""))
                      .Where(x => Regex.IsMatch(x.Element("episode")?.Value ?? "", filter?.EpisodeNr ?? ""))
                      .Where(x => Regex.IsMatch(x.Element("date")?.Value ?? "", filter?.Date ?? ""))
                      .Where(x => Regex.IsMatch(x.Element("updated")?.Value ?? "", filter?.Updated ?? ""))
                      .Where(x => Regex.IsMatch(x.Element("name")?.Value ?? "", filter?.TorrentName ?? ""))
                      .Where(x => Regex.IsMatch(x.Element("hash")?.Value ?? "", filter?.TorrentHash ?? ""));

        var objList = new List<Episode>();

        foreach (var m in matches) {
            var ep = new Episode();

            ep.Title = m.Element("title")?.Value ?? "";
            ep.SeasonNr = m.Element("season")?.Value ?? "";
            ep.EpisodeNr = m.Element("episode")?.Value ?? "";
            ep.Date = m.Element("date")?.Value ?? "";
            ep.Updated = m.Element("updated")?.Value ?? "";
            ep.TorrentName = m.Element("name")?.Value ?? "";
            ep.TorrentHash = m.Element("hash")?.Value ?? "";
            
            objList.Add(ep);
        }

        return objList;
    }

    public static void Save(XmlType type, IEnumerable<Episode> episodes)
    {
        var torrentsXML = new XDocument(new XElement("episodes"));

        if (type == XmlType.Metadata) {
            foreach (var ep in episodes) {
                torrentsXML.Root.Add(new XElement("episode",
                                 new XElement("title", ep.Title),
                                 new XElement("season", ep.SeasonNr),
                                 new XElement("episode", ep.EpisodeNr),
                                 new XElement("date", ep.Date),
                                 new XElement("updated", ep.Updated)
                                 ));
            }

            torrentsXML.Save(Globals.METADATA_XML_PATH);

        } else if (type == XmlType.Downloads) {
            foreach (var ep in episodes) {
                torrentsXML.Root.Add(new XElement("episode",
                                 new XElement("title", ep.Title),
                                 new XElement("season", ep.SeasonNr),
                                 new XElement("episode", ep.EpisodeNr),
                                 new XElement("name", ep.TorrentName),
                                 new XElement("hash", ep.TorrentHash)
                                 ));
            }

            torrentsXML.Save(Globals.TORRENTS_XML_PATH);
        }
    }

    /// <summary>Removes an episode from the XML torrent queue (doesn't remove the torrent).</summary>
    /// <param name="season">Season number of the episode.</param>
    /// <param name="episode">Episode number of the episode.</param>
    public static void RemoveFromTorrentQueue(string season, string episode)
    {
        if (String.IsNullOrWhiteSpace(season) ||
            String.IsNullOrWhiteSpace(episode) ||
            !File.Exists(Globals.TORRENTS_XML_PATH)) {

            LogWriter.Logger.Warn($"Could not remove 'S{season}E{episode}' from the download queue!");
            return;
        }

        //var episodes = XmlOps.Search(XmlOps.XmlType.Downloads)
        //                             .Where(file => file.SeasonNr != season)
        //                             .Where(file => file.EpisodeNr != episode);

        var concatHash = Generic.CalculateConcatHash(season, episode);
        var episodes = XmlOps.Search(XmlOps.XmlType.Downloads)
                                     .Where(file => Generic.CalculateConcatHash(file.SeasonNr, file.EpisodeNr) != concatHash);


        XmlOps.Save(XmlOps.XmlType.Downloads, episodes);
        LogWriter.Logger.Info($"Removed 'S{season}E{episode}' from the download queue.");
    }

    /// <summary>
    /// Removes an episode from the XML torrent queue (doesn't remove the torrent).
    /// </summary>
    /// <param name="torrentHash">Torrent hash associated with the episode.</param>
    public static void RemoveFromTorrentQueue(string torrentHash)
    {
        if (String.IsNullOrWhiteSpace(torrentHash) ||
            !File.Exists(Globals.TORRENTS_XML_PATH)) {

            LogWriter.Logger.Warn($"Could not remove '{torrentHash}' from the download queue!");
            return;
        }

        var episodes = XmlOps.Search(XmlOps.XmlType.Downloads)
                                     .Where(file => file.TorrentHash != torrentHash);

        XmlOps.Save(XmlOps.XmlType.Downloads, episodes);
        LogWriter.Logger.Info($"Removed '{torrentHash}' from the download queue.");
    }

    /// <summary>Add/Update an episode to the XML torrent queue. Necessary for keeping track of torrents that are in-progress.<br/>
    /// If an entry for the episode already exists, only the <paramref name="torrentName"/> and <paramref name="torrentHash"/> arguments are updated (if the parameters are empty, then they're ignored).</summary>
    /// <remarks>This method is used for both adding and updating entries.</remarks>
    /// <param name="season">Season number of the episode.</param>
    /// <param name="episode">Episode number of the episode.</param>
    /// <param name="torrentName">(Optional) Path to the torrent destination folder.</param>
    /// <param name="torrentHash">(Optional) Torrent hash necessary for keeping track of the torrent.</param>
    public static async Task UpdateTorrentQueue(string season, string episode, string torrentName = "", string torrentHash = "")
    {
        //XDocument torrentsXML;

        //if (File.Exists(Globals.TORRENTS_XML_PATH)) {
        //    torrentsXML = XDocument.Load(Globals.TORRENTS_XML_PATH);
        //} else {
        //    torrentsXML = new XDocument(new XElement("episodes"));
        //}

        var torrentsXML = XmlOps.Search(XmlType.Downloads).ToList();
        var matchingEpisodes = XmlOps.Search(XmlType.Downloads)
                                     .Where(x => x.SeasonNr == season)
                                     .Where(x => x.EpisodeNr == episode);

        //var matchingEpisodes = from ep in torrentsXML.Descendants("episode")
        //                       where (string)ep.Element("season") == season.ToString()
        //                       where (string)ep.Element("episode") == episode.ToString()
        //                       select ep;

        // if download queue doesn't contain any matches, then query relevant info from the metadata and add to queue
        if (matchingEpisodes.Count() == 0) {
            //if (!File.Exists(Globals.METADATA_XML_PATH)) { await XmlOps.UpdateMetadata(); }
            //var metaXML = XDocument.Load(Globals.METADATA_XML_PATH);

            matchingEpisodes = XmlOps.Search(XmlType.Metadata)
                                     .Where(x => x.SeasonNr == season)
                                     .Where(x => x.EpisodeNr == episode);

            //matchingEpisodes = from ep in metaXML.Descendants("episode")
            //                   where (string)ep.Element("season") == season.ToString()
            //                   where (string)ep.Element("episode") == episode.ToString()
            //                   select ep;

            // metadata doesn't contain the needed episode, therefor exit; otherwise add name and hash to metadata info and add to queue
            if (matchingEpisodes.Count() == 0) {
                LogWriter.Logger.Warn($"No S{season}E{episode} entry found in metadata.");
                return;
            } else {
                matchingEpisodes.First().TorrentName = torrentName;
                matchingEpisodes.First().TorrentHash = torrentHash;

                //matchingEpisodes.First().Add(new XElement("name", torrentName),
                //                             new XElement("hash", torrentHash));

                //torrentsXML.Add(matchingEpisodes.First());
                //LogWriter.Write($"[INFO] Added S{season}E{episode} to the XML torrent queue.");

                torrentsXML.Add(matchingEpisodes.First());
            }
        } else {
            var index = torrentsXML.IndexOf(matchingEpisodes.First());

            if (torrentName != "") {
                LogWriter.Logger.Debug($"Updated XML torrent queue <NAME> attribute for S{season}E{episode} -- '{matchingEpisodes.First().TorrentName}' => '{torrentName}'");
                torrentsXML[index].TorrentName = torrentName;
            }
            if (torrentHash != "") {
                LogWriter.Logger.Debug($"Updated XML torrent queue <HASH> attribute for S{season}E{episode} -- '{matchingEpisodes.First().TorrentHash}' => '{torrentHash}'");
                torrentsXML[index].TorrentHash = torrentHash;
            }
        }

        XmlOps.Save(XmlType.Downloads, torrentsXML);
        //torrentsXML.Save(Globals.TORRENTS_XML_PATH);
    }

    /// <summary>Updates the metadata for all seasons/episodes.</summary>
    /// <remarks>The updated metadata is saved to .\metadata.xml. The metadata includes the following elements for each episode: title, season [digit], episode [digit], date [MMM DD, YYYY].</remarks>
    /// <param name="forceUpdate">Force a metadata update regardless of any task schedules.</param>
    /// <example>
    /// An example snippet of the saved file format.
    /// <code>
    /// <![CDATA[
    /// <?xml version="1.0" encoding="utf-8"?>
    /// <episodes>
    ///   <episode>
    ///     <title>British Grand Prix</title>
    ///     <season>1950</season>
    ///     <episode>01</episode>
    ///     <date>May 13, 1950</date>
    ///   </episode>
    ///   [...]
    /// </episodes>
    /// ]]>
    /// </code>
    /// </example>
    public static async Task UpdateMetadata(bool forceUpdate = false)
    {
        //if (File.Exists(Globals.METADATA_XML_PATH) && !forceUpdate) {
        //    if (DateTimeOffset.Now.ToUnixTimeSeconds() <= Globals.SETTINGS.LastUpdate + Globals.SETTINGS.UpdateInterval) {
        //        var nextUpdate = Globals.SETTINGS.LastUpdate + Globals.SETTINGS.UpdateInterval;
        //        LogWriter.Write($"[INFO] Skipping metadata update. Next update: {DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(nextUpdate)).LocalDateTime}");
        //        return;
        //    }
        //}

        var metaHTML = HTML.GetHTMLDoc();

        if (metaHTML != null) {
            LogWriter.Logger.Info("Saving updated metadata file...");
            HTML.DataScraper(metaHTML).Save(Globals.METADATA_XML_PATH);
            //Globals.SETTINGS.LastUpdate = DateTimeOffset.Now.ToUnixTimeSeconds();
            //Globals.SETTINGS.Save();
        } else if (File.Exists(Globals.METADATA_XML_PATH)) {
            LogWriter.Logger.Warn("Failed to update metadata file. Using existing metadata...");
        } else {
            LogWriter.Logger.Fatal("Failed to update metadata and no existing metadata file exists.");
            Environment.Exit(1);
        }

        Settings.Default.Cron_MetadataUpdate_LastRun = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Settings.Default.Save();
    }
}
