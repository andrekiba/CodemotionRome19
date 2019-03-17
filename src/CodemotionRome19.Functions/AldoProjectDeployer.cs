using System;
using System.Threading.Tasks;
using CodemotionRome19.Core.AzureDevOps;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Models;
using CodemotionRome19.Core.Notification;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CodemotionRome19.Functions
{
    public class AldoProjectDeployer
    {
        #region Fields

        const string BreakStrong = "<break strength=\"strong\"/>";

        readonly IConfiguration configuration;
        readonly INotificationService notificationService;
        readonly IAzureDevOpsService azureDevOpsService;

        #endregion 

        public AldoProjectDeployer(IConfiguration configuration, INotificationService notificationService, IAzureDevOpsService azureDevOpsService)
        {
            this.configuration = configuration;
            this.notificationService = notificationService;
            this.azureDevOpsService = azureDevOpsService;
        }

        [FunctionName("AldoProjectDeployer")]
        public async Task Run([QueueTrigger("project-deploy", Connection = "AzureWebJobsStorage")]ProjectToDeploy pd)
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            var log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(connectionString, storageTableName: "AldoProjectDeployerLog")
                .CreateLogger();

            try
            {
                var releaseTrigger = await azureDevOpsService.TriggerRelease(pd);

                if (!pd.FromNewResource)
                {
                    var notificationMessage = releaseTrigger.IsSuccess ?
                        $"Aldo. Sto deployando il progetto {BreakStrong} {pd.ProjectName}. Puoi seguirne lo stato sul portale Azure DevOps."
                        : $"Aldo. C'è stato une errore durante il deploy del progetto {BreakStrong} {pd.ProjectName}.";

                    var notificationResult = await notificationService.SendUserNotification(pd.RequestedByUser, notificationMessage);

                    if (notificationResult.IsFailure)
                        log.Error(notificationResult.Error);
                }                    
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }
    }
}
