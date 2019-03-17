using System.Threading.Tasks;
using CodemotionRome19.Core.Base;
using CodemotionRome19.Core.Models;

namespace CodemotionRome19.Core.AzureDevOps
{
    public interface IAzureDevOpsService
    {
        Task<Result> TriggerBuild(ProjectToDeploy pd);
        Task<Result> TriggerRelease(ProjectToDeploy pd);
    }
}
