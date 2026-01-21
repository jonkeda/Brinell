using System.Diagnostics;
using System.Drawing;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Maui.Enums;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;

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
            Arguments = arguments ?? string.Empty
        };
        
        _application = Application.Launch(processStartInfo);
        
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
            Thread.Sleep(100);
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
                Thread.Sleep(100);
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
