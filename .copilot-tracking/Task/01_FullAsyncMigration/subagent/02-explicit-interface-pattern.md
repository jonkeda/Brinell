---
title: "Explicit Interface Implementation for Sync/Async Naming Conflicts"
description: "Research findings on using C# explicit interface implementation to resolve sync and async method naming conflicts in Brinell.Html"
ms.date: 2026-02-24
ms.topic: reference
---

## Scope

This document analyzes whether C# explicit interface implementation can host sync and async methods with identical names on the same class, and evaluates the ergonomics of each approach for Brinell test authors.

## Current Codebase Structure

### Core Interfaces (Brinell.Core)

All sync, all return `TScope` for fluent chaining:

```text
IControlObject<TScope>
  bool IsExists()
  bool? IsVisible()
  bool? IsEnabled()
  bool WaitExists(bool?, int?)
  TScope AssertExists(bool?, string?, int?)
  string? GetText(int?)
  TScope AssertText(string?, string?, int?)
  ...

IClickableControlObject<TScope> : IControlObject<TScope>
  bool? IsClickable()
  TScope Click(int?)
  TScope DoubleClick(int?)
  TScope RightClick(int?)
  TScope Hover(int?)
  ...

IToggleControlObject<TScope> : IControlObject<TScope>
  bool? IsChecked()
  TScope Toggle(int?)
  TScope SetChecked(bool?, int?)
  TScope Check(int?)
  TScope Uncheck(int?)
  ...
```

### HTML Implementation Hierarchy (Brinell.Html)

```text
ObjectBase                          (Poll, DefaultTimeoutMs)
  └── ControlBase<TScope>           : IControlObject<TScope>
        └── Control<TScope>          (Click, SendKeys, Clear, ScrollIntoView)
              └── ClickableControlBase<TScope>  (DoubleClick, RightClick, Hover)
                    └── ToggleControlBase<TScope>  (IsChecked, SetChecked, etc.)
```

Critical observation: HTML control classes do NOT implement `IClickableControlObject<TScope>` or `IToggleControlObject<TScope>`. Signatures also differ in subtle ways:

| Core Interface | HTML Implementation | Difference |
|----------------|---------------------|------------|
| `TScope Click(int? timeoutMs = null)` | `TScope Click()` (no param) | Parameter mismatch |
| `bool? IsChecked()` | `bool IsChecked()` | Nullable vs non-nullable return |
| `TScope SetChecked(bool?, int?)` | `TScope SetChecked(bool value)` | Param count, nullable |

This means the HTML layer has its own "API shape" that mirrors Core but is not interface-bound to it.

## C# Explicit Interface Implementation Analysis

### The Question

Can a single class implement both `IControlObject<TScope>` (sync) and a proposed `IHtmlAsyncControlObject<TScope>` (async) when both define methods with the same name and parameters but different return types?

### The Answer: Yes

C# explicit interface implementation resolves methods that differ only by return type. The CLR supports return type differentiation in method signatures at the IL level; the C# language restriction on overloading by return type applies only to regular class members. Explicit interface implementations bypass this restriction because each is qualified by its interface name.

### Compilable Proof

```csharp
// Sync interface (existing)
public interface ISyncControl<TScope>
{
    bool IsExists();
    TScope Click(int? timeoutMs = null);
}

// Async interface (new, same method names)
public interface IAsyncControl<TScope>
{
    Task<bool> IsExists();
    Task<TScope> Click(int? timeoutMs = null);
}

// Implementation: one implicit, one explicit
public class ButtonControl<TScope> : ISyncControl<TScope>, IAsyncControl<TScope>
{
    // Implicit implementation — serves ISyncControl and class-level access
    public bool IsExists()
    {
        return true; // sync element lookup
    }

    public TScope Click(int? timeoutMs = null)
    {
        // sync click
        return default!;
    }

    // Explicit implementation — serves IAsyncControl only
    async Task<bool> IAsyncControl<TScope>.IsExists()
    {
        return await Task.FromResult(true); // async element lookup
    }

    async Task<TScope> IAsyncControl<TScope>.Click(int? timeoutMs = null)
    {
        await Task.Delay(0); // real async Playwright call
        return default!;
    }
}
```

This compiles without error. The rules:

1. The implicit (non-explicit) members satisfy `ISyncControl<TScope>` and are accessible from the concrete type.
2. The explicit members satisfy `IAsyncControl<TScope>` and are accessible only through that interface reference.
3. No ambiguity exists because the compiler resolves each call through the reference type.

### Access Patterns

```csharp
var button = new ButtonControl<MyPage>();

// Sync — works directly on the concrete type
button.Click();             // calls implicit TScope Click(int?)
button.IsExists();          // calls implicit bool IsExists()

// Async — requires interface reference
IAsyncControl<MyPage> asyncRef = button;
await asyncRef.Click();     // calls explicit Task<TScope> Click(int?)
await asyncRef.IsExists();  // calls explicit Task<bool> IsExists()
```

### Pitfalls

| Pitfall | Severity | Mitigation |
|---------|----------|------------|
| Explicit members invisible on concrete type | Medium | Extension methods bridge the gap (see below) |
| Cast required to reach async methods | Medium | Property or extension method provides typed access |
| Generic TScope constraints must match both interfaces | Low | Use same `where TScope : IHtmlScope<TScope>` on both |
| IDE IntelliSense hides explicit members | Medium | Extension methods restore discoverability |
| `await button.Click()` won't compile from concrete ref | Expected | By design; prevents accidental sync/async confusion |

## Extension Method Bridge Pattern

The most ergonomic approach combines explicit interface implementation with extension methods that add an `Async` suffix:

```csharp
public static class HtmlAsyncExtensions
{
    public static Task<TScope> ClickAsync<TScope>(
        this IAsyncControl<TScope> control, int? timeoutMs = null)
        => control.Click(timeoutMs);

    public static Task<bool> IsExistsAsync<TScope>(
        this IAsyncControl<TScope> control)
        => control.IsExists();

    public static Task<TScope> AssertExistsAsync<TScope>(
        this IAsyncControl<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        => control.AssertExists(expected, message, timeoutMs);
}
```

This lets test authors write:

```csharp
// Sync (unchanged)
page.Button.Click();

// Async — extension method, no cast needed
await page.Button.ClickAsync();
await page.Button.IsExistsAsync();
```

The extension methods resolve because the compiler sees that `ButtonControl<TScope>` implements `IAsyncControl<TScope>`. No cast required. Full IntelliSense. Both patterns coexist cleanly.

## Approach Comparison

### Approach A: Same Names + Explicit Implementation (Recommended)

```csharp
// Interface
public interface IHtmlAsyncClickable<TScope>
{
    Task<TScope> Click(int? timeoutMs = null);
}

// Implementation
public class Button<TScope> : IHtmlAsyncClickable<TScope>
{
    public TScope Click(int? timeoutMs = null) { ... }                          // sync
    async Task<TScope> IHtmlAsyncClickable<TScope>.Click(int? timeoutMs = null) { ... }  // async
}

// Test consumption
page.Button.Click();              // sync
await page.Button.ClickAsync();   // async via extension method
```

| Pro | Con |
|-----|-----|
| Interface methods match 1:1 with sync counterparts | Extension methods required for ergonomic access |
| Clean interface contract | Explicit members hidden on concrete type |
| Enables gradual migration | Two implementations per method to maintain |

### Approach B: Async Suffix on Interface Methods

```csharp
// Interface
public interface IHtmlAsyncClickable<TScope>
{
    Task<TScope> ClickAsync(int? timeoutMs = null);
}

// Implementation
public class Button<TScope> : IHtmlAsyncClickable<TScope>
{
    public TScope Click(int? timeoutMs = null) { ... }
    public async Task<TScope> ClickAsync(int? timeoutMs = null) { ... }
}

// Test consumption
page.Button.Click();
await page.Button.ClickAsync();
```

| Pro | Con |
|-----|-----|
| No explicit implementation needed | Async suffix on interface methods diverges from sync interface |
| Both methods visible on concrete type | Pollutes IntelliSense with doubled method count |
| Follows .NET naming conventions | Cannot share interface name/shape with sync version |
| No extension methods required | |

### Approach C: CancellationToken Disambiguation

```csharp
// Same interface name, different param list
public interface IHtmlAsyncClickable<TScope>
{
    Task<TScope> Click(CancellationToken ct, int? timeoutMs = null);
}

// Test consumption
page.Button.Click();                                  // sync
await page.Button.Click(CancellationToken.None);      // async
```

| Pro | Con |
|-----|-----|
| Single method name | Forced `CancellationToken.None` at every call site |
| Technically a different overload | Confusing which overload is sync vs async |
| | Test code becomes noisier |

### Approach D: Wrapper Property (page.Button.Async.Click())

```csharp
// Wrapper class
public class AsyncWrapper<TControl, TScope> where TControl : IAsyncControl<TScope>
{
    private readonly TControl _inner;
    public Task<TScope> Click(int? timeoutMs = null) => ((IAsyncControl<TScope>)_inner).Click(timeoutMs);
}

// Test consumption
page.Button.Click();              // sync
await page.Button.Async.Click();  // async
```

| Pro | Con |
|-----|-----|
| Clear namespace separation | Extra wrapper class per control type (or a generic one) |
| Discoverable via property | `Async` property on every control |
| | Breaks fluent chaining (Async returns wrapper, not scope) |

## Recommendation

**Approach A (explicit implementation) + extension method bridge** is the strongest option for Brinell because:

1. The async interfaces mirror the sync interfaces exactly (same method names, same parameters), which keeps the framework's contract surface clean and symmetric.
2. Extension methods restore full IntelliSense and eliminate casts at test call sites.
3. Test consumption is clean: `page.Button.Click()` for sync, `await page.Button.ClickAsync()` for async.
4. Approach B is a close second and simpler to implement. If the team prefers standard .NET `Async` suffix conventions over interface symmetry, Approach B avoids all explicit implementation complexity.

**Approach B** is the pragmatic fallback if explicit implementation maintenance burden is a concern. It trades interface elegance for implementation simplicity.

Approaches C and D are not recommended due to poor ergonomics.

## Concrete Brinell Implementation Sketch

### New async interfaces (Brinell.Html)

```csharp
namespace Brinell.Html.Interfaces;

public interface IHtmlAsyncControlObject<TScope>
{
    Task<bool> IsExists();
    Task<bool?> IsVisible();
    Task<bool?> IsEnabled();
    Task<bool> WaitExists(bool? expected, int? timeoutMs = null);
    Task<bool> WaitVisible(bool? expected, int? timeoutMs = null);
    Task<TScope> AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    Task<TScope> AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    Task<string?> GetText(int? timeoutMs = null);
    Task<TScope> AssertText(string? expected, string? message = null, int? timeoutMs = null);
}

public interface IHtmlAsyncClickable<TScope> : IHtmlAsyncControlObject<TScope>
{
    Task<TScope> Click(int? timeoutMs = null);
    Task<TScope> DoubleClick(int? timeoutMs = null);
    Task<TScope> Hover(int? timeoutMs = null);
}

public interface IHtmlAsyncToggle<TScope> : IHtmlAsyncClickable<TScope>
{
    Task<bool?> IsChecked();
    Task<TScope> Toggle(int? timeoutMs = null);
    Task<TScope> SetChecked(bool? @checked, int? timeoutMs = null);
}
```

### Implementation in ControlBase

```csharp
public abstract class ControlBase<TScope> : ObjectBase,
    IControlObject<TScope>,
    IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    // --- Sync (implicit, unchanged) ---
    public bool IsExists() => TryFindElement() != null;

    public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // existing sync implementation
    }

    // --- Async (explicit) ---
    async Task<bool> IHtmlAsyncControlObject<TScope>.IsExists()
    {
        return await Task.Run(() => TryFindElement() != null);
        // In Playwright: return await _page.Locator(selector).IsVisibleAsync();
    }

    async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertExists(
        bool? expected, string? message, int? timeoutMs)
    {
        // Playwright-native async assertion
        await Task.CompletedTask; // placeholder
        return ContainingScope;
    }
}
```

### Extension methods for ergonomic access

```csharp
namespace Brinell.Html;

public static class HtmlAsyncExtensions
{
    public static Task<bool> IsExistsAsync<TScope>(
        this IHtmlAsyncControlObject<TScope> control)
        => control.IsExists();

    public static Task<TScope> AssertExistsAsync<TScope>(
        this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        => control.AssertExists(expected, message, timeoutMs);

    public static Task<TScope> ClickAsync<TScope>(
        this IHtmlAsyncClickable<TScope> control, int? timeoutMs = null)
        => control.Click(timeoutMs);

    public static Task<TScope> ToggleAsync<TScope>(
        this IHtmlAsyncToggle<TScope> control, int? timeoutMs = null)
        => control.Toggle(timeoutMs);
}
```

### Test consumption

```csharp
public class LoginPage : HtmlPageObjectBase<LoginPage>
{
    public ButtonControl<LoginPage> SubmitButton { get; }
    public EntryControl<LoginPage> UsernameField { get; }
}

// Sync test (unchanged)
[Fact]
public void Login_SyncPath()
{
    page.UsernameField.Enter("admin");
    page.SubmitButton.Click();
    page.AssertVisible(true);
}

// Async test (new)
[Fact]
public async Task Login_AsyncPath()
{
    await page.UsernameField.EnterAsync("admin");
    await page.SubmitButton.ClickAsync();
    await page.AssertVisibleAsync(true);
}

// Mixed (transition period)
[Fact]
public async Task Login_MixedPath()
{
    page.UsernameField.Enter("admin");       // sync — still works
    await page.SubmitButton.ClickAsync();    // async — uses Playwright native
    page.AssertVisible(true);                // sync — polling fallback
}
```

## Notable Codebase Finding

The HTML control hierarchy does NOT implement `IClickableControlObject<TScope>` or `IToggleControlObject<TScope>` from Core. The HTML classes provide their own method shapes that parallel but differ from the Core interfaces:

| Item | Implication |
|------|-------------|
| `Control<T>.Click()` takes no params | New async interface can define `Click(int?)` without conflicting with the parameterless version |
| `ToggleControlBase.IsChecked()` returns `bool` not `bool?` | Async interface should match the HTML signature, not Core |
| HTML controls only implement `IControlObject<T>` from Core | Async interfaces need to mirror the actual HTML method shapes |

This means: async interfaces should be designed to match the HTML layer's actual signatures, not the Core interface signatures. The naming conflict analysis above remains valid since `IControlObject<TScope>` IS implemented.

## Verification Checklist

| Item | Status |
|------|--------|
| Explicit interface implementation resolves return-type-only differences | Confirmed |
| Generic TScope works in both explicit and implicit members | Confirmed |
| Extension methods bridge explicit members to ergonomic `Async` suffix | Confirmed |
| No runtime dispatch ambiguity | Confirmed |
| IntelliSense shows both sync and async (via extensions) | Confirmed |
| HTML classes only implement `IControlObject<T>` from Core | Confirmed |
| HTML method signatures diverge from Core capability interfaces | Confirmed |
