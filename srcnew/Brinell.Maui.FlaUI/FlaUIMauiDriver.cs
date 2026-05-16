using System.Diagnostics;
using System.Drawing;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Utilities;
using Brinell.Maui.Enums;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiDriver"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// </summary>
public sealed class FlaUIMauiDriver : IMauiDriver, IDisposable
{
    private readonly UIA3Automation _automation;
    private readonly Application? _application;
    private readonly AutomationElement _rootElement;
    private readonly ConditionFactory _conditionFactory;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new FlaUIMauiDriver for an existing window.
    /// </summary>
    /// <param name="windowHandle">The window handle to attach to.</param>
    public FlaUIMauiDriver(IntPtr windowHandle)
    {
        _automation = new UIA3Automation();
        _rootElement = _automation.FromHandle(windowHandle);
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
    }
    
    /// <summary>
    /// Creates a new FlaUIMauiDriver by launching an application.
    /// </summary>
    /// <param name="executablePath">Path to the application executable.</param>
    /// <param name="arguments">Optional command line arguments.</param>
    public FlaUIMauiDriver(string executablePath, string? arguments = null)
    {
        _automation = new UIA3Automation();
        
        var processStartInfo = new ProcessStartInfo(executablePath)
        {
            Arguments = arguments ?? string.Empty,
            UseShellExecute = true
        };
        
        var process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {executablePath}");
        
        // Give the process time to initialize before attaching
        process.WaitForInputIdle();
        
        _application = Application.Attach(process);
        
        // Wait for main window
        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _rootElement = window ?? throw new InvalidOperationException("Failed to get main window");
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
    }
    
    /// <summary>
    /// Creates a new FlaUIMauiDriver by attaching to a running process.
    /// </summary>
    /// <param name="process">The process to attach to.</param>
    public FlaUIMauiDriver(Process process)
    {
        _automation = new UIA3Automation();
        _application = Application.Attach(process);
        
        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _rootElement = window ?? throw new InvalidOperationException("Failed to get main window");
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
    }
    
    #region Platform
    
    /// <inheritdoc />
    public MauiPlatform Platform => MauiPlatform.Windows;
    
    #endregion
    
    #region Internal
    
    /// <summary>
    /// Gets the condition factory for building search conditions.
    /// </summary>
    internal ConditionFactory ConditionFactory => _conditionFactory;
    
    /// <summary>
    /// Gets the underlying automation instance.
    /// </summary>
    internal UIA3Automation Automation => _automation;

    /// <summary>
    /// Checks whether a screen point falls within the root window bounds.
    /// </summary>
    /// <param name="point">Screen point to validate.</param>
    /// <param name="padding">Optional inset padding in pixels.</param>
    /// <returns>True when point is within the root window rectangle.</returns>
    internal bool IsPointInsideRootWindow(Point point, int padding = 0)
    {
        try
        {
            var rect = _rootElement.BoundingRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return false;
            }

            var left = rect.Left + padding;
            var right = rect.Right - padding;
            var top = rect.Top + padding;
            var bottom = rect.Bottom - padding;

            return point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures the root window is focused and activated before interaction.
    /// </summary>
    internal void EnsureRootWindowFocused()
    {
        try
        {
            if (_rootElement.Patterns.Window.IsSupported)
            {
                var windowPattern = _rootElement.Patterns.Window.Pattern;
                if (windowPattern.WindowVisualState.Value == WindowVisualState.Minimized)
                {
                    windowPattern.SetWindowVisualState(WindowVisualState.Normal);
                }
            }
        }
        catch
        {
            // Ignore window visual state failures and continue with focus fallback.
        }

        try
        {
            _rootElement.SetForeground();
        }
        catch
        {
            // SetForeground can fail if the window is not top-level; fall back to Focus.
            try
            {
                _rootElement.Focus();
            }
            catch
            {
                // Ignore focus failures; interaction will proceed regardless.
            }
        }
    }
    
    #endregion
    
    #region Element Finding (IDriver<IMauiElement>)
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        var condition = locator.ToCondition(_conditionFactory);
        
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var found = _rootElement.FindFirstDescendant(condition);
            if (found != null)
            {
                return new FlaUIMauiElement(found, this);
            }
            
            if (timeoutMs <= 0) break;
            WaitHelper.Pause(100);
        }
        
        throw new ElementNotFoundException(locator);
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        var condition = locator.ToCondition(_conditionFactory);
        
        if (timeoutMs > 0)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                var found = _rootElement.FindAllDescendants(condition);
                if (found.Length > 0)
                {
                    return found.Select(e => new FlaUIMauiElement(e, this)).ToList();
                }
                WaitHelper.Pause(100);
            }
        }
        
        var elements = _rootElement.FindAllDescendants(condition);
        return elements.Select(e => new FlaUIMauiElement(e, this)).ToList();
    }
    
    /// <inheritdoc />
    public bool TryFindElement(Locator locator, out IMauiElement? element, int timeoutMs = 0)
    {
        try
        {
            element = FindElement(locator, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            element = null;
            return false;
        }
    }
    
    #endregion
    
    #region Window Management
    
    /// <inheritdoc />
    public string CurrentWindowHandle => _rootElement.Properties.NativeWindowHandle.Value.ToString();
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> WindowHandles
    {
        get
        {
            if (_application != null)
            {
                return _application.GetAllTopLevelWindows(_automation)
                    .Select(w => w.Properties.NativeWindowHandle.Value.ToString())
                    .ToList();
            }
            return new[] { CurrentWindowHandle };
        }
    }
    
    #endregion
    
    #region Session Management
    
    /// <inheritdoc />
    public void Quit()
    {
        _application?.Close();
    }
    
    /// <inheritdoc />
    public void Close()
    {
        if (_rootElement.Patterns.Window.IsSupported)
        {
            _rootElement.Patterns.Window.Pattern.Close();
        }
    }
    
    #endregion
    
    #region Screenshots
    
    /// <inheritdoc />
    public byte[] GetScreenshot()
    {
        var capture = Capture.Element(_rootElement);
        using var ms = new MemoryStream();
        capture.Bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
    
    #endregion
    
    #region Context Switching (Not applicable for FlaUI)
    
    /// <inheritdoc />
    public string Context
    {
        get => "NATIVE_APP";
        set { } // No-op for FlaUI
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> Contexts => new[] { "NATIVE_APP" };
    
    #endregion
    
    #region Script Execution (Not applicable for FlaUI)
    
    /// <inheritdoc />
    public object? ExecuteScript(string script, params object[] args)
    {
        // FlaUI doesn't support script execution
        throw new NotSupportedException("Script execution is not supported by FlaUI driver");
    }
    
    #endregion
    
    #region IDiagnosticDriver
    
    /// <inheritdoc />
    public string GetPageSource()
    {
        // Build an XML representation of the automation tree
        return BuildAutomationTree(_rootElement);
    }
    
    /// <inheritdoc />
    public string GetAutomationTree()
    {
        return BuildAutomationTree(_rootElement);
    }
    
    private static string BuildAutomationTree(AutomationElement element, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var sb = new System.Text.StringBuilder();
        
        // Use safe property access - some elements don't support all properties
        string automationId = "";
        string name = "";
        string className = "";
        string controlType = "Unknown";
        
        try { automationId = element.Properties.AutomationId.ValueOrDefault ?? ""; } catch { }
        try { name = element.Properties.Name.ValueOrDefault ?? ""; } catch { }
        try { className = element.Properties.ClassName.ValueOrDefault ?? ""; } catch { }
        try { controlType = element.ControlType.ToString(); } catch { }
        
        sb.AppendLine($"{indent}<{controlType} AutomationId=\"{automationId}\" Name=\"{name}\" ClassName=\"{className}\">");
        
        try
        {
            foreach (var child in element.FindAllChildren())
            {
                sb.Append(BuildAutomationTree(child, depth + 1));
            }
        }
        catch
        {
            // Ignore errors traversing children
        }
        
        sb.AppendLine($"{indent}</{controlType}>");
        return sb.ToString();
    }
    
    #endregion
    
    #region Navigation
    
    /// <inheritdoc />
    public void NavigateTo(string destination)
    {
        // FlaUI desktop apps don't support URL navigation
        throw new NotSupportedException("URL navigation is not supported by FlaUI driver for desktop apps");
    }
    
    /// <inheritdoc />
    public void NavigateBack()
    {
        // Try to invoke back button or Alt+Left
        // For desktop apps, we can try sending keyboard shortcut
        global::FlaUI.Core.Input.Keyboard.TypeSimultaneously(
            global::FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT,
            global::FlaUI.Core.WindowsAPI.VirtualKeyShort.LEFT);
    }
    
    /// <inheritdoc />
    public void Refresh()
    {
        // Try F5 refresh for desktop apps
        global::FlaUI.Core.Input.Keyboard.Type(global::FlaUI.Core.WindowsAPI.VirtualKeyShort.F5);
    }
    
    /// <inheritdoc />
    public byte[] TakeScreenshot() => GetScreenshot();
    
    /// <inheritdoc />
    public void ResetAppState()
    {
        // For desktop apps, close and relaunch
        if (_application != null)
        {
            // Note: This doesn't fully reset - caller may need to recreate the driver
            _application.Close();
        }
    }
    
    #endregion
    
    #region Popup Windows
    
    /// <inheritdoc />
    public IMauiElement FindPopupElement(Locator locator, int timeoutMs = 5000)
    {
        var condition = locator.ToCondition(_conditionFactory);
        
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var found = FindInPopupWindows(condition);
            if (found != null)
            {
                return found;
            }
            
            if (timeoutMs <= 0) break;
            WaitHelper.Pause(100);
        }
        
        throw new ElementNotFoundException(locator);
    }
    
    /// <inheritdoc />
    public bool TryFindPopupElement(Locator locator, out IMauiElement? element, int timeoutMs = 0)
    {
        try
        {
            element = FindPopupElement(locator, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            element = null;
            return false;
        }
    }
    
    /// <summary>
    /// Searches all top-level windows of the process (excluding the main window)
    /// for a descendant matching the condition.
    /// </summary>
    private IMauiElement? FindInPopupWindows(ConditionBase condition)
    {
        if (_application == null) return null;
        
        var allWindows = _application.GetAllTopLevelWindows(_automation);
        var mainHandle = _rootElement.Properties.NativeWindowHandle.ValueOrDefault;
        
        foreach (var window in allWindows)
        {
            // Skip the main window — that's what normal FindElement searches
            if (window.Properties.NativeWindowHandle.ValueOrDefault == mainHandle)
                continue;
            
            // Search descendants of the popup window
            var found = window.FindFirstDescendant(condition);
            if (found != null)
            {
                return new FlaUIMauiElement(found, this);
            }
            
            // Also check the popup window element itself
            try
            {
                var selfMatch = window.FindFirst(TreeScope.Element, condition);
                if (selfMatch != null)
                {
                    return new FlaUIMauiElement(selfMatch, this);
                }
            }
            catch
            {
                // Ignore — some windows may not support all property queries
            }
        }
        
        return null;
    }
    
    #endregion
    
    #region Platform-Specific
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindByAndroidUIAutomator(string uiAutomatorQuery)
    {
        // Not supported on Windows - return empty list
        return Array.Empty<IMauiElement>();
    }
    
    #endregion
    
    #region IDisposable
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _application?.Close();
            _automation.Dispose();
            _disposed = true;
        }
    }
    
    #endregion
}
