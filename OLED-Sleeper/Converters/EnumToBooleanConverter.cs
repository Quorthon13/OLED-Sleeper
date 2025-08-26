using System.Globalization;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    /// <summary>
    /// Converts between enum values and booleans for binding RadioButtons to enum properties in WPF.
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts an enum value to a boolean for RadioButton binding.
        /// </summary>
        /// <param name="value">The enum value from the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The enum value to compare against (as string).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if the enum value matches the parameter; otherwise, false.</returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var enumValue = value.ToString();
            var targetValue = parameter.ToString();
            return enumValue != null && enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Converts a boolean back to an enum value for RadioButton binding.
        /// </summary>
        /// <param name="value">The boolean value from the binding target.</param>
        /// <param name="targetType">The type to convert to (the enum type).</param>
        /// <param name="parameter">The enum value to return if true (as string).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The enum value if true; otherwise, null.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool b || !b || parameter == null)
                return null;

            return Enum.Parse(targetType, parameter.ToString() ?? string.Empty);
        }
    }
}