namespace VirtoCommerce.ExportModule.Core.Model
{
    public class BasicProviderConfiguration : IExportProviderConfiguration
    {
        public BasicProviderConfiguration()
        {
            Type = GetType().Name;

        }
        public string Type { get; set; }
    }
}
