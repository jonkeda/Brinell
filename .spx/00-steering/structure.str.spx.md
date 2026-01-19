# Project Structure: Brinell UI Test Framework

## Directory Organization

```Markdown
Brinell/
├── .github/                    # GitHub-specific files
│   └── copilot-instructions.md # Copilot AI guidance
├── .spx/                       # SPX workflow documents
│   └── 00-steering/            # Steering documents
├── samples/                    # Example applications
│   ├── Brinell.Samples.Maui.App/          # MAUI sample app
│   ├── Brinell.Samples.Maui.UITests/      # MAUI UI tests
│   ├── Brinell.Samples.Blazor.App/        # Blazor sample app
│   └── ...                                 # Other platform samples
├── srcnew/                     # Source code (see details below)
├── testsnew/                   # Test projects (see details below)
├── Directory.Build.props       # Shared MSBuild properties
├── Directory.Packages.props    # Central package management
├── Brinell.sln                 # Solution file
├── global.json                 # SDK version pinning
├── nuget.config                # NuGet configuration
├── README.md                   # Project readme
├── CHANGELOG.md                # Version history
├── CONTRIBUTING.md             # Contribution guidelines
├── LICENSE                     # MIT license
└── VERSIONING.md               # Versioning policy
```

### Source Code Structure (`srcnew/`)

```Markdown
srcnew/
├── .spx/                           # SPX specifications for srcnew
│   ├── 01-specs/                   # Feature specifications
│   ├── 02-issues/                  # Issue tracking
│   └── 03-fixes/                   # Bug fix documentation
├── Brinell.Core/                   # Core interfaces (no dependencies)
│   ├── Abstractions/               # Base abstractions
│   │   └── Controls/               # Control base classes
│   ├── Attributes/                 # Test attributes
│   ├── Configuration/              # TimeoutSettings, etc.
│   ├── Exceptions/                 # Exception types
│   ├── Interfaces/                 # All core interfaces
│   │   ├── IControlObject.cs       # Base control with TScope
│   │   ├── IClickableControlObject.cs
│   │   ├── IEditableTextControlObject.cs
│   │   ├── IToggleControlObject.cs
│   │   ├── ISelectorControlObject.cs
│   │   ├── IRangeControlObject.cs
│   │   ├── IScrollableControlObject.cs
│   │   ├── IContainerControl.cs
│   │   ├── IElementScope.cs        # Hierarchical scope
│   │   ├── IPageObject.cs
│   │   └── ITestContext.cs
│   ├── Locators/                   # Locator, LocatorStrategy
│   ├── Logging/                    # ITestLogger, LogResult
│   ├── Models/                     # Shared models
│   ├── Services/                   # Service interfaces
│   ├── Testing/                    # Test base classes
│   └── Utilities/                  # Common utilities
├── Brinell.Maui/                   # MAUI platform (active)
│   ├── Context/                    # IMauiTestContext, MauiTestContext
│   ├── Controls/                   # MAUI control implementations
│   │   ├── MauiControlBase.cs      # Base with Is/Wait/Assert
│   │   ├── MauiButtonControl.cs    # IClickableControlObject
│   │   ├── MauiEntryControl.cs     # IEditableTextControlObject
│   │   ├── MauiContainerBase.cs    # Container scoping
│   │   ├── MauiListControl.cs      # List container
│   │   ├── MauiTabControl.cs       # Tab control
│   │   └── MauiFlyoutItemControl.cs
│   ├── Extensions/                 # Extension methods
│   ├── Gestures/                   # Mobile gesture support
│   ├── Interfaces/                 # MAUI-specific interfaces
│   │   ├── IMauiScope.cs           # Scope abstraction
│   │   ├── IMauiPage.cs
│   │   ├── IMauiContainer.cs
│   │   └── IMauiElement.cs
│   ├── Pages/                      # MauiPageObjectBase<TSelf>
│   ├── Testing/                    # Test fixtures
│   └── Wrappers/                   # Element wrappers
├── Brinell.Wpf/                    # WPF platform (placeholder)
├── Brinell.WinForms/               # WinForms platform (placeholder)
├── Brinell.Html/                   # HTML/Playwright platform (placeholder)
├── Brinell.Blazor/                 # Blazor platform (placeholder)
├── Brinell.Stride/                 # Stride game engine (placeholder)
│   └── Communication/              # Named pipe communication
├── Brinell.Mocking/                # API mocking (WireMock)
│   ├── MockApiServer.cs
│   └── ApiStubBuilder.cs
├── Brinell.Automation/             # Stride automation host
│   ├── AutomationServer.cs
│   ├── AutomationGameSystem.cs
│   └── StrideUIHandler.cs
├── Directory.Build.props           # Shared build properties
└── Directory.Packages.props        # Central package versions
```

### Test Structure (`testsnew/`)

```Markdown
testsnew/
├── Brinell.Core.Tests/             # Core interface unit tests
├── Brinell.Maui.Tests/             # MAUI unit tests
│   └── FluentChainingTests.cs      # Fluent API tests
├── Brinell.Maui.UITests/           # MAUI UI integration tests
│   ├── AppiumFixture.cs            # Test fixture with Appium
│   ├── AppiumCollection.cs         # xUnit collection
│   ├── TestConstants.cs            # Test configuration
│   ├── Pages/                      # Page objects for sample app
│   │   ├── MainPage.cs
│   │   ├── AppShellPage.cs
│   │   └── ContainerDemoPage.cs
│   ├── Containers/                 # Container definitions
│   │   ├── ContactContainer.cs
│   │   ├── TaskItemContainer.cs
│   │   ├── UserProfileContainer.cs
│   │   ├── OuterContainer.cs
│   │   └── InnerContainer.cs
│   └── Tests/                      # Test classes
│       ├── MainPageTests.cs
│       ├── ButtonControlTests.cs
│       ├── EntryControlTests.cs
│       ├── ContainerScopingTests.cs
│       ├── NestedContainerTests.cs
│       ├── IndexedContainerTests.cs
│       └── ListContainerTests.cs
├── Brinell.Blazor.Tests/           # Blazor unit tests
├── Brinell.Blazor.UITests/         # Blazor UI tests (placeholder)
├── Brinell.Html.Tests/             # HTML unit tests
├── Brinell.Html.UITests/           # HTML UI tests (placeholder)
├── Brinell.Wpf.Tests/              # WPF unit tests
├── Brinell.Wpf.UITests/            # WPF UI tests (placeholder)
├── Brinell.WinForms.Tests/         # WinForms unit tests
├── Brinell.WinForms.UITests/       # WinForms UI tests (placeholder)
├── Brinell.Stride.Tests/           # Stride unit tests
├── Brinell.Stride.UITests/         # Stride UI tests (placeholder)
├── Brinell.Mocking.Tests/          # Mocking tests
├── Brinell.Automation.Tests/       # Automation tests
├── Directory.Build.props           # Test-specific build props
└── Directory.Packages.props        # Test package versions
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
// Base control interface with generic scope for fluent chaining
IControlObject<TScope>              // Foundation for all control objects

// Capability interfaces (noun-style with ControlObject suffix)
IClickableControlObject<TScope>     // Click capability
ITextControlObject<TScope>          // Text display capability
IEditableTextControlObject<TScope>  // Text input capability
IToggleControlObject<TScope>        // Toggle capability
ISelectorControlObject<TScope>      // Selection capability
IRangeControlObject<TScope>         // Range value capability
IScrollableControlObject<TScope>    // Scrolling capability

// Scoping interfaces
IElementScope<TElement>             // Element finding abstraction
IContainerControl<TElement>         // Container with scoped search
IPageObject<TElement>               // Page-level scope
```

### Control Class Naming Patterns

```csharp
// Platform prefix + control name
MauiControlBase<TScope>             // MAUI base control
MauiButtonControl<TScope>           // MAUI button (IClickableControlObject)
MauiEntryControl<TScope>            // MAUI text entry (IEditableTextControlObject)
MauiContainerBase<TParent, TSelf>   // MAUI container base

// Page object base with CRTP
MauiPageObjectBase<TSelf>           // MAUI page (TSelf for fluent)

// Similar patterns for other platforms
WpfControlBase<TScope>              // WPF controls
BlazorControlBase<TScope>           // Blazor controls
HtmlControlBase<TScope>             // HTML/Playwright controls
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
├── Brinell.Core.Abstractions.Controls  # Base control abstractions
├── Brinell.Core.Attributes             # Test attributes
├── Brinell.Core.Configuration          # TimeoutSettings, etc.
├── Brinell.Core.Exceptions             # Exception types
├── Brinell.Core.Interfaces             # All core interfaces
├── Brinell.Core.Locators               # Locator, LocatorStrategy
├── Brinell.Core.Logging                # ITestLogger, LogResult
├── Brinell.Core.Models                 # Shared models
├── Brinell.Core.Services               # Service interfaces
├── Brinell.Core.Testing                # Test base classes
└── Brinell.Core.Utilities              # Common utilities

Brinell.Maui
├── Brinell.Maui                        # Root namespace
├── Brinell.Maui.Context                # MauiTestContext
├── Brinell.Maui.Controls               # Control implementations
├── Brinell.Maui.Extensions             # Extension methods
├── Brinell.Maui.Gestures               # Mobile gestures
├── Brinell.Maui.Interfaces             # MAUI-specific interfaces
├── Brinell.Maui.Pages                  # Page object base classes
├── Brinell.Maui.Testing                # Test fixtures
└── Brinell.Maui.Wrappers               # Element wrappers
```

## Code Structure Patterns

### Interface Definition Pattern

```csharp
namespace Brinell.Core.Interfaces;

/// <summary>
/// [XML documentation describing purpose]
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public interface IControlName<TScope>
{
    // State methods (immediate, no waiting)
    bool IsState();
    
    // Wait methods (poll until condition or timeout)
    // Nullable skip pattern: null expected means skip
    bool WaitState(bool? expected, int? timeoutMs = null);
    
    // Assert methods (wait, throw on failure, return scope)
    TScope AssertState(bool? expected, string? message = null, int? timeoutMs = null);
    
    // Action methods (return scope for chaining)
    TScope DoAction(int? timeoutMs = null);
}
```

### Control Implementation Pattern

```csharp
namespace Brinell.Maui.Controls;

/// <summary>
/// Platform-specific control implementation with fluent chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type (page or container).</typeparam>
public class MauiControlName<TScope> : MauiControlBase<TScope>, IControlInterface<TScope>
    where TScope : IMauiScope<TScope>
{
    public MauiControlName(IMauiScope<TScope> scope, Locator locator) 
        : base(scope, locator) { }
    
    public MauiControlName(IMauiScope<TScope> scope, string locatorValue) 
        : base(scope, locatorValue) { }
    
    // IControlInterface Implementation
    // Uses RunWithElement for logging and element finding
    public TScope DoAction(int? timeoutMs = null)
    {
        return RunWithElement(nameof(DoAction), timeoutMs, element =>
        {
            // Core operation with pre-found element
            element.Click();
        });
    }
}
```

### Page Object Pattern

```csharp
namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object with factory methods for creating controls.
/// Uses CRTP (MauiPageObjectBase<TSelf>) for fluent returns.
/// </summary>
public class MainPage : MauiPageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context) : base(context) { }

    public override string Name => "MainPage";

    public override bool IsLoaded(int? timeoutMs = null)
        => TitleLabel.IsExists();

    // Control factory methods return controls scoped to this page
    public MauiControlBase<MainPage> TitleLabel => Control("TitleLabel");
    public MauiButtonControl<MainPage> GreetButton => Button("GreetButton");
    public MauiEntryControl<MainPage> NameEntry => Entry("NameEntry");
}
```

### Container Pattern

```csharp
namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Container that scopes child element searches.
/// TParent is the parent scope (page or container).
/// TSelf is this container type (for fluent returns within container).
/// </summary>
public class ContactContainer : MauiContainerBase<ContainerDemoPage, ContactContainer>
{
    private readonly int _index;

    public ContactContainer(IMauiScope<ContainerDemoPage> parentScope, int index)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, $"Contact_{index}"))
    {
        _index = index;
    }

    public int Index => _index;
    
    // Controls scoped within this container
    public MauiControlBase<ContactContainer> NameLabel => new(this, "ContactName");
    public MauiButtonControl<ContactContainer> CallButton => Button("ContactCallButton");
}
```

### Test Class Pattern

```csharp
namespace Brinell.Maui.UITests.Tests;

[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Page", "MainPage")]
public class MainPageTests
{
    private readonly AppiumFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public MainPageTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Feature", "Greeting")]
    public void MainPage_EnterNameAndGreet_ShowsGreetingMessage()
    {
        // Arrange
        Page.NameEntry.Clear();

        // Act - fluent chain
        Page.NameEntry.Enter("Alice")
            .GreetButton.Click();

        // Assert
        Page.GreetingLabel.AssertText("Hello, Alice!");
    }
    
    [Fact]
    [Trait("Pattern", "FluentChaining")]
    public void MainPage_FluentChaining_WorksCorrectly()
    {
        // Full fluent chain from arrange through assert
        Page.NameEntry.Clear()
            .NameEntry.Enter("Bob")
            .NameEntry.AssertText("Bob")
            .GreetButton.Click()
            .GreetingLabel.AssertText("Hello, Bob!");
    }
}
```

## Code Organization Principles

### 1. Scope-Based Element Finding

Element finding uses hierarchical scopes with IElementScope abstraction:

```
IElementScope<TElement>           # Base element finding
├── IPageObject<TElement>         # Page scope (root search)
└── IContainerControl<TElement>   # Container scope (scoped search)
```

Scopes provide:
- `TryFindElement(Locator)` - Returns null if not found
- `FindElement(Locator)` - Throws if not found
- `FindElements(Locator)` - Returns all matches
- `IsReady(timeout)` / `WaitReady(timeout)` - Scope availability

### 2. Interface Segregation

Interfaces are organized by capability, not by control type:

```
IControlObject<TScope>                # All controls (base)
├── IClickableControlObject<TScope>   # Click capability
├── ITextControlObject<TScope>        # Text display capability
│   └── IEditableTextControlObject<TScope>  # Text input
├── IToggleControlObject<TScope>      # Toggle capability
├── ISelectorControlObject<TScope>    # Selection capability
├── IRangeControlObject<TScope>       # Range value capability
└── IScrollableControlObject<TScope>  # Scrolling capability
```

A single control can implement multiple capability interfaces:

```csharp
public class MauiButtonControl<TScope> : MauiControlBase<TScope>, 
    IClickableControlObject<TScope>
```

### 3. Self-Contained Platforms

Each platform package is self-contained:
- Has its own TestContext implementation (IMauiTestContext, IWpfTestContext)
- Has its own control base classes (MauiControlBase<TScope>, WpfControlBase<TScope>)
- Uses native automation library directly
- No dependencies on other platform packages

### 4. Is/Wait/Assert Pattern

Every state-based capability follows this pattern:

| Method | Behavior | Returns | Nullable Skip |
|--------|----------|---------|---------------|
| `Is*()` | Immediate check, no waiting | `bool` or `bool?` | N/A |
| `Wait*(expected, timeout)` | Polls until condition or timeout | `bool` | Yes (null = skip) |
| `Assert*(expected, message, timeout)` | Waits, throws if not met | `TScope` | Yes (null = skip) |

### 5. Container Scoping

Containers provide hierarchical element scoping with parent navigation:

```csharp
// Navigate down into container, then back up to page
Page.GetContact(0).NameLabel.AssertText("Alice")
    .CallButton.Click()
    .Parent               // Navigate back to ContainerDemoPage
    .StatusLabel.AssertText("Calling...");

// Container defines scoped controls
public class ContactContainer : MauiContainerBase<ContainerDemoPage, ContactContainer>
{
    // Controls search within container root, not global page
    public MauiButtonControl<ContactContainer> CallButton => Button("CallButton");
}
```

## Module Boundaries

### Dependency Direction

```
Application Tests
       ↓
Platform Packages (Brinell.Maui, Brinell.Wpf, Brinell.Html, Brinell.Blazor, etc.)
       ↓
Brinell.Core (interfaces only)
       ↓
External Libraries (Appium, FlaUI, Playwright)
```

### Allowed Dependencies

| Package | May Depend On |
|---------|--------------|
| `Brinell.Core` | System libraries, no external dependencies |
| `Brinell.Maui` | Brinell.Core, Appium.WebDriver |
| `Brinell.Wpf` | Brinell.Core, FlaUI.Core, FlaUI.UIA3 |
| `Brinell.WinForms` | Brinell.Core, FlaUI.Core, FlaUI.UIA3 |
| `Brinell.Html` | Brinell.Core, Microsoft.Playwright |
| `Brinell.Blazor` | Brinell.Core, Microsoft.Playwright |
| `Brinell.Stride` | Brinell.Core, Stride.Engine |
| `Brinell.Mocking` | WireMock.Net |
| `Brinell.Automation` | Stride.Engine (for in-game automation) |
| Test Projects | Any Brinell package, xUnit, FluentAssertions |

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
/// // Example usage - fluent chaining
/// Page.NameEntry.Clear()
///     .NameEntry.Enter("Alice")
///     .GreetButton.Click()
///     .GreetingLabel.AssertText("Hello, Alice!");
/// </code>
/// </example>
```

**Document Version:** 2.0  
**Created:** January 13, 2026  
**Updated:** January 19, 2026  
**Workflow:** steering_workflow/structure
