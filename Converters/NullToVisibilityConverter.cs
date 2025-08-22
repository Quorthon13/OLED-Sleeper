// File: /Converters/NullToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        // This property lets us reverse the converter's logic in XAML
        public bool IsReversed { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Standard behavior: visible if the value is null
            bool isVisible = (value == null);

            // If IsReversed is true, flip the logic
            if (IsReversed)
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}