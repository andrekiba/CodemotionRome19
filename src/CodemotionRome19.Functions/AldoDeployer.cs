using System;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Base;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Notification;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CodemotionRome19.Functions
{
    public class AldoDeployer
    {
        readonly IConfiguration configuration;
        readonly IAzureService azureService;
        readonly IDeploymentService deploymentService;
        readonly INotificationService notificationService;

        public AldoDeployer(IConfiguration configuration, IAzureService azureService, IDeploymentService deploymentService, INotificationService notificationService)
        {
            this.configuration = configuration;
            this.azureService = azureService;
            this.deploymentService = deploymentService;
            this.notificationService = notificationService;
        }

        [FunctionName("AldoDeployer")]
        public async Task Run([QueueTrigger("azure-resource-deploy", Connection = "AzureWebJobsStorage")]AzureResourceToDeploy ard, 
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {ard.AzureResource.Type.Name}");

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            //var tableName = Environment.GetEnvironmentVariable("deployLog", EnvironmentVariableTarget.Process);
            var serilog = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(connectionString, storageTableName: "AldoDeployerLog")
                .CreateLogger();

            try
            {
                var deployOptions = new DeploymentOptions
                {
                    Region = Region.EuropeWest,
                    ResourceGroupName = "TestCodemotionRome19",
                    UseExistingResourceGroup = true,
                    SubscriptionId = configuration.SubscriptionId
                };

                var azure = await azureService.Authenticate(configuration.ClientId, configuration.ClientSecret, configuration.TenantId);

                var deployResult = await deploymentService.Deploy(azure, deployOptions, ard.AzureResource);

                await Task.Delay(TimeSpan.FromSeconds(1));

                const string failed = "è fallito";
                const string success = "è andato a buon fine";
                var resourceName = ard.AzureResource.Name.IsNullOrWhiteSpace() ? string.Empty : ard.AzureResource.Name;

                var notificationMessage = $"Aldo. Il deploy della risorsa {ard.AzureResource.Type.Name} {resourceName} {(deployResult.IsSuccess ? success : failed)}";

                var notificationResult = await notificationService.SendUserNotification(ard.RequestedByUser, notificationMessage);

                if(notificationResult.IsFailure)
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
