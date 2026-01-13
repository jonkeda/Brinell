# Project Structure: Brinell UI Test Framework

## Directory Organization

```Markdown
Brinell/
├── .github/                    # GitHub-specific files
│   └── copilot-instructions.md # Copilot AI guidance
├── samples/                    # Example applications
├── srcnew/                     # Source code (see details below)
├── tests/                      # Test projects (see details below)
├── Directory.Build.props       # Shared MSBuild properties
├── Directory.Packages.props    # Central package management
├── BrinellNew.sln                 # Solution file
├── global.json                 # SDK version pinning
├── nuget.config                # NuGet configuration
├── README.md                   # Project readme
├── CHANGELOG.md                # Version history
├── CONTRIBUTING.md             # Contribution guidelines
├── LICENSE                     # MIT license
└── VERSIONING.md               # Versioning policy
```

### Source Code Structure (`src/`)

```Markdown
```

### Test Structure (`tests/`)

```Markdown
```

## Naming Conventions

### Files

| Type | Convention | Example |
|------|------------|---------|
| **Interfaces** | `I` + PascalCase | `IControlObject.cs`, `ITextControl.cs` |
| **Classes** | PascalCase | `ButtonControl.cs`, `MauiTestContext.cs` |
| **Base Classes** | PascalCase + `Base` | `ControlObjectBase.cs`, `TextControlBase.cs` |
| **Test Files** | Class + `Tests` | `ControlObjectTests.cs` |
| **Projects** | `Brinell.` prefix | `Brinell.Core`, `Brinell.Maui` |
| **Test Projects** | Project + `.Tests` | `Brinell.Core.Tests.` |

### Code

| Type | Convention | Example |
|------|------------|---------|
| **Namespaces** | PascalCase, matches folder | `Brinell.Core.Abstractions.Controls` |
| **Interfaces** | `I` prefix + PascalCase | `IControlObject`, `IClickableControl` |
| **Classes** | PascalCase | `ButtonControl`, `MauiTestContext` |
| **Methods** | PascalCase | `IsExists()`, `WaitVisible()` |
| **Properties** | PascalCase | `AutomationId`, `Page` |
| **Private Fields** | `_` prefix + camelCase | `_element`, `_context` |
| **Parameters** | camelCase | `timeoutMs`, `automationId` |
| **Constants** | PascalCase | `DefaultTimeout`, `MaxRetries` |
| **Local Variables** | camelCase | `element`, `isVisible` |

### Interface Naming Patterns

```csharp
// Base control interface
IControlObject              // Foundation for all control objects

// naming (noun-style with ControlObject suffix)
IClickableControlObject     // clickable control
ITextControlObject          // text control
IToggleControlObject        // toggle control
```

### Control Class Naming Patterns

```csharp
// Platform-specific controls
ButtonControl               // MAUI button
EntryControl                // MAUI text entry
PickerControl               // MAUI dropdown picker

// Base classes
ControlObjectBase           // All controls inherit
TextControlObjectBase       // Text-related controls
ToggleControlObjectBase     // Toggle-related controls
SelectorControlObjectBase   // Selection controls
ContainerControlObjectBase  // Container controls
```

## Import Patterns

### Import Order

```csharp
// 1. System namespaces
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 2. External package namespaces
using OpenQA.Selenium;
using Microsoft.Playwright;

// 3. Brinell.Core namespaces
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.ControlObjects;

// 4. Platform-specific namespaces
using Brinell.Maui.ControlObjects;
using Brinell.Maui.Infrastructure;
```

### Namespace Organization

```
Brinell.Core
├── Brinell.Core.Abstractions           # Core interfaces
├── Brinell.Core.Abstractions.ControlObjects  # Control interfaces
├── Brinell.Core.Attributes             # Test attributes
├── Brinell.Core.Configuration          # Configuration
├── Brinell.Core.Exceptions             # Exception types
├── Brinell.Core.Locators               # Locator types
└── Brinell.Core.Logging                # Logging

Brinell.Maui
├── Brinell.Maui.Abstractions           # Platform abstractions
├── Brinell.Maui.ControlsObject         # Control implementations
├── Brinell.Maui.Context                # v6 Test context
├── Brinell.Maui.Pages                  # v6 Page objects
├── Brinell.Maui.Gestures               # Mobile gestures
├── Brinell.Maui.Infrastructure         # Platform setup
├── Brinell.Maui.Services               # Services
└── Brinell.Maui.Testing                # Test bases
```

## Code Structure Patterns

### Interface Definition Pattern

```csharp
namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// [XML documentation describing purpose]
/// </summary>
public interface IControlName
{
    #region Properties
    
    /// <summary>Property documentation</summary>
    Locator PropertyName { get; }
    
    #endregion

    #region State Methods (Is/Wait/Check/Assert)
    
    bool IsState();
    bool WaitState(bool? expected = true, int? timeoutMs = null);
    void CheckState(bool? expected = true, int? timeoutMs = null);
    void AssertState(string? message = null);
    
    #endregion

    #region Action Methods
    
    void DoAction();
    
    #endregion
}
```

### Control Implementation Pattern

```csharp
namespace Brinell.Platform.Controls;

/// <summary>
/// Platform-specific control implementation.
/// </summary>
public class ControlName : ControlBase, IControlInterface
{
    #region Constructors
    
    public ControlName(TestContext context, string automationId) 
        : base(context, automationId) { }
    
    public ControlName(TestContext context, string automationId, IContainerControl? container) 
        : base(context, automationId, container) { }
    
    #endregion

    #region IControlInterface Implementation
    
    // Implement interface methods
    
    #endregion

    #region Protected Methods
    
    // Platform-specific helpers
    
    #endregion
}
```

### Test Class Pattern

```csharp
namespace Brinell.Platform.Tests;

public class ControlNameTests : PlatformTestBase
{
    #region Setup
    
    // Test setup if needed
    
    #endregion

    #region Existence Tests
    
    [Fact]
    public void IsExists_WhenElementExists_ReturnsTrue() { }
    
    #endregion

    #region State Tests
    
    [Fact]
    public void IsEnabled_WhenEnabled_ReturnsTrue() { }
    
    #endregion

    #region Action Tests
    
    [Fact]
    public void Click_WhenEnabled_PerformsClick() { }
    
    #endregion
}
```

## Code Organization Principles

### 1. Interface Segregation

Interfaces are organized by capability, not by control type:

```
IControlObject                # All controls (base)
├── IClickableControlObject   # Click capability
├── ITextControlObject        # Text display capability
├── IToggleControlObject      # Toggle capability
├── ISelectorControlObject    # Selection capability
└── IContainerControlObject   # Contains children
```

A single control can implement multiple capability interfaces:

```csharp
public class ButtonControl : ControlBase, IClickableControlObject, ITextControlObject
```

### 2. Self-Contained Platforms

Each platform package is self-contained:
- Has its own TestContext implementation
- Has its own control base classes
- Uses native automation library directly
- No dependencies on other platform packages

### 3. Is/Wait/Check/Assert Pattern

Every state-based capability follows this pattern:

| Method | Behavior | Returns |
|--------|----------|---------|
| `Is*()` | Immediate check, no waiting | `bool` |
| `Wait*(expected, timeout)` | Polls until condition or timeout | `bool` |
| `Check*(expected, timeout)` | Waits, throws if not met | `void` |
| `Assert*(message)` | Immediate assertion | `void` |

### 4. Container Scoping

Controls support scoped element search:

```csharp
// Global search
var button = new ButtonControl(this, "SaveButton");

// Scoped to container
var container = new FrameControl(this, "DialogFrame");
var button = new ButtonControl(this, "SaveButton");
```

## Module Boundaries

### Dependency Direction

```
Application Tests
       ↓
Platform Packages (Brinell.Maui, Brinell.Wpf, etc.)
       ↓
Brinell.Core (interfaces only)
       ↓
External Libraries (FlaUI, Appium, Selenium, Playwright)
```

### Allowed Dependencies

| Package | May Depend On |
|---------|--------------|
| `Brinell.Core` | System libraries |
| `Brinell.Wpf` | Brinell.Core, FlaUI |
| `Brinell.Maui` | Brinell.Core, Appium |
| `Brinell.Html` | Brinell.Core, Playwright |
| `Brinell.Testing` | Brinell.Core, test utilities |
| `Brinell.Mocking` | Brinell.Core, WireMock |
| Test Projects | Any Brinell package, test frameworks |

### Forbidden Dependencies

- Platform packages MUST NOT depend on other platform packages
- Core MUST NOT depend on any platform package
- Core MUST NOT depend on external automation libraries

## Code Size Guidelines

### File Size

| Type | Guideline |
|------|-----------|
| Interface files | < 200 lines (focus on single capability) |
| Control implementations | < 500 lines (consider splitting) |
| Test files | < 400 lines (group by feature) |
| Base classes | < 600 lines (may be larger) |

### Method Size

| Type | Guideline |
|------|-----------|
| Public methods | < 30 lines |
| Private helpers | < 50 lines |
| Test methods | < 20 lines (Arrange-Act-Assert) |

### Complexity Guidelines

- Maximum nesting depth: 3 levels
- Maximum parameters per method: 5
- Prefer method extraction over complex conditionals
- Use regions for organizing large files

## Documentation Standards

### XML Documentation Required For

- All public interfaces and their members
- All public classes and their members
- All public methods, properties, and events
- Exception descriptions with `<exception>` tags

### XML Documentation Format

```csharp
/// <summary>
/// Brief description of the member.
/// </summary>
/// <param name="paramName">Description of the parameter.</param>
/// <returns>Description of the return value.</returns>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
/// <remarks>
/// Additional details, usage examples, or important notes.
/// </remarks>
/// <example>
/// <code>
/// // Example usage
/// var control = new ButtonControl(context, "myButton");
/// </code>
/// </example>
```

**Document Version:** 1.0  
**Created:** January 13, 2026  
**Workflow:** steering_workflow/structure
