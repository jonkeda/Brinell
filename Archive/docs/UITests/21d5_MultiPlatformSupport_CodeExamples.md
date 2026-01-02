# 5. Multi-Platform Support - Code Examples

**Parent:** [Multi-Platform Support](21d5_MultiPlatformSupport.md)

---

## 5.1 TestContextFactory

```csharp
namespace Oravey.UITestFramework.Core.Infrastructure;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;

/// <summary>
/// Factory for creating platform-specific test contexts.
/// </summary>
public static class TestContextFactory
{
    /// <summary>
    /// Create test context for the specified platform.
    /// </summary>
    public static ITestContext Create(Platform platform, TestConfiguration config)
    {
        return platform switch
        {
            Platform.Windows => CreateWpfContext(config),
            Platform.WindowsMaui => CreateMauiWindowsContext(config),
            Platform.Android => CreateAndroidContext(config),
            Platform.iOS => CreateiOSContext(config),
            Platform.Web => CreateWebContext(config),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), 
                $"Unsupported platform: {platform}")
        };
    }
    
    /// <summary>
    /// Create test context from configuration file.
    /// Respects UITEST_PLATFORM environment variable override.
    /// </summary>
    public static ITestContext CreateFromConfig(string configPath = "appsettings.uitest.json")
    {
        var config = TestConfiguration.Load(configPath);
        
        // Allow environment variable override
        var envPlatform = Environment.GetEnvironmentVariable("UITEST_PLATFORM");
        if (!string.IsNullOrEmpty(envPlatform) && Enum.TryParse<Platform>(envPlatform, out var platform))
        {
            config.Platform = platform;
        }
        
        return Create(config.Platform, config);
    }
    
    private static ITestContext CreateWpfContext(TestConfiguration config)
    {
        // Dynamically load WPF assembly to avoid hard dependency
        var assembly = LoadPlatformAssembly("Oravey.UITestFramework.Wpf");
        var contextType = assembly.GetType("Oravey.UITestFramework.Wpf.Infrastructure.FlaUITestContext")!;
        return (ITestContext)Activator.CreateInstance(contextType, config.ApplicationPath, config)!;
    }
    
    private static ITestContext CreateMauiWindowsContext(TestConfiguration config)
    {
        var assembly = LoadPlatformAssembly("Oravey.UITestFramework.Maui");
        var contextType = assembly.GetType("Oravey.UITestFramework.Maui.Infrastructure.AppiumTestContext")!;
        var createMethod = contextType.GetMethod("CreateWindows")!;
        return (ITestContext)createMethod.Invoke(null, [config.ApplicationPath, config])!;
    }
    
    private static ITestContext CreateAndroidContext(TestConfiguration config)
    {
        var assembly = LoadPlatformAssembly("Oravey.UITestFramework.Maui");
        var contextType = assembly.GetType("Oravey.UITestFramework.Maui.Infrastructure.AppiumTestContext")!;
        var createMethod = contextType.GetMethod("CreateAndroid")!;
        return (ITestContext)createMethod.Invoke(null, [config.ApplicationPath, config])!;
    }
    
    private static ITestContext CreateiOSContext(TestConfiguration config)
    {
        var assembly = LoadPlatformAssembly("Oravey.UITestFramework.Maui");
        var contextType = assembly.GetType("Oravey.UITestFramework.Maui.Infrastructure.AppiumTestContext")!;
        var createMethod = contextType.GetMethod("CreateiOS")!;
        return (ITestContext)createMethod.Invoke(null, [config.ApplicationPath, config])!;
    }
    
    private static ITestContext CreateWebContext(TestConfiguration config)
    {
        var assembly = LoadPlatformAssembly("Oravey.UITestFramework.Html");
        var contextType = assembly.GetType("Oravey.UITestFramework.Html.Infrastructure.SeleniumTestContext")!;
        return (ITestContext)Activator.CreateInstance(
            contextType, 
            config.BaseUrl, 
            config.BrowserType ?? "Chrome", 
            config)!;
    }
    
    private static System.Reflection.Assembly LoadPlatformAssembly(string assemblyName)
    {
        try
        {
            return System.Reflection.Assembly.Load(assemblyName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to load platform assembly '{assemblyName}'. " +
                "Ensure the platform-specific package is referenced.", ex);
        }
    }
}
```

---

## 5.2 Platform Attributes

```csharp
namespace Oravey.UITestFramework.Core.Attributes;

using Oravey.UITestFramework.Core.Abstractions;

/// <summary>
/// Run test only on specified platforms.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PlatformAttribute : Attribute
{
    public Platform[] Platforms { get; }
    
    public PlatformAttribute(params Platform[] platforms)
    {
        Platforms = platforms;
    }
    
    public bool ShouldRun(Platform currentPlatform)
    {
        return Platforms.Contains(currentPlatform);
    }
}

/// <summary>
/// Skip test on specified platforms.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipOnPlatformAttribute : Attribute
{
    public Platform[] Platforms { get; }
    public string Reason { get; }
    
    public SkipOnPlatformAttribute(params Platform[] platforms)
    {
        Platforms = platforms;
        Reason = $"Skipped on: {string.Join(", ", platforms)}";
    }
    
    public SkipOnPlatformAttribute(string reason, params Platform[] platforms)
    {
        Platforms = platforms;
        Reason = reason;
    }
    
    public bool ShouldSkip(Platform currentPlatform)
    {
        return Platforms.Contains(currentPlatform);
    }
}

/// <summary>
/// Run test only on mobile platforms (Android, iOS).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MobileOnlyAttribute : PlatformAttribute
{
    public MobileOnlyAttribute() : base(Platform.Android, Platform.iOS) { }
}

/// <summary>
/// Run test only on desktop platforms (Windows, WindowsMaui).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class DesktopOnlyAttribute : PlatformAttribute
{
    public DesktopOnlyAttribute() : base(Platform.Windows, Platform.WindowsMaui) { }
}

/// <summary>
/// Run test only on web platform.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class WebOnlyAttribute : PlatformAttribute
{
    public WebOnlyAttribute() : base(Platform.Web) { }
}
```

---

## 5.3 xUnit Platform Skip Integration

```csharp
namespace Oravey.UITestFramework.Core.XUnit;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Attributes;
using Xunit.Sdk;

/// <summary>
/// xUnit discoverer that respects platform attributes.
/// </summary>
public class PlatformDiscoverer : IXunitTestCaseDiscoverer
{
    private readonly IMessageSink _diagnosticMessageSink;
    
    public PlatformDiscoverer(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
    }
    
    public IEnumerable<IXunitTestCase> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo factAttribute)
    {
        var currentPlatform = GetCurrentPlatform();
        
        // Check PlatformAttribute
        var platformAttr = testMethod.Method
            .GetCustomAttributes(typeof(PlatformAttribute))
            .FirstOrDefault();
        
        if (platformAttr != null)
        {
            var platforms = platformAttr.GetNamedArgument<Platform[]>("Platforms");
            if (!platforms.Contains(currentPlatform))
            {
                yield return new SkippedTestCase(
                    _diagnosticMessageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    discoveryOptions.MethodDisplayOptionsOrDefault(),
                    testMethod,
                    $"Skipped: Test requires {string.Join(" or ", platforms)}");
                yield break;
            }
        }
        
        // Check SkipOnPlatformAttribute
        var skipAttr = testMethod.Method
            .GetCustomAttributes(typeof(SkipOnPlatformAttribute))
            .FirstOrDefault();
        
        if (skipAttr != null)
        {
            var platforms = skipAttr.GetNamedArgument<Platform[]>("Platforms");
            if (platforms.Contains(currentPlatform))
            {
                var reason = skipAttr.GetNamedArgument<string>("Reason");
                yield return new SkippedTestCase(
                    _diagnosticMessageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    discoveryOptions.MethodDisplayOptionsOrDefault(),
                    testMethod,
                    reason);
                yield break;
            }
        }
        
        // Test should run
        yield return new XunitTestCase(
            _diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod);
    }
    
    private static Platform GetCurrentPlatform()
    {
        var envPlatform = Environment.GetEnvironmentVariable("UITEST_PLATFORM");
        if (!string.IsNullOrEmpty(envPlatform) && Enum.TryParse<Platform>(envPlatform, out var platform))
        {
            return platform;
        }
        return Platform.Windows; // Default
    }
}
```

---

## 5.4 Multi-Platform Test Base

```csharp
namespace Oravey.UITestFramework.Core.Testing;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;
using Oravey.UITestFramework.Core.Infrastructure;
using Xunit.Abstractions;

/// <summary>
/// Base class for multi-platform UI tests.
/// Automatically creates appropriate context based on configuration.
/// </summary>
public abstract class MultiPlatformTestBase : IDisposable
{
    protected ITestContext Context { get; private set; } = null!;
    protected ITestOutputHelper Output { get; }
    
    protected MultiPlatformTestBase(ITestOutputHelper output)
    {
        Output = output;
        InitializeContext();
    }
    
    private void InitializeContext()
    {
        Context = TestContextFactory.CreateFromConfig();
        Context.TestName = GetType().Name;
        
        Output.WriteLine($"Platform: {Context.Platform}");
        Output.WriteLine($"Automation: {Context.Platform.GetAutomationLibrary()}");
    }
    
    /// <summary>
    /// Check if current platform is mobile.
    /// </summary>
    protected bool IsMobile => Context.Platform.IsMobile();
    
    /// <summary>
    /// Check if current platform is desktop.
    /// </summary>
    protected bool IsDesktop => Context.Platform.IsDesktop();
    
    /// <summary>
    /// Check if current platform is web.
    /// </summary>
    protected bool IsWeb => Context.Platform.IsWeb();
    
    /// <summary>
    /// Execute action only on mobile platforms.
    /// </summary>
    protected void OnMobile(Action action)
    {
        if (IsMobile) action();
    }
    
    /// <summary>
    /// Execute action only on desktop platforms.
    /// </summary>
    protected void OnDesktop(Action action)
    {
        if (IsDesktop) action();
    }
    
    /// <summary>
    /// Execute action only on web platform.
    /// </summary>
    protected void OnWeb(Action action)
    {
        if (IsWeb) action();
    }
    
    public virtual void Dispose()
    {
        Context?.Dispose();
    }
}
```

---

## 5.5 Shared Page Object Example

```csharp
namespace Oravey.Tools.UITests.PageObjects;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;

/// <summary>
/// Interface for shell page - implemented per platform if needed.
/// </summary>
public interface IShellPage
{
    void WaitForPageReady();
    bool IsDisplayed();
    ISettingsPage NavigateToSettings();
}

/// <summary>
/// Base shell page with shared logic.
/// </summary>
public abstract class ShellPageBase : PageObjectBase, IShellPage
{
    protected ShellPageBase(ITestContext context) : base(context, "Shell")
    {
    }
    
    /// <summary>
    /// Navigation button AutomationId - same across platforms.
    /// </summary>
    protected abstract string SettingsButtonId { get; }
    
    public override bool IsDisplayed()
    {
        return Context.Driver.ElementExists(SettingsButtonId);
    }
    
    public abstract ISettingsPage NavigateToSettings();
}

/// <summary>
/// WPF-specific shell page.
/// </summary>
public class WpfShellPage : ShellPageBase
{
    public ButtonControl SettingsButton { get; }
    
    protected override string SettingsButtonId => "SettingsButton";
    
    public WpfShellPage(ITestContext context) : base(context)
    {
        SettingsButton = new ButtonControl(context, this, SettingsButtonId);
    }
    
    public override ISettingsPage NavigateToSettings()
    {
        Log("Navigating to Settings");
        SettingsButton.Click();
        var settings = new WpfSettingsPage(Context);
        settings.WaitForPageReady();
        return settings;
    }
}

/// <summary>
/// Web-specific shell page.
/// </summary>
public class WebShellPage : ShellPageBase
{
    public LinkControl SettingsLink { get; }
    
    protected override string SettingsButtonId => "settings-link";
    
    public WebShellPage(ITestContext context) : base(context)
    {
        SettingsLink = new LinkControl(context, this, SettingsButtonId);
    }
    
    public override ISettingsPage NavigateToSettings()
    {
        Log("Navigating to Settings");
        SettingsLink.Click();
        var settings = new WebSettingsPage(Context);
        settings.WaitForPageReady();
        return settings;
    }
}
```

---

## 5.6 Platform-Conditional Test Example

```csharp
namespace Oravey.Tools.UITests.Tests;

using FluentAssertions;
using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Attributes;
using Oravey.UITestFramework.Core.Testing;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "UITest")]
[Collection("UITests")]
public class CrossPlatformTests : MultiPlatformTestBase
{
    public CrossPlatformTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Shell_Displays_After_Launch()
    {
        // Arrange - create appropriate page object
        var shell = CreateShellPage();
        
        // Act
        shell.WaitForPageReady();
        
        // Assert
        shell.IsDisplayed().Should().BeTrue();
    }
    
    [Fact]
    [MobileOnly]
    public void Mobile_Can_Swipe_To_Refresh()
    {
        // This test only runs on Android/iOS
        var shell = CreateShellPage();
        shell.WaitForPageReady();
        
        // Mobile-specific gesture
        var mauiContext = (AppiumTestContext)Context;
        mauiContext.Swipe(SwipeDirection.Down);
        
        // Assert refresh occurred
        shell.WaitForPageReady();
    }
    
    [Fact]
    [WebOnly]
    public void Web_Can_Navigate_Back()
    {
        // This test only runs on Web
        var shell = CreateShellPage();
        shell.WaitForPageReady();
        
        var settings = shell.NavigateToSettings();
        settings.WaitForPageReady();
        
        // Web-specific navigation
        var webContext = (SeleniumTestContext)Context;
        webContext.NavigateBack();
        
        shell.WaitForPageReady();
        shell.IsDisplayed().Should().BeTrue();
    }
    
    [Fact]
    [SkipOnPlatform(Platform.iOS, Reason = "iOS simulator not available in CI")]
    public void Settings_Can_Be_Changed()
    {
        var shell = CreateShellPage();
        shell.WaitForPageReady();
        
        var settings = shell.NavigateToSettings();
        settings.WaitForPageReady();
        
        // Platform-independent test logic
        settings.ThemeToggle.AssertVisible(true);
    }
    
    private IShellPage CreateShellPage()
    {
        return Context.Platform switch
        {
            Platform.Windows => new WpfShellPage(Context),
            Platform.WindowsMaui or Platform.Android or Platform.iOS => new MauiShellPage(Context),
            Platform.Web => new WebShellPage(Context),
            _ => throw new NotSupportedException($"Platform {Context.Platform} not supported")
        };
    }
}
```

---

## 5.7 Configuration Examples

### 5.7.1 Full Multi-Platform Configuration

```json
{
  "UITest": {
    "Platform": "Windows",
    "DefaultTimeoutMs": 10000,
    "ShortTimeoutMs": 3000,
    "PollingIntervalMs": 250,
    "LogFilePath": "logs/uitest_{platform}_{date}.csv",
    
    "Platforms": {
      "Windows": {
        "ApplicationPath": "bin\\Debug\\net9.0-windows\\Oravey.Tools.Wpf.exe"
      },
      "WindowsMaui": {
        "ApplicationPath": "bin\\Debug\\net9.0-windows10.0.19041.0\\Oravey.Tools.Maui.exe",
        "AppiumServerUrl": "http://127.0.0.1:4723"
      },
      "Android": {
        "ApplicationPath": "bin\\Release\\net9.0-android\\Oravey.Tools.Maui-Signed.apk",
        "AppiumServerUrl": "http://127.0.0.1:4723",
        "DeviceName": "emulator-5554"
      },
      "iOS": {
        "ApplicationPath": "bin\\Release\\net9.0-ios\\Oravey.Tools.Maui.app",
        "AppiumServerUrl": "http://127.0.0.1:4723",
        "DeviceName": "iPhone 15 Pro"
      },
      "Web": {
        "BaseUrl": "https://localhost:5001",
        "BrowserType": "Chrome",
        "Headless": false
      }
    }
  }
}
```

---

*Related: [ControlObject Hierarchy Code Examples](21d6_ControlObjectHierarchy_CodeExamples.md)*
