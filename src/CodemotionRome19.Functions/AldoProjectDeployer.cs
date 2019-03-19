using System;
using System.Threading.Tasks;
using CodemotionRome19.Core.AzureDevOps;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Models;
using CodemotionRome19.Core.Notification;
using CodemotionRome19.Functions.Extensions;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CodemotionRome19.Functions
{
    public class AldoProjectDeployer
    {
        #region Fields

        readonly IConfiguration configuration;
        readonly INotificationService notificationService;
        readonly IAzureDevOpsService azureDevOpsService;
        readonly ILogger log;

        #endregion 

        public AldoProjectDeployer(IConfiguration configuration, INotificationService notificationService, IAzureDevOpsService azureDevOpsService)
        {
            this.configuration = configuration;
            this.notificationService = notificationService;
            this.azureDevOpsService = azureDevOpsService;

            log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(configuration.GetValue("AzureWebJobsStorage"), storageTableName: $"{nameof(AldoProjectDeployer)}Log")
                .CreateLogger();
        }

        [FunctionName("AldoProjectDeployer")]
        public async Task Run([QueueTrigger("project-deploy", Connection = "AzureWebJobsStorage")]ProjectToDeploy pd)
        {
            try
            {
                var releaseTrigger = await azureDevOpsService.TriggerRelease(pd);

                //if (!pd.FromNewResource)
                //{
                //    var notificationMessage = releaseTrigger.IsSuccess ?
                //        $"Aldo. Sto deployando il progetto {S.BreakMedium} {pd.ProjectName}. Puoi seguirne lo stato sul portale Azure DevOps."
                //        : $"Aldo. C'è stato une errore durante il deploy del progetto {S.BreakMedium} {pd.ProjectName}.";

                //    var notificationResult = await notificationService.SendUserNotification(pd.RequestedByUser, notificationMessage);

                //    if (notificationResult.IsFailure)
                //        log.Error(notificationResult.Error);
                //}                    
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }
    }
}
