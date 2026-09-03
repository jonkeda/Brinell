using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Utilities;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using System.Drawing;
using FlaUI.Core.Patterns;
using Brinell.Maui.Configuration;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiElement"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// Also implements pattern-based interfaces for enhanced Windows Automation support.
/// </summary>
public sealed class FlaUIMauiElement : IMauiElement, IInvokePatternElement, ISelectionItemPatternElement, ILegacyIAccessiblePatternElement, IRangePatternElement, IExpandCollapsePatternElement<IMauiElement>, ITogglePatternElement
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
    /// <para>
    /// Whether the element is on screen <em>right now</em>. UIA's <c>IsOffscreen</c> is false
    /// when the element is inside the viewport, and true when it is scrolled away, clipped, or
    /// not rendered.
    /// </para>
    /// <para>
    /// Deliberately nothing more. A control needing anything else — a Switch whose wrapper
    /// reports zero bounds, say — overrides <c>IsVisibleCore</c>, because that is control
    /// knowledge and an element must not know what a MAUI view means.
    /// </para>
    /// <para>
    /// "Visible once the user scrolls to it" is a different question, and the control object
    /// answers it separately through <c>IsVisibleAfterScroll</c>.
    /// </para>
    /// </remarks>
    public bool Visible => !_element.IsOffscreen;

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

                // MAUI wraps text controls on WinUI: the AutomationId sits on a wrapper whose
                // own Value pattern is absent, and the text lives in a nested Edit. Reading
                // through to it here means a caller just asks for Text - it does not need to
                // know that this platform nests, which is why there is no longer an
                // INestedTextElement capability for controls to probe.
                var nested = FindNestedTextBoxElement();
                if (nested != null)
                {
                    var nestedText = nested.Patterns.Value.IsSupported
                        ? nested.Patterns.Value.Pattern.Value.Value
                        : nested.Properties.Name.ValueOrDefault;
                    if (!string.IsNullOrEmpty(nestedText))
                        return nestedText;
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
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// A real mouse click, and the last resort: UI Automation patterns handle every click the
    /// suite performs. It exists because a control can genuinely expose no usable pattern, and
    /// because on Android and iOS a tap is the ordinary path rather than a fallback.
    /// </para>
    /// </remarks>
    public void Click()
    {
        var rect = _element.BoundingRectangle;
        var center = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        _driver.PointerClick(center, nameof(Click));
    }
   
    /// <inheritdoc />
    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
    {
        switch (method)
        {
            case TextInputMethod.Keys:
                _driver.FocusForGlobalKeyboardInput(_element, nameof(SendKeys));
                _driver.GlobalType(text, nameof(SendKeys));
                break;
            case TextInputMethod.Paste:
                _driver.FocusForGlobalKeyboardInput(_element, nameof(SendKeys));
                _driver.SetClipboardTextForInput(text, nameof(SendKeys));
                _driver.GlobalTypeSimultaneously(
                    nameof(SendKeys),
                    VirtualKeyShort.CONTROL,
                    VirtualKeyShort.KEY_V);
                break;
            case TextInputMethod.SetValue:
                if (TrySetTextValue(text))
                    return;

                _driver.FocusForGlobalKeyboardInput(_element, nameof(SendKeys));
                _driver.GlobalType(text, nameof(SendKeys));
                break;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (TrySetTextValue(string.Empty))
            return;

        // Select all and delete
        _driver.FocusForGlobalKeyboardInput(_element, nameof(Clear));
        _driver.GlobalTypeSimultaneously(
            nameof(Clear),
            VirtualKeyShort.CONTROL,
            VirtualKeyShort.KEY_A);
        _driver.GlobalType(VirtualKeyShort.DELETE, nameof(Clear));
    }

    /// <inheritdoc />
    public void DoubleClick()
    {
        _driver.PointerDoubleClick(_element, nameof(DoubleClick));
    }

    /// <inheritdoc />
    public void RightClick()
    {
        _driver.PointerRightClick(_element, nameof(RightClick));
    }

    /// <inheritdoc />
    public void Hover()
    {
        var rect = _element.BoundingRectangle;
        var center = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        _driver.PointerHover(center, nameof(Hover));
    }

    /// <inheritdoc />
    public void LongPress(int durationMs = 1000)
    {
        var rect = _element.BoundingRectangle;
        var center = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        _driver.PointerLongPress(center, durationMs, nameof(LongPress));
    }

    /// <inheritdoc />
    /// <inheritdoc />
    /// <remarks>
    /// Drives the UIA Scroll pattern on this element, or on the nearest scrollable
    /// ancestor when this element does not scroll itself — a MAUI CollectionView is often
    /// wrapped, so the addressable element and the scrolling one differ.
    /// </remarks>
    public bool TryScrollContent(int verticalSteps, int horizontalSteps = 0)
    {
        if (verticalSteps == 0 && horizontalSteps == 0)
            return false;

        var scroll = FindScrollPattern();
        if (scroll == null)
            return false;

        try
        {
            var before = scroll.VerticalScrollPercent.ValueOrDefault;

            scroll.Scroll(ToAmount(horizontalSteps), ToAmount(verticalSteps));

            // The scroll percent does not update synchronously: read immediately it reports
            // the pre-scroll value, making a successful scroll look like no progress.
            return WaitForScrollChange(scroll, before);
        }
        catch (Exception)
        {
            // A dead element or an unsupported combination is a negative answer, not a
            // fault: the caller falls back or stops.
            return false;
        }
    }

    /// <summary>
    /// Polls until the scroll percent moves away from <paramref name="before"/>.
    /// </summary>
    /// <returns>True if it moved; false if it stayed put for the whole window.</returns>
    /// <remarks>
    /// A false return legitimately means "already at the extreme". The window is short
    /// because the caller polls the realized item count separately.
    /// </remarks>
    private static bool WaitForScrollChange(IScrollPattern scroll, double before)
    {
        const int budgetMs = 500;
        const int intervalMs = 25;

        for (var waited = 0; waited < budgetMs; waited += intervalMs)
        {
            var now = scroll.VerticalScrollPercent.ValueOrDefault;
            if (Math.Abs(now - before) > 0.01)
                return true;

            Thread.Sleep(intervalMs);
        }

        return false;
    }

    /// <summary>
    /// Maps a step count onto the UIA scroll increments, which are coarse by design.
    /// </summary>
    private static ScrollAmount ToAmount(int steps) => steps switch
    {
        0 => ScrollAmount.NoAmount,
        > 0 => ScrollAmount.LargeIncrement,
        _ => ScrollAmount.LargeDecrement,
    };

    /// <summary>
    /// Returns this element's scroll pattern, or the nearest ancestor's.
    /// </summary>
    private IScrollPattern? FindScrollPattern()
    {
        if (_element.Patterns.Scroll.IsSupported)
            return _element.Patterns.Scroll.Pattern;

        var parent = _element.Parent;
        while (parent != null)
        {
            if (parent.Patterns.Scroll.IsSupported)
                return parent.Patterns.Scroll.Pattern;

            parent = parent.Parent;
        }

        return null;
    }

    public void ScrollIntoView(int timeoutMs = 5000)
    {
        if (_element.Patterns.ScrollItem.IsSupported)
        {
            _element.Patterns.ScrollItem.Pattern.ScrollIntoView();
        }

        if (!_element.IsOffscreen)
            return;

        // Find scrollable parent
        var parent = _element.Parent;
        IScrollPattern? scroll = null;

        while (parent != null)
        {
            if (parent.Patterns.Scroll.IsSupported)
            {
                scroll = parent.Patterns.Scroll.Pattern;
                break;
            }
            parent = parent.Parent;
        }

        if (scroll == null || parent == null)
            return;

        // If bounding rectangle is valid, try geometry-based scroll
        var elementRect = _element.BoundingRectangle;
        var parentRect = parent.BoundingRectangle;

        var rectValid =
            elementRect is { Bottom: > 0, Top: > 0, Height: > 0, Width: > 0 };

        if (rectValid)
        {
            if (elementRect.Bottom > parentRect.Bottom)
            {
                scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            }
            else if (elementRect.Top < parentRect.Top)
            {
                scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallDecrement);
            }

            return;
        }

        //
        // FALLBACK: bounding rectangle invalid → use percent-based scrolling
        //

        // 1. Scroll to top
        double last = -1;

        scroll.Scroll(
            ScrollAmount.NoAmount,
            ScrollAmount.SmallDecrement);

        Thread.Sleep(50);
        while (scroll.VerticalScrollPercent > 0)
        {
            // Detect no movement → break
            if (Math.Abs(scroll.VerticalScrollPercent - last) < 0.01)
                break;

            last = scroll.VerticalScrollPercent;

            scroll.Scroll(
                ScrollAmount.NoAmount,
                ScrollAmount.LargeDecrement);  

            Thread.Sleep(50); 
        }

        // 2. Scroll down until element becomes visible
        last = -1;

        while (_element.IsOffscreen && scroll.VerticalScrollPercent < 100)
        {
            if (Math.Abs(scroll.VerticalScrollPercent - last) < 0.01)
                break; // stuck → stop

            last = scroll.VerticalScrollPercent;

            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            Thread.Sleep(50);
        }

        last = -1;
        while (_element.IsOffscreen && scroll.VerticalScrollPercent < 100)
        {
            if (Math.Abs(scroll.VerticalScrollPercent - last) < 0.01)
                break; // stuck → stop

            last = scroll.VerticalScrollPercent;

            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallIncrement);
            Thread.Sleep(50);
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
            // Use mouse wheel at the element center — most reliable for MAUI ScrollView on WinUI3
            var center = new Point(
                _element.BoundingRectangle.X + _element.BoundingRectangle.Width / 2,
                _element.BoundingRectangle.Y + _element.BoundingRectangle.Height / 2);
            // deltaY < 0 means swipe up → scroll down → negative wheel
            var wheelClicks = deltaY < 0 ? -5 : 5;
            _driver.PointerScroll(center, wheelClicks, nameof(Swipe));
            WaitHelper.Pause(200); // Wait for scroll to settle
            return;
        }

        // Non-scroll gestures: simulate with mouse drag
        _driver.PointerDrag(
            new Point(startX, startY),
            new Point(endX, endY),
            durationMs,
            nameof(Swipe));
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
        _driver.FocusForGlobalKeyboardInput(_element, nameof(Submit));
        _driver.GlobalType(VirtualKeyShort.ENTER, nameof(Submit));
    }

    #endregion

    #region Internal

    /// <summary>
    /// Gets the underlying FlaUI AutomationElement for internal use.
    /// </summary>
    internal AutomationElement Element => _element;

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
            new FlaUIMauiElement(target, _driver).Click();
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
            new FlaUIMauiElement(item, _driver).Click();
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

    #region Nested text resolution

    // MAUI wraps text controls on WinUI: the AutomationId sits on a wrapper whose own Value
    // pattern is absent, and the real text lives in a nested Edit. Resolving that is this
    // element's job - the Text property and TrySetTextValue below both go through it - so no
    // control needs to know this platform nests, and there is no INestedTextElement capability
    // for one to probe.

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

    /// <summary>
    /// Writes a value through the UIA Value pattern, reaching into a wrapper if it has to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Self first, wrapper only if self cannot be written. MAUI maps <c>Entry</c> and
    /// <c>Editor</c> straight to a writable WinUI <c>Edit</c>, but <c>SearchBar</c> becomes an
    /// AutoSuggestBox: a <c>Group</c> with no Value pattern of its own and the real field nested
    /// inside. The descendant search is what makes SearchBar writable at all, and it runs only
    /// once the direct write is ruled out.
    /// </para>
    /// </remarks>
    private bool TrySetTextValue(string text)
    {
        if (TryWriteValue(_element, text))
            return true;

        var nested = FindNestedTextBoxElement();
        return nested != null && TryWriteValue(nested, text);
    }

    /// <summary>
    /// Writes to one element, reporting whether its Value pattern accepted the write.
    /// </summary>
    private static bool TryWriteValue(AutomationElement target, string text)
    {
        try
        {
            if (!target.Patterns.Value.IsSupported)
                return false;

            var pattern = target.Patterns.Value.Pattern;
            if (pattern.IsReadOnly.Value)
                return false;

            pattern.SetValue(text);
            return true;
        }
        catch
        {
            return false;
        }
    }


    #endregion
}
