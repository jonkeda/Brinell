# SPEC-015b: Element Lookup Optimization - Phase 2

**Status:** Draft  
**Created:** January 18, 2026  
**Parent:** SPEC-015 (Element Lookup Optimization)  
**Author:** Brinell Framework Team

---

## 1. Overview

This specification extends SPEC-015 to apply the element-passing optimization pattern to all remaining controls and methods in the Brinell framework.

### 1.1 SPEC-015 Recap

SPEC-015 established the pattern:
- **Public methods** use `RunWithElement()` to find element once and delegate to Core
- **Protected Core methods** accept pre-found element and perform the actual work
- **Result:** `Click()` reduced from ~53 FindElement calls to 1-3 calls (40% faster tests)

### 1.2 Scope of Phase 2

Apply the same pattern to:
1. `MauiEntryControl` - Enter, Clear, SetText, GetPlaceholder, IsReadOnly
2. `MauiFlyoutItemControl` - Click, DoubleClick, RightClick
3. `MauiControlBase` - SendKeys, GetText
4. `MauiListControl` - GetItemCount

---

## 2. Methods to Optimize

### 2.1 MauiEntryControl

| Method | Current FindElement Calls | After Optimization |
|--------|---------------------------|-------------------|
| `Enter()` | CheckEnabled(~20) + FindElement(1) = ~21 | 1 |
| `Clear()` | CheckEnabled(~20) + FindElement(1) = ~21 | 1 |
| `SetText()` | CheckEnabled(~20) + FindElement(1) = ~21 | 1 |
| `GetPlaceholder()` | TryFindElement(1) | 1 (no change) |
| `IsReadOnly()` | TryFindElement(1) | 1 (no change) |

**Current Code (problematic):**
```csharp
public TScope Enter(string? text, int? timeoutMs = null)
{
    Run<string>(nameof(Enter), text, () =>
    {
        CheckEnabled(timeoutMs);      // ← Calls WaitExists (~20 FindElement)
        var element = FindElement();  // ← Another FindElement call
        element.SendKeys(text);
    });
    return ContainingScope;
}

public void CheckEnabled(int? timeoutMs = null)
{
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    if (!WaitExists(true, timeout))   // ← Polls with FindElement
        throw new ElementNotFoundException(...);
    if (IsEnabled() == false)         // ← Another FindElement
        throw new InvalidOperationException(...);
}
```

**Optimized Code:**
```csharp
public TScope Enter(string? text, int? timeoutMs = null)
{
    if (text == null) return ContainingScope;
    
    return RunWithElement(nameof(Enter), text, timeoutMs, element =>
    {
        EnterCore(element, text, timeoutMs);
    });
}

protected void EnterCore(IMauiElement element, string text, int? timeoutMs = null)
{
    CheckEnabledCore(element, timeoutMs);
    element.SendKeys(text);
}

protected void CheckEnabledCore(IMauiElement element, int? timeoutMs = null)
{
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    
    // Element already exists (was found by RunWithElement)
    // Just check if it's enabled
    if (IsEnabledCore(element) != true)
    {
        if (!WaitEnabledCore(element, true, timeout))
        {
            throw new InvalidOperationException(
                $"Element is disabled and cannot be interacted with. Locator: {Locator}");
        }
    }
}
```

### 2.2 MauiFlyoutItemControl

| Method | Current FindElement Calls | After Optimization |
|--------|---------------------------|-------------------|
| `Click()` | FindElement(1) | 1 (no change, but use RunWithElement pattern) |
| `DoubleClick()` | FindElement(1) | 1 |
| `RightClick()` | FindElement(1) | 1 |
| `IsClickable()` | IsVisible(1) + IsEnabled(1) = 2 | 1 |
| `WaitClickable()` | Poll with IsClickable (~20 x 2) = ~40 | 1 + polling with element |

**Current Code:**
```csharp
public TScope Click(int? timeoutMs = null)
{
    Run(nameof(Click), () =>
    {
        var element = FindElement();
        element.Click();
    });
    return ContainingScope;
}

public bool? IsClickable()
{
    var isVisible = IsVisible();   // ← FindElement
    var isEnabled = IsEnabled();   // ← FindElement again
    
    if (isVisible == null || isEnabled == null)
        return null;
    
    return isVisible.Value && isEnabled.Value;
}
```

**Optimized Code:**
```csharp
public TScope Click(int? timeoutMs = null)
{
    return RunWithElement(nameof(Click), timeoutMs, element =>
    {
        ClickCore(element);
    });
}

protected void ClickCore(IMauiElement element)
{
    element.Click();
}

public bool? IsClickable()
{
    return IsClickableCore(TryFindElement());
}

protected bool? IsClickableCore(IMauiElement? element)
{
    var isVisible = IsVisibleCore(element);
    var isEnabled = IsEnabledCore(element);
    
    if (isVisible == null || isEnabled == null)
        return null;
    
    return isVisible.Value && isEnabled.Value;
}
```

### 2.3 MauiControlBase

| Method | Current FindElement Calls | After Optimization |
|--------|---------------------------|-------------------|
| `SendKeys()` | FindElement(2) - click + sendkeys | 1 |
| `GetText()` | TryFindElement(1) | 1 (no change) |

**Current Code:**
```csharp
public virtual TScope SendKeys(string keys)
{
    Run(nameof(SendKeys), keys, () =>
    {
        var element = FindElement();
        element.Click();  // Focus
        element.SendKeys(keys);
    });
    return ContainingScope;
}
```

**Optimized Code:**
```csharp
public virtual TScope SendKeys(string keys)
{
    return RunWithElement(nameof(SendKeys), keys, null, element =>
    {
        SendKeysCore(element, keys);
    });
}

protected virtual void SendKeysCore(IMauiElement element, string keys)
{
    element.Click();  // Focus
    element.SendKeys(keys);
}
```

### 2.4 MauiListControl

| Method | Current FindElement Calls | After Optimization |
|--------|---------------------------|-------------------|
| `GetItemCount()` | TryFindElement(1) | 1 (no change) |
| `GetAllItems()` | GetItemCount(1) | 1 (no change) |

**Note:** MauiListControl is already optimal - single TryFindElement call.

---

## 3. Method Mapping - Complete

### 3.1 MauiControlBase (Base Class)

| Public Method | Run Wrapper | Core Method |
|---------------|-------------|-------------|
| `IsExists()` | N/A | N/A |
| `IsVisible()` | N/A | `IsVisibleCore(element)` |
| `IsEnabled()` | N/A | `IsEnabledCore(element)` |
| `WaitExists()` | N/A | N/A |
| `WaitVisible()` | N/A | `WaitVisibleCore(element, ...)` |
| `WaitEnabled()` | N/A | `WaitEnabledCore(element, ...)` |
| `SendKeys()` | `RunWithElement()` | `SendKeysCore(element, keys)` |
| `GetText()` | N/A | `GetTextCore(element)` |

### 3.2 MauiButtonControl

| Public Method | Run Wrapper | Core Method |
|---------------|-------------|-------------|
| `Click()` | `RunWithElement()` | `ClickCore(element)` |
| `DoubleClick()` | `RunWithElement()` | `DoubleClickCore(element)` |
| `RightClick()` | `RunWithElement()` | `RightClickCore(element)` |
| `IsClickable()` | N/A | `IsClickableCore(element)` |
| `WaitClickable()` | N/A | `WaitClickableCore(element, ...)` |
| `CheckClickable()` | N/A | `CheckClickableCore(element)` |

### 3.3 MauiEntryControl

| Public Method | Run Wrapper | Core Method |
|---------------|-------------|-------------|
| `Enter()` | `RunWithElement()` | `EnterCore(element, text)` |
| `Clear()` | `RunWithElement()` | `ClearCore(element)` |
| `SetText()` | `RunWithElement()` | `SetTextCore(element, text)` |
| `CheckEnabled()` | N/A | `CheckEnabledCore(element)` |
| `GetPlaceholder()` | N/A | `GetPlaceholderCore(element)` |
| `IsReadOnly()` | N/A | `IsReadOnlyCore(element)` |
| `WaitPlaceholder()` | N/A | `WaitPlaceholderCore(element, ...)` |
| `WaitReadOnly()` | N/A | `WaitReadOnlyCore(element, ...)` |

### 3.4 MauiFlyoutItemControl

| Public Method | Run Wrapper | Core Method |
|---------------|-------------|-------------|
| `Click()` | `RunWithElement()` | `ClickCore(element)` |
| `DoubleClick()` | `RunWithElement()` | `DoubleClickCore(element)` |
| `RightClick()` | `RunWithElement()` | `RightClickCore(element)` |
| `IsClickable()` | N/A | `IsClickableCore(element)` |
| `WaitClickable()` | N/A | `WaitClickableCore(element, ...)` |

---

## 4. Implementation Details

### 4.1 MauiEntryControl - Full Implementation

```csharp
public class MauiEntryControl<TScope> : MauiControlBase<TScope>, IEditableTextControlObject<TScope>
{
    #region IEditableTextControlObject - Public API
    
    public TScope Enter(string? text, int? timeoutMs = null)
    {
        if (text == null) return ContainingScope;
        
        return RunWithElement(nameof(Enter), text, timeoutMs, element =>
        {
            EnterCore(element, text, timeoutMs);
        });
    }
    
    public TScope Clear(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Clear), timeoutMs, element =>
        {
            ClearCore(element, timeoutMs);
        });
    }
    
    public TScope SetText(string? text, int? timeoutMs = null)
    {
        if (text == null) return ContainingScope;
        
        return RunWithElement(nameof(SetText), text, timeoutMs, element =>
        {
            SetTextCore(element, text, timeoutMs);
        });
    }
    
    public void CheckEnabled(int? timeoutMs = null)
    {
        var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
        CheckEnabledCore(element, timeoutMs);
    }
    
    #endregion
    
    #region Core Methods (Element-Aware)
    
    protected void EnterCore(IMauiElement element, string text, int? timeoutMs = null)
    {
        CheckEnabledCore(element, timeoutMs);
        element.SendKeys(text);
    }
    
    protected void ClearCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckEnabledCore(element, timeoutMs);
        element.Clear();
    }
    
    protected void SetTextCore(IMauiElement element, string text, int? timeoutMs = null)
    {
        CheckEnabledCore(element, timeoutMs);
        element.Clear();
        element.SendKeys(text);
    }
    
    protected void CheckEnabledCore(IMauiElement element, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        
        // Element already exists - just check enabled state
        if (IsEnabledCore(element) != true)
        {
            if (!WaitEnabledCore(element, true, timeout))
            {
                throw new InvalidOperationException(
                    $"Element is disabled and cannot be interacted with. Locator: {Locator}");
            }
        }
    }
    
    #endregion
    
    #region Placeholder - Core Methods
    
    protected string? GetPlaceholderCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        return element.GetAttribute("Name")
            ?? element.GetAttribute("HelpText")
            ?? element.GetAttribute("hint")
            ?? element.GetAttribute("placeholderValue")
            ?? element.GetAttribute("placeholder");
    }
    
    public string? GetPlaceholder()
    {
        return GetPlaceholderCore(TryFindElement());
    }
    
    protected bool WaitPlaceholderCore(IMauiElement element, string expected, int timeoutMs)
    {
        return PollWithElement(element, e => GetPlaceholderCore(e) == expected, timeoutMs);
    }
    
    public bool WaitPlaceholder(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null) return false;
        
        return WaitPlaceholderCore(element, expected, timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region ReadOnly - Core Methods
    
    protected bool? IsReadOnlyCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        var readOnly = element.GetAttribute("readonly") ?? element.GetAttribute("isReadOnly");
        if (readOnly != null) return readOnly.Equals("true", StringComparison.OrdinalIgnoreCase);
        
        var editable = element.GetAttribute("editable");
        if (editable != null) return !editable.Equals("true", StringComparison.OrdinalIgnoreCase);
        
        return false;
    }
    
    public bool? IsReadOnly()
    {
        return IsReadOnlyCore(TryFindElement());
    }
    
    protected bool WaitReadOnlyCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(element, e => IsReadOnlyCore(e) == expected, timeoutMs);
    }
    
    public bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null) return expected.Value == false; // Not found = not readonly
        
        return WaitReadOnlyCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
}
```

### 4.2 MauiFlyoutItemControl - Full Implementation

```csharp
public class MauiFlyoutItemControl<TScope> : MauiControlBase<TScope>, IClickableControlObject<TScope>
{
    #region IClickableControlObject - Public API
    
    public TScope Click(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Click), timeoutMs, element =>
        {
            ClickCore(element);
        });
    }
    
    public TScope DoubleClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(DoubleClick), timeoutMs, element =>
        {
            DoubleClickCore(element);
        });
    }
    
    public TScope RightClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(RightClick), timeoutMs, element =>
        {
            RightClickCore(element);
        });
    }
    
    public bool? IsClickable()
    {
        return IsClickableCore(TryFindElement());
    }
    
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null)
            return expected.Value == false;
        
        return WaitClickableCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region Core Methods (Element-Aware)
    
    protected void ClickCore(IMauiElement element)
    {
        element.Click();
    }
    
    protected void DoubleClickCore(IMauiElement element)
    {
        element.Click();
        element.Click();
    }
    
    protected void RightClickCore(IMauiElement element)
    {
        var unwrappedElement = element.UnwrapElement();
        var unwrappedDriver = Context.Driver.UnwrapDriver();
        
        var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
        actions.ContextClick(unwrappedElement).Perform();
    }
    
    protected bool? IsClickableCore(IMauiElement? element)
    {
        var isVisible = IsVisibleCore(element);
        var isEnabled = IsEnabledCore(element);
        
        if (isVisible == null || isEnabled == null)
            return null;
        
        return isVisible.Value && isEnabled.Value;
    }
    
    protected bool WaitClickableCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(element, e => IsClickableCore(e) == expected, timeoutMs);
    }
    
    #endregion
}
```

### 4.3 MauiControlBase - SendKeys Optimization

```csharp
public class MauiControlBase<TScope>
{
    #region Basic Interactions
    
    public virtual TScope SendKeys(string keys)
    {
        return RunWithElement(nameof(SendKeys), keys, null, element =>
        {
            SendKeysCore(element, keys);
        });
    }
    
    protected virtual void SendKeysCore(IMauiElement element, string keys)
    {
        element.Click();  // Focus the element first
        element.SendKeys(keys);
    }
    
    #endregion
    
    #region Text - Core Methods
    
    protected string? GetTextCore(IMauiElement? element)
    {
        if (element == null) return null;
        return element.Text;
    }
    
    public string? GetText(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return GetTextCore(TryFindElement());
    }
    
    #endregion
}
```

---

## 5. Implementation Plan

### Phase 2.1: MauiControlBase Updates

1. ✅ Already done in SPEC-015:
   - `FindElementWithWait()`
   - `PollWithElement()`
   - `RunWithElement()` overloads
   - `IsVisibleCore()`, `IsEnabledCore()`
   - `WaitVisibleCore()`, `WaitEnabledCore()`

2. To add:
   - `SendKeysCore()` method
   - `GetTextCore()` method
   - Update `SendKeys()` to use `RunWithElement()`

### Phase 2.2: MauiEntryControl Updates

1. Add Core methods:
   - `EnterCore()`
   - `ClearCore()`
   - `SetTextCore()`
   - `CheckEnabledCore()`
   - `GetPlaceholderCore()`
   - `IsReadOnlyCore()`
   - `WaitPlaceholderCore()`
   - `WaitReadOnlyCore()`

2. Update public methods to use `RunWithElement()` and delegate to Core

### Phase 2.3: MauiFlyoutItemControl Updates

1. Add Core methods:
   - `ClickCore()`
   - `DoubleClickCore()`
   - `RightClickCore()`
   - `IsClickableCore()`
   - `WaitClickableCore()`

2. Update public methods to use `RunWithElement()` and delegate to Core

### Phase 2.4: Testing

1. Run all existing UI tests
2. Verify no regressions
3. Measure performance improvement

---

## 6. Performance Expectations

| Control | Method | Before | After | Improvement |
|---------|--------|--------|-------|-------------|
| MauiEntryControl | Enter() | ~21 calls | 1 call | 21x |
| MauiEntryControl | Clear() | ~21 calls | 1 call | 21x |
| MauiEntryControl | SetText() | ~21 calls | 1 call | 21x |
| MauiFlyoutItemControl | Click() | 1 call | 1 call | (pattern consistency) |
| MauiFlyoutItemControl | IsClickable() | 2 calls | 1 call | 2x |
| MauiFlyoutItemControl | WaitClickable() | ~40 calls | 1 call | 40x |
| MauiControlBase | SendKeys() | 1 call | 1 call | (pattern consistency) |

**Total estimated improvement for typical test with 5 Entry operations:**  
Before: 5 × 21 = 105 FindElement calls  
After: 5 × 1 = 5 FindElement calls  
**Improvement: 21x reduction**

---

## 7. Success Criteria

1. ✅ All action methods use `RunWithElement()` pattern
2. ✅ All state-check methods have `*Core()` overloads
3. ✅ All existing UI tests pass without modification
4. ✅ No public API changes
5. ✅ Measurable performance improvement

---

## 8. References

- [SPEC-015: Element Lookup Optimization](./SPEC-015-Element-Lookup-Optimization.md)
- [MauiControlBase.cs](../srcnew/Brinell.Maui/Controls/MauiControlBase.cs)
- [MauiEntryControl.cs](../srcnew/Brinell.Maui/Controls/MauiEntryControl.cs)
- [MauiFlyoutItemControl.cs](../srcnew/Brinell.Maui/Controls/MauiFlyoutItemControl.cs)

---

**Revision History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-18 | Brinell Team | Initial draft |
