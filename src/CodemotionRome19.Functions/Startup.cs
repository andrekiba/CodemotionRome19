using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Functions;
using CodemotionRome19.Functions.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Startup))]
namespace CodemotionRome19.Functions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<IAzureConfiguration, AppSettings>();
            builder.Services.AddTransient<IAzureService, AzureService>();
            builder.Services.AddTransient<IDeploymentService, DeploymentService>();
        }
    }
}
