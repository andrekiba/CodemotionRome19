using System;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Models;
using CodemotionRome19.Core.Notification;
using CodemotionRome19.Functions.Extensions;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CodemotionRome19.Functions
{
    public class AldoResourceDeployer
    {
        #region Fields

        readonly IConfiguration configuration;
        readonly IAzureService azureService;
        readonly IDeploymentService deploymentService;
        readonly INotificationService notificationService;
        readonly ILogger log;

        #endregion 

        public AldoResourceDeployer(IConfiguration configuration, IAzureService azureService, IDeploymentService deploymentService, INotificationService notificationService)
        {
            this.configuration = configuration;
            this.azureService = azureService;
            this.deploymentService = deploymentService;
            this.notificationService = notificationService;

            log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(configuration.GetValue("AzureWebJobsStorage"), storageTableName: $"{nameof(AldoResourceDeployer)}Log")
                .CreateLogger();
        }

        [FunctionName("AldoResourceDeployer")]
        public async Task Run([QueueTrigger("azure-resource-deploy", Connection = "AzureWebJobsStorage")]AzureResourceToDeploy ard,
            [Queue("project-deploy", Connection = "AzureWebJobsStorage")] IAsyncCollector<ProjectToDeploy> projectDeployQueue)
        {
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

                string notificationMessage;

                if (deployResult.IsSuccess)
                {                    
                    if (ard.Project != null)
                    {
                        notificationMessage = $"Aldo. Ho creato la risorsa {S.BreakMedium} {ard.AzureResource.Name}. " +
                                              $"Ora sto deployando il progetto {S.BreakMedium} {ard.Project.ProjectName}. Puoi seguirne lo stato sul portale Azure DevOps.";

                        ard.Project.Variables.Add("ResourceName", deployResult.Value);

                        await projectDeployQueue.AddAsync(ard.Project);
                    }
                    else
                    {
                        notificationMessage = $"Aldo. Il deploy della risorsa {S.BreakMedium} {ard.AzureResource.Name} è andato a buon fine.";
                    }
                }
                else
                {
                    notificationMessage = $"Aldo. Il deploy della risorsa {S.BreakMedium} {ard.AzureResource.Name} è fallito.";
                }

                var notificationResult = await notificationService.SendUserNotification(ard.RequestedByUser, notificationMessage);

                if(notificationResult.IsFailure)
                    log.Error(notificationResult.Error);
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }        
    }
}
