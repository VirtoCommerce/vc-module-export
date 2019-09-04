using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Security;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    /// <summary>
    /// Export security handler which checks user have all specified permissions.
    /// </summary>
    public class PermissionExportSecurityHandler : IExportSecurityHandler
    {
        private ISecurityService _securityService;
        private string[] _permissions;

        public PermissionExportSecurityHandler(ISecurityService securityService, params string[] permissions)
        {
            _securityService = securityService;
            _permissions = permissions;
        }

        public bool Authorize(string userName, ExportDataQuery dataQuery)
        {
            var result = true;
            foreach (var permission in _permissions)
            {
                result &= _securityService.UserHasAnyPermission(userName, null, permission);
            }
            return result;
        }
    }
}
