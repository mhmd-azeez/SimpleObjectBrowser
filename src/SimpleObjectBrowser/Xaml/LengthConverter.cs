using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SimpleObjectBrowser.Xaml
{
    public class LengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return string.Empty;
            }

            const double kilo = 1024;
            const double mega = kilo * kilo;
            const double giga = mega * kilo;

            if (value is long number)
            {
                if (number > giga)
                {
                    return $"{number / giga:N2} GB";
                }
                else if (number > mega)
                {
                    return $"{number / mega:N2} MB";
                }
                else if (number > kilo)
                {
                    return $"{number / kilo:N2} KB";
                }
                else
                {
                    return $"{number} B";
                }
            }

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
