namespace VirtoCommerce.ExportModule.Core.Security
{
    public interface IExportSecurityHandler
    {
        bool Authorize(string userName, object resource);
    }
}
