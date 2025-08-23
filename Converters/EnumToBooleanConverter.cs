using System.Globalization;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    // Refactored: New converter to bind RadioButtons to an enum property.
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var enumValue = value.ToString();
            var targetValue = parameter.ToString();
            return enumValue != null && enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool == false || parameter == null)
                return null;

            if ((bool)value)
            {
                return Enum.Parse(targetType, parameter.ToString() ?? string.Empty);
            }
            return null;
        }
    }
}