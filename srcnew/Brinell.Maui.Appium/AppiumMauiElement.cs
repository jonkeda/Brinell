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
/// Implements the two capability interfaces that mobile platforms can actually honour:
/// <see cref="ITogglePatternElement"/> and <see cref="ISelectionItemPatternElement"/>. The
/// UIA-shaped capabilities (<c>IInvokePatternElement</c>,
/// <c>ILegacyIAccessiblePatternElement</c>, <c>IExpandCollapsePatternElement</c>) are
/// deliberately <em>not</em> implemented: not
/// implementing an interface is how this element reports "unsupported", and controls then
/// fall through to <see cref="Click"/> or <see cref="Text"/>, which is correct on mobile.
/// </para>
/// <para>
/// Android and iOS express toggle and selection state through different attributes, so each
/// capability reads the platform's own attribute rather than a common one. That difference
/// stops here, at the element: no control object above this layer branches on platform.
/// </para>
/// </remarks>
public sealed class AppiumMauiElement : IMauiElement, ITogglePatternElement, ISelectionItemPatternElement
{
    private readonly AppiumElement _element;
    private readonly AppiumMauiDriver _driver;
    
    /// <summary>
    /// Creates a new AppiumMauiElement wrapper.
    /// </summary>
    /// <param name="element">The AppiumElement to wrap.</param>
    /// <param name="driver">The driver that owns this element.</param>
    /// <exception cref="ArgumentNullException">Thrown when element or driver is null.</exception>
    public AppiumMauiElement(AppiumElement element, AppiumMauiDriver driver)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }
    
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
    /// <para>
    /// Anything else clears and types: slower, needs a keyboard, but works on any driver
    /// rather than failing on the ones this method has not been taught.
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
                _element.Clear();
                _element.SendKeys(text);
                break;
        }
    }

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
                // Desktop: use Actions API
                var actions = new Actions(_driver.Driver);
                actions.ClickAndHold(_element)
                       .Pause(TimeSpan.FromMilliseconds(durationMs))
                       .Release()
                       .Perform();
                break;
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
                ScrollIntoViewWindows();
                break;
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
    
    private void ScrollIntoViewWindows()
    {
        try
        {
            _driver.Driver.ExecuteScript("windows: scroll", new Dictionary<string, object>
            {
                { "elementId", _element.Id },
                { "direction", "down" },
                { "percent", 0.5 }
            });
        }
        catch
        {
            // Try JavaScript fallback for webview
            if (_driver.Driver is IJavaScriptExecutor jsExecutor)
            {
                jsExecutor.ExecuteScript("arguments[0].scrollIntoView({behavior: 'auto', block: 'center'});", _element);
            }
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
                // Desktop: use W3C Actions (touch pointer for Windows compatibility)
                PerformSwipeWithActions(startX, startY, endX, endY, durationMs);
                break;
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
    
    private void PerformSwipeWithActions(int startX, int startY, int endX, int endY, int durationMs)
    {
        // Try touch pointer first (works on Windows Appium)
        try
        {
            var actions = new Actions(_driver.Driver);
            actions.MoveToLocation(startX, startY)
                   .ClickAndHold()
                   .Pause(TimeSpan.FromMilliseconds(100))
                   .MoveToLocation(endX, endY)
                   .Pause(TimeSpan.FromMilliseconds(durationMs))
                   .Release()
                   .Perform();
        }
        catch
        {
            // Fallback: Use JavaScript scrollBy for relative scroll
            var deltaX = endX - startX;
            var deltaY = endY - startY;
            if (_driver.Driver is IJavaScriptExecutor jsExecutor)
            {
                jsExecutor.ExecuteScript($"window.scrollBy({-deltaX}, {-deltaY});");
            }
        }
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

    #region ITogglePatternElement

    /// <summary>
    /// The attribute carrying checked state, per platform.
    /// </summary>
    /// <remarks>
    /// Android surfaces <c>checked</c> ("true"/"false") on CheckBox and Switch. iOS surfaces
    /// <c>value</c> ("1"/"0") on a UISwitch. Anything else has no known attribute, so the
    /// capability reports unsupported rather than guessing.
    /// </remarks>
    private string? ToggleStateAttribute => _driver.Platform switch
    {
        MauiPlatform.Android => "checked",
        MauiPlatform.iOS => "value",
        _ => null
    };

    /// <inheritdoc />
    /// <remarks>
    /// Support is decided by whether the element actually reports the platform's toggle
    /// attribute, not by its control type: a MAUI Switch and a CheckBox both surface it,
    /// while a Label does not, and the attribute is the only reliable way to tell them apart
    /// across drivers.
    /// </remarks>
    public bool SupportsTogglePattern
    {
        get
        {
            var attribute = ToggleStateAttribute;
            if (attribute == null) return false;

            try
            {
                // Android reports checked="false" on EVERY view, a plain Button included, so
                // "the attribute is present" cannot distinguish a real toggle. `checkable` is
                // the attribute that actually says whether this control has a checked state
                // at all. Without this gate the probe was true for everything, which is the
                // same defect that made the SelectionItem probe fire on ordinary buttons.
                if (_driver.Platform == MauiPlatform.Android
                    && !IsAttributeTrue("checkable"))
                {
                    return false;
                }

                return !string.IsNullOrEmpty(_element.GetAttribute(attribute));
            }
            catch
            {
                // A driver may throw rather than return null for an absent attribute.
                return false;
            }
        }
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

    /// <inheritdoc />
    public bool? IsTogglePatternChecked()
    {
        var attribute = ToggleStateAttribute;
        if (attribute == null) return null;

        try
        {
            var value = _element.GetAttribute(attribute);
            if (string.IsNullOrEmpty(value)) return null;

            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.Ordinal);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Neither platform exposes a toggle command, only toggle state, so this taps the element
    /// and confirms the state actually moved. Reporting success without that check would let a
    /// tap that hit a disabled or mis-located control read as a successful toggle — the same
    /// failure mode that made <c>LegacyIAccessible</c> unusable in the Windows click ladder.
    /// </remarks>
    public bool TogglePattern()
    {
        if (!SupportsTogglePattern) return false;

        var before = IsTogglePatternChecked();

        try
        {
            _element.Click();
        }
        catch
        {
            return false;
        }

        // A control that reports no state cannot be verified; treat the tap as done rather
        // than claiming a transition that cannot be observed.
        if (before == null) return true;

        return IsTogglePatternChecked() != before;
    }

    /// <inheritdoc />
    public bool SetToggleStatePattern(bool isChecked)
    {
        if (!SupportsTogglePattern) return false;

        var current = IsTogglePatternChecked();
        if (current == isChecked) return true;

        return TogglePattern() && IsTogglePatternChecked() == isChecked;
    }

    #endregion

    #region ISelectionItemPatternElement

    /// <inheritdoc />
    /// <remarks>
    /// Mobile has no selection pattern distinct from tapping: a list row is selected by being
    /// tapped. Support is therefore reported only when the element exposes selection state to
    /// confirm the result with — otherwise the control's ladder falls through to its own
    /// click, which is the same action without a false claim of pattern support.
    /// </remarks>
    public bool SupportsSelectionItemPattern
    {
        get
        {
            // Deliberately always false on mobile.
            //
            // The obvious probe - "does the element report a 'selected' attribute" - is wrong:
            // every Android view reports selected="false", including a plain
            // android.widget.Button. That made the probe true for everything, so
            // SelectItemPattern clicked, saw Selected still false, reported failure, and the
            // caller's ladder clicked AGAIN - two taps for one Click(). It is what made
            // Button_MultipleTaps_IncrementsCount see "2 times" where it expected "1 time".
            //
            // There is no mobile equivalent of the UIA SelectionItem pattern: selecting a row
            // IS tapping it. Reporting unsupported lets a control fall through to its own
            // click, which performs exactly one tap and is the correct mobile behaviour.
            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Always false, because <see cref="SupportsSelectionItemPattern"/> is. Kept rather than
    /// throwing so the interface stays honest: a caller that asks is told "not available", the
    /// same answer it gets from the probe, and the control falls through to its own click.
    /// </remarks>
    public bool SelectItemPattern() => false;

    #endregion

    #region Internal

    /// <summary>
    /// Gets the underlying AppiumElement for internal use.
    /// </summary>
    internal AppiumElement Element => _element;

    #endregion
}
