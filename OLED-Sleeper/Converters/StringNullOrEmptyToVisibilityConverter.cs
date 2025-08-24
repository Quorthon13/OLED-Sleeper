using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the string is null or empty (no error), collapse the visibility.
            // Otherwise (there is an error string), make it visible.
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}