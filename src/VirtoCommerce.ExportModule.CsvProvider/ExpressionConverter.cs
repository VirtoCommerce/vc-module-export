using System;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ExportModule.CsvProvider
{
    public class ExpressionConverter<T> : ITypeConverter
    {
        public Func<string, T> InExpression { get; set; }
        public Func<T, string> OutExpression { get; set; }
        public ExpressionConverter(Func<string, T> inExp, Func<T, string> outExp)
        {
            InExpression = inExp;
            OutExpression = outExp;
        }
        public bool CanConvertFrom(Type type)
        {
            return InExpression != null;
        }

        public bool CanConvertTo(Type type)
        {
            return OutExpression != null;
        }

        public string ConvertToString(TypeConverterOptions options, object value)
        {
            return OutExpression((T)value);
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (OutExpression != null)
            {
                return OutExpression((T)value);
            }

            return value.ToString();
        }

        public object ConvertFromString(TypeConverterOptions options, string text)
        {
            return InExpression(text);
        }

        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (text.IsNullOrEmpty())
                return null;

            if (InExpression != null)
            {
                return InExpression(text);
            }

            return null;
        }
    }
}
