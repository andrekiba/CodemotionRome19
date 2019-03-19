using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Notification;
using CodemotionRome19.Core.AzureDevOps;
using CodemotionRome19.Core.Base;
using Serilog;

namespace CodemotionRome19.Functions
{
    public class AldoNotifier
    {
        #region Fields

        const string BreakStrong = "<break strength=\"strong\"/>";

        readonly IConfiguration configuration;
        readonly INotificationService notificationService;
        readonly IAzureDevOpsService azureDevOpsService;

        #endregion 

        public AldoNotifier(IConfiguration configuration, INotificationService notificationService, IAzureDevOpsService azureDevOpsService)
        {
            this.configuration = configuration;
            this.notificationService = notificationService;
            this.azureDevOpsService = azureDevOpsService;
        }

        [FunctionName("AldoNotifier")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            var log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(connectionString, storageTableName: "AldoNotifierLog")
                .CreateLogger();

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string eventType = data.eventType;
                Result<string> userToNotify = Result.Fail<string>("User not found");
                string message = string.Empty;

                if (eventType == "ms.vss-release.deployment-completed-event")
                {
                    string idProject = data.resource.project.id;
                    var idRelease = Convert.ToInt32(data.resource.deployment.release.id);
                    var ok = data.resource.deployment.deploymentStatus == "succeeded";
                    message = $"Aldo. Deployment del progetto {data.resource.project.name} in {data.resource.deployment.releaseEnvironment.name} " +
                              $"{(ok ? "terminato con successo!" : "fallito!")}";
                    userToNotify = await azureDevOpsService.GetReleaseRequestor(idProject, idRelease);
                }

                //if (eventType == "build.complete")
                //{
                //    string idProject = ??;
                //    var idBuild = Convert.ToInt32(data.resource.id);
                //    message = data.message.text;
                //    userToNotify = await azureDevOpsService.GetBuildRequestedBy(idProject, idBuild);
                //}

                if (userToNotify.IsSuccess)
                {
                    var notificationResult = await notificationService.SendUserNotification(userToNotify.Value, message);

                    if (notificationResult.IsFailure)
                        log.Error(notificationResult.Error);
                }

                return new OkResult();
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
