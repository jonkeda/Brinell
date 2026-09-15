using System.Drawing;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Utilities;
using Brinell.Maui.Enums;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Brinell.Maui.Appium;

/// <summary>
/// Appium-based implementation of <see cref="IMauiElement"/> for <b>Android and iOS</b>.
/// Delegates all operations to the underlying AppiumElement.
/// </summary>
/// <remarks>
/// <para>
/// Windows is served by <c>Brinell.Maui.FlaUI</c>, not by this driver. Anything here shaped
/// for the desktop is a mistake rather than a fallback: a Windows default on
/// <c>Locator.ToBy</c> silently resolved every Android AutomationId as an AccessibilityId, and
/// an iOS-only script name (<c>mobile: setValue</c>) was called on Android where it does not
/// exist. Both were invisible until a device ran.
/// </para>
/// <remarks>
/// <para>
/// Answers <see cref="IMauiElement"/> in its own terms. It used to implement three UI Automation
/// <c>*PatternElement</c> interfaces - toggle, selection item, value - so controls written against
/// Windows could cast to them; controls now ask what they mean (<see cref="Checked"/>,
/// <see cref="SupportsInvoke"/>) and a member this platform has no route for keeps the interface's
/// default, which says so (step 107).
/// </para>
/// <para>
/// Android and iOS express toggle and selection state through different attributes, so each
/// capability reads the platform's own attribute rather than a common one. That difference
/// stops here, at the element: no control object above this layer branches on platform.
/// </para>
/// </remarks>
public sealed class AppiumMauiElement : IMauiElement
{
    private readonly AppiumElement? _wrapped;
    private readonly AppiumMauiDriver _driver;

    /// <summary>
    /// Creates a new AppiumMauiElement wrapper.
    /// </summary>
    /// <param name="element">The AppiumElement to wrap.</param>
    /// <param name="driver">The driver that owns this element.</param>
    /// <exception cref="ArgumentNullException">Thrown when element or driver is null.</exception>
    public AppiumMauiElement(AppiumElement element, AppiumMauiDriver driver)
    {
        _wrapped = element ?? throw new ArgumentNullException(nameof(element));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <summary>Creates the app element, which stands for the whole screen's hierarchy.</summary>
    private AppiumMauiElement(AppiumMauiDriver driver)
    {
        _driver = driver;
    }

    /// <summary>The app element: <see cref="IMauiDriver.AppElement"/>.</summary>
    internal static AppiumMauiElement ForApp(AppiumMauiDriver driver) => new(driver);

    /// <summary>Whether this element stands for the app rather than one of its elements.</summary>
    private bool IsApp => _wrapped is null;

    /// <summary>
    /// The Appium element: the one wrapped, or for the app element the hierarchy's root node.
    /// </summary>
    /// <remarks>
    /// Found only when a member actually needs a node. The app-level members - the flyout, the
    /// dialog, scroll-finding - address the whole screen and never do, so asking the app element
    /// for them costs no round trip.
    /// </remarks>
    private AppiumElement _element => _wrapped ?? _driver.FindRootNode();
    
    #region State Properties (IElement<IMauiElement>)
    
    /// <inheritdoc />
    public bool Visible => _element.Displayed;
    
    /// <inheritdoc />
    public bool Enabled => _element.Enabled;
    
    /// <inheritdoc />
    public bool Selected => _element.Selected;
    
    /// <inheritdoc />
    public string? Text => _element.Text;
    
    /// <inheritdoc />
    public string? TagName => _element.TagName;
    
    /// <inheritdoc />
    public Point Location => _element.Location;
    
    /// <inheritdoc />
    public Size Size => _element.Size;
    
    /// <inheritdoc />
    public Rectangle Rect => new Rectangle(Location, Size);
    
    #endregion
    
    #region Actions (IElement<IMauiElement>)
    
    /// <inheritdoc />
    public void Click() => _element.Click();

    #region Activation

    // All three are a tap, and that is not a fallback - it is how a touch platform performs
    // these semantics. Android and iOS have no automation patterns to invoke, toggle or select
    // through; a user does all three with one finger, and so does the driver.
    //
    // Writing it here, once, is the point of the design. The shared ladder used to reach the
    // same conclusion by probing three patterns that are never present on this platform, on
    // every control, on every call. See .my/fix/design-controls-know-how-to-click.md.

    /// <inheritdoc />
    public void Invoke() => Click();

    /// <inheritdoc />
    public void Toggle() => Click();

    /// <inheritdoc />
    public void Select() => Click();

    /// <inheritdoc />
    /// <remarks>True: a tap is how this platform invokes, toggles and selects alike.</remarks>
    public bool SupportsInvoke => true;

    /// <inheritdoc />
    public bool SupportsSelect => true;

    /// <inheritdoc />
    /// <remarks>
    /// A tap on the item, which is the ordinary route on a touch platform. On an item the id is not
    /// needed: the element was found already. On the app element the item is found first, by the
    /// accessibility id MAUI puts a toolbar item's <c>AutomationId</c> in - on Android the node's
    /// <c>resource-id</c> is empty and the value is in <c>content-desc</c>.
    /// </remarks>
    public void InvokeToolbarItem(string automationId)
    {
        if (!IsApp)
        {
            Click();
            return;
        }

        if (string.IsNullOrEmpty(automationId))
        {
            throw new BrinellException("Could not invoke a toolbar item: no AutomationId was given.");
        }

        _driver.FindElement(Locator.ByAccessibilityId(automationId)).Click();
    }

    #endregion

    #region Focus and text

    // The mobile routes that used to sit in the control objects, behind a Supports* question
    // whose false branch only ever ran here. Windows performs these through the app's verbs.

    /// <inheritdoc />
    /// <remarks>A tap: how a touch platform focuses a field.</remarks>
    public void Focus() => Click();

    /// <inheritdoc />
    /// <remarks>
    /// Tab, which moves focus on to the next control rather than removing it. This platform has
    /// no way to simply drop focus.
    /// </remarks>
    public void ClearFocus() => SendKeys(Keys.Tab);

    /// <inheritdoc />
    /// <remarks>Types the text at the end of what the field holds.</remarks>
    public void AppendText(string text) => SendKeys(text);

    #endregion

    #region Range

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Arrow keys.</b> Neither platform publishes a settable range value from outside, so the
    /// value is stepped from the nearer of the minimum, the maximum and the current value, then
    /// polled until it lands within a step of the target.
    /// </para>
    /// <para>
    /// No bounds or step are published here either, so these are 0, 100 and 1 - the same
    /// assumptions the Slider control made when this route lived in it. The current value is read
    /// from the element's text.
    /// </para>
    /// </remarks>
    public void SetRangeValue(double value)
    {
        const double min = 0;
        const double max = 100;
        const double step = 1;

        Click();
        WaitHelper.Pause(50);

        var current = ReadRangeText() ?? min;

        if (Math.Abs(value - min) < Math.Abs(value - current))
        {
            SendKeys(Keys.Home);
            WaitHelper.Pause(50);
            PressRepeatedly(Keys.ArrowRight, (int)Math.Round((value - min) / step));
        }
        else if (Math.Abs(value - max) < Math.Abs(value - current))
        {
            SendKeys(Keys.End);
            WaitHelper.Pause(50);
            PressRepeatedly(Keys.ArrowLeft, (int)Math.Round((max - value) / step));
        }
        else
        {
            var steps = (int)Math.Round((value - current) / step);
            PressRepeatedly(steps > 0 ? Keys.ArrowRight : Keys.ArrowLeft, Math.Abs(steps));
        }

        WaitHelper.WaitFor(
            ReadRangeText,
            reading => reading.HasValue && Math.Abs(reading.Value - value) <= Math.Max(step, 0.5),
            timeoutMs: 1000,
            pollingIntervalMs: 50);
    }

    private void PressRepeatedly(string key, int times)
    {
        for (var i = 0; i < times; i++)
        {
            SendKeys(key);
            WaitHelper.Pause(10);
        }
    }

    private double? ReadRangeText()
        => double.TryParse(Text, out var parsed) ? parsed : null;

    #endregion

    #region Gestures

    /// <inheritdoc />
    /// <remarks>
    /// <b>The honest list, not the optimistic one.</b> A touch platform could in principle
    /// synthesise all nine gestures, and saying so would make this method a promise the next one
    /// cannot keep. These five are what is implemented; the rest need W3C pointer action
    /// sequences, which nothing here writes yet.
    /// </remarks>
    public bool SupportsGesture(MauiGesture gesture) => gesture is
        MauiGesture.Tap
        or MauiGesture.SwipeLeft
        or MauiGesture.SwipeRight
        or MauiGesture.SwipeUp
        or MauiGesture.SwipeDown;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>This is where a swipe becomes a swipe on a touch device.</b> A control object names the
    /// gesture and its element performs it however the platform does - on Windows by handing a
    /// verb to the app, here by dragging across the element's own bounds. Both are written once,
    /// per platform, which is the whole shape of the design: the control knows what, the element
    /// knows how.
    /// </para>
    /// <para>
    /// A gesture that is not in <see cref="SupportsGesture"/> throws rather than doing something
    /// near enough. Delivering a press for a long press, or a tap for a pinch, would report a
    /// gesture the app never received.
    /// </para>
    /// </remarks>
    public void PerformGesture(MauiGesture gesture)
    {
        switch (gesture)
        {
            case MauiGesture.Tap:
                Click();
                return;

            case MauiGesture.SwipeLeft:
                this.SwipeLeft();
                return;

            case MauiGesture.SwipeRight:
                this.SwipeRight();
                return;

            case MauiGesture.SwipeUp:
                this.SwipeUp();
                return;

            case MauiGesture.SwipeDown:
                this.SwipeDown();
                return;

            default:
                throw new NotSupportedException(
                    $"{nameof(AppiumMauiElement)} cannot perform {gesture}. It needs a W3C pointer "
                    + "action sequence, which this driver does not write yet.");
        }
    }

    #endregion

    
    /// <inheritdoc />
    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
    {
        switch (method)
        {
            case TextInputMethod.Keys:
                _element.SendKeys(text);
                break;
            case TextInputMethod.Paste:
                // Set clipboard and paste (platform-specific)
                SetClipboardAndPaste(text);
                break;
            case TextInputMethod.SetValue:
                SetValueDirectly(text);
                break;
        }
    }
    
    private void SetClipboardAndPaste(string text)
    {
        // Clipboard/paste operations are complex and platform-specific.
        // For now, fall back to SendKeys which works across all platforms.
        // Full clipboard support can be added when we have platform-specific driver implementations.
        _element.SendKeys(text);
    }
    
    /// <summary>
    /// Sets an element's value directly, bypassing the keyboard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The script name differs per driver and there is no shared one. XCUITest exposes
    /// <c>mobile: setValue</c>; UiAutomator2 does not have that name at all and exposes
    /// <c>mobile: replaceElementValue</c> instead. Calling the iOS name on Android fails with
    /// "Unsupported execute method 'mobile: setValue', did you mean 'mobile: setUiMode'?",
    /// which reads like a driver version problem rather than the wrong API for the platform.
    /// </para>
    /// </remarks>
    private void SetValueDirectly(string text)
    {
        switch (_driver.Platform)
        {
            case MauiPlatform.Android:
                _driver.Driver.ExecuteScript("mobile: replaceElementValue", new Dictionary<string, object>
                {
                    { "elementId", _element.Id },
                    { "text", text }
                });
                break;

            case MauiPlatform.iOS:
                _driver.Driver.ExecuteScript("mobile: setValue", new Dictionary<string, object>
                {
                    { "elementId", _element.Id },
                    { "text", text }
                });
                break;

            default:
                throw NotAMobilePlatform(nameof(SetValueDirectly));
        }
    }

    /// <summary>
    /// The exception for a branch this driver cannot reach: a platform other than Android or iOS.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AppiumMauiDriver"/> refuses any other platform in its constructor, so this is a
    /// guard rather than a route.
    /// </para>
    /// <para>
    /// These branches used to hold desktop fallbacks - W3C mouse actions, <c>windows: scroll</c>,
    /// a JavaScript <c>scrollBy</c>, clearing and typing - written for Appium's Windows driver.
    /// Windows is driven by <c>Brinell.Maui.FlaUI</c>, so none of them ever ran.
    /// </para>
    /// </remarks>
    /// <param name="operation">What was asked.</param>
    /// <returns>The exception to throw.</returns>
    private PlatformNotSupportedException NotAMobilePlatform(string operation)
        => new(
            $"{nameof(AppiumMauiElement)}.{operation} has no route for {_driver.Platform}. This "
            + "driver serves Android and iOS; MAUI on Windows is driven by Brinell.Maui.FlaUI.");

    /// <inheritdoc />
    public void Clear() => _element.Clear();
    
    /// <inheritdoc />
    public void DoubleClick()
    {
        var actions = new Actions(_driver.Driver);
        actions.DoubleClick(_element).Perform();
    }
    
    /// <inheritdoc />
    public void RightClick()
    {
        var actions = new Actions(_driver.Driver);
        actions.ContextClick(_element).Perform();
    }
    
    /// <inheritdoc />
    public void Hover()
    {
        var actions = new Actions(_driver.Driver);
        actions.MoveToElement(_element).Perform();
    }
    
    /// <inheritdoc />
    public void LongPress(int durationMs = 1000)
    {
        switch (_driver.Platform)
        {
            case MauiPlatform.Android:
                _driver.Driver.ExecuteScript("mobile: longClickGesture", new Dictionary<string, object>
                {
                    { "elementId", _element.Id },
                    { "duration", durationMs }
                });
                break;
            case MauiPlatform.iOS:
                _driver.Driver.ExecuteScript("mobile: touchAndHold", new Dictionary<string, object>
                {
                    { "elementId", _element.Id },
                    { "duration", durationMs / 1000.0 } // iOS uses seconds
                });
                break;
            default:
                throw NotAMobilePlatform(nameof(LongPress));
        }
    }
    
    /// <inheritdoc />
    public void ScrollIntoView(int timeoutMs = 5000)
    {
        // Check if element is already visible
        try
        {
            if (_element.Displayed) return;
        }
        catch
        {
            // Continue with scroll attempt
        }
        
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        // Use platform-specific scrolling
        switch (_driver.Platform)
        {
            case MauiPlatform.Android:
                ScrollIntoViewAndroid(startTime, timeout);
                break;
            case MauiPlatform.iOS:
                ScrollIntoViewiOS();
                break;
            default:
                throw NotAMobilePlatform(nameof(ScrollIntoView));
        }
    }
    
    private void ScrollIntoViewAndroid(DateTime startTime, TimeSpan timeout)
    {
        try
        {
            var windowSize = _driver.Driver.Manage().Window.Size;
            var location = _element.Location;
            
            var direction = location.Y > windowSize.Height ? "down" : 
                           location.Y < 0 ? "up" : "down";
            
            var scrollParams = new Dictionary<string, object>
            {
                { "left", 50 },
                { "top", 150 },
                { "width", windowSize.Width - 100 },
                { "height", windowSize.Height - 300 },
                { "direction", direction },
                { "percent", 0.7 }
            };
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    if (_element.Displayed) return;
                }
                catch { }
                
                try
                {
                    var canScrollMore = _driver.Driver.ExecuteScript("mobile: scrollGesture", scrollParams);
                    WaitHelper.Pause(150);
                    
                    if (canScrollMore is bool canScroll && !canScroll)
                    {
                        if (direction == "down")
                        {
                            scrollParams["direction"] = "up";
                            direction = "up";
                            continue;
                        }
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }
        catch
        {
            // Scroll attempt failed
        }
    }
    
    private void ScrollIntoViewiOS()
    {
        try
        {
            var scrollParams = new Dictionary<string, object>
            {
                { "element", _element.Id },
                { "toVisible", true }
            };
            
            _driver.Driver.ExecuteScript("mobile: scroll", scrollParams);
        }
        catch
        {
            // Fallback
            try
            {
                _driver.Driver.ExecuteScript("mobile: scroll", 
                    new Dictionary<string, object> { { "direction", "down" } });
            }
            catch { }
        }
    }
    
    /// <summary>
    /// Waits until the element stops moving, so a caller acts on where it is rather than where it
    /// was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Android scrolling flings: <c>UiScrollable</c> hands back control while the container is
    /// still coasting, so a tap issued then lands at coordinates the element has already left
    /// and silently does nothing. Two identical rectangles in a row means settled; it returns as
    /// soon as the element is still rather than sleeping a fixed time.
    /// </para>
    /// <para>
    /// The mechanism is general — two identical rectangles in a row — and depends on nothing but
    /// <see cref="Rect"/>; it lives here because the need does not generalise. UIA scrolling is
    /// synchronous, and Playwright already performs this check internally as its "stable"
    /// actionability requirement. If a smooth-scrolling Windows surface ever needs it, the home
    /// is <c>ElementGeometryExtensions</c> beside <c>HasUsableBounds</c>, and it is a move rather
    /// than a rewrite.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Scrolls the element clear of the bottom of the screen when it has come to rest there.
    /// </summary>
    /// <remarks>
    /// <c>scrollIntoView</c> stops as soon as the element is on screen, which leaves it hard
    /// against the bottom edge — under Android's navigation bar, which sits above the app and
    /// swallows touches aimed at what is beneath it. The element is then visible, stationary and
    /// perfectly findable, and the tap simply does not reach it.
    /// </remarks>
    internal void NudgeClearOfBottomEdge()
    {
        try
        {
            var screenHeight = _driver.Driver.Manage().Window.Size.Height;
            var margin = screenHeight / 8;
            if (Rect.Bottom <= screenHeight - margin)
            {
                return;
            }

            _driver.Driver.ExecuteScript("mobile: scrollGesture", new Dictionary<string, object>
            {
                { "left", 50 },
                { "top", 150 },
                { "width", _driver.Driver.Manage().Window.Size.Width - 100 },
                { "height", screenHeight - 300 },
                { "direction", "down" },
                { "percent", 0.25 }
            });

            WaitUntilPositionSettles();
        }
        catch
        {
            // Not being able to move it is not a reason to refuse to act on it.
        }
    }

    internal void WaitUntilPositionSettles()
    {
        const int MaxChecks = 10;
        const int IntervalMs = 50;

        try
        {
            var previous = Rect;
            for (var check = 0; check < MaxChecks; check++)
            {
                WaitHelper.Pause(IntervalMs);
                var current = Rect;
                if (current == previous)
                {
                    return;
                }

                previous = current;
            }
        }
        catch
        {
            // A position we cannot read is one we cannot wait on; let the caller proceed.
        }
    }


    /// <inheritdoc />
    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        switch (_driver.Platform)
        {
            case MauiPlatform.Android:
                _driver.Driver.ExecuteScript("mobile: swipeGesture", new Dictionary<string, object>
                {
                    { "left", Math.Min(startX, endX) },
                    { "top", Math.Min(startY, endY) },
                    { "width", Math.Abs(endX - startX) + 1 },
                    { "height", Math.Abs(endY - startY) + 1 },
                    { "direction", GetSwipeDirection(startX, startY, endX, endY) },
                    { "percent", 1.0 },
                    { "speed", (int)(1000.0 / durationMs * 1000) }
                });
                break;
            case MauiPlatform.iOS:
                _driver.Driver.ExecuteScript("mobile: swipe", new Dictionary<string, object>
                {
                    { "startX", startX },
                    { "startY", startY },
                    { "endX", endX },
                    { "endY", endY },
                    { "duration", durationMs / 1000.0 }
                });
                break;
            default:
                throw NotAMobilePlatform(nameof(Swipe));
        }
    }
    
    private static string GetSwipeDirection(int startX, int startY, int endX, int endY)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        
        if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            return deltaX > 0 ? "right" : "left";
        }
        return deltaY > 0 ? "down" : "up";
    }
    
    #endregion
    
    #region Element Finding (IElement<IMauiElement>)
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        var by = locator.ToBy(_driver.Platform);
        
        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(_driver.Driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                var element = wait.Until(d => _element.FindElement(by));
                return new AppiumMauiElement(element, _driver);
            }
            catch (WebDriverTimeoutException)
            {
                throw new ElementNotFoundException(locator);
            }
        }
        
        try
        {
            return new AppiumMauiElement(_element.FindElement(by), _driver);
        }
        catch (NoSuchElementException)
        {
            throw new ElementNotFoundException(locator);
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        var by = locator.ToBy(_driver.Platform);
        
        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(_driver.Driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                wait.Until(d => _element.FindElements(by).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                return Array.Empty<IMauiElement>();
            }
        }
        
        var elements = _element.FindElements(by);
        return elements.Select(e => new AppiumMauiElement(e, _driver)).ToList();
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
    /// <remarks>
    /// Android reports the resource id fully qualified - <c>com.example.app:id/SaveButton</c> -
    /// where MAUI put only the <c>AutomationId</c>, so the package prefix is stripped and the
    /// caller gets back what the app author wrote. iOS carries the accessibility identifier,
    /// which the driver reports as <c>name</c>.
    /// </remarks>
    public string? AutomationId => _driver.Platform switch
    {
        MauiPlatform.Android => WithoutResourcePackage(Present(GetAttribute("resource-id"))),
        MauiPlatform.iOS => Present(GetAttribute("name")),
        _ => null
    };

    /// <inheritdoc />
    public string? Hint => _driver.Platform switch
    {
        MauiPlatform.Android => Present(GetAttribute("hint")),
        MauiPlatform.iOS => Present(GetAttribute("placeholderValue")),
        _ => null
    };

    /// <inheritdoc />
    public bool Focused => _driver.Platform switch
    {
        MauiPlatform.Android => IsTrue(GetAttribute("focused")),
        MauiPlatform.iOS => IsTrue(GetAttribute("hasFocus")),
        _ => false
    };

    private static bool IsTrue(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    /// <remarks>
    /// Android's accessible name is the content description; an element without one is named by
    /// its text, which is what a screen reader falls back to as well.
    /// </remarks>
    public string? Name => _driver.Platform switch
    {
        MauiPlatform.Android => Present(GetAttribute("content-desc")) ?? Present(_element.Text),
        MauiPlatform.iOS => Present(GetAttribute("name")) ?? Present(GetAttribute("label")),
        _ => null
    };

    /// <summary>
    /// An attribute value, or null when there is none.
    /// </summary>
    /// <remarks>
    /// UiAutomator2 reports a missing attribute as the four characters <c>null</c> rather than
    /// as nothing, so an element with no resource id answers "null" to a caller comparing ids.
    /// An element whose content really is the word "null" is misread here, which is the price
    /// of a driver that does not distinguish the two.
    /// </remarks>
    private static string? Present(string? value)
        => string.IsNullOrEmpty(value) || value == "null" ? null : value;

    /// <summary>
    /// Takes the identifier out of an Android resource id.
    /// </summary>
    private static string? WithoutResourcePackage(string? resourceId)
    {
        if (string.IsNullOrEmpty(resourceId)) return resourceId;

        var separator = resourceId.LastIndexOf('/');
        return separator >= 0 ? resourceId[(separator + 1)..] : resourceId;
    }

    /// <inheritdoc />
    /// <remarks>
    /// An attribute the platform does not expose reads as null rather than throwing. UiAutomator2
    /// raises <c>NotImplementedException</c> for anything outside its fixed list, so a control
    /// probing several candidate names — as the progress and placeholder readers do — would abort
    /// on the first miss, while the same probe returns null on Windows.
    /// </remarks>
    public string? GetAttribute(string attributeName)
    {
        try
        {
            return _element.GetAttribute(attributeName);
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public string? GetDomAttribute(string attributeName) => _element.GetDomAttribute(attributeName);
    
    /// <inheritdoc />
    public string? GetDomProperty(string propertyName) => _element.GetDomProperty(propertyName);
    
    /// <inheritdoc />
    public string? GetCssValue(string propertyName) => _element.GetCssValue(propertyName);
    
    /// <inheritdoc />
    public void Submit() => _element.Submit();
    
    #endregion

    #region Checked state

    /// <summary>
    /// The attribute carrying checked state, per platform.
    /// </summary>
    /// <remarks>
    /// Android surfaces <c>checked</c> ("true"/"false") on CheckBox and Switch. iOS surfaces
    /// <c>value</c> ("1"/"0") on a UISwitch. Anything else has no known attribute, so the
    /// element reports no checked state rather than guessing.
    /// </remarks>
    private string? ToggleStateAttribute => _driver.Platform switch
    {
        MauiPlatform.Android => "checked",
        MauiPlatform.iOS => "value",
        _ => null
    };

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Null unless the element really has a checked state. Android reports <c>checked="false"</c>
    /// on <i>every</i> view, a plain Button included, so the attribute being present cannot
    /// distinguish a real toggle; <c>checkable</c> is the attribute that does.
    /// </para>
    /// </remarks>
    public bool? Checked
    {
        get
        {
            var attribute = ToggleStateAttribute;
            if (attribute == null) return null;

            try
            {
                if (_driver.Platform == MauiPlatform.Android && !IsAttributeTrue("checkable"))
                {
                    return null;
                }

                var value = _element.GetAttribute(attribute);
                if (string.IsNullOrEmpty(value)) return null;

                return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("1", StringComparison.Ordinal);
            }
            catch
            {
                // A driver may throw rather than return null for an absent attribute.
                return null;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Neither platform has a set-state command, so this reads the state and taps when it
    /// differs. The control verifies the state it asked for.
    /// </remarks>
    public void SetChecked(bool isChecked)
    {
        if (Checked == isChecked)
            return;

        Toggle();
    }

    /// <summary>
    /// Whether an attribute is present and reads as true.
    /// </summary>
    private bool IsAttributeTrue(string name)
    {
        try
        {
            var value = _element.GetAttribute(name);
            return !string.IsNullOrEmpty(value)
                && value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Scrolling

    /// <summary>Margin in pixels kept away from an element's edges when swiping.</summary>
    private const int EdgeInset = 20;

    /// <summary>
    /// An element shorter (or narrower) than this cannot be swiped meaningfully - the start and end
    /// points would collapse onto each other.
    /// </summary>
    private const int MinimumSwipeExtent = 40;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>A swipe across the element's own bounds</b>, one per step: from far to near drags the
    /// content towards the start, revealing what follows. A swipe cannot report whether anything
    /// moved, so a performed step is <see cref="ScrollStep.Unconfirmed"/> and the caller checks
    /// the content.
    /// </para>
    /// <para>
    /// This geometry lived in the cross-platform <c>ScrollHelper</c>, behind a
    /// <c>SupportsScrollContent</c> question this platform always answered false.
    /// </para>
    /// </remarks>
    public ScrollStep ScrollContent(int verticalSteps, int horizontalSteps = 0)
    {
        if (verticalSteps == 0 && horizontalSteps == 0)
            return ScrollStep.NotMoved;

        var rect = Rect;

        if (verticalSteps != 0)
        {
            if (rect.Height <= MinimumSwipeExtent)
                return ScrollStep.NotMoved;

            var centerX = rect.X + (rect.Width / 2);
            var near = rect.Y + EdgeInset;
            var far = rect.Y + rect.Height - EdgeInset;

            for (var step = 0; step < Math.Abs(verticalSteps); step++)
            {
                if (verticalSteps > 0)
                    Swipe(centerX, far, centerX, near);
                else
                    Swipe(centerX, near, centerX, far);
            }
        }

        if (horizontalSteps != 0)
        {
            if (rect.Width <= MinimumSwipeExtent)
                return ScrollStep.NotMoved;

            var centerY = rect.Y + (rect.Height / 2);
            var near = rect.X + EdgeInset;
            var far = rect.X + rect.Width - EdgeInset;

            for (var step = 0; step < Math.Abs(horizontalSteps); step++)
            {
                if (horizontalSteps > 0)
                    Swipe(far, centerY, near, centerY);
                else
                    Swipe(near, centerY, far, centerY);
            }
        }

        return ScrollStep.Unconfirmed;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <c>UiScrollable</c> rooted at this element, or on the app element at the first scrollable
    /// container on screen. Android only; iOS answers null until it is written and run on a device.
    /// </remarks>
    public IMauiElement? TryFindByScrolling(Locator locator)
        => _driver.TryFindByScrollingWithin(IsApp ? null : this, locator);

    #endregion

    #region Selection

    /// <summary>
    /// The rows of the dialog an Android <c>Picker</c> opens: MAUI builds it with
    /// <c>AlertDialog.Builder.SetItems</c>, which lists them under the framework's own id.
    /// </summary>
    private static readonly Locator AndroidPickerItems =
        Locator.ByXPath("//*[@resource-id='android:id/select_dialog_listview']/*");

    /// <summary>How long the picker's dialog gets to show its rows.</summary>
    private const int PickerItemsTimeoutMs = 5000;

    /// <inheritdoc />
    /// <remarks>The picker's own text, which is its selected item.</remarks>
    public string? SelectedItemText => Text;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Taps the picker open and taps the row showing the text. The rows are the ones the dialog has
    /// realized, so an item far down a long list is not found until something scrolls to it.
    /// </para>
    /// <para>
    /// <b>Compiled, not yet run on a device.</b> This replaces the control layer's tap route, which
    /// tapped the picker open and then threw because nothing listed its items. iOS draws a picker
    /// wheel instead and has no route yet.
    /// </para>
    /// </remarks>
    public void SelectByText(string text)
    {
        var items = OpenPicker(nameof(SelectByText));
        var item = items.FirstOrDefault(i => i.Text == text);

        if (item == null)
        {
            DismissPicker();
            throw new InvalidOperationException(
                $"'{AutomationId}' has no item with text '{text}' among the {items.Count} its dialog shows.");
        }

        item.Click();
    }

    /// <inheritdoc />
    /// <remarks>See <see cref="SelectByText"/>: the same route and the same caveats.</remarks>
    public void SelectIndex(int index)
    {
        var items = OpenPicker(nameof(SelectIndex));

        if (index < 0 || index >= items.Count)
        {
            DismissPicker();
            throw new ArgumentOutOfRangeException(
                nameof(index), index, $"'{AutomationId}' shows {items.Count} item(s); there is no index {index}.");
        }

        items[index].Click();
    }

    private IReadOnlyList<IMauiElement> OpenPicker(string operation)
    {
        if (_driver.Platform != MauiPlatform.Android)
        {
            throw new PlatformNotSupportedException(
                $"{nameof(AppiumMauiElement)}.{operation} has no route for {_driver.Platform}: its "
                + "picker has not been mapped. Dump the tree and add it here.");
        }

        Click();
        return _driver.FindElements(AndroidPickerItems, PickerItemsTimeoutMs);
    }

    /// <summary>Closes the picker's dialog without choosing, so a failed selection leaves nothing open.</summary>
    private void DismissPicker()
    {
        try
        {
            _driver.Driver.Navigate().Back();
        }
        catch
        {
            // The exception about the missing item is the one worth reporting.
        }
    }

    #endregion

    #region The app

    /// <summary>How long Shell's chrome gets to appear.</summary>
    private const int ChromeFindTimeoutMs = 5000;

    /// <inheritdoc />
    /// <remarks>Taps the drawer's opener, which Android names by its content description.</remarks>
    public void OpenFlyout()
    {
        RequireApp(nameof(OpenFlyout));
        RequireAndroid("the flyout opener");

        _driver.FindElement(Locator.ByAccessibilityId("Open navigation drawer"), ChromeFindTimeoutMs).Click();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Back, which is how Android's drawer is dismissed. Its opener changes its content description
    /// once open, so it is not a handle for closing.
    /// </remarks>
    public void CloseFlyout()
    {
        RequireApp(nameof(CloseFlyout));
        RequireAndroid("dismissing the flyout");

        _driver.Driver.Navigate().Back();
    }

    /// <inheritdoc />
    /// <remarks>Null: nothing on this platform reports it, and the caller counts the drawer's items.</remarks>
    public bool? IsFlyoutOpen
    {
        get
        {
            RequireApp(nameof(IsFlyoutOpen));
            return null;
        }
    }

    /// <inheritdoc />
    /// <remarks>Null: there is no app-side alert report on this platform yet.</remarks>
    public AlertContents? ReadAlert()
    {
        RequireApp(nameof(ReadAlert));
        return null;
    }

    /// <inheritdoc />
    public IMauiElement? TryFindActiveDialog()
    {
        RequireApp(nameof(TryFindActiveDialog));
        return _driver.TryFindActiveDialogRoot();
    }

    /// <inheritdoc />
    /// <remarks>
    /// The real node, by the id MAUI maps an <c>AutomationId</c> to: the resource id on most
    /// controls, the accessibility id on the rest. Null where it is not in the tree.
    /// </remarks>
    public IMauiElement? TryFindDeclared(string automationId)
    {
        RequireApp(nameof(TryFindDeclared));

        if (string.IsNullOrEmpty(automationId))
            return null;

        return _driver.FindElements(Locator.ByAutomationId(automationId)).FirstOrDefault()
            ?? _driver.FindElements(Locator.ByAccessibilityId(automationId)).FirstOrDefault();
    }

    private void RequireApp(string operation)
    {
        if (!IsApp)
        {
            throw new NotSupportedException(
                $"{operation} is a question about the app, not about one element. "
                + "Ask IMauiTestContext.AppElement.");
        }
    }

    /// <summary>
    /// Refuses rather than guesses on a platform whose Shell chrome nobody has mapped.
    /// </summary>
    private void RequireAndroid(string what)
    {
        if (_driver.Platform != MauiPlatform.Android)
        {
            throw new PlatformNotSupportedException(
                $"Shell chrome on {_driver.Platform} has not been mapped: nothing is known about {what}. "
                + "Dump the tree and add it - see .my/navigation/design-shell-sample-app.md.");
        }
    }

    #endregion

    #region Internal

    /// <summary>
    /// Gets the underlying AppiumElement for internal use.
    /// </summary>
    internal AppiumElement Element => _element;

    #endregion
}
