using System.Drawing;

namespace Brinell.Maui.Wrappers;

/// <summary>
/// Production wrapper that delegates all operations to the underlying AppiumElement.
/// This class is a thin pass-through with minimal overhead.
/// </summary>
public sealed class MauiElement : IMauiElement
{
    private readonly AppiumElement _element;
    
    /// <summary>
    /// Creates a new MauiElement wrapper.
    /// </summary>
    /// <param name="element">The AppiumElement to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when element is null.</exception>
    public MauiElement(AppiumElement element)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
    }
    
    #region State Properties
    
    /// <inheritdoc />
    public bool Displayed => _element.Displayed;
    
    /// <inheritdoc />
    public bool Enabled => _element.Enabled;
    
    /// <inheritdoc />
    public bool Selected => _element.Selected;
    
    /// <inheritdoc />
    public string Text => _element.Text;
    
    /// <inheritdoc />
    public string TagName => _element.TagName;
    
    /// <inheritdoc />
    public Point Location => _element.Location;
    
    /// <inheritdoc />
    public Size Size => _element.Size;
    
    #endregion
    
    #region Actions
    
    /// <inheritdoc />
    public void Click() => _element.Click();
    
    /// <inheritdoc />
    public void SendKeys(string text) => _element.SendKeys(text);
    
    /// <inheritdoc />
    public void Clear() => _element.Clear();
    
    /// <inheritdoc />
    public void Submit() => _element.Submit();
    
    /// <inheritdoc />
    public void ScrollIntoView(IMauiDriver driver)
    {
        // For Windows MAUI apps, elements are typically already accessible in the UI Automation tree
        // The Windows driver doesn't support mouse pointer actions (MoveToElement)
        // If the element is not visible, we can try using JavaScript to scroll or use touch actions
        
        try
        {
            // Check if element is already displayed
            if (_element.Displayed)
                return;
                
            // Try using JavaScript executor to scroll element into view
            var unwrappedDriver = driver.UnwrapDriver();
            if (unwrappedDriver is IJavaScriptExecutor jsExecutor)
            {
                jsExecutor.ExecuteScript("arguments[0].scrollIntoView({behavior: 'auto', block: 'center'});", _element);
            }
        }
        catch
        {
            // If scrolling fails, continue anyway - element may still be interactable
            // Some elements in MAUI Windows apps don't need scrolling
        }
    }
    
    #endregion
    
    #region Attribute Access
    
    /// <inheritdoc />
    public string? GetAttribute(string attributeName) => _element.GetAttribute(attributeName);
    
    /// <inheritdoc />
    public string? GetDomAttribute(string attributeName) => _element.GetDomAttribute(attributeName);
    
    /// <inheritdoc />
    public string? GetDomProperty(string propertyName) => _element.GetDomProperty(propertyName);
    
    /// <inheritdoc />
    public string GetCssValue(string propertyName) => _element.GetCssValue(propertyName);
    
    #endregion
    
    #region Child Element Finding
    
    /// <inheritdoc />
    public IMauiElement FindElement(By by) => new MauiElement(_element.FindElement(by));
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(By by)
    {
        var elements = _element.FindElements(by);
        return elements.Select(e => new MauiElement(e)).ToList();
    }
    
    #endregion
    
    #region Escape Hatch
    
    /// <inheritdoc />
    public AppiumElement UnwrapElement() => _element;
    
    #endregion
}
