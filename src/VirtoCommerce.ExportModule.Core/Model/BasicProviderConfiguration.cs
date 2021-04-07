namespace VirtoCommerce.ExportModule.Core.Model
{
    /// <summary>
    /// Basic implementation of provider configuration
    /// </summary>
    public class BasicProviderConfiguration : IExportProviderConfiguration
    {
        /// <summary>
        /// Basic implementation of provider configuration
        /// </summary>
        public BasicProviderConfiguration()
        {
            Type = GetType().Name;

        }
        /// <summary>
        /// Type discriminator to instantiate proper descendant (a.e. thru the universal PolymorphJsonConverter)
        /// </summary>
        public string Type { get; set; }
    }
}
