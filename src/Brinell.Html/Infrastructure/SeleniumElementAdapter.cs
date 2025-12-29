using Brinell.Core.Abstractions;

namespace Brinell.Html.Infrastructure;

/// <summary>
/// Represents an HTML element found by Selenium.
/// </summary>
public class SeleniumElementAdapter : IElementAdapter
{
    private readonly OpenQA.Selenium.IWebElement _element;

    /// <summary>
    /// The AutomationId (data-automation-id attribute) of this element.
    /// </summary>
    public string AutomationId { get; }

    /// <summary>
    /// The native Selenium WebElement.
    /// </summary>
    public object NativeElement => _element;
    
    /// <summary>
    /// The native WebElement with proper typing.
    /// </summary>
    public OpenQA.Selenium.IWebElement WebElement => _element;

    public SeleniumElementAdapter(OpenQA.Selenium.IWebElement element, string automationId)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        AutomationId = automationId;
    }
    
    public SeleniumElementAdapter(OpenQA.Selenium.IWebElement element)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        AutomationId = element.GetAttribute("data-automation-id") 
                      ?? element.GetAttribute("id") 
                      ?? string.Empty;
    }
}
