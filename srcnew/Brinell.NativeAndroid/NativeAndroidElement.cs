namespace Brinell.NativeAndroid;

/// <summary>
/// Native Android element wrapper around AppiumElement.
/// </summary>
public sealed class NativeAndroidElement : IElement<NativeAndroidElement>
{
    private readonly AppiumElement element;
    private readonly NativeAndroidDriver driver;

    public NativeAndroidElement(AppiumElement element, NativeAndroidDriver driver)
    {
        this.element = element ?? throw new ArgumentNullException(nameof(element));
        this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public AppiumElement RawElement => element;

    public bool Visible => element.Displayed;

    public bool Enabled => element.Enabled;

    public bool Selected => element.Selected;

    public string? Text => element.Text;

    public string? TagName => element.TagName;

    public Point Location => element.Location;

    public Size Size => element.Size;

    public Rectangle Rect => new(Location, Size);

    public string? ResourceId => GetAttribute("resource-id");

    public string? ContentDescription => GetAttribute("content-desc");

    public string? ClassName => GetAttribute("class");

    public void Click() => element.Click();

    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
    {
        ArgumentNullException.ThrowIfNull(text);

        switch (method)
        {
            case TextInputMethod.Keys:
            case TextInputMethod.Paste:
                element.SendKeys(text);
                break;
            case TextInputMethod.SetValue:
                driver.RawDriver.ExecuteScript("mobile: setValue", new Dictionary<string, object>
                {
                    ["elementId"] = element.Id,
                    ["text"] = text
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported text input method.");
        }
    }

    public void Clear() => element.Clear();

    public void DoubleClick()
    {
        var actions = new Actions(driver.RawDriver);
        actions.DoubleClick(element).Perform();
    }

    public void RightClick()
    {
        var actions = new Actions(driver.RawDriver);
        actions.ContextClick(element).Perform();
    }

    public void Hover()
    {
        var actions = new Actions(driver.RawDriver);
        actions.MoveToElement(element).Perform();
    }

    public void LongPress(int durationMs = 1000)
    {
        driver.RawDriver.ExecuteScript("mobile: longClickGesture", new Dictionary<string, object>
        {
            ["elementId"] = element.Id,
            ["duration"] = durationMs
        });
    }

    public void ScrollIntoView(int timeoutMs = 5000)
    {
        if (TryIsDisplayed())
        {
            return;
        }

        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMs);
        var windowSize = driver.RawDriver.Manage().Window.Size;
        var scrollArea = new Dictionary<string, object>
        {
            ["left"] = 20,
            ["top"] = 120,
            ["width"] = Math.Max(1, windowSize.Width - 40),
            ["height"] = Math.Max(1, windowSize.Height - 240),
            ["direction"] = "down",
            ["percent"] = 0.75
        };

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                driver.RawDriver.ExecuteScript("mobile: scrollGesture", scrollArea);
            }
            catch (WebDriverException)
            {
                return;
            }

            if (TryIsDisplayed())
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        driver.RawDriver.ExecuteScript("mobile: swipeGesture", new Dictionary<string, object>
        {
            ["left"] = Math.Min(startX, endX),
            ["top"] = Math.Min(startY, endY),
            ["width"] = Math.Max(1, Math.Abs(endX - startX)),
            ["height"] = Math.Max(1, Math.Abs(endY - startY)),
            ["direction"] = GetSwipeDirection(startX, startY, endX, endY),
            ["percent"] = 1.0,
            ["speed"] = Math.Max(1, durationMs)
        });
    }

    public string? GetAttribute(string name) => element.GetAttribute(name);

    public NativeAndroidElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var by = locator.ToAndroidBy();

        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(driver.RawDriver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                var child = wait.Until(_ => element.FindElement(by));
                return new NativeAndroidElement(child, driver);
            }
            catch (WebDriverTimeoutException)
            {
                throw new ElementNotFoundException(locator, timeoutMs);
            }
        }

        try
        {
            return new NativeAndroidElement(element.FindElement(by), driver);
        }
        catch (NoSuchElementException)
        {
            throw new ElementNotFoundException(locator, timeoutMs);
        }
    }

    public IReadOnlyList<NativeAndroidElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var by = locator.ToAndroidBy();

        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(driver.RawDriver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                wait.Until(_ => element.FindElements(by).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                return Array.Empty<NativeAndroidElement>();
            }
        }

        return element.FindElements(by)
            .Select(child => new NativeAndroidElement(child, driver))
            .ToList();
    }

    public bool TryFindElement(Locator locator, out NativeAndroidElement? child, int timeoutMs = 0)
    {
        try
        {
            child = FindElement(locator, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            child = null;
            return false;
        }
    }

    private bool TryIsDisplayed()
    {
        try
        {
            return element.Displayed;
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    private static string GetSwipeDirection(int startX, int startY, int endX, int endY)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;

        return Math.Abs(deltaX) > Math.Abs(deltaY)
            ? deltaX > 0 ? "right" : "left"
            : deltaY > 0 ? "down" : "up";
    }
}
