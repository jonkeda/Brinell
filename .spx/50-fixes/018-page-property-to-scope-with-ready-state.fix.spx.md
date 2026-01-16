# x`Fix 018: Move Page Property to IElementScope and Add Ready State

| Field            | Value            |
| ---------------- | ---------------- |
| Status           | Resolved         |
| Date Created     | January 15, 2026 |
| Date Resolved    | January 15, 2026 |
| Affected Version | 0.1.0            |
| Fixed Version    | 0.1.0            |

## Summary

The `Page` property is currently on `ControlObjectBase<TScope>` but should be on `IElementScope` instead. This makes more semantic sense: the scope knows which page it belongs to (a page returns itself, a container returns its parent's page). Additionally, element finding should verify the scope is ready before attempting to find elements.

## Symptoms

1. Controls have a `Page` property via base class, but scopes (where the context belongs) don't expose it
2. No ready-state checking before element finding - can lead to flaky tests if page/container not ready
3. Container controls need to navigate to parent to find page, but this isn't part of the scope contract

## Evidence

### Current Structure

```csharp
// ControlObjectBase has Page but IElementScope doesn't
public abstract class ControlObjectBase<TScope>
{
    protected IPageObject? Page { get; }  // ← Here
}

public interface IElementScope
{
    LocatorStrategy DefaultLocatorStrategy { get; }
    // No Page property
    // No ready state checking
}
```

### Design Issue

- A `MauiPageObjectBase` IS its own page
- A `MauiContainerBase` should get `Page` from its parent scope hierarchy
- Controls shouldn't own the page reference - they should ask their scope

## Root Cause

The original design placed `Page` on controls for convenience, but the natural owner is the scope (page or container). The scope hierarchy naturally knows which page it belongs to.

### Affected Components

- `srcnew/Brinell.Core/Abstractions/Controls/ControlObjectBase.cs`
- `srcnew/Brinell.Core/Interfaces/IElementScope.cs`
- `srcnew/Brinell.Core/Interfaces/IPageObject.cs`
- `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`
- `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`
- `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`

## Proposed Solution

### Approach

**1. Add `Page` and ready-state to `IElementScope`:**

```csharp
public interface IElementScope
{
    LocatorStrategy DefaultLocatorStrategy { get; }
  
    /// <summary>
    /// The page containing this scope.
    /// For pages, returns self. For containers, returns parent's page.
    /// </summary>
    IPageObject? Page { get; }
  
    /// <summary>
    /// Check if the scope is ready for element finding.
    /// </summary>
    bool IsReady(int? timeoutMs = null);
  
    /// <summary>
    /// Wait until the scope is ready.
    /// </summary>
    bool WaitReady(int? timeoutMs = null);
}
```

**2. Remove `Page` from `ControlObjectBase`:**

```csharp
public abstract class ControlObjectBase<TScope>
{
    protected Locator Locator { get; }
    protected IElementScope Scope { get; }
    // Page removed - access via Scope.Page
}
```

**3. Implement in pages (return self):**

```csharp
public abstract class MauiPageObjectBase<TSelf> : ...
{
    public IPageObject Page => this;
  
    public bool IsReady(int? timeoutMs = null) => IsLoaded(timeoutMs);
    public bool WaitReady(int? timeoutMs = null) => WaitLoaded(true, timeoutMs);
}
```

**4. Implement in containers (delegate to parent):**

```csharp
public abstract class MauiContainerBase<TParent, TSelf> : ...
{
    // Get page from parent scope chain
    public IPageObject? Page => ((IElementScope)_parent).Page;
  
    public bool IsReady(int? timeoutMs = null)
    {
        // Check parent is ready AND container root exists
        var parentScope = (IElementScope)_parent;
        if (!parentScope.IsReady(timeoutMs)) return false;
        return TryGetContainerRoot() != null;
    }
  
    public bool WaitReady(int? timeoutMs = null)
    {
        var parentScope = (IElementScope)_parent;
        if (!parentScope.WaitReady(timeoutMs)) return false;
        return Poll(() => TryGetContainerRoot() != null, timeoutMs ?? DefaultTimeoutMs);
    }
}
```

**5. Element finding checks ready state:**

```csharp
public IMauiElement? TryFindElement(Locator locator)
{
    // Ensure scope is ready before finding
    if (!IsReady()) return null;
    // ... find element
}
```

### Affected Files

| File                                                               | Expected Change                                                                          |
| ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| `srcnew/Brinell.Core/Interfaces/IElementScope.cs`                | Add `Page`, `IsReady`, `WaitReady`                                                 |
| `srcnew/Brinell.Core/Abstractions/Controls/ControlObjectBase.cs` | Remove `Page` property and constructor parameter                                       |
| `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`                | Implement `Page` (return self), `IsReady`, `WaitReady`                             |
| `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`              | Implement `Page` (from parent), `IsReady`, `WaitReady`, check ready before finding |
| `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`                | Access `Page` via `Scope.Page`, update constructors                                  |

## Files Modified

| File                                                               | Change                                                                                   |
| ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| `srcnew/Brinell.Core/Interfaces/IElementScope.cs`                | Added `Page`, `IsReady(int?)`, `WaitReady(int?)` to interface                      |
| `srcnew/Brinell.Core/Interfaces/IPagedScope.cs`                  | Added `new` keyword to `Page` property to hide base                                  |
| `srcnew/Brinell.Core/Abstractions/Controls/ControlObjectBase.cs` | Removed `Page` parameter from constructor, `Page` now accesses `Scope.Page`        |
| `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`                | Implemented `Page => this`, `IsReady`, `WaitReady` delegating to load state        |
| `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`              | Implemented `Page` from parent chain, `IsReady`/`WaitReady` checking parent + root |
| `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`                | Updated constructors to use simplified base constructor                                  |
| `srcnew/Brinell.Maui/Context/MauiTestContext.cs`                 | Implemented `Page => null`, `IsReady`/`WaitReady` (always ready)                   |
| `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`               | Fixed pre-existing bug with `regex` variable scope                                     |

## Verification

- [X] Original symptoms resolved
- [X] No new issues introduced
- [X] Tests pass (14 total: 13 passed, 1 skipped)
- [X] Build succeeds

## Related

- [Fix 017: Hide Internal Interface Members](./017-hide-internal-interface-members.fix.spx.md) - Related architectural change

## Notes

- This change improves the semantic model: scopes own the page context, not controls
- Ready-state checking will reduce flaky tests by ensuring scopes are ready before element finding
- The `IPageObject` already has `IsLoaded`/`WaitLoaded` which can be reused for `IsReady`/`WaitReady`
