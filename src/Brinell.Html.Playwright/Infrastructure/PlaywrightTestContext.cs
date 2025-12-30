using System.Diagnostics;
using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Logging;
using Brinell.Core.Screenshots;
using TestPlatform = Brinell.Core.Abstractions.Platform;

namespace Brinell.Html.Playwright.Infrastructure;

/// <summary>
/// Playwright test context implementation for web UI testing.
/// </summary>
public class PlaywrightTestContext : ITestContext
{
    private readonly Action<string>? _consoleLogger;
    private readonly PlaywrightDriverAdapter _driver;
    private readonly IScreenshotService _screenshotService;

    public string TestName { get; set; } = "Unknown";
    public TestPlatform Platform => TestPlatform.Web;

    public int DefaultTimeoutMs { get; init; } = 10000;
    public int ShortTimeoutMs { get; init; } = 2000;
    public int PollingIntervalMs { get; init; } = 100;

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
    /// The underlying Playwright driver adapter.
    /// </summary>
    public PlaywrightDriverAdapter Driver => _driver;

    /// <summary>
    /// The underlying Playwright page.
    /// </summary>
    public IPage Page => _driver.Page;

    /// <summary>
    /// Create a Playwright test context.
    /// </summary>
    public PlaywrightTestContext(PlaywrightDriverAdapter driver, Action<string>? logger = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _consoleLogger = logger;
        _screenshotService = new PlaywrightScreenshotService(() => _driver.Page);
    }

    /// <summary>
    /// Create a Playwright test context with CSV logging.
    /// </summary>
    public PlaywrightTestContext(PlaywrightDriverAdapter driver, ITestLogger csvLogger, Action<string>? consoleLogger = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Logger = csvLogger;
        _consoleLogger = consoleLogger;
        _screenshotService = new PlaywrightScreenshotService(() => _driver.Page);
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

    /// <summary>
    /// Wait for a condition asynchronously (preferred in Playwright).
    /// </summary>
    public async Task<bool> WaitForAsync(Func<Task<bool>> condition, int? timeoutMs = null, string description = "condition")
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var stopwatch = Stopwatch.StartNew();

        Log($"Waiting for: {description} (timeout: {timeout}ms)");

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            try
            {
                if (await condition())
                {
                    Log($"Condition met: {description} (elapsed: {stopwatch.ElapsedMilliseconds}ms)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Condition check threw: {ex.GetType().Name} - continuing to poll");
            }

            await Task.Delay(PollingIntervalMs);
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

    /// <summary>
    /// Click element asynchronously.
    /// </summary>
    public async Task ClickElementAsync(string automationId)
    {
        var element = await _driver.FindElementAsync(automationId);
        if (element != null)
        {
            await _driver.ClickAsync(element);
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

    /// <summary>
    /// Enter text asynchronously (uses Playwright's fill).
    /// </summary>
    public async Task EnterTextAsync(string automationId, string text)
    {
        var element = await _driver.FindElementAsync(automationId);
        if (element != null)
        {
            await _driver.FillAsync(element, text);
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
            var screenshotBytes = _driver.TakeScreenshot();
            if (screenshotBytes == null)
            {
                Log("Screenshot not supported by this driver");
                return null;
            }

            var fileName = $"{TestName}_{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var screenshotDir = Path.Combine(Path.GetTempPath(), "OraveyUITests");
            Directory.CreateDirectory(screenshotDir);
            var path = Path.Combine(screenshotDir, fileName);
            File.WriteAllBytes(path, screenshotBytes);
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
    /// Navigate to a URL.
    /// </summary>
    public void NavigateTo(string url)
    {
        Log($"Navigating to: {url}");
        _driver.NavigateTo(url);
    }

    /// <summary>
    /// Navigate to a URL asynchronously.
    /// </summary>
    public async Task NavigateToAsync(string url)
    {
        Log($"Navigating to: {url}");
        await _driver.NavigateToAsync(url);
    }

    /// <summary>
    /// Get the current URL.
    /// </summary>
    public string GetCurrentUrl()
    {
        return _driver.GetCurrentUrl();
    }

    /// <summary>
    /// Get the page title.
    /// </summary>
    public string GetTitle()
    {
        return _driver.GetTitle();
    }

    /// <summary>
    /// Refresh the current page.
    /// </summary>
    public void Refresh()
    {
        Log("Refreshing page");
        _driver.Refresh();
    }

    /// <summary>
    /// Execute JavaScript.
    /// </summary>
    public object? ExecuteScript(string script, params object[] args)
    {
        return _driver.ExecuteScript(script, args);
    }

    /// <summary>
    /// Execute JavaScript asynchronously.
    /// </summary>
    public async Task<T?> ExecuteScriptAsync<T>(string script, params object[] args)
    {
        return await _driver.ExecuteScriptAsync<T>(script, args);
    }

    /// <summary>
    /// Wait for page to finish loading.
    /// </summary>
    public async Task WaitForLoadStateAsync(LoadState? state = null)
    {
        await _driver.WaitForLoadStateAsync(state);
    }

    /// <summary>
    /// Start tracing for debugging.
    /// </summary>
    public async Task StartTracingAsync(string name)
    {
        await _driver.BrowserContext.Tracing.StartAsync(new TracingStartOptions
        {
            Name = name,
            Screenshots = true,
            Snapshots = true
        });
    }

    /// <summary>
    /// Stop tracing and save to file.
    /// </summary>
    public async Task StopTracingAsync(string path)
    {
        await _driver.BrowserContext.Tracing.StopAsync(new TracingStopOptions
        {
            Path = path
        });
    }

    /// <summary>
    /// Mock a route/URL for network interception.
    /// </summary>
    public async Task MockRouteAsync(string urlPattern, Func<IRoute, Task> handler)
    {
        await _driver.Page.RouteAsync(urlPattern, handler);
    }
}
