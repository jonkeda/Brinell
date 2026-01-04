# SPEC-006-002m: Context Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. PageBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class PageBase : IPageObject
{
    protected readonly ITestContext _context;
    private readonly string _pageName;

    protected PageBase(ITestContext context, string? pageName = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _pageName = pageName ?? GetType().Name;
    }

    #region IPageObject Implementation

    public string PageName => _pageName;
    public ITestContext Context => _context;

    // Full implementation for WaitForReady with logging
    public virtual bool WaitForReady(int? timeoutMs = null)
    {
        Log($"WaitForReady()");
        var timeout = timeoutMs ?? _context.DefaultTimeout;
        return WaitUntil(IsReady, timeout);
    }

    // Full implementation for IsReady
    public virtual bool IsReady()
    {
        var indicator = GetReadyIndicator();
        if (indicator == null) return true;
        
        var ready = indicator.IsVisible();
        Log($"IsReady: {ready}");
        return ready;
    }

    // Full implementation for AssertReady
    public virtual void AssertReady(string? message = null, int? timeoutMs = null)
    {
        if (!WaitForReady(timeoutMs))
        {
            ThrowAssertionFailed("PageReady", "false", "true",
                message ?? $"Page '{PageName}' did not become ready within timeout.");
        }
        LogAssertPass("PageReady", "true", "true");
    }

    #endregion

    #region Abstract Methods

    protected abstract IControlObject? GetReadyIndicator();

    #endregion

    #region Control Factory Methods

    public T FindControl<T>(ControlLocator locator) where T : IControlObject
    {
        Log($"FindControl<{typeof(T).Name}>({locator})");
        return CreateControl<T>(locator);
    }

    protected abstract T CreateControl<T>(ControlLocator locator) where T : IControlObject;

    #endregion

    #region Logging Helpers

    protected void Log(string message)
    {
        _context.Logger?.Log(_context.TestName, PageName, null, message);
    }

    protected void LogAssertPass(string assertType, string? actual, string? expected)
    {
        _context.Logger?.LogAssertPass(_context.TestName, PageName, null, assertType, actual, expected);
    }

    protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
    {
        _context.Logger?.ThrowAssertionFailed(_context.TestName, PageName, null, assertType, actual, expected, message, _context);
    }

    #endregion

    #region Wait Helpers

    protected bool WaitUntil(Func<bool> condition, int timeoutMs)
    {
        return _context.WaitFor(condition, timeoutMs);
    }

    #endregion
}
```

---

## 2. TestContextBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class TestContextBase : ITestContext
{
    private readonly TestSettings _settings;
    private ITestLogger? _logger;

    protected TestContextBase(TestSettings? settings = null)
    {
        _settings = settings ?? TestSettings.Default;
    }

    #region ITestContext Implementation

    public string TestName { get; set; } = string.Empty;
    public int DefaultTimeout => _settings.DefaultTimeout;
    public int PollingInterval => _settings.PollingInterval;
    public ITestLogger? Logger => _logger;

    public void SetLogger(ITestLogger? logger)
    {
        _logger = logger;
    }

    // Full implementation for WaitFor with timeout handling
    public virtual bool WaitFor(Func<bool> condition, int? timeoutMs = null, string? description = null)
    {
        var timeout = timeoutMs ?? DefaultTimeout;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            try
            {
                if (condition())
                {
                    _logger?.LogWait(TestName, description, true, (int)stopwatch.ElapsedMilliseconds);
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions during polling
            }
            
            Thread.Sleep(PollingInterval);
        }
        
        _logger?.LogWait(TestName, description, false, timeout);
        return false;
    }

    #endregion

    #region Abstract Methods

    public abstract void Initialize();
    public abstract void Cleanup();
    public abstract byte[] CaptureScreenshot();

    #endregion

    #region Lifecycle

    public virtual void StartTest(string testName)
    {
        TestName = testName;
        _logger?.StartTest(testName);
    }

    public virtual void EndTest(bool success)
    {
        _logger?.EndTest(TestName, success);
    }

    #endregion
}
```

---

## 3. TestSettings

```csharp
namespace Brinell.Core;

public class TestSettings
{
    public int DefaultTimeout { get; set; } = 30000;
    public int PollingInterval { get; set; } = 100;
    public bool CaptureScreenshotOnFailure { get; set; } = true;
    public string? ScreenshotDirectory { get; set; }
    public string? LogDirectory { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Info;

    public static TestSettings Default => new TestSettings();
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
```

---

## 4. MAUI Implementation

```csharp
namespace Brinell.Maui;

public class MauiPage : PageBase
{
    protected readonly MauiTestContext _mauiContext;

    public MauiPage(MauiTestContext context, string? pageName = null)
        : base(context, pageName)
    {
        _mauiContext = context;
    }

    // Full implementation for CreateControl
    protected override T CreateControl<T>(ControlLocator locator)
    {
        // Use control factory to create platform-specific control
        return MauiControlFactory.Create<T>(locator, this, _mauiContext);
    }

    // Full implementation for GetReadyIndicator
    protected override IControlObject? GetReadyIndicator()
    {
        // Override in derived pages to specify ready indicator
        return null;
    }

    // Method signatures only
    public MauiButton Button(ControlLocator locator);
    public MauiEntry Entry(ControlLocator locator);
    public MauiLabel Label(ControlLocator locator);
    public MauiCheckBox CheckBox(ControlLocator locator);
    public MauiSwitch Switch(ControlLocator locator);
    public MauiPicker Picker(ControlLocator locator);
    public MauiSlider Slider(ControlLocator locator);
    public MauiCollectionView CollectionView(ControlLocator locator);
    public MauiScrollView ScrollView(ControlLocator locator);
    public MauiImage Image(ControlLocator locator);
}

public class MauiTestContext : TestContextBase
{
    private AppiumDriver? _driver;
    private readonly MauiTestSettings _settings;

    public MauiTestContext(MauiTestSettings? settings = null)
        : base(settings)
    {
        _settings = settings ?? new MauiTestSettings();
    }

    public AppiumDriver Driver => _driver ?? throw new InvalidOperationException("Driver not initialized.");

    // Full implementation for Initialize
    public override void Initialize()
    {
        var options = new AppiumOptions();
        options.DeviceName = _settings.DeviceName;
        options.PlatformName = _settings.PlatformName;
        options.App = _settings.AppPath;
        options.AutomationName = _settings.AutomationName;
        
        _driver = new AndroidDriver(new Uri(_settings.AppiumServerUrl), options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(DefaultTimeout);
    }

    // Full implementation for Cleanup
    public override void Cleanup()
    {
        _driver?.Quit();
        _driver = null;
    }

    // Full implementation for CaptureScreenshot
    public override byte[] CaptureScreenshot()
    {
        if (_driver == null)
            return Array.Empty<byte>();
        
        var screenshot = _driver.GetScreenshot();
        return screenshot.AsByteArray;
    }

    // Method signatures only
    public AppiumElement? FindElement(ControlLocator locator, int? timeoutMs = null);
    public IReadOnlyList<AppiumElement> FindElements(ControlLocator locator, int? timeoutMs = null);
    public void PerformGesture(GestureType gesture, Point start, Point end);
}

public class MauiTestSettings : TestSettings
{
    public string DeviceName { get; set; } = "emulator-5554";
    public string PlatformName { get; set; } = "Android";
    public string? AppPath { get; set; }
    public string AutomationName { get; set; } = "UiAutomator2";
    public string AppiumServerUrl { get; set; } = "http://localhost:4723";
}
```

---

## 5. Blazor Implementation

```csharp
namespace Brinell.Blazor;

public class BlazorPage : PageBase
{
    protected readonly BlazorTestContext _blazorContext;

    public BlazorPage(BlazorTestContext context, string? pageName = null)
        : base(context, pageName)
    {
        _blazorContext = context;
    }

    // Full implementation for CreateControl
    protected override T CreateControl<T>(ControlLocator locator)
    {
        // Use control factory to create platform-specific control
        return BlazorControlFactory.Create<T>(locator, this, _blazorContext);
    }

    // Full implementation for GetReadyIndicator
    protected override IControlObject? GetReadyIndicator()
    {
        // Override in derived pages to specify ready indicator
        return null;
    }

    // Full implementation for NavigateTo
    public void NavigateTo(string url, int? timeoutMs = null)
    {
        Log($"NavigateTo('{url}')");
        _blazorContext.Page.GotoAsync(url).GetAwaiter().GetResult();
        WaitForReady(timeoutMs);
    }

    // Full implementation for GetCurrentUrl
    public string GetCurrentUrl()
    {
        var url = _blazorContext.Page.Url;
        Log($"GetCurrentUrl: '{url}'");
        return url;
    }

    // Method signatures only
    public BlazorButton Button(ControlLocator locator);
    public BlazorInput Input(ControlLocator locator);
    public BlazorSpan Span(ControlLocator locator);
    public BlazorCheckbox Checkbox(ControlLocator locator);
    public BlazorSelect Select(ControlLocator locator);
    public BlazorRange Range(ControlLocator locator);
    public BlazorListBox ListBox(ControlLocator locator);
    public BlazorTable Table(ControlLocator locator);
    public BlazorImage Image(ControlLocator locator);
    public BlazorTabs Tabs(ControlLocator locator);
    public BlazorModal Modal(ControlLocator locator);
}

public class BlazorTestContext : TestContextBase
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;
    private readonly BlazorTestSettings _settings;

    public BlazorTestContext(BlazorTestSettings? settings = null)
        : base(settings)
    {
        _settings = settings ?? new BlazorTestSettings();
    }

    public IPage Page => _page ?? throw new InvalidOperationException("Page not initialized.");

    // Full implementation for Initialize
    public override void Initialize()
    {
        _playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
        
        _browser = _settings.BrowserType switch
        {
            BrowserType.Firefox => _playwright.Firefox.LaunchAsync(new() { Headless = _settings.Headless }).GetAwaiter().GetResult(),
            BrowserType.WebKit => _playwright.Webkit.LaunchAsync(new() { Headless = _settings.Headless }).GetAwaiter().GetResult(),
            _ => _playwright.Chromium.LaunchAsync(new() { Headless = _settings.Headless }).GetAwaiter().GetResult(),
        };
        
        _browserContext = _browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = _settings.ViewportWidth, Height = _settings.ViewportHeight }
        }).GetAwaiter().GetResult();
        
        _page = _browserContext.NewPageAsync().GetAwaiter().GetResult();
        _page.SetDefaultTimeout(_settings.DefaultTimeout);
    }

    // Full implementation for Cleanup
    public override void Cleanup()
    {
        _page?.CloseAsync().GetAwaiter().GetResult();
        _browserContext?.CloseAsync().GetAwaiter().GetResult();
        _browser?.CloseAsync().GetAwaiter().GetResult();
        _playwright?.Dispose();
        
        _page = null;
        _browserContext = null;
        _browser = null;
        _playwright = null;
    }

    // Full implementation for CaptureScreenshot
    public override byte[] CaptureScreenshot()
    {
        if (_page == null)
            return Array.Empty<byte>();
        
        return _page.ScreenshotAsync().GetAwaiter().GetResult();
    }

    // Method signatures only
    public ILocator GetLocator(ControlLocator locator, int? timeoutMs = null);
    public IReadOnlyList<ILocator> GetLocators(ControlLocator locator, int? timeoutMs = null);
}

public class BlazorTestSettings : TestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public BrowserType BrowserType { get; set; } = BrowserType.Chromium;
    public bool Headless { get; set; } = true;
    public int ViewportWidth { get; set; } = 1280;
    public int ViewportHeight { get; set; } = 720;
}

public enum BrowserType
{
    Chromium,
    Firefox,
    WebKit
}
```

---

## 6. Control Factories

```csharp
namespace Brinell.Maui;

public static class MauiControlFactory
{
    public static T Create<T>(ControlLocator locator, IPageObject? page, MauiTestContext context) where T : IControlObject
    {
        var type = typeof(T);
        
        // Map interface types to implementation types
        if (type == typeof(IClickableControlObject) || type == typeof(IButtonControlObject))
            return (T)(object)new MauiButton(locator, page, context);
        
        if (type == typeof(ITextControlObject) || type == typeof(IEditableTextControlObject))
            return (T)(object)new MauiEntry(locator, page, context);
        
        if (type == typeof(ILabelControlObject))
            return (T)(object)new MauiLabel(locator, page, context);
        
        if (type == typeof(ICheckBoxControlObject))
            return (T)(object)new MauiCheckBox(locator, page, context);
        
        if (type == typeof(ISwitchControlObject))
            return (T)(object)new MauiSwitch(locator, page, context);
        
        if (type == typeof(IPickerControlObject))
            return (T)(object)new MauiPicker(locator, page, context);
        
        if (type == typeof(ISliderControlObject))
            return (T)(object)new MauiSlider(locator, page, context);
        
        // ... additional mappings
        
        throw new NotSupportedException($"Control type {type.Name} not supported.");
    }
}

namespace Brinell.Blazor;

public static class BlazorControlFactory
{
    public static T Create<T>(ControlLocator locator, IPageObject? page, BlazorTestContext context) where T : IControlObject
    {
        var type = typeof(T);
        
        // Map interface types to implementation types
        if (type == typeof(IClickableControlObject) || type == typeof(IButtonControlObject))
            return (T)(object)new BlazorButton(locator, page, context);
        
        if (type == typeof(ITextControlObject) || type == typeof(IEditableTextControlObject))
            return (T)(object)new BlazorInput(locator, page, context);
        
        if (type == typeof(ILabelControlObject))
            return (T)(object)new BlazorSpan(locator, page, context);
        
        if (type == typeof(ICheckBoxControlObject))
            return (T)(object)new BlazorCheckbox(locator, page, context);
        
        if (type == typeof(ISelectControlObject))
            return (T)(object)new BlazorSelect(locator, page, context);
        
        if (type == typeof(IRangeControlObject))
            return (T)(object)new BlazorRange(locator, page, context);
        
        // ... additional mappings
        
        throw new NotSupportedException($"Control type {type.Name} not supported.");
    }
}
```

---

**Next:** [SPEC-006-002n: Exception Classes](SPEC-006-002-CLASSES-EXCEPTIONS.md)
