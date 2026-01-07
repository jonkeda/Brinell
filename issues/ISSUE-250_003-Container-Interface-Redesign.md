# ISSUE: 250_003 Container Interface Redesign

**Date:** January 7, 2026
**Status:** Completed
**Affected Specs:** 250_003, 250_003a, 250_003b, 250_005

---

## Problem Statement

The container interfaces were incorrectly designed:

1. **IContainerControlObject** - Should be generic `IContainerControlObject<T>` with a `Child` property returning the typed content, similar to how `PageObject` works with control properties
2. **IContainerControl** - Was incorrectly inheriting from `IPageObject` instead of `IControlObject`

---

## Design Changes

### IContainerControlObject<T>

**Before:**
```csharp
public interface IContainerControlObject : IControlObject
{
    T FindChild<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
    bool ChildExists(Locator locator, int? timeoutMs = null);
    bool WaitChildExists(Locator locator, bool? expected, int? timeoutMs = null);
}
```

**After:**
```csharp
public interface IContainerControlObject<T> : IControlObject where T : IControlObject
{
    /// <summary>
    /// The child content control within this container.
    /// </summary>
    T Child { get; }
}
```

**Rationale:**
- Works like `IPageObject` where controls are defined as properties
- `ContainerControlBase<T>` will have `Child` as a property that test writers define
- Simplifies the API - no dynamic `FindChild<T>()` method needed
- Type-safe at compile time

### IContainerControl

**Before:**
```csharp
public interface IContainerControl : IPageObject
{
    T FindControl<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
    // ... other methods
}
```

**After:**
```csharp
public interface IContainerControl : IControlObject
{
    // Container-level scoping methods
}
```

**Rationale:**
- Containers are controls, not pages
- Inheriting from `IControlObject` gives access to standard control behavior (IsExists, IsVisible, etc.)
- Container scoping is about limiting search scope, not about being a page

### IListContainerControlObject<T>

**Before:**
```csharp
public interface IListContainerControlObject : IContainerControlObject
{
    IReadOnlyList<T> FindChildren<T>(Locator? locator = null, int? timeoutMs = null);
    T GetChildAt<T>(int index, int? timeoutMs = null);
    int GetChildCount(Locator? locator = null, int? timeoutMs = null);
}
```

**After:**
```csharp
public interface IListContainerControlObject<T> : IControlObject where T : IControlObject
{
    IReadOnlyList<T> Children { get; }
    T this[int index] { get; }
    int Count { get; }
    T? FirstOrDefault(Func<T, bool> predicate);
}
```

**Rationale:**
- Typed `Children` property like `Child` in single container
- Indexer provides direct typed access
- `Count` property is simpler than `GetChildCount()` method
- LINQ-friendly with typed collection

---

## Implementation Summary

### 250_003_IContainerControlObject.spx.md ✅

Updated to v1.1 with generic interface:

```csharp
public interface IContainerControlObject<T> : IControlObject where T : IControlObject
{
    T Child { get; }
}
```

### 250_003a_IContainerControl.spx.md ✅

Updated to v1.1, now inherits from `IControlObject`:

```csharp
public interface IContainerControl : IControlObject
{
    T FindControl<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
    IReadOnlyList<T> FindControls<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
    bool ControlExists(Locator locator, int? timeoutMs = null);
    bool WaitControlExists(Locator locator, bool? expected, int? timeoutMs = null);
    int GetControlCount(Locator locator, int? timeoutMs = null);
}
```

### 250_003b_IListContainerControlObject.spx.md ✅

Updated to v1.1 with generic interface:

```csharp
public interface IListContainerControlObject<T> : IControlObject where T : IControlObject
{
    IReadOnlyList<T> Children { get; }
    T this[int index] { get; }
    int Count { get; }
    T? FirstOrDefault(Func<T, bool> predicate);
    bool WaitCount(int? expected, int? timeoutMs = null);
    void AssertCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## Hierarchy Summary

```
IControlObject
    ├── IContainerControlObject<T>      (single typed child via Child property)
    ├── IListContainerControlObject<T>  (multiple typed children via Children property)
    └── IContainerControl               (dynamic scoped control finding)
```

---

## Files Updated

| File | Change | Status |
|------|--------|--------|
| 250_003_IContainerControlObject.spx.md | Made generic with `Child` property | ✅ Done |
| 250_003a_IContainerControl.spx.md | Changed to inherit from `IControlObject` | ✅ Done |
| 250_003b_IListContainerControlObject.spx.md | Made generic with `Children` property | ✅ Done |
| 250_005_InterfaceHierarchy.spx.md | Update hierarchy diagram | ⏳ Pending |

---

## Validation

- [x] Generic interfaces designed correctly
- [x] Usage examples are clear and practical
- [ ] Hierarchy diagram reflects new design
- [x] Related documents updated

---

**Created:** January 7, 2026
**Completed:** January 7, 2026
