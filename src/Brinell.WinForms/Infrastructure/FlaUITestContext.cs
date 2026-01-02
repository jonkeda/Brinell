using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using Brinell.Core.Abstractions;
using Brinell.Core.Logging;
using Brinell.Core.Screenshots;

namespace Brinell.WinForms.Infrastructure;

/// <summary>
/// FlaUI test context implementation for WinForms UI testing.
/// Implements ITestContext for common operations, adds WinForms-specific methods.
/// </summary>
public class FlaUITestContext : ITestContext
{
    private readonly Action<string>? _consoleLogger;
    private readonly FlaUIDriverAdapter _driver;
    private readonly IScreenshotService _screenshotService;
    
    public string TestName { get; set; } = "Unknown";
    public Platform Platform => Platform.Windows;
    
    public int DefaultTimeoutMs { get; init; } = 10000;
    public int ShortTimeoutMs { get; init; } = 100;
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
    /// The main application window.
    /// </summary>
    public Window MainWindow => _driver.MainWindow;
    
    /// <summary>
    /// The underlying FlaUI driver adapter.
    /// </summary>
    public FlaUIDriverAdapter Driver => _driver;

    public FlaUITestContext(FlaUIDriverAdapter driver, Action<string>? logger = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _consoleLogger = logger;
        _screenshotService = new FlaUIScreenshotService(() => MainWindow);
    }
    
    public FlaUITestContext(FlaUIDriverAdapter driver, ITestLogger csvLogger, Action<string>? consoleLogger = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Logger = csvLogger;
        _consoleLogger = consoleLogger;
        _screenshotService = new FlaUIScreenshotService(() => MainWindow);
    }
    
    /// <summary>
    /// Capture a failure screenshot.
    /// </summary>
    /// <param name="suffix">Descriptive suffix for the screenshot file (e.g., "dialog-not-displayed").</param>
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

    #region WinForms-specific element operations
    
    /// <summary>
    /// Find an element by AutomationId directly using FlaUI.
    /// Internal - use ControlObjects for test code.
    /// </summary>
    internal AutomationElement? FindElementInternal(string automationId)
    {
        // Check if the automationId matches the main window itself
        if (MainWindow.AutomationId == automationId)
        {
            return MainWindow;
        }
        
        return MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
    }

    /// <summary>
    /// Find an element by AutomationId directly using FlaUI.
    /// </summary>
    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public AutomationElement? FindElement(string automationId) => FindElementInternal(automationId);
    
    /// <summary>
    /// Find an element by XPath.
    /// Internal - use ControlObjects for test code.
    /// </summary>
    internal AutomationElement? FindElementByXPathInternal(string xpath)
    {
        return MainWindow.FindFirstByXPath(xpath);
    }

    /// <summary>
    /// Find an element by XPath.
    /// </summary>
    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public AutomationElement? FindElementByXPath(string xpath) => FindElementByXPathInternal(xpath);
    
    /// <summary>
    /// Find all elements matching AutomationId.
    /// Internal - use ControlObjects for test code.
    /// </summary>
    internal IReadOnlyCollection<AutomationElement> FindElementsInternal(string automationId)
    {
        return MainWindow.FindAllDescendants(cf => cf.ByAutomationId(automationId));
    }

    /// <summary>
    /// Find all elements matching AutomationId.
    /// </summary>
    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public IReadOnlyCollection<AutomationElement> FindElements(string automationId) => FindElementsInternal(automationId);
    
    #endregion

    #region Element operations (WinForms-specific)
    
    internal bool ElementExistsInternal(string automationId)
    {
        return FindElementInternal(automationId) != null;
    }

    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public bool ElementExists(string automationId) => ElementExistsInternal(automationId);

    internal bool ElementIsVisibleInternal(string automationId)
    {
        var element = FindElementInternal(automationId);
        return element != null && !element.IsOffscreen;
    }

    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public bool ElementIsVisible(string automationId) => ElementIsVisibleInternal(automationId);
    
    internal bool ElementIsEnabledInternal(string automationId)
    {
        var element = FindElementInternal(automationId);
        return element?.IsEnabled ?? false;
    }

    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public bool ElementIsEnabled(string automationId) => ElementIsEnabledInternal(automationId);
    
    internal string GetElementTextInternal(string automationId)
    {
        var element = FindElementInternal(automationId);
        if (element == null) return string.Empty;
        
        var textBox = element.AsTextBox();
        if (textBox != null) return textBox.Text ?? string.Empty;
        
        var label = element.AsLabel();
        if (label != null) return label.Text ?? string.Empty;
        
        return element.Name ?? string.Empty;
    }

    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public string GetElementText(string automationId) => GetElementTextInternal(automationId);
    
    internal void ClickElementInternal(string automationId)
    {
        var element = FindElementInternal(automationId);
        if (element != null)
        {
            // Try to use Invoke pattern for buttons (more reliable for commands)
            var button = element.AsButton();
            if (button != null)
            {
                button.Invoke();
            }
            else
            {
                element.Click();
            }
        }
        else
        {
            throw new InvalidOperationException($"Element '{automationId}' not found for click operation.");
        }
    }

    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public void ClickElement(string automationId) => ClickElementInternal(automationId);
    
    internal void EnterTextInternal(string automationId, string text)
    {
        var element = FindElementInternal(automationId);
        if (element != null)
        {
            var textBox = element.AsTextBox();
            if (textBox != null)
            {
                textBox.Text = string.Empty;
                textBox.Enter(text);
            }
        }
        else
        {
            throw new InvalidOperationException($"Element '{automationId}' not found for enter text operation.");
        }
    }

    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public void EnterText(string automationId, string text) => EnterTextInternal(automationId, text);
    
    internal void ClearElementInternal(string automationId)
    {
        var element = FindElementInternal(automationId);
        if (element != null)
        {
            var textBox = element.AsTextBox();
            if (textBox != null)
            {
                textBox.Text = string.Empty;
            }
        }
        else
        {
            throw new InvalidOperationException($"Element '{automationId}' not found for clear operation.");
        }
    }

    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public void ClearElement(string automationId) => ClearElementInternal(automationId);
    
    #endregion

    public string? TakeScreenshot(string name)
    {
        try
        {
            var screenshot = Capture.Screen();
            var fileName = $"{TestName}_{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var screenshotDir = Path.Combine(Path.GetTempPath(), "BrinellUITests");
            Directory.CreateDirectory(screenshotDir);
            var path = Path.Combine(screenshotDir, fileName);
            screenshot.ToFile(path);
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
    /// Internal - use ControlObjects for test code.
    /// </summary>
    internal AutomationElement? WaitForElementInternal(string automationId, int? timeoutMs = null)
    {
        AutomationElement? element = null;
        var found = WaitFor(() =>
        {
            element = FindElementInternal(automationId);
            return element != null;
        }, timeoutMs, $"element '{automationId}'");
        
        return found ? element : null;
    }

    /// <summary>
    /// Wait for element to exist.
    /// </summary>
    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public AutomationElement? WaitForElement(string automationId, int? timeoutMs = null) 
        => WaitForElementInternal(automationId, timeoutMs);

    /// <summary>
    /// Wait for element to be visible.
    /// Internal - use ControlObjects for test code.
    /// </summary>
    internal AutomationElement? WaitForElementVisibleInternal(string automationId, int? timeoutMs = null)
    {
        AutomationElement? element = null;
        var found = WaitFor(() =>
        {
            element = FindElementInternal(automationId);
            return element != null && !element.IsOffscreen;
        }, timeoutMs, $"element '{automationId}' visible");
        
        return found ? element : null;
    }

    /// <summary>
    /// Wait for element to be visible.
    /// </summary>
    [Obsolete("Use ControlObjects instead of direct element access. This method will be removed in a future version.")]
    public AutomationElement? WaitForElementVisible(string automationId, int? timeoutMs = null)
        => WaitForElementVisibleInternal(automationId, timeoutMs);
}
