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
    public AppiumMauiDriver(AppiumDriver driver, MauiPlatform platform)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
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
    public void NavigateBack() => _driver.Navigate().Back();
    
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
    
    #region Popup Windows
    
    /// <inheritdoc />
    /// <remarks>
    /// On Appium (Android/iOS), dialogs are part of the normal element tree,
    /// so this delegates to <see cref="FindElement"/>.
    /// </remarks>
    public IMauiElement FindPopupElement(Locator locator, int timeoutMs = 5000)
        => FindElement(locator, timeoutMs);
    
    /// <inheritdoc />
    public bool TryFindPopupElement(Locator locator, out IMauiElement? element, int timeoutMs = 0)
        => TryFindElement(locator, out element, timeoutMs);
    
    #endregion
    
    #region Scrolling

    /// <inheritdoc />
    /// <remarks>
    /// Android only. iOS would be served by <c>mobile: scroll</c> with the container as
    /// <c>element</c>; until that is written and run on a device, answering null keeps the
    /// caller on the plain-lookup result rather than on an untested path.
    /// </remarks>
    public IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator)
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
            (element as AppiumMauiElement)?.WaitUntilPositionSettles();
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
