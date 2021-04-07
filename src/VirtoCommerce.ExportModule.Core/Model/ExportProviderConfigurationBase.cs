namespace VirtoCommerce.ExportModule.Core.Model
{
    /// <summary>
    /// Basic implementation of provider configuration
    /// </summary>
    public abstract class ExportProviderConfigurationBase : IExportProviderConfiguration
    {
        /// <summary>
        /// Default protected constructor 
        /// </summary>
        protected ExportProviderConfigurationBase()
        {
            Type = GetType().Name;

        }
        /// <summary>
        /// Type discriminator to instantiate proper descendant (e.g. thru the universal PolymorphJsonConverter)
        /// </summary>
        public string Type { get; set; }
    }
}
