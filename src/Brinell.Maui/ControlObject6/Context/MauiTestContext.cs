using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Controls;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Context;

/// <summary>
/// Test context for MAUI/Appium-based UI tests.
/// Manages the Appium driver and provides configuration.
/// </summary>
/// <remarks>
/// Control creation uses the 'new' pattern directly in PageObjects:
/// <code>
/// var button = new ButtonControl(context, "SubmitBtn", page);
/// var entry = new EntryControl(context, "Username", page);
/// </code>
/// </remarks>
public class MauiTestContext : ITestContext
{
    private readonly AppiumDriver _driver;

    /// <inheritdoc />
    public int DefaultTimeoutMs { get; set; } = 30000;

    /// <inheritdoc />
    public int DefaultPollingIntervalMs { get; set; } = 100;

    /// <inheritdoc />
    public IPageObject? CurrentPage { get; private set; }

    /// <summary>
    /// Gets the underlying Appium driver.
    /// </summary>
    public AppiumDriver Driver => _driver;

    /// <summary>
    /// Creates a new MAUI test context with the specified driver.
    /// </summary>
    /// <param name="driver">The Appium driver to use.</param>
    public MauiTestContext(AppiumDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <inheritdoc />
    public void NavigateTo(string? route, int? timeoutMs = null)
    {
        if (route is null) return;

        // For MAUI apps, navigation is typically done via app URLs or deep links
        _driver.Navigate().GoToUrl(route);
    }

    /// <inheritdoc />
    public TPage NavigateTo<TPage>(int? timeoutMs = null) where TPage : IPageObject
    {
        var page = CreatePage<TPage>();
        CurrentPage = page;
        page.WaitLoaded(true, timeoutMs ?? DefaultTimeoutMs);
        return page;
    }

    /// <inheritdoc />
    public void TakeScreenshot(string? filename)
    {
        if (filename is null) return;

        var screenshot = _driver.GetScreenshot();
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        screenshot.SaveAsFile(path);
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
    /// Creates a page object of the specified type.
    /// </summary>
    public TPage CreatePage<TPage>() where TPage : IPageObject
    {
        return (TPage)Activator.CreateInstance(typeof(TPage), this)!;
    }
}
