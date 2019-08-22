using VirtoCommerce.ExportModule.Core.Security;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    /// <summary>
    /// Simple export security handler. Checks all of specified permissions
    /// </summary>
    class ExportSecurityPermissionHandler : IExportSecurityHandler
    {
        private ISecurityService _securityService;
        private string[] _permissions;

        public ExportSecurityPermissionHandler(ISecurityService securityService, string[] permissions)
        {
            _securityService = securityService;
            _permissions = permissions;
        }

        public bool Authorize(string userName, object resource)
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
