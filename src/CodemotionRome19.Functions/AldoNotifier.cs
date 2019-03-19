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
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string eventType = data.eventType;
                var userToNotify = Result.Fail<string>("User not found");
                var message = string.Empty;

                switch (eventType)
                {
                    case "ms.vss-release.deployment-completed-event":
                    {
                        var release = data.resource;
                        var deployment = release.deployment;
                        var project = release.project;
                        var ok = deployment.deploymentStatus == "succeeded";
                        message = $"Aldo. Deployment del progetto {project.name} in {release.environment.name} " +
                                  $"{(ok ? "terminato con successo!" : "fallito!")}";
                        userToNotify = await azureDevOpsService.GetReleaseRequestor(project.id.ToString(), Convert.ToInt32(deployment.release.id.ToString()));
                        break;
                    }
                    case "build.complete":
                    {
                        var build = data.resource;
                        var project = build.definition.project;
                        var ok = build.result == "succeeded";
                        message = $"Aldo. Build {build.buildNumber} del progetto {project.name} {(ok ? "terminata con successo!" : "fallita!")}";
                        userToNotify = await azureDevOpsService.GetBuildRequestor(project.id.ToString(), Convert.ToInt32(build.id.ToString()));
                        break;
                    }
                }

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
