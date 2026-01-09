using Brinell.Blazor.Controls;
using Brinell.Blazor.Interfaces;
using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using OpenQA.Selenium;

namespace Brinell.Blazor;

/// <summary>
/// Blazor test context implementation. Manages the Selenium WebDriver lifecycle
/// and provides access to configuration, logging, and element finding.
/// </summary>
public class BlazorTestContext : IBlazorTestContext, IDisposable
{
    private bool _disposed;
    private const string ContextName = "BlazorTestContext";

    /// <summary>
    /// Initializes a new instance of the BlazorTestContext class with an existing driver.
    /// </summary>
    /// <param name="driver">The Selenium WebDriver to use.</param>
    /// <param name="baseUrl">The base URL for the Blazor application.</param>
    /// <param name="timeouts">Optional timeout settings. Uses defaults if not specified.</param>
    /// <param name="logger">Optional logger. Uses NullTestLogger if not specified.</param>
    public BlazorTestContext(
        IWebDriver driver,
        string baseUrl,
        TimeoutSettings? timeouts = null,
        ITestLogger? logger = null)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        Timeouts = timeouts ?? TimeoutSettings.Default;
        Logger = logger ?? NullTestLogger.Instance;

        Logger.LogInfo(ContextName, null, $"BlazorTestContext initialized with base URL: {baseUrl}");
    }

    /// <inheritdoc />
    public IWebDriver Driver { get; }

    /// <inheritdoc />
    public string BaseUrl { get; }

    /// <inheritdoc />
    public TimeoutSettings Timeouts { get; }

    /// <inheritdoc />
    public ITestLogger Logger { get; }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Css;

    #region IElementScope Implementation

    /// <inheritdoc />
    public IWebElement? TryFindElement(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator);
            return Driver.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IWebElement FindElement(Locator locator)
    {
        var element = TryFindElement(locator);
        if (element == null)
        {
            throw new Core.Exceptions.ElementNotFoundException(locator,
                $"Element not found: {locator}");
        }
        return element;
    }

    /// <inheritdoc />
    public IReadOnlyList<IWebElement> FindElements(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator);
            return Driver.FindElements(by).ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<IWebElement>();
        }
    }

    #endregion

    #region Navigation

    /// <inheritdoc />
    public void NavigateTo(string destination)
    {
        Logger.LogNavigation(ContextName, destination);

        // If destination is relative, combine with base URL
        string url;
        if (Uri.IsWellFormedUriString(destination, UriKind.Absolute))
        {
            url = destination;
        }
        else
        {
            url = CombineUrl(BaseUrl, destination);
        }

        Driver.Navigate().GoToUrl(url);

        // Wait for Blazor to initialize after navigation
        WaitForBlazorIdle();
    }

    /// <inheritdoc />
    public void NavigateBack()
    {
        Logger.LogNavigation(ContextName, "back");
        Driver.Navigate().Back();
        WaitForBlazorIdle();
    }

    /// <inheritdoc />
    public void Refresh()
    {
        Logger.LogAction(ContextName, null, "Browser", "Refresh");
        Driver.Navigate().Refresh();
        WaitForBlazorIdle();
    }

    #endregion

    #region Blazor-Specific

    /// <inheritdoc />
    public bool WaitForBlazorIdle(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Timeouts.Animation;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Check if we can execute JavaScript
            if (Driver is not IJavaScriptExecutor jsExecutor)
            {
                Logger.LogWarning("Driver does not support JavaScript execution");
                return true;
            }

            // Wait for Blazor to complete rendering
            // This script checks if Blazor's render queue is empty
            const string blazorIdleScript = @"
                if (typeof window.Blazor === 'undefined') {
                    return true; // Not a Blazor app or not yet loaded
                }
                
                // For Blazor Server
                if (window.Blazor._internal && window.Blazor._internal.navigationManager) {
                    return true; // Navigation manager exists, app is initialized
                }
                
                // For Blazor WebAssembly
                if (window.Blazor.start) {
                    return true; // Blazor is initialized
                }
                
                return true;
            ";

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                try
                {
                    var result = jsExecutor.ExecuteScript(blazorIdleScript);
                    if (result is true)
                    {
                        // Small additional delay for any final DOM updates
                        Thread.Sleep(50);
                        return true;
                    }
                }
                catch (JavaScriptException)
                {
                    // Script execution failed, continue polling
                }

                Thread.Sleep(Timeouts.PollingInterval);
            }

            Logger.LogWarning($"Blazor did not become idle within {timeout}ms");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ContextName, null, null, "WaitForBlazorIdle", ex);
            return false;
        }
    }

    /// <inheritdoc />
    public object? ExecuteScript(string script, params object[] args)
    {
        if (Driver is not IJavaScriptExecutor jsExecutor)
        {
            throw new NotSupportedException("Driver does not support JavaScript execution");
        }

        Logger.LogDebug($"Execute script: {script.Substring(0, Math.Min(100, script.Length))}...");
        return jsExecutor.ExecuteScript(script, args);
    }

    #endregion

    #region Screenshots

    /// <inheritdoc />
    public byte[] TakeScreenshot()
    {
        try
        {
            if (Driver is not ITakesScreenshot screenshotDriver)
            {
                Logger.LogWarning("Driver does not support screenshots");
                return Array.Empty<byte>();
            }

            var screenshot = screenshotDriver.GetScreenshot();
            Logger.LogInfo(ContextName, null, "Screenshot captured");
            return screenshot.AsByteArray;
        }
        catch (Exception ex)
        {
            Logger.LogError(ContextName, null, null, "TakeScreenshot", ex);
            return Array.Empty<byte>();
        }
    }

    /// <inheritdoc />
    public void SaveScreenshot(string path)
    {
        var bytes = TakeScreenshot();
        if (bytes.Length > 0)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, bytes);
            Logger.LogInfo(ContextName, null, $"Screenshot saved to: {path}");
        }
    }

    #endregion

    #region App State

    /// <inheritdoc />
    public void ResetAppState()
    {
        Logger.LogAction(ContextName, null, "Browser", "ResetAppState");
        try
        {
            // Clear cookies
            Driver.Manage().Cookies.DeleteAllCookies();

            // Clear local storage and session storage via JavaScript
            if (Driver is IJavaScriptExecutor jsExecutor)
            {
                try
                {
                    jsExecutor.ExecuteScript("window.localStorage.clear();");
                    jsExecutor.ExecuteScript("window.sessionStorage.clear();");
                }
                catch (JavaScriptException)
                {
                    // Storage might not be accessible
                }
            }

            Logger.LogInfo(ContextName, null, "App state reset complete");
        }
        catch (Exception ex)
        {
            Logger.LogError(ContextName, null, null, "ResetAppState", ex);
        }
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the test context and the underlying driver.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Logger.LogInfo(ContextName, null, "Disposing BlazorTestContext");
                try
                {
                    Driver?.Quit();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ContextName, null, null, "Dispose", ex);
                }
            }
            _disposed = true;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Combines a base URL with a relative path.
    /// </summary>
    private static string CombineUrl(string baseUrl, string path)
    {
        if (Uri.TryCreate(new Uri(baseUrl), path, out var result))
        {
            return result.ToString();
        }

        // Fallback to simple concatenation
        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedPath = path.TrimStart('/');
        return $"{trimmedBase}/{trimmedPath}";
    }

    #endregion
}
