using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLED_Sleeper.UI.Converters
{
    /// <summary>
    /// Converts two double values (left, top) into a <see cref="Thickness"/> for margin binding in WPF.
    /// </summary>
    public class LeftTopToMarginConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts an array of two doubles (left, top) to a <see cref="Thickness"/>.
        /// </summary>
        /// <param name="values">Array containing left and top values.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A <see cref="Thickness"/> with left and top set, or (0,0,0,0) if invalid.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double left && values[1] is double top)
            {
                return new Thickness(left, top, 0, 0);
            }
            return new Thickness(0);
        }

        /// <summary>
        /// Not implemented. Throws <see cref="NotImplementedException"/>.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}