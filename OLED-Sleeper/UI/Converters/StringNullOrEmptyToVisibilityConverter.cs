using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLED_Sleeper.UI.Converters
{
    /// <summary>
    /// Converts a string to <see cref="Visibility"/> for WPF UI elements.
    /// Returns <see cref="Visibility.Collapsed"/> if the string is null or empty, otherwise <see cref="Visibility.Visible"/>.
    /// </summary>
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a string to <see cref="Visibility"/>.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Visibility.Collapsed if the string is null or empty; otherwise, Visibility.Visible.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Not implemented. Throws <see cref="NotImplementedException"/>.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}