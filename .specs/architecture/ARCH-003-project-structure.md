# Project Structure & Conventions

**Version:** 1.0 | **Status:** Active | **Source:** SPX structure steering doc

## Directory Organization

```
Brinell/
├── .github/                    # GitHub-specific files
│   └── copilot-instructions.md # Copilot AI guidance
├── .specs/                     # Specification documents (this folder)
├── samples/                    # Example applications
│   ├── Brinell.Samples.Maui.App/
│   ├── Brinell.Samples.Blazor.App/
│   └── ...
├── srcnew/                     # Active source code
├── testsnew/                   # Active test projects
├── Directory.Build.props       # Shared MSBuild properties
├── Directory.Packages.props    # Central package management
├── Brinell.sln                 # Solution file
├── global.json                 # SDK version pinning
└── nuget.config                # NuGet configuration
```

## Source Code Structure (srcnew/)

```
srcnew/
├── Brinell.Core/                   # Core interfaces (no dependencies)
│   ├── Abstractions/Controls/      # Control base abstractions
│   ├── Attributes/                 # Test attributes
│   ├── Configuration/              # TimeoutSettings, etc.
│   ├── Exceptions/                 # Exception types
│   ├── Interfaces/                 # All core interfaces
│   ├── Locators/                   # Locator, LocatorStrategy
│   ├── Logging/                    # ITestLogger, LogResult
│   ├── Models/                     # Shared models
│   ├── Services/                   # Service interfaces
│   ├── Testing/                    # Test base classes
│   └── Utilities/                  # Common utilities
├── Brinell.Maui/                   # MAUI platform (active)
│   ├── Context/                    # IMauiTestContext, MauiTestContext
│   ├── Controls/                   # Control implementations
│   ├── Extensions/                 # Extension methods
│   ├── Gestures/                   # Mobile gesture support
│   ├── Interfaces/                 # MAUI-specific interfaces
│   ├── Pages/                      # MauiPageObjectBase<TSelf>
│   ├── Testing/                    # Test fixtures
│   └── Wrappers/                   # Element wrappers
├── Brinell.Maui.Appium/            # Appium driver for MAUI
├── Brinell.Maui.FlaUI/             # FlaUI driver for MAUI (desktop)
├── Brinell.Maui.CommunityToolkit/  # CommunityToolkit control support
├── Brinell.Blazor/                 # Blazor platform (scaffolded)
├── Brinell.Html/                   # HTML/Playwright platform (scaffolded)
├── Brinell.Wpf/                    # WPF platform (scaffolded)
├── Brinell.WinForms/               # WinForms platform (scaffolded)
├── Brinell.Stride/                 # Stride game engine (scaffolded)
├── Brinell.Automation/             # Stride automation host
├── Brinell.Mocking/                # API mocking (WireMock)
├── Directory.Build.props           # Shared build properties
└── Directory.Packages.props        # Central package versions
```

## Test Structure (testsnew/)

```
testsnew/
├── Brinell.Core.Tests/             # Core interface unit tests
├── Brinell.Maui.Tests/             # MAUI unit tests
├── Brinell.Maui.UITests/           # MAUI UI integration tests
│   ├── AppiumFixture.cs            # Test fixture
│   ├── AppiumCollection.cs         # xUnit collection
│   ├── TestConstants.cs            # Test configuration
│   ├── Pages/                      # Page objects for sample app
│   ├── Containers/                 # Container definitions
│   └── Tests/                      # Test classes
├── Brinell.Blazor.Tests/           # Blazor unit tests (placeholder)
├── Brinell.Wpf.Tests/              # WPF unit tests (placeholder)
├── Brinell.WinForms.Tests/         # WinForms unit tests (placeholder)
├── Brinell.Stride.Tests/           # Stride unit tests (placeholder)
└── Directory.Build.props           # Test-specific build props
```

## Naming Conventions

### Files

| Type | Convention | Example |
|------|------------|---------|
| Interfaces | `I` + PascalCase | `IControlObject.cs` |
| Classes | PascalCase | `ButtonControl.cs` |
| Base Classes | PascalCase + `Base` | `ControlObjectBase.cs` |
| Test Files | Class + `Tests` | `ControlObjectTests.cs` |
| Projects | `Brinell.` prefix | `Brinell.Core` |
| Test Projects | Project + `.Tests` | `Brinell.Core.Tests` |

### Code

| Type | Convention | Example |
|------|------------|---------|
| Namespaces | PascalCase, matches folder | `Brinell.Core.Interfaces` |
| Interfaces | `I` prefix | `IControlObject`, `IClickableControl` |
| Classes | PascalCase | `ButtonControl`, `MauiTestContext` |
| Methods | PascalCase | `IsExists()`, `WaitVisible()` |
| Properties | PascalCase | `AutomationId`, `Page` |
| Private Fields | `_` + camelCase | `_element`, `_context` |
| Parameters | camelCase | `timeoutMs`, `automationId` |
| Constants | PascalCase | `DefaultTimeout`, `MaxRetries` |

### Interface Naming

```csharp
IControlObject<TScope>              // Foundation for all controls
IClickableControlObject<TScope>     // Capability interface (noun-style)
IEditableTextControlObject<TScope>  // Extended capability
IElementScope<TElement>             // Scoping interface
IContainerControl<TElement>         // Container with scoped search
```

### Control Class Naming

```csharp
MauiControlBase<TScope>             // Platform prefix + Base
MauiButtonControl<TScope>           // Platform prefix + control name
MauiContainerBase<TParent, TSelf>   // Container with two type params
MauiPageObjectBase<TSelf>           // CRTP for fluent returns
```

## Code Structure Patterns

### Interface Definition

```csharp
public interface IControlName<TScope>
{
    bool IsState();                                              // Immediate
    bool WaitState(bool? expected, int? timeoutMs = null);       // Poll
    TScope AssertState(bool? expected, string? msg, int? t);     // Assert+fluent
    TScope DoAction(int? timeoutMs = null);                      // Action+fluent
}
```

### Control Implementation

```csharp
public class MauiControlName<TScope> : MauiControlBase<TScope>, IControlInterface<TScope>
    where TScope : IMauiScope<TScope>
{
    public MauiControlName(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator) { }

    public TScope DoAction(int? timeoutMs = null)
        => RunWithElement(nameof(DoAction), timeoutMs, element => element.Click());
}
```

### Page Object

```csharp
public class MainPage : MauiPageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context) : base(context) { }
    public override string Name => "MainPage";
    public override bool IsLoaded(int? t = null) => TitleLabel.IsExists();

    public MauiControlBase<MainPage> TitleLabel => Control("TitleLabel");
    public MauiButtonControl<MainPage> GreetButton => Button("GreetButton");
    public MauiEntryControl<MainPage> NameEntry => Entry("NameEntry");
}
```

### Container

```csharp
public class ContactContainer : MauiContainerBase<ContainerDemoPage, ContactContainer>
{
    public ContactContainer(IMauiScope<ContainerDemoPage> parentScope, int index)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, $"Contact_{index}")) { }

    public MauiControlBase<ContactContainer> NameLabel => new(this, "ContactName");
    public MauiButtonControl<ContactContainer> CallButton => Button("ContactCallButton");
}
```

### Test Class

```csharp
[Collection("Appium")]
[Trait("Category", "UITest")]
public class MainPageTests
{
    private readonly AppiumFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public MainPageTests(AppiumFixture fixture) => _fixture = fixture;

    [Fact]
    public void MainPage_EnterNameAndGreet_ShowsGreeting()
    {
        Page.NameEntry.Clear()
            .NameEntry.Enter("Bob")
            .GreetButton.Click()
            .GreetingLabel.AssertText("Hello, Bob!");
    }
}
```

## Module Boundaries

### Dependency Rules

```
Test Projects → Platform Packages → Brinell.Core → System libraries only
                     ↓
              External Libraries (Appium, FlaUI, Playwright)
```

| Package | Allowed Dependencies |
|---------|---------------------|
| `Brinell.Core` | System libraries only |
| `Brinell.Maui` | Core + Appium.WebDriver |
| `Brinell.Wpf` | Core + FlaUI.Core + FlaUI.UIA3 |
| `Brinell.Html` | Core + Microsoft.Playwright |

**Forbidden:** Platform packages must not depend on other platform packages. Core must not reference any automation library.

## Code Size Guidelines

| Type | Max Lines |
|------|-----------|
| Interface files | ~200 |
| Control implementations | ~500 |
| Test files | ~400 |
| Base classes | ~600 |
| Public methods | ~30 |
| Test methods | ~20 |

Prefer method extraction over complex conditionals. Maximum nesting depth: 3 levels.

## Import Order

```csharp
using System;                           // 1. System
using OpenQA.Selenium;                  // 2. External packages
using Brinell.Core.Interfaces;          // 3. Brinell.Core
using Brinell.Maui.Controls;            // 4. Platform-specific
```
