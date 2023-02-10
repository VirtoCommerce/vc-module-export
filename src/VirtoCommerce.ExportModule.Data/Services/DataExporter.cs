using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.ExportModule.Data.Extensions;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ExportModule.Data.Services
{
    public class DataExporter : IDataExporter
    {
        private readonly IKnownExportTypesResolver _exportTypesResolver;
        private readonly IExportProviderFactory _exportProviderFactory;

        public DataExporter(IKnownExportTypesResolver exportTypesResolver, IExportProviderFactory exportProviderFactory)
        {
            _exportTypesResolver = exportTypesResolver;
            _exportProviderFactory = exportProviderFactory;
        }

        public void Export(Stream stream, ExportDataRequest request, Action<ExportProgressInfo> progressCallback, ICancellationToken token)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            token.ThrowIfCancellationRequested();

            var exportedTypeDefinition = _exportTypesResolver.ResolveExportedTypeDefinition(request.ExportTypeName);
            var pagedDataSource = (exportedTypeDefinition.DataSourceFactory ?? throw new ArgumentNullException(nameof(ExportedTypeDefinition.DataSourceFactory))).Create(request.DataQuery);

            var completedMessage = "Export completed";
            var totalCount = pagedDataSource.GetTotalCount();
            var exportedCount = 0;
            var exportProgress = new ExportProgressInfo
            {
                ProcessedCount = 0,
                TotalCount = totalCount,
                Description = "Export has started",
            };
            progressCallback(exportProgress);

            try
            {
                exportProgress.Description = "Creating provider…";
                progressCallback(exportProgress);

                using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true) { AutoFlush = true };
                using var exportProvider = _exportProviderFactory.CreateProvider(request);

                var needTabularData = exportProvider.IsTabular;
                if (needTabularData && !exportedTypeDefinition.IsTabularExportSupported)
                {
                    throw new NotSupportedException($"Provider \"{exportProvider.TypeName}\" does not support tabular export.");
                }

                exportProgress.Description = "Fetching…";
                progressCallback(exportProgress);

                while (pagedDataSource.Fetch())
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var obj in pagedDataSource.Items)
                    {
                        try
                        {
                            var preparedObject = obj.CloneTyped();

                            if (preparedObject is IEnumerable<IExportable> enumerable)
                            {
                                foreach (var exportable in enumerable)
                                {
                                    WriteRecord(exportProvider, writer, request, exportable, needTabularData);
                                }
                            }
                            else
                            {
                                WriteRecord(exportProvider, writer, request, preparedObject, needTabularData);
                            }
                        }
                        catch (Exception e)
                        {
                            exportProgress.Errors.Add(e.Message);
                            progressCallback(exportProgress);
                        }
                        exportedCount++;
                    }

                    exportProgress.ProcessedCount = exportedCount;

                    if (exportedCount != totalCount)
                    {
                        exportProgress.Description = $"{exportedCount} out of {totalCount} have been exported.";
                        progressCallback(exportProgress);
                    }
                }
            }
            catch (Exception e)
            {
                exportProgress.Errors.Add(e.Message);
            }
            finally
            {
                if (exportProgress.Errors.Count > 0)
                {
                    completedMessage = "Export completed with errors";
                }

                exportProgress.Description = $"{completedMessage}: {exportedCount} out of {totalCount} have been exported.";
                progressCallback(exportProgress);
            }
        }


        private static void WriteRecord(IExportProvider exportProvider, TextWriter writer, ExportDataRequest request, IExportable exportable, bool needTabularData)
        {
            if (needTabularData)
            {
                var tabular = exportable as ITabularConvertible ??
                              throw new NotSupportedException($"Object should be {nameof(ITabularConvertible)} to be exported using tabular provider.");
                exportable = tabular.ToTabular();
            }

            request.DataQuery.FilterProperties(exportable);
            exportProvider.WriteRecord(writer, exportable);
        }
    }
}
