using System;
using System.IO;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Functions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CodemotionRome19.Functions
{
    public class TestFunc
    {
        readonly AppSettings appSettings;
        readonly IAzureService azureService;
        readonly IDeploymentService deploymentService;

        public TestFunc(AppSettings appSettings, IAzureService azureService, IDeploymentService deploymentService)
        {
            this.appSettings = appSettings;
            this.azureService = azureService;
            this.deploymentService = deploymentService;
        }

        [FunctionName("TestFunc")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            var tableName = Environment.GetEnvironmentVariable("deployLog", EnvironmentVariableTarget.Process);
            var serilog = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(connectionString, storageTableName: tableName)
                .CreateLogger();

            serilog.Information("test func running");

            //try
            //{
            //    var azure = await azureService.Authenticate(appSettings.ClientId, appSettings.ClientSecret, appSettings.TenantId);
            //    var resourceGroups = azure.WithSubscription(appSettings.SubscriptionId).ResourceGroups.List();
            //    var subscriptions = azure.Subscriptions.List();

            //    var funcDeployOptions = new DeploymentOptions
            //    {
            //        Region = Region.EuropeWest,
            //        ResourceGroupName = "TestCodemotionRome19",
            //        UseExistingResourceGroup = false,
            //    };

            //    var result = await deploymentService.Deploy(azure, funcDeployOptions, new AzureResource {Name = "CodemotionRomeFuncTest1"});
            //}
            //catch (Exception e)
            //{
            //    log.LogError(e, "Azzzz!!!!");
            //}

            string name = req.Query["name"];

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
