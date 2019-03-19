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
    public class AldoProjectBuilder
    {
        #region Fields

        readonly IConfiguration configuration;
        readonly INotificationService notificationService;
        readonly IAzureDevOpsService azureDevOpsService;
        readonly ILogger log;

        #endregion 

        public AldoProjectBuilder(IConfiguration configuration, INotificationService notificationService, IAzureDevOpsService azureDevOpsService)
        {
            this.configuration = configuration;
            this.notificationService = notificationService;
            this.azureDevOpsService = azureDevOpsService;

            log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(configuration.GetValue("AzureWebJobsStorage"), storageTableName: $"{nameof(AldoProjectBuilder)}Log")
                .CreateLogger();
        }

        [FunctionName("AldoProjectBuilder")]
        public async Task Run([QueueTrigger("project-build", Connection = "AzureWebJobsStorage")]ProjectToBuild pb)
        {
            try
            {
                var buildTrigger = await azureDevOpsService.TriggerBuild(pb);

                //var notificationMessage = buildTrigger.IsSuccess ?
                //    $"Aldo. Sto facendo la build del progetto {S.BreakMedium} {pb.ProjectName}. Puoi seguirne lo stato sul portale Azure DevOps."
                //    : $"Aldo. C'è stato une errore durante la build del progetto {S.BreakMedium} {pb.ProjectName}.";

                //var notificationResult = await notificationService.SendUserNotification(pb.RequestedByUser, notificationMessage);

                //if (notificationResult.IsFailure)
                //    log.Error(notificationResult.Error);

            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }
    }
}
