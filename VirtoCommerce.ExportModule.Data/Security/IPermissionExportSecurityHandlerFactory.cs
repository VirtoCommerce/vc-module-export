using VirtoCommerce.ExportModule.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    /// <summary>
    /// Factory for permission based export security handlers.
    /// </summary>
    public interface IPermissionExportSecurityHandlerFactory
    {
        /// <summary>
        /// Creates permission based handler for checking that all specified permissions are presented.
        /// </summary>
        /// <param name="permissions">Permissions which handler checks for.</param>
        IExportSecurityHandler Create(params string[] permissions);
    }
}
