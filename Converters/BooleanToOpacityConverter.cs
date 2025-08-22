// File: /Converters/BooleanToOpacityConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    public class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the value is true, return 1.0 (fully opaque)
            // If the value is false, return 0.5 (semi-transparent)
            return (value is bool b && b) ? 1.0 : 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}