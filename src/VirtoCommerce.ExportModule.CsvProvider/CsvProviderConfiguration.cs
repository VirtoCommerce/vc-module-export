using System.Globalization;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using VirtoCommerce.ExportModule.Core.Model;

namespace VirtoCommerce.ExportModule.CsvProvider
{
    public class CsvProviderConfiguration : IExportProviderConfiguration
    {
        [JsonIgnore]
        public Configuration Configuration { get; set; } = new Configuration(cultureInfo: CultureInfo.InvariantCulture);

        public string Delimiter { get; set; }
    }
}
