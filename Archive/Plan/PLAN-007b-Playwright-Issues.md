# PLAN-007b: Playwright Test Issues

**Created:** January 3, 2026  
**Status:** ✅ Fixed

---

## Problem Statement

Playwright tests appear to:
1. Wait for a long time (~22 seconds per test)
2. Then quit without doing meaningful work
3. Most tests pass but may not be actually testing anything

## Investigation Areas

1. [x] Check test base class setup - is browser/page initialized correctly?
2. [x] Check page object selectors - are they finding elements?
3. [x] Check navigation - is the app being navigated to?
4. [x] Compare with working Selenium (Html) tests
5. [x] Check if async/await is being used correctly
6. [x] Check timeout settings

---

## Diagnosis Steps

### Step 1: Check BlazorSampleTestBase

Review how the Playwright test base initializes the browser and navigates to the app.

### Step 2: Check Page Objects

Review if page objects are finding elements correctly.

### Step 3: Run a simple test with logging

Add diagnostic logging to understand what's happening.

---

## Findings

### Issue 1: Excessive Wait Times ✅ FIXED

**Problem:** Tests take ~22 seconds each because `WaitForBlazorReadyAsync` waits for 2x10 second timeouts:
1. `WaitForDocumentReadyAsync` - 10 seconds
2. `WaitForBlazorConnectionAsync` - 10 seconds

The `WaitForBlazorConnectionAsync` JavaScript condition may not evaluate correctly:
```javascript
if (typeof Blazor !== 'undefined' && Blazor._internal) {
    return true;
}
```

This check may be failing because:
- Blazor's internal API may have changed
- The `Blazor._internal` object may not exist in the current version
- The fallback `document.readyState === 'complete'` is inside the same condition

**Solution:** Simplified the Blazor ready check to use Playwright's built-in `WaitForLoadStateAsync(NetworkIdle)`:
- Much more reliable than JavaScript polling
- Test time reduced from ~22 seconds to ~2 seconds per test

### Issue 2: Text Whitespace Not Normalized ✅ FIXED

**Problem:** `GetText()` and `GetTextAsync()` used `TextContentAsync()` which preserves whitespace.
HTML like:
```html
<a id="download-link">
    Download Sample PDF
</a>
```
Returns `"\n    Download Sample PDF\n"` instead of `"Download Sample PDF"`.

**Solution:** Changed to use `InnerTextAsync()` which normalizes whitespace for visible text.

### Issue 3: HTML Element IDs Confirmed ✓

The Blazor app HTML contains correct IDs:
- `id="counter-title"` ✓
- `id="count-display"` ✓  
- `id="increment-btn"` ✓
- `id="reset-btn"` ✓

Element finding is working correctly.

---

## Files Modified

1. **BlazorPlaywrightTestBase.cs** - Simplified `WaitForBlazorReadyAsync()` to use `WaitForLoadStateAsync(NetworkIdle)`
2. **ControlBase.cs** - Changed `GetText()` and `GetTextAsync()` to use `InnerTextAsync()` for whitespace normalization

---

## Result

All 32 Playwright tests pass in ~42 seconds total (vs ~11+ minutes before fix).
