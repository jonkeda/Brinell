using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;

namespace Brinell.Maui.Infrastructure;

/// <summary>
/// Appium element adapter implementing IElementAdapter.
/// Wraps Appium's AppiumElement for use with the UITestFramework.
/// </summary>
public class AppiumElementAdapter : IElementAdapter
{
    private readonly AppiumElement _element;
    
    public AppiumElementAdapter(AppiumElement element)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
    }
    
    /// <summary>
    /// The AutomationId of this element.
    /// For MAUI apps, this corresponds to the AutomationId property set in XAML.
    /// </summary>
    public string AutomationId => _element.GetAttribute("AutomationId") 
                                  ?? _element.GetAttribute("accessibility-id") 
                                  ?? string.Empty;
    
    /// <summary>
    /// The native Appium element.
    /// </summary>
    public object NativeElement => _element;
    
    /// <summary>
    /// Get the underlying Appium element.
    /// </summary>
    public AppiumElement Element => _element;
}
