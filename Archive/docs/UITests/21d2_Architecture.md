# 2. Architecture

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d2_Architecture_CodeExamples.md](21d2_Architecture_CodeExamples.md)  
**Previous:** [Overview](21d1_Overview.md)  
**Version:** 3.0 (Updated December 2025)

---

## 2.1 Component Relationships

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Application-Specific UITests                         │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐  │
│  │ Wpf.UITests      │  │ Maui.UITests     │  │ Web.UITests          │  │
│  │  - PageObjects   │  │  - PageObjects   │  │  - PageObjects       │  │
│  │  - Tests         │  │  - Tests         │  │  - Tests             │  │
│  └────────┬─────────┘  └────────┬─────────┘  └──────────┬───────────┘  │
└───────────┼─────────────────────┼───────────────────────┼──────────────┘
            │                     │                       │
            ▼                     ▼                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                  Platform-Specific Framework Projects                   │
│        (Each is self-contained with base classes + native driver)       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐  │
│  │ .Wpf             │  │ .Maui            │  │ .Html                │  │
│  │ ┌──────────────┐ │  │ ┌──────────────┐ │  │ ┌────────────────┐   │  │
│  │ │FlaUITestCtx  │ │  │ │AppiumTestCtx │ │  │ │SeleniumTestCtx │   │  │
│  │ └──────────────┘ │  │ └──────────────┘ │  │ └────────────────┘   │  │
│  │ ┌──────────────┐ │  │ ┌──────────────┐ │  │ ┌────────────────┐   │  │
│  │ │ControlBase   │ │  │ │ControlBase   │ │  │ │ControlBase     │   │  │
│  │ │PageBase      │ │  │ │PageBase      │ │  │ │PageBase        │   │  │
│  │ │TextCtrlBase  │ │  │ │TextCtrlBase  │ │  │ │TextCtrlBase    │   │  │
│  │ │... etc       │ │  │ │... etc       │ │  │ │... etc         │   │  │
│  │ └──────────────┘ │  │ └──────────────┘ │  │ └────────────────┘   │  │
│  │ Uses: FlaUI     │  │ Uses: Appium     │  │ Uses: Selenium       │  │
│  │ directly        │  │ directly         │  │ directly             │  │
│  └────────┬─────────┘  └────────┬─────────┘  └──────────┬───────────┘  │
└───────────┼─────────────────────┼───────────────────────┼──────────────┘
            │                     │                       │
            └─────────────────────┼───────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                  Oravey.UITestFramework.Core (Interfaces Only)          │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  ITestContext (simplified)      │  IPageObject                    │ │
│  │   - TestName, Platform          │   - Name, IsDisplayed()         │ │
│  │   - Logger, WaitFor()           │   - WaitForDisplayed()          │ │
│  │   - DefaultTimeoutMs            │   - WaitForHidden()             │ │
│  ├───────────────────────────────────────────────────────────────────┤ │
│  │  Control Interfaces:                                              │ │
│  │   IControlObject, ITextControl, IToggleControl, ISelectorControl  │ │
│  │   IRangeControl, IItemsControl, IContentControl                   │ │
│  ├───────────────────────────────────────────────────────────────────┤ │
│  │  Utilities: Exceptions, Logging (ITestLogger, CsvTestLogger),     │ │
│  │  Attributes, Testing, Configuration                               │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    Platform Libraries (External)                        │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────────┐   │
│  │ FlaUI      │  │ Appium     │  │ Selenium   │  │ WireMock.Net   │   │
│  │ (WPF)      │  │ (Mobile)   │  │ (Web)      │  │ (Mocking)      │   │
│  └────────────┘  └────────────┘  └────────────┘  └────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 2.2 Project Structure

```
Oravey/Sources/UITestFramework/
├── Oravey.UITestFramework.Core/           # INTERFACES ONLY
│   ├── Abstractions/
│   │   ├── Platform.cs                    # Platform enum + extensions
│   │   ├── ITestContext.cs                # Simplified context interface
│   │   ├── IPageObject.cs                 # Page object interface
│   │   └── Controls/
│   │       ├── IControlObject.cs          # Base control interface
│   │       ├── ITextControl.cs            # Text input interface
│   │       ├── IToggleControl.cs          # Toggle control interface
│   │       ├── ISelectorControl.cs        # Selection control interface
│   │       ├── IRangeControl.cs           # Range/slider interface
│   │       ├── IItemsControl.cs           # Items control interface
│   │       └── IContentControl.cs         # Clickable content interface
│   ├── Exceptions/                        # Framework exceptions
│   ├── Logging/
│   │   ├── ITestLogger.cs                 # Logger interface
│   │   └── CsvTestLogger.cs               # CSV format logger
│   ├── Attributes/                        # Test attributes
│   └── Testing/                           # Test utilities
│
├── Oravey.UITestFramework.Wpf/            # SELF-CONTAINED WPF
│   ├── Infrastructure/
│   │   ├── FlaUITestContext.cs            # Implements ITestContext + element ops
│   │   └── FlaUIDriverAdapter.cs          # Application lifecycle
│   └── Controls/
│       ├── Base/                          # WPF-specific base classes
│       │   ├── ControlBase.cs             # Implements IControlObject
│       │   ├── PageBase.cs                # Implements IPageObject
│       │   ├── BusyPageBase.cs            # Pages with loading states
│       │   ├── ContentControlBase.cs      # Clickable controls
│       │   ├── TextControlBase.cs         # Text input controls
│       │   ├── ToggleControlBase.cs       # Toggle controls
│       │   ├── SelectorControlBase.cs     # Selection controls
│       │   ├── RangeControlBase.cs        # Range controls
│       │   └── ItemsControlBase.cs        # Collection controls
│       ├── ButtonControl.cs
│       ├── TextBoxControl.cs
│       ├── LabelControl.cs
│       ├── ListBoxControl.cs
│       ├── ComboBoxControl.cs
│       ├── CheckBoxControl.cs
│       └── SliderControl.cs
│
├── Oravey.UITestFramework.Maui/           # SELF-CONTAINED MAUI
│   ├── Infrastructure/
│   │   └── AppiumTestContext.cs           # Implements ITestContext + element ops
│   └── Controls/
│       ├── Base/                          # MAUI-specific base classes
│       │   ├── ControlBase.cs
│       │   ├── PageBase.cs
│       │   ├── ContentControlBase.cs
│       │   ├── TextControlBase.cs
│       │   ├── ToggleControlBase.cs
│       │   ├── SelectorControlBase.cs
│       │   ├── RangeControlBase.cs
│       │   └── ItemsControlBase.cs
│       ├── ButtonControl.cs
│       ├── EntryControl.cs
│       ├── LabelControl.cs
│       ├── SwitchControl.cs
│       └── PickerControl.cs
│
├── Oravey.UITestFramework.Html/           # SELF-CONTAINED HTML
│   ├── Infrastructure/
│   │   └── SeleniumTestContext.cs         # Implements ITestContext + element ops
│   └── Controls/
│       ├── Base/                          # HTML-specific base classes
│       │   ├── ControlBase.cs             # With HTML helpers (GetAttribute, etc.)
│       │   ├── PageBase.cs
│       │   ├── LoadingPageBase.cs         # Pages with loading states
│       │   ├── ContentControlBase.cs      # With Hover support
│       │   ├── TextControlBase.cs         # With Focus/Blur
│       │   ├── ToggleControlBase.cs
│       │   ├── SelectorControlBase.cs     # With SelectByValue
│       │   └── RangeControlBase.cs        # With GetStep, GetPercentage
│       ├── ButtonControl.cs
│       ├── TextInputControl.cs
│       ├── TextAreaControl.cs
│       ├── LabelControl.cs
│       ├── LinkControl.cs
│       ├── CheckBoxControl.cs
│       ├── SelectControl.cs
│       ├── RangeInputControl.cs
│       └── ProgressControl.cs
│
└── Oravey.UITestFramework.Mocking/
    ├── MockApiServer.cs                   # WireMock wrapper
    └── MockEndpoint.cs                    # Endpoint configuration
```

---

## 2.3 Layer Responsibilities

### 2.3.1 Core Layer (Interfaces Only)

| Component | Responsibility |
|-----------|----------------|
| `Platform` enum | Platform identification with extension methods |
| `ITestContext` | Simplified interface: logging, config, waiting |
| `IPageObject` | Page object contract |
| `IControlObject` | Base control contract |
| `ITextControl` | Text input contract |
| `IToggleControl` | Toggle control contract |
| `ISelectorControl` | Selection control contract |
| `IRangeControl` | Range/slider contract |
| `IItemsControl` | Items/collection contract |
| `ITestLogger` | CSV format structured logging |
| Exceptions | Framework exception types |

**Note:** Core no longer contains base classes or adapters. All implementations are in platform projects.

### 2.3.2 Platform Layer (Self-Contained)

Each platform project is fully self-contained:

| Project | Native Driver | Base Classes |
|---------|---------------|--------------|
| `.Wpf` | FlaUI (direct) | `ControlBase`, `PageBase`, `BusyPageBase`, etc. |
| `.Maui` | Appium (direct) | `ControlBase`, `PageBase`, etc. |
| `.Html` | Selenium (direct) | `ControlBase`, `PageBase`, `LoadingPageBase`, etc. |

Platform projects implement Core interfaces while using their native drivers directly - no adapter abstraction layer.

### 2.3.3 Mocking Layer

| Component | Responsibility |
|-----------|----------------|
| `MockApiServer` | Start/stop WireMock server |
| Stub methods | Configure endpoint responses |
| Error simulation | Test error handling |
| Delay simulation | Test timeout handling |

### 2.3.4 Application Layer

| Component | Responsibility |
|-----------|----------------|
| `UITestBase` | Test lifecycle (launch, dispose) |
| Page Objects | Application-specific pages (using platform `PageBase`) |
| Tests | Application-specific test cases |

---

## 2.4 Data Flow

```
Test Method
    │
    ▼
PageObject.NavigateTo()
    │
    ├── Logger.Log("Navigate", ...)    ← CSV logging
    │
    ▼
ControlObject.Click()
    │
    ├── CheckClickable()               ← Always check before action
    │   ├── WaitClickable()
    │   │   ├── WaitVisible(true)
    │   │   └── WaitEnabled(true)
    │   └── throw if fails
    │
    ├── Driver.Click(element)          ← Platform driver
    │
    └── Logger.Log("Click", ...)       ← CSV logging
    
    ▼
PageObject.WaitForPageReady()
    │
    ├── WaitForDisplayed()             ← Key control visible
    │
    └── WaitForNotBusy()               ← IsBusy indicator false
    
    ▼
ControlObject.AssertText("expected")
    │
    ├── GetText()                      ← Get current value
    │
    ├── Compare with expected
    │
    ├── Logger.Log("Assert", ...)      ← CSV logging
    │
    └── throw AssertionException if fails
```

---

## 2.5 Key Design Decisions

### 2.5.1 Platform as Enum
**Decision:** Use `Platform` enum instead of string + `IsWindows`/`IsMobile` properties.

**Rationale:**
- Type safety at compile time
- Extension methods for platform queries
- No string comparison bugs
- Clearer intent in code

### 2.5.2 Core = Interfaces Only (v3)
**Decision:** Core contains only interfaces, exceptions, and utilities - no base classes or adapters.

**Rationale:**
- Cleaner separation of concerns
- Each platform can optimize for its native driver
- No unnecessary abstraction overhead
- Simpler dependency graph

### 2.5.3 Platform Self-Containment (v3)
**Decision:** Each platform project has its own complete base class hierarchy.

**Rationale:**
- Platform-specific optimizations possible
- No forced inheritance from shared classes
- Each platform can add platform-specific methods to base classes
- HTML can have `GetAttribute()`, WPF can have `AsButton()`, etc.

### 2.5.4 No Adapter Layer (v3)
**Decision:** Platforms use native drivers directly (FlaUI, Appium, Selenium).

**Rationale:**
- Adapter layer added complexity without value
- Native driver APIs are fundamentally different
- Direct access enables platform-specific features
- Simpler debugging and stack traces

### 2.5.5 Navigation Returns Void (v3)
**Decision:** Navigation methods return `void`; tests create page objects explicitly.

**Rationale:**
- Clearer separation of navigation action and page creation
- Tests have full control over page instantiation
- Easier to test navigation failures
- More explicit and readable test code

### 2.5.6 Virtual Methods
**Decision:** All base class methods are `virtual`.

**Rationale:**
- Platform-specific overrides possible
- Test customization without inheritance workarounds
- Consistent extensibility pattern

### 2.5.7 Always Check Before Action
**Decision:** Every action method calls a Check method first.

**Rationale:**
- Fail fast with clear error messages
- Consistent behavior across platforms
- No silent failures

### 2.5.8 CSV Logging
**Decision:** Structured CSV format for all log entries.

**Rationale:**
- Machine-parseable for analysis
- Consistent format across tests
- Easy filtering and aggregation

---

*Next: [Core Framework](21d3_CoreFramework.md)*
