using System.Web.Http;
using VirtoCommerce.ExportModule.Core;
using VirtoCommerce.Platform.Core.Web.Security;

namespace VirtoCommerce.ExportModule.Web.Controllers.Api
{
    [RoutePrefix("api/VirtoCommerceExportModule")]
    public class VirtoCommerceExportModuleController : ApiController
    {
        // GET: api/VirtoCommerceExportModule
        [HttpGet]
        [Route("")]
        [CheckPermission(Permission = ModuleConstants.Security.Permissions.Read)]
        public IHttpActionResult Get()
        {
            return Ok(new { result = "Hello world!" });
        }
    }
}
