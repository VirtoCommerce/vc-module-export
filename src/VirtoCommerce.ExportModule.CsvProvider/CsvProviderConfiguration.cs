using System.Globalization;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using VirtoCommerce.ExportModule.Core.Model;

namespace VirtoCommerce.ExportModule.CsvProvider
{
    public class CsvProviderConfiguration : BasicProviderConfiguration
    {
        [JsonIgnore]
        public Configuration Configuration { get; set; } = new Configuration(cultureInfo: CultureInfo.InvariantCulture);

        public string Delimiter
        {
            get
            {
                return Configuration.Delimiter;
            }
            set
            {
                Configuration.Delimiter = value;
            }
        }
    }
}
