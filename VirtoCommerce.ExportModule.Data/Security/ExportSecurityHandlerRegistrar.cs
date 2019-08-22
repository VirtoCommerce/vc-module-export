using System;
using System.Collections.Generic;
using VirtoCommerce.ExportModule.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    public class ExportSecurityHandlerRegistrar : IExportSecurityHandlerRegistrar
    {
        private Dictionary<string, Func<IExportSecurityHandler>> _handlers = new Dictionary<string, Func<IExportSecurityHandler>>();

        public IExportSecurityHandler GetHandler(string handlerName)
        {
            return _handlers[handlerName]();
        }

        public void Register(string handlerName, Func<IExportSecurityHandler> handlerFactory)
        {
            _handlers.Add(handlerName, handlerFactory);
        }
    }
}
