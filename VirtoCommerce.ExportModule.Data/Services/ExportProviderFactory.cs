using System;
using System.Linq;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ExportModule.Data.Services
{
    public class ExportProviderFactory : IExportProviderFactory
    {
        private readonly Func<ExportDataRequest, IExportProvider>[] _providerFactories;

        public ExportProviderFactory(Func<ExportDataRequest, IExportProvider>[] providerFactories)
        {
            _providerFactories = providerFactories;
        }

        public virtual IExportProvider CreateProvider(ExportDataRequest exportDataRequest)
        {
            if (exportDataRequest == null)
            {
                throw new ArgumentNullException(nameof(exportDataRequest));
            }

            var result = _providerFactories.FirstOrDefault(x => x(exportDataRequest).TypeName.EqualsInvariant(exportDataRequest.ProviderName));

            return result != null ? result(exportDataRequest) : null;
        }
    }
}
