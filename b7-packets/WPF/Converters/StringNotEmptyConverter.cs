using System;
using System.Globalization;
using System.Windows.Data;

namespace b7.Packets.WPF
{
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.IsNullOrWhiteSpace((string)value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
