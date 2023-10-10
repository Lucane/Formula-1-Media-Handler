using Quartz;
using System.ServiceProcess;
using Formula_1_Media_Handler;

public class ServiceHandler
{
    [DisallowConcurrentExecution]
    public class TorrentCheck : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await ScheduledTasks.TorrentCheck();
        }
    }

    [DisallowConcurrentExecution]
    public class MonitoredCheck : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var nextFire = Generic.ConvertFromDateTimeOffset(context.NextFireTimeUtc.GetValueOrDefault());
            LogWriter.Logger.Trace($"Executing cron job for monitored seasons. Next job scheduled at '{nextFire}'");
            await ScheduledTasks.MonitoredCheck();
        }
    }

    [DisallowConcurrentExecution]
    public class MetadataUpdate : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var nextFire = Generic.ConvertFromDateTimeOffset(context.NextFireTimeUtc.GetValueOrDefault());
            LogWriter.Logger.Trace($"Executing cron job for updating metadata. Next job scheduled at '{nextFire}'");
            await XmlOps.UpdateMetadata(true);
        }
    }

    public class Setup
    {
        public static void RunAsAService()
        {
            ServiceBase[] servicesToRun = new ServiceBase[] {
                new Formula_1_Media_Handler.Service()
            };

            ServiceBase.Run(servicesToRun);
        }
    }
}
