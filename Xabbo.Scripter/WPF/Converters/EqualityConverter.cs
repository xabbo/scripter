using System;
using System.Globalization;
using System.Windows.Data;

namespace Xabbo.Scripter.WPF.Converters
{
    [ValueConversion(typeof(object), typeof(bool))]
    internal class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return parameter is null;
            else return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
