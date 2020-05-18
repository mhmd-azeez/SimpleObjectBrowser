using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SimpleObjectBrowser.Xaml
{
    public class BooleanToNumberConverter : IValueConverter
    {
        public int True { get; set; }
        public int False { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? True : False;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int number)
            {
                return number == True;
            }

            return value;
        }
    }
}
