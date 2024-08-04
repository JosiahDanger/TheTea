using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TheTea
{
    public class EnumToStringConverter : IValueConverter
    {
        public object? Convert
        (
            object? value, Type targetType, object? parameter, CultureInfo culture
        )
        {
            /* TODO: 
             *  Is it considered bad programming practice to use the ToString() method 
             *  for this purpose? */
            return value?.ToString();
        }

        public object? ConvertBack
        (
            object? value, Type targetType, object? parameter, CultureInfo culture
        )
        {
            throw new NotSupportedException();
        }
    }
}
