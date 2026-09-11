using Brinell.Core.Diagnostics;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Utilities;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Drawing;
using FlaUI.Core.Patterns;
using Brinell.Maui.Configuration;
using Brinell.Maui.FlaUI.Bridge;
using Brinell.Maui.Interfaces;
using Brinell.Uia;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiElement"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// Also implements pattern-based interfaces for enhanced Windows Automation support.
/// </summary>
public sealed class FlaUIMauiElement : IMauiElement, IInvokePatternElement, ISelectionItemPatternElement, ILegacyIAccessiblePatternElement, IRangePatternElement, IExpandCollapsePatternElement<IMauiElement>, ITogglePatternElement, IValuePatternElement, IFocusPatternElement
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
    public string? AutomationId
    {
        get
        {
            try
            {
                return _element.Properties.AutomationId.ValueOrDefault;
            }
            catch
            {
                return null;
            }
        }
    }

    #region IValuePatternElement

    /// <summary>
    /// The element carrying the Value pattern: this element, or the Edit that MAUI nests
    /// inside it. The nesting is the same one <see cref="Text"/> reads through.
    /// </summary>
    private AutomationElement? ValuePatternElement
    {
        get
        {
            try
            {
                if (_element.Patterns.Value.IsSupported) return _element;

                var nested = FindNestedTextBoxElement();
                return nested?.Patterns.Value.IsSupported == true ? nested : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public bool SupportsValuePattern => ValuePatternElement != null;

    /// <inheritdoc />
    public string? GetValuePattern()
    {
        try
        {
            return ValuePatternElement?.Patterns.Value.Pattern.Value.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool? IsValuePatternReadOnly()
    {
        try
        {
            return ValuePatternElement?.Patterns.Value.Pattern.IsReadOnly.Value;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    /// <inheritdoc />
    public string? Hint
    {
        get
        {
            try
            {
                return _element.Properties.HelpText.ValueOrDefault;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>UI Automation first, and the app itself only if UI Automation says no.</b> Windows
    /// keyboard focus is a property of the foreground thread: a window that is not in front has
    /// no focused control as far as <c>HasKeyboardFocus</c> is concerned, however the app sees
    /// it. So on an occluded or off-screen app - the whole point of stage B - a field that MAUI
    /// considers focused reports false here, and every assertion about focus would be wrong in
    /// the one configuration this work exists to support.
    /// </para>
    /// <para>
    /// Asking the app costs a walk of the bridge, so it is asked only on the false path. A true
    /// from UI Automation is already the answer, and it is the answer on nearly every read.
    /// </para>
    /// </remarks>
    public bool Focused
    {
        get
        {
            try
            {
                if (_element.Properties.HasKeyboardFocus.ValueOrDefault)
                {
                    return true;
                }
            }
            catch
            {
                // Fall through to the app's own answer, which is the better one anyway.
            }

            return TryBridge(BrinellVerb.IsFocused, string.Empty, out var reported)
                   && bool.TryParse(reported, out var focused)
                   && focused;
        }
    }

    /// <inheritdoc />
    public string? Name
    {
        get
        {
            try
            {
                return _element.Properties.Name.ValueOrDefault;
            }
            catch
            {
                return null;
            }
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
        PhysicalInput.Used("FlaUIMauiElement.Click", "the Invoke pattern, or the bridge's Tap verb (step 18)");
        _driver.EnsureRootWindowFocused();
        _element.Click();
    }

    #region Activation

    /// <inheritdoc />
    /// <remarks>
    /// The Invoke pattern, and nothing else. Where a control genuinely cannot be invoked - a
    /// MAUI <c>ToolbarItem</c> is the known case - the control says <c>Click</c> instead; it is
    /// not this method's job to guess a substitute.
    /// </remarks>
    public void Invoke() => Perform(
        nameof(Invoke), SupportsInvokePattern, InvokePattern, "InvokePattern");

    /// <inheritdoc />
    public void Toggle() => Perform(
        nameof(Toggle), SupportsTogglePattern, TogglePattern, "TogglePattern");

    /// <inheritdoc />
    public void Select() => Perform(
        nameof(Select), SupportsSelectionItemPattern, SelectItemPattern, "SelectionItemPattern");

    /// <summary>
    /// Runs one automation pattern, or explains which half of it was missing.
    /// </summary>
    /// <remarks>
    /// Absent and present-but-refused are told apart deliberately. The first means the control
    /// object named the wrong operation for this element; the second means the platform accepted
    /// the call and did not do the thing, which is a fault further down. The old ladder reported
    /// neither - it moved on to the next rung and, if that worked, said nothing at all.
    /// </remarks>
    private void Perform(string operation, bool supported, Func<bool> run, string pattern)
    {
        var name = AutomationId ?? Name ?? "(unnamed)";

        if (!supported)
        {
            throw new NotSupportedException(
                $"'{name}' does not expose the UI Automation {pattern}, so it cannot be asked to "
                + $"{operation}. Either the control object names the wrong operation for this "
                + "element, or this is not the element that was intended.");
        }

        if (!run())
        {
            throw new InvalidOperationException(
                $"The UI Automation {pattern} on '{name}' was available but refused to {operation}.");
        }
    }

    #endregion
   
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b><see cref="TextInputMethod.Keys"/> still types, and that is deliberate.</b> A keyboard
    /// raises <c>TextChanged</c> per character, applies <c>MaxLength</c> as it goes and lets a
    /// numeric keyboard refuse a letter; setting the text raises one change for the whole value.
    /// A test <i>of</i> input behaviour needs the former, so this method keeps meaning "type
    /// it", and the bridge becomes the default for the other two - which are about
    /// <i>arranging</i> a field's contents, not about the input pipeline.
    /// </para>
    /// <para>
    /// <b><see cref="TextInputMethod.Paste"/> no longer reaches the clipboard when the bridge
    /// can take it.</b> The physical route writes the machine-wide clipboard and sends Ctrl+V:
    /// it destroys whatever the person at the keyboard had copied, and two runs on one machine
    /// corrupt each other. It stays as the fallback because an uninstrumented app has nothing
    /// else.
    /// </para>
    /// </remarks>
    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
    {
        switch (method)
        {
            case TextInputMethod.Keys:
                PhysicalInput.Used("FlaUIMauiElement.SendKeys(Keys)", "nothing, when the test is of the input pipeline; the SetText verb otherwise");
                FocusForKeyboardInput();
                Keyboard.Type(text);
                break;
            case TextInputMethod.Paste:
                if (TryBridge(BrinellVerb.SetText, text, out _))
                    return;

                PhysicalInput.Used("FlaUIMauiElement.SendKeys(Paste)", "the SetText verb - this one also destroys the user's clipboard");
                FocusForKeyboardInput();
                System.Windows.Forms.Clipboard.SetText(text);
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
                break;
            case TextInputMethod.SetValue:
                // The Value pattern first, and the bridge second. Both are semantic, so the
                // ordering is not about avoiding physical input at all - it is that the pattern
                // is two cross-process calls against an element already in hand, where the
                // bridge is a raw walk of the window's children before it can even start. The
                // bridge earns its place on the elements the pattern refuses: a read-only
                // wrapper, or a control that publishes no Value pattern at all.
                if (TrySetTextValue(text))
                    return;

                if (TryBridge(BrinellVerb.SetText, text, out _))
                    return;

                PhysicalInput.Used("FlaUIMauiElement.SendKeys(SetValue fallback)", "the SetText verb");
                FocusForKeyboardInput();
                Keyboard.Type(text);
                break;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (TrySetTextValue(string.Empty))
            return;

        if (TryBridge(BrinellVerb.ClearText))
            return;

        // Select all and delete
        PhysicalInput.Used("FlaUIMauiElement.Clear(Ctrl+A,Delete)", "the ClearText verb");
        FocusForKeyboardInput();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(VirtualKeyShort.DELETE);
    }

    /// <inheritdoc />
    public void DoubleClick()
    {
        PhysicalInput.Used("FlaUIMauiElement.DoubleClick", "the DoubleTap gesture verb (step 18)");
        _driver.EnsureRootWindowFocused();
        _element.DoubleClick();
    }

    /// <inheritdoc />
    public void RightClick()
    {
        PhysicalInput.Used("FlaUIMauiElement.RightClick", "the InvokeMenuItem verb (step 26)");
        _driver.EnsureRootWindowFocused();
        _element.RightClick();
    }

    /// <inheritdoc />
    public void Hover()
    {
        var rect = _element.BoundingRectangle;
        var center = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        PhysicalInput.Used("FlaUIMauiElement.Hover", "a pointer-enter verb (not yet planned)");
        _driver.EnsureRootWindowFocused();
        Mouse.MoveTo(center);
    }

    /// <inheritdoc />
    public void LongPress(int durationMs = 1000)
    {
        var rect = _element.BoundingRectangle;
        var center = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        _driver.PointerLongPress(center, durationMs);
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
            PhysicalInput.Used("FlaUIMauiElement.Swipe(wheel)", "the ScrollTo verb (step 21)");
            _driver.EnsureRootWindowFocused();
            Mouse.MoveTo(center);
            Mouse.Scroll(wheelClicks);
            WaitHelper.Pause(200); // Wait for scroll to settle
            return;
        }

        // Non-scroll gestures: simulate with mouse drag
        _driver.PointerDrag(
            new Point(startX, startY),
            new Point(endX, endY),
            durationMs);
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
    /// <remarks>
    /// The bridge raises the control's own completion command, which is what Enter would have
    /// caused. It refuses when the app handles completion with an event handler rather than a
    /// bound command - <c>Entry.SendCompleted</c> is internal in MAUI, so there is genuinely no
    /// public route - and then the real Enter below is the only way to reach the app's
    /// behaviour.
    /// </remarks>
    public void Submit()
    {
        // Exchange, not Invoke, even though it carries no strings. Submit lives in the text
        // range and the provider answers the whole range on one path; splitting a range across
        // both methods is how a verb ends up reaching the app down a route that has never heard
        // of it, and being refused for a reason that has nothing to do with the element.
        if (TryBridge(BrinellVerb.Submit))
            return;

        PhysicalInput.Used("FlaUIMauiElement.Submit(Enter)", "the Submit verb");
        FocusForKeyboardInput();
        Keyboard.Type(VirtualKeyShort.ENTER);
    }

    #endregion

    #region Internal

    /// <summary>
    /// Gets the underlying FlaUI AutomationElement for internal use.
    /// </summary>
    internal AutomationElement Element => _element;

    #region IFocusPatternElement Implementation

    /// <inheritdoc />
    /// <remarks>UIA can always focus an element; there is nothing to advertise.</remarks>
    public bool SupportsSetFocus => true;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>The bridge first, and this is the difference the whole of stage B turns on.</b>
    /// Focus and the desktop foreground window are separate things that the physical path had
    /// to conflate: it calls <c>SetForeground</c> because the global keystrokes that usually
    /// follow go wherever the foreground is. Asking for focus on its own needs none of that, so
    /// the bridge route leaves the machine with whoever is sitting at it — the app can be
    /// occluded, or off to one side, and the caret still lands in the right field.
    /// </para>
    /// <para>
    /// The fallback is the old path in full, because an app without the bridge must keep
    /// working exactly as it did.
    /// </para>
    /// </remarks>
    public bool SetFocus()
    {
        if (TryBridge(BrinellVerb.Focus))
            return true;

        try
        {
            FocusForKeyboardInput();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    /// <summary>
    /// Brings the app to the front, then gives this element keyboard focus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both halves are needed. FlaUI's <c>Focus()</c> only activates the window when the element
    /// *is* a window; for a control it takes the <c>FocusNative</c> branch, which sets keyboard
    /// focus without raising anything — so the global <c>Keyboard</c> input that follows would
    /// reach whichever window is actually in front.
    /// </para>
    /// <para>
    /// <b>Not routed through the bridge, deliberately.</b> Every caller of this is about to send
    /// real keystrokes, and those need the foreground however the focus was obtained. The bridge
    /// belongs one level up, in <see cref="SetFocus"/> and in the text verbs, where focus is the
    /// whole request rather than the setup for a keystroke.
    /// </para>
    /// </remarks>
    private void FocusForKeyboardInput()
    {
        _driver.EnsureRootWindowFocused();
        _element.Focus();
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

    #region Gestures (Brinell UI Automation bridge)

    /// <inheritdoc />
    /// <remarks>
    /// Answered by asking the app under test what it declared for this element, not by
    /// inspecting the control. A <c>SwipeView</c> that has not opted in reports false, which is
    /// correct: nothing can drive it semantically until the app says so.
    /// </remarks>
    public bool SupportsGesture(MauiGesture gesture)
        => GestureRunner.Supports(_driver.RootElement, _driver.Automation, AutomationId, gesture);

    /// <inheritdoc />
    /// <remarks>
    /// Nothing is cached between calls. A page can be navigated away from and back, and its
    /// bridge elements are republished each time with new runtime ids; an answer cached against
    /// this element would be about the previous incarnation.
    /// </remarks>
    /// <exception cref="GestureUnavailableException">
    /// The app publishes no bridge, this element was not declared, or the verb was refused.
    /// </exception>
    public void PerformGesture(MauiGesture gesture)
        => GestureRunner.Perform(_driver.RootElement, _driver.Automation, AutomationId, gesture);

    /// <inheritdoc />
    /// <remarks>
    /// Appending is the one text operation that cannot be assembled out of a read and a write
    /// from this side: between the two calls the app is still running, and anything it does to
    /// the field lands in the middle. The bridge does both on one pass of the app's UI thread,
    /// which is why this is a verb of its own rather than sugar over <c>GetText</c> and
    /// <c>SetText</c>.
    /// </remarks>
    public bool TryAppendText(string text)
        => TryBridge(BrinellVerb.AppendText, text, out _);

    /// <inheritdoc />
    /// <remarks>
    /// The physical route for this is a Tab keystroke, which does not so much clear focus as
    /// move it to whatever happens to be next - a different operation with a visible side
    /// effect, and one that needs the app in front. The verb removes focus and does nothing
    /// else.
    /// </remarks>
    public bool TryClearFocus() => TryBridge(BrinellVerb.Unfocus);

    /// <summary>
    /// Sends a verb to this element, and says whether the app performed it.
    /// </summary>
    /// <remarks>
    /// The bridge rung of every ladder in this class. False covers all the ordinary negatives -
    /// the app has no bridge, this element was never declared, the element refused the verb -
    /// because at this level they call for the same response: try the next rung.
    /// </remarks>
    /// <param name="verb">The verb to send.</param>
    /// <returns>Whether the app performed it.</returns>
    private bool TryBridge(BrinellVerb verb) => TryBridge(verb, string.Empty, out _);

    /// <summary>Sends a verb with an argument, and says whether the app performed it.</summary>
    /// <param name="verb">The verb to send.</param>
    /// <param name="argument">Its argument.</param>
    /// <param name="result">What the app returned.</param>
    /// <returns>Whether the app performed it.</returns>
    private bool TryBridge(BrinellVerb verb, string argument, out string result)
    {
        var outcome = BridgeVerbRunner.Send(
            _driver.RootElement, _driver.Automation, AutomationId, verb, argument);

        result = outcome.Value;
        return outcome.Delivered;
    }

    #endregion
}
