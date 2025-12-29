namespace Brinell.Core.Screenshots;

/// <summary>
/// Technology-agnostic screenshot capture service.
/// Each UI testing technology (WPF/FlaUI, MAUI/Appium, Web/Selenium)
/// provides its own implementation.
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// Capture a screenshot of the test window only (not the entire desktop).
    /// </summary>
    /// <returns>PNG image data, or empty array if capture fails.</returns>
    byte[] CaptureWindow();
    
    /// <summary>
    /// Save screenshot to the test results folder.
    /// </summary>
    /// <param name="imageData">PNG image data.</param>
    /// <param name="testName">Full test name (class.method).</param>
    /// <param name="suffix">Descriptive suffix (e.g., "failure", "exception").</param>
    /// <returns>The saved file path, or empty string if save fails.</returns>
    string SaveScreenshot(byte[] imageData, string testName, string suffix);
    
    /// <summary>
    /// Get the configured screenshot output directory.
    /// </summary>
    string ScreenshotDirectory { get; }
}
