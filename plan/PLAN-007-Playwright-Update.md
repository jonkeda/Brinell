# PLAN-007: Brinell.Html.Playwright Platform Update

**Created:** January 3, 2026
**Status:** ✅ Complete (32 tests passing)

---

## Overview

Update the Brinell.Html.Playwright platform to include missing base classes and controls that were added to Brinell.Html. This platform uses Playwright for async web testing.

---

## Current State

### Base Classes Present
- [x] ControlBase / ControlBaseAsync
- [x] ContentControlBase
- [x] PageBase / LoadingPageBase
- [x] RangeControlBase
- [x] SelectorControlBase
- [x] TextControlBase
- [x] ToggleControlBase
- [x] **BusyPageBase** ✅ Added
- [x] **ItemsControlBase** ✅ Added
- [x] **ScrollableControlBase** ✅ Added

### Controls Present
- [x] ButtonControl / ButtonControlAsync
- [x] CheckBoxControl
- [x] LabelControl
- [x] LinkControl
- [x] ProgressControl
- [x] RangeInputControl
- [x] SelectControl
- [x] TextAreaControl
- [x] TextInputControl / TextControlAsync
- [x] **ListControl** ✅ Added
- [x] **TableControl** ✅ Added
- [x] **ScrollContainerControl** ✅ Added

---

## Implementation Tasks

### Phase 1: Base Classes

| Task | File | Status |
|------|------|--------|
| Create BusyPageBase | Controls/Base/BusyPageBase.cs | ✅ |
| Create ItemsControlBase | Controls/Base/ItemsControlBase.cs | ✅ |
| Create ScrollableControlBase | Controls/Base/ScrollableControlBase.cs | ✅ |

### Phase 2: Concrete Controls

| Task | File | Status |
|------|------|--------|
| Create ListControl | Controls/ListControl.cs | ✅ |
| Create TableControl | Controls/TableControl.cs | ✅ |
| Create ScrollContainerControl | Controls/ScrollContainerControl.cs | ✅ |

### Phase 3: Testing

| Task | Status |
|------|--------|
| Build Brinell.Html.Playwright | ✅ |
| Create docs/run/Playwright.md | ⬜ (deferred) |
| Run existing Playwright tests | ✅ 32/32 passing |
| Add TableTests (if needed) | ⬜ (deferred - Selenium tests cover TableControl) |

---

## Key Differences from Html (Selenium)

| Aspect | Html (Selenium) | Html.Playwright |
|--------|----------------|-----------------|
| Element Type | IWebElement | ILocator |
| Async Pattern | Sync with GetAwaiter | Native async/await |
| Context | SeleniumTestContext | PlaywrightTestContext |
| Script Execution | ExecuteScript | EvaluateAsync |
| Stale Handling | StaleElementReferenceException | Built-in locator retry |

---

## Notes

- Playwright locators are lazily evaluated and auto-retry, so stale element handling is built-in
- All methods should have async versions as primary (sync for convenience)
- Use `Page.EvaluateAsync<T>()` for JavaScript execution
- Container constructor pattern should use `ILocator?` for consistency

---

## Test Execution

```powershell
# Build
dotnet build samples/Brinell.Samples.Blazor.PlaywrightTests

# Run tests
$env:HEADLESS="true"
$env:BLAZOR_APP_URL="http://localhost:5180"
dotnet test samples/Brinell.Samples.Blazor.PlaywrightTests --logger "console;verbosity=normal"
```

---

## Completion Criteria

- [x] All base classes created
- [x] All concrete controls created
- [x] Build succeeds with no errors
- [x] All Playwright tests pass (32/32)
- [ ] Documentation updated (deferred)

---

## Issues Resolved

See [PLAN-007b-Playwright-Issues.md](PLAN-007b-Playwright-Issues.md) for details:
1. **Excessive Wait Times** - Fixed by using `WaitForLoadStateAsync(NetworkIdle)` instead of custom JavaScript polling
2. **Text Whitespace** - Fixed by using `InnerTextAsync()` instead of `TextContentAsync()`
