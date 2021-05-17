using System;
using CsvHelper.Configuration;

namespace VirtoCommerce.ExportModule.CsvProvider
{
    public static class CsvHelperExtensions
    {
        public static MemberMap UsingExpression<T>(this MemberMap map, Func<string, T> readExpression,
            Func<T, string> writeExpression)
        {
            return map.TypeConverter(new ExpressionConverter<T>(readExpression, writeExpression));
        }
    }
}
