// File: Converters/NullToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    /// <summary>
    /// Converts null values to <see cref="Visibility"/> for WPF UI elements.
    /// If <see cref="IsReversed"/> is true, the logic is inverted.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether the conversion logic is reversed.
        /// </summary>
        public bool IsReversed { get; set; }

        /// <summary>
        /// Converts a value to <see cref="Visibility.Visible"/> if null, otherwise <see cref="Visibility.Collapsed"/>.
        /// If <see cref="IsReversed"/> is true, the logic is inverted.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Visibility.Visible or Visibility.Collapsed based on the value and IsReversed.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            if (IsReversed)
            {
                isNull = !isNull;
            }
            return isNull ? Visibility.Visible : Visibility.Collapsed;
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