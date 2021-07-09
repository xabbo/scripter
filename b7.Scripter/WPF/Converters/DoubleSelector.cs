using System;
using System.Globalization;
using System.Windows.Data;

namespace b7.Scripter.WPF.Converters
{
    [ValueConversion(typeof(bool), typeof(double))]
    public class DoubleSelector : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) return ((bool)value) ? 1 : 0;

            string[] split = ((string)parameter).Split(',');
            if (split.Length != 2 ||
                !double.TryParse(split[0], out double trueValue) ||
                !double.TryParse(split[1], out double falseValue))
            {
                throw new FormatException("Invalid parameter");
            }

            return ((bool)value) ? trueValue : falseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
