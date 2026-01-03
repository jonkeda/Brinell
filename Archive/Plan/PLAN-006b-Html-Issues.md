# PLAN-006b: Brinell.Html Issues Diagnosis

**Created:** January 3, 2026
**Status:** ✅ Fixed
**Platform:** Brinell.Html (Selenium)

---

## Issue Summary

During testing of the new `TableControl` and `ItemsControlBase` classes, we encountered a `StaleElementReferenceException` when calling `GetRowCount()` on the TableControl.

### Error Message
```
OpenQA.Selenium.StaleElementReferenceException : stale element reference: 
stale element not found in the current frame
```

### Stack Trace (Key Lines)
```
at Brinell.Html.Controls.Base.ItemsControlBase.FindItems()
at Brinell.Html.Controls.Base.ItemsControlBase.GetItemCount()
at Brinell.Html.Controls.TableControl.GetRowCount()
```

---

## Root Cause Analysis

### Issue 1: Stale Element References in Blazor SPA

**Problem:**
Blazor Server applications use SignalR to update the DOM dynamically. When the page renders, element references obtained from Selenium become "stale" because Blazor replaces DOM nodes during its rendering cycle.

**Code Analysis:**

Html `ItemsControlBase.FindItems()`:
```csharp
protected virtual IReadOnlyList<IWebElement> FindItems()
{
    var container = FindElement();  // Gets element, may be stale after Blazor render
    if (container == null) return Array.Empty<IWebElement>();
    
    return container.FindElements(By.CssSelector(ItemSelector)).ToList();
}
```

The `FindElement()` method in `ControlBase` returns an `IWebElement` directly, which can become stale if Blazor re-renders the page between the call to `FindElement()` and `FindElements()`.

**Comparison with MAUI:**

MAUI `ItemsControlBase.GetItemCount()`:
```csharp
public virtual int GetItemCount()
{
    var element = FindElement();  // Fresh lookup each time
    if (element != null)
    {
        var items = element.FindElements(By.XPath(".//*[@clickable='true']"));
        return items.Count;
    }
    return 0;
}
```

The MAUI implementation also looks up `FindElement()` each time, but the key difference is:

1. **Native apps don't have dynamic DOM** - MAUI/Appium talks to a native app via UIA which doesn't have the DOM replacement issue
2. **Selenium+Blazor race condition** - Between finding the container and finding children, Blazor may have re-rendered

### Issue 2: Timing Between Navigation and DOM Stability

**Problem:**
The test navigates to `/dashboard` and immediately tries to interact with the table. Even though `WaitForDisplayed()` succeeds (checking if title exists), Blazor may still be hydrating interactive components.

**Timeline:**
```
1. Navigate to /dashboard
2. WaitForBlazorReady() - document.readyState = complete
3. WaitForDisplayed() - #dashboard-title visible
4. GetActivityRowCount() called
5. Blazor re-renders table during SignalR hydration
6. container.FindElements() fails - stale reference
```

---

## Comparison: Html vs MAUI Implementation

| Aspect | MAUI (Appium) | Html (Selenium) |
|--------|---------------|-----------------|
| **Element Lookup** | `FindElementDirect(automationId)` returns fresh lookup | `FindElementDirect(automationId)` returns fresh lookup |
| **DOM Stability** | Native UI tree is stable | Web DOM can be replaced by SPA frameworks |
| **Container Reference** | `AppiumElement` stored, rarely stale | `IWebElement` can become stale after re-render |
| **Child Lookup** | `element.FindElements(By.XPath(...))` | `element.FindElements(By.CssSelector(...))` |
| **Framework Behavior** | MAUI doesn't replace visual tree during navigation | Blazor replaces DOM nodes during render cycle |

### Key Difference: Element Reference Lifetime

**MAUI:**
- Native elements persist in the visual tree
- Container element remains valid as long as the control exists
- No "stale element" concept in native automation

**Selenium+Blazor:**
- DOM nodes are replaced during Blazor render cycles
- `IWebElement` references become invalid when DOM changes
- Must re-query elements after any potential DOM mutation

---

## Proposed Solutions

### Solution 1: Re-query Container Every Time (Recommended)

Modify `FindItems()` to be more defensive and handle stale elements:

```csharp
protected virtual IReadOnlyList<IWebElement> FindItems()
{
    try
    {
        var container = FindElement();
        if (container == null) return Array.Empty<IWebElement>();
        
        return container.FindElements(By.CssSelector(ItemSelector)).ToList();
    }
    catch (StaleElementReferenceException)
    {
        // DOM changed, retry with fresh lookup
        var container = FindElement();
        if (container == null) return Array.Empty<IWebElement>();
        
        return container.FindElements(By.CssSelector(ItemSelector)).ToList();
    }
}
```

### Solution 2: Use JavaScript for Item Count (More Reliable)

Use JavaScript execution to query items, avoiding Selenium element references:

```csharp
public virtual int GetItemCount()
{
    var script = $@"
        var container = document.querySelector('{AutomationId}');
        if (!container) return 0;
        return container.querySelectorAll('{ItemSelector}').length;
    ";
    var result = _context.ExecuteScript(script);
    return Convert.ToInt32(result);
}
```

### Solution 3: Add Wait for DOM Stability

Add a method to wait for DOM to be stable before querying:

```csharp
protected virtual void WaitForDomStable(int stabilityMs = 100)
{
    _context.WaitFor(() =>
    {
        var count1 = GetItemCountUnsafe();
        Thread.Sleep(stabilityMs);
        var count2 = GetItemCountUnsafe();
        return count1 == count2;
    }, 2000, "DOM stability");
}
```

---

## Implementation Priority

1. **Solution 1** - Quick fix, handles common case
2. **Solution 3** - Add DOM stability wait for Blazor pages
3. **Solution 2** - Use JavaScript for performance-critical operations

---

## Additional Observations

### ControlBase Differences

| Feature | MAUI ControlBase | Html ControlBase |
|---------|------------------|------------------|
| Container support | ✅ `AppiumElement? _container` | ✅ `IWebElement? _container` |
| Find element | ✅ Returns fresh lookup | ✅ Returns fresh lookup |
| Gestures | ✅ Tap, DoubleTap, LongPress, Swipe | ❌ Click only |
| Attributes | ❌ Not applicable | ✅ GetAttribute, HasClass |
| CSS values | ❌ Not applicable | ✅ GetCssValue |

### ItemsControlBase Differences

| Feature | MAUI ItemsControlBase | Html ItemsControlBase |
|---------|----------------------|----------------------|
| Item selector | XPath: `.//*[@clickable='true']` | CSS: Abstract `ItemSelector` property |
| Scroll support | ✅ ScrollDown/ScrollUp via swipe | ❌ Not yet implemented |
| Wait methods | ✅ WaitForItemCount, WaitForItems | ✅ WaitItemCount, WaitItemCountAtLeast |
| Stale handling | ❌ Not needed | ❌ Missing - needs addition |

---

## Next Steps

1. [x] Implement Solution 1 (stale element retry) in `ItemsControlBase`
2. [x] Add explicit DOM stability wait after Blazor navigation (existing WaitForBlazorReady sufficient)
3. [x] Test all TableTests with the fix (10/10 passing)
4. [ ] Consider Solution 2 for performance if needed (deferred - not needed currently)
5. [x] Update PLAN-006-Html-Update.md with final status

## Resolution

**Fix Applied:** Added stale element retry logic to `ItemsControlBase.FindItems()`:

```csharp
protected virtual IReadOnlyList<IWebElement> FindItems()
{
    for (int attempt = 0; attempt <= MaxStaleRetries; attempt++)
    {
        try
        {
            var container = FindElement();
            if (container == null) return Array.Empty<IWebElement>();
            
            return container.FindElements(By.CssSelector(ItemSelector)).ToList();
        }
        catch (StaleElementReferenceException) when (attempt < MaxStaleRetries)
        {
            Log($"Stale element detected, retrying ({attempt + 1}/{MaxStaleRetries})...");
            Thread.Sleep(50);
        }
    }
    return Array.Empty<IWebElement>();
}
```

**Result:** All 33 Blazor UI tests passing (including 10 new TableTests).

---

## Files Modified

- `src/Brinell.Html/Controls/Base/ItemsControlBase.cs` - Added stale element handling ✅
