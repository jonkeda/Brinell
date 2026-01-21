using System.Drawing;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Maui.Enums;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Brinell.Maui.Appium;

/// <summary>
/// Appium-based implementation of <see cref="IMauiElement"/>.
/// Delegates all operations to the underlying AppiumElement.
/// </summary>
public sealed class AppiumMauiElement : IMauiElement
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
                // Use direct value setting (bypasses keyboard)
                _driver.Driver.ExecuteScript("mobile: setValue", new Dictionary<string, object>
                {
                    { "elementId", _element.Id },
                    { "text", text }
                });
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
                    Thread.Sleep(150);
                    
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
    public string? GetAttribute(string attributeName) => _element.GetAttribute(attributeName);
    
    /// <inheritdoc />
    public string? GetDomAttribute(string attributeName) => _element.GetDomAttribute(attributeName);
    
    /// <inheritdoc />
    public string? GetDomProperty(string propertyName) => _element.GetDomProperty(propertyName);
    
    /// <inheritdoc />
    public string? GetCssValue(string propertyName) => _element.GetCssValue(propertyName);
    
    /// <inheritdoc />
    public void Submit() => _element.Submit();
    
    #endregion
    
    #region Internal
    
    /// <summary>
    /// Gets the underlying AppiumElement for internal use.
    /// </summary>
    internal AppiumElement Element => _element;
    
    #endregion
}
