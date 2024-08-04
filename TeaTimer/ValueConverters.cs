using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;

namespace TeaTimer
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
