using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Brinell.Samples.Wpf.App.Infrastructure.Converters;

/// <summary>
/// Converts a null/empty value to Visibility. 
/// Returns Visible when value is not null/empty, Collapsed otherwise.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
