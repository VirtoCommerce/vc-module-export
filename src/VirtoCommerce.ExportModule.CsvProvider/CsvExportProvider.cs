using System;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;

namespace VirtoCommerce.ExportModule.CsvProvider
{
    public sealed class CsvExportProvider : IExportProvider
    {
        public string TypeName => nameof(CsvExportProvider);
        public ExportedTypePropertyInfo[] IncludedProperties { get; private set; }
        public string ExportedFileExtension => "csv";
        public bool IsTabular => true;

        [JsonIgnore]
        public IExportProviderConfiguration Configuration { get; }

        private CsvWriter _csvWriter;

        public CsvExportProvider(ExportDataRequest exportDataRequest)
        {
            if (exportDataRequest == null)
            {
                ArgumentNullException.ThrowIfNull(nameof(exportDataRequest));
            }

            Configuration = exportDataRequest.ProviderConfig as CsvProviderConfiguration ?? new CsvProviderConfiguration();
            IncludedProperties = exportDataRequest.DataQuery?.IncludedProperties;
        }

        public void WriteRecord(TextWriter writer, IExportable objectToRecord)
        {
            EnsureWriterCreated(writer);

            AddClassMap(objectToRecord.GetType());

            _csvWriter.WriteRecords(new object[] { objectToRecord });
        }

        public void Dispose()
        {
            _csvWriter?.Flush();
            _csvWriter?.Dispose();
        }


        private void EnsureWriterCreated(TextWriter textWriter)
        {
            if (_csvWriter == null)
            {
                var csvProviderConfiguration = (Configuration as CsvProviderConfiguration);
                var csvConfiguration = new CsvConfiguration(cultureInfo: CultureInfo.InvariantCulture)
                {
                    Delimiter = csvProviderConfiguration.Delimiter,
                    Encoding = Encoding.GetEncoding(csvProviderConfiguration.Encoding),
                };

                _csvWriter = new CsvWriter(textWriter, csvConfiguration, leaveOpen: true);
            }
        }

        private void AddClassMap(Type objectType)
        {
            var csvContext = _csvWriter.Context;
            var mapForType = csvContext.Maps[objectType];

            if (mapForType == null)
            {
                var constructor = typeof(MetadataFilteredMap<>).MakeGenericType(objectType).GetConstructor(IncludedProperties != null
                    ? [typeof(ExportedTypePropertyInfo[])]
                    : Array.Empty<Type>());
                var classMap = (ClassMap)constructor.Invoke(IncludedProperties != null ? new[] { IncludedProperties } : null);

                _csvWriter.Context.RegisterClassMap(classMap);
            }
        }
    }
}
