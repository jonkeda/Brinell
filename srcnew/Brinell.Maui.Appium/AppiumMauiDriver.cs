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
public sealed class AppiumMauiDriver : IMauiDriver, IDisposable
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
    
    #endregion
    
    #region Element Finding (IDriver<IMauiElement>)
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        var by = locator.ToBy(_platform);
        
        if (timeoutMs > 0)
        {
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
        
        try
        {
            return new AppiumMauiElement((AppiumElement)_driver.FindElement(by), this);
        }
        catch (NoSuchElementException)
        {
            throw new ElementNotFoundException(locator);
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        var by = locator.ToBy(_platform);
        
        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                // Wait for at least one element to appear
                wait.Until(d => d.FindElements(by).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                // No elements found within timeout, return empty list
                return Array.Empty<IMauiElement>();
            }
        }
        
        var elements = _driver.FindElements(by);
        return elements.Select(e => new AppiumMauiElement(e, this)).ToList();
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
    /// device - see <c>.my/navigation/rca-android-return-to-hub.md</c>.
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
    /// See <c>.my/navigation/rca-android-return-to-hub.md</c>.
    /// </remarks>
    public void NavigateBack() => _driver.Navigate().Back();

    /// <summary>
    /// The toolbar's up button, which a <c>NavigationPage</c> shows on every page above its root.
    /// </summary>
    /// <remarks>
    /// Android's English content description (<c>abc_action_bar_up_description</c>); a device in
    /// another language shows a translated one.
    /// </remarks>
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

    #endregion
    
    #region Scrolling

    /// <summary>
    /// Finds an element by scrolling a container until it enters the tree. Answers
    /// <c>IMauiElement.TryFindByScrolling</c>.
    /// </summary>
    /// <remarks>
    /// Android only. iOS would be served by <c>mobile: scroll</c> with the container as
    /// <c>element</c>; until that is written and run on a device, answering null keeps the
    /// caller on the plain-lookup result rather than on an untested path.
    /// </remarks>
    /// <param name="container">The container to scroll, or null for the first scrollable on screen.</param>
    /// <param name="locator">The locator for the element.</param>
    internal IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator)
    {
        if (_platform != MauiPlatform.Android)
        {
            return null;
        }

        var scrollable = ScrollableSelector(container);

        // resource-id first, then content-desc: MAUI maps AutomationId to one or the other
        // depending on the control.
        string[] matchers =
        [
            $"new UiSelector().resourceIdMatches(\".*{locator.Value}\")",
            $"new UiSelector().description(\"{locator.Value}\")"
        ];

        foreach (var matcher in matchers)
        {
            try
            {
                var elements = FindByUiAutomator(
                    $"new UiScrollable({scrollable}).scrollIntoView({matcher})");
                if (elements.Count > 0)
                {
                    return ReResolveAfterScrolling(locator) ?? elements[0];
                }
            }
            catch
            {
                // Try the next matcher.
            }
        }

        return null;
    }

    /// <summary>
    /// The <c>UiSelector</c> naming the container to scroll.
    /// </summary>
    /// <remarks>
    /// A named container is scrolled by its own resource-id. Without one there is nothing to
    /// name, so the selector falls back to the first scrollable container on screen — which is
    /// wrong wherever an outer <c>ScrollView</c> wraps the container that actually scrolls, and
    /// is why the parameter exists.
    /// </remarks>
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
    /// <remarks>
    /// <c>UiScrollable(...).scrollIntoView(...)</c> is a scrolling command that happens to
    /// return a node. Acting on that node is not the same as acting on the element: it is
    /// matched by a <c>resourceIdMatches</c> regex during the scroll rather than by the caller's
    /// own locator. Re-resolving costs one lookup, now that the element is on screen, and gives
    /// the caller the element it actually asked for.
    /// </remarks>
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

    /// <summary>
    /// Runs a raw UIAutomator query. Android-only, and private: a caller that wants this has
    /// already chosen to be Android-specific, and none exists outside the scroll path.
    /// </summary>
    private IReadOnlyList<IMauiElement> FindByUiAutomator(string uiAutomatorQuery)
    {
        try
        {
            var elements = _driver.FindElements(MobileBy.AndroidUIAutomator(uiAutomatorQuery));
            return elements.Select(e => new AppiumMauiElement(e, this)).ToList();
        }
        catch
        {
            return Array.Empty<IMauiElement>();
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
