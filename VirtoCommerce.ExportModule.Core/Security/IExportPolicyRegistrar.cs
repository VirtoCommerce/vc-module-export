using System.Security.Principal;

namespace VirtoCommerce.ExportModule.Core.Security
{
    public interface IExportPolicyRegistrar
    {
        void RegisterPolicy(string policy, string permission);

        bool Authorize(IPrincipal principal, string policyName);
    }
}
