using VirtoCommerce.ExportModule.Core.Security;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    public class PermissionExportSecurityHandlerFactory : IPermissionExportSecurityHandlerFactory
    {
        private readonly ISecurityService _securityService;

        public PermissionExportSecurityHandlerFactory(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public IExportSecurityHandler Create(params string[] permissions)
        {
            return new PermissionExportSecurityHandler(_securityService, permissions);
        }
    }
}
