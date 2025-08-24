// File: /Converters/EqualityToBooleanConverter.cs

using System.Globalization;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    public class EqualityToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString()?.Equals(parameter?.ToString(), StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? parameter : Binding.DoNothing;
        }
    }
}