using System;
using System.IO;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ExportModule.Core.Services
{
    /// <summary>
    /// Interface for data exporter implementation.
    /// </summary>
    public interface IDataExporter
    {
        void Export(ExportDataRequest request, Action<ExportProgressInfo> progressCallback, ICancellationToken token);
    }
}
