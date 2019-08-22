using System;

namespace VirtoCommerce.ExportModule.Core.Security
{
    /// <summary>
    /// Registrar for export security policy handlders.
    /// </summary>
    public interface IExportSecurityHandlerRegistrar
    {
        /// <summary>
        /// Register handler for export policy.
        /// </summary>
        /// <param name="policyName">Export security policy name.</param>
        /// <param name="handlerFactory">Factory to instantiate handler.</param>
        void RegisterHandler(string policyName, Func<IExportSecurityHandler> handlerFactory);
        /// <summary>
        /// Gets registered handler by policy name
        /// </summary>
        /// <param name="policyName">Security policy name.</param>
        /// <returns>Registered handler for the policy, otherwise null.</returns>
        IExportSecurityHandler GetHandler(string policyName);
    }
}
