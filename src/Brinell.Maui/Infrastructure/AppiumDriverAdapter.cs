using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Interactions;
using Brinell.Core.Abstractions;
using Brinell.Maui.Gestures;
using PointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Brinell.Maui.Infrastructure;

/// <summary>
/// Appium driver adapter for MAUI app automation.
/// Supports Windows, Android, and iOS platforms.
/// </summary>
public class AppiumDriverAdapter : IDriverAdapter
{
    private readonly AppiumDriver _driver;
    private readonly string _platform;

    /// <summary>
    /// The underlying Appium driver.
    /// </summary>
    public AppiumDriver Driver => _driver;

    /// <summary>
    /// The underlying Appium driver (for IWebDriver access).
    /// </summary>
    public AppiumDriver CurrentDriver => _driver;
    
    /// <summary>
    /// The platform being tested (Windows, Android, iOS).
    /// </summary>
    public string Platform => _platform;

    /// <summary>
    /// The platform name in standard format.
    /// </summary>
    public string PlatformName => _platform;

    /// <summary>
    /// Check if running on Android.
    /// </summary>
    public bool IsAndroid => _platform.Equals("Android", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if running on iOS.
    /// </summary>
    public bool IsIOS => _platform.Equals("iOS", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if running on Windows.
    /// </summary>
    public bool IsWindows => _platform.Equals("Windows", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Get the device name.
    /// </summary>
    public string DeviceName => GetCapability("deviceName") ?? GetCapability("device") ?? "Unknown";

    /// <summary>
    /// Get the app ID/bundle ID.
    /// </summary>
    public string AppId => GetCapability("appPackage") ?? GetCapability("bundleId") ?? GetCapability("app") ?? "";

    /// <summary>
    /// Get the screen size.
    /// </summary>
    public System.Drawing.Size ScreenSize => _driver.Manage().Window.Size;

    /// <summary>
    /// Get driver capabilities.
    /// </summary>
    public OpenQA.Selenium.ICapabilities GetCapabilities() => _driver.Capabilities;

    private string? GetCapability(string name)
    {
        try
        {
            return _driver.Capabilities.GetCapability(name)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Create driver for Windows MAUI app.
    /// </summary>
    public AppiumDriverAdapter(string appPath, Uri appiumServerUri, TimeSpan? commandTimeout = null)
    {
        var options = new AppiumOptions
        {
            App = appPath,
            PlatformName = "Windows",
            AutomationName = "Windows"
        };
        
        var timeout = commandTimeout ?? TimeSpan.FromMinutes(2);
        _driver = new WindowsDriver(appiumServerUri, options, timeout);
        _platform = "Windows";
    }

    /// <summary>
    /// Create driver with pre-configured options.
    /// </summary>
    public AppiumDriverAdapter(AppiumDriver driver, string platform)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _platform = platform;
    }

    /// <summary>
    /// Create driver for Android MAUI app.
    /// </summary>
    public static AppiumDriverAdapter CreateAndroid(
        string appPath, 
        Uri appiumServerUri,
        string? deviceName = null,
        TimeSpan? commandTimeout = null)
    {
        var options = new AppiumOptions
        {
            App = appPath,
            PlatformName = "Android",
            AutomationName = "UiAutomator2"
        };
        
        if (!string.IsNullOrEmpty(deviceName))
            options.DeviceName = deviceName;
        
        var timeout = commandTimeout ?? TimeSpan.FromMinutes(2);
        var driver = new AndroidDriver(appiumServerUri, options, timeout);
        return new AppiumDriverAdapter(driver, "Android");
    }

    /// <summary>
    /// Create driver for iOS MAUI app.
    /// </summary>
    public static AppiumDriverAdapter CreateiOS(
        string appPath,
        Uri appiumServerUri,
        string? deviceName = null,
        string? platformVersion = null,
        TimeSpan? commandTimeout = null)
    {
        var options = new AppiumOptions
        {
            App = appPath,
            PlatformName = "iOS",
            AutomationName = "XCUITest"
        };
        
        if (!string.IsNullOrEmpty(deviceName))
            options.DeviceName = deviceName;
        if (!string.IsNullOrEmpty(platformVersion))
            options.PlatformVersion = platformVersion;
        
        var timeout = commandTimeout ?? TimeSpan.FromMinutes(2);
        var driver = new IOSDriver(appiumServerUri, options, timeout);
        return new AppiumDriverAdapter(driver, "iOS");
    }

    public IElementAdapter? FindElement(string automationId)
    {
        try
        {
            // Try accessibility ID first (MAUI AutomationId maps to this)
            var element = _driver.FindElement(MobileBy.AccessibilityId(automationId));
            return element != null ? new AppiumElementAdapter((AppiumElement)element) : null;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Find element and return the raw AppiumElement (for direct Appium access).
    /// </summary>
    public AppiumElement? FindElementDirect(string automationId)
    {
        try
        {
            var element = _driver.FindElement(MobileBy.AccessibilityId(automationId));
            return element as AppiumElement;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    public IElementAdapter? FindElementByXPath(string xpath)
    {
        try
        {
            var element = _driver.FindElement(By.XPath(xpath));
            return element != null ? new AppiumElementAdapter((AppiumElement)element) : null;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    public IReadOnlyCollection<IElementAdapter> FindElements(string automationId)
    {
        try
        {
            var elements = _driver.FindElements(MobileBy.AccessibilityId(automationId));
            return elements.Select(e => new AppiumElementAdapter((AppiumElement)e)).ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<IElementAdapter>();
        }
    }

    public void Click(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            ae.Click();
        }
    }

    public void SendKeys(IElementAdapter element, string text)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            ae.Clear();
            ae.SendKeys(text);
        }
    }

    public void Clear(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            ae.Clear();
        }
    }

    public string? GetText(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            // Try Text attribute first, then fall back to GetAttribute
            return ae.Text ?? ae.GetAttribute("text") ?? ae.GetAttribute("value");
        }
        return null;
    }

    public string? GetAttribute(IElementAdapter element, string name)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            return ae.GetAttribute(name);
        }
        return null;
    }

    public bool IsDisplayed(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            return ae.Displayed;
        }
        return false;
    }

    public bool IsEnabled(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            return ae.Enabled;
        }
        return false;
    }

    /// <summary>
    /// Find element within a container element by AutomationId.
    /// </summary>
    /// <param name="container">The container element to search within.</param>
    /// <param name="automationId">The AutomationId to find.</param>
    /// <returns>The element if found, null otherwise.</returns>
    public AppiumElement? FindElementInContainer(AppiumElement container, string automationId)
    {
        try
        {
            return container.FindElement(MobileBy.AccessibilityId(automationId)) as AppiumElement;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Find all elements within a container element by AutomationId.
    /// </summary>
    /// <param name="container">The container element to search within.</param>
    /// <param name="automationId">The AutomationId to find.</param>
    /// <returns>Collection of matching elements.</returns>
    public IReadOnlyCollection<AppiumElement> FindElementsInContainer(AppiumElement container, string automationId)
    {
        try
        {
            return container.FindElements(MobileBy.AccessibilityId(automationId))
                .Cast<AppiumElement>()
                .ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<AppiumElement>();
        }
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit();
        }
        catch
        {
            // Ignore errors during cleanup
        }
        finally
        {
            _driver?.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    #region Gesture Methods

    /// <summary>
    /// Perform a double-tap on an element.
    /// </summary>
    /// <param name="element">The element to double-tap.</param>
    public void PerformDoubleTap(AppiumElement element)
    {
        var finger = new PointerInputDevice(PointerKind.Touch, "finger");
        var actions = new ActionSequence(finger);
        
        var location = element.Location;
        var size = element.Size;
        var centerX = location.X + size.Width / 2;
        var centerY = location.Y + size.Height / 2;
        
        // First tap
        actions.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, centerX, centerY, TimeSpan.Zero));
        actions.AddAction(finger.CreatePointerDown(MouseButton.Left));
        actions.AddAction(finger.CreatePointerUp(MouseButton.Left));
        
        // Brief pause
        actions.AddAction(finger.CreatePause(TimeSpan.FromMilliseconds(100)));
        
        // Second tap
        actions.AddAction(finger.CreatePointerDown(MouseButton.Left));
        actions.AddAction(finger.CreatePointerUp(MouseButton.Left));
        
        _driver.PerformActions(new List<ActionSequence> { actions });
    }

    /// <summary>
    /// Perform a long press on an element.
    /// </summary>
    /// <param name="element">The element to long-press.</param>
    /// <param name="durationMs">Duration of the press in milliseconds.</param>
    public void PerformLongPress(AppiumElement element, int durationMs = 1000)
    {
        var finger = new PointerInputDevice(PointerKind.Touch, "finger");
        var actions = new ActionSequence(finger);
        
        var location = element.Location;
        var size = element.Size;
        var centerX = location.X + size.Width / 2;
        var centerY = location.Y + size.Height / 2;
        
        actions.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, centerX, centerY, TimeSpan.Zero));
        actions.AddAction(finger.CreatePointerDown(MouseButton.Left));
        actions.AddAction(finger.CreatePause(TimeSpan.FromMilliseconds(durationMs)));
        actions.AddAction(finger.CreatePointerUp(MouseButton.Left));
        
        _driver.PerformActions(new List<ActionSequence> { actions });
    }

    /// <summary>
    /// Perform a swipe gesture starting from an element.
    /// </summary>
    /// <param name="element">The element to swipe from.</param>
    /// <param name="direction">Direction to swipe.</param>
    /// <param name="distance">Distance to swipe in pixels.</param>
    /// <param name="durationMs">Duration of the swipe in milliseconds.</param>
    public void PerformSwipe(AppiumElement element, SwipeDirection direction, int distance = 200, int durationMs = 300)
    {
        // For Windows, use mouse-based scrolling instead of touch
        if (IsWindows)
        {
            PerformWindowsScroll(element, direction, distance);
            return;
        }

        var finger = new PointerInputDevice(PointerKind.Touch, "finger");
        var actions = new ActionSequence(finger);
        
        var location = element.Location;
        var size = element.Size;
        var startX = location.X + size.Width / 2;
        var startY = location.Y + size.Height / 2;
        
        var (endX, endY) = direction switch
        {
            SwipeDirection.Left => (startX - distance, startY),
            SwipeDirection.Right => (startX + distance, startY),
            SwipeDirection.Up => (startX, startY - distance),
            SwipeDirection.Down => (startX, startY + distance),
            _ => (startX, startY)
        };
        
        actions.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
        actions.AddAction(finger.CreatePointerDown(MouseButton.Left));
        actions.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(durationMs)));
        actions.AddAction(finger.CreatePointerUp(MouseButton.Left));
        
        _driver.PerformActions(new List<ActionSequence> { actions });
    }

    /// <summary>
    /// Perform Windows-specific scrolling using mouse drag.
    /// Uses PointerKind.Pen for Windows desktop compatibility.
    /// </summary>
    private void PerformWindowsScroll(AppiumElement element, SwipeDirection direction, int distance)
    {
        var pen = new PointerInputDevice(PointerKind.Pen, "pen");
        var actions = new ActionSequence(pen);
        
        var location = element.Location;
        var size = element.Size;
        var startX = location.X + size.Width / 2;
        var startY = location.Y + size.Height / 2;
        
        var (endX, endY) = direction switch
        {
            SwipeDirection.Left => (startX - distance, startY),
            SwipeDirection.Right => (startX + distance, startY),
            SwipeDirection.Up => (startX, startY - distance),
            SwipeDirection.Down => (startX, startY + distance),
            _ => (startX, startY)
        };
        
        // Move to element center, click, drag, release
        actions.AddAction(pen.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
        actions.AddAction(pen.CreatePointerDown(MouseButton.Left));
        actions.AddAction(pen.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(300)));
        actions.AddAction(pen.CreatePointerUp(MouseButton.Left));
        
        _driver.PerformActions(new List<ActionSequence> { actions });
        
        // Brief pause to let the UI settle after scroll
        Thread.Sleep(150);
    }

    /// <summary>
    /// Perform a swipe gesture across the entire screen.
    /// </summary>
    /// <param name="direction">Direction to swipe.</param>
    /// <param name="durationMs">Duration of the swipe in milliseconds.</param>
    public void PerformScreenSwipe(SwipeDirection direction, int durationMs = 500)
    {
        var windowSize = _driver.Manage().Window.Size;
        var centerX = windowSize.Width / 2;
        var centerY = windowSize.Height / 2;
        
        // Use mouse pointer for Windows, touch for mobile
        var pointer = IsWindows 
            ? new PointerInputDevice(PointerKind.Pen, "pen")
            : new PointerInputDevice(PointerKind.Touch, "finger");
        var actions = new ActionSequence(pointer);
        
        int startX, startY, endX, endY;
        var distance = Math.Min(windowSize.Width, windowSize.Height) / 3;
        
        switch (direction)
        {
            case SwipeDirection.Left:
                startX = centerX + distance;
                startY = centerY;
                endX = centerX - distance;
                endY = centerY;
                break;
            case SwipeDirection.Right:
                startX = centerX - distance;
                startY = centerY;
                endX = centerX + distance;
                endY = centerY;
                break;
            case SwipeDirection.Up:
                startX = centerX;
                startY = centerY + distance;
                endX = centerX;
                endY = centerY - distance;
                break;
            case SwipeDirection.Down:
                startX = centerX;
                startY = centerY - distance;
                endX = centerX;
                endY = centerY + distance;
                break;
            default:
                return;
        }
        
        actions.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
        actions.AddAction(pointer.CreatePointerDown(MouseButton.Left));
        actions.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(durationMs)));
        actions.AddAction(pointer.CreatePointerUp(MouseButton.Left));
        
        _driver.PerformActions(new List<ActionSequence> { actions });
    }

    /// <summary>
    /// Tap at specific screen coordinates.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    public void TapAtCoordinates(int x, int y)
    {
        // Use mouse pointer for Windows, touch for mobile
        var pointer = IsWindows 
            ? new PointerInputDevice(PointerKind.Pen, "pen")
            : new PointerInputDevice(PointerKind.Touch, "finger");
        var actions = new ActionSequence(pointer);
        
        actions.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, x, y, TimeSpan.Zero));
        actions.AddAction(pointer.CreatePointerDown(MouseButton.Left));
        actions.AddAction(pointer.CreatePointerUp(MouseButton.Left));
        
        _driver.PerformActions(new List<ActionSequence> { actions });
    }

    /// <summary>
    /// Drag from one location to another.
    /// </summary>
    /// <param name="startX">Start X coordinate.</param>
    /// <param name="startY">Start Y coordinate.</param>
    /// <param name="endX">End X coordinate.</param>
    /// <param name="endY">End Y coordinate.</param>
    /// <param name="durationMs">Duration of drag in milliseconds.</param>
    public void PerformDrag(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        // Use mouse pointer for Windows, touch for mobile
        var pointer = IsWindows 
            ? new PointerInputDevice(PointerKind.Pen, "pen")
            : new PointerInputDevice(PointerKind.Touch, "finger");
        var actions = new ActionSequence(pointer);
        
        actions.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
        actions.AddAction(pointer.CreatePointerDown(MouseButton.Left));
        actions.AddAction(pointer.CreatePause(TimeSpan.FromMilliseconds(100))); // Brief pause before drag
        actions.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(durationMs)));
        actions.AddAction(pointer.CreatePointerUp(MouseButton.Left));
        
        _driver.PerformActions(new List<ActionSequence> { actions });
    }

    #endregion

    #region App Lifecycle Methods

    /// <summary>
    /// Send app to background.
    /// </summary>
    public void SendToBackground()
    {
        SendToBackground(1000);
    }

    /// <summary>
    /// Send app to background for a specific duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public void SendToBackground(int durationMs)
    {
        if (_driver is AndroidDriver androidDriver)
        {
            androidDriver.BackgroundApp(TimeSpan.FromMilliseconds(durationMs));
        }
        else if (_driver is IOSDriver iosDriver)
        {
            iosDriver.BackgroundApp(TimeSpan.FromMilliseconds(durationMs));
        }
        else
        {
            // Windows - minimize window
            _driver.Manage().Window.Minimize();
            Thread.Sleep(durationMs);
            _driver.Manage().Window.Maximize();
        }
    }

    /// <summary>
    /// Bring app to foreground.
    /// </summary>
    public void BringToForeground()
    {
        if (!string.IsNullOrEmpty(AppId))
        {
            Activate(AppId);
        }
        else
        {
            _driver.Manage().Window.Maximize();
        }
    }

    /// <summary>
    /// Reset the app.
    /// </summary>
    public void ResetApp()
    {
        // Reset by terminating and relaunching
        if (!string.IsNullOrEmpty(AppId))
        {
            try
            {
                _driver.TerminateApp(AppId);
                Thread.Sleep(500);
                _driver.ActivateApp(AppId);
            }
            catch
            {
                // Fallback: just activate
                _driver.ActivateApp(AppId);
            }
        }
    }

    /// <summary>
    /// Terminate an app.
    /// </summary>
    public void Terminate(string appId)
    {
        _driver.TerminateApp(appId);
    }

    /// <summary>
    /// Activate an app.
    /// </summary>
    public void Activate(string appId)
    {
        _driver.ActivateApp(appId);
    }

    /// <summary>
    /// Get app state.
    /// Note: App state queries are platform-specific and may not be available in all driver versions.
    /// </summary>
    public AppState GetAppState(string appId)
    {
        try
        {
            // Try using the mobile:queryAppState mobile command for Android
            if (_platform.Equals("Android", StringComparison.OrdinalIgnoreCase))
            {
                var result = _driver.ExecuteScript("mobile: queryAppState", new Dictionary<string, object> { { "appId", appId } });
                if (result is long state)
                {
                    return state switch
                    {
                        0 => AppState.NotInstalled,
                        1 => AppState.NotRunning,
                        2 => AppState.RunningInBackground,
                        3 => AppState.RunningInBackground, // Suspended
                        4 => AppState.RunningInForeground,
                        _ => AppState.NotRunning
                    };
                }
            }
        }
        catch
        {
            // Fallback if the command is not supported
        }
        
        // Default: assume running in foreground if driver is active
        return AppState.RunningInForeground;
    }

    /// <summary>
    /// Install an app.
    /// </summary>
    public void InstallApp(string appPath)
    {
        _driver.InstallApp(appPath);
    }

    /// <summary>
    /// Uninstall an app.
    /// </summary>
    public void Uninstall(string appId)
    {
        _driver.RemoveApp(appId);
    }

    #endregion

    #region Orientation Methods

    /// <summary>
    /// Rotate device to portrait orientation.
    /// </summary>
    public void RotateToPortrait()
    {
        _driver.Orientation = ScreenOrientation.Portrait;
    }

    /// <summary>
    /// Rotate device to landscape orientation.
    /// </summary>
    public void RotateToLandscape()
    {
        _driver.Orientation = ScreenOrientation.Landscape;
    }

    /// <summary>
    /// Get current orientation.
    /// </summary>
    public ScreenOrientation GetOrientation()
    {
        return _driver.Orientation;
    }

    #endregion
}
