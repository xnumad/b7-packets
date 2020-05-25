using System;
using System.Globalization;
using System.Windows.Data;

namespace b7.Packets.WPF
{
    [ValueConversion(typeof(string), typeof(string))]
    public class NewlineEscapeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value)
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
