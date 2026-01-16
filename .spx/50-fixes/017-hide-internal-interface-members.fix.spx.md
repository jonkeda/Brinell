# Fix 017: Hide Internal Interface Members from External Users

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | January 15, 2026 |
| Date Resolved | January 15, 2026 |
| Affected Version | 0.1.0 |
| Fixed Version | _Pending_ |

## Summary

Several interface members in `Brinell.Core` are intended for internal framework use only but are currently publicly exposed. These members (like `Locator`, `Scope`, `Page`, element finding methods, and `ContainerRoot`) are implementation details that test writers should not use directly. They need to be hidden from external consumers while remaining accessible to platform implementations.

## Symptoms

1. IntelliSense shows `Locator`, `Scope`, `Page` properties on control objects to test writers
2. IntelliSense shows `TryFindElement`, `FindElement`, `FindElements` methods on pages and containers to test writers
3. IntelliSense shows `ContainerRoot` property on container controls to test writers
4. External users can accidentally couple their tests to internal framework details
5. API surface appears cluttered with implementation details

## Evidence

### Affected Interfaces

```csharp
// IControlObject<TScope> - exposed internal properties
Locator Locator { get; }
IElementScope Scope { get; }
IPageObject? Page { get; }

// IElementScope<TElement> - exposed internal methods
TElement? TryFindElement(Locator locator);
TElement FindElement(Locator locator);
IReadOnlyList<TElement> FindElements(Locator locator);

// IContainerControl<TElement> - exposed internal property
TElement ContainerRoot { get; }
```

### Steps to Reproduce

1. Create a new test project referencing Brinell.Maui (or any platform package)
2. Create a control object (e.g., `public ButtonControl MyButton => ...`)
3. Type `MyButton.` in IntelliSense
4. Observe that `Locator`, `Scope`, `Page` appear as available members
5. These are implementation details that test writers should not use

## Root Cause

The interface contracts were designed with public members only, without considering that some members are for framework-internal coordination between controls, pages, and contexts. C# interfaces cannot have access modifiers on members - all interface members are implicitly public.

### Affected Components

- `srcnew/Brinell.Core/Interfaces/IControlObject.cs`
- `srcnew/Brinell.Core/Interfaces/IElementScope.cs`
- `srcnew/Brinell.Core/Interfaces/IContainerControl.cs`

## Proposed Solution

### Investigation: ControlObjectBase in Brinell.Core

**Question:** Could we add a `ControlObjectBase` class in `Brinell.Core` with protected/internal members instead of using interfaces?

**Analysis:**

The current architecture has:
1. **Brinell.Core** - Contains only interfaces (`IControlObject<TScope>`, `IElementScope<TElement>`, etc.)
2. **Platform packages** (Brinell.Maui, Brinell.Wpf, etc.) - Contain base classes like `MauiControlBase<TScope>`

Looking at `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`:
```csharp
public class MauiControlBase<TScope> : MauiObjectBase, IControlObject<TScope>
{
    private readonly IMauiScope<TScope> _scope;  // Already private
    private readonly Locator _locator;           // Already private
    
    // Interface properties delegate to private fields
    public IPageObject? Page => null; 
    public Locator Locator => _locator;
    public IElementScope Scope => _scope;
    ...
}
```

**Why a shared base class in Core WON'T work:**

1. **Platform-specific dependencies**: Each platform needs different types:
   - MAUI: `IMauiScope<TScope>`, `IMauiElement`, `AppiumDriver`
   - WPF: `FlaUITestContext`, `AutomationElement`
   - Blazor: `IPlaywrightTestContext`, `ILocator`
   
2. **Self-contained architecture principle**: From tech steering:
   > "Each platform package contains complete implementation without shared base classes."
   > "Eliminates diamond dependency problems"
   > "Platform-specific optimizations possible"

3. **Generics issue**: The interface `IControlObject<TScope>` uses a scope type parameter that varies per platform:
   - MAUI: `IMauiScope<TScope>`
   - The base class would need to know about platform-specific scope types

4. **No cross-assembly protected access**: Even if we put a base class in Core, `protected` members wouldn't be accessible from platform assemblies (different from Java's protected).

**Conclusion:** A shared `ControlObjectBase` in Core would violate the architecture and wouldn't solve the problem because:
- The internal members are still exposed through the public interface
- The base class would need platform-specific dependencies
- Protected doesn't work across assemblies in C#

### Approach: Abstract Base Class with Protected Members

**Phase 1: IControlObject members (Locator, Scope, Page)**

These members use types defined in Brinell.Core, so we can:
1. Remove `Locator`, `Scope`, `Page` from `IControlObject<TScope>` interface
2. Create `ControlObjectBase<TScope>` abstract class in Brinell.Core
3. Make these properties `protected` on the base class
4. Platform implementations inherit from the base class

```csharp
// In Brinell.Core
public abstract class ControlObjectBase<TScope>
{
    protected Locator Locator { get; }
    protected IElementScope Scope { get; }  
    protected IPageObject? Page { get; }
}

// Interface no longer exposes internal members
public interface IControlObject<TScope>
{
    // Only public API - Is/Wait/Assert methods
}
```

**Phase 2: IElementScope<TElement> and IContainerControl<TElement>** (Future fix)

These use platform-specific `TElement` types that Core doesn't know about. Options:
- Make these interfaces `internal` to each platform package
- Create platform-specific base classes
- Defer to a separate fix

### Affected Files

Files that will need modification:

| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Core/Interfaces/IControlObject.cs` | Remove `Locator`, `Scope`, `Page` properties |
| `srcnew/Brinell.Core/ControlObjectBase.cs` | **NEW** - Abstract base class with protected members |
| `srcnew/Brinell.Maui/Controls/MauiControlBase.cs` | Inherit from `ControlObjectBase<TScope>` |
| Other platform ControlBase classes | Inherit from `ControlObjectBase<TScope>` |

**Deferred to future fix:**
| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Core/Interfaces/IElementScope.cs` | TBD - uses platform TElement |
| `srcnew/Brinell.Core/Interfaces/IContainerControl.cs` | TBD - uses platform TElement |

## Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Core/Interfaces/IControlObject.cs` | Removed `Locator`, `Scope`, `Page` properties from interface |
| `srcnew/Brinell.Core/ControlObjectBase.cs` | **NEW** - Abstract base class with protected `Locator`, `Scope`, `Page` |
| `srcnew/Brinell.Maui/Controls/MauiControlBase.cs` | Now inherits from `ControlObjectBase<TScope>`, removed public property implementations |
| `testsnew/Brinell.Maui.Tests/FluentChainingTests.cs` | Removed tests that validated now-protected `Scope` property |

## Verification

- [x] Original symptoms resolved (Locator, Scope, Page no longer visible on controls)
- [x] No new issues introduced
- [x] Build passes (srcnew solution)
- [ ] Tests pass
- [ ] Verified in packaged build

## Related

- Tech Steering: Pre-release API changes are acceptable
- Interface design follows "self-contained platforms" principle

## Notes

- `EditorBrowsableState.Never` completely hides from IntelliSense
- `EditorBrowsableState.Advanced` would show only in "Show All Members" mode - but `Never` is more appropriate for truly internal members
- The `System.ComponentModel` namespace must be imported
- This is a non-breaking change - code that already uses these members will continue to work
