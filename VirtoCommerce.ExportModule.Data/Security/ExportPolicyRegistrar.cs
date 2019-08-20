using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using VirtoCommerce.ExportModule.Core.Security;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    public class ExportPolicyRegistrar : IExportPolicyRegistrar
    {
        private readonly ISecurityService _securityService;

        public ExportPolicyRegistrar(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public Dictionary<string, string> _policies = new Dictionary<string, string>();
        public void RegisterPolicy(string policy, string permission)
        {
            _policies.Add(policy, permission);
        }

        public bool Authorize(IPrincipal principal, string policyName)
        {
            var permission = _policies[policyName];
            var result = _securityService.UserHasAnyPermission(principal.Identity.Name, null, new[] {permission});
            return result;
        }
    }
}
