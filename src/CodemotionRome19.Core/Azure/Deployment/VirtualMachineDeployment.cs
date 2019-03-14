using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class VirtualMachineDeployment : BaseDeployment
    {
        public VirtualMachineDeployment(
            string virtualMachineName,
            IAuthenticated azure,
            DeploymentOptions options)
            : base(azure, options)
        {
            VirtualMachineName = virtualMachineName;
        }

        public string VirtualMachineName { get; }

        protected override Task ExecuteCreateAsync()
        {
            var definition = Azure
                .WithSubscription(Options.SubscriptionId)
                .VirtualMachines
                .Define(VirtualMachineName)
                .WithRegion(Options.Region);

            var withReasourceGroup = Options.UseExistingResourceGroup
                ? definition.WithExistingResourceGroup(Options.ResourceGroupName)
                : definition.WithNewResourceGroup(Options.ResourceGroupName);

            var create = withReasourceGroup
                .WithNewPrimaryNetwork("10.0.0.0/6")
                .WithPrimaryPrivateIPAddressDynamic()
                .WithNewPrimaryPublicIPAddress("mywindowsvmdns")
                .WithPopularWindowsImage(KnownWindowsVirtualMachineImage.WindowsServer2012R2Datacenter)
                .WithAdminUsername("andre")
                .WithAdminPassword("p4ssw0rd")
                .WithSize(VirtualMachineSizeTypes.BasicA0);

            return create.CreateAsync();
        }

        protected override string GetDeploymentName() => $"'{VirtualMachineName}' Virtual Machine";
    }
}
