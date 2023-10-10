using System.ServiceProcess;
using System.Configuration.Install;
using System.Timers;
using System.Reflection;
using System.ComponentModel;
using Formula_1_Media_Handler.Properties;
using Quartz;
using Quartz.Impl;

namespace Formula_1_Media_Handler;

partial class Service : ServiceBase
{
    private static System.Timers.Timer StartupTimer;
    //private static System.Timers.Timer MonitoredCheckTimer;
    //private static System.Timers.Timer MetadataUpdateTimer;

    public Service()
    {
        InitializeComponent();
    }

    protected override void OnStart(string[] args)
    {
        //Thread.Sleep(10000);
        //System.Diagnostics.Debugger.Launch();
        //LogWriter.Write("SERVICE CHECK3", LogWriter.Type.INFO);
        StartupTimer = new System.Timers.Timer(10000);
        StartupTimer.Elapsed += new ElapsedEventHandler(Startup);
        StartupTimer.Enabled = true;

        //Thread t = new Thread(new ThreadStart(this.InitTimer));
        //t.Start();
    }

    protected override async void OnStop()
    {
        //StartupTimer.Stop();
        Settings.Default.Save();
        StdSchedulerFactory factory = new StdSchedulerFactory();
        IScheduler scheduler = await factory.GetScheduler();
        await scheduler.Shutdown();

        // TODO: Add code here to perform any tear-down necessary to stop your service.
        //TorrentCheckTimer.Stop();
        //MonitoredCheckTimer.Stop();
        //MetadataUpdateTimer.Stop();
    }

    public static async void Startup(object source, ElapsedEventArgs e)
    {
        StartupTimer.Stop();
        StartupTimer.Enabled = false;

        try {
            LogWriter.Write("Starting cron jobs...", LogWriter.Type.INFO);

            await Generic.StartupChecks();

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
            LogWriter.Write("Failed to start the service. Shutting down...", LogWriter.Type.FATAL, ex, true);
        }
    }

    private void InitTimer()
    {
        StartupTimer = new System.Timers.Timer();
        //wire up the timer event 
        StartupTimer.Elapsed += new ElapsedEventHandler(Startup);
        //set timer interval   
        //var timeInSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimerIntervalInSeconds"]);
        StartupTimer.Interval = (10000);
        // timer.Interval is in milliseconds, so times above by 1000 
        StartupTimer.Enabled = true;
    }

    //protected override void OnStart(string[] args)
    //{
    //    while (true) {

    //    }
    //    // TODO: Add code here to start your service.
    //    TorrentCheckTimer = new System.Timers.Timer(Settings.Default.TimerCheckTorrent);
    //    TorrentCheckTimer.Elapsed += new ElapsedEventHandler(TimerActions.TorrentCheck);
    //    TorrentCheckTimer.Enabled = true;

    //    MonitoredCheckTimer = new System.Timers.Timer(Settings.Default.TimerCheckMonitored);
    //    MonitoredCheckTimer.Elapsed += new ElapsedEventHandler(TimerActions.MonitoredCheck);
    //    MonitoredCheckTimer.Enabled = true;

    //    MetadataUpdateTimer = new System.Timers.Timer(Settings.Default.UpdateInterval);
    //    MetadataUpdateTimer.Elapsed += new ElapsedEventHandler(TimerActions.MetadataUpdate);
    //    MetadataUpdateTimer.Enabled = true;
    //}

    //protected override void OnStop()
    //{
    //    // TODO: Add code here to perform any tear-down necessary to stop your service.
    //    TorrentCheckTimer.Stop();
    //    MonitoredCheckTimer.Stop();
    //    MetadataUpdateTimer.Stop();
    //}
}



//public class TimerActions
//{
//    [DisallowConcurrentExecution]
//    public class CheckTorrents : IJob
//    {
//        public async Task Execute(IJobExecutionContext context)
//        {
//            await Console.Out.WriteLineAsync("Greetings from HelloJob!");
//            Settings.Default.Cron_CheckTorrents_LastRun = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            Settings.Default.Save();
//        }
//    }

//    [DisallowConcurrentExecution]
//    public class CheckMonitored : IJob
//    {
//        public async Task Execute(IJobExecutionContext context)
//        {
//            await Console.Out.WriteLineAsync("Greetings from HelloJob!");
//            Settings.Default.Cron_CheckMonitored_LastRun = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            Settings.Default.Save();
//        }
//    }

//    [DisallowConcurrentExecution]
//    public class MetadataUpdate : IJob
//    {
//        public async Task Execute(IJobExecutionContext context)
//        {
//            await Console.Out.WriteLineAsync("Greetings from HelloJob!");
//            Settings.Default.Cron_MetadataUpdate_LastRun = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            Settings.Default.Save();
//        }
//    }

//    [DisallowConcurrentExecution]
//    public class RefreshTimers : IJob
//    {
//        public async Task Execute(IJobExecutionContext context)
//        {
//            await Console.Out.WriteLineAsync("Greetings from HelloJob!");
//        }
//    }

//    //public static void TorrentCheck(object source, ElapsedEventArgs e)
//    //{
//    //    ServiceHandler service = new ServiceHandler();
//    //    service.TorrentCheck();
//    //}

//    //public static void MonitoredCheck(object source, ElapsedEventArgs e)
//    //{
//    //    ServiceHandler service = new ServiceHandler();
//    //    service.MonitoredCheck();
//    //}

//    //public static void MetadataUpdate(object source, ElapsedEventArgs e)
//    //{
//    //    ServiceHandler service = new ServiceHandler();
//    //    service.MonitoredCheck();
//    //}
//}

[RunInstaller(true)]
public class HybridSvxServiceInstaller : Installer
{
    public HybridSvxServiceInstaller()
    {
        ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
        ServiceInstaller serviceInstaller = new ServiceInstaller();

        // Setup the Service Account type per your requirement
        serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
        //serviceProcessInstaller.Username = null;
        //serviceProcessInstaller.Password = null;

        serviceInstaller.ServiceName = "f1svc";
        serviceInstaller.DisplayName = "Formula 1 Media Handler Service";
        serviceInstaller.StartType = ServiceStartMode.Automatic;
        serviceInstaller.Description = "Formula 1 Media Handler Service";

        this.Installers.Add(serviceProcessInstaller);
        this.Installers.Add(serviceInstaller);
    }

}

public class SelfInstaller
{
    //private static readonly string _exePath = Assembly.GetExecutingAssembly().Location;
    public static void Install()
    {
        //bool result;
        try {
            ManagedInstallerClass.InstallHelper(new string[] {
                Assembly.GetExecutingAssembly().Location
            });

            //ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });

            //var s = new ServiceInstaller {
            //    Context = new InstallContext(),
            //    ServiceName = "f1svc"
            //};
            //s.Install(null);

            //ServiceInstaller serviceObj = new ServiceInstaller();
            //InstallContext context = new InstallContext($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\serviceUninstaller.log", null);
            //serviceObj.Context = context;
            //serviceObj.ServiceName = "f1svc";
            //serviceObj.Uninstall(null);

            LogWriter.Write($"Successfully installed the program as a service.", LogWriter.Type.INFO);
        } catch (Exception ex) {

            LogWriter.Write($"Failed to install the program as a service!", LogWriter.Type.WARNING, ex);
            //result = false;
            //return result;
        }
        //result = true;
        //return result;
    }

    public static void Uninstall()
    {
        try {
            ServiceInstaller serviceObj = new ServiceInstaller();
            InstallContext context = new InstallContext($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location + @"\serviceUninstaller.log")}", null);
            serviceObj.Context = context;
            serviceObj.ServiceName = "f1svc";
            serviceObj.Uninstall(null);
            //ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
            //ManagedInstallerClass.InstallHelper(new string[] {
            //    "/u", SelfInstaller._exePath
            //});

            LogWriter.Write($"Successfully uninstalled the service.", LogWriter.Type.INFO);
        } catch (Exception ex) {
            LogWriter.Write($"Failed to uninstall the service!", LogWriter.Type.WARNING, ex);
        }
    }

}
