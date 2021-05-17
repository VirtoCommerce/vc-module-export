using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CsvHelper.Configuration;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Data.Extensions;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ExportModule.CsvProvider
{
    /// <summary>
    /// Custom ClassMap implementation which includes type properties. Supports nested properties.
    /// Does not map <see cref="IEnumerable{Entity}"/> as these are not representable in CSV structure in suitable manner.
    /// </summary>
    /// <typeparam name="T">Mapped type.</typeparam>
    public class MetadataFilteredMap<T> : ClassMap<T>
    {
        public MetadataFilteredMap() : this(null)
        { }

        public MetadataFilteredMap(ExportedTypePropertyInfo[] includedProperties)
        {
            var exportedType = typeof(T);
            var dynamicPropertiesInfos = includedProperties.Where(x => x.IsProperty).ToArray();
            var usualProperties = includedProperties.Except(dynamicPropertiesInfos).ToArray();
            var includedPropertiesInfo = usualProperties ?? exportedType.GetPropertyNames().PropertyInfos;
            var columnIndex = 0;

            ClassMap currentClassMap = null;

            foreach (var includedPropertyInfo in includedPropertiesInfo)
            {
                var propertyNames = includedPropertyInfo.FullName.Split('.');
                var currentType = exportedType;
                currentClassMap = this;

                for (int i = 0; i < propertyNames.Length; i++)
                {
                    var propertyName = propertyNames[i];
                    var propertyInfo = currentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                    if (propertyInfo?.CanRead != true)
                    {
                        break;
                    }

                    // Do not write enumerables (ICollection, etc.)
                    if (IsEnumerableEntityProperty(propertyInfo))
                    {
                        break;
                    }

                    // Add memberMap
                    if (i == propertyNames.Length - 1)
                    {
                        var memberMap = CreateMemberMap(currentType, propertyInfo, includedPropertyInfo.DisplayName, ref columnIndex);

                        currentClassMap.MemberMaps.Add(memberMap);
                        currentClassMap = this;
                    }
                    // Working with nested properties - create or get References
                    else
                    {
                        var referenceMap = currentClassMap.ReferenceMaps.Find(propertyInfo);

                        currentType = propertyInfo.PropertyType;

                        if (referenceMap == null)
                        {
                            var referenceClassMapType = typeof(EmptyClassMapImpl<>).MakeGenericType(new[] { currentType });

                            referenceMap = new MemberReferenceMap(propertyInfo, (ClassMap)Activator.CreateInstance(referenceClassMapType));
                            currentClassMap.ReferenceMaps.Add(referenceMap);
                        }

                        currentClassMap = referenceMap.Data.Mapping;
                    }

                }
            }


            // For Dyn properties
            if (dynamicPropertiesInfos.Any() && IsIHasProperties(exportedType))
            {
                currentClassMap = this;

                // Exporting multiple csv fields from the same property (which is a collection)
                foreach (var propertyCsvColumn in dynamicPropertiesInfos)
                {
                    // create CsvPropertyMap manually, because this.Map(x =>...) does not allow
                    // to export multiple entries for the same property

                    var propertyValuesInfo = exportedType.GetProperty(nameof(IHasProperties.Properties));
                    var csvPropertyMap = MemberMap.CreateGeneric(exportedType, propertyValuesInfo);
                    csvPropertyMap.Name(propertyCsvColumn.FullName);

                    csvPropertyMap.Data.Index = ++columnIndex;

                    // create custom converter instance which will get the required record from the collection
                    csvPropertyMap.UsingExpression<ICollection<Property>>(null, properties =>
                    {
                        var property = properties.FirstOrDefault(x => x.Name == propertyCsvColumn.FullName && x.Values.Any());
                        var propertyValues = Array.Empty<string>();
                        if (property != null)
                        {
                            if (property.Dictionary)
                            {
                                propertyValues = property.Values
                                    ?.Where(x => !string.IsNullOrEmpty(x.Alias))
                                    .Select(x => x.Alias)
                                    .Distinct()
                                    .ToArray();
                            }
                            else
                            {
                                propertyValues = property.Values
                                    ?.Where(x => x.Value != null || x.Alias != null)
                                    .Select(x => x.Alias ?? x.Value.ToString())
                                    .ToArray();
                            }
                        }

                        return string.Join(',', propertyValues);
                    });

                    currentClassMap.MemberMaps.Add(csvPropertyMap);
                }
            }
        }


        private static bool IsEnumerableEntityProperty(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.PropertyType;

            return type.GetInterfaces().Any(x =>
                x.IsGenericType
                && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                && type.GetGenericArguments().Any(x => x.IsSubclassOf(typeof(Entity)));
        }

        private static bool IsIHasProperties(Type type)
        {
            return type.GetInterfaces().Contains(typeof(IHasProperties));
        }

        private static MemberMap CreateMemberMap(Type currentType, PropertyInfo propertyInfo, string columnName, ref int columnIndex)
        {
            var memberMap = MemberMap.CreateGeneric(currentType, propertyInfo);

            memberMap.Data.TypeConverterOptions.CultureInfo = CultureInfo.InvariantCulture;
            memberMap.Data.TypeConverterOptions.NumberStyle = NumberStyles.Any;
            memberMap.Data.TypeConverterOptions.BooleanTrueValues.AddRange(new List<string>() { "yes", "true" });
            memberMap.Data.TypeConverterOptions.BooleanFalseValues.AddRange(new List<string>() { "false", "no" });
            memberMap.Data.Names.Add(columnName);
            memberMap.Data.NameIndex = memberMap.Data.Names.Count - 1;
            memberMap.Data.Index = ++columnIndex;

            return memberMap;
        }
    }
}
