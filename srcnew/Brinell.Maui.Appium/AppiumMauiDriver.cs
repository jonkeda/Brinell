using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Maui.Enums;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Brinell.Maui.Appium;

/// <summary>
/// Appium-based implementation of <see cref="IMauiDriver"/>.
/// Delegates all operations to the underlying AppiumDriver.
/// </summary>
public sealed class AppiumMauiDriver : IMauiDriver, IDisposable, Brinell.Core.Recording.IRecordable
{
    private readonly AppiumDriver _driver;
    private readonly MauiPlatform _platform;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new AppiumMauiDriver wrapper.
    /// </summary>
    /// <param name="driver">The AppiumDriver to wrap.</param>
    /// <param name="platform">The platform this driver is connected to.</param>
    /// <exception cref="ArgumentNullException">Thrown when driver is null.</exception>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown for any platform but Android and iOS. Windows is driven by <c>Brinell.Maui.FlaUI</c>,
    /// and the element code here has no desktop routes to fall back on.
    /// </exception>
    public AppiumMauiDriver(AppiumDriver driver, MauiPlatform platform)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));

        if (platform is not (MauiPlatform.Android or MauiPlatform.iOS))
        {
            throw new PlatformNotSupportedException(
                $"{nameof(AppiumMauiDriver)} drives Android and iOS only, not {platform}. "
                + "MAUI on Windows is driven by Brinell.Maui.FlaUI - MauiDriverFactory chooses it.");
        }

        _platform = platform;
    }
    
    #region Platform
    
    /// <inheritdoc />
    public MauiPlatform Platform => _platform;

    /// <summary>
    /// The Android device the session drives, by the serial UiAutomator2 reports (<c>deviceUDID</c>,
    /// else the <c>udid</c> asked for); null on iOS, where a simulator and a device cannot be told apart here.
    /// </summary>
    public Brinell.Core.Recording.RecordingTarget? RecordingTarget
        => _platform == MauiPlatform.Android && !_disposed
           && (_driver.Capabilities.GetCapability("deviceUDID") ?? _driver.Capabilities.GetCapability("udid"))?.ToString() is { Length: > 0 } serial
            ? new Brinell.Core.Recording.RecordingTarget.AndroidDevice(serial)
            : null;
    
    #endregion
    
    #region Element Finding

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        var by = locator.ToBy(_platform);

        try
        {
            return _driver.FindElements(by).Select(e => new AppiumMauiElement(e, this)).ToList();
        }
        catch (Exception error) when (AppiumErrors.IsSessionGone(error))
        {
            throw new AppUnavailableException("the driver session has ended.", error);
        }
    }

    /// <summary>
    /// Finds a piece of app chrome the driver acts on itself, waiting for it to appear.
    /// </summary>
    /// <param name="locator">The chrome element's locator.</param>
    /// <param name="timeoutMs">How long to wait for it to appear.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">It did not appear in time.</exception>
    internal IMauiElement FindChrome(Locator locator, int timeoutMs)
    {
        var by = locator.ToBy(_platform);
        var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(timeoutMs));

        try
        {
            var element = wait.Until(d => d.FindElement(by));
            return new AppiumMauiElement((AppiumElement)element, this);
        }
        catch (WebDriverTimeoutException)
        {
            throw new ElementNotFoundException(locator);
        }
    }

    /// <summary>
    /// Finds every match for a piece of app chrome, waiting for at least one to appear.
    /// </summary>
    /// <param name="locator">The chrome elements' locator.</param>
    /// <param name="timeoutMs">How long to wait for the first to appear.</param>
    /// <returns>The matches; empty when none appeared in time.</returns>
    internal IReadOnlyList<IMauiElement> FindAllChrome(Locator locator, int timeoutMs)
    {
        var by = locator.ToBy(_platform);
        var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(timeoutMs));

        try
        {
            wait.Until(d => d.FindElements(by).Count > 0);
        }
        catch (WebDriverTimeoutException)
        {
            return Array.Empty<IMauiElement>();
        }

        return FindElements(locator);
    }

    #endregion

    #region The app

    /// <inheritdoc />
    /// <remarks>
    /// Stands for the whole screen. It finds a root node only when a member needs one; the
    /// app-level members address the screen and do not.
    /// </remarks>
    public IMauiElement AppElement => AppiumMauiElement.ForApp(this);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Android renders Shell's tabs as a bottom navigation bar whose items are frame layouts
    /// carrying the tab's title as their content description - the only frame layouts on the page
    /// that carry one, which is what makes the locator exact rather than merely plausible.
    /// </para>
    /// <para>
    /// <b>The hosts are found beneath the Shell, never above it.</b> They are looked up in the
    /// page's scope, whose root is the Shell's own view (the <c>DrawerLayout</c>, once the app
    /// publishes its id), and a scope only searches down. They used to be <c>android:id/content</c>,
    /// the frame <i>above</i> the Shell, which no scoped lookup can reach: every tab and flyout
    /// collection reported zero items. Each host is named by what it holds, as measured on the
    /// device.
    /// </para>
    /// <para>
    /// The drawer's items are view groups carrying their title as a content description, inside
    /// the drawer's recycler view, and they leave the tree entirely while the drawer is shut. iOS
    /// has not been mapped and throws.
    /// </para>
    /// </remarks>
    public ShellChromeLocators ShellChrome => _platform == MauiPlatform.Android
        ? AndroidShellChrome
        : throw new PlatformNotSupportedException(
            $"Shell chrome on {_platform} has not been mapped. Dump the tree and add it to "
            + $"{nameof(AppiumMauiDriver)}.");

    private static readonly ShellChromeLocators AndroidShellChrome = new(
        TabHost: Locator.ByXPath("//android.view.ViewGroup[android.widget.FrameLayout[@content-desc!='']]"),
        Tab: Locator.ByXPath("//android.widget.FrameLayout[@content-desc!='']"),
        FlyoutHost: Locator.ByXPath(
            "//androidx.recyclerview.widget.RecyclerView[.//android.view.ViewGroup[@content-desc!='']]"),
        FlyoutItem: Locator.ByXPath("//android.view.ViewGroup[@content-desc!='']"));

    /// <summary>The hierarchy's root node, for the app element's members that need a node.</summary>
    internal AppiumElement FindRootNode()
    {
        var by = _platform == MauiPlatform.Android
            ? By.XPath("/hierarchy/*[1]")
            : MobileBy.ClassName("XCUIElementTypeApplication");

        return (AppiumElement)_driver.FindElement(by);
    }

    #endregion

    #region Window Management
    
    /// <inheritdoc />
    public string CurrentWindowHandle => _driver.CurrentWindowHandle;
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> WindowHandles => _driver.WindowHandles;
    
    #endregion
    
    #region Session Management
    
    /// <inheritdoc />
    public void Quit() => _driver.Quit();
    
    /// <inheritdoc />
    public void Close() => _driver.Close();
    
    #endregion
    
    #region Screenshots
    
    /// <inheritdoc />
    public byte[] GetScreenshot() => _driver.GetScreenshot().AsByteArray;
    
    #endregion
    
    #region Context Switching
    
    /// <inheritdoc />
    public string Context
    {
        get => _driver.Context;
        set => _driver.Context = value;
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> Contexts => _driver.Contexts;
    
    #endregion
    
    #region Script Execution
    
    /// <inheritdoc />
    public object? ExecuteScript(string script, params object[] args)
    {
        return _driver.ExecuteScript(script, args);
    }
    
    #endregion
    
    #region IDiagnosticDriver
    
    /// <inheritdoc />
    public string GetPageSource() => _driver.PageSource;
    
    /// <inheritdoc />
    public string GetAutomationTree() => _driver.PageSource;
    
    #endregion
    
    #region Navigation
    
    /// <inheritdoc />
    public void NavigateTo(string destination) => _driver.Navigate().GoToUrl(destination);
    
    /// <inheritdoc />
    /// <remarks>
    /// <b>Sends back whatever <see cref="IsAtNavigationRoot"/> thinks.</b> That answer reads the
    /// toolbar, and not everything above the first page has one: an Android modal page - a
    /// CommunityToolkit popup, say - covers the app and reports no way back. Refusing here on that
    /// reading was tried and blocked the one press that dismisses such a page, so the judgement
    /// stays with the caller, which can see what it expected to be on screen.
    /// </remarks>
    public void NavigateBack()
    {
        using var mark = Brinell.Core.Recording.Interactions.Report(Brinell.Core.Recording.InteractionKind.Key, () => System.Drawing.Rectangle.Empty, key: "Back");

        // iOS: XCUITest's back taps the navigation bar's back button and silently does nothing
        // when the app hides that bar behind its own title bar. The system's left-edge swipe is
        // the platform's other way back and does not depend on the bar.
        if (_platform == MauiPlatform.iOS && _driver.FindElements(IosNavigationBar).Count == 0)
        {
            var size = _driver.Manage().Window.Size;
            _driver.ExecuteScript("mobile: dragFromToForDuration", new Dictionary<string, object>
            {
                { "duration", 0.1 },
                { "fromX", 1 },
                { "fromY", size.Height / 2 },
                { "toX", size.Width * 3 / 4 },
                { "toY", size.Height / 2 },
            });
            return;
        }

        // Android: with the soft keyboard up, the system's back only hides the keyboard. A prompt
        // left open by a failed test showed it - one back hid the keyboard, the dialog stayed, and
        // every later test in the class failed "could not get back to the hub". Hiding it first
        // makes this one back mean what the caller asked for.
        if (_platform == MauiPlatform.Android && IsKeyboardShown())
        {
            _driver.ExecuteScript("mobile: hideKeyboard", new Dictionary<string, object>());
        }

        _driver.Navigate().Back();
    }

    private bool IsKeyboardShown()
        => _driver.ExecuteScript("mobile: isKeyboardShown", new Dictionary<string, object>()) is true;

    private static readonly By IosNavigationBar = MobileBy.ClassName("XCUIElementTypeNavigationBar");

    /// <summary>
    /// The toolbar's up button, which a <c>NavigationPage</c> shows on every page above its root.
    /// </summary>
    private static readonly By AndroidNavigateUp =
        By.XPath("//android.widget.ImageButton[@content-desc='Navigate up']");

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Android: true when the toolbar shows no "Navigate up" button. Without this override the
    /// interface default answered true on every page, so <c>MauiFixture</c> never went back and every
    /// test after the first failed to reach the hub.
    /// </para>
    /// <para>
    /// iOS keeps the default: its navigation bar has not been mapped.
    /// </para>
    /// </remarks>
    public bool IsAtNavigationRoot()
        => _platform != MauiPlatform.Android || _driver.FindElements(AndroidNavigateUp).Count == 0;

    /// <inheritdoc />
    /// <remarks>
    /// A lower bound on Android: 1 at the root, otherwise 2. The toolbar shows whether there is a
    /// page to go back to, not how many; to unwind several, loop on <see cref="IsAtNavigationRoot"/>.
    /// </remarks>
    public int NavigationDepth() => IsAtNavigationRoot() ? 1 : 2;
    
    /// <inheritdoc />
    public void Refresh() => _driver.Navigate().Refresh();
    
    /// <inheritdoc />
    public byte[] TakeScreenshot() => GetScreenshot();
    
    /// <inheritdoc />
    public void ResetAppState()
    {
        var bundleId = _driver.Capabilities.GetCapability("appPackage")?.ToString()
                    ?? _driver.Capabilities.GetCapability("bundleId")?.ToString();
        
        if (!string.IsNullOrEmpty(bundleId))
        {
            _driver.TerminateApp(bundleId);
            _driver.ActivateApp(bundleId);
        }
    }
    
    #endregion
    
    #region Dialogs

    /// <summary>The dialog on screen, or null. Answers <c>AppElement.TryFindActiveDialog</c>.</summary>
    internal IMauiElement? TryFindActiveDialogRoot()
    {
        var by = _platform switch
        {
            // Any package: the framework AlertDialog's root is android:id/parentPanel, but MAUI's
            // DisplayAlert is AndroidX AppCompat's, whose ids carry the app's package
            // (probed 2026-09-19 in the Todo sample: com.brinell.samples.todo:id/parentPanel).
            // Looking for the framework id only never found a MAUI alert at all.
            MauiPlatform.Android => MobileBy.AndroidUIAutomator("new UiSelector().resourceIdMatches(\".*:id/parentPanel\")"),
            MauiPlatform.iOS => MobileBy.ClassName("XCUIElementTypeAlert"),
            _ => null
        };

        if (by == null)
            return null;

        var root = _driver.FindElements(by).LastOrDefault();
        return root == null ? null : new AppiumMauiElement(root, this);
    }

    /// <inheritdoc />
    /// <remarks>Every gesture is touch input on Android and iOS; see <c>AppiumMauiElement.PerformGesture</c>.</remarks>
    public bool SupportsGesture(string automationId, MauiGesture gesture)
        => _platform is MauiPlatform.Android or MauiPlatform.iOS;

    /// <inheritdoc />
    public void PerformGesture(string automationId, MauiGesture gesture)
        => PerformGesture(automationId, gesture, 0, 0);

    /// <inheritdoc />
    /// <remarks>
    /// The element as <c>AppElement.TryFindDeclared</c> finds it - by resource id, then accessibility
    /// id, which is what MAUI maps an <c>AutomationId</c> to - and the gesture as real touch input on
    /// it. A gesture's target is often a container with no text of its own: a RefreshView, a SwipeView.
    /// </remarks>
    /// <exception cref="ElementNotFoundException">No element carries the id now.</exception>
    public void PerformGesture(string automationId, MauiGesture gesture, int arg1, int arg2)
    {
        // Below the fold it is not in the tree at all - UiAutomator2 leaves out what the user
        // cannot see - so the page is scrolled to it, as a user would before swiping it.
        var target = (AppElement.TryFindDeclared(automationId)
                      ?? TryFindByScrollingWithin(null, Locator.ByAutomationId(automationId))) as AppiumMauiElement
            ?? throw new ElementNotFoundException(Locator.ByAutomationId(automationId));

        target.PerformGesture(gesture, arg1, arg2);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The device's locale as <c>mobile: deviceInfo</c> reports it (<c>en_US</c>), as the culture's
    /// standard data: a device knows nothing of this machine's custom formats. Null when the
    /// device does not say.
    /// </remarks>
    public System.Globalization.CultureInfo? DeviceCulture
    {
        get
        {
            try
            {
                if (_driver.ExecuteScript("mobile: deviceInfo", new Dictionary<string, object>())
                        is IDictionary<string, object> info
                    && info.TryGetValue("locale", out var locale)
                    && locale is string name
                    && name.Length > 0)
                {
                    return System.Globalization.CultureInfo.GetCultureInfo(name.Replace('_', '-'));
                }
            }
            catch (System.Globalization.CultureNotFoundException)
            {
                // A locale .NET does not know: the machine's culture it is, as before.
            }

            return null;
        }
    }

    /// <summary>
    /// Whether an element this session finds is necessarily on screen: Android, where UiAutomator2
    /// leaves invisible elements out of its tree unless <c>allowInvisibleElements</c> is set.
    /// </summary>
    internal bool FindsOnlyVisibleElements => _findsOnlyVisible ??=
        _platform == MauiPlatform.Android
        && !string.Equals(
            Convert.ToString(_driver.Capabilities.GetCapability("settings[allowInvisibleElements]"),
                System.Globalization.CultureInfo.InvariantCulture),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private bool? _findsOnlyVisible;

    /// <summary>Screen pixels per device-independent unit, read once per session.</summary>
    internal double PixelsPerUnit()
    {
        if (_pixelsPerUnit is { } known)
        {
            return known;
        }

        var scale = 1.0;
        if (_platform == MauiPlatform.Android
            && _driver.ExecuteScript("mobile: deviceInfo", new Dictionary<string, object>())
                is IDictionary<string, object> info
            && info.TryGetValue("displayDensity", out var dpi)
            && double.TryParse(
                Convert.ToString(dpi, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var density)
            && density > 0)
        {
            scale = density / 160.0;
        }

        _pixelsPerUnit = scale;
        return scale;
    }

    private double? _pixelsPerUnit;

    /// <summary>The alert on screen as MAUI was asked for it, or null. Answers <c>AppElement.ReadAlert</c>.</summary>
    /// <remarks>
    /// Android only. AppCompat's alert keeps its title, message and both buttons inside
    /// <c>parentPanel</c>, by ids that carry the app's package (see <see cref="TryFindActiveDialogRoot"/>).
    /// MAUI sets the accept as the positive button (<c>button1</c>) and the cancel as the negative one
    /// (<c>button2</c>), so an alert with one button has only <c>button2</c> and reads with an empty
    /// accept, as on Windows.
    /// </remarks>
    internal AlertContents? ReadActiveAlert()
    {
        if (_platform != MauiPlatform.Android)
        {
            return null;
        }

        var root = _driver.FindElements(AndroidDialogRoot).LastOrDefault();
        if (root == null)
        {
            return null;
        }

        string Part(string id)
            => root.FindElements(MobileBy.AndroidUIAutomator(
                    $"new UiSelector().resourceIdMatches(\".*:id/{id}\")"))
                .FirstOrDefault()?.Text ?? string.Empty;

        return new AlertContents(Part("alertTitle"), Part("message"), Part("button1"), Part("button2"));
    }

    private static readonly By AndroidDialogRoot =
        MobileBy.AndroidUIAutomator("new UiSelector().resourceIdMatches(\".*:id/parentPanel\")");

    #endregion
    
    #region Scrolling

    /// <summary>
    /// Finds an element by scrolling a container until it enters the tree. Answers
    /// <c>IMauiElement.TryFindByScrolling</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A lookup, a swipe, a lookup</b>: down from where the scroller is, then up, each swipe one
    /// <c>mobile: scrollGesture</c>, which also says whether the scroller can move further. It
    /// replaced <c>UiScrollable.scrollIntoView</c>, which flings to the top before every search -
    /// even when the page is there already - and cost 3.6 s a hit and 11 s a miss per matcher, two
    /// matchers a miss (measured over a night's runs; 60 % of the scroll-heavy classes' time).
    /// </para>
    /// <para>
    /// An <c>AutomationId</c> is looked for as MAUI maps it: the resource id, then the content
    /// description.
    /// </para>
    /// </remarks>
    /// <param name="container">The container to scroll, or null for the first scrollable on screen.</param>
    /// <param name="locator">The locator for the element.</param>
    internal IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator)
    {
        if (_platform != MauiPlatform.Android)
        {
            return null;
        }

        if (FindOnScreen(locator) is { } already)
        {
            return already;
        }

        var scroller = _driver.FindElements(MobileBy.AndroidUIAutomator(ScrollableSelector(container))).FirstOrDefault();
        if (scroller is null)
        {
            return null;
        }

        foreach (var direction in ScrollDirections)
        {
            for (var swipe = 0; swipe < MaxScrollSwipes; swipe++)
            {
                bool more;
                try
                {
                    more = _driver.ExecuteScript("mobile: scrollGesture", new Dictionary<string, object>
                    {
                        { "elementId", ((AppiumElement)scroller).Id },
                        { "direction", direction },
                        { "percent", ScrollPercent },
                    }) is true;
                }
                catch (WebDriverException)
                {
                    return null;
                }

                if (FindOnScreen(locator) is not null)
                {
                    return ReResolveAfterScrolling(locator) ?? FindOnScreen(locator);
                }

                if (!more)
                {
                    break;
                }
            }
        }

        return null;
    }

    /// <summary>Down first: a page opens at its top, and most lookups are for what follows.</summary>
    private static readonly string[] ScrollDirections = ["down", "up"];

    /// <summary>Swipes each way before giving up; the sample's longest page is a few screens.</summary>
    private const int MaxScrollSwipes = 12;

    /// <summary>How much of the scroller one swipe moves.</summary>
    private const double ScrollPercent = 0.7;

    /// <summary>The element if it is in the tree now, by the locator, or an AutomationId's description.</summary>
    private IMauiElement? FindOnScreen(Locator locator)
    {
        var found = FindElements(locator).FirstOrDefault();
        if (found is null && locator.Strategy == LocatorStrategy.AutomationId)
        {
            found = FindElements(Locator.ByAccessibilityId(locator.Value)).FirstOrDefault();
        }

        return found;
    }

    /// <summary>
    /// The <c>UiSelector</c> naming the container to scroll.
    /// </summary>
    private static string ScrollableSelector(IMauiElement? container)
    {
        var resourceId = TryGetResourceId(container);

        return resourceId == null
            ? "new UiSelector().scrollable(true).instance(0)"
            : $"new UiSelector().resourceIdMatches(\".*{resourceId}\")";
    }

    private static string? TryGetResourceId(IMauiElement? container)
    {
        if (container == null)
        {
            return null;
        }

        try
        {
            var resourceId = container.GetAttribute("resource-id");
            return string.IsNullOrEmpty(resourceId) ? null : resourceId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Looks the element up again by its own locator once <c>UiScrollable</c> has brought it
    /// into view.
    /// </summary>
    /// <param name="locator">The caller's locator.</param>
    /// <returns>The freshly resolved element, or null to fall back to the scroll result.</returns>
    private IMauiElement? ReResolveAfterScrolling(Locator locator)
    {
        try
        {
            var elements = FindElements(locator);
            if (elements.Count == 0)
            {
                return null;
            }

            var element = elements[0];
            if (element is AppiumMauiElement appiumElement)
            {
                appiumElement.WaitUntilPositionSettles();
                appiumElement.NudgeClearOfBottomEdge();
            }

            return element;
        }
        catch
        {
            return null;
        }
    }

    #endregion
    
    #region IDisposable
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _driver.Quit();
            _disposed = true;
        }
    }
    
    #endregion
    
    #region Internal (for AppiumMauiElement access)
    
    /// <summary>
    /// Gets the underlying AppiumDriver for internal use.
    /// </summary>
    internal AppiumDriver Driver => _driver;
    
    #endregion
}
