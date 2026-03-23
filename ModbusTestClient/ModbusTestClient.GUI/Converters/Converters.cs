using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ModbusTestClient.Core.Models;

namespace ModbusTestClient.GUI.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            string param = parameter?.ToString() ?? "connection";
            return param switch
            {
                "connection" => b ? new SolidColorBrush(Color.FromRgb(16, 124, 16))
                                   : new SolidColorBrush(Color.FromRgb(209, 52, 56)),
                "coil" => b ? new SolidColorBrush(Color.FromRgb(16, 124, 16))
                             : new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToConnectionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? "● Connected" : "● Disconnected";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            bool invert = parameter?.ToString() == "invert";
            if (invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Info => new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    LogLevel.Success => new SolidColorBrush(Color.FromRgb(16, 124, 16)),
                    LogLevel.Warning => new SolidColorBrush(Color.FromRgb(200, 150, 0)),
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(209, 52, 56)),
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !(bool)value;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => !(bool)value;
    }
}
