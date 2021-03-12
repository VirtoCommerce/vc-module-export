using Newtonsoft.Json;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.CsvProvider;

namespace VirtoCommerce.ExportModule.Web.JsonConverters
{
    public class ExportDataRequestCsvProviderConfigJsonConverter : ExportDataRequestProviderConfigJsonConverterBase
    {
        protected override string ProviderName => nameof(CsvExportProvider);

        protected override IExportProviderConfiguration GetProviderConfiguration(JsonReader reader, JsonSerializer serializer)
        {
            return serializer.Deserialize<CsvProviderConfiguration>(reader);
        }
    }
}
