using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ExportModule.Web.JsonConverters
{
    public abstract class ExportDataRequestProviderConfigJsonConverterBase : JsonConverter
    {
        protected abstract string ProviderName { get; }
        private static readonly Type[] _knownTypes = { typeof(ExportDataRequest) };


        public override bool CanWrite => false;
        public override bool CanRead => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            var typeName = objectType.Name;
            var providerName = obj["providerName"]?.Value<string>();

            var result = AbstractTypeFactory<ExportDataRequest>.TryCreateInstance(typeName);
            if (result == null)
            {
                throw new NotSupportedException("Unknown ExportDataRequest type: " + typeName);
            }

            if (!string.IsNullOrEmpty(providerName) && providerName.EqualsInvariant(ProviderName))
            {
                var providerConfig = obj["providerConfig"];

                if (providerConfig != null)
                {
                    result.ProviderConfig = GetProviderConfiguration(providerConfig.CreateReader(), serializer);
                }
            }

            serializer.Populate(obj.CreateReader(), result);

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return _knownTypes.Any(x => x.IsAssignableFrom(objectType));
        }


        protected abstract IExportProviderConfiguration GetProviderConfiguration(JsonReader reader, JsonSerializer serializer);
    }
}
