using VirtoCommerce.ExportModule.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    /// <summary>
    /// Factory for export security handlders.
    /// </summary>
    public interface IPermissionExportSecurityHandlerFactory
    {
        /// <summary>
        /// Register permission based handler for checking that all specified permissions are presented.
        /// </summary>
        /// <param name="permissions">Permissions which handler checks for.</param>
        IExportSecurityHandler Create(params string[] permissions);
    }
}
