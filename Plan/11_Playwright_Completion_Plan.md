# Plan 11: Complete Playwright Integration for Blazor

## Overview
This plan completes the Playwright integration started in Plan 08. The infrastructure is in place; this plan focuses on:
1. Adding all remaining control objects
2. Updating documentation
3. Adding Playwright-specific instructions

## Current State (Completed)

### Infrastructure ✅
- `Brinell.Html.Playwright` project created
- `PlaywrightElementAdapter` - ILocator wrapper
- `PlaywrightDriverAdapter` - IPage wrapper  
- `PlaywrightScreenshotService` - Screenshot capture
- `PlaywrightTestContext` - Test context with tracing/mocking
- `PlaywrightUITestBase` - Test base class

### Control Base Classes ✅
- `ControlBase` - Is/Wait/Check/Assert pattern
- `PageBase` - Page object pattern
- `ContentControlBase` - Clickable controls base
- `TextControlBase` - Text input base

### Controls (Partial) ✅
- `ButtonControl`
- `LabelControl`  
- `TextInputControl`

### Sample Tests ✅
- `Brinell.Samples.Blazor.PlaywrightTests` project
- `CounterPage` page object
- 9 passing tests

---

## Phase 1: Add Remaining Controls (1 day)

### 1.1 Controls to Add

| Selenium Control | Playwright Control | Priority | Notes |
|------------------|-------------------|----------|-------|
| `CheckBoxControl` | `CheckBoxControl` | High | input[type=checkbox] |
| `LinkControl` | `LinkControl` | High | `<a>` elements |
| `SelectControl` | `SelectControl` | High | `<select>` dropdowns |
| `TextAreaControl` | `TextAreaControl` | Medium | Multi-line text input |
| `RangeInputControl` | `RangeInputControl` | Medium | Sliders |
| `ProgressControl` | `ProgressControl` | Low | Progress bars (read-only) |

### 1.2 CheckBoxControl Implementation
```csharp
namespace Brinell.Html.Playwright.Controls;

public class CheckBoxControl : ContentControlBase
{
    public CheckBoxControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId) { }

    // Is methods
    public bool IsChecked() { ... }
    public async Task<bool> IsCheckedAsync() { ... }

    // Actions
    public void Check() { ... }
    public async Task CheckAsync() { ... }
    public void Uncheck() { ... }
    public async Task UncheckAsync() { ... }
    public void Toggle() { ... }
    public async Task ToggleAsync() { ... }

    // Wait methods
    public bool WaitChecked(bool expected = true, int? timeoutMs = null) { ... }

    // Assert methods
    public void AssertChecked(string? message = null) { ... }
    public void AssertUnchecked(string? message = null) { ... }
}
```

### 1.3 LinkControl Implementation
```csharp
public class LinkControl : ContentControlBase
{
    // Inherits Click, DoubleClick, RightClick, Hover from ContentControlBase
    
    // Link-specific
    public string? GetHref() { ... }
    public async Task<string?> GetHrefAsync() { ... }
    public string? GetTarget() { ... }
    
    // Assert methods
    public void AssertHref(string expected, string? message = null) { ... }
    public void AssertOpensInNewTab(string? message = null) { ... }
}
```

### 1.4 SelectControl Implementation
```csharp
public class SelectControl : ControlBase
{
    // Get state
    public string GetSelectedValue() { ... }
    public async Task<string> GetSelectedValueAsync() { ... }
    public string GetSelectedText() { ... }
    public async Task<string> GetSelectedTextAsync() { ... }
    public IReadOnlyList<string> GetOptions() { ... }
    
    // Actions
    public void SelectByValue(string value) { ... }
    public async Task SelectByValueAsync(string value) { ... }
    public void SelectByText(string text) { ... }
    public async Task SelectByTextAsync(string text) { ... }
    public void SelectByIndex(int index) { ... }
    
    // Multi-select support
    public bool IsMultiple { get; }
    public IReadOnlyList<string> GetSelectedValues() { ... }
    public void SelectMultiple(params string[] values) { ... }
    
    // Wait methods
    public bool WaitSelectedValue(string expected, int? timeoutMs = null) { ... }
    
    // Assert methods
    public void AssertSelectedValue(string expected, string? message = null) { ... }
    public void AssertSelectedText(string expected, string? message = null) { ... }
    public void AssertHasOption(string value, string? message = null) { ... }
}
```

### 1.5 TextAreaControl Implementation
```csharp
public class TextAreaControl : TextControlBase
{
    // Inherits: Enter, Clear, ClearAndEnter, GetText, Focus, Blur
    
    // TextArea-specific
    public int GetRows() { ... }
    public int GetCols() { ... }
    public int? GetMaxLength() { ... }
    public string? GetPlaceholder() { ... }
    
    // Assert methods (inherited + specific)
    public void AssertRowCount(int expected, string? message = null) { ... }
}
```

### 1.6 RangeInputControl Implementation  
```csharp
public class RangeInputControl : ControlBase
{
    // Get state
    public double GetValue() { ... }
    public async Task<double> GetValueAsync() { ... }
    public double GetMin() { ... }
    public double GetMax() { ... }
    public double GetStep() { ... }
    
    // Actions
    public void SetValue(double value) { ... }
    public async Task SetValueAsync(double value) { ... }
    public void Increment(int steps = 1) { ... }
    public void Decrement(int steps = 1) { ... }
    
    // Assert methods
    public void AssertValue(double expected, double tolerance = 0.001, string? message = null) { ... }
    public void AssertValueInRange(double min, double max, string? message = null) { ... }
}
```

### 1.7 ProgressControl Implementation
```csharp
public class ProgressControl : ControlBase
{
    // Read-only state
    public double GetValue() { ... }
    public async Task<double> GetValueAsync() { ... }
    public double GetMax() { ... }
    public double GetPercentage() { ... }
    public bool IsIndeterminate() { ... }
    
    // Wait methods
    public bool WaitForValue(double expected, int? timeoutMs = null) { ... }
    public bool WaitForComplete(int? timeoutMs = null) { ... }
    
    // Assert methods  
    public void AssertValue(double expected, string? message = null) { ... }
    public void AssertPercentage(double expected, double tolerance = 1.0, string? message = null) { ... }
    public void AssertComplete(string? message = null) { ... }
}
```

---

## Phase 2: Update Documentation (0.5 day)

### 2.1 New Platform Guide
Create `docs/platform-guides/playwright.md`:

```markdown
# Playwright Testing Guide

## Overview
Brinell.Html.Playwright provides Playwright-based browser automation as an 
alternative to Selenium. It offers faster execution, built-in auto-waiting,
and powerful debugging features.

## Installation
...

## Quick Start
...

## Comparison with Selenium
...

## Playwright-Specific Features
- Tracing
- Video Recording
- Network Mocking
- Multi-browser Support
...
```

### 2.2 Update docs/README.md
Add Playwright to the platform list and provide navigation.

### 2.3 Update docs/01-quick-start.md
Add Playwright as an option alongside Selenium for web testing.

### 2.4 Update docs/02-framework-overview.md
Add Brinell.Html.Playwright to the architecture diagram and package list.

---

## Phase 3: Add Playwright Instructions (0.5 day)

### 3.1 Create Instructions File
Create `.github/instructions/uitests-playwright.instructions.md`:

```markdown
# Playwright UI Test Instructions

## File Pattern
- applyTo: **/Playwright/**/*.cs, **/*PlaywrightTests*/**/*.cs

## Guidelines

### Async-First Pattern
- All Playwright operations are natively async
- Prefer async methods over sync wrappers
- Use `await` consistently in test methods

### Test Structure
```csharp
[Fact]
public async Task TestName_Scenario_ExpectedResult()
{
    // Arrange
    await LaunchBrowserAsync();
    await NavigateToPageAsync("/path");
    await WaitForBlazorReadyAsync();
    
    // Act
    var page = new MyPage(Context);
    await page.DoSomethingAsync();
    
    // Assert
    await page.AssertSomethingAsync();
}
```

### Selector Strategy
- Prefer CSS selectors: `#id`, `.class`, `[data-testid='value']`
- Avoid XPath unless necessary
- Use data-automation-id or data-testid attributes

### Auto-Waiting
- Playwright auto-waits for elements to be actionable
- Don't add explicit waits unless testing timing
- Use WaitForDisplayedAsync() only when needed

### Debugging
- Set `Headless = false` in test base for visual debugging
- Use `await StartTracingAsync()` to capture execution trace
- Check trace files with Playwright Trace Viewer

### Network Mocking
```csharp
await Context.MockRouteAsync("**/api/data", async route =>
{
    await route.FulfillAsync(new RouteFulfillOptions
    {
        Body = "{\"mocked\": true}",
        ContentType = "application/json"
    });
});
```

### Browser Selection
```csharp
protected override BrowserType BrowserType => BrowserType.Firefox;
```
```

### 3.2 Update copilot-instructions.md
If the Brinell project has a copilot-instructions.md, add reference to the new Playwright instructions file.

---

## Phase 4: Additional Sample Tests (0.5 day)

### 4.1 Add Tests for New Controls
Create test files demonstrating each control type:
- `CheckBoxTests.cs`
- `SelectTests.cs` 
- `LinkTests.cs`

### 4.2 Update Sample Blazor App
Add pages to exercise new controls:
- Add checkbox examples
- Add select/dropdown examples
- Add link navigation examples

---

## Task Summary

| Task | Duration | Status |
|------|----------|--------|
| 1.1 Create CheckBoxControl | 1 hr | Not Started |
| 1.2 Create LinkControl | 1 hr | Not Started |
| 1.3 Create SelectControl | 2 hr | Not Started |
| 1.4 Create TextAreaControl | 1 hr | Not Started |
| 1.5 Create RangeInputControl | 1 hr | Not Started |
| 1.6 Create ProgressControl | 1 hr | Not Started |
| 2.1 Create playwright.md guide | 2 hr | Not Started |
| 2.2-2.4 Update existing docs | 1 hr | Not Started |
| 3.1 Create uitests-playwright.instructions.md | 1 hr | Not Started |
| 3.2 Update copilot-instructions.md | 0.5 hr | Not Started |
| 4.1 Add sample tests | 1 hr | Not Started |
| 4.2 Update sample app | 1 hr | Not Started |
| **Total** | **~13 hours** | |

---

## Success Criteria

- [ ] All 6 remaining controls implemented with full Is/Wait/Check/Assert pattern
- [ ] Controls have both sync and async methods
- [ ] Playwright platform guide created
- [ ] Quick start updated with Playwright option
- [ ] Playwright instructions file created
- [ ] Sample tests for new controls pass
- [ ] All existing tests still pass

---

## File Changes Summary

### New Files
```
src/Brinell.Html.Playwright/Controls/
├── CheckBoxControl.cs
├── LinkControl.cs
├── SelectControl.cs
├── TextAreaControl.cs
├── RangeInputControl.cs
└── ProgressControl.cs

docs/platform-guides/
└── playwright.md

.github/instructions/
└── uitests-playwright.instructions.md

samples/Brinell.Samples.Blazor.PlaywrightTests/Tests/
├── CheckBoxTests.cs
├── SelectTests.cs
└── LinkTests.cs
```

### Modified Files
```
docs/README.md
docs/01-quick-start.md
docs/02-framework-overview.md
.github/copilot-instructions.md (if exists)
samples/Brinell.Samples.Blazor.App/... (new pages)
```

---

## Dependencies

- Brinell.Html.Playwright project (✅ Complete)
- Microsoft.Playwright v1.50.0 (✅ Installed)
- Sample Blazor app (✅ Running)
