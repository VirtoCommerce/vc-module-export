using VirtoCommerce.ExportModule.Core.Model;

namespace VirtoCommerce.ExportModule.JsonProvider
{
    /// <summary>
    /// Configuration for json export provider
    /// </summary>
    public class JsonProviderConfiguration : ExportProviderConfigurationBase
    {

        /// <summary>
        /// Causes child objects to be indented
        /// </summary>
        public bool Indented { get; set; } = false;
    }
}
