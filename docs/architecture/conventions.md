# Coding Conventions

**Analysis Date:** 2026-03-02

## Naming Patterns

**Files:**
- `PascalCase.cs` for all C# source files (matches the primary class/interface name)
- `GlobalUsings.cs` per project — project-wide global using directives
- Test files: `{ClassName}Tests.cs` for unit/integration tests, suffixed by feature area for UITests

**Types:**
- Interfaces: `I{Name}` — e.g., `IControlObject<TScope>`, `IMauiScope<TScope>`, `ITestLogger`
- Abstract base classes: `{Name}Base` — e.g., `ControlBase<TScope>`, `ContainerBase<TParent, TSelf>`, `MauiTestFixtureBase`
- Concrete controls: `{ControlName}<TScope>` — e.g., `Button<TScope>`, `Entry<TScope>`, `Switch<TScope>`
- Page objects: `{PageName}Page` — e.g., `MainPage`, `UserFormPage`, `AppShellPage`
- Exceptions: `{Description}Exception` — e.g., `ElementNotFoundException`, `AssertionException`
- Enums: `PascalCase` name + `PascalCase` values — e.g., `LocatorStrategy.AutomationId`, `MauiPlatform.Android`

**Methods:**
- State checks: `Is{State}()` — `IsExists()`, `IsVisible()`, `IsEnabled()`
- Wait operations: `Wait{State}(bool? expected, int? timeoutMs = null)` — `WaitExists`, `WaitVisible`, `WaitLoaded`
- Assertions: `Assert{State}(bool? expected, string? message = null, int? timeoutMs = null)` — `AssertExists`, `AssertText`
- Actions: verb-noun — `Click()`, `Enter(string text)`, `SetText(string text)`, `Clear()`
- Core methods: `{Action}Core()` (virtual, overridable) — `ClearCore()`, `SetTextCore()`, `ClickCore()`
- Factory methods on pages: control type name — `Button(locator)`, `Entry(locator)`, `Label(locator)`

**Variables/Fields:**
- Private fields: `_camelCase` — e.g., `_mauiScope`, `_context`, `_mockContext`
- Parameters: `camelCase` — e.g., `locator`, `timeoutMs`, `locatorValue`
- Local variables: `camelCase`
- Constants/static readonly: `PascalCase` (no UPPER_SNAKE) — e.g., `TimeoutSettings.Default`, `TimeoutSettings.Fast`

**Generic Type Parameters:**
- `TScope` — the fluent chain return type (containing page or container)
- `TSelf` — the CRTP self-reference (for page objects)
- `TElement` — the platform's native automation element type
- `TParent` — parent scope for container controls in double-generic variants

## Code Style

**Formatting:**
- `.editorconfig` at root — defines indentation, line endings, and style rules
- 4-space indentation (standard C# convention)
- No explicit Prettier/clang-format; relies on .editorconfig + Roslyn analyzers

**Compiler settings (Directory.Build.props):**
- `<LangVersion>latest</LangVersion>` — use current C# language features
- `<Nullable>enable</Nullable>` — full nullable reference type annotations required
- `<ImplicitUsings>enable</ImplicitUsings>` — System, LINQ, etc. auto-imported
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — zero warnings policy; all warnings are build failures

**XML Documentation:**
- Required on all public and protected members
- `<summary>`, `<typeparam>`, `<param>`, `<returns>`, `<exception>` used consistently
- Inherited docs via `<inheritdoc />` for interface implementations

## Import Organization

**Global usings (`GlobalUsings.cs` per project):**
- Core: `global using Brinell.Core.Interfaces; global using Brinell.Core.Locators;`
- Maui: adds `System.Diagnostics`, `Brinell.Core.*`, `Brinell.Maui.*`, `OpenQA.Selenium`, `OpenQA.Selenium.Appium`
- Pattern: frequently-used framework namespaces go in GlobalUsings; per-file usings for less common

**Per-file usings:**
- Order: framework namespaces (`Brinell.*`) first, then external packages (`OpenQA.Selenium.*`)
- No unused using directives (TreatWarningsAsErrors catches them)

## Error Handling

**Patterns:**
- Do not swallow exceptions — empty `catch { }` blocks are banned (documented in copilot-instructions.md)
- Only catch exceptions you can handle — use specific exception types (`WebDriverException`, `ElementNotFoundException`)
- Control flow: use null checks / `TryFind*` methods instead of try/catch for expected conditions
- Polling loop body: may catch `StaleElementReferenceException` and similar transient errors — must be documented with comment
- External driver errors: catch `WebDriverException` for transient operations (e.g., XPath fallback in DatePicker/TimePicker)

**Do:**
```csharp
// ✅ Catch specific, documented
var button = TryFindChildButton(element);
if (button != null)
{
    button.Click();
    return;
}
// Fall through to base implementation
```

**Don't:**
```csharp
// ❌ Empty catch — banned
try { button.Click(); }
catch { }
```

## Common Patterns

**Is/Wait/Assert triple — every state check:**
```csharp
bool IsExists();                                    // immediate, no wait
bool WaitExists(bool? expected, int? timeoutMs);    // poll until match or timeout
TScope AssertExists(bool? expected, string? msg);   // throws AssertionException on failure
```

**Fluent chaining with CRTP:**
```csharp
// On page objects:
public abstract class PageObjectBase<TSelf> : ObjectBase, IMauiPage<TSelf>
    where TSelf : PageObjectBase<TSelf>
{
    public TSelf Self => (TSelf)this;
}

// Controls return TScope (the page or container):
loginPage
    .Username.Enter("testuser")
    .Password.Enter("testpass")
    .LoginButton.Click();
```

**Null-skip semantics on Wait/Assert:**
```csharp
// If expected is null, method returns immediately (skip — useful for optional checks)
bool WaitExists(bool? expected, int? timeoutMs = null);
// Usage:
control.WaitExists(null); // always returns true — used for optional steps
control.WaitExists(true, 5000); // polls until element exists
```

**Control constructor — two overloads:**
```csharp
// Overload 1: explicit Locator
public Button(IMauiScope<TScope> scope, Locator locator) : base(scope, locator) {}

// Overload 2: string shorthand (uses scope's DefaultLocatorStrategy)
public Button(IMauiScope<TScope> scope, string locatorValue) : base(scope, locatorValue) {}
```

**Core method override pattern — virtual/override not new:**
```csharp
// Base: virtual core method
protected virtual string? GetTextCore(AppiumElement element) { ... }

// Derived: override, not new
protected override string? GetTextCore(AppiumElement element) { ... }
```

**No Thread.Sleep — poll instead:**
```csharp
// ✅ Always wait for a condition:
_element.WaitVisible(true, timeoutMs: 5000);

// ❌ Never:
Thread.Sleep(500); // banned
Task.Delay(500);   // banned
```

**TimeoutSettings with factory presets:**
```csharp
TimeoutSettings.Default  // 5s default, 10s page, 100ms polling
TimeoutSettings.Fast     // 2s default, 1s element, 50ms polling
TimeoutSettings.Slow     // 15s default, 30s page, 200ms polling
settings.With(defaultWait: 10000) // immutable copy with override
```

---

*Conventions analysis: 2026-03-02*
