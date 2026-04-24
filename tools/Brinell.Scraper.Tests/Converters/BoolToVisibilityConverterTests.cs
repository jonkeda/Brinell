using System.Globalization;
using System.Windows;
using Brinell.Scraper.Converters;
using Xunit;

namespace Brinell.Scraper.Tests.Converters;

#pragma warning disable CS8625
public sealed class BoolToVisibilityConverterTests
{
    [Fact]
    public void BoolToVisibility_True_ReturnsVisible()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void BoolToVisibility_False_ReturnsCollapsed()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void InverseBoolToVisibility_True_ReturnsCollapsed()
    {
        var converter = new InverseBoolToVisibilityConverter();

        var result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void InverseBoolToVisibility_False_ReturnsVisible()
    {
        var converter = new InverseBoolToVisibilityConverter();

        var result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, result);
    }
}
