using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Tests.ControlObject6.Mocks;

/// <summary>
/// Testable version of MauiTestContext that uses an IAppiumDriverWrapper
/// instead of directly using AppiumDriver.
/// This allows unit testing without requiring a real Appium driver.
/// </summary>
public class TestableMauiTestContext : ITestContext
{
    private readonly IAppiumDriverWrapper _driverWrapper;

    /// <inheritdoc />
    public int DefaultTimeoutMs { get; set; } = 30000;

    /// <inheritdoc />
    public int DefaultPollingIntervalMs { get; set; } = 100;

    /// <inheritdoc />
    public IPageObject? CurrentPage { get; private set; }

    /// <summary>
    /// Gets the underlying driver wrapper.
    /// </summary>
    public IAppiumDriverWrapper DriverWrapper => _driverWrapper;

    /// <summary>
    /// Creates a new testable MAUI test context with the specified driver wrapper.
    /// </summary>
    public TestableMauiTestContext(IAppiumDriverWrapper driverWrapper)
    {
        _driverWrapper = driverWrapper ?? throw new ArgumentNullException(nameof(driverWrapper));
    }

    /// <inheritdoc />
    public void NavigateTo(string? route, int? timeoutMs = null)
    {
        if (route is null) return;
        _driverWrapper.Navigate().GoToUrl(route);
    }

    /// <inheritdoc />
    public TPage NavigateTo<TPage>(int? timeoutMs = null) where TPage : IPageObject
    {
        throw new NotImplementedException("Page navigation not supported in testable context");
    }

    /// <inheritdoc />
    public void TakeScreenshot(string? filename)
    {
        if (filename is null) return;
        
        var screenshot = _driverWrapper.GetScreenshot();
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        // Note: In tests, we don't actually save the file
        // screenshot.SaveAsFile(path);
    }

    /// <inheritdoc />
    public void Log(string? message)
    {
        if (message is null) return;
        Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} - {message}");
    }

    /// <inheritdoc />
    public void LogError(string? message)
    {
        if (message is null) return;
        Console.Error.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}");
    }

    /// <summary>
    /// Finds an element using the given locator.
    /// </summary>
    public IWebElement FindElement(By by) => _driverWrapper.FindElement(by);
}
