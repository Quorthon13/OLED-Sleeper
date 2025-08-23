// File: Converters/MultiValueConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    // Refactored: New converter to pass multiple values (width, height) to a command.
    public class MultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}