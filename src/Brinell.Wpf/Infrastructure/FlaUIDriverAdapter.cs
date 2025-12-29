using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Brinell.Core.Abstractions;

namespace Brinell.Wpf.Infrastructure;

/// <summary>
/// FlaUI driver adapter for WPF UI automation.
/// </summary>
public class FlaUIDriverAdapter : IDriverAdapter
{
    private readonly Application _application;
    private readonly AutomationBase _automation;
    private readonly Window _mainWindow;

    /// <summary>
    /// The main application window.
    /// </summary>
    public Window MainWindow => _mainWindow;
    
    /// <summary>
    /// The FlaUI automation instance.
    /// </summary>
    public AutomationBase Automation => _automation;

    /// <summary>
    /// Create driver by launching application.
    /// </summary>
    public FlaUIDriverAdapter(string applicationPath, string? arguments = null)
    {
        _automation = new UIA3Automation();
        _application = arguments != null 
            ? Application.Launch(applicationPath, arguments)
            : Application.Launch(applicationPath);
        _mainWindow = _application.GetMainWindow(_automation) 
            ?? throw new InvalidOperationException("Could not get main window from application");
    }

    /// <summary>
    /// Create driver from existing application.
    /// </summary>
    public FlaUIDriverAdapter(Application application)
    {
        _automation = new UIA3Automation();
        _application = application;
        _mainWindow = _application.GetMainWindow(_automation)
            ?? throw new InvalidOperationException("Could not get main window from application");
    }

    public IElementAdapter? FindElement(string automationId)
    {
        // Check if the automationId matches the main window itself
        if (_mainWindow.AutomationId == automationId)
        {
            return new FlaUIElementAdapter(_mainWindow);
        }
        
        var element = _mainWindow.FindFirstDescendant(cf => 
            cf.ByAutomationId(automationId));
        return element != null ? new FlaUIElementAdapter(element) : null;
    }

    public IElementAdapter? FindElementByXPath(string xpath)
    {
        var element = _mainWindow.FindFirstByXPath(xpath);
        return element != null ? new FlaUIElementAdapter(element) : null;
    }

    public IReadOnlyCollection<IElementAdapter> FindElements(string automationId)
    {
        var elements = _mainWindow.FindAllDescendants(cf => 
            cf.ByAutomationId(automationId));
        return elements.Select(e => new FlaUIElementAdapter(e)).ToList();
    }

    public void Click(IElementAdapter element)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            ae.Click();
        }
    }

    public void SendKeys(IElementAdapter element, string text)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            var textBox = ae.AsTextBox();
            textBox?.Enter(text);
        }
    }
    
    /// <summary>
    /// Enter text into an element (clears first).
    /// </summary>
    public void EnterText(IElementAdapter element, string text)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            var textBox = ae.AsTextBox();
            if (textBox != null)
            {
                textBox.Text = string.Empty;
                textBox.Enter(text);
            }
        }
    }

    public void Clear(IElementAdapter element)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            var textBox = ae.AsTextBox();
            if (textBox != null)
            {
                textBox.Text = string.Empty;
            }
        }
    }

    public string? GetText(IElementAdapter element)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            // Try different patterns
            var textBox = ae.AsTextBox();
            if (textBox != null) return textBox.Text;
            
            var label = ae.AsLabel();
            if (label != null) return label.Text;
            
            return ae.Name;
        }
        return null;
    }

    public string? GetAttribute(IElementAdapter element, string name)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            return name.ToLower() switch
            {
                "name" => ae.Name,
                "automationid" => ae.AutomationId,
                "classname" => ae.ClassName,
                "controltype" => ae.ControlType.ToString(),
                "isenabled" => ae.IsEnabled.ToString(),
                "isoffscreen" => ae.IsOffscreen.ToString(),
                _ => null
            };
        }
        return null;
    }

    public bool IsDisplayed(IElementAdapter element)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            return !ae.IsOffscreen;
        }
        return false;
    }

    public bool IsEnabled(IElementAdapter element)
    {
        if (element.NativeElement is AutomationElement ae)
        {
            return ae.IsEnabled;
        }
        return false;
    }

    public void Dispose()
    {
        _application?.Close();
        _automation?.Dispose();
        GC.SuppressFinalize(this);
    }
}
