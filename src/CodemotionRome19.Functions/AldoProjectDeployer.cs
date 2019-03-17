using System;
using System.Threading.Tasks;
using CodemotionRome19.Core.AzureDevOps;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Models;
using CodemotionRome19.Core.Notification;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CodemotionRome19.Functions
{
    public class AldoProjectDeployer
    {
        readonly IConfiguration configuration;
        readonly INotificationService notificationService;
        readonly IAzureDevOpsService azureDevOpsService;

        public AldoProjectDeployer(IConfiguration configuration, INotificationService notificationService, IAzureDevOpsService azureDevOpsService)
        {
            this.configuration = configuration;
            this.notificationService = notificationService;
            this.azureDevOpsService = azureDevOpsService;
        }

        [FunctionName("AldoProjectDeployer")]
        public async Task Run([QueueTrigger("project-deploy", Connection = "AzureWebJobsStorage")]ProjectToDeploy pd, 
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {pd.ProjectName}");

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            //var tableName = Environment.GetEnvironmentVariable("deployLog", EnvironmentVariableTarget.Process);
            var serilog = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(connectionString, storageTableName: "AldoProjectDeployerLog")
                .CreateLogger();

            try
            {
                var releaseTrigger = await azureDevOpsService.TriggerRelease(pd);

                var notificationMessage = releaseTrigger.IsSuccess ? 
                    $"Aldo. Sto deployando il progetto <break strength=\"strong\"/> {pd.ProjectName}. Puoi seguirne lo stato sul portale Azure DevOps." 
                    : $"Aldo. Errore durante il deploy del progetto <break strength=\"strong\"/> {pd.ProjectName}. Verifica sul portale Azure DevOps.";

                var notificationResult = await notificationService.SendUserNotification(pd.RequestedByUser, notificationMessage);

                if (notificationResult.IsFailure)
                    serilog.Error(notificationResult.Error);                    
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.LogError(error);
                serilog.Error(error);
            }
        }
    }
}
