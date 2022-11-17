using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Hangfire;

namespace VirtoCommerce.ExportModule.Web.BackgroundJobs
{
    public class ExportJob
    {
        private readonly IDataExporter _dataExporter;
        private readonly IExportFileStorage _exportFileStorage;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IExportProviderFactory _exportProviderFactory;

        public ExportJob(
            IDataExporter dataExporter,
            IExportFileStorage exportFileStorage,
            IPushNotificationManager pushNotificationManager,
            IExportProviderFactory exportProviderFactory)
        {
            _dataExporter = dataExporter;
            _exportFileStorage = exportFileStorage;
            _pushNotificationManager = pushNotificationManager;
            _exportProviderFactory = exportProviderFactory;
        }

        public async Task ExportBackgroundAsync(ExportDataRequest request, ExportPushNotification notification, IJobCancellationToken cancellationToken, PerformContext context)
        {
            void ProgressCallback(ExportProgressInfo x)
            {
                notification.Patch(x);
                notification.JobId = context.BackgroundJob.Id;
                _pushNotificationManager.Send(notification);
            }

            try
            {
                // Do not like provider creation here to get file extension, maybe need to pass created provider to Exporter.
                // Create stream inside Exporter is not good as it is not Exporter responsibility to decide where to write.
                var provider = _exportProviderFactory.CreateProvider(request);

                var fileName = _exportFileStorage.GenerateFileName(DateTime.UtcNow, provider.ExportedFileExtension);

                await using (var stream = await _exportFileStorage.OpenWriteAsync(fileName))
                {
                    _dataExporter.Export(stream, request, ProgressCallback, new JobCancellationTokenWrapper(cancellationToken));
                }

                notification.DownloadUrl = $"/api/export/download/{fileName}";
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
