using System;
using System.Collections.Generic;
using VirtoCommerce.ExportModule.Core.Security;

namespace VirtoCommerce.ExportModule.Data.Security
{
    public class ExportSecurityHandlerRegistrar : IExportSecurityHandlerRegistrar
    {
        private readonly Dictionary<string, Func<IExportSecurityHandler>> _handlerFactories = new Dictionary<string, Func<IExportSecurityHandler>>();

        public IExportSecurityHandler GetHandler(string policyName)
        {
            return _handlerFactories.ContainsKey(policyName) ? _handlerFactories[policyName]() : null;
        }

        public void RegisterHandler(string policyName, Func<IExportSecurityHandler> handlerFactory)
        {
            _handlerFactories[policyName] = handlerFactory;
        }
    }
}
