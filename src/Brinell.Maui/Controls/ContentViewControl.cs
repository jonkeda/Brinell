using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI ContentView control wrapper.
/// Provides a simple content container.
/// </summary>
public class ContentViewControl : ContentControlBase
{
    public ContentViewControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ContentViewControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Find a child control by automation ID.
    /// </summary>
    /// <typeparam name="T">The control type.</typeparam>
    /// <param name="childAutomationId">The child's automation ID.</param>
    public T? FindChild<T>(string childAutomationId) where T : ControlBase
    {
        var element = _context.Driver.FindElementDirect(childAutomationId);
        if (element == null) return null;
        
        return (T?)Activator.CreateInstance(typeof(T), _context, _page, childAutomationId);
    }

    /// <summary>
    /// Check if the content view has any content.
    /// </summary>
    public bool HasContent()
    {
        var element = FindElement();
        if (element == null) return false;
        
        // Check for child elements
        try
        {
            var children = element.FindElements(OpenQA.Selenium.By.XPath("./*"));
            return children.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}
