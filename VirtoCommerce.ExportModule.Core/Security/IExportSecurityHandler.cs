using VirtoCommerce.ExportModule.Core.Model;

namespace VirtoCommerce.ExportModule.Core.Security
{
    public interface IExportSecurityHandler
    {
        bool Authorize(string userName, ExportDataQuery dataQuery);
    }
}
