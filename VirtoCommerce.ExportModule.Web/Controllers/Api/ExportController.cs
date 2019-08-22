using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using Hangfire;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.ExportModule.Web.BackgroundJobs;
using VirtoCommerce.ExportModule.Web.Model;
using VirtoCommerce.ExportModule.Web.Security;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Security;

namespace VirtoCommerce.ExportModule.Web.Controllers
{
    [RoutePrefix("api/export")]
    public class ExportController : ApiController
    {
        private readonly Func<ExportDataRequest, IExportProvider>[] _exportProviderFactories;
        private readonly IKnownExportTypesRegistrar _knownExportTypesRegistrar;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IKnownExportTypesResolver _knownExportTypesResolver;
        private readonly string _defaultExportFolder;
        private readonly ISecurityService _securityService;


        public ExportController(
            Func<ExportDataRequest, IExportProvider>[] exportProviderFactories,
            IKnownExportTypesRegistrar knownExportTypesRegistrar,
            IUserNameResolver userNameResolver,
            IModuleInitializerOptions moduleInitializerOptions,
            IKnownExportTypesResolver knownExportTypesResolver,
            ISecurityService securityService)
        {
            _exportProviderFactories = exportProviderFactories;
            _knownExportTypesRegistrar = knownExportTypesRegistrar;
            _userNameResolver = userNameResolver;
            _knownExportTypesResolver = knownExportTypesResolver;
            _defaultExportFolder = moduleInitializerOptions.VirtualRoot + "/App_Data/Export/";
            _securityService = securityService;
        }

        /// <summary>
        /// Gets the list of types ready to be exported
        /// </summary>
        /// <returns>The list of exported known types</returns>
        [HttpGet]
        [Route("knowntypes")]
        [ResponseType(typeof(ExportedTypeDefinition[]))]
        [CheckPermission(Permission = ExportPredefinedPermissions.Access)]
        public IHttpActionResult GetExportedKnownTypes()
        {
            return Ok(_knownExportTypesRegistrar.GetRegisteredTypes());
        }

        /// <summary>
        /// Gets the list of available export providers
        /// </summary>
        /// <returns>The list of export providers</returns>
        [HttpGet]
        [Route("providers")]
        [ResponseType(typeof(IExportProvider[]))]
        [CheckPermission(Permission = ExportPredefinedPermissions.Access)]
        public IHttpActionResult GetExportProviders()
        {
            return Ok(_exportProviderFactories.Select(x => x(new ExportDataRequest())).ToArray());
        }

        /// <summary>
        /// Provides generic viewable entities collection based on the request
        /// </summary>
        /// <param name="request">Data request</param>
        /// <returns>Viewable entities search result</returns>
        [HttpPost]
        [Route("data")]
        [ResponseType(typeof(ExportableSearchResult))]
        [CheckPermission(Permission = ExportPredefinedPermissions.Access)]
        public IHttpActionResult GetData([FromBody]ExportDataRequest request)
        {

            if (!_securityService.UserHasAnyPermission(User.Identity.Name, null, request.ExportTypeName + "ExportDataPolicy"))
            {
                return Unauthorized();
            }

            //var authorizationResult = await _authorizationService.AuthorizeAsync(User, request, request.ExportTypeName + "ExportDataPolicy");
            //if (!authorizationResult.Succeeded)
            //{
            //    return Unauthorized();
            //}

            var exportedTypeDefinition = _knownExportTypesResolver.ResolveExportedTypeDefinition(request.ExportTypeName);
            var pagedDataSource = exportedTypeDefinition.ExportedDataSourceFactory(request.DataQuery);

            pagedDataSource.Fetch();
            var queryResult = pagedDataSource.Items;
            var result = new ExportableSearchResult()
            {
                TotalCount = pagedDataSource.GetTotalCount(),
                Results = queryResult.ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// Starts export task
        /// </summary>
        /// <param name="request">Export task description</param>
        /// <returns>Export task id</returns>
        [HttpPost]
        [Route("run")]
        [CheckPermission(Permission = ExportPredefinedPermissions.Access)]
        //[ResponseType(typeof(PlatformExportPushNotification))]
        public IHttpActionResult RunExport([FromBody]ExportDataRequest request)
        {

            if (!_securityService.UserHasAnyPermission(User.Identity.Name, null, request.ExportTypeName + "ExportDataPolicy"))
            {
                return Unauthorized();
            }

            //var authorizationResult = await _authorizationService.AuthorizeAsync(User, request, request.ExportTypeName + "ExportDataPolicy");
            //if (!authorizationResult.Succeeded)
            //{
            //    return Unauthorized();
            //}


            var user = User;
            var notification = new ExportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = $"{request.ExportTypeName} export task",
                Description = "starting export...."
            };
            //_pushNotificationManager.Send(notification);

            var jobId = BackgroundJob.Enqueue<ExportJob>(x => x.ExportBackground(request, notification, JobCancellationToken.Null, null));
            notification.JobId = jobId;

            return Ok(notification);
        }

        /// <summary>
        /// Attempts to cancel export task
        /// </summary>
        /// <param name="cancellationRequest">Cancellation request with task id</param>
        /// <returns></returns>
        [HttpPost]
        [Route("task/cancel")]
        [CheckPermission(Permission = ExportPredefinedPermissions.Access)]
        public IHttpActionResult CancelExport([FromBody]ExportCancellationRequest cancellationRequest)
        {
            BackgroundJob.Delete(cancellationRequest.JobId);
            return Ok();
        }

        /// <summary>
        /// Downloads file by its name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("download/{fileName}")]
        //[Authorize(ModuleConstants.Security.Permissions.Download)]
        public HttpResponseMessage DownloadExportFile([FromUri] string fileName)
        {
            var localTmpFolder = HostingEnvironment.MapPath(_defaultExportFolder);
            var localPath = Path.Combine(localTmpFolder, Path.GetFileName(fileName));

            var stream = File.Open(localPath, FileMode.Open);
            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(localPath));
            return result;
        }
    }
}
