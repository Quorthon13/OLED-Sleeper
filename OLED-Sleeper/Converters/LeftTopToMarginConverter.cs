// File: Converters/LeftTopToMarginConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLED_Sleeper.Converters
{
    public class LeftTopToMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double left && values[1] is double top)
            {
                return new Thickness(left, top, 0, 0);
            }
            return new Thickness(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}