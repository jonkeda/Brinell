using System.Globalization;
using System.Windows.Media;
using Brinell.Scraper.Converters;
using Brinell.Scraper.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Brinell.Scraper.Tests.Converters;

public sealed class LogLevelToBrushConverterTests : IClassFixture<StaThreadFixture>
{
    private readonly StaThreadFixture _sta;

    public LogLevelToBrushConverterTests(StaThreadFixture sta) => _sta = sta;

    [Fact]
    public void Debug_ReturnsGrayBrush()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();
            var brush = (SolidColorBrush)converter.Convert(LogLevel.Debug, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(Colors.Gray, brush.Color);
        });
    }

    [Fact]
    public void Trace_ReturnsGrayBrush()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();
            var brush = (SolidColorBrush)converter.Convert(LogLevel.Trace, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(Colors.Gray, brush.Color);
        });
    }

    [Fact]
    public void Information_ReturnsDarkBlueBrush()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();
            var brush = (SolidColorBrush)converter.Convert(LogLevel.Information, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(Colors.DarkBlue, brush.Color);
        });
    }

    [Fact]
    public void Warning_ReturnsDarkOrangeBrush()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();
            var brush = (SolidColorBrush)converter.Convert(LogLevel.Warning, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(Colors.DarkOrange, brush.Color);
        });
    }

    [Fact]
    public void Error_ReturnsRedBrush()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();
            var brush = (SolidColorBrush)converter.Convert(LogLevel.Error, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(Colors.Red, brush.Color);
        });
    }

    [Fact]
    public void Critical_ReturnsRedBrush()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();
            var brush = (SolidColorBrush)converter.Convert(LogLevel.Critical, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(Colors.Red, brush.Color);
        });
    }

    [Fact]
    public void InvalidValue_ReturnsDefaultBrush()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();
            var brush = (SolidColorBrush)converter.Convert("not a log level", typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(Colors.Black, brush.Color);
        });
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        _sta.Run(() =>
        {
            var converter = new LogLevelToBrushConverter();

            Assert.Throws<NotSupportedException>(() =>
                converter.ConvertBack(null!, typeof(LogLevel), null!, CultureInfo.InvariantCulture));
        });
    }
}
