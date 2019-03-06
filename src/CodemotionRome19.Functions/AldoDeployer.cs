using System;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Azure.Deployment;
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
        public async Task Run([QueueTrigger("azure-resources", Connection = "AzureWebJobsStorage")]AzureResource azureResource, 
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {azureResource}");

            try
            {
                var deployOptions = new DeploymentOptions
                {
                    Region = Region.EuropeWest,
                    ResourceGroupName = "TestCodemotionRome19",
                    UseExistingResourceGroup = true
                };

                var azure = await azureService.Authenticate(appSettings.ClientId, appSettings.ClientSecret, appSettings.TenantId);
                var deployResult = await deploymentService.Deploy(azure, deployOptions, azureResource);
                var notificationResult = await notificationService.SendNotification(deployResult);
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.LogError(error);
            }
        }

        
    }
}
