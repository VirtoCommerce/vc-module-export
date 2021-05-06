using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using VirtoCommerce.ExportModule.Core;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.ExportModule.Data.Extensions;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Extensions;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ExportModule.Data.Services
{
    public class DataExporter : IDataExporter
    {
        private readonly IKnownExportTypesResolver _exportTypesResolver;
        private readonly IExportProviderFactory _exportProviderFactory;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly PlatformOptions _platformOptions;
        private readonly ISettingsManager _settingsManager;
        private readonly IBlobUrlResolver _blobUrlResolver;

        public DataExporter(IKnownExportTypesResolver exportTypesResolver
            , IExportProviderFactory exportProviderFactory
            , IBlobStorageProvider blobStorageProvider
            , ISettingsManager settingsManager
            , IBlobUrlResolver blobUrlResolver
            , IOptions<PlatformOptions> platformOptions)
        {
            _exportTypesResolver = exportTypesResolver;
            _exportProviderFactory = exportProviderFactory;
            _blobStorageProvider = blobStorageProvider;
            _platformOptions = platformOptions.Value;
            _settingsManager = settingsManager;
            _blobUrlResolver = blobUrlResolver;
        }


        public void Export(ExportDataRequest request, Action<ExportProgressInfo> progressCallback, ICancellationToken token)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            token.ThrowIfCancellationRequested();

            var exportedTypeDefinition = _exportTypesResolver.ResolveExportedTypeDefinition(request.ExportTypeName);
            var pagedDataSource = (exportedTypeDefinition.DataSourceFactory ?? throw new ArgumentNullException(nameof(ExportedTypeDefinition.DataSourceFactory))).Create(request.DataQuery);

            var completedMessage = $"Export completed";
            var totalCount = pagedDataSource.GetTotalCount();
            var exportedCount = 0;
            var exportProgress = new ExportProgressInfo()
            {
                ProcessedCount = 0,
                TotalCount = totalCount,
                Description = "Export has started",
            };
            var mainExportFilePath = string.Empty;

            progressCallback(exportProgress);

            var openedStreamWritersDict = new Dictionary<string, StreamWriter>().WithDefaultValue(null);

            try
            {
                exportProgress.Description = "Creating provider…";
                progressCallback(exportProgress);
                             
                using (var exportProvider = _exportProviderFactory.CreateProvider(request))
                {
                    mainExportFilePath = GetExportFilePath(request.ExportFileNameTemplate, exportProvider.ExportedFileExtension);
                    using (var writer = new StreamWriter(_blobStorageProvider.OpenWrite(mainExportFilePath), Encoding.UTF8, 1024, true) { AutoFlush = true })
                    {
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

                            var objectBatch = pagedDataSource.Items;

                            foreach (var obj in objectBatch)
                            {
                                try
                                {
                                    var preparedObject = obj.Clone() as IExportable;

                                    request.DataQuery.FilterProperties(preparedObject);

                                    if (needTabularData)
                                    {
                                        preparedObject = (preparedObject as ITabularConvertible)?.ToTabular() ??
                                                         throw new NotSupportedException($"Object should be {nameof(ITabularConvertible)} to be exported using tabular provider.");
                                    }
                                    //TODO: Find out why exportProvider  doesn't work with streams that return from a blob provider
                                    exportProvider.WriteRecord(writer, preparedObject);

                                    //process an exported object's partitions
                                    //TODO: refactor
                                    if (obj is IHasPartitions hasPartitions)
                                    {
                                        var partitions = hasPartitions.GetPartitions();
                                        foreach (var partition in partitions.Where(x => !x.Items.IsNullOrEmpty()))
                                        {
                                            //TODO: refactor
                                            var partitionFilePath = GetExportFilePath(partition.PartitionName, exportProvider.ExportedFileExtension);
                                            var partitionWriter = openedStreamWritersDict[partitionFilePath];
                                            if (partitionWriter == null)
                                            {
                                                partitionWriter = new StreamWriter(_blobStorageProvider.OpenWrite(partitionFilePath), Encoding.UTF8, 1024, true) { AutoFlush = true };
                                                openedStreamWritersDict[partitionFilePath] = partitionWriter;
                                            }

                                            foreach (var partitionItem in partition.Items)
                                            {
                                                var partitionExportedItem = partitionItem.Clone() as IExportable;

                                                if (needTabularData)
                                                {
                                                    partitionExportedItem = (partitionExportedItem as ITabularConvertible)?.ToTabular() ??
                                                                     throw new NotSupportedException($"Object should be {nameof(ITabularConvertible)} to be exported using tabular provider.");
                                                }
                                                exportProvider.WriteRecord(partitionWriter, partitionExportedItem);
                                            }
                                        }
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
                    completedMessage = $"Export completed with errors";
                }

                exportProgress.Description = $"{completedMessage}: {exportedCount} out of {totalCount} have been exported.";
                exportProgress.Finished = DateTime.UtcNow;
                exportProgress.DownloadUrl = _blobUrlResolver.GetAbsoluteUrl(mainExportFilePath);
                foreach(var pair in openedStreamWritersDict)
                {
                    exportProgress.PartitionUrls.Add(_blobUrlResolver.GetAbsoluteUrl(pair.Key));
                    pair.Value.Flush();
                    pair.Value.Dispose();
                }
                progressCallback(exportProgress);
            }
        }
       
        private string GetExportFilePath(string fileNameTemplate, string extension)
        {
            if (string.IsNullOrEmpty(_platformOptions.DefaultExportFolder))
            {
                throw new PlatformException($"{nameof(_platformOptions.DefaultExportFolder)} must be set.");
            }
            var result = fileNameTemplate;
            if (string.IsNullOrEmpty(result))
            {
                result = _settingsManager.GetValue(ModuleConstants.Settings.General.ExportFileNameTemplate.Name, ModuleConstants.Settings.General.ExportFileNameTemplate.DefaultValue.ToString());
            }
            result = string.Format(result, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(extension))
            {
                result = Path.ChangeExtension(result, extension);
            }

            return UrlHelperExtensions.Combine(_platformOptions.DefaultExportFolder, result);
        }
    }
}
