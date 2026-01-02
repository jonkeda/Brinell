# 5. Multi-Platform Support

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d5_MultiPlatformSupport_CodeExamples.md](21d5_MultiPlatformSupport_CodeExamples.md)  
**Previous:** [Platform Implementations](21d4_PlatformImplementations.md)  
**Version:** 3.0 (Updated December 2025)

---

## 5.1 Overview

Multi-platform support enables running the same test logic across different platforms with minimal code duplication.

**Key Architecture (v3):** Each platform uses its native driver directly - no adapter abstraction.

### 5.1.1 Supported Configurations

| Configuration | Platform Enum | Use Case |
|---------------|---------------|----------|
| Windows WPF | `Platform.Windows` | Desktop WPF apps |
| Windows MAUI | `Platform.WindowsMaui` | Cross-platform desktop |
| Android | `Platform.Android` | Mobile Android |
| iOS | `Platform.iOS` | Mobile iOS |
| Web Chrome | `Platform.Web` | Browser testing |
| Web Firefox | `Platform.Web` | Browser testing |
| Web Edge | `Platform.Web` | Browser testing |

---

## 5.2 Configuration-Based Platform Selection

### 5.2.1 Configuration File

```json
{
  "UITest": {
    "Platform": "Windows",
    "ApplicationPath": "Oravey.Tools.Wpf.exe",
    "Platforms": {
      "Windows": {
        "ApplicationPath": "bin\\Debug\\net9.0-windows\\Oravey.Tools.Wpf.exe"
      },
      "WindowsMaui": {
        "ApplicationPath": "bin\\Debug\\net9.0-windows\\Oravey.Tools.Maui.exe",
        "AppiumServerUrl": "http://127.0.0.1:4723"
      },
      "Android": {
        "ApplicationPath": "Oravey.Tools.Maui-Signed.apk",
        "AppiumServerUrl": "http://127.0.0.1:4723"
      },
      "iOS": {
        "ApplicationPath": "Oravey.Tools.Maui.app",
        "AppiumServerUrl": "http://127.0.0.1:4723"
      },
      "Web": {
        "BaseUrl": "https://localhost:5001",
        "BrowserType": "Chrome"
      }
    }
  }
}
```

### 5.2.2 Environment Variable Override

```bash
# Override platform via environment variable
$env:UITEST_PLATFORM = "Android"
dotnet test Oravey.Tools.UITests
```

---

## 5.3 Platform-Specific Test Attributes

### 5.3.1 Available Attributes

| Attribute | Description |
|-----------|-------------|
| `[Platform(Platform.Windows)]` | Run only on specified platform(s) |
| `[SkipOnPlatform(Platform.iOS)]` | Skip on specified platform(s) |
| `[MobileOnly]` | Run only on Android/iOS |
| `[DesktopOnly]` | Run only on Windows/WindowsMaui |
| `[WebOnly]` | Run only on Web platform |

### 5.3.2 Usage Examples

```csharp
[Platform(Platform.Windows)]
public void Test_Only_On_Windows() { }

[Platform(Platform.Android, Platform.iOS)]
public void Test_On_Mobile_Platforms() { }

[SkipOnPlatform(Platform.iOS)]
public void Test_Skip_On_iOS() { }

[MobileOnly]
public void Test_Mobile_Gesture() { }

[WebOnly]
public void Test_Browser_Navigation() { }
```

---

## 5.4 Shared Page Objects

### 5.4.1 Strategy

Page objects can be shared across platforms using:
1. **Interface-based**: Define interface, implement per platform
2. **Abstract base**: Common logic in base, platform specifics in derived
3. **Conditional logic**: Platform checks within single class

### 5.4.2 Recommended Approach: Abstract Base

```
IShellPage (interface)
    ↓
ShellPageBase (abstract - shared logic)
    ↓
├── WpfShellPage (WPF specifics)
├── MauiShellPage (MAUI specifics)
└── WebShellPage (Web specifics)
```

---

## 5.5 Platform Project Structure

Each platform project is self-contained with its own base class hierarchy:

```
Platform/
├── Infrastructure/
│   └── [Platform]TestContext.cs       # Implements ITestContext + element ops
├── Controls/
│   ├── Base/                          # Platform-specific base classes
│   │   ├── ControlBase.cs             # Implements IControlObject
│   │   ├── PageBase.cs                # Implements IPageObject
│   │   ├── ContentControlBase.cs      # Clickable controls
│   │   ├── TextControlBase.cs         # Text input controls
│   │   ├── ToggleControlBase.cs       # Toggle controls
│   │   ├── SelectorControlBase.cs     # Selection controls
│   │   ├── RangeControlBase.cs        # Range controls
│   │   └── ItemsControlBase.cs        # Collection controls
│   └── [Platform-specific controls]
└── Testing/
    └── [Platform]UITestBase.cs
```

**Note:** There is no shared base class hierarchy in Core. Each platform implements its own using native driver access.

---

## 5.6 Platform-Specific Controls

### 5.5.1 Control Mapping

| Logical Control | WPF | MAUI | HTML |
|-----------------|-----|------|------|
| Button | `Button` | `Button` | `<button>` |
| Text Input | `TextBox` | `Entry` | `<input type="text">` |
| Label | `TextBlock` | `Label` | `<span>`, `<label>` |
| Checkbox | `CheckBox` | `CheckBox` | `<input type="checkbox">` |
| List | `ListBox` | `CollectionView` | `<ul>`, `<select>` |
| Switch | N/A | `Switch` | `<input type="checkbox">` |

### 5.5.2 AutomationId Mapping

| Platform | Attribute |
|----------|-----------|
| WPF | `AutomationProperties.AutomationId` |
| MAUI | `AutomationId` |
| HTML | `data-automation-id` or `id` |

---

## 5.6 Test Base Class Factory Pattern

### 5.6.1 Factory Method

```csharp
public abstract class MultiPlatformTestBase
{
    protected ITestContext Context { get; private set; }
    
    protected void InitializeContext()
    {
        var config = TestConfiguration.Load();
        Context = TestContextFactory.Create(config.Platform, config);
    }
}
```

### 5.6.2 Platform-Specific Initialization

The factory creates the appropriate context based on platform:
- `Platform.Windows` → `FlaUITestContext`
- `Platform.WindowsMaui` → `AppiumTestContext.CreateWindows()`
- `Platform.Android` → `AppiumTestContext.CreateAndroid()`
- `Platform.iOS` → `AppiumTestContext.CreateiOS()`
- `Platform.Web` → `SeleniumTestContext`

---

## 5.7 Cross-Platform Test Writing Guidelines

### 5.7.1 DO

- ✅ Use AutomationId consistently across platforms
- ✅ Use abstract page objects for shared logic
- ✅ Use platform attributes to skip incompatible tests
- ✅ Use `Platform.IsMobile()` for conditional gestures
- ✅ Use configuration for platform-specific values

### 5.7.2 DON'T

- ❌ Hardcode platform-specific selectors
- ❌ Use `typeof(Context)` checks
- ❌ Duplicate test logic per platform
- ❌ Ignore platform limitations

---

## 5.8 CI/CD Matrix Testing

### 5.8.1 GitHub Actions Example

```yaml
strategy:
  matrix:
    platform: [Windows, Android, Web]
    include:
      - platform: Windows
        os: windows-latest
      - platform: Android
        os: ubuntu-latest
      - platform: Web
        os: ubuntu-latest

steps:
  - run: dotnet test --filter "Category=UITest"
    env:
      UITEST_PLATFORM: ${{ matrix.platform }}
```

### 5.8.2 Azure DevOps Example

```yaml
strategy:
  matrix:
    Windows:
      platform: Windows
      vmImage: windows-latest
    Android:
      platform: Android
      vmImage: ubuntu-latest
    Web:
      platform: Web
      vmImage: ubuntu-latest

steps:
  - script: dotnet test --filter "Category=UITest"
    env:
      UITEST_PLATFORM: $(platform)
```

---

*Next: [ControlObject Hierarchy](21d6_ControlObjectHierarchy.md)*
