using Brinell.Core.Diagnostics;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Utilities;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Drawing;
using System.Globalization;
using FlaUI.Core.Patterns;
using Brinell.Maui.Configuration;
using Brinell.Maui.FlaUI.Bridge;
using Brinell.Maui.Interfaces;
using Brinell.Uia;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiElement"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// </summary>
/// <remarks>
/// UI Automation patterns are this class's implementation detail, not its API. It used to
/// implement eight <c>*PatternElement</c> interfaces that controls cast to; controls now ask
/// <see cref="IMauiElement"/> in terms of what they do, and this class answers from the patterns
/// (step 107).
/// </remarks>
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
    /// <remarks>
    /// The SelectionItem pattern and nothing else. It used to fall back to the Toggle pattern, so
    /// a checked CheckBox reported itself selected; checked state is <see cref="Checked"/> (step 105a).
    /// </remarks>
    public bool Selected
    {
        get
        {
            try
            {
                return _element.Patterns.SelectionItem.IsSupported
                       && _element.Patterns.SelectionItem.Pattern.IsSelected.Value;
            }
            catch
            {
                return false;
            }
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

    #region Text field state

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
    public string? Value
    {
        get
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
    }

    /// <inheritdoc />
    public bool? IsReadOnly
    {
        get
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
        _driver.Pointer.Click(_element);
    }

    #region Activation

    /// <inheritdoc />
    /// <remarks>
    /// The Invoke pattern, and nothing else. Where a control genuinely cannot be invoked - a
    /// MAUI <c>ToolbarItem</c> is the known case - the control says <c>Click</c> instead; it is
    /// not this method's job to guess a substitute.
    /// </remarks>
    public void Invoke() => Perform(
        nameof(Invoke), SupportsInvoke, RunInvokePattern, "InvokePattern");

    /// <inheritdoc />
    public void Toggle() => Perform(
        nameof(Toggle), SupportsToggle, RunTogglePattern, "TogglePattern");

    /// <inheritdoc />
    public void Select() => Perform(
        nameof(Select), SupportsSelect, RunSelectionItemPattern, "SelectionItemPattern");

    /// <inheritdoc />
    public bool SupportsInvoke => HasPattern(() => _element.Patterns.Invoke.IsSupported);

    /// <inheritdoc />
    public bool SupportsToggle => HasPattern(() => _element.Patterns.Toggle.IsSupported);

    /// <inheritdoc />
    public bool SupportsSelect => HasPattern(() => _element.Patterns.SelectionItem.IsSupported);

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
                FocusForKeyboardInput("FlaUIMauiElement.SendKeys(Keys)", "nothing, when the test is of the input pipeline; the SetText verb otherwise");
                Keyboard.Type(text);
                break;
            case TextInputMethod.Paste:
                if (TryBridge(BrinellVerb.SetText, text, out _))
                    return;

                FocusForKeyboardInput("FlaUIMauiElement.SendKeys(Paste)", "the SetText verb - this one also destroys the user's clipboard");
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

                FocusForKeyboardInput("FlaUIMauiElement.SendKeys(SetValue fallback)", "the SetText verb");
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
        FocusForKeyboardInput("FlaUIMauiElement.Clear(Ctrl+A,Delete)", "the ClearText verb");
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(VirtualKeyShort.DELETE);
    }

    /// <inheritdoc />
    public void DoubleClick()
    {
        _driver.Pointer.DoubleClick(_element);
    }

    /// <inheritdoc />
    public void RightClick()
    {
        // Still physical, and still right for what it is - see PhysicalPointer.RightClick.
        _driver.Pointer.RightClick(_element);
    }

    /// <inheritdoc />
    public void Hover()
    {
        _driver.Pointer.Hover(_element);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>The bridge's <c>LongPress</c> verb where the app declares it, the real pointer where it
    /// does not</b> - one question, then one route (step 103). The verb has no duration: the app
    /// raises its own long-press handling, which is the thing a test of it wants. The pointer
    /// route is refused under a quiet run, with a message naming the verb.
    /// </remarks>
    public void LongPress(int durationMs = 1000)
    {
        if (SupportsGesture(MauiGesture.LongPress))
        {
            PerformGesture(MauiGesture.LongPress);
            return;
        }

        _driver.Pointer.LongPress(_element, durationMs);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The UIA Scroll pattern on this element, or on the nearest scrollable ancestor. The route
    /// for an app that has not declared the bridge's scroll verbs; it is not pointer input.
    /// </remarks>
    public bool SupportsScrollContent => FindScrollPattern() != null;

    /// <inheritdoc />
    public bool ScrollContent(int verticalSteps, int horizontalSteps = 0)
    {
        var scroll = FindScrollPattern()
            ?? throw new NotSupportedException(
                $"'{AutomationId ?? Name ?? "(unnamed)"}' neither exposes the UI Automation Scroll "
                + "pattern nor sits inside anything that does.");

        if (verticalSteps == 0 && horizontalSteps == 0)
            return false;

        var before = (scroll.VerticalScrollPercent.ValueOrDefault, scroll.HorizontalScrollPercent.ValueOrDefault);

        try
        {
            scroll.Scroll(ToAmount(horizontalSteps), ToAmount(verticalSteps));
        }
        catch (Exception)
        {
            // UIA refuses a scroll past the end with an error rather than a no-op, and a dead
            // element fails the same way. Both are "did not move", which is what is reported.
            return false;
        }

        // The scroll percent does not update synchronously: read immediately it reports the
        // pre-scroll value, making a successful scroll look like no progress.
        return WaitForScrollChange(scroll, before);
    }

    /// <summary>
    /// Polls until either scroll percent moves away from where it was.
    /// </summary>
    /// <returns>True if it moved; false if it stayed put for the whole window.</returns>
    private static bool WaitForScrollChange(IScrollPattern scroll, (double Vertical, double Horizontal) before)
    {
        const int budgetMs = 500;
        const int intervalMs = 25;

        for (var waited = 0; waited < budgetMs; waited += intervalMs)
        {
            if (Math.Abs(scroll.VerticalScrollPercent.ValueOrDefault - before.Vertical) > 0.01
                || Math.Abs(scroll.HorizontalScrollPercent.ValueOrDefault - before.Horizontal) > 0.01)
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
        try
        {
            for (var candidate = _element; candidate != null; candidate = candidate.Parent)
            {
                if (candidate.Patterns.Scroll.IsSupported)
                    return candidate.Patterns.Scroll.Pattern;
            }
        }
        catch
        {
            // A dead element, or one whose ancestors cannot be walked: no route.
        }

        return null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Two routes, both semantic, and no guessing.</b> First the element's own ScrollItem
    /// pattern, which a list item has. If that leaves it off screen - or there is none - the
    /// nearest ancestor whose app-side element declares the bridge's <c>ScrollTo</c> verb is asked
    /// to reveal this element by id, which is exactly what a <c>ScrollView</c> knows how to do.
    /// </para>
    /// <para>
    /// <b>What went is a loop</b> that scrolled the nearest ancestor to the top, then down a large
    /// step at a time until this element stopped reporting itself off screen, then down a small
    /// step at a time - sleeping 50 ms between each, and giving up when the percentage stopped
    /// changing. It is the "scroll a bit, poll a percentage, guess" pattern the <c>ScrollTo</c>
    /// verb replaced (step 105c). An element neither route can reveal stays where it is, and the
    /// caller's visibility check reports it.
    /// </para>
    /// </remarks>
    public void ScrollIntoView(int timeoutMs = 5000)
    {
        try
        {
            if (_element.Patterns.ScrollItem.IsSupported)
            {
                _element.Patterns.ScrollItem.Pattern.ScrollIntoView();
            }

            if (!_element.IsOffscreen)
                return;
        }
        catch
        {
            // A ScrollItem that refuses is the same as none: try the container.
        }

        var id = AutomationId;
        if (string.IsNullOrEmpty(id))
            return;

        for (var ancestor = _element.Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            var container = new FlaUIMauiElement(ancestor, _driver);
            if (!string.IsNullOrEmpty(container.AutomationId) && container.SupportsScrollVerbs)
            {
                container.ScrollTo(id);
                return;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>The bridge's swipe verb for the direction, where the app declares it; the real pointer
    /// where it does not</b> (step 105b). The direction is the dominant axis of the two points.
    /// </para>
    /// <para>
    /// <b>No mouse wheel.</b> A mostly-vertical swipe used to become five wheel clicks at the
    /// element's centre, because a drag does not scroll a WinUI <c>ScrollViewer</c>. Scrolling has
    /// its own routes now - <see cref="ScrollTo"/>, <see cref="ScrollToIndex"/> and
    /// <see cref="ScrollContent"/> - and a swipe that is secretly a scroll hid which one ran.
    /// </para>
    /// </remarks>
    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;

        var direction = Math.Abs(deltaX) >= Math.Abs(deltaY)
            ? (deltaX < 0 ? MauiGesture.SwipeLeft : MauiGesture.SwipeRight)
            : (deltaY < 0 ? MauiGesture.SwipeUp : MauiGesture.SwipeDown);

        if (SupportsGesture(direction))
        {
            PerformGesture(direction);
            return;
        }

        _driver.Pointer.Drag(new Point(startX, startY), new Point(endX, endY), durationMs);
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

        FocusForKeyboardInput("FlaUIMauiElement.Submit(Enter)", "the Submit verb");
        Keyboard.Type(VirtualKeyShort.ENTER);
    }

    #endregion

    #region Internal

    /// <summary>
    /// Gets the underlying FlaUI AutomationElement for internal use.
    /// </summary>
    internal AutomationElement Element => _element;

    #region Focus

    /// <inheritdoc />
    /// <remarks>UIA can always focus an element; there is nothing to advertise.</remarks>
    public bool SupportsFocus => true;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>The bridge first, and this is the difference the whole of stage B turns on.</b>
    /// Focus and the desktop foreground window are separate things that the physical path had
    /// to conflate: it calls <c>SetForeground</c> because the global keystrokes that usually
    /// follow go wherever the foreground is. Asking for focus on its own needs none of that, so
    /// the bridge route leaves the machine with whoever is sitting at it.
    /// </para>
    /// <para>
    /// The fallback is the old path in full, because an app without the bridge must keep
    /// working exactly as it did - and like every physical route it is refused under a quiet run.
    /// </para>
    /// </remarks>
    public void Focus()
    {
        if (TryBridge(BrinellVerb.Focus))
            return;

        FocusForKeyboardInput("FlaUIMauiElement.Focus", "the Focus verb");
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
    /// <param name="site">Who is about to send keystrokes, for the physical-input record.</param>
    /// <param name="replacement">The semantic route that would avoid it, for the refusal message.</param>
    private void FocusForKeyboardInput(string site, string replacement)
    {
        // The one record for the keystrokes that follow: taking the foreground is what makes
        // them physical, so it is recorded here rather than once by the caller and again here.
        _driver.Pointer.BringAppToFront(site, replacement);
        _element.Focus();
    }

    #endregion

    #region Checked state

    /// <inheritdoc />
    /// <remarks>The Toggle pattern: <c>Switch</c> maps to ToggleSwitch, <c>CheckBox</c> to CheckBox.</remarks>
    public bool? Checked
    {
        get
        {
            try
            {
                return _element.Patterns.Toggle.IsSupported
                    ? _element.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// UI Automation has no set-state call, but a state read and a toggle against the element in
    /// hand is as close as the platform comes, and the read makes it idempotent. The control
    /// verifies the outcome.
    /// </remarks>
    public bool SupportsSetChecked => SupportsToggle;

    /// <inheritdoc />
    public void SetChecked(bool isChecked)
    {
        if (Checked == isChecked)
            return;

        Toggle();
    }

    private bool RunTogglePattern()
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

    #endregion

    #region Activation patterns

    private bool HasPattern(Func<bool> probe)
    {
        try
        {
            return probe();
        }
        catch
        {
            return false;
        }
    }

    private bool RunInvokePattern()
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

    private bool RunSelectionItemPattern()
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

    #region Range

    /// <inheritdoc />
    public double? RangeValue => ReadRange(p => p.Value.Value);

    /// <inheritdoc />
    public double? RangeMinimum => ReadRange(p => p.Minimum.Value);

    /// <inheritdoc />
    public double? RangeMaximum => ReadRange(p => p.Maximum.Value);

    /// <inheritdoc />
    public double? RangeSmallChange => ReadRange(p => p.SmallChange.Value);

    /// <inheritdoc />
    public bool SupportsSetRangeValue => HasPattern(() => _element.Patterns.RangeValue.IsSupported);

    /// <inheritdoc />
    /// <remarks>Clamped to the published bounds, as the platform would clamp a drag.</remarks>
    public void SetRangeValue(double value)
    {
        if (!SupportsSetRangeValue)
        {
            throw new NotSupportedException(
                $"'{AutomationId ?? Name ?? "(unnamed)"}' does not expose the UI Automation "
                + "RangeValue pattern, so its value cannot be set.");
        }

        var pattern = _element.Patterns.RangeValue.Pattern;
        var clamped = Math.Clamp(value, pattern.Minimum.Value, pattern.Maximum.Value);
        pattern.SetValue(clamped);
    }

    private double? ReadRange(Func<IRangeValuePattern, double> read)
    {
        try
        {
            return _element.Patterns.RangeValue.IsSupported
                ? read(_element.Patterns.RangeValue.Pattern)
                : null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Dropdown

    /// <inheritdoc />
    /// <remarks>ExpandCollapse. A WinUI <c>ComboBox</c> is the case this exists for.</remarks>
    public bool SupportsDropdown => HasPattern(() => _element.Patterns.ExpandCollapse.IsSupported);

    /// <inheritdoc />
    public bool IsDropdownOpen
        => SupportsDropdown
           && _element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value
               == ExpandCollapseState.Expanded;

    /// <inheritdoc />
    public void OpenDropdown()
    {
        RequireDropdown(nameof(OpenDropdown));

        _element.Patterns.ExpandCollapse.Pattern.Expand();

        if (!WaitHelper.WaitFor(() => IsDropdownOpen, timeoutMs: 2000, pollingIntervalMs: 50))
        {
            throw new InvalidOperationException(
                $"The dropdown on '{AutomationId ?? Name ?? "(unnamed)"}' accepted Expand and did "
                + "not report itself open.");
        }
    }

    /// <inheritdoc />
    public void CloseDropdown()
    {
        RequireDropdown(nameof(CloseDropdown));

        if (IsDropdownOpen)
        {
            _element.Patterns.ExpandCollapse.Pattern.Collapse();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Looked for among descendants, then among children: a WinUI ComboBox's popup items are
    /// reported under one or the other depending on how it was templated.
    /// </remarks>
    public IReadOnlyList<IMauiElement> ReadDropdownItems()
    {
        RequireDropdown(nameof(ReadDropdownItems));

        AutomationElement[] items = [];

        WaitHelper.WaitFor(() =>
        {
            items = _element.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
            if (items.Length > 0) return true;

            items = _element.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));
            return items.Length > 0;
        }, timeoutMs: 2000, pollingIntervalMs: 50);

        return items.Select(item => (IMauiElement)new FlaUIMauiElement(item, _driver)).ToList();
    }

    /// <inheritdoc />
    /// <remarks>The Selection pattern, which names the chosen item rather than the combo box's header.</remarks>
    public string? SelectedItemText
    {
        get
        {
            try
            {
                if (!_element.Patterns.Selection.IsSupported)
                    return null;

                var selection = _element.Patterns.Selection.Pattern.Selection.Value;
                return selection is { Length: > 0 } ? selection[0].Name : null;
            }
            catch
            {
                return null;
            }
        }
    }

    private void RequireDropdown(string operation)
    {
        if (!SupportsDropdown)
        {
            throw new NotSupportedException(
                $"'{AutomationId ?? Name ?? "(unnamed)"}' does not expose the UI Automation "
                + $"ExpandCollapse pattern, so it has no dropdown to {operation}.");
        }
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
    public bool SupportsAppendText => BridgeDeclares(BrinellVerb.AppendText);

    /// <inheritdoc />
    public void AppendText(string text)
    {
        if (!TryBridge(BrinellVerb.AppendText, text, out _))
        {
            throw new BrinellException(
                $"'{AutomationId}' did not append the text. Either it does not declare AppendText, "
                + "or the app refused - a read-only or disabled field refuses the bridge as it "
                + "refuses a keyboard.");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The physical route for this is a Tab keystroke, which does not so much clear focus as
    /// move it to whatever happens to be next - a different operation with a visible side
    /// effect, and one that needs the app in front. The verb removes focus and does nothing
    /// else.
    /// </remarks>
    public bool SupportsClearFocus => BridgeDeclares(BrinellVerb.Unfocus);

    /// <inheritdoc />
    public void ClearFocus()
    {
        if (!TryBridge(BrinellVerb.Unfocus))
        {
            throw new BrinellException(
                $"'{AutomationId}' did not give up focus. Either it does not declare Unfocus, or "
                + "the app refused.");
        }
    }

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
    #region State the platform cannot be asked for

    /// <inheritdoc />
    public bool SupportsStateReads => BridgeDeclares(BrinellVerb.GetState);

    /// <inheritdoc />
    public string ReadState(string property)
    {
        if (!TryBridge(BrinellVerb.GetState, property, out var value))
        {
            throw new NotSupportedException(
                $"'{AutomationId}' does not answer GetState('{property}'). Either the app has not "
                + "declared GetState on it, or its provider has no case for that name - the two "
                + "read alike from here, and both are changes to the app under test.");
        }

        return value;
    }

    #endregion

    #region Scrolling

    /// <inheritdoc />
    public bool SupportsScrollVerbs => BridgeDeclares(BrinellVerb.ScrollPosition);

    /// <inheritdoc />
    /// <remarks>
    /// <b>Returns when the viewport has stopped moving, not when the request was accepted.</b>
    /// MAUI's <c>ScrollToAsync</c> is asynchronous even with animation off - the app fires it and
    /// cannot await it, because the continuation needs the UI thread the verb is already holding.
    /// So the settling is done from this side, where waiting costs nothing.
    /// <para>
    /// Measured: without this, a scroll followed immediately by <c>ReadScrollPosition</c> returns
    /// the offset from before the scroll. It survived alone and failed under a full run, which is
    /// the shape that gets blamed on whatever else is going wrong at the time - in this case it
    /// spent months inside step 36's noise.
    /// </para>
    /// </remarks>
    public void ScrollTo(string automationId)
    {
        if (!TryBridge(BrinellVerb.ScrollTo, automationId, out _))
        {
            throw new BrinellException(
                $"'{AutomationId}' could not scroll to '{automationId}'. Either the scroller does "
                + "not declare the ScrollTo verb, or nothing under it carries that AutomationId - "
                + "the verb searches the whole subtree, so a miss means the id is wrong or the "
                + "element is not in this scroller.");
        }

        WaitForTheViewportToSettle();
    }

    /// <summary>
    /// Blocks until two consecutive reads of the scroll offset agree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Settled, not arrived.</b> Waiting for a particular offset would mean predicting what
    /// <c>MakeVisible</c> decides is enough, which is a MAUI layout question this side cannot
    /// answer. Two equal readings is the weakest claim that is still the one the caller needs.
    /// </para>
    /// <para>
    /// <b>Returns rather than throws when it never settles.</b> A scroller that is still moving
    /// after the budget is a real thing - an inertia animation, a slow list - and the caller's own
    /// assertion is a better place to fail than a helper that cannot know what was wanted. The
    /// budget is generous because it is only ever paid in full when something is wrong.
    /// </para>
    /// </remarks>
    private void WaitForTheViewportToSettle()
    {
        if (!SupportsScrollVerbs)
        {
            return;
        }

        const int budgetMs = 2_000;
        const int pollMs = 25;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var previous = ReadScrollPosition();

        while (clock.ElapsedMilliseconds < budgetMs)
        {
            Thread.Sleep(pollMs);

            var current = ReadScrollPosition();

            if (current.X == previous.X && current.Y == previous.Y)
            {
                return;
            }

            previous = current;
        }
    }

    /// <inheritdoc />
    public bool SupportsScrollToIndex => BridgeDeclares(BrinellVerb.ScrollToIndex);

    /// <inheritdoc />
    /// <remarks>
    /// Settles like <see cref="ScrollTo(string)"/>, and for the same measured reason: MAUI's
    /// scroll is asynchronous whatever the animation setting says, so "the request was accepted"
    /// and "the rows have moved" are different moments. A collection has the further wrinkle that
    /// the row a caller is about to look for does not exist until the scroll lands.
    /// </remarks>
    public void ScrollToIndex(int index)
    {
        if (!TryBridge(BrinellVerb.ScrollToIndex, index, 0))
        {
            throw new BrinellException(
                $"'{AutomationId}' could not scroll to index {index}. Either it does not declare "
                + "the ScrollToIndex verb, or it is not a collection, or the index is past the "
                + "end of its items.");
        }

        WaitForTheViewportToSettle();
    }

    /// <inheritdoc />
    public ScrollPosition ReadScrollPosition()
    {
        if (!TryBridge(BrinellVerb.ScrollPosition, string.Empty, out var reported))
        {
            throw new NotSupportedException(
                $"'{AutomationId}' does not answer ScrollPosition. Declare it with "
                + "uia:GestureAutomation.Verbs on the ScrollView in the app under test.");
        }

        var parts = reported.Split(',');
        if (parts.Length != 6)
        {
            throw new BrinellException(
                $"'{AutomationId}' answered ScrollPosition with '{reported}', which is not the "
                + "six comma-separated numbers the verb returns. The two ends of the bridge "
                + "disagree about the format.");
        }

        return new ScrollPosition(
            Number(parts[0]), Number(parts[1]), Number(parts[2]),
            Number(parts[3]), Number(parts[4]), Number(parts[5]));

        static double Number(string value)
            => double.Parse(value, CultureInfo.InvariantCulture);
    }

    #endregion

    #region Selection

    /// <inheritdoc />
    public bool SupportsSelectIndex => BridgeDeclares(BrinellVerb.SelectIndex);

    /// <inheritdoc />
    public bool SupportsSelectByText => BridgeDeclares(BrinellVerb.SelectByText);

    /// <inheritdoc />
    /// <remarks>
    /// The app range-checks against its own item list, so an index past the end is refused by
    /// the only party that knows how many items there are. The dropdown route counted the items
    /// the popup had rendered, which is a different number while a virtualized list is filling.
    /// </remarks>
    public void SelectIndex(int index)
    {
        var outcome = BridgeVerbRunner.Invoke(
            _driver.RootElement, _driver.Automation, AutomationId, BrinellVerb.SelectIndex, index);

        if (outcome.Delivered)
        {
            return;
        }

        // Named per answer rather than as one list of suspects. The verb distinguishes four
        // things and they call for four different fixes, so collapsing them here would undo
        // the reason the app bothers to distinguish them.
        var reason = outcome.HResult switch
        {
            HResults.E_INVALIDARG => "the index is negative",

            HResults.UIA_E_ELEMENTNOTAVAILABLE =>
                "the index is past the end of its items, or its list has not filled yet",

            HResults.UIA_E_NOTSUPPORTED =>
                "it does not declare the SelectIndex verb, it is not a picker, or an earlier "
                + "item is equal to this one - MAUI cannot hold that selection, because it "
                + "resolves SelectedItem back to the first equal entry and the two never agree",

            _ => outcome.Reason,
        };

        throw new BrinellException($"'{AutomationId}' could not select index {index}: {reason}.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// The app answers with the text it landed on, which is checked here rather than left to a
    /// later assertion: a picker whose selection is two-way bound can decline a value, and the
    /// old route would have reported that as a successful selection.
    /// </remarks>
    public void SelectByText(string text)
    {
        if (!TryBridge(BrinellVerb.SelectByText, text, out var landed))
        {
            throw new BrinellException(
                $"'{AutomationId}' could not select '{text}'. Either it does not declare the "
                + "SelectByText verb, or it is not a picker, or no item shows that text.");
        }

        if (landed != text)
        {
            throw new BrinellException(
                $"'{AutomationId}' was asked for '{text}' and now shows '{landed}'. The picker "
                + "declined the value, which a two-way bound SelectedItem can do.");
        }
    }

    #endregion

    #region Dates and times

    /// <inheritdoc />
    public bool SupportsSetDate => BridgeDeclares(BrinellVerb.SetDate);

    /// <inheritdoc />
    public bool SupportsSetTime => BridgeDeclares(BrinellVerb.SetTime);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The app sets <c>DatePicker.Date</c> and reports what the control then holds, which is not
    /// always what was sent: a picker with a <c>MinimumDate</c> or <c>MaximumDate</c> clamps. A
    /// clamped write comes back as <c>S_FALSE</c> and is raised here rather than left for an
    /// assertion about something else to discover.
    /// </para>
    /// <para>
    /// Invariant format on the wire. The value is written by a test and read by an app that may
    /// be running under any culture, and a date meaning one thing at each end is how this fails
    /// in June and passes in July.
    /// </para>
    /// </remarks>
    public void SetDate(DateTime date)
    {
        var wanted = date.ToString(BridgeDateFormat, CultureInfo.InvariantCulture);

        if (!TryBridge(BrinellVerb.SetDate, wanted, out var landed))
        {
            throw new NotSupportedException(
                $"'{AutomationId}' does not answer SetDate. Declare it with "
                + "uia:GestureAutomation.Verbs on the DatePicker in the app under test, or drive "
                + "the control through its calendar.");
        }

        if (landed != wanted)
        {
            throw new BrinellException(
                $"'{AutomationId}' was set to {wanted} and now holds '{landed}'. A DatePicker "
                + "clamps to its MinimumDate and MaximumDate, which is the usual cause.");
        }
    }

    /// <inheritdoc />
    /// <remarks>See <see cref="SetDate"/>; the same reporting applies.</remarks>
    public void SetTime(TimeSpan time)
    {
        var wanted = time.ToString(BridgeTimeFormat, CultureInfo.InvariantCulture);

        if (!TryBridge(BrinellVerb.SetTime, wanted, out var landed))
        {
            throw new NotSupportedException(
                $"'{AutomationId}' does not answer SetTime. Declare it with "
                + "uia:GestureAutomation.Verbs on the TimePicker in the app under test, or drive "
                + "the control through its clock flyout.");
        }

        if (landed != wanted)
        {
            throw new BrinellException(
                $"'{AutomationId}' was set to {wanted} and now holds '{landed}'.");
        }
    }

    /// <summary>
    /// The wire format for a date, which both ends must agree on exactly.
    /// </summary>
    /// <remarks>
    /// Duplicated from the provider's <c>MauiCapabilities.DateFormat</c> rather than shared,
    /// for the reason the whole contract project exists: the app under test is not always one
    /// Brinell can add a reference to. A mismatch here is caught by <c>DateVerbTests</c>, which
    /// round-trips a value through a real app.
    /// </remarks>
    private const string BridgeDateFormat = "yyyy-MM-dd";

    /// <summary>The wire format for a time. See <see cref="BridgeDateFormat"/>.</summary>
    private const string BridgeTimeFormat = @"hh\:mm\:ss";

    #endregion

    private bool TryBridge(BrinellVerb verb) => TryBridge(verb, string.Empty, out _);

    /// <summary>Sends a verb that carries two numbers, and says whether the app performed it.</summary>
    /// <param name="verb">The verb to send.</param>
    /// <param name="arg1">First argument, meaning defined per verb.</param>
    /// <param name="arg2">Second argument, meaning defined per verb.</param>
    /// <returns>Whether the app performed it.</returns>
    private bool TryBridge(BrinellVerb verb, int arg1, int arg2)
        => BridgeVerbRunner.Invoke(
            _driver.RootElement, _driver.Automation, AutomationId, verb, arg1, arg2).Delivered;

    /// <summary>
    /// Whether the app under test declares this verb on this element.
    /// </summary>
    /// <remarks>
    /// <b>A question asked before choosing a route, not a rung tried before another.</b> The
    /// distinction is the whole of <c>design-controls-know-how-to-click.md</c>: nothing is
    /// performed to find out, so no route can leave the app changed on its way past. The answer
    /// comes from the app's own declaration and is the same every time it is asked.
    /// </remarks>
    /// <param name="verb">The verb.</param>
    /// <returns>Whether the bridge would carry it.</returns>
    private bool BridgeDeclares(BrinellVerb verb)
        => BridgeVerbRunner.Supports(_driver.RootElement, _driver.Automation, AutomationId, verb);

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
