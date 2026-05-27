using System.Drawing;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Utilities;
using Brinell.Maui.Enums;
using Brinell.Maui.Interfaces;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiElement"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// Also implements pattern-based interfaces for enhanced Windows Automation support.
/// </summary>
public sealed class FlaUIMauiElement : IMauiElement, IInvokePatternElement, ISelectionItemPatternElement, ILegacyIAccessiblePatternElement, IRangePatternElement, IExpandCollapsePatternElement, INestedTextElement, ITogglePatternElement
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
        _driver.EnsureRootWindowFocused();

        if (InvokePattern())
            return;

        if (SelectItemPattern())
            return;

        if (DoDefaultActionPattern())
            return;

        ClickWithPointerFallback();
    }

    private void ClickWithPointerFallback()
    {
        _driver.EnsureRootWindowFocused();

        var rect = _element.BoundingRectangle;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            throw new InvalidOperationException(
                $"Element is not gesture-clickable because bounds are empty ({rect}).");
        }

        if (!PointerGesturesEnabled())
        {
            throw new InvalidOperationException(
                "Pointer gestures are disabled. Brinell will not move the system mouse unless " +
                "BRINELL_ALLOW_POINTER_INPUT=true is set for this test run.");
        }

        var center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        Mouse.MoveTo(center);
        Mouse.Down(MouseButton.Left);
        WaitHelper.Pause(120);
        Mouse.Up(MouseButton.Left);
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
                if (TrySetTextValue(text))
                    return;

                _element.Focus();
                Keyboard.Type(text);
                break;
        }
    }
    
    /// <inheritdoc />
    public void Clear()
    {
        if (TrySetTextValue(string.Empty))
            return;

        // Select all and delete
        _element.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(VirtualKeyShort.DELETE);
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
        Mouse.Position = center;
        Mouse.Down(MouseButton.Left);
        WaitHelper.Pause(durationMs);
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
        // On Windows desktop, vertical swipes should use mouse wheel
        // since mouse drag doesn't scroll MAUI ScrollView controls.
        var deltaY = endY - startY;
        var deltaX = endX - startX;

        // Detect vertical-only scroll gesture (typical swipe to scroll)
        if (Math.Abs(deltaX) < 20 && Math.Abs(deltaY) > 20)
        {
            _driver.EnsureRootWindowFocused();

            // Use mouse wheel at the element center — most reliable for MAUI ScrollView on WinUI3
            var center = new Point(
                _element.BoundingRectangle.X + _element.BoundingRectangle.Width / 2,
                _element.BoundingRectangle.Y + _element.BoundingRectangle.Height / 2);
            Mouse.MoveTo(center);
            // deltaY < 0 means swipe up → scroll down → negative wheel
            var wheelClicks = deltaY < 0 ? -5 : 5;
            Mouse.Scroll(wheelClicks);
            WaitHelper.Pause(200); // Wait for scroll to settle
            return;
        }

        // Non-scroll gestures: simulate with mouse drag
        Mouse.MoveTo(new Point(startX, startY));
        Mouse.Down(MouseButton.Left);

        // Interpolate movement for smooth swipe
        var steps = Math.Max(10, durationMs / 50);
        var dx = (endX - startX) / (double)steps;
        var dy = (endY - startY) / (double)steps;
        var stepDelay = durationMs / steps;

        for (int i = 1; i <= steps; i++)
        {
            var x = (int)(startX + dx * i);
            var y = (int)(startY + dy * i);
            Mouse.MoveTo(new Point(x, y));
            WaitHelper.Pause(stepDelay);
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
        
        do
        {
            var found = _element.FindFirstDescendant(condition);
            if (found != null)
            {
                return new FlaUIMauiElement(found, _driver);
            }
            
            if (timeoutMs <= 0) break;
            WaitHelper.Pause(100);
        }
        while (DateTime.UtcNow - startTime < timeout);
        
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
                WaitHelper.Pause(100);
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
                "scroll.verticalscrollpercent" => GetScrollPatternValue(p => p.VerticalScrollPercent.Value),
                "scroll.horizontalscrollpercent" => GetScrollPatternValue(p => p.HorizontalScrollPercent.Value),
                "scroll.verticallyscrollable" => GetScrollPatternBool(p => p.VerticallyScrollable.Value),
                "scroll.horizontallyscrollable" => GetScrollPatternBool(p => p.HorizontallyScrollable.Value),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a numeric value from the Scroll pattern, or null if not supported.
    /// </summary>
    private string? GetScrollPatternValue(Func<global::FlaUI.Core.Patterns.IScrollPattern, double> accessor)
    {
        if (!_element.Patterns.Scroll.IsSupported)
            return null;
        var value = accessor(_element.Patterns.Scroll.Pattern);
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets a boolean value from the Scroll pattern, or null if not supported.
    /// </summary>
    private string? GetScrollPatternBool(Func<global::FlaUI.Core.Patterns.IScrollPattern, bool> accessor)
    {
        if (!_element.Patterns.Scroll.IsSupported)
            return null;
        return accessor(_element.Patterns.Scroll.Pattern).ToString();
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

    private static bool PointerGesturesEnabled()
    {
        var value = Environment.GetEnvironmentVariable("BRINELL_ALLOW_POINTER_INPUT");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
    
    #endregion

    #region ITogglePatternElement Implementation

    /// <inheritdoc />
    public bool SupportsTogglePattern
    {
        get
        {
            try
            {
                return _element.Patterns.Toggle.IsSupported;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool? IsTogglePatternChecked()
    {
        try
        {
            if (!_element.Patterns.Toggle.IsSupported)
                return null;

            return _element.Patterns.Toggle.Pattern.ToggleState.Value ==
                   global::FlaUI.Core.Definitions.ToggleState.On;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool TogglePattern()
    {
        try
        {
            if (!_element.Patterns.Toggle.IsSupported)
                return false;

            _driver.EnsureRootWindowFocused();
            _element.Patterns.Toggle.Pattern.Toggle();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetToggleStatePattern(bool isChecked)
    {
        var current = IsTogglePatternChecked();
        if (current == null)
            return false;

        if (current == isChecked)
            return true;

        return TogglePattern();
    }

    #endregion

    #region IInvokePatternElement Implementation

    /// <inheritdoc />
    public bool SupportsInvokePattern
    {
        get
        {
            try
            {
                return _element.Patterns.Invoke.IsSupported;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool InvokePattern()
    {
        try
        {
            if (!_element.Patterns.Invoke.IsSupported)
                return false;

            _driver.EnsureRootWindowFocused();
            _element.Patterns.Invoke.Pattern.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region ILegacyIAccessiblePatternElement Implementation

    /// <inheritdoc />
    public bool SupportsLegacyIAccessiblePattern
    {
        get
        {
            try
            {
                return _element.Patterns.LegacyIAccessible.IsSupported;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool DoDefaultActionPattern()
    {
        try
        {
            if (!_element.Patterns.LegacyIAccessible.IsSupported)
                return false;

            _driver.EnsureRootWindowFocused();
            _element.Patterns.LegacyIAccessible.Pattern.DoDefaultAction();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region ISelectionItemPatternElement Implementation

    /// <inheritdoc />
    public bool SupportsSelectionItemPattern
    {
        get
        {
            try
            {
                return _element.Patterns.SelectionItem.IsSupported;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool SelectItemPattern()
    {
        try
        {
            if (!_element.Patterns.SelectionItem.IsSupported)
                return false;

            _driver.EnsureRootWindowFocused();
            _element.Patterns.SelectionItem.Pattern.Select();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
    
    #region IRangePatternElement Implementation
    
    /// <inheritdoc />
    public bool SupportsRangeValue
    {
        get
        {
            try
            {
                return _element.Patterns.RangeValue.IsSupported;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <inheritdoc />
    public bool SetRangeValue(double value)
    {
        try
        {
            if (!_element.Patterns.RangeValue.IsSupported)
                return false;
                
            var pattern = _element.Patterns.RangeValue.Pattern;
            
            // Clamp value to valid range
            var min = pattern.Minimum.Value;
            var max = pattern.Maximum.Value;
            var clampedValue = Math.Max(min, Math.Min(max, value));
            
            pattern.SetValue(clampedValue);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <inheritdoc />
    public double? GetRangeValue()
    {
        try
        {
            if (!_element.Patterns.RangeValue.IsSupported)
                return null;
            return _element.Patterns.RangeValue.Pattern.Value.Value;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public double? GetRangeMinimum()
    {
        try
        {
            if (!_element.Patterns.RangeValue.IsSupported)
                return null;
            return _element.Patterns.RangeValue.Pattern.Minimum.Value;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public double? GetRangeMaximum()
    {
        try
        {
            if (!_element.Patterns.RangeValue.IsSupported)
                return null;
            return _element.Patterns.RangeValue.Pattern.Maximum.Value;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public double? GetRangeSmallChange()
    {
        try
        {
            if (!_element.Patterns.RangeValue.IsSupported)
                return null;
            return _element.Patterns.RangeValue.Pattern.SmallChange.Value;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region IExpandCollapsePatternElement Implementation
    
    /// <inheritdoc />
    public bool SupportsExpandCollapse
    {
        get
        {
            if (!_element.Patterns.ExpandCollapse.IsSupported)
                return false;
            return true;
        }
    }
    
    /// <inheritdoc />
    public bool IsExpanded
    {
        get
        {
            if (!_element.Patterns.ExpandCollapse.IsSupported)
                return false;
            return _element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value == 
                   global::FlaUI.Core.Definitions.ExpandCollapseState.Expanded;
        }
    }
    
    /// <inheritdoc />
    public bool Expand()
    {
        if (!_element.Patterns.ExpandCollapse.IsSupported)
            return false;
            
        _element.Patterns.ExpandCollapse.Pattern.Expand();
        
        // Poll until expanded state is confirmed
        WaitHelper.WaitFor(
            () => _element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value == 
                  global::FlaUI.Core.Definitions.ExpandCollapseState.Expanded,
            timeoutMs: 2000,
            pollingIntervalMs: 50);
        
        return IsExpanded;
    }
    
    /// <inheritdoc />
    public bool Collapse()
    {
        if (!_element.Patterns.ExpandCollapse.IsSupported)
            return false;
            
        _element.Patterns.ExpandCollapse.Pattern.Collapse();
        return true;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement>? GetExpandedItems()
    {
        if (!_element.Patterns.ExpandCollapse.IsSupported)
            return null;
        
        var wasExpanded = IsExpanded;
        
        // Expand if not already expanded
        if (!wasExpanded)
        {
            if (!Expand())
                return null;
        }
        
        try
        {
            global::FlaUI.Core.AutomationElements.AutomationElement[] items = [];
            
            // Poll for ListItem elements to appear after expansion
            WaitHelper.WaitFor(() =>
            {
                // Try descendants of this element
                items = _element.FindAllDescendants(cf => 
                    cf.ByControlType(global::FlaUI.Core.Definitions.ControlType.ListItem));
                if (items.Length > 0) return true;
                
                // Try direct/logical children (FlaUI ComboBox pattern)
                items = _element.FindAllChildren(cf => 
                    cf.ByControlType(global::FlaUI.Core.Definitions.ControlType.ListItem));
                return items.Length > 0;
            }, timeoutMs: 2000, pollingIntervalMs: 50);
            
            return items.Select(e => new FlaUIMauiElement(e, _driver) as IMauiElement).ToList();
        }
        finally
        {
            // Restore original state
            if (!wasExpanded)
            {
                Collapse();
            }
        }
    }
    
    /// <inheritdoc />
    public bool SelectItemByText(string text)
    {
        if (!_element.Patterns.ExpandCollapse.IsSupported)
            return false;
        
        _element.Patterns.ExpandCollapse.Pattern.Expand();
        WaitHelper.WaitFor(() => IsExpanded, timeoutMs: 2000, pollingIntervalMs: 50);
        
        // Find ListItem descendants
        global::FlaUI.Core.AutomationElements.AutomationElement[] items = [];
        WaitHelper.WaitFor(() =>
        {
            items = _element.FindAllDescendants(cf => 
                cf.ByControlType(global::FlaUI.Core.Definitions.ControlType.ListItem));
            return items.Length > 0;
        }, timeoutMs: 2000, pollingIntervalMs: 50);
        
        var target = items.FirstOrDefault(i => i.Name == text);
        if (target == null)
        {
            Collapse();
            return false;
        }
        
        // Use SelectionItemPattern — the standard UIA way to select items
        if (target.Patterns.SelectionItem.IsSupported)
        {
            target.Patterns.SelectionItem.Pattern.Select();
        }
        else
        {
            target.Click();
        }
        
        // Wait for the dropdown to collapse (selection should auto-close)
        WaitHelper.WaitFor(() => !IsExpanded, timeoutMs: 2000, pollingIntervalMs: 50);
        
        if (IsExpanded)
            Collapse();
        
        return true;
    }
    
    /// <inheritdoc />
    public bool SelectItemByIndex(int index)
    {
        if (!_element.Patterns.ExpandCollapse.IsSupported)
            return false;
        
        _element.Patterns.ExpandCollapse.Pattern.Expand();
        WaitHelper.WaitFor(() => IsExpanded, timeoutMs: 2000, pollingIntervalMs: 50);
        
        // Find ListItem descendants
        global::FlaUI.Core.AutomationElements.AutomationElement[] items = [];
        WaitHelper.WaitFor(() =>
        {
            items = _element.FindAllDescendants(cf => 
                cf.ByControlType(global::FlaUI.Core.Definitions.ControlType.ListItem));
            return items.Length > 0;
        }, timeoutMs: 2000, pollingIntervalMs: 50);
        
        if (index >= items.Length)
        {
            Collapse();
            return false;
        }
        
        var item = items[index];
        
        // Use SelectionItemPattern — the standard UIA way to select items
        if (item.Patterns.SelectionItem.IsSupported)
        {
            item.Patterns.SelectionItem.Pattern.Select();
        }
        else
        {
            item.Click();
        }
        
        // Wait for the dropdown to collapse (selection should auto-close)
        WaitHelper.WaitFor(() => !IsExpanded, timeoutMs: 2000, pollingIntervalMs: 50);
        
        if (IsExpanded)
            Collapse();
        
        return true;
    }
    
    /// <inheritdoc />
    public string? GetSelectedItemText()
    {
        // Use SelectionPattern to get the currently selected item
        if (!_element.Patterns.Selection.IsSupported)
            return null;
        
        var selection = _element.Patterns.Selection.Pattern.Selection.Value;
        if (selection == null || selection.Length == 0)
            return null;
        
        return selection[0].Name;
    }
    
    #endregion
    
    #region INestedTextElement Implementation
    
    /// <inheritdoc />
    public IMauiElement? FindNestedTextBox()
    {
        try
        {
            var textBox = FindNestedTextBoxElement();
            
            if (textBox != null)
                return new FlaUIMauiElement(textBox, _driver);
                
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public string? GetNestedText()
    {
        try
        {
            // First try direct Value pattern
            if (_element.Patterns.Value.IsSupported)
            {
                var value = _element.Patterns.Value.Pattern.Value.Value;
                if (value != null)
                    return value;
            }
            
            // Fall back to nested TextBox
            var nestedTextBox = FindNestedTextBox();
            if (nestedTextBox != null)
            {
                return nestedTextBox.Text;
            }
            
            // Last resort: Name property
            return _element.Properties.Name.ValueOrDefault;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public bool ClearWithFallback()
    {
        try
        {
            bool IsEmpty(string? value) => string.IsNullOrEmpty(value);

            if (TrySetTextValue(string.Empty) && IsEmpty(GetNestedText()))
                return true;

            var focusTargets = GetTextValueTargets();

            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (IsEmpty(GetNestedText()))
                    return true;

                // ValuePattern attempt on wrapper and nested targets.
                foreach (var target in focusTargets)
                {
                    if (target.Patterns.Value.IsSupported && !target.Patterns.Value.Pattern.IsReadOnly.Value)
                    {
                        target.Patterns.Value.Pattern.SetValue(string.Empty);
                        if (IsEmpty(GetNestedText()))
                            return true;
                    }
                }

                // Keyboard attempt: Ctrl+A then Delete on each possible focus target.
                foreach (var target in focusTargets)
                {
                    target.Focus();
                    Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
                    Keyboard.Type(VirtualKeyShort.DELETE);
                    if (IsEmpty(GetNestedText()))
                        return true;
                }

                // Keyboard attempt: force end and backspace remaining characters.
                var remainingText = GetNestedText() ?? string.Empty;
                if (remainingText.Length > 0)
                {
                    var target = focusTargets[0];
                    target.Focus();
                    Keyboard.Type(VirtualKeyShort.END);

                    var backspaceCount = Math.Max(remainingText.Length + 10, 20);
                    for (var i = 0; i < backspaceCount; i++)
                    {
                        Keyboard.Type(VirtualKeyShort.BACK);
                    }

                    if (IsEmpty(GetNestedText()))
                        return true;
                }
            }

            return IsEmpty(GetNestedText());
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetTextWithFallback(string text)
        => TrySetTextValue(text);

    private AutomationElement? FindNestedTextBoxElement()
    {
        try
        {
            return _element.FindFirstDescendant(cf =>
                cf.ByControlType(global::FlaUI.Core.Definitions.ControlType.Edit));
        }
        catch
        {
            return null;
        }
    }

    private List<AutomationElement> GetTextValueTargets()
    {
        var targets = new List<AutomationElement>();
        var nestedTextBox = FindNestedTextBoxElement();
        if (nestedTextBox != null)
            targets.Add(nestedTextBox);

        if (!targets.Contains(_element))
            targets.Add(_element);

        return targets;
    }

    private bool TrySetTextValue(string text)
    {
        foreach (var target in GetTextValueTargets())
        {
            try
            {
                if (!target.Patterns.Value.IsSupported)
                    continue;

                var pattern = target.Patterns.Value.Pattern;
                if (pattern.IsReadOnly.Value)
                    continue;

                pattern.SetValue(text);
                return true;
            }
            catch
            {
                // Try the next candidate target.
            }
        }

        return false;
    }
    
    #endregion
}
