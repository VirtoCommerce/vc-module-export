using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Options;
using VirtoCommerce.ExportModule.Core;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Extensions;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Hangfire;

namespace VirtoCommerce.ExportModule.Web.BackgroundJobs
{
    public class ExportJob
    {
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly PlatformOptions _platformOptions;
        private readonly IDataExporter _dataExporter;
        private readonly IExportProviderFactory _exportProviderFactory;
        private readonly ISettingsManager _settingsManager;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;

        private string fileNameTemplate;

        public ExportJob(IDataExporter dataExporter,
            IPushNotificationManager pushNotificationManager,
            IOptions<PlatformOptions> platformOptions,
            IExportProviderFactory exportProviderFactory,
            ISettingsManager settingsManager,
            IBlobStorageProvider blobStorageProvider,
            IBlobUrlResolver blobUrlResolver)
        {
            _dataExporter = dataExporter;
            _pushNotificationManager = pushNotificationManager;
            _platformOptions = platformOptions.Value;
            _exportProviderFactory = exportProviderFactory;
            _settingsManager = settingsManager;
            _blobStorageProvider = blobStorageProvider;
            _blobUrlResolver = blobUrlResolver;
        }

        private string FileNameTemplate
        {
            get
            {
                if (fileNameTemplate == null)
                {
                    fileNameTemplate = _settingsManager.GetValue(ModuleConstants.Settings.General.ExportFileNameTemplate.Name, ModuleConstants.Settings.General.ExportFileNameTemplate.DefaultValue.ToString());
                }

                return fileNameTemplate;
            }
        }

        public async Task ExportBackgroundAsync(ExportDataRequest request, ExportPushNotification notification, IJobCancellationToken cancellationToken, PerformContext context)
        {
            void progressCallback(ExportProgressInfo x)
            {
                notification.Patch(x);
                notification.JobId = context.BackgroundJob.Id;
                _pushNotificationManager.Send(notification);
            }

            try
            {
                if (string.IsNullOrEmpty(_platformOptions.DefaultExportFolder))
                {
                    throw new PlatformException($"{nameof(_platformOptions.DefaultExportFolder)} should be set.");
                }

                var fileName = string.Format(FileNameTemplate, DateTime.UtcNow);

                // Do not like provider creation here to get file extension, maybe need to pass created provider to Exporter.
                // Create stream inside Exporter is not good as it is not Exporter resposibility to decide where to write.
                var provider = _exportProviderFactory.CreateProvider(request);

                if (!string.IsNullOrEmpty(provider.ExportedFileExtension))
                {
                    fileName = Path.ChangeExtension(fileName, provider.ExportedFileExtension);
                }

                var url = UrlHelperExtensions.Combine(_platformOptions.DefaultExportFolder, fileName);
                using (var blobStream = _blobStorageProvider.OpenWrite(url))
                {
                    _dataExporter.Export(blobStream, request, progressCallback, new JobCancellationTokenWrapper(cancellationToken));
                }

                notification.DownloadUrl = _blobUrlResolver.GetAbsoluteUrl(url);
            }
            catch (JobAbortedException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                notification.Errors.Add(ex.ExpandExceptionMessage());
            }
            finally
            {
                notification.Description = "Export finished";
                notification.Finished = DateTime.UtcNow;
                await _pushNotificationManager.SendAsync(notification);
            }
        }
    }
}
