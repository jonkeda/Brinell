# PLAN-SPEC-006c-POC: POC Update Based on Review Findings

**Version:** 1.0  
**Created:** January 4, 2026  
**Status:** Planned  
**Related:** [REVIEW-006-POC-ControlObject6-Answers](../Reviews/REVIEW-006-POC-ControlObject6-Answers.md)

---

## Overview

This plan addresses the findings from the POC review and updates the ControlObject6 implementation accordingly.

---

## Changes Summary

| # | Finding | Action | Priority |
|---|---------|--------|----------|
| 1 | No string constructor | Add string overloads | High |
| 2 | No virtual methods | Add base classes with virtual Click/Enter | High |
| 3 | No logging | Add Log() calls to all operations | High |
| 4 | Actions created each time | **Skip** - not caching | N/A |
| 5 | ControlFactory unnecessary | Remove factory, use `new` | Medium |

---

## Phase 1: Add String Constructors (1 hour)

### 1.1 MAUI Controls

Add string constructor overloads to all controls:

**ButtonControl.cs:**
```csharp
public ButtonControl(MauiTestContext context, string automationId, IPageObject? page = null)
    : this(context, By.AutomationId(automationId), page)
{
}
```

**EntryControl.cs:**
```csharp
public EntryControl(MauiTestContext context, string automationId, IPageObject? page = null)
    : this(context, By.AutomationId(automationId), page)
{
}
```

**ControlObjectBase.cs:**
```csharp
protected ControlObjectBase(MauiTestContext context, string automationId, IPageObject? page)
    : this(context, By.AutomationId(automationId), page)
{
}
```

### 1.2 Blazor Controls

Same pattern for async controls:

**ButtonControl.cs (Blazor):**
```csharp
public ButtonControl(BlazorTestContext context, string locator, IAsyncPageObject? page = null)
    : this(context, By.TestId(locator), page)
{
}
```

**InputControl.cs:**
```csharp
public InputControl(BlazorTestContext context, string locator, IAsyncPageObject? page = null)
    : this(context, By.TestId(locator), page)
{
}
```

---

## Phase 2: Add Base Classes with Virtual Methods (2 hours)

### 2.1 Create ClickableControlBase (MAUI)

**File:** `src/Brinell.Maui/ControlObject6/Controls/ClickableControlBase.cs`

```csharp
namespace Brinell.Maui.ControlObject6.Controls;

public abstract class ClickableControlBase : ControlObjectBase, IClickableControlObject
{
    protected ClickableControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ClickableControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    public virtual void Click(int? timeoutMs = null)
    {
        Log($"Click()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        element.Click();
    }

    public virtual void DoubleClick(int? timeoutMs = null)
    {
        Log($"DoubleClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        var actions = new Actions(Driver);
        actions.DoubleClick(element).Perform();
    }

    public virtual void RightClick(int? timeoutMs = null)
    {
        Log($"RightClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        var actions = new Actions(Driver);
        actions.ContextClick(element).Perform();
    }

    public virtual void Hover(int? timeoutMs = null)
    {
        Log($"Hover()");
        CheckVisible(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        var actions = new Actions(Driver);
        actions.MoveToElement(element).Perform();
    }

    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        Log($"LongPress(duration={durationMs ?? 1000}ms)");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        // Implementation...
    }
}
```

### 2.2 Create TextControlBase (MAUI)

**File:** `src/Brinell.Maui/ControlObject6/Controls/TextControlBase.cs`

```csharp
namespace Brinell.Maui.ControlObject6.Controls;

public abstract class TextControlBase : ClickableControlBase, ITextControlObject
{
    protected TextControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected TextControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    public virtual bool IsFocused() { ... }
    public virtual void Focus(int? timeoutMs = null) { ... }
    public virtual void Blur(int? timeoutMs = null) { ... }

    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"Enter(\"{text}\")");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        element.Clear();
        element.SendKeys(text);
    }

    public virtual void Clear(int? timeoutMs = null)
    {
        Log("Clear()");
        CheckVisible(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        element.Clear();
    }

    public virtual void ClearAndEnter(string? text, int? timeoutMs = null)
    {
        Clear(timeoutMs);
        if (text is not null)
        {
            Log($"ClearAndEnter(\"{text}\")");
            var element = FindElementRequired(timeoutMs);
            element.SendKeys(text);
        }
    }

    public virtual void Append(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"Append(\"{text}\")");
        CheckVisible(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        element.SendKeys(text);
    }

    public virtual bool IsReadOnly() { ... }
    public virtual int GetTextLength(int? timeoutMs = null) { ... }
}
```

### 2.3 Blazor Base Classes

Create async equivalents:
- `AsyncClickableControlBase.cs`
- `AsyncTextControlBase.cs`

---

## Phase 3: Add Logging (1.5 hours)

### 3.1 Add Log Method to ControlObjectBase

**ControlObjectBase.cs:**
```csharp
/// <summary>
/// Logs a message using the test context.
/// </summary>
protected void Log(string message)
{
    Context.Log($"[{GetType().Name}] {Locator}: {message}");
}
```

### 3.2 Add Logging to All Operations

**Example - Click:**
```csharp
public virtual void Click(int? timeoutMs = null)
{
    Log("Click()");
    CheckVisible(true, timeoutMs);
    CheckEnabled(true, timeoutMs);
    var element = FindElementRequired(timeoutMs);
    element.Click();
}
```

**Example - Enter:**
```csharp
public virtual void Enter(string? text, int? timeoutMs = null)
{
    if (text is null) return;
    Log($"Enter(\"{text}\")");
    CheckVisible(true, timeoutMs);
    CheckEnabled(true, timeoutMs);
    var element = FindElementRequired(timeoutMs);
    element.Clear();
    element.SendKeys(text);
}
```

### 3.3 Logging Locations

Add `Log()` calls to:

| Class | Methods |
|-------|---------|
| ControlObjectBase | IsExists, IsVisible, IsEnabled, GetText, WaitExists, WaitVisible, CheckExists, AssertExists |
| ClickableControlBase | Click, DoubleClick, RightClick, Hover, LongPress |
| TextControlBase | Enter, Clear, ClearAndEnter, Append, Focus, Blur |
| ButtonControl | (inherits from base) |
| EntryControl | (inherits from base) |

---

## Phase 4: Remove ControlFactory (1 hour)

### 4.1 Remove from MauiTestContext

**Before:**
```csharp
public class MauiTestContext : ITestContext
{
    private readonly ControlFactory _controlFactory;
    
    public T CreateControl<T>(ControlLocator locator) where T : IControlObject
    {
        return _controlFactory.Create<T>(locator, null);
    }
    
    internal class ControlFactory { ... }
}
```

**After:**
```csharp
public class MauiTestContext : ITestContext
{
    // Remove: _controlFactory field
    // Remove: CreateControl<T> method
    // Remove: ControlFactory class
}
```

### 4.2 Remove from BlazorTestContext

Same pattern - remove factory class and method.

### 4.3 Update ITestContext Interface

**Before:**
```csharp
public interface ITestContext
{
    T CreateControl<T>(ControlLocator locator) where T : IControlObject;
}
```

**After:**
```csharp
public interface ITestContext
{
    // Remove: CreateControl<T>
}
```

### 4.4 Update Documentation

Update test case documents to use `new` instead of factory:

**Before:**
```csharp
var button = Context.CreateControl<IClickableControlObject>(By.AutomationId("btn"));
```

**After:**
```csharp
var button = new ButtonControl(Context, "btn", this);
```

---

## Phase 5: Update PageObjectBase (30 minutes)

Ensure PageObjectBase uses `new` pattern:

```csharp
public abstract class PageObjectBase : IPageObject
{
    protected readonly MauiTestContext Context;
    
    protected PageObjectBase(MauiTestContext context)
    {
        Context = context;
    }
    
    // Helper for creating controls - PageObjects use 'new'
    protected ButtonControl Button(string automationId) 
        => new(Context, automationId, this);
    
    protected EntryControl Entry(string automationId) 
        => new(Context, automationId, this);
}
```

---

## Implementation Checklist

### Phase 1: String Constructors
- [ ] ControlObjectBase - add string constructor
- [ ] ButtonControl - add string constructor
- [ ] EntryControl - add string constructor
- [ ] AsyncControlObjectBase - add string constructor
- [ ] ButtonControl (Blazor) - add string constructor
- [ ] InputControl (Blazor) - add string constructor

### Phase 2: Virtual Base Classes
- [ ] Create ClickableControlBase.cs (MAUI)
- [ ] Create TextControlBase.cs (MAUI)
- [ ] Update ButtonControl to inherit ClickableControlBase
- [ ] Update EntryControl to inherit TextControlBase
- [ ] Create AsyncClickableControlBase.cs (Blazor)
- [ ] Create AsyncTextControlBase.cs (Blazor)
- [ ] Update Blazor ButtonControl to inherit base
- [ ] Update Blazor InputControl to inherit base

### Phase 3: Logging
- [ ] Add Log() method to ControlObjectBase
- [ ] Add Log() method to AsyncControlObjectBase
- [ ] Add logging to all MAUI control operations
- [ ] Add logging to all Blazor control operations

### Phase 4: Remove ControlFactory
- [ ] Remove from MauiTestContext
- [ ] Remove from BlazorTestContext
- [ ] Remove CreateControl from ITestContext interface
- [ ] Update documentation

### Phase 5: Build and Verify
- [ ] Build Brinell.Core
- [ ] Build Brinell.Maui
- [ ] Build Brinell.Blazor
- [ ] Verify no errors

---

## File Changes Summary

| File | Change |
|------|--------|
| `ControlObjectBase.cs` | Add string constructor, add Log() |
| `ButtonControl.cs` (MAUI) | Add string constructor, inherit ClickableControlBase |
| `EntryControl.cs` | Add string constructor, inherit TextControlBase |
| `ClickableControlBase.cs` | **NEW** - virtual Click methods |
| `TextControlBase.cs` | **NEW** - virtual Enter methods |
| `MauiTestContext.cs` | Remove ControlFactory |
| `AsyncControlObjectBase.cs` | Add string constructor, add Log() |
| `ButtonControl.cs` (Blazor) | Add string constructor |
| `InputControl.cs` | Add string constructor |
| `AsyncClickableControlBase.cs` | **NEW** - virtual async Click |
| `AsyncTextControlBase.cs` | **NEW** - virtual async Enter |
| `BlazorTestContext.cs` | Remove ControlFactory |
| `ITestContext.cs` | Remove CreateControl |
| `IAsyncTestContext.cs` | Remove CreateControl |

---

## Estimated Effort

| Phase | Hours |
|-------|-------|
| Phase 1: String Constructors | 1.0 |
| Phase 2: Virtual Base Classes | 2.0 |
| Phase 3: Logging | 1.5 |
| Phase 4: Remove Factory | 1.0 |
| Phase 5: Build & Verify | 0.5 |
| **Total** | **6.0** |

---

## Success Criteria

- [ ] All controls have string constructor overloads
- [ ] Click, DoubleClick, RightClick, Hover, LongPress are virtual
- [ ] Enter, Clear, ClearAndEnter, Append are virtual
- [ ] All operations log with `Log()`
- [ ] ControlFactory is removed
- [ ] All projects build successfully
- [ ] PageObjects use `new` pattern
