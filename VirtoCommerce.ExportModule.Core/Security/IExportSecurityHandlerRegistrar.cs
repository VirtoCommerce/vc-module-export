using System;

namespace VirtoCommerce.ExportModule.Core.Security
{
    public interface IExportSecurityHandlerRegistrar
    {
        void Register(string handlerName, Func<IExportSecurityHandler> handlerFactory);
        IExportSecurityHandler GetHandler(string handlerName);
    }
}
