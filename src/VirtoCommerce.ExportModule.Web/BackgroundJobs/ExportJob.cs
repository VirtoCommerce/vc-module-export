using System;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.ExportModule.Web.BackgroundJobs
{
    public class ExportJobPayload
    {
        public ExportDataRequest Request { get; set; }
        public ExportPushNotification Notification { get; set; }
    }

    /// <summary>
    /// Engine-agnostic export job. Replaces the former Hangfire job that used
    /// <c>PerformContext</c>/<c>IJobCancellationToken</c>: progress still flows through the module's own
    /// push-notification manager; the job id comes from <see cref="IJobExecutionContext.JobId"/> and
    /// cancellation from the handler's <see cref="CancellationToken"/>.
    /// </summary>
    public class ExportJob(
        IDataExporter dataExporter,
        IExportFileStorage exportFileStorage,
        IPushNotificationManager pushNotificationManager,
        IExportProviderFactory exportProviderFactory) : IBackgroundJobHandler<ExportJobPayload>
    {
        public async Task Execute(ExportJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var request = payload.Request;
            var notification = payload.Notification;

            void ProgressCallback(ExportProgressInfo x)
            {
                notification.Patch(x);
                notification.JobId = context.JobId;
                pushNotificationManager.Send(notification);
            }

            try
            {
                // Do not like provider creation here to get file extension, maybe need to pass created provider to Exporter.
                // Create stream inside Exporter is not good as it is not Exporter responsibility to decide where to write.
                var provider = exportProviderFactory.CreateProvider(request);

                var fileName = exportFileStorage.GenerateFileName(DateTime.UtcNow, provider.ExportedFileExtension);

                await using (var stream = await exportFileStorage.OpenWriteAsync(fileName))
                {
                    dataExporter.Export(stream, request, ProgressCallback, cancellationToken);
                }

                notification.DownloadUrl = $"/api/export/download/{fileName}";
            }
            catch (OperationCanceledException)
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
                await pushNotificationManager.SendAsync(notification);
            }
        }
    }
}
