namespace VirtoCommerce.ExportModule.Core.Model
{
    /// <summary>
    /// Incapsulates data required to start export: export type, query for data to export, provider to record
    /// </summary>
    public class ExportDataRequest
    {
        /// <summary>
        /// Full type name of exportable entity
        /// </summary>
        public string ExportTypeName { get; set; }
        /// <summary>
        /// Query information to retrive exported data
        /// </summary>
        public ExportDataQuery DataQuery { get; set; }
        /// <summary>
        /// Export provider configuration
        /// </summary>
        public IExportProviderConfiguration ProviderConfig { get; set; }
        /// <summary>
        /// Selected export provider name
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Export file name pattern
        /// example: export_{0:yyyyMMddHHmmss}
        /// </summary>
        public string ExportFileNameTemplate { get; set; }
    }
}
