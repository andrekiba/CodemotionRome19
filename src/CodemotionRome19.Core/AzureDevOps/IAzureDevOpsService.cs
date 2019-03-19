using System.Threading.Tasks;
using CodemotionRome19.Core.Base;
using CodemotionRome19.Core.Models;

namespace CodemotionRome19.Core.AzureDevOps
{
    public interface IAzureDevOpsService
    {
        Task<Result> TriggerBuild(ProjectToBuild pb);
        Task<Result<string>> GetBuildRequestor(string idProject, int idBuild);
        Task<Result> TriggerRelease(ProjectToDeploy pd);
        Task<Result<string>> GetReleaseRequestor(string idProject, int idRelease);
    }
}
