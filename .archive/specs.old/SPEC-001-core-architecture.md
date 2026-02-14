# SPEC-001: Core Architecture

**Version:** 3.1  
**Status:** Active  
**Last Updated:** January 2026  
**Implements:** REQ-001 (Multi-Platform Support), REQ-009 (Test Isolation)

---

## 1. Purpose

This specification defines the architectural structure of the UI Test Framework, including component relationships, layer responsibilities, and project organization.

---

## 2. Architecture Overview

### 2.1 Four-Layer Architecture

The framework consists of four distinct layers:

```
┌────────────────────────────────────────────────────────────┐
│ Layer 4: Application Tests                                │
│ • Application-specific page objects                       │
│ • Test classes                                            │
│ • Test data and fixtures                                  │
└────────────────────────┬───────────────────────────────────┘
                         │ depends on
┌────────────────────────▼───────────────────────────────────┐
│ Layer 3: Platform Implementations (Self-Contained)        │
│ • Brinell.Wpf                                             │
│ • Brinell.WinForms                                        │
│ • Brinell.Maui                                            │
│ • Brinell.Html                                            │
│ • Brinell.Html.Playwright                                 │
│ • Brinell.Stride                                          │
│                                                            │
│ Each contains:                                            │
│ • TestContext (ITestContext + element operations)         │
│ • Complete base class hierarchy                           │
│ • Platform-specific controls                              │
│ • Native driver access (no adapters)                      │
└────────────────────────┬───────────────────────────────────┘
                         │ depends on
┌────────────────────────▼───────────────────────────────────┐
│ Layer 2: Core (Interfaces Only)                          │
│ • Brinell.Core                                            │
│                                                            │
│ Contains:                                                 │
│ • Interface contracts (ITestContext, IPageObject, etc.)   │
│ • Platform enum with extension methods                    │
│ • Logging interfaces and CSV logger                       │
│ • Exception types                                         │
│ • Utilities (configuration, attributes)                   │
└────────────────────────┬───────────────────────────────────┘
                         │
┌────────────────────────▼───────────────────────────────────┐
│ Layer 1: External Libraries                               │
│ • FlaUI.Core (WPF, WinForms)                              │
│ • Appium.WebDriver (MAUI/Mobile)                          │
│ • Selenium.WebDriver (Web)                                │
│ • Playwright (Blazor/Modern Web)                          │
│ • xUnit                                                   │
└────────────────────────────────────────────────────────────┘
```

---

## 3. Layer Specifications

### 3.1 Core Layer (Layer 2)

**Project:** `Brinell.Core`  
**Purpose:** Define interface contracts without implementation

#### 3.1.1 Core Layer MUST Contain

| Component | Description |
|-----------|-------------|
| **Interface Contracts** | `ITestContext`, `IPageObject`, `IControlObject`, and capability interfaces |
| **Platform Enum** | Type-safe platform identification with extension methods |
| **Logging Contracts** | `ITestLogger` interface and `CsvTestLogger` implementation |
| **Exception Types** | Framework-specific exception classes |
| **Configuration** | Configuration models and helpers |
| **Attributes** | Test attributes for platform filtering |

#### 3.1.2 Core Layer MUST NOT Contain

- Base class implementations (moved to platform projects in v3.0)
- Adapter abstractions (removed in v3.0)
- Platform-specific code
- Direct dependencies on FlaUI, Appium, or Selenium

#### 3.1.3 Core Layer Dependencies

```xml
<ItemGroup>
  <!-- Minimal dependencies -->
  <PackageReference Include="Microsoft.Extensions.Configuration" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" />
</ItemGroup>
```

**Rationale:** See [DES-002: Interface-Based Design](DES-002-interface-based-design.md)

---

### 3.2 Platform Layer (Layer 3)

**Projects:** 
- `Brinell.Wpf`
- `Brinell.WinForms`
- `Brinell.Maui`
- `Brinell.Html`
- `Brinell.Html.Playwright`
- `Brinell.Stride`

**Purpose:** Provide complete, self-contained implementations for each platform

#### 3.2.1 Platform Project Structure

```
Brinell.{Platform}/
├── Infrastructure/
│   ├── {Platform}TestContext.cs       # Implements ITestContext
│   └── {Platform}DriverAdapter.cs     # App lifecycle management
├── Controls/
│   ├── Base/
│   │   ├── ControlBase.cs             # Implements IControlObject
│   │   ├── PageBase.cs                # Implements IPageObject
│   │   ├── BusyPageBase.cs            # IsBusy tracking (required)
│   │   ├── ContentControlBase.cs      # Clickable controls
│   │   ├── TextControlBase.cs         # Text input controls
│   │   ├── ToggleControlBase.cs       # Toggle controls
│   │   ├── SelectorControlBase.cs     # Selection controls
│   │   ├── RangeControlBase.cs        # Range controls
│   │   └── ItemsControlBase.cs        # Collection controls
│   └── [Concrete control classes]
└── Testing/
    └── {Platform}UITestBase.cs        # Base class for tests
```

#### 3.2.2 Platform Layer MUST Provide

1. **TestContext Implementation**
   - Implements `ITestContext` interface
   - Provides element finding and interaction methods
   - Manages application lifecycle
   - Accesses native driver directly (FlaUI/Appium/Selenium)

2. **Complete Base Class Hierarchy**
   - `ControlBase` implementing `IControlObject`
   - `PageBase` implementing `IPageObject`
   - Capability base classes (TextControlBase, etc.)
   - All methods MUST be `virtual` for extensibility

3. **Concrete Control Classes**
   - Button, TextBox, Label, CheckBox, etc.
   - Platform-specific controls (e.g., WPF DataGrid, HTML Select)
   - Inherit from appropriate capability base classes

4. **Test Base Class**
   - Manages test lifecycle (setup, teardown)
   - Provides application launch helpers
   - Handles resource cleanup

#### 3.2.3 Platform Layer Dependencies

Each platform project references:
- `Brinell.Core` (interfaces)
- Native automation library (FlaUI, Appium, Selenium, or Playwright)
- xUnit framework

```xml
<!-- WPF Example -->
<ItemGroup>
  <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  <PackageReference Include="FlaUI.Core" Version="4.0.0" />
  <PackageReference Include="FlaUI.UIA3" Version="4.0.0" />
  <PackageReference Include="xunit" Version="2.9.*" />
</ItemGroup>
```

#### 3.2.4 Platform Isolation

Platform projects MUST be self-contained:
- MUST NOT reference other platform projects
- MUST NOT share base class implementations
- MUST use native drivers directly (no shared adapter layer)

**Rationale:** See [DES-003: Native Driver Access](DES-003-native-driver-access.md)

---

### 3.3 Application Test Layer (Layer 4)

**Projects:** Application-specific test projects (e.g., `MyApp.UITests`)

**Purpose:** Contains application-specific test code

#### 3.3.1 Application Test Project Structure

```
MyApp.UITests/
├── PageObjects/
│   ├── MainWindowPage.cs
│   ├── SettingsPage.cs
│   └── Dialogs/
│       └── ConfirmDialog.cs
├── Tests/
│   ├── NavigationTests.cs
│   ├── SettingsTests.cs
│   └── DataEntryTests.cs
├── TestData/
│   └── Users.json
├── Fixtures/
│   └── TestDataFixture.cs
├── appsettings.json
└── MyApp.UITests.csproj
```

#### 3.3.2 Application Test Layer MUST Contain

1. **Page Objects**
   - Application-specific page classes
   - Inherit from platform `PageBase`
   - Encapsulate page structure and behavior

2. **Test Classes**
   - xUnit test classes
   - Inherit from platform `UITestBase`
   - Follow AAA pattern (Arrange-Act-Assert)

3. **Configuration**
   - `appsettings.json` with application path, timeouts, etc.
   - Environment-specific settings

4. **Test Data**
   - JSON files, builders, fixtures
   - Isolated per test or shared via fixtures

#### 3.3.3 Application Test Layer Dependencies

```xml
<ItemGroup>
  <!-- Reference ONE platform implementation -->
  <ProjectReference Include="..\Brinell.Wpf\Brinell.Wpf.csproj" />
  
  <!-- Test framework -->
  <PackageReference Include="xunit" Version="2.9.*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.9.*" />
  <!-- Note: FluentAssertions is NOT recommended - use built-in Assert methods -->
</ItemGroup>
```

---

## 4. Component Relationships

### 4.1 Dependency Rules

1. **Downward Dependencies Only**
   - Application tests depend on platform implementations
   - Platform implementations depend on core interfaces
   - Core has no internal framework dependencies

2. **No Circular Dependencies**
   - Core MUST NOT depend on platform implementations
   - Platform implementations MUST NOT depend on each other
   - Tests MUST NOT be referenced by framework

3. **No Upward References**
   - Platform implementations MUST NOT know about specific applications
   - Core MUST NOT know about platform implementations

### 4.2 Communication Flow

```
Test Method
    │
    └─> PageObject (Application Layer)
            │
            └─> Control (Platform Layer)
                    │
                    ├─> ControlBase.CheckClickable()
                    │       │
                    │       └─> WaitVisible() + WaitEnabled()
                    │               │
                    │               └─> TestContext.WaitFor()
                    │
                    ├─> Native Driver (FlaUI/Appium/Selenium)
                    │       │
                    │       └─> Automation API
                    │
                    └─> Logger.LogAction()
```

---

## 5. Platform Abstraction Strategy

### 5.1 Abstraction Through Interfaces (Not Inheritance)

**v3.0 Change:** Framework uses interfaces for abstraction, not shared base classes.

| Abstraction | v2.0 (Old) | v3.0 (New) |
|-------------|------------|------------|
| **Core** | Base classes + adapters | Interfaces only |
| **Platform** | Inherits from Core | Implements Core interfaces |
| **Driver Access** | Through adapter | Direct access |

### 5.2 Platform Selection

Applications select platform at compile time by referencing the appropriate platform project:

```csharp
// WPF Application
using Brinell.Wpf;

public class MyTests : WpfUITestBase
{
    [Fact]
    public void Test_Something()
    {
        var page = LaunchApp<MainWindowPage>();
        // Uses FlaUI directly
    }
}
```

---

## 6. Project Organization

### 6.1 Solution Structure

```
Brinell.sln
├── src/
│   ├── Brinell.Core/
│   ├── Brinell.Wpf/
│   ├── Brinell.WinForms/
│   ├── Brinell.Maui/
│   ├── Brinell.Html/
│   ├── Brinell.Html.Playwright/
│   ├── Brinell.Stride/
│   ├── Brinell.Stride.Automation/    # In-game automation handler
│   ├── Brinell.Testing/              # Test utilities
│   └── Brinell.Mocking/              # API mocking support
├── samples/
│   ├── Brinell.Samples.Wpf.App/
│   ├── Brinell.Samples.Wpf.UITests/
│   ├── Brinell.Samples.Maui.App/
│   ├── Brinell.Samples.Maui.UITests/
│   ├── Brinell.Samples.Blazor.App/
│   ├── Brinell.Samples.Blazor.PlaywrightTests/
│   └── ...
├── tests/
└── docs/
```

### 6.2 Namespace Organization

```
Brinell.Core
├── Abstractions
│   ├── ITestContext
│   ├── IPageObject
│   └── Controls
│       ├── IControlObject
│       ├── ITextControl
│       ├── IToggleControl
│       ├── ISelectorControl
│       ├── IRangeControl
│       ├── IItemsControl
│       └── IContainerControl
├── Logging
│   ├── ITestLogger
│   └── CsvTestLogger
├── Exceptions
└── Configuration

Brinell.Wpf
├── Infrastructure
│   └── FlaUITestContext
├── Controls
│   ├── Base
│   │   ├── ControlBase
│   │   ├── PageBase
│   │   ├── BusyPageBase
│   │   └── ...
│   └── [Concrete controls]
└── Testing
    └── WpfUITestBase
```

---

## 7. Configuration Management

### 7.1 Configuration Sources (Priority Order)

1. **Environment Variables** (highest priority)
2. **appsettings.{Environment}.json**
3. **appsettings.json** (lowest priority)

### 7.2 Standard Configuration Schema

```json
{
  "UITest": {
    "Platform": "Windows",
    "ApplicationPath": "path/to/app.exe",
    "DefaultTimeoutMs": 10000,
    "ShortTimeoutMs": 3000,
    "PollingIntervalMs": 250,
    "LogOutputPath": "logs",
    "ScreenshotPath": "screenshots",
    "Platforms": {
      "Windows": {
        "ApplicationPath": "bin/Debug/net9.0-windows/MyApp.exe"
      },
      "Web": {
        "BaseUrl": "https://localhost:5001",
        "BrowserType": "Chrome"
      }
    }
  }
}
```

---

## 8. Thread Safety

### 8.1 Thread Safety Requirements

1. **TestContext**
   - MUST be thread-safe for property access
   - MUST NOT be shared between tests running in parallel
   - Each test MUST create its own context instance

2. **Loggers**
   - MUST be thread-safe for concurrent writes
   - SHOULD use file locking or per-test log files

3. **Configuration**
   - MUST be thread-safe for read operations
   - Configuration loading MAY use singleton pattern

### 8.2 Test Isolation

Tests MUST be isolated:
- Each test gets its own application instance
- Each test gets its own test context
- Shared fixtures MUST be thread-safe

---

## 9. Extension Points

### 9.1 Framework Extension Points

1. **Custom Controls**
   ```csharp
   public class CustomGridControl : ItemsControlBase
   {
       // Custom implementation
   }
   ```

2. **Custom Page Base**
   ```csharp
   public class CustomPageBase : PageBase
   {
       // Custom page behavior
   }
   ```

3. **Custom Test Base**
   ```csharp
   public class CustomTestBase : WpfUITestBase
   {
       // Custom test lifecycle
   }
   ```

4. **Custom Loggers**
   ```csharp
   public class CustomLogger : ITestLogger
   {
       // Custom logging implementation
   }
   ```

**Rationale:** See [DES-005: Virtual Methods](DES-005-virtual-methods.md)

---

## 10. Verification

### 10.1 Architecture Verification

The architecture MUST be verified through:

1. **Dependency Analysis**
   - No circular dependencies
   - Core has no platform dependencies
   - Platforms don't reference each other

2. **Interface Compliance**
   - All platform implementations implement Core interfaces
   - All public APIs have XML documentation

3. **Isolation Testing**
   - Tests can run in parallel
   - No shared mutable state

---

## 11. Change History

| Version | Date | Changes |
|---------|------|---------|
| 3.1 | Jan 2026 | Renamed to Brinell.*, added WinForms/Playwright/Stride platforms, BusyPageBase now required, removed FluentAssertions reference |
| 3.0 | Dec 2025 | Core = interfaces only, platform-specific base classes, direct driver access |
| 2.0 | Dec 2025 | Added logging layer, structured configuration |
| 1.0 | Nov 2025 | Initial architecture specification |

---

*Next: [SPEC-002: Interface Contracts](SPEC-002-interface-contracts.md)*
