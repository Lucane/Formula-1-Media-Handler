using Formula_1_Media_Handler.Properties;
using Sharprompt;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Quartz;
using System.ServiceProcess;
using CronExpressionDescriptor;
using System.ComponentModel;
using Formula_1_Media_Handler;

public class Setup
{
    public class StartPrompt
    {
        private const string EditMonitoredSeasons = "▪ Edit monitored seasons";
        private const string EditTaskSchedules = "▪ Change task schedules";
        private const string EditMediaPath = "▪ Edit media directory path";
        private const string ForceMetadataUpdate = "▪ Force metadata update";
        private const string ForceMonitoredSearch = "▪ Force search for monitored seasons";
        private const string StartService = "▪ Start the Windows service";
        private const string StopService = "▪ Stop the Windows service";
        //private const string InstallService = "▪ Install service (requires admin)";
        //private const string UninstallService = "▪ Uninstall service (requires admin)";
        //private const string ExitSetup = "◂ EXIT";

        private static List<string> GetConstants()
        {
            var type = typeof(StartPrompt);
            var fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).Select(fi => fi.GetValue(null).ToString()).ToList();
        }

        public static async Task Execute()
        {
            Console.Title = "Formula 1 Media Handler";
            Console.OutputEncoding = Encoding.UTF8;

            // [REVIEW] only required for the integrated service selfinstaller, but it isn't functional at the moment
            //bool isElevated;
            //using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
            //    WindowsPrincipal principal = new WindowsPrincipal(identity);
            //    isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            //}

            while (true) {
                Prompt.Symbols.Selector = new Symbol("►", "►");
                Prompt.Symbols.NotSelect = new Symbol("○", "○");
                Prompt.Symbols.Selected = new Symbol("●", "●");
                Prompt.Symbols.Done = new Symbol("", "");
                Prompt.Symbols.Prompt = new Symbol("", "");
                Prompt.ColorSchema.Select = ConsoleColor.Cyan;


                var emptyCronJob = " ... no cron job exists for this task!";
                var torrentCheckReadable = ExpressionDescriptor.GetDescription(Settings.Default.Cron_CheckTorrents_Job).ToLower();
                var torrentLastRan = Settings.Default.Cron_CheckTorrents_LastRun == 0 ? "" :
                    $"(last ran at {DateTimeOffset.FromUnixTimeSeconds(Settings.Default.Cron_CheckTorrents_LastRun).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")})";

                var monitoredCheckReadable = ExpressionDescriptor.GetDescription(Settings.Default.Cron_CheckMonitored_Job).ToLower();
                var monitoredLastRan = Settings.Default.Cron_CheckMonitored_LastRun == 0 ? "" :
                    $"(last ran at {DateTimeOffset.FromUnixTimeSeconds(Settings.Default.Cron_CheckMonitored_LastRun).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")})";

                var metadataUpdateReadable = ExpressionDescriptor.GetDescription(Settings.Default.Cron_MetadataUpdate_Job).ToLower();
                var metadataLastRan = Settings.Default.Cron_MetadataUpdate_LastRun == 0 ? "" :
                    $"(last ran at {DateTimeOffset.FromUnixTimeSeconds(Settings.Default.Cron_MetadataUpdate_LastRun).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")})";


                var serviceStatus = new ServiceController("f1svc").Status;
                var torrentClientVersion = await TorrentClient.Connection.Instance.Client.GetQBittorrentVersionAsync();

                Console.WriteLine("\n────────────────────────────────────────────────────────────────────────────────────────────\n");
                Console.WriteLine($"Check for completed torrents {torrentCheckReadable ?? emptyCronJob} {torrentLastRan}");
                Console.WriteLine($"Check for monitored seasons {monitoredCheckReadable ?? emptyCronJob} {monitoredLastRan}");
                Console.WriteLine($"Update the metadata {metadataUpdateReadable ?? emptyCronJob} {metadataLastRan}");
                Console.WriteLine();
                Console.WriteLine($"TVDB URL is: {Settings.Default.TvdbURL}");
                Console.WriteLine($"Media directory is: {Settings.Default.Destination}");
                Console.WriteLine($"qBittorrent Web UI address is: {Settings.Default.QBWebURI}");
                Console.WriteLine();
                Console.WriteLine($"The service is: {serviceStatus}");
                Console.WriteLine($"Connection to qBittorrent was successful: v{torrentClientVersion}");
                Console.WriteLine("\n────────────────────────────────────────────────────────────────────────────────────────────\n");


                var subSelection = Prompt.Select("♦ SETTINGS", GetConstants());
                Console.Clear();

                switch (subSelection)
                {
                    case EditMonitoredSeasons:
                        Actions.MonitoredSeasons();
                        break;

                    case EditTaskSchedules:
                        Actions.CronSchedule();
                        break;

                    case EditMediaPath:
                        Actions.MediaPath();
                        break;

                    case ForceMetadataUpdate:
                        {
                            var confirm = Prompt.Confirm("Do you wish to continue?");
                            Console.Clear();

                            if (confirm)
                            {
                                await XmlOps.UpdateMetadata(forceUpdate: true);
                                LogWriter.Logger.Info($"Successfully updated metadata...");
                            }
                        }
                        break;

                    case ForceMonitoredSearch:
                        {
                            var confirm = Prompt.Confirm("Do you wish to continue?");
                            Console.Clear();
                            if (confirm) await ScheduledTasks.MonitoredCheck();
                        }
                        break;

                    case StartService:
                        {
                            var confirm = Prompt.Confirm("Do you wish to continue?");
                            Console.Clear();
                            if (confirm)
                            {
                                Actions.RunNssm("start f1svc");


                                //ProcessStartInfo startInfo = new ProcessStartInfo();
                                //startInfo.FileName = @$"{Globals.EXE_PATH}nssm.exe";
                                //startInfo.Arguments = @"start f1svc";
                                //Process.Start(startInfo);


                                //Process cmd = new Process();

                                //cmd.StartInfo.FileName = "cmd.exe";
                                //cmd.StartInfo.RedirectStandardInput = true;
                                //cmd.StartInfo.RedirectStandardOutput = true;
                                //cmd.StartInfo.CreateNoWindow = true;
                                //cmd.StartInfo.UseShellExecute = false;

                                //cmd.Start();

                                //cmd.StandardInput.WriteLine(@$"{Globals.EXE_PATH}nssm.exe start f1svc");
                                //cmd.StandardInput.Flush();
                                //cmd.StandardInput.Close();
                                //Console.WriteLine(cmd.StandardOutput.ReadToEnd());


                                //string strCmdText;
                                //strCmdText = @$"{Globals.EXE_PATH}\nssm.exe /C copy /b Image1.jpg + Archive.rar Image2.jpg";
                                //Process.Start("CMD.exe", strCmdText);

                                //ProcessStartInfo startInfo = new ProcessStartInfo();
                                //startInfo.FileName = @$"{Globals.EXE_PATH}\nssm.exe";
                                //startInfo.Arguments = @"C:\etc\desktop\file.spp C:\etc\desktop\file.txt";
                                //Process.Start(startInfo);

                                ServiceController serviceController = new ServiceController("f1svc");
                                if (serviceController.Status == ServiceControllerStatus.Running) LogWriter.Logger.Info("The service was started manually.");
                                else LogWriter.Logger.Error($"Failed to manually start the service! (status '{serviceController.Status}')");
                            }
                        }

                        break;

                    case StopService:
                        {
                            var confirm = Prompt.Confirm("Do you wish to continue?");
                            Console.Clear();
                            if (confirm)
                            {
                                Actions.RunNssm("stop f1svc");

                                //Process cmd = new Process();

                                //cmd.StartInfo.FileName = "cmd.exe";
                                //cmd.StartInfo.RedirectStandardInput = true;
                                //cmd.StartInfo.RedirectStandardOutput = true;
                                //cmd.StartInfo.CreateNoWindow = true;
                                //cmd.StartInfo.UseShellExecute = false;

                                //cmd.Start();

                                //cmd.StandardInput.WriteLine(@$"{Globals.EXE_PATH}nssm.exe stop f1svc");
                                //cmd.StandardInput.Flush();
                                //cmd.StandardInput.Close();
                                //Console.WriteLine(cmd.StandardOutput.ReadToEnd());


                                ServiceController serviceController = new ServiceController("f1svc");
                                //TimeSpan timeout = TimeSpan.FromMilliseconds(10000);
                                //serviceController.Stop();
                                //serviceController.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                                if (serviceController.Status == ServiceControllerStatus.Stopped) LogWriter.Logger.Info("The service was halted manually.");
                                else LogWriter.Logger.Error($"Failed to manually halt the service! (status '{serviceController.Status}')");
                            }
                        }

                        break;

                        // [REVIEW] only required for the integrated service selfinstaller, but it isn't functional at the moment
                        //case InstallService: {
                        //        if (!isElevated) {
                        //            LogWriter.Write($"Unable to perform this action without administrator privileges!", LogWriter.Type.INFO);
                        //            break;
                        //        }

                        //        var confirm = Prompt.Confirm("Do you wish to install the program as a service?");
                        //        Console.Clear();

                        //        if (confirm) {
                        //            Formula_1_Media_Handler.SelfInstaller.Install();
                        //        }

                        //    }
                        //    break;

                        //case UninstallService: {
                        //        if (!isElevated) {
                        //            LogWriter.Write($"Unable to perform this action without administrator privileges!", LogWriter.Type.INFO);
                        //            break;
                        //        }

                        //        var confirm = Prompt.Confirm("Do you wish to uninstall the program service?");
                        //        Console.Clear();

                        //        if (confirm) {
                        //            Formula_1_Media_Handler.SelfInstaller.Uninstall();
                        //        }
                        //    }
                        //    break;
                }

                Prompt.Symbols.Selector = new Symbol("", "");
                Prompt.Symbols.NotSelect = new Symbol("", "");
                Prompt.Symbols.Selected = new Symbol("", "");
                Prompt.Symbols.Done = new Symbol("", "");
                Prompt.Symbols.Prompt = new Symbol("", "");
            }
        }
    }

    public class FirstTimeSetup
    {

    }

    private class Actions
    {
        public static void RunNssm (string command)
        {
            const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @$"{Globals.EXE_PATH}\nssm.exe";
            startInfo.Arguments = command;
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            startInfo.Verb = "runas";

            try
            {
                var nssm = Process.Start(startInfo);
                nssm?.WaitForExit();
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == ERROR_CANCELLED)
                    Console.WriteLine("Admin access was denied. Please run this command as an admin to execute NSSM.");
                else
                    throw;
            }
        }

        public static void MonitoredSeasons()
        {
            Console.Clear();

            //var metaXML = XDocument.Load(Globals.METADATA_XML_PATH);
            //var seasons = from ep in metaXML.Descendants("episode")
            //              select (string)ep.Element("season");

            var monXML = XDocument.Load(Globals.MONITORED_PATH);
            var monSeasons = from ep in monXML.Descendants("season")
                             select (string)ep;

            var allSeasons = XmlOps.Search(XmlOps.XmlType.Metadata)
                                   .Select(x => x.SeasonNr)
                                   .Where(x => !String.IsNullOrWhiteSpace(x))
                                   .Distinct()
                                   .Reverse();

            //allSeasons = allSeasons.Where(s => !String.IsNullOrWhiteSpace(s)).Distinct().Reverse();
            monSeasons = monSeasons.Where(s => !String.IsNullOrWhiteSpace(s)).Distinct();

            var monitored = Prompt.MultiSelect("Selected seasons", allSeasons, pageSize: 20, defaultValues: monSeasons);

            var saveConfirm = Prompt.Confirm("Do you wish to save these changes?");
            Console.Clear();

            if (saveConfirm) {
                var monitoredXML = new XDocument(new XElement("seasons"));
                foreach (var m in monitored) {
                    monitoredXML.Root!.Add(new XElement("season", m));
                }

                monitoredXML.Save(Globals.MONITORED_PATH);
                LogWriter.Logger.Info($"Successfully updated monitored seasons.");
            }
        }

        public static void CronSchedule()
        {
            Console.Clear();

            Prompt.Symbols.Prompt = new Symbol("?", "?");
            Prompt.Symbols.Done = new Symbol("√", "√");

            //var formatReadable = "{0:D2}d {1:D2}h {2:D2}m";
            //var formatClean = "{0:D2}.{1:D2}:{2:D2}";
            //var formatRegExReadable = @"00(d|h|m)\s?";

            var emptyCronJob = "No cron job exists for this task!";
            var torrentCheckReadable = ExpressionDescriptor.GetDescription(Settings.Default.Cron_CheckTorrents_Job).ToLower();
            var monitoredCheckReadable = ExpressionDescriptor.GetDescription(Settings.Default.Cron_CheckMonitored_Job).ToLower();
            var metadataUpdateReadable = ExpressionDescriptor.GetDescription(Settings.Default.Cron_MetadataUpdate_Job).ToLower();

            //TimeSpan ts = TimeSpan.FromMilliseconds(Settings.Default.TimerCheckTorrent);
            //var torrentCheckReadable = Regex.Replace(String.Format(formatReadable, ts.Days, ts.Hours, ts.Minutes), formatRegExReadable, "");
            //var torrentCheckClean = String.Format(formatClean, ts.Days, ts.Hours, ts.Minutes);

            //ts = TimeSpan.FromMilliseconds(Settings.Default.TimerCheckMonitored);
            //var monitoredCheckReadable = Regex.Replace(String.Format(formatReadable, ts.Days, ts.Hours, ts.Minutes), formatRegExReadable, "");
            //var monitoredCheckClean = String.Format(formatClean, ts.Days, ts.Hours, ts.Minutes);

            //ts = TimeSpan.FromMilliseconds(Settings.Default.UpdateInterval);
            //var metadataUpdateReadable = Regex.Replace(String.Format(formatReadable, ts.Days, ts.Hours, ts.Minutes), formatRegExReadable, "");
            //var metadataUpdateClean = String.Format(formatClean, ts.Days, ts.Hours, ts.Minutes);

            Console.WriteLine("\n────────────────────────── CURRENT TASK SCHEDULES ──────────────────────────\n");
            Console.WriteLine($"Check for completed torrents {torrentCheckReadable ?? emptyCronJob} ({Settings.Default.Cron_CheckTorrents_Job})");
            Console.WriteLine($"Check for monitored seasons {monitoredCheckReadable ?? emptyCronJob} ({Settings.Default.Cron_CheckMonitored_Job})");
            Console.WriteLine($"Update the metadata {metadataUpdateReadable ?? emptyCronJob} ({Settings.Default.Cron_MetadataUpdate_Job})");
            Console.WriteLine("\n────────────────────────────────────────────────────────────────────────────\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Tasks are scheduled with cron jobs: https://www.cronmaker.com/\n" +
                              "Please use the linked website to generate a valid cron expression.\n" +
                              "Leave the input field blank to preserve existing values.\n");
            Console.ResetColor();


            string? torrent = null;
            do {
                if (torrent != null) {
                    //Program.ClearPreviousLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("» Please input a valid cron expression!");
                    Console.ResetColor();
                }

                torrent = Prompt.Input<string?>("New cron schedule for completed torrents", defaultValue: Settings.Default.Cron_CheckTorrents_Job);
            } while (!CronExpression.IsValidExpression(torrent));

            string? monitored = null;
            do {
                if (monitored != null) {
                    //Program.ClearPreviousLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("» Please input a valid cron expression!");
                    Console.ResetColor();
                }

                monitored = Prompt.Input<string?>("New cron schedule for monitored seasons", defaultValue: Settings.Default.Cron_CheckMonitored_Job);
            } while (!CronExpression.IsValidExpression(monitored));

            string? metadata = null;
            do {
                if (metadata != null) {
                    //Program.ClearPreviousLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("» Please input a valid cron expression!");
                    Console.ResetColor();
                }

                metadata = Prompt.Input<string?>("New cron schedule for metadata updates", defaultValue: Settings.Default.Cron_MetadataUpdate_Job);
            } while (!CronExpression.IsValidExpression(metadata));


            torrentCheckReadable = ExpressionDescriptor.GetDescription(torrent).ToLower();
            monitoredCheckReadable = ExpressionDescriptor.GetDescription(monitored).ToLower();
            metadataUpdateReadable = ExpressionDescriptor.GetDescription(metadata).ToLower();

            Console.WriteLine("\n────────────────────────── NEW TASK SCHEDULES ───────────────────────────\n");
            Console.WriteLine($"Check for completed torrents {torrentCheckReadable ?? emptyCronJob} ({torrent})");
            Console.WriteLine($"Check for monitored seasons {monitoredCheckReadable ?? emptyCronJob} ({monitored})");
            Console.WriteLine($"Update the metadata {metadataUpdateReadable ?? emptyCronJob} ({metadata})");
            Console.WriteLine("\n─────────────────────────────────────────────────────────────────────────\n");

            var saveConfirm = Prompt.Confirm("Do you wish to save these changes?");
            Console.Clear();

            if (saveConfirm) {
                Settings.Default.Cron_CheckTorrents_Job = torrent;
                Settings.Default.Cron_CheckMonitored_Job = monitored;
                Settings.Default.Cron_MetadataUpdate_Job = metadata;
                Settings.Default.Save();
                LogWriter.Logger.Info($"Successfully updated task schedules.");
            }
        }

        public static void TaskSchedule()
        {
            Prompt.Symbols.Prompt = new Symbol("?", "?");
            Prompt.Symbols.Done = new Symbol("√", "√");

            var formatReadable = "{0:D2}d {1:D2}h {2:D2}m";
            var formatClean = "{0:D2}.{1:D2}:{2:D2}";
            var formatRegExReadable = @"00(d|h|m)\s?";

            TimeSpan ts = TimeSpan.FromMilliseconds(Settings.Default.TimerCheckTorrent);
            var torrentCheckReadable = Regex.Replace(String.Format(formatReadable, ts.Days, ts.Hours, ts.Minutes), formatRegExReadable, "");
            var torrentCheckClean = String.Format(formatClean, ts.Days, ts.Hours, ts.Minutes);

            ts = TimeSpan.FromMilliseconds(Settings.Default.TimerCheckMonitored);
            var monitoredCheckReadable = Regex.Replace(String.Format(formatReadable, ts.Days, ts.Hours, ts.Minutes), formatRegExReadable, "");
            var monitoredCheckClean = String.Format(formatClean, ts.Days, ts.Hours, ts.Minutes);

            ts = TimeSpan.FromMilliseconds(Settings.Default.UpdateInterval);
            var metadataUpdateReadable = Regex.Replace(String.Format(formatReadable, ts.Days, ts.Hours, ts.Minutes), formatRegExReadable, "");
            var metadataUpdateClean = String.Format(formatClean, ts.Days, ts.Hours, ts.Minutes);

            Console.WriteLine("\n───────────────────── CURRENT TASK SCHEDULES ─────────────────────\n");
            Console.WriteLine("Check for completed torrents every: " + torrentCheckReadable);
            Console.WriteLine("Check for monitored seasons every: " + monitoredCheckReadable);
            Console.WriteLine("Update the metadata every: " + metadataUpdateReadable);
            Console.WriteLine("\n──────────────────────────────────────────────────────────────────\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Use the format 'DD.HH:MM' when inputting new values (days/'DD' is optional)." +
                                "\nLeave the input field blank to preserve existing values.\n");
            Console.ResetColor();

            var vals = new[] { Validators.RegularExpression(@"^(\d+\.)?\d+\:\d+$", "Please input a valid value in the format 'DD.HH:MM'") };

            var torrent = TimeSpan.Parse(
                Prompt.Input<string>(
                    "New check interval for completed torrents",
                    defaultValue: torrentCheckClean,
                    validators: vals
                    )
                );

            var monitored = TimeSpan.Parse(
                Prompt.Input<string>(
                    "New check interval for monitored seasons",
                    defaultValue: monitoredCheckClean,
                    validators: vals
                    )
                );

            var metadata = TimeSpan.Parse(
                Prompt.Input<string>(
                    "New update interval for metadata",
                    defaultValue: metadataUpdateClean,
                    validators: vals
                    )
                );

            torrentCheckReadable = Regex.Replace(String.Format(formatReadable, torrent.Days, torrent.Hours, torrent.Minutes), formatRegExReadable, "");
            monitoredCheckReadable = Regex.Replace(String.Format(formatReadable, monitored.Days, monitored.Hours, monitored.Minutes), formatRegExReadable, "");
            metadataUpdateReadable = Regex.Replace(String.Format(formatReadable, metadata.Days, metadata.Hours, metadata.Minutes), formatRegExReadable, "");

            Console.WriteLine("\n───────────────────── NEW TASK SCHEDULES ─────────────────────\n");
            Console.WriteLine("Check for completed torrents every: " + torrentCheckReadable);
            Console.WriteLine("Check for monitored seasons every: " + monitoredCheckReadable);
            Console.WriteLine("Update the metadata every: " + metadataUpdateReadable);
            Console.WriteLine("\n──────────────────────────────────────────────────────────────\n");

            var saveConfirm = Prompt.Confirm("Do you wish to save these changes?");
            Console.Clear();

            if (saveConfirm) {
                Settings.Default.TimerCheckTorrent = torrent.TotalMilliseconds;
                Settings.Default.TimerCheckMonitored = monitored.TotalMilliseconds;
                Settings.Default.UpdateInterval = metadata.TotalMilliseconds;
                Settings.Default.Save();
                LogWriter.Logger.Info($"Successfully updated task schedules.");
            }
        }

        public static void MediaPath()
        {
            Console.Clear();

            Prompt.Symbols.Prompt = new Symbol("?", "?");
            Prompt.Symbols.Done = new Symbol("√", "√");

            Console.WriteLine("\n───────────────────── CURRENT MEDIA DIRECTORY ─────────────────────\n");
            Console.WriteLine(Settings.Default.Destination);
            Console.WriteLine("\n───────────────────────────────────────────────────────────────────\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Here you can edit the root directory of your 'Formula 1' media files\n" +
                            "Please note that this directory should contain every season and episode.\n" +
                            @"For example: 'E:\Media\TV Shows\Formula 1'" + "\n");
            Console.ResetColor();

            string? directory = null;
            do {
                if (directory != null) {
                    //Program.ClearPreviousLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("» Please input a valid directory path!");
                    Console.ResetColor();
                }

                directory = Prompt.Input<string?>("New directory path", defaultValue: Settings.Default.Destination);

            } while (!Directory.Exists(directory));

            var confirm = Prompt.Confirm("Do you wish to save this change?");
            Console.Clear();

            if (confirm) {
                Settings.Default.Destination = Path.GetFullPath(directory);
                Settings.Default.Save();
                LogWriter.Logger.Info($"Successfully updated local media directory path.");
            }
        }
    }
}
