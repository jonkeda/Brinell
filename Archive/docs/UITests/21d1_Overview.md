# 1. Overview

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d1_Overview_CodeExamples.md](21d1_Overview_CodeExamples.md)  
**Version:** 3.0 (Updated December 2025)

---

## 1.1 Framework Stack

| Component | Version | Purpose |
|-----------|---------|---------|
| **FlaUI** | 4.0.0 | WPF desktop automation via UI Automation |
| **Appium.WebDriver** | 8.0.0 | MAUI/Mobile automation (W3C compliant) |
| **Selenium.WebDriver** | 4.27.0 | HTML/Web browser automation |
| **WireMock.Net** | 1.6.x | HTTP API mocking for isolated testing |
| **xUnit** | 2.9.x | Test framework with traits and collections |
| **FluentAssertions** | 6.x | Readable assertion library |

---

## 1.2 Four-Layer Architecture

| Layer | Project | Purpose |
|-------|---------|---------|
| **Core** | `Oravey.UITestFramework.Core` | **Interfaces only** - ITestContext, IPageObject, control interfaces |
| **Platform** | `.Wpf` / `.Maui` / `.Html` | Self-contained: context, base classes, controls, native drivers |
| **Mocking** | `.Mocking` | WireMock integration for API mocking |
| **Application** | `*.UITests` | Application-specific tests and page objects |

**Key Architecture (v3):** Core defines contracts only. Each platform is fully self-contained with its own base class hierarchy using native driver access (FlaUI, Appium, Selenium directly - no adapter layer).

---

## 1.3 Key Design Principles

### 1.3.1 Control Object Pattern
Encapsulate control interactions with built-in waits and state verification.

### 1.3.2 Page Object Pattern
Encapsulate page structure and workflows. **Navigation methods return void** - tests create page objects explicitly.

### 1.3.3 Wait/Check/Is/Assert Pattern
Four-tier method pattern for consistent timeout handling and assertions:
- **Is*** - Immediate state check
- **Wait*** - Poll until condition or timeout
- **Check*** - Wait + throw on failure
- **Assert*** - Semantic assertion with logging

### 1.3.4 Always Check Before Action
**CRITICAL:** Every action method must verify preconditions:
- `Click()` calls `CheckClickable()` first
- `EnterText()` calls `CheckEnabled()` first
- `Toggle()` calls `CheckClickable()` first

### 1.3.5 Platform Abstraction
Single enum-based platform identification via `Platform` enum:
- Replaces string-based `Platform` property
- Removes `IsWindows` and `IsMobile` boolean properties
- Extension methods provide platform queries

### 1.3.6 IsBusy State Tracking
Page readiness detection via busy indicators:
- Pages expose `IsBusy` property
- `WaitForPageReady()` waits for not-busy state
- Automatic busy detection after navigation

### 1.3.7 Standardized CSV Logging
Structured logging for analysis and debugging:
```
Test;Page;ControlId;Action;Value;ExpectedValue;Result;Message
```

### 1.3.8 Virtual Methods for Reuse
All base class methods are `virtual` for customization in derived classes.

---

## 1.4 Supported Platforms

| Platform Enum | UI Framework | Automation Library | Target |
|---------------|--------------|-------------------|--------|
| `Platform.Windows` | WPF | FlaUI (UIA3) | Windows Desktop |
| `Platform.WindowsMaui` | MAUI | Appium | Windows Desktop |
| `Platform.Android` | MAUI | Appium | Android Mobile |
| `Platform.iOS` | MAUI | Appium | iOS Mobile |
| `Platform.Web` | HTML | Selenium | Web Browsers |

---

## 1.5 Testing Pyramid Reminder

```
           /\
          /  \        UI Tests (< 5%)
         /    \       - Smoke tests only
        /──────\      - Navigation works
       /        \     - Controls visible
      /──────────\    
     / Integration \   Integration Tests (10-20%)
    /   Tests       \  - API contracts
   /────────────────\  - Database operations
  /                  \ 
 /    Unit Tests      \  Unit Tests (75-85%)
/______________________\ - Business logic
                         - ViewModels
                         - Calculations
```

### What to Test in UI Tests
- ✅ Application launches successfully
- ✅ Navigation between views works
- ✅ Critical controls are visible and enabled
- ✅ Basic user flows complete (happy path)

### What NOT to Test in UI Tests
- ❌ Business logic (use unit tests)
- ❌ Form validation (use ViewModel unit tests)
- ❌ Data persistence (use integration tests)
- ❌ Edge cases and error handling (use unit tests)
- ❌ Complex multi-step workflows (use integration tests)

---

## 1.6 Project Dependencies

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Application UITests                                                      │
│   Oravey.Tools.Wpf.UITests                                              │
│   Oravey.Tools.Maui.UITests                                             │
│   Oravey.Web.UITests                                                    │
└─────────────────────────────────────────────────────────────────────────┘
         │ references
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ Platform Implementations                                                 │
│   Oravey.UITestFramework.Wpf     → FlaUI                               │
│   Oravey.UITestFramework.Maui    → Appium.WebDriver                    │
│   Oravey.UITestFramework.Html    → Selenium.WebDriver                  │
└─────────────────────────────────────────────────────────────────────────┘
         │ references
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ Core + Mocking                                                          │
│   Oravey.UITestFramework.Core    (abstractions, base classes)          │
│   Oravey.UITestFramework.Mocking → WireMock.Net                        │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 1.7 Key Files Quick Reference

### Core Project (Interfaces Only)
| File | Purpose |
|------|----------|
| `Platform.cs` | Platform enum with extension methods |
| `ITestContext.cs` | Simplified test context interface |
| `IPageObject.cs` | Page object interface |
| `IControlObject.cs` | Base control interface |
| `ITextControl.cs`, `IToggleControl.cs`, etc. | Control capability interfaces |
| `TestLogger.cs` | CSV format logging |

### Platform Projects (Self-Contained)
| File | Purpose |
|------|----------|
| `[Platform]TestContext.cs` | Implements ITestContext + element operations |
| `ControlBase.cs` | Platform-specific base for controls |
| `PageBase.cs` | Platform-specific base for pages |
| `ContentControlBase.cs`, `TextControlBase.cs`, etc. | Capability base classes |

---

*Next: [Architecture](21d2_Architecture.md)*
