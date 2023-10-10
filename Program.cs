using Formula_1_Media_Handler.Properties;
using HtmlAgilityPack;
using QBittorrent.Client;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Quartz;
using Quartz.Impl;
using Formula_1_Media_Handler;

AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogWriter.AppDomain_CurrentDomain_UnhandledException);



#if DEBUG

var results = await TorrentClient.Connection.Instance.Client.StartSearchAsync("test123");
await TorrentClient.Connection.Instance.Client.LogoutAsync();
Console.WriteLine("RES1: " + results);

results = await TorrentClient.Connection.Instance.Client.StartSearchAsync("test1234");
Console.WriteLine("RES2: " + results);

Console.ReadKey();
Environment.Exit(0);

#endif

//var level = ConfigurationUserLevel.PerUserRoamingAndLocal;
//var configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(level);
//var configurationFilePath = configuration.FilePath;

//LogWriter.Logger.Info(configurationFilePath);
//LogWriter.Logger.Info(Settings.Default.Cron_CheckMonitored_Job);


if (Environment.UserInteractive) {
    await Generic.StartupChecks();
    //await Generic.RefreshSettings();
    await Setup.StartPrompt.Execute();

    //if (Settings.Default.FirstTimeSetup) {

    //} else {

    //}
} else {
    await Generic.StartupChecks();
    //await Generic.RefreshSettings();
    await Generic.StartCronJobs();
    //ServiceHandler.Setup.RunAsAService();
    Console.ReadKey();
    LogWriter.Logger.Warn("Service is shutting down...");
}


public class Globals
{
    //public static Settings? SETTINGS = Formula_1_Media_Handler.Properties.Settings.Default;
    //public static readonly string? EXE_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public static readonly string? EXE_PATH = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string LOG_PATH = $@"{EXE_PATH}\F1MH.log";
    public static readonly string METADATA_XML_PATH = $@"{EXE_PATH}\metadata.xml";
    public static readonly string TORRENTS_XML_PATH = $@"{EXE_PATH}\downloads.xml";
    public static readonly string MONITORED_PATH = $@"{EXE_PATH}\monitored.xml";
}

public class HTML
{
    public static XDocument DataScraper(HtmlDocument htmlDoc)
    {
        var metaXML = new XDocument(new XElement("episodes"));

        foreach (HtmlNode episodeNode in htmlDoc.DocumentNode.SelectNodes("//li[@class='list-group-item']")) {

            var episodeLabelNode = episodeNode.SelectSingleNode(".//span[@class='text-muted episode-label']");
            var season = Regex.Match(episodeLabelNode.InnerText, @"S(\d+)E").Groups[1].Value;
            var episode = Regex.Match(episodeLabelNode.InnerText, @"E(\d+)").Groups[1].Value;
            var title = episodeNode.SelectSingleNode(".//*[@class='list-group-item-heading']/a").InnerText.Trim();
            var date = episodeNode.SelectSingleNode(".//*[@class='list-inline text-muted']/li").InnerText.Trim();

            metaXML.Root.Add(new XElement("episode",
                new XElement("title", title),
                new XElement("season", season),
                new XElement("episode", episode),
                new XElement("date", date)
            ));
        }

        return metaXML;
    }

    public static HtmlDocument? GetHTMLDoc()
    {
        try {
            var web = new HtmlWeb();
            var htmlDoc = web.Load(Settings.Default.TvdbURL);
            return htmlDoc;
        }
        catch (Exception ex) {
            LogWriter.Logger.Info("Failed to load HTML document from TVDB.", ex);
            return null;
        }
    }
}

public class Generic
{
    //public class Options
    //{
    //    [Option('v', "verbose", HelpText = "Set output to verbose messages.")]
    //    public bool Verbose { get; set; }

    //    [Option('p', "param", HelpText = "Change program parameters.")]
    //    public bool Param { get; set; }

    //    [Option('m', "metadata", HelpText = "Force metadata update.")]
    //    public bool Metadata { get; set; }

    //    [Option('s', "source", Required = true, HelpText = "Source directory to import files from.")]
    //    public string Source { get; set; }
    //}

    //private static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
    //{
    //    if (e != null && e.ExceptionObject != null) {
    //        LogWriter.Write("Encountered an exception!", LogWriter.Type.FATAL, (Exception)e.ExceptionObject);
    //    }
    //}

    //public static async Task RefreshSettings()
    //{
    //    foreach (SettingsProperty currentProperty in Settings.Default.Properties) {
    //        Settings.Default[currentProperty.Name] = Settings.Default[currentProperty.];
    //        Settings.Default.Save();
    //    }
    //}

    //public static bool IsNullOrEmpty(object obj)
    //{
    //    bool isNull = obj.GetType().GetProperties()
    //                        .All(p => p.GetValue(obj) != null);

    //    Console.WriteLine(isNull);

    //    //var properties = obj.GetType().GetProperties();
    //    //Console.WriteLine(obj == null);

    //    //foreach (var p in properties) {
    //    //    var boo = String.IsNullOrEmpty(p.GetValue(obj, null).ToString());
    //    //    var boo2 = p.GetValue(obj, null) == DBNull.Value;
    //    //    Console.WriteLine(p.Name + " - " + p.GetValue(obj, null) + " - " + boo + " / " + boo2);
    //    //}

    //    //foreach (PropertyInfo pi in myObject.GetType().GetProperties()) {
    //    //    string value = (string)pi.GetValue(myObject);
    //    //    if (String.IsNullOrEmpty(value)) {
    //    //        return false;
    //    //    }
    //    //}

    //    return true;
    //}

    //public static DateTime ConvertFromDateTimeOffset(DateTimeOffset dateTime)
    //{
    //    if (dateTime.Offset.Equals(TimeSpan.Zero))
    //        return dateTime.UtcDateTime;
    //    else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
    //        return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local);
    //    else
    //        return dateTime.DateTime;
    //}

    public static string ConvertFromDateTimeOffset(DateTimeOffset dateTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        if (dateTime.Offset.Equals(TimeSpan.Zero))
            return dateTime.UtcDateTime.ToLocalTime().ToString(format);
        else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
            return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local).ToString(format);
        else
            return dateTime.DateTime.ToString(format);
    }

    public static async Task StartCronJobs()
    {
        try {
            LogWriter.Logger.Trace("Generating cron jobs...");

            StdSchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = await factory.GetScheduler();

            await scheduler.Start();

            IJobDetail torrentsJob = JobBuilder.Create<ServiceHandler.TorrentCheck>()
                .WithIdentity(nameof(Settings.Default.Cron_CheckTorrents_Job))
                .Build();

            ITrigger torrentsTrigger = TriggerBuilder.Create()
                .WithIdentity(nameof(Settings.Default.Cron_CheckTorrents_Job))
                .WithCronSchedule(Settings.Default.Cron_CheckTorrents_Job, x => x.WithMisfireHandlingInstructionFireAndProceed())
                .StartAt(DateTimeOffset.FromUnixTimeSeconds(Settings.Default.Cron_CheckTorrents_LastRun))
                .Build();


            IJobDetail monitoredJob = JobBuilder.Create<ServiceHandler.MonitoredCheck>()
                .WithIdentity(nameof(Settings.Default.Cron_CheckMonitored_Job))
                .Build();

            ITrigger monitoredTrigger = TriggerBuilder.Create()
                .WithIdentity(nameof(Settings.Default.Cron_CheckMonitored_Job))
                .WithCronSchedule(Settings.Default.Cron_CheckMonitored_Job, x => x.WithMisfireHandlingInstructionFireAndProceed())
                .StartAt(DateTimeOffset.FromUnixTimeSeconds(Settings.Default.Cron_CheckMonitored_LastRun))
                .Build();


            IJobDetail metadataJob = JobBuilder.Create<ServiceHandler.MetadataUpdate>()
                .WithIdentity(nameof(Settings.Default.Cron_MetadataUpdate_Job))
                .Build();

            ITrigger metadataTrigger = TriggerBuilder.Create()
                .WithIdentity(nameof(Settings.Default.Cron_MetadataUpdate_Job))
                .WithCronSchedule(Settings.Default.Cron_MetadataUpdate_Job, x => x.WithMisfireHandlingInstructionFireAndProceed())
                .StartAt(DateTimeOffset.FromUnixTimeSeconds(Settings.Default.Cron_MetadataUpdate_LastRun))
                .Build();


            //CronExpression expression = new CronExpression(Settings.Default.Cron_CheckTorrents_Job);
            //expression.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            //DateTimeOffset? nextFireUTCTime = expression.GetNextValidTimeAfter(Settings.Default.Cron_CheckTorrents_LastRun);
            //if (Settings.Default.Cron_CheckTorrents_LastRun > nextFireUTCTime) {
            //    Console.WriteLine("next: " + nextFireUTCTime + " | " + DateTime.UtcNow);
            //}

            await scheduler.ScheduleJob(torrentsJob, torrentsTrigger);
            await Task.Delay(TimeSpan.FromSeconds(60));

            await scheduler.ScheduleJob(monitoredJob, monitoredTrigger);
            await Task.Delay(TimeSpan.FromSeconds(60));

            await scheduler.ScheduleJob(metadataJob, metadataTrigger);
            await Task.Delay(TimeSpan.FromSeconds(60));

        } catch (Exception ex) {
            LogWriter.Logger.Fatal("Failed to start cron jobs. Shutting down...", ex);
            throw;
        }
    }

    //public class CronTask
    //{
    //    public IJobDetail JobDetail { get; set; } = new IJobDetail;
    //    public ITrigger JobTrigger { get; set; }

    //}

    //public static async Task<CronTask> GetCronTask(string identity, string cronExpression, long lastRun)
    //{
    //    IJobDetail detail = JobBuilder.Create<ServiceHandler.TorrentCheck>()
    //            .WithIdentity(identity)
    //            .Build();

    //    ITrigger trigger = TriggerBuilder.Create()
    //        .WithIdentity(identity)
    //        .WithCronSchedule(cronExpression, x => x.WithMisfireHandlingInstructionFireAndProceed())
    //        .StartAt(DateTimeOffset.FromUnixTimeSeconds(lastRun))
    //        .Build();

    //    var task = new CronTask();
    //    task.JobDetail = detail;
    //    task.JobTrigger = trigger;

    //    return task;
    //}

//    private static async Task Main(string[] args)
//    {
//#if DEBUG

//        var obj = new XmlOps.Episode();
//        obj = null;
//        //obj.EpisodeNr = "1";

//        var boo = IsNullOrEmpty(obj);
//        Console.ReadKey();
//        Environment.Exit(0);
//#endif

//        if (Environment.UserInteractive) {
//            await StartupChecks();
//            //await RefreshSettings();
//            await Setup.StartPrompt.Execute();

//            //if (Settings.Default.FirstTimeSetup) {

//            //} else {

//            //}
//        } else {
//            await StartupChecks();
//            //await RefreshSettings();
//            await StartCronJobs();
//            //ServiceHandler.Setup.RunAsAService();
//        }

//        Console.ReadKey();


//        //Parser.Default.ParseArguments<Options>(args)
//        //           .WithParsed<Options>(o => {
//        //               if (o.Verbose) {
//        //                   Console.WriteLine($"Verbose output enabled. Current Arguments: -v {o.Verbose}");
//        //               }
//        //               else if (o.Param || SETTINGS.FirstTimeSetup) {
//        //                   Console.WriteLine("----------CURRENT PROGRAM PARAMETERS----------");
//        //                   Console.WriteLine("Source folder: ");
//        //                   Console.WriteLine("Input the source folder of the ");
//        //               } else {

//        //               }
//    }

    /// <summary>
    /// Clears the previous line in the console.
    /// </summary>
    public static void ClearPreviousLine()
    {
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write(new string(' ', Console.BufferWidth));
        Console.SetCursorPosition(0, Console.CursorTop - 1);
    }

    public static void WriteToEndOfPrevious(string input)
    {
        if (Console.CursorTop == 0) { return; }

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.WriteLine("\b" + input);
    }


    /// <summary>
    /// Deprecated function, but can be used to automatically match files to the corresponding season/episode.<br/>
    /// This function automatically finds, matches and imports all files from the specified directory and its subdirectories.
    /// </summary>
    /// <param name="path">Directory path to the contained unprocessed media files.</param>
    /*public static void FileParser(string path)
    {
        if (!Directory.Exists(path)) {
            LogWriter.Logger.Warn($"Source directory doesn't exist: '{path}'");
            return;
        }

        var folderName = new DirectoryInfo(path).Name;
        //var folderNameClean = CleanSourceName(folderName);

        //var regexRelease = "SkyF1HD|F1TV|1080p|720p";
        //var title = Regex.Match(folderNameClean, $@"\d+\s(\D+(\s\d)?)\s({regexRelease})", RegexOptions.IgnoreCase).Groups[1].Value;
        var season = Regex.Match(folderName, @"[x\s\.](\d{4})[x\s\.]").Groups[1].Value;
        var title = InputOutput.ReturnCleanTitle(folderName);
        title = RemoveNonOccurringWords(title, Convert.ToInt32(season));

        bool dirEmpty = !Directory.EnumerateFileSystemEntries(path).Any();
        string? matchingFile = InputOutput.FileMatch(path);

        if (dirEmpty) {
            LogWriter.Logger.Warn($"Source directory is empty: '{path}'");
            Environment.Exit(0);
        }
        if (matchingFile == null) {
            LogWriter.Logger.Warn($"No media files found in the source directory: '{path}'");
            Environment.Exit(0);
        }

        //var matchingSeason = XmlOps.Search(XmlOps.XmlType.Downloads)
        //                             .Where(file => file.SeasonNr == season);

        var metaXML = XDocument.Load(Globals.METADATA_XML_PATH);
        var matchingSeason = from ep in metaXML.Descendants("episode")
                             where (string)ep.Element("season") == season
                             select ep;

        if (matchingSeason.Count() == 0) {
            LogWriter.Logger.Error($"No season '{season}' entries found in metadata. Make sure metadata is up-to-date.");
            Environment.Exit(0);
        }

        XElement? closestMatchElement = null;
        int matchValuePlaceholder = 1000;
        int closestMatchValue = matchValuePlaceholder;

        foreach (var e in matchingSeason) {
            var t = (string)e.Element("title");
            var ld = LevenshteinDistance(title, t, true);

            if (ld < closestMatchValue) {
                closestMatchElement = e;
                closestMatchValue = ld;
            }
        }

        LogWriter.Logger.Info($"Closest match against '{new DirectoryInfo(path).Name}' => (LD{closestMatchValue}) " +
            $"S{(string)closestMatchElement.Element("season")}" +
            $"E{(string)closestMatchElement.Element("episode")}" +
            $": '{(string)closestMatchElement.Element("title")}'");

        if (closestMatchValue > 5) {
            LogWriter.Logger.Warn($"No high confidence title matches found, skipping file import.");
            Environment.Exit(0);
        }

        var regexRelease = "SkyF1HD|F1TV|1080p|720p";
        var fileEnd = Regex.Match(folderName, @$"({regexRelease}).*", RegexOptions.IgnoreCase).Value;
        var fileExt = Path.GetExtension(matchingFile);

        var destinationPath = InputOutput.GenerateOutputPath(
            (string)closestMatchElement.Element("title"),
            (string)closestMatchElement.Element("season"),
            (string)closestMatchElement.Element("episode"),
            fileEnd, fileExt);
        
        FileInfo fi = new FileInfo(matchingFile);
        if (fi.Exists) {
            fi.MoveTo(destinationPath);
            LogWriter.Logger.Info($"Successfully imported file '{matchingFile}' to '{destinationPath}'");
        }
    }*/

    /// <summary>Calculates the Levenshtein edit distance between two strings (how many alterations were required to make the two strings identical).</summary>
    /// <seealso cref="https://en.wikipedia.org/wiki/Levenshtein_distance"/>
    /// <param name="one">String to be compared.</param>
    /// <param name="two">String to be compared.</param>
    /// <param name="ignoreCase">(Optional) Comparisons are done case insensitive.</param>
    //https://www.dotnetperls.com/levenshtein
    public static int LevenshteinDistance(string one, string two, bool ignoreCase = false)
    {
        if (ignoreCase) {
            one = one.ToLower();
            two = two.ToLower();
        }

        int n = one.Length;
        int m = two.Length;
        int[,] d = new int[n + 1, m + 1];

        // Verify arguments.
        if (n == 0) {
            return m;
        }

        if (m == 0) {
            return n;
        }

        // Initialize arrays.
        for (int i = 0; i <= n; d[i, 0] = i++) {
        }

        for (int j = 0; j <= m; d[0, j] = j++) {
        }

        // Begin looping.
        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= m; j++) {
                // Compute cost.
                int cost = (two[j - 1] == one[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + cost);
            }
        }
        // Return cost.
        return d[n, m];
    }

    /// <summary>Returns the episode title with any words removed that don't occur throughout the season. Necessary for title validation.</summary>
    /// <param name="name">Episode title/name to be sanitized.</param>
    /// <param name="season">Episode season number.</param>
    public static string? RemoveNonOccurringWords(string name, int season)
    {
        var nameClean = Regex.Replace(name, @"[^a-zA-Z0-9 -]", "").ToLower();
        var metaXML = XDocument.Load(Globals.METADATA_XML_PATH);

        var seasonTitles = XmlOps.Search(XmlOps.XmlType.Metadata)
                                 .Where(x => x.SeasonNr == season.ToString())
                                 .Select(x => x.Title);
        
        //var seasonTitles = from ep in metaXML.Descendants("episode")
        //                     where (string)ep.Element("season") == season.ToString()
        //                     select ep.Element("title").Value;

        if (seasonTitles.Count() == 0) {
            LogWriter.Logger.Error($"No season '{season}' entries found in metadata. Make sure metadata is up-to-date.");
            return null;
        }

        var allTitleWords = new List<string>();
        foreach (var title in seasonTitles) {
            var titleClean = Regex.Replace(title, @"[^a-zA-Z0-9 -]", "").ToLower();
            allTitleWords.AddRange(titleClean.Split(' ').ToList());
        }

        allTitleWords = allTitleWords.Distinct().ToList();
        var result = String.Join(" ", nameClean.Split(' ').Where(w => allTitleWords.Contains(w)));
        return result;
    }

    /// <summary>
    /// Returns the hash value of two concetated strings:<br/>
    /// <c><paramref name="a"/>(length) + <paramref name="a"/> + <paramref name="separator"/> + <paramref name="b"/>(length) + <paramref name="b"/></c>
    /// </summary>
    /// <param name="a">String to concetate.</param>
    /// <param name="b">String to concetate.</param>
    /// <param name="separator">String separator.</param>
    /// <returns></returns>
    public static int CalculateConcatHash(string a, string b, string separator = "||")
    {
        a = a.Trim(); b = b.Trim();
        var concat = a.Length + a + separator + b.Length + b;
        return concat.GetHashCode();
    }

    /// <summary>
    /// Performs startup checks to ensure smooth operation.<br/>
    /// Checks for any missing files and generates new ones where needed.
    /// </summary>
    /// <returns></returns>
    internal static async Task StartupChecks()
    {
        LogWriter.Logger.Info("Performing startup checks...");

        if (Settings.Default == null) {
            LogWriter.Logger.Fatal("Failed to load configuration file. Exiting program...");
            Environment.Exit(13);
        }

        if (Globals.EXE_PATH == null) {
            LogWriter.Logger.Fatal("Failed to retrieve path to executable. Exiting program...");
            Environment.Exit(13);
        }

        if (!File.Exists(Globals.METADATA_XML_PATH)) {
            LogWriter.Logger.Info("Metadata file doesn't exist. Updating metadata...");
            await XmlOps.UpdateMetadata(true);
        }

        if (!File.Exists(Globals.TORRENTS_XML_PATH)) {
            LogWriter.Logger.Warn("Torrent queue file doesn't exist. Creating file...");
            var downloads = new XElement("episodes");
            downloads.Save(Globals.TORRENTS_XML_PATH);
        }

        if (!File.Exists(Globals.MONITORED_PATH)) {
            LogWriter.Logger.Warn("Monitored seasons file doesn't exist. Creating file...");
            var downloads = new XElement("seasons");
            downloads.Save(Globals.MONITORED_PATH);
        }
    }

    
}
