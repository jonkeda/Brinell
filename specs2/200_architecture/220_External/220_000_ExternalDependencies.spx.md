# 220.000 External Dependencies

**Block Type:** EXT (External Dependency)  
**Edition:** 🟢Ⅰ Lite  
**ID:** 220.000  
**Title:** External Dependencies Overview  
**Status:** Draft  
**Version:** 1.0

---

## Overview

Brinell uses platform-specific automation drivers for UI interaction. Each platform package has a single primary dependency.

| Platform | Package | Driver | NuGet Dependency |
|----------|---------|--------|------------------|
| MAUI | `Brinell.Maui` | Appium | `Appium.WebDriver` 5.x |
| Blazor | `Brinell.Blazor` | Selenium | `Selenium.WebDriver` 4.x |
| Blazor (alt) | `Brinell.Blazor.Playwright` | Playwright | `Microsoft.Playwright` 1.x |
| WPF | `Brinell.Wpf` | FlaUI | `FlaUI.Core`, `FlaUI.UIA3` 4.x |

---

## 1. Appium (MAUI)

**Purpose:** Cross-platform mobile automation for Android, iOS, and Windows.

### Key Types

| Type | Purpose |
|------|---------|
| `AppiumDriver` | Base driver for all platforms |
| `AppiumElement` | UI element wrapper |
| `MobileBy` | Locator strategies |

### Locator Mapping

| Brinell | Appium |
|---------|--------|
| `AutomationId` | `MobileBy.AccessibilityId()` |
| `XPath` | `MobileBy.XPath()` |
| `ClassName` | `MobileBy.ClassName()` |

### Element Properties

| Brinell | Appium |
|---------|--------|
| `IsVisible()` | `element.Displayed` |
| `IsEnabled()` | `element.Enabled` |
| `GetText()` | `element.Text` |

### Server Requirements

```bash
npm install -g appium
appium driver install uiautomator2  # Android
appium driver install xcuitest      # iOS
appium driver install windows       # Windows
appium --address 127.0.0.1 --port 4723
```

---

## 2. Selenium WebDriver (Blazor)

**Purpose:** Cross-browser automation for Chrome, Firefox, Edge, Safari.

### Key Types

| Type | Purpose |
|------|---------|
| `IWebDriver` | Base driver interface |
| `IWebElement` | UI element interface |
| `By` | Locator strategies |

### Locator Mapping

| Brinell | Selenium |
|---------|----------|
| `AutomationId` | `By.CssSelector("[data-automation-id='...']")` |
| `Id` | `By.Id()` |
| `XPath` | `By.XPath()` |
| `CssSelector` | `By.CssSelector()` |

### Element Properties

| Brinell | Selenium |
|---------|----------|
| `IsVisible()` | `element.Displayed` |
| `IsEnabled()` | `element.Enabled` |
| `GetText()` | `element.Text` |

### Driver Setup

```csharp
// Recommended: Use WebDriverManager for automatic driver management
new WebDriverManager.DriverManager().SetUpDriver(new ChromeConfig());
var driver = new ChromeDriver();
```

### Blazor Considerations

- **Blazor Server:** May need longer waits for SignalR updates
- **Blazor WebAssembly:** Initial load time for .NET runtime
- **Test IDs:** Use `data-automation-id` attribute

---

## 3. Playwright (Blazor Alternative)

**Status:** 🔮 Planned for future release

**Purpose:** Modern browser automation with auto-waiting and tracing.

### Key Differences from Selenium

| Feature | Selenium | Playwright |
|---------|----------|------------|
| Waiting | Manual | Auto-wait |
| Browser install | External driver | Automatic |
| Network mocking | Limited | Full |
| Tracing | Manual | Built-in |

### Key Types

| Type | Purpose |
|------|---------|
| `IPage` | Page instance |
| `ILocator` | Auto-waiting locator |
| `IBrowser` | Browser instance |

### Locator Mapping

| Brinell | Playwright |
|---------|------------|
| `AutomationId` | `page.GetByTestId()` |
| `Text` | `page.GetByText()` |
| `Role` | `page.GetByRole()` |

---

## 4. FlaUI (WPF)

**Purpose:** Windows desktop automation wrapping Microsoft UI Automation.

### Key Types

| Type | Purpose |
|------|---------|
| `Application` | Application instance |
| `UIA3Automation` | UI Automation provider |
| `AutomationElement` | UI element wrapper |
| `ConditionFactory` | Locator builder |

### Locator Mapping

| Brinell | FlaUI |
|---------|-------|
| `AutomationId` | `cf.ByAutomationId()` |
| `Name` | `cf.ByName()` |
| `ClassName` | `cf.ByClassName()` |
| `ControlType` | `cf.ByControlType()` |

### Element Properties

| Brinell | FlaUI |
|---------|-------|
| `IsVisible()` | `element.IsOffscreen == false` |
| `IsEnabled()` | `element.IsEnabled` |
| `GetText()` | `element.Name` or pattern |

### Application Launch

```csharp
var app = Application.Launch(@"C:\path\to\app.exe");
using var automation = new UIA3Automation();
var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
```

### WPF Setup

```xml
<!-- XAML: Set AutomationId -->
<Button AutomationProperties.AutomationId="SubmitButton" Content="Submit"/>
```

---

## Version Compatibility

| Brinell | Appium.WebDriver | Selenium.WebDriver | Playwright | FlaUI |
|---------|------------------|-------------------|------------|-------|
| 1.0.x | 5.x | 4.x | 1.x | 4.x |

---

## Related Documents

- [IMauiTestContext](../../250_specifications/250_000_Foundation/250_009_PlatformContexts.spx.md) — MAUI context interface
- [IBlazorTestContext](../../250_specifications/250_000_Foundation/250_009_PlatformContexts.spx.md) — Blazor context interface
- [IWpfTestContext](../../250_specifications/250_000_Foundation/250_009_PlatformContexts.spx.md) — WPF context interface

---

## Detailed Documentation

For comprehensive setup guides and troubleshooting, see:

- [220_001_Appium.spx.md](220_001_Appium.spx.md) — Full Appium documentation
- [220_002_Selenium.spx.md](220_002_Selenium.spx.md) — Full Selenium documentation
- [220_003_Playwright.spx.md](220_003_Playwright.spx.md) — Full Playwright documentation
- [220_004_FlaUI.spx.md](220_004_FlaUI.spx.md) — Full FlaUI documentation
