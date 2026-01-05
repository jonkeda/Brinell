using System.Diagnostics;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using Brinell.Core.Abstractions;
using Brinell.Core.Logging;
using Brinell.Core.Screenshots;

namespace Brinell.Maui.Infrastructure;

/// <summary>
/// Appium test context implementation for MAUI app UI testing.
/// Implements ITestContext with Appium-specific functionality.
/// </summary>
public class AppiumTestContext : ITestContext, IDisposable
{
    private readonly Action<string>? _consoleLogger;
    private readonly AppiumDriverAdapter _driver;
    private readonly IScreenshotService _screenshotService;
    
    public string TestName { get; set; } = "Unknown";
    
    /// <summary>
    /// Current platform enum value.
    /// </summary>
    public Platform Platform => _driver.Platform switch
    {
        "Windows" => Core.Abstractions.Platform.WindowsMaui,
        "Android" => Core.Abstractions.Platform.Android,
        "iOS" => Core.Abstractions.Platform.iOS,
        _ => Core.Abstractions.Platform.WindowsMaui
    };
    
    /// <summary>
    /// Check if running on a mobile platform.
    /// </summary>
    public bool IsMobile => Platform == Core.Abstractions.Platform.Android 
                            || Platform == Core.Abstractions.Platform.iOS;
    
    // MAUI apps may need longer timeouts than WPF due to app startup and rendering
    public int DefaultTimeoutMs { get; init; } = 15000;
    public int ShortTimeoutMs { get; init; } = 100;
    public int PollingIntervalMs { get; init; } = 200;
    
    /// <summary>
    /// Logger for CSV output. Set this to enable CSV logging.
    /// </summary>
    public ITestLogger? Logger { get; private set; }
    
    /// <summary>
    /// Screenshot service for capturing failure screenshots.
    /// </summary>
    public IScreenshotService Screenshots => _screenshotService;
    
    /// <summary>
    /// Set the CSV logger for this context.
    /// </summary>
    public void SetLogger(ITestLogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// The underlying Appium driver adapter.
    /// </summary>
    public AppiumDriverAdapter Driver => _driver;

    public AppiumTestContext(AppiumDriverAdapter driver, Action<string>? logger = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _consoleLogger = logger;
        _screenshotService = new AppiumScreenshotService(() => _driver.Driver);
    }
    
    public AppiumTestContext(AppiumDriverAdapter driver, ITestLogger csvLogger, Action<string>? consoleLogger = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Logger = csvLogger;
        _consoleLogger = consoleLogger;
        _screenshotService = new AppiumScreenshotService(() => _driver.Driver);
    }

    /// <summary>
    /// Create a test context from options.
    /// </summary>
    public static AppiumTestContext Create(AppiumTestOptions options, Action<string>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var serverUri = new Uri(options.ServerUrl);
        AppiumDriverAdapter driver;

        switch (options.PlatformName.ToUpperInvariant())
        {
            case "ANDROID":
                driver = AppiumDriverAdapter.CreateAndroid(
                    options.AppPath,
                    serverUri,
                    options.DeviceName,
                    options.CommandTimeout);
                break;

            case "IOS":
                driver = AppiumDriverAdapter.CreateiOS(
                    options.AppPath,
                    serverUri,
                    options.DeviceName,
                    options.PlatformVersion,
                    options.CommandTimeout);
                break;

            case "WINDOWS":
            default:
                driver = new AppiumDriverAdapter(options.AppPath, serverUri, options.CommandTimeout);
                break;
        }

        return new AppiumTestContext(driver, logger)
        {
            DefaultTimeoutMs = options.DefaultTimeoutMs
        };
    }
    
    /// <summary>
    /// Capture a failure screenshot. Call this before throwing exceptions.
    /// </summary>
    /// <param name="suffix">Descriptive suffix for the screenshot file (e.g., "page-not-displayed").</param>
    /// <returns>Path to saved screenshot, or empty string if capture failed.</returns>
    public string CaptureFailureScreenshot(string suffix = "failure")
    {
        try
        {
            var imageData = _screenshotService.CaptureWindow();
            if (imageData.Length == 0)
            {
                Log("WARNING: Failed to capture screenshot - no image data");
                return string.Empty;
            }
            
            var path = _screenshotService.SaveScreenshot(imageData, TestName, suffix);
            if (!string.IsNullOrEmpty(path))
            {
                Log($"Screenshot saved: {path}");
            }
            return path;
        }
        catch (Exception ex)
        {
            Log($"WARNING: Failed to capture screenshot: {ex.Message}");
            return string.Empty;
        }
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var formatted = $"[{timestamp}] [{TestName}] {message}";
        _consoleLogger?.Invoke(formatted);
        Debug.WriteLine(formatted);
    }

    public void LogError(Exception ex, string context)
    {
        var innerMsg = ex.InnerException != null
            ? $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
            : "";
        Log($"ERROR [{context}] {ex.GetType().Name}: {ex.Message}{innerMsg}");
        
        // Also log to CSV logger
        Logger?.LogError(TestName, context, "", "Error", ex);
    }

    public bool WaitFor(Func<bool> condition, int? timeoutMs = null, string description = "condition")
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var stopwatch = Stopwatch.StartNew();

        Log($"Waiting for: {description} (timeout: {timeout}ms)");

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            try
            {
                if (condition())
                {
                    Log($"Condition met: {description} (elapsed: {stopwatch.ElapsedMilliseconds}ms)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Condition check threw: {ex.GetType().Name} - continuing to poll");
            }

            Thread.Sleep(PollingIntervalMs);
        }

        Log($"Timeout waiting for: {description} (elapsed: {stopwatch.ElapsedMilliseconds}ms)");
        return false;
    }

    public bool ElementExists(string automationId)
    {
        return _driver.FindElement(automationId) != null;
    }

    public bool ElementIsVisible(string automationId)
    {
        var element = _driver.FindElement(automationId);
        return element != null && _driver.IsDisplayed(element);
    }
    
    public bool ElementIsEnabled(string automationId)
    {
        var element = _driver.FindElement(automationId);
        return element != null && _driver.IsEnabled(element);
    }
    
    public string GetElementText(string automationId)
    {
        var element = _driver.FindElement(automationId);
        return element != null ? (_driver.GetText(element) ?? string.Empty) : string.Empty;
    }
    
    public void ClickElement(string automationId)
    {
        var element = _driver.FindElement(automationId);
        if (element != null)
        {
            _driver.Click(element);
        }
        else
        {
            throw new InvalidOperationException($"Element '{automationId}' not found for click operation.");
        }
    }
    
    public void EnterText(string automationId, string text)
    {
        var element = _driver.FindElement(automationId);
        if (element != null)
        {
            _driver.Clear(element);
            _driver.SendKeys(element, text);
        }
        else
        {
            throw new InvalidOperationException($"Element '{automationId}' not found for enter text operation.");
        }
    }
    
    public void ClearElement(string automationId)
    {
        var element = _driver.FindElement(automationId);
        if (element != null)
        {
            _driver.Clear(element);
        }
        else
        {
            throw new InvalidOperationException($"Element '{automationId}' not found for clear operation.");
        }
    }

    public string? TakeScreenshot(string name)
    {
        try
        {
            var screenshot = _driver.Driver.GetScreenshot();
            var fileName = $"{TestName}_{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var screenshotDir = Path.Combine(Path.GetTempPath(), "OraveyUITests", "MAUI");
            Directory.CreateDirectory(screenshotDir);
            var path = Path.Combine(screenshotDir, fileName);
            screenshot.SaveAsFile(path);
            Log($"Screenshot saved: {path}");
            return path;
        }
        catch (Exception ex)
        {
            LogError(ex, "TakeScreenshot");
            return null;
        }
    }

    /// <summary>
    /// Wait for element to exist.
    /// </summary>
    public IElementAdapter? WaitForElement(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? element = null;
        var found = WaitFor(() =>
        {
            element = _driver.FindElement(automationId);
            return element != null;
        }, timeoutMs, $"element '{automationId}'");
        
        return found ? element : null;
    }

    /// <summary>
    /// Wait for element to be visible.
    /// </summary>
    public IElementAdapter? WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? element = null;
        var found = WaitFor(() =>
        {
            element = _driver.FindElement(automationId);
            return element != null && _driver.IsDisplayed(element);
        }, timeoutMs, $"element '{automationId}' visible");
        
        return found ? element : null;
    }

    /// <summary>
    /// Hide the keyboard if visible (mobile platforms).
    /// </summary>
    public void HideKeyboard()
    {
        if (IsMobile)
        {
            try
            {
                _driver.Driver.HideKeyboard();
            }
            catch
            {
                // Ignore - keyboard may already be hidden
            }
        }
    }

    /// <summary>
    /// Find element by Name property (useful for Shell FlyoutItems on Windows).
    /// On Windows MAUI, Shell FlyoutItem's Title becomes the Name property.
    /// </summary>
    /// <param name="name">The Name property value to search for.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if element with given Name exists.</returns>
    public bool ElementExistsByName(string name, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? ShortTimeoutMs;
        var endTime = DateTime.Now.AddMilliseconds(timeout);
        
        while (DateTime.Now < endTime)
        {
            try
            {
                // Use XPath with @Name attribute - By.Name() translates to CSS selector
                // which is not supported by Windows Appium driver
                var element = _driver.Driver.FindElement(
                    OpenQA.Selenium.By.XPath($"//*[@Name='{name}']"));
                if (element != null)
                    return true;
            }
            catch (OpenQA.Selenium.NoSuchElementException)
            {
                // Continue polling
            }
            Thread.Sleep(PollingIntervalMs);
        }
        return false;
    }

    /// <summary>
    /// Click element by Name property (useful for Shell FlyoutItems on Windows).
    /// </summary>
    /// <param name="name">The Name property value to search for.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    public void ClickElementByName(string name, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var endTime = DateTime.Now.AddMilliseconds(timeout);
        
        while (DateTime.Now < endTime)
        {
            try
            {
                // Use XPath with @Name attribute - By.Name() translates to CSS selector
                // which is not supported by Windows Appium driver
                var element = _driver.Driver.FindElement(
                    OpenQA.Selenium.By.XPath($"//*[@Name='{name}']"));
                if (element != null)
                {
                    element.Click();
                    return;
                }
            }
            catch (OpenQA.Selenium.NoSuchElementException)
            {
                // Continue polling
            }
            Thread.Sleep(PollingIntervalMs);
        }
        throw new InvalidOperationException($"Element with Name '{name}' not found.");
    }

    /// <summary>
    /// Find element by XPath query.
    /// </summary>
    /// <param name="xpath">XPath query.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if element matching XPath exists.</returns>
    public bool ElementExistsByXPath(string xpath, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? ShortTimeoutMs;
        var endTime = DateTime.Now.AddMilliseconds(timeout);
        
        while (DateTime.Now < endTime)
        {
            try
            {
                var elements = _driver.Driver.FindElements(OpenQA.Selenium.By.XPath(xpath));
                if (elements.Count > 0)
                    return true;
            }
            catch
            {
                // Continue polling
            }
            Thread.Sleep(PollingIntervalMs);
        }
        return false;
    }

    /// <summary>
    /// Swipe gesture for mobile platforms.
    /// </summary>
    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        if (IsMobile)
        {
            Log($"Swipe from ({startX},{startY}) to ({endX},{endY})");
            // Implementation would use touch actions
        }
    }

    /// <summary>
    /// Dispose the test context and underlying driver.
    /// </summary>
    public void Dispose()
    {
        _driver?.Dispose();
        GC.SuppressFinalize(this);
    }
}
