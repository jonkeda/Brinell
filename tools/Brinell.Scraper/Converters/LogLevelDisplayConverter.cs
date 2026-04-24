using System.Globalization;
using System.Windows.Data;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Converters;

public sealed class LogLevelDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is LogLevel level ? level switch
        {
            LogLevel.Trace => "All",
            _ => level.ToString()
        } : value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
