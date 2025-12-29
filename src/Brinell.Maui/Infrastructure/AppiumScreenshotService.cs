using OpenQA.Selenium.Appium;
using Brinell.Core.Screenshots;

namespace Brinell.Maui.Infrastructure;

/// <summary>
/// Appium/MAUI-specific screenshot capture service.
/// Captures only the app window, not the entire device screen.
/// </summary>
public class AppiumScreenshotService : ScreenshotServiceBase
{
    private readonly Func<AppiumDriver?> _driverProvider;
    
    /// <summary>
    /// Create an Appium screenshot service.
    /// </summary>
    /// <param name="driverProvider">Function that returns the current Appium driver.</param>
    /// <param name="outputDirectory">Optional output directory override.</param>
    public AppiumScreenshotService(Func<AppiumDriver?> driverProvider, string? outputDirectory = null)
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
            
            // Appium's GetScreenshot captures the app window
            var screenshot = driver.GetScreenshot();
            return screenshot.AsByteArray;
        }
        catch
        {
            return [];
        }
    }
}
