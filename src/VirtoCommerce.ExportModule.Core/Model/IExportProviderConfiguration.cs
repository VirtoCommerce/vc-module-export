namespace VirtoCommerce.ExportModule.Core.Model
{
    public interface IExportProviderConfiguration
    {
        /// <summary>
        /// Type discriminator to instantiate proper descendant (a.e. thru the universal PolymorphJsonConverter)
        /// </summary>
        public string Type { get; set; }
    }
}
