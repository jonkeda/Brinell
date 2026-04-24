using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Converters;

public sealed class LogLevelToBrushConverter : IValueConverter
{
    private static readonly Brush DebugBrush = new SolidColorBrush(Colors.Gray);
    private static readonly Brush InfoBrush = new SolidColorBrush(Colors.DarkBlue);
    private static readonly Brush WarningBrush = new SolidColorBrush(Colors.DarkOrange);
    private static readonly Brush ErrorBrush = new SolidColorBrush(Colors.Red);
    private static readonly Brush DefaultBrush = new SolidColorBrush(Colors.Black);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is LogLevel level ? level switch
        {
            LogLevel.Trace or LogLevel.Debug => DebugBrush,
            LogLevel.Information => InfoBrush,
            LogLevel.Warning => WarningBrush,
            LogLevel.Error or LogLevel.Critical => ErrorBrush,
            _ => DefaultBrush
        } : DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
