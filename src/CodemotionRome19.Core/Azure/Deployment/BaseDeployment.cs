using System.Diagnostics;
using System.Threading.Tasks;
using CodemotionRome19.Core.Base;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public abstract class BaseDeployment
    {
        readonly Stopwatch watch;

        protected BaseDeployment(Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure, DeploymentOptions options)
        {
            Azure = azure;
            Options = options;

            watch = new Stopwatch();
        }

        public Microsoft.Azure.Management.Fluent.Azure.IAuthenticated Azure { get; }

        public DeploymentOptions Options { get; }

        public async Task CreateAsync()
        {
            watch.Restart();

            await ExecuteCreateAsync();

            var totalSeconds = watch.Elapsed.TotalSeconds;
            Debug.WriteLine($"'{GetDeploymentName()}' created in {totalSeconds} seconds");

            watch.Stop();
        }

        protected abstract string GetEventName();

        protected abstract string GetDeploymentName();

        protected abstract Task ExecuteCreateAsync();
    }
}
