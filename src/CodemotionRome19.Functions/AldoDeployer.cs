using System;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Models;
using CodemotionRome19.Core.Notification;
using CodemotionRome19.Functions.Configuration;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CodemotionRome19.Functions
{
    public class AldoDeployer
    {
        readonly AppSettings appSettings;
        readonly IAzureService azureService;
        readonly IDeploymentService deploymentService;
        readonly INotificationService notificationService;

        public AldoDeployer(AppSettings appSettings, IAzureService azureService, IDeploymentService deploymentService, INotificationService notificationService)
        {
            this.appSettings = appSettings;
            this.azureService = azureService;
            this.deploymentService = deploymentService;
            this.notificationService = notificationService;
        }

        [FunctionName("AldoDeployer")]
        public async Task Run([QueueTrigger("azure-resource-deploy", Connection = "AzureWebJobsStorage")]AzureResourceToDeploy azureResourceToDeploy, 
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {azureResourceToDeploy}");

            try
            {
                var deployOptions = new DeploymentOptions
                {
                    Region = Region.EuropeWest,
                    ResourceGroupName = "TestCodemotionRome19",
                    UseExistingResourceGroup = true,
                    SubscriptionId = appSettings.SubscriptionId
                };

                var azure = await azureService.Authenticate(appSettings.ClientId, appSettings.ClientSecret, appSettings.TenantId);

                var deployResult = await deploymentService.Deploy(azure, deployOptions, azureResourceToDeploy.AzureResource);

                await Task.Delay(TimeSpan.FromSeconds(2));

                var notificationMessage = deployResult.IsSuccess ? $"Il deploy della risorsa {azureResourceToDeploy.AzureResource.Type.Name} richiesta è andato a buon fine" : 
                    $"Il deploy della risorsa {azureResourceToDeploy.AzureResource.Type.Name} richiesta è fallito";

                var notificationResult = await notificationService.SendUserNotification(azureResourceToDeploy.RequestedByUser, notificationMessage);
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.LogError(error);
            }
        }

        
    }
}
