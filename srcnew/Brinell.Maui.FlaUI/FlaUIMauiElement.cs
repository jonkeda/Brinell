using System.Drawing;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Maui.Enums;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiElement"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// </summary>
public sealed class FlaUIMauiElement : IMauiElement
{
    private readonly AutomationElement _element;
    private readonly FlaUIMauiDriver _driver;
    
    /// <summary>
    /// Creates a new FlaUIMauiElement wrapper.
    /// </summary>
    /// <param name="element">The FlaUI AutomationElement to wrap.</param>
    /// <param name="driver">The driver that owns this element.</param>
    /// <exception cref="ArgumentNullException">Thrown when element or driver is null.</exception>
    public FlaUIMauiElement(AutomationElement element, FlaUIMauiDriver driver)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }
    
    #region State Properties (IElement<IMauiElement>)
    
    /// <inheritdoc />
    /// <remarks>
    /// Uses multiple checks for visibility because FlaUI's IsOffscreen can be 
    /// unreliable for some MAUI controls (e.g., Switch on Windows).
    /// An element is considered visible if:
    /// 1. IsOffscreen is false, OR
    /// 2. The element has a valid (non-empty) bounding rectangle, OR
    /// 3. The element is enabled and has visible children (MAUI wrapper elements), OR
    /// 4. The element is enabled and supports Toggle pattern (MAUI Switch workaround)
    /// </remarks>
    public bool Visible
    {
        get
        {
            try
            {
                // Primary check: IsOffscreen property
                if (!_element.IsOffscreen)
                    return true;
                
                // Fallback 1: Check if element has a valid bounding rectangle
                // Some controls incorrectly report IsOffscreen=true but are actually visible
                var bounds = _element.BoundingRectangle;
                if (bounds.Width > 0 && bounds.Height > 0)
                    return true;
                
                // Fallback 2: For MAUI wrapper elements with zero bounds,
                // check if any child has valid bounds
                if (_element.IsEnabled)
                {
                    var children = _element.FindAllChildren();
                    foreach (var child in children)
                    {
                        try
                        {
                            if (!child.IsOffscreen)
                                return true;
                            var childBounds = child.BoundingRectangle;
                            if (childBounds.Width > 0 && childBounds.Height > 0)
                                return true;
                        }
                        catch
                        {
                            // Ignore children that can't be queried
                        }
                    }
                }
                
                // Fallback 3: MAUI Switch on Windows workaround
                // If the element supports Toggle pattern and is enabled, treat as visible
                // This handles cases where MAUI wraps WinUI controls with zero-bounds containers
                if (_element.IsEnabled && _element.Patterns.Toggle.IsSupported)
                    return true;
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <inheritdoc />
    public bool Enabled => _element.IsEnabled;
    
    /// <inheritdoc />
    public bool Selected
    {
        get
        {
            // Try SelectionItemPattern first
            if (_element.Patterns.SelectionItem.IsSupported)
            {
                return _element.Patterns.SelectionItem.Pattern.IsSelected.Value;
            }
            // Fallback to Toggle pattern (for checkboxes)
            if (_element.Patterns.Toggle.IsSupported)
            {
                return _element.Patterns.Toggle.Pattern.ToggleState.Value == 
                       global::FlaUI.Core.Definitions.ToggleState.On;
            }
            return false;
        }
    }
    
    /// <inheritdoc />
    public string? Text
    {
        get
        {
            try
            {
                // Try Value pattern first (for text inputs)
                if (_element.Patterns.Value.IsSupported)
                {
                    return _element.Patterns.Value.Pattern.Value.Value;
                }
                // Try RangeValue pattern for sliders and other range controls
                if (_element.Patterns.RangeValue.IsSupported)
                {
                    return _element.Patterns.RangeValue.Pattern.Value.Value.ToString();
                }
                // Fallback to Name property with safe access
                return _element.Properties.Name.ValueOrDefault;
            }
            catch
            {
                return null;
            }
        }
    }
    
    /// <inheritdoc />
    public string? TagName => _element.ControlType.ToString();
    
    /// <inheritdoc />
    public Point Location => new Point(_element.BoundingRectangle.X, _element.BoundingRectangle.Y);
    
    /// <inheritdoc />
    public Size Size => new Size(_element.BoundingRectangle.Width, _element.BoundingRectangle.Height);
    
    /// <inheritdoc />
    public Rectangle Rect => new Rectangle(Location, Size);
    
    #endregion
    
    #region Actions (IElement<IMauiElement>)
    
    /// <inheritdoc />
    public void Click()
    {
        // Try Invoke pattern first (for buttons)
        if (_element.Patterns.Invoke.IsSupported)
        {
            _element.Patterns.Invoke.Pattern.Invoke();
            return;
        }
        
        // Fallback to mouse click
        _element.Click();
    }
    
    /// <inheritdoc />
    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
    {
        switch (method)
        {
            case TextInputMethod.Keys:
                _element.Focus();
                Keyboard.Type(text);
                break;
            case TextInputMethod.Paste:
                _element.Focus();
                System.Windows.Forms.Clipboard.SetText(text);
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
                break;
            case TextInputMethod.SetValue:
                if (_element.Patterns.Value.IsSupported)
                {
                    _element.Patterns.Value.Pattern.SetValue(text);
                }
                else
                {
                    // Fallback to keyboard
                    _element.Focus();
                    Keyboard.Type(text);
                }
                break;
        }
    }
    
    /// <inheritdoc />
    public void Clear()
    {
        if (_element.Patterns.Value.IsSupported)
        {
            _element.Patterns.Value.Pattern.SetValue(string.Empty);
        }
        else
        {
            // Select all and delete
            _element.Focus();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(VirtualKeyShort.DELETE);
        }
    }
    
    /// <inheritdoc />
    public void DoubleClick()
    {
        _element.DoubleClick();
    }
    
    /// <inheritdoc />
    public void RightClick()
    {
        _element.RightClick();
    }
    
    /// <inheritdoc />
    public void Hover()
    {
        var rect = _element.BoundingRectangle;
        var center = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        Mouse.MoveTo(center);
    }
    
    /// <inheritdoc />
    public void LongPress(int durationMs = 1000)
    {
        var rect = _element.BoundingRectangle;
        var center = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        Mouse.MoveTo(center);
        Mouse.Down(MouseButton.Left);
        Thread.Sleep(durationMs);
        Mouse.Up(MouseButton.Left);
    }
    
    /// <inheritdoc />
    public void ScrollIntoView(int timeoutMs = 5000)
    {
        if (_element.Patterns.ScrollItem.IsSupported)
        {
            _element.Patterns.ScrollItem.Pattern.ScrollIntoView();
        }
        else
        {
            // Try to find scrollable parent and scroll
            var parent = _element.Parent;
            while (parent != null)
            {
                if (parent.Patterns.Scroll.IsSupported)
                {
                    // Calculate if element is in view
                    var parentRect = parent.BoundingRectangle;
                    var elementRect = _element.BoundingRectangle;
                    
                    if (elementRect.Bottom > parentRect.Bottom)
                    {
                        parent.Patterns.Scroll.Pattern.Scroll(
                            global::FlaUI.Core.Definitions.ScrollAmount.NoAmount,
                            global::FlaUI.Core.Definitions.ScrollAmount.LargeIncrement);
                    }
                    else if (elementRect.Top < parentRect.Top)
                    {
                        parent.Patterns.Scroll.Pattern.Scroll(
                            global::FlaUI.Core.Definitions.ScrollAmount.NoAmount,
                            global::FlaUI.Core.Definitions.ScrollAmount.LargeDecrement);
                    }
                    break;
                }
                parent = parent.Parent;
            }
        }
    }
    
    /// <inheritdoc />
    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        // FlaUI doesn't have native swipe support, simulate with mouse drag
        Mouse.MoveTo(new Point(startX, startY));
        Mouse.Down(MouseButton.Left);
        
        // Interpolate movement for smooth swipe
        var steps = Math.Max(10, durationMs / 50);
        var deltaX = (endX - startX) / (double)steps;
        var deltaY = (endY - startY) / (double)steps;
        var stepDelay = durationMs / steps;
        
        for (int i = 1; i <= steps; i++)
        {
            var x = (int)(startX + deltaX * i);
            var y = (int)(startY + deltaY * i);
            Mouse.MoveTo(new Point(x, y));
            Thread.Sleep(stepDelay);
        }
        
        Mouse.Up(MouseButton.Left);
    }
    
    #endregion
    
    #region Element Finding (IElement<IMauiElement>)
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        var condition = locator.ToCondition(_driver.ConditionFactory);
        
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var found = _element.FindFirstDescendant(condition);
            if (found != null)
            {
                return new FlaUIMauiElement(found, _driver);
            }
            
            if (timeoutMs <= 0) break;
            Thread.Sleep(100);
        }
        
        throw new ElementNotFoundException(locator);
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        var condition = locator.ToCondition(_driver.ConditionFactory);
        
        if (timeoutMs > 0)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                var found = _element.FindAllDescendants(condition);
                if (found.Length > 0)
                {
                    return found.Select(e => new FlaUIMauiElement(e, _driver)).ToList();
                }
                Thread.Sleep(100);
            }
        }
        
        var elements = _element.FindAllDescendants(condition);
        return elements.Select(e => new FlaUIMauiElement(e, _driver)).ToList();
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
    
    #region Attribute Access (IMauiElement)
    
    /// <inheritdoc />
    public string? GetAttribute(string attributeName)
    {
        try
        {
            // Map common attribute names to FlaUI properties
            // Use safe property access to avoid PropertyNotSupportedException
            return attributeName.ToLowerInvariant() switch
            {
                "name" => _element.Properties.Name.ValueOrDefault,
                "automationid" => _element.Properties.AutomationId.ValueOrDefault,
                "classname" or "class" => _element.Properties.ClassName.ValueOrDefault,
                "controltype" => _element.ControlType.ToString(),
                "enabled" => _element.IsEnabled.ToString(),
                "visible" => (!_element.IsOffscreen).ToString(),
                "helptext" => _element.Properties.HelpText.ValueOrDefault,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public string? GetDomAttribute(string attributeName)
    {
        // FlaUI doesn't have DOM attributes - return null
        return null;
    }
    
    /// <inheritdoc />
    public string? GetDomProperty(string propertyName)
    {
        // FlaUI doesn't have DOM properties - return null
        return null;
    }
    
    /// <inheritdoc />
    public string? GetCssValue(string propertyName)
    {
        // FlaUI doesn't have CSS values - return null
        return null;
    }
    
    /// <inheritdoc />
    public void Submit()
    {
        // Try to find and click a submit button, or press Enter
        Keyboard.Type(VirtualKeyShort.ENTER);
    }
    
    #endregion
    
    #region Internal
    
    /// <summary>
    /// Gets the underlying FlaUI AutomationElement for internal use.
    /// </summary>
    internal AutomationElement Element => _element;
    
    #endregion
}
