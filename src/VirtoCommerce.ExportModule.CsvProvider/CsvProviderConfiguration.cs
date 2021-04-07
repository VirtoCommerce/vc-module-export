using VirtoCommerce.ExportModule.Core.Model;

namespace VirtoCommerce.ExportModule.CsvProvider
{
    /// <summary>
    /// Configuration for comma separated values export provider
    /// </summary>
    public class CsvProviderConfiguration : ExportProviderConfigurationBase
    {
        /// <summary>
        /// Delimiter of fields
        /// </summary>
        public string Delimiter { get; set; } = ",";

        /// <summary>
        /// Name of the codepage should be used.  
        /// Full list of codepages can be found at https://docs.microsoft.com/en-US/dotnet/api/system.text.encoding?view=netcore-3.1
        /// </summary>
        public string Encoding { get; set; } = System.Text.Encoding.Default.EncodingName;
    }
}
