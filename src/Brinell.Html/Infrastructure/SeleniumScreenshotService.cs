using OpenQA.Selenium;
using Brinell.Core.Screenshots;

namespace Brinell.Html.Infrastructure;

/// <summary>
/// Selenium/Web-specific screenshot capture service.
/// Captures only the browser window, not the entire desktop.
/// </summary>
public class SeleniumScreenshotService : ScreenshotServiceBase
{
    private readonly Func<IWebDriver?> _driverProvider;
    
    /// <summary>
    /// Create a Selenium screenshot service.
    /// </summary>
    /// <param name="driverProvider">Function that returns the current WebDriver.</param>
    /// <param name="outputDirectory">Optional output directory override.</param>
    public SeleniumScreenshotService(Func<IWebDriver?> driverProvider, string? outputDirectory = null)
        : base(outputDirectory)
    {
        _driverProvider = driverProvider ?? throw new ArgumentNullException(nameof(driverProvider));
    }
    
    /// <inheritdoc />
    public override byte[] CaptureWindow()
    {
        try
        {
            var driver = _driverProvider();
            if (driver == null)
                return [];
            
            // Selenium's GetScreenshot captures the browser window/viewport
            if (driver is ITakesScreenshot screenshotDriver)
            {
                var screenshot = screenshotDriver.GetScreenshot();
                return screenshot.AsByteArray;
            }
            
            return [];
        }
        catch
        {
            return [];
        }
    }
}
