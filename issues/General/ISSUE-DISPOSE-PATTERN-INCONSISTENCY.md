# Issue: Dispose Pattern Inconsistency Across Test Bases

**Created:** January 4, 2026  
**Priority:** Medium  
**Category:** Code Quality / Consistency  
**Status:** Open

---

## Summary

The dispose/cleanup patterns across the existing test base classes are inconsistent. Some use robust dispose patterns with double-dispose protection, while others have minimal cleanup. This inconsistency can lead to resource leaks and makes the codebase harder to maintain.

---

## Current State Analysis

### 1. Core Base Class: `UITestBase<TContext>`
**Location:** [src/Brinell.Core/Testing/UITestBase.cs](../../src/Brinell.Core/Testing/UITestBase.cs)

| Feature | Status | Notes |
|---------|--------|-------|
| `IDisposable` | ✅ Yes | Standard dispose pattern |
| `IAsyncLifetime` | ✅ Yes | xUnit async lifecycle |
| `_disposed` flag | ✅ Yes | Double-dispose protection |
| `GC.SuppressFinalize` | ✅ Yes | Proper pattern |
| `virtual Dispose(bool)` | ✅ Yes | Allows subclass override |
| Cleanup actions | ⚠️ Minimal | Only cleans up logger |

**Code:**
```csharp
protected virtual void Dispose(bool disposing)
{
    if (!_disposed)
    {
        if (disposing)
        {
            try
            {
                Log("Cleaning up test resources...");
                Logger?.LogAction(TestName, "", "", "Cleanup");
                _logger?.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Error disposing resources: {ex.Message}");
            }
        }
        _disposed = true;
    }
}
```

---

### 2. MAUI Test Base: `MauiTestBase`
**Location:** [samples/Brinell.Samples.Maui.UITests/MauiTestBase.cs](../../samples/Brinell.Samples.Maui.UITests/MauiTestBase.cs)

| Feature | Status | Notes |
|---------|--------|-------|
| `IDisposable` | ✅ Yes | |
| `IAsyncLifetime` | ❌ No | Not needed for sync Appium |
| `_disposed` flag | ❌ No | **Missing protection** |
| `GC.SuppressFinalize` | ✅ Yes | |
| `virtual Dispose(bool)` | ❌ No | **Not overridable** |
| Cleanup actions | ✅ Good | Delegates to `Context.Dispose()` |

**Code:**
```csharp
public void Dispose()
{
    Context?.Dispose();  // Delegates cleanup
    GC.SuppressFinalize(this);
}
```

**Problem:** No double-dispose protection, not overridable.

---

### 3. MAUI ControlObject6 Test Base: `MauiTestBase6`
**Location:** [samples/Brinell.Samples.Maui.UITests.ControlObject6/MauiTestBase6.cs](../../samples/Brinell.Samples.Maui.UITests.ControlObject6/MauiTestBase6.cs)

| Feature | Status | Notes |
|---------|--------|-------|
| `IDisposable` | ✅ Yes | |
| `IAsyncLifetime` | ❌ No | |
| `_disposed` flag | ✅ Yes | Double-dispose protection |
| `GC.SuppressFinalize` | ✅ Yes | |
| `virtual Dispose(bool)` | ✅ Yes | Allows override |
| Cleanup actions | ✅ Good | Calls `_driver?.Quit()` |

**Code:**
```csharp
protected virtual void Dispose(bool disposing)
{
    if (_disposed) return;

    if (disposing)
    {
        try
        {
            _driver?.Quit();
        }
        catch (Exception ex)
        {
            Log($"Error disposing driver: {ex.Message}");
        }
    }

    _disposed = true;
}
```

✅ **Best implementation** - has all the proper patterns.

---

### 4. Appium Test Context: `AppiumTestContext`
**Location:** [src/Brinell.Maui/Infrastructure/AppiumTestContext.cs](../../src/Brinell.Maui/Infrastructure/AppiumTestContext.cs)

| Feature | Status | Notes |
|---------|--------|-------|
| `IDisposable` | ✅ Yes | |
| `_disposed` flag | ❌ No | **Missing protection** |
| `GC.SuppressFinalize` | ✅ Yes | |
| `virtual Dispose(bool)` | ❌ No | **Not using pattern** |
| Cleanup actions | ⚠️ Minimal | Only disposes driver |

**Code:**
```csharp
public void Dispose()
{
    _driver?.Dispose();
    GC.SuppressFinalize(this);
}
```

**Problem:** No double-dispose protection, doesn't dispose logger.

---

### 5. Blazor Sample Test Base: `BlazorSampleTestBase`
**Location:** [samples/Brinell.Samples.Blazor.UITests/TestBase/BlazorSampleTestBase.cs](../../samples/Brinell.Samples.Blazor.UITests/TestBase/BlazorSampleTestBase.cs)

| Feature | Status | Notes |
|---------|--------|-------|
| Inherits from | `HtmlUITestBase` | Gets dispose from parent |
| Own cleanup | ❌ None | Relies entirely on base class |

**Note:** This class correctly delegates to base class, which is fine.

---

### 6. HTML UI Test Base: `HtmlUITestBase`
**Location:** [src/Brinell.Html/Testing/HtmlUITestBase.cs](../../src/Brinell.Html/Testing/HtmlUITestBase.cs)

| Feature | Status | Notes |
|---------|--------|-------|
| Inherits from | `UITestBase<SeleniumTestContext>` | Gets dispose from parent |
| `override Dispose(bool)` | ✅ Yes | Properly overrides base |
| Cleanup actions | ✅ Good | Calls `CloseBrowser()` |

**Code:**
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        try
        {
            CloseBrowser();
        }
        catch (Exception ex)
        {
            Log($"Error closing browser: {ex.Message}");
        }
    }
    
    base.Dispose(disposing);
}
```

✅ **Good implementation** - properly chains to base.

---

### 7. Blazor ControlObject6 Test Base: `BlazorTestBase6`
**Location:** [samples/Brinell.Samples.Blazor.UITests.ControlObject6/TestBase/BlazorTestBase6.cs](../../samples/Brinell.Samples.Blazor.UITests.ControlObject6/TestBase/BlazorTestBase6.cs)

| Feature | Status | Notes |
|---------|--------|-------|
| `IAsyncLifetime` | ✅ Yes | xUnit async lifecycle |
| `IDisposable` | ❌ No | Only async cleanup |
| `_disposed` flag | ❌ No | **Missing protection** |
| `virtual DisposeAsync()` | ❌ No | **Not overridable** |
| Cleanup actions | ✅ Good | Proper ordered cleanup |

**Code:**
```csharp
public async Task DisposeAsync()
{
    try
    {
        if (_page != null) await _page.CloseAsync();
        if (_browserContext != null) await _browserContext.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
    catch (Exception ex)
    {
        Log($"Error during cleanup: {ex.Message}");
    }
}
```

**Problem:** No double-dispose protection, not overridable.

---

## Comparison Matrix

| Class | IDisposable | IAsyncLifetime | `_disposed` flag | `virtual Dispose` | Proper Cleanup |
|-------|-------------|----------------|------------------|-------------------|----------------|
| `UITestBase<T>` | ✅ | ✅ | ✅ | ✅ | ⚠️ Minimal |
| `MauiTestBase` | ✅ | ❌ | ❌ | ❌ | ✅ |
| `MauiTestBase6` | ✅ | ❌ | ✅ | ✅ | ✅ |
| `AppiumTestContext` | ✅ | ❌ | ❌ | ❌ | ⚠️ |
| `HtmlUITestBase` | ✅ | ✅ | ✅ (inherited) | ✅ | ✅ |
| `BlazorTestBase6` | ❌ | ✅ | ❌ | ❌ | ✅ |

---

## Recommendations

### Priority 1: Add Double-Dispose Protection (High)

**Classes to fix:**
- `MauiTestBase`
- `AppiumTestContext`
- `BlazorTestBase6`

**Pattern to implement:**
```csharp
private bool _disposed;

protected virtual void Dispose(bool disposing)
{
    if (_disposed) return;
    
    if (disposing)
    {
        // cleanup managed resources
    }
    
    _disposed = true;
}
```

### Priority 2: Make Dispose Virtual (Medium)

**Classes to fix:**
- `MauiTestBase` - Add `protected virtual void Dispose(bool disposing)`
- `BlazorTestBase6` - Add `protected virtual Task DisposeAsyncCore()`

**Pattern for async:**
```csharp
private bool _disposed;

public async Task DisposeAsync()
{
    if (_disposed) return;
    _disposed = true;
    
    await DisposeAsyncCore();
}

protected virtual async Task DisposeAsyncCore()
{
    // cleanup
}
```

### Priority 3: Consistent Logger Cleanup (Low)

**Issue:** `AppiumTestContext` doesn't dispose the logger.

**Fix:**
```csharp
public void Dispose()
{
    if (_disposed) return;
    
    _driver?.Dispose();
    _logger?.Dispose();  // Add this
    
    _disposed = true;
    GC.SuppressFinalize(this);
}
```

---

## Proposed Standard Pattern

### Synchronous (MAUI/Appium)
```csharp
public class MyTestBase : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                // Cleanup managed resources in order
                _context?.Dispose();
                _driver?.Quit();
                _logger?.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Error during cleanup: {ex.Message}");
            }
        }

        _disposed = true;
    }
}
```

### Asynchronous (Blazor/Playwright)
```csharp
public class MyTestBase : IAsyncLifetime
{
    private bool _disposed;

    public async Task DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await DisposeAsyncCore();
    }

    protected virtual async Task DisposeAsyncCore()
    {
        try
        {
            // Cleanup in reverse order of creation
            if (_page != null) await _page.CloseAsync();
            if (_browserContext != null) await _browserContext.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }
        catch (Exception ex)
        {
            Log($"Error during cleanup: {ex.Message}");
        }
    }
}
```

---

## Acceptance Criteria

- [ ] All test base classes have `_disposed` flag for double-dispose protection
- [ ] All dispose methods are `virtual` or have virtual core method
- [ ] All contexts properly dispose their loggers
- [ ] Cleanup order is documented (reverse of creation)
- [ ] Exception handling wraps all cleanup code
- [ ] `GC.SuppressFinalize` called in `Dispose()` when `_disposed` flag is used

---

## Related Files

- [src/Brinell.Core/Testing/UITestBase.cs](../../src/Brinell.Core/Testing/UITestBase.cs)
- [src/Brinell.Maui/Infrastructure/AppiumTestContext.cs](../../src/Brinell.Maui/Infrastructure/AppiumTestContext.cs)
- [src/Brinell.Html/Testing/HtmlUITestBase.cs](../../src/Brinell.Html/Testing/HtmlUITestBase.cs)
- [samples/Brinell.Samples.Maui.UITests/MauiTestBase.cs](../../samples/Brinell.Samples.Maui.UITests/MauiTestBase.cs)
- [samples/Brinell.Samples.Maui.UITests.ControlObject6/MauiTestBase6.cs](../../samples/Brinell.Samples.Maui.UITests.ControlObject6/MauiTestBase6.cs)
- [samples/Brinell.Samples.Blazor.UITests.ControlObject6/TestBase/BlazorTestBase6.cs](../../samples/Brinell.Samples.Blazor.UITests.ControlObject6/TestBase/BlazorTestBase6.cs)

---

## Notes

- The ControlObject6 test bases (`MauiTestBase6`) have better patterns than the original versions
- Consider using `MauiTestBase6` as the template when fixing `MauiTestBase`
- The `HtmlUITestBase` is a good example of proper inheritance and override patterns
