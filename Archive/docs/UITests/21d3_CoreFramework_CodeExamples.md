# 3. Core Framework - Code Examples

**Parent:** [Core Framework](21d3_CoreFramework.md)  
**Version:** 3.0 (Updated December 2025)

**Note (v3):** Core contains **interfaces only**. Base classes and adapters have been removed. Each platform project provides its own implementations.

---

## 3.1 Platform Enum and Extensions

```csharp
namespace Oravey.UITestFramework.Core.Abstractions;

/// <summary>
/// Target platform for UI tests.
/// Replaces string-based identification and IsWindows/IsMobile properties.
/// </summary>
public enum Platform
{
    /// <summary>Windows desktop WPF application (FlaUI)</summary>
    Windows,
    
    /// <summary>Windows MAUI application (Appium)</summary>
    WindowsMaui,
    
    /// <summary>Android mobile application (Appium)</summary>
    Android,
    
    /// <summary>iOS mobile application (Appium)</summary>
    iOS,
    
    /// <summary>Web browser HTML application (Selenium)</summary>
    Web
}

/// <summary>
/// Extension methods for Platform enum.
/// These replace the IsWindows and IsMobile boolean properties.
/// </summary>
public static class PlatformExtensions
{
    /// <summary>
    /// Returns true for Android or iOS platforms.
    /// Replaces: IsMobile property
    /// </summary>
    public static bool IsMobile(this Platform platform) =>
        platform is Platform.Android or Platform.iOS;
    
    /// <summary>
    /// Returns true for Windows or WindowsMaui platforms.
    /// Replaces: IsWindows property (partially)
    /// </summary>
    public static bool IsDesktop(this Platform platform) =>
        platform is Platform.Windows or Platform.WindowsMaui;
    
    /// <summary>
    /// Returns true for Web platform.
    /// </summary>
    public static bool IsWeb(this Platform platform) =>
        platform == Platform.Web;
    
    /// <summary>
    /// Returns true for platforms that support touch gestures.
    /// </summary>
    public static bool SupportsGestures(this Platform platform) =>
        platform.IsMobile();
    
    /// <summary>
    /// Returns true for Windows platform (WPF via FlaUI).
    /// </summary>
    public static bool IsWindowsDesktop(this Platform platform) =>
        platform == Platform.Windows;
    
    /// <summary>
    /// Returns true for MAUI-based platforms (uses Appium).
    /// </summary>
    public static bool IsMaui(this Platform platform) =>
        platform is Platform.WindowsMaui or Platform.Android or Platform.iOS;
    
    /// <summary>
    /// Gets the automation library name for this platform.
    /// </summary>
    public static string GetAutomationLibrary(this Platform platform) =>
        platform switch
        {
            Platform.Windows => "FlaUI",
            Platform.WindowsMaui => "Appium",
            Platform.Android => "Appium",
            Platform.iOS => "Appium",
            Platform.Web => "Selenium",
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };
    
    /// <summary>
    /// Gets a human-readable description of the platform.
    /// </summary>
    public static string GetDescription(this Platform platform) =>
        platform switch
        {
            Platform.Windows => "Windows Desktop (WPF)",
            Platform.WindowsMaui => "Windows Desktop (MAUI)",
            Platform.Android => "Android Mobile",
            Platform.iOS => "iOS Mobile",
            Platform.Web => "Web Browser",
            _ => platform.ToString()
        };
}
```

---

## 3.2 ITestContext Interface (Simplified in v3)

```csharp
namespace Oravey.UITestFramework.Core.Abstractions;

/// <summary>
/// Simplified context interface for UI tests.
/// Platform-specific contexts (FlaUITestContext, etc.) extend this
/// with element operations.
/// </summary>
public interface ITestContext : IDisposable
{
    /// <summary>Current test name for logging.</summary>
    string TestName { get; set; }
    
    /// <summary>
    /// Target platform enum value.
    /// Use extension methods for platform queries:
    /// - Platform.IsMobile()
    /// - Platform.IsDesktop()
    /// - Platform.IsWeb()
    /// </summary>
    Platform Platform { get; }
    
    /// <summary>CSV format logger for structured logging.</summary>
    ITestLogger? Logger { get; }
    
    /// <summary>Default timeout for wait operations (10000ms).</summary>
    int DefaultTimeoutMs { get; }
    
    /// <summary>Short timeout for quick checks (3000ms).</summary>
    int ShortTimeoutMs { get; }
    
    /// <summary>Polling interval for wait loops (250ms).</summary>
    int PollingIntervalMs { get; }
    
    /// <summary>
    /// Generic polling wait until condition is met or timeout.
    /// </summary>
    bool WaitFor(Func<bool> condition, TimeSpan? timeout = null, string? description = null);
    
    /// <summary>
    /// Log a message through the structured logger.
    /// </summary>
    void Log(string message);
    
    /// <summary>
    /// Capture screenshot to file.
    /// </summary>
    string? TakeScreenshot(string name);
}
```

---

## 3.3 Control Interfaces (New in v3)

### 3.3.1 IControlObject (Base Interface)

```csharp
namespace Oravey.UITestFramework.Core.Abstractions;

/// <summary>
/// Base interface for all control objects.
/// Platform implementations provide concrete base classes.
/// </summary>
public interface IControlObject
{
    /// <summary>Identifier for locating control.</summary>
    string AutomationId { get; }
    
    /// <summary>Test context reference.</summary>
    ITestContext? Context { get; }
    
    /// <summary>Parent page object.</summary>
    IPageObject? Page { get; }
    
    // State checks
    bool IsExists();
    bool IsVisible();
    bool IsEnabled();
    bool IsClickable();
    string GetText();
    
    // Wait methods
    bool WaitExists(bool expected = true, TimeSpan? timeout = null);
    bool WaitVisible(bool expected = true, TimeSpan? timeout = null);
    bool WaitEnabled(bool expected = true, TimeSpan? timeout = null);
    bool WaitClickable(TimeSpan? timeout = null);
    
    // Check methods (throw on failure)
    void CheckExists(bool expected = true, TimeSpan? timeout = null);
    void CheckVisible(bool expected = true, TimeSpan? timeout = null);
    void CheckEnabled(bool expected = true, TimeSpan? timeout = null);
    void CheckClickable(TimeSpan? timeout = null);
    
    // Assert methods (with logging)
    void AssertExists(bool expected = true, TimeSpan? timeout = null, string? message = null);
    void AssertVisible(bool expected = true, TimeSpan? timeout = null, string? message = null);
    void AssertEnabled(bool expected = true, TimeSpan? timeout = null, string? message = null);
    void AssertClickable(TimeSpan? timeout = null, string? message = null);
    void AssertTextEquals(string expected, TimeSpan? timeout = null, string? message = null);
}
```

### 3.3.2 ITextControl

```csharp
/// <summary>
/// Interface for text input controls.
/// </summary>
public interface ITextControl : IControlObject
{
    void Enter(string text);
    void Clear();
    void ClearAndEnter(string text);
    void SetText(string text);  // Alias for ClearAndEnter
    void Append(string text);
}
```

### 3.3.3 IToggleControl

```csharp
/// <summary>
/// Interface for toggle/checkbox controls.
/// </summary>
public interface IToggleControl : IControlObject
{
    bool IsChecked();
    void Toggle();
    void Check();
    void Uncheck();
    void SetChecked(bool value);
    void AssertChecked(string? message = null);
    void AssertUnchecked(string? message = null);
}
```

### 3.3.4 IContentControl

```csharp
/// <summary>
/// Interface for clickable content controls.
/// </summary>
public interface IContentControl : IControlObject
{
    void Click();
    void DoubleClick();
}
```

### 3.3.5 ISelectorControl

```csharp
/// <summary>
/// Interface for selection controls (dropdowns, pickers).
/// </summary>
public interface ISelectorControl : IControlObject
{
    string GetSelectedText();
    int GetSelectedIndex();
    void SelectByIndex(int index);
    void SelectByText(string text);
    void AssertSelectedText(string expected, TimeSpan? timeout = null, string? message = null);
}
```

### 3.3.6 IRangeControl

```csharp
/// <summary>
/// Interface for range/slider controls.
/// </summary>
public interface IRangeControl : IControlObject
{
    double GetValue();
    double GetMinimum();
    double GetMaximum();
    void SetValue(double value);
    void AssertValue(double expected, double tolerance = 0.01, string? message = null);
}
```

### 3.3.7 IItemsControl

```csharp
/// <summary>
/// Interface for list/collection controls.
/// </summary>
public interface IItemsControl : IControlObject
{
    IReadOnlyList<string> GetItems();
    int GetItemCount();
    void AssertItemCount(int expected, TimeSpan? timeout = null, string? message = null);
}
```

---

## 3.5 ITestLogger Interface

```csharp
namespace Oravey.UITestFramework.Core.Logging;

/// <summary>
/// Structured logging interface for UI tests.
/// All log entries are formatted as CSV for analysis.
/// Format: Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
/// </summary>
public interface ITestLogger : IDisposable
{
    /// <summary>
    /// Log a control action (click, type, clear, etc.).
    /// </summary>
    void LogAction(
        string testName,
        string? pageName,
        string? controlId,
        string action,
        string? value,
        string? expectedValue,
        string result,
        string? message);
    
    /// <summary>
    /// Log an assertion result.
    /// </summary>
    void LogAssertion(
        string testName,
        string? pageName,
        string? controlId,
        string assertionType,
        string? actualValue,
        string? expectedValue,
        bool passed,
        string? message);
    
    /// <summary>
    /// Log page navigation.
    /// </summary>
    void LogNavigation(
        string testName,
        string? fromPage,
        string toPage,
        string? message);
    
    /// <summary>
    /// Log an error.
    /// </summary>
    void LogError(
        string testName,
        string? pageName,
        string? controlId,
        string errorType,
        string message,
        Exception? exception);
    
    /// <summary>
    /// Log informational message.
    /// </summary>
    void LogInfo(
        string testName,
        string? pageName,
        string message);
    
    /// <summary>
    /// Flush pending log entries to file.
    /// </summary>
    void Flush();
}
```

---

## 3.6 TestConfiguration Class

```csharp
namespace Oravey.UITestFramework.Core.Configuration;

/// <summary>
/// Configuration for UI tests loaded from appsettings.uitest.json.
/// </summary>
public class TestConfiguration
{
    /// <summary>Target platform.</summary>
    public Platform Platform { get; set; } = Platform.Windows;
    
    /// <summary>Path to application executable.</summary>
    public string ApplicationPath { get; set; } = string.Empty;
    
    /// <summary>Base URL for web tests.</summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>Browser type for Selenium (Chrome, Firefox, Edge).</summary>
    public string? BrowserType { get; set; } = "Chrome";
    
    /// <summary>Appium server URL.</summary>
    public string? AppiumServerUrl { get; set; } = "http://127.0.0.1:4723";
    
    /// <summary>Cloud testing provider configuration.</summary>
    public CloudProviderConfig? CloudProvider { get; set; }
    
    /// <summary>Default timeout in milliseconds.</summary>
    public int DefaultTimeoutMs { get; set; } = 10000;
    
    /// <summary>Short timeout in milliseconds.</summary>
    public int ShortTimeoutMs { get; set; } = 3000;
    
    /// <summary>Polling interval in milliseconds.</summary>
    public int PollingIntervalMs { get; set; } = 250;
    
    /// <summary>Path for CSV log file (supports {date} placeholder).</summary>
    public string? LogFilePath { get; set; }
    
    /// <summary>Whether to take screenshots on failure.</summary>
    public bool ScreenshotOnFailure { get; set; } = true;
    
    /// <summary>Directory for screenshots.</summary>
    public string ScreenshotDirectory { get; set; } = "screenshots";
    
    /// <summary>
    /// Load configuration from file.
    /// </summary>
    public static TestConfiguration Load(string path = "appsettings.uitest.json")
    {
        if (!File.Exists(path))
            return new TestConfiguration();
        
        var json = File.ReadAllText(path);
        var root = JsonSerializer.Deserialize<JsonElement>(json);
        
        if (root.TryGetProperty("UITest", out var uitest))
        {
            return JsonSerializer.Deserialize<TestConfiguration>(uitest.GetRawText())
                ?? new TestConfiguration();
        }
        
        return new TestConfiguration();
    }
}
```

---

## 3.7 CloudProviderConfig Class

```csharp
namespace Oravey.UITestFramework.Core.Configuration;

/// <summary>
/// Cloud testing provider (BrowserStack, SauceLabs) configuration.
/// </summary>
public class CloudProviderConfig
{
    /// <summary>Cloud provider type.</summary>
    public CloudProvider Provider { get; set; }
    
    /// <summary>Provider username.</summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>Provider access key (from environment or config).</summary>
    public string AccessKey { get; set; } = string.Empty;
    
    /// <summary>Remote driver hub URL.</summary>
    public string HubUrl { get; set; } = string.Empty;
    
    /// <summary>Additional capabilities for the driver.</summary>
    public Dictionary<string, object> Capabilities { get; set; } = new();
    
    /// <summary>Project name for grouping in provider dashboard.</summary>
    public string? ProjectName { get; set; }
    
    /// <summary>Build name for this test run.</summary>
    public string? BuildName { get; set; }
}

/// <summary>
/// Supported cloud testing providers.
/// </summary>
public enum CloudProvider
{
    None,
    BrowserStack,
    SauceLabs,
    LambdaTest
}
```

---

## 3.8 Configuration File Example

```json
{
  "UITest": {
    "Platform": "Windows",
    "ApplicationPath": "bin\\Debug\\net9.0-windows\\Oravey.Tools.Wpf.exe",
    "DefaultTimeoutMs": 10000,
    "ShortTimeoutMs": 3000,
    "PollingIntervalMs": 250,
    "LogFilePath": "logs/uitest_{date}.csv",
    "ScreenshotOnFailure": true,
    "ScreenshotDirectory": "screenshots",
    "CloudProvider": null
  }
}
```

---

## 3.9 Web Test Configuration Example

```json
{
  "UITest": {
    "Platform": "Web",
    "BaseUrl": "https://localhost:5001",
    "BrowserType": "Chrome",
    "DefaultTimeoutMs": 15000,
    "ShortTimeoutMs": 5000,
    "PollingIntervalMs": 500,
    "LogFilePath": "logs/webtest_{date}.csv"
  }
}
```

---

## 3.10 Cloud Provider Configuration Example

```json
{
  "UITest": {
    "Platform": "Web",
    "BaseUrl": "https://myapp.example.com",
    "BrowserType": "Chrome",
    "CloudProvider": {
      "Provider": "BrowserStack",
      "Username": "${BROWSERSTACK_USERNAME}",
      "AccessKey": "${BROWSERSTACK_ACCESS_KEY}",
      "HubUrl": "https://hub-cloud.browserstack.com/wd/hub",
      "ProjectName": "Oravey",
      "BuildName": "CI-${BUILD_NUMBER}",
      "Capabilities": {
        "os": "Windows",
        "osVersion": "11",
        "browserVersion": "latest",
        "resolution": "1920x1080"
      }
    }
  }
}
```

---

*Related: [Platform Implementations Code Examples](21d4_PlatformImplementations_CodeExamples.md)*
