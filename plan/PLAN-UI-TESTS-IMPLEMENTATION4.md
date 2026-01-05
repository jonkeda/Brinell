# PLAN-UI-TESTS-IMPLEMENTATION4

**Complete UI Test Implementation with Proper Page Objects**

**Created:** January 5, 2026  
**Status:** Ready for Implementation  
**Supersedes:** PLAN-UI-TESTS-IMPLEMENTATION3 (approach was incorrect)  
**Reference:** Brinell Instruction Files (`.github/instructions/`)

---

## 1. Lessons Learned from Previous Attempts

### 1.1 What Went Wrong

| Attempt | Issue | Lesson |
|---------|-------|--------|
| PLAN-1 | 122 build errors | Used incorrect API methods (FluentAssertions, wrong method names) |
| PLAN-2 | Partial fix | Fixed API issues but didn't follow page object patterns |
| PLAN-3 | Wrong approach | Deleted broken files instead of fixing them properly |

### 1.2 Critical Rules (from Instructions)

1. **Create ONE page object per page** - Don't mix multiple pages in one file
2. **Use control assertions** - `control.AssertTextEquals()` NOT FluentAssertions
3. **MAUI uses sync methods** - `Click()`, `GetText()`, `IsVisible()`
4. **Blazor uses sync methods too** - The framework handles async internally
5. **Page objects define controls** - Tests use page objects, not raw selectors
6. **Wait for page display** - Always call `WaitForDisplayed()` before interactions

### 1.3 Framework API Summary

**MAUI Controls (from `Brinell.Maui.Controls`):**
- `ButtonControl`, `LabelControl`, `EntryControl`, `EditorControl`
- `SwitchControl`, `CheckBoxControl`
- `SliderControl`, `ProgressBarControl`
- `PickerControl`, `DatePickerControl`, `TimePickerControl`
- `ActivityIndicatorControl`, `ScrollViewControl`

**Blazor/HTML Controls (from `Brinell.Html.Controls`):**
- `ButtonControl`, `LabelControl`, `LinkControl`
- `TextInputControl`, `TextAreaControl`
- `CheckBoxControl`, `SelectControl`
- `RangeInputControl`, `ProgressControl`
- `TableControl`, `ListControl`

### 1.4 Verified Control Methods

All controls inherit from `ControlBase` with these methods:

```csharp
// State checks (immediate, no wait)
bool IsExists()
bool IsVisible()
bool IsEnabled()
string GetText()

// Wait methods (poll until condition or timeout)
bool WaitExists(bool expected = true, int? timeoutMs = null)
bool WaitVisible(bool expected = true, int? timeoutMs = null)
bool WaitEnabled(bool expected = true, int? timeoutMs = null)

// Check methods (throw if not met, with screenshot)
void CheckExists(bool expected = true, int? timeoutMs = null)
void CheckVisible(bool expected = true, int? timeoutMs = null)

// Assert methods (test assertions with logging)
void AssertExists(string? message = null)
void AssertVisible(string? message = null)
void AssertEnabled(string? message = null)
void AssertDisabled(string? message = null)
void AssertTextEquals(string expected, string? message = null)
void AssertTextContains(string expected, string? message = null)

// Actions (MAUI)
void Click()
void Tap()
void DoubleTap()
void LongPress(int durationMs = 1000)

// Actions (HTML)
void Click()
void DoubleClick()
void RightClick()
void Hover()
```

---

## 2. Current State Assessment

### 2.1 Sample App Pages

**MAUI App Pages:**
| Page | File | AutomationId |
|------|------|--------------|
| Main | `MainPage.xaml` | `MainPage` |
| Dashboard | `DashboardPage.xaml` | `DashboardPage` |
| DataGrid | `DataGridPage.xaml` | `DataGridPage` |
| UserForm | `UserFormPage.xaml` | `UserFormPage` |
| Advanced | `AdvancedPage.xaml` | `AdvancedPage` |
| MediaGallery | `MediaGalleryPage.xaml` | `MediaGalleryPage` |
| Navigation | `NavigationDemoPage.xaml` | `NavigationDemoPage` |
| Validation | `ValidationPage.xaml` | `ValidationPage` |

**Blazor App Pages:**
| Page | File | Selector |
|------|------|----------|
| Home | `Index.razor` | `#home-page` or `#page-title` |
| Counter | `Counter.razor` | `#counter-title` |
| Login | `Login.razor` | `#login-form` |
| Dashboard | `Dashboard.razor` | `#dashboard` |
| DataTable | `DataTable.razor` | `#data-table` |
| FormControls | `FormControls.razor` | `#form-controls` |
| UserForm | `UserForm.razor` | `#user-form` |
| Navigation | `Navigation.razor` | `#navigation` |
| MediaGallery | `MediaGallery.razor` | `#media-gallery` |
| Advanced | `Advanced.razor` | `#advanced` |
| Validation | `Validation.razor` | `#validation` |

### 2.2 Existing Test Projects

**MAUI UITests (`Brinell.Samples.Maui.UITests`):**
- `Pages/MainPageObject.cs` - Exists ✅
- `Tests/` - 5 test files exist, need review

**Blazor UITests (`Brinell.Samples.Blazor.UITests`):**
- `PageObjects/` - 4 page objects exist (CounterPage, DashboardPage, HomePage, LoginPage)
- `Tests/` - 4 test files exist, need review

### 2.3 What Needs to be Done

| Task | MAUI | Blazor |
|------|------|--------|
| Review existing page objects | 1 file | 4 files |
| Create missing page objects | 7 files | 7 files |
| Review existing tests | 5 files | 4 files |
| Fix assertion patterns | Remove FluentAssertions | Remove FluentAssertions |
| Create missing test categories | ~4 files | ~4 files |

---

## 3. Implementation Plan

### Phase 1: Audit and Fix Existing Code

**Goal:** Get both projects building with correct patterns

#### 1.1 MAUI - Review and Fix

1. **Review `MainPageObject.cs`** - Check control types and patterns
2. **Review all test files** - Replace FluentAssertions with control assertions
3. **Build and verify** - 0 errors

#### 1.2 Blazor - Review and Fix

1. **Review 4 existing page objects** - Check control types and selectors
2. **Review 4 existing test files** - Replace FluentAssertions with control assertions
3. **Build and verify** - 0 errors

### Phase 2: Create Missing Page Objects

**One page object per sample app page**

#### 2.1 MAUI Page Objects (7 new files)

| File | Page | Key Controls |
|------|------|--------------|
| `DashboardPageObject.cs` | DashboardPage | Labels, Buttons, RefreshView |
| `DataGridPageObject.cs` | DataGridPage | CollectionView, SearchBar |
| `UserFormPageObject.cs` | UserFormPage | Entries, Pickers, Buttons |
| `AdvancedPageObject.cs` | AdvancedPage | Frame, SwipeView, Gestures |
| `MediaGalleryPageObject.cs` | MediaGalleryPage | Image, WebView |
| `NavigationPageObject.cs` | NavigationDemoPage | Shell navigation |
| `ValidationPageObject.cs` | ValidationPage | Entries with validation |

#### 2.2 Blazor Page Objects (7 new files)

| File | Page | Key Controls |
|------|------|--------------|
| `DataTablePage.cs` | DataTable.razor | Table, Search, Pagination |
| `FormControlsPage.cs` | FormControls.razor | CheckBox, Select, Range |
| `UserFormPage.cs` | UserForm.razor | Inputs, Buttons |
| `NavigationPage.cs` | Navigation.razor | NavMenu, Links |
| `MediaGalleryPage.cs` | MediaGallery.razor | Images |
| `AdvancedPage.cs` | Advanced.razor | Interaction elements |
| `ValidationPage.cs` | Validation.razor | Validated inputs |

### Phase 3: Create Comprehensive Tests

**Organized by control category**

#### 3.1 MAUI Test Files

| File | Tests | Controls Covered |
|------|-------|-----------------|
| `CounterTests.cs` | 6 | Button, Label (increment/decrement) |
| `TextInputTests.cs` | 8 | Entry, Editor |
| `ToggleControlTests.cs` | 8 | Switch, CheckBox |
| `SliderTests.cs` | 6 | Slider, ProgressBar |
| `ActivityIndicatorTests.cs` | 4 | ActivityIndicator |
| `PickerTests.cs` | 6 | Picker, DatePicker, TimePicker |
| `NavigationTests.cs` | 6 | Page navigation |
| `FormValidationTests.cs` | 6 | Form with validation |
| **TOTAL** | **~50** | |

#### 3.2 Blazor Test Files

| File | Tests | Controls Covered |
|------|-------|-----------------|
| `CounterTests.cs` | 6 | Button, Label |
| `LoginTests.cs` | 6 | Input, Button, validation |
| `NavigationTests.cs` | 6 | Links, navigation |
| `TableTests.cs` | 8 | Table, pagination |
| `FormControlTests.cs` | 8 | CheckBox, Select, Range |
| `TextInputTests.cs` | 6 | TextInput, TextArea |
| **TOTAL** | **~40** | |

---

## 4. Page Object Templates

### 4.1 MAUI Page Object Template

```csharp
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Samples.Maui.UITests.Pages;

/// <summary>
/// Page object for [PageName].
/// </summary>
public class [PageName]PageObject : PageBase
{
    public override string AutomationId => "[PageAutomationId]";
    
    public [PageName]PageObject(AppiumTestContext context) : base(context) { }
    
    // ═══════════════════════════════════════════════════════════════
    // CONTROLS
    // ═══════════════════════════════════════════════════════════════
    
    public LabelControl TitleLabel => new(_context, this, "TitleLabel");
    public ButtonControl ActionButton => new(_context, this, "ActionButton");
    // ... more controls
    
    // ═══════════════════════════════════════════════════════════════
    // PAGE DETECTION
    // ═══════════════════════════════════════════════════════════════
    
    public override bool IsDisplayed()
    {
        return TitleLabel.IsVisible();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════
    
    public [PageName]PageObject DoAction()
    {
        Log("DoAction()");
        ActionButton.Click();
        return this;
    }
}
```

### 4.2 Blazor Page Object Template

```csharp
using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for [PageName].
/// </summary>
public class [PageName]Page : PageBase
{
    public override string AutomationId => "#page-selector";
    
    // ═══════════════════════════════════════════════════════════════
    // CONTROLS - Initialize in constructor
    // ═══════════════════════════════════════════════════════════════
    
    public LabelControl PageTitle { get; }
    public ButtonControl ActionButton { get; }
    // ... more controls
    
    public [PageName]Page(SeleniumTestContext context) : base(context)
    {
        PageTitle = new LabelControl(context, this, "#page-title");
        ActionButton = new ButtonControl(context, this, "#action-btn");
    }
    
    // ═══════════════════════════════════════════════════════════════
    // PAGE DETECTION
    // ═══════════════════════════════════════════════════════════════
    
    public override bool IsDisplayed()
    {
        return PageTitle.IsVisible();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════
    
    public [PageName]Page ClickAction()
    {
        Log("ClickAction()");
        ActionButton.Click();
        return this;
    }
}
```

### 4.3 Test File Template

```csharp
using Xunit;
using Brinell.Samples.[Platform].UITests.Pages;  // or PageObjects

namespace Brinell.Samples.[Platform].UITests.Tests;

public class [Feature]Tests : [Platform]TestBase
{
    [Fact]
    public void Control_Action_ExpectedResult()
    {
        // Arrange
        var page = new [PageName]Page(_context);
        page.WaitForDisplayed();
        
        // Act
        page.SomeControl.Click();
        
        // Assert - USE CONTROL ASSERTIONS
        page.ResultLabel.AssertTextEquals("Expected");
    }
}
```

---

## 5. Detailed Task Breakdown

### Phase 1: Audit and Fix (Day 1)

| # | Task | Est. Time |
|---|------|-----------|
| 1.1 | Read MainPageObject.cs (MAUI) | 5 min |
| 1.2 | Read all MAUI test files | 15 min |
| 1.3 | Fix MAUI tests - use control assertions | 30 min |
| 1.4 | Build MAUI tests, verify 0 errors | 5 min |
| 1.5 | Read 4 Blazor page objects | 10 min |
| 1.6 | Read all Blazor test files | 15 min |
| 1.7 | Fix Blazor tests - use control assertions | 30 min |
| 1.8 | Build Blazor tests, verify 0 errors | 5 min |
| **TOTAL** | | **~115 min** |

### Phase 2: Create Page Objects (Day 2-3)

| # | Task | Est. Time |
|---|------|-----------|
| 2.1 | Read MainPage.xaml - identify AutomationIds | 10 min |
| 2.2 | Create/update MainPageObject.cs | 15 min |
| 2.3 | Read DashboardPage.xaml | 10 min |
| 2.4 | Create DashboardPageObject.cs | 15 min |
| 2.5 | Read DataGridPage.xaml | 10 min |
| 2.6 | Create DataGridPageObject.cs | 15 min |
| 2.7 | Read UserFormPage.xaml | 10 min |
| 2.8 | Create UserFormPageObject.cs | 15 min |
| 2.9-2.12 | Create remaining 4 MAUI page objects | 60 min |
| 2.13 | Read Blazor .razor files for selectors | 30 min |
| 2.14-2.20 | Create 7 Blazor page objects | 105 min |
| **TOTAL** | | **~295 min** |

### Phase 3: Create/Fix Tests (Day 4-5)

| # | Task | Est. Time |
|---|------|-----------|
| 3.1 | Fix/complete CounterTests.cs (MAUI) | 20 min |
| 3.2 | Fix/complete TextInputTests.cs (MAUI) | 20 min |
| 3.3 | Fix/complete ToggleControlTests.cs (MAUI) | 20 min |
| 3.4 | Fix/complete SliderTests.cs (MAUI) | 20 min |
| 3.5 | Create PickerTests.cs (MAUI) | 25 min |
| 3.6 | Create NavigationTests.cs (MAUI) | 25 min |
| 3.7 | Fix CounterTests.cs (Blazor) | 20 min |
| 3.8 | Fix LoginTests.cs (Blazor) | 20 min |
| 3.9 | Fix NavigationTests.cs (Blazor) | 20 min |
| 3.10 | Fix TableTests.cs (Blazor) | 25 min |
| 3.11 | Create FormControlTests.cs (Blazor) | 25 min |
| 3.12 | Create TextInputTests.cs (Blazor) | 20 min |
| 3.13 | Final build verification | 10 min |
| **TOTAL** | | **~270 min** |

---

## 6. File Structure

### 6.1 Target MAUI Structure

```
samples/Brinell.Samples.Maui.UITests/
├── Brinell.Samples.Maui.UITests.csproj
├── MauiTestBase.cs
├── xunit.runner.json
├── Pages/
│   ├── MainPageObject.cs             ← Review/Fix
│   ├── DashboardPageObject.cs        ← Create
│   ├── DataGridPageObject.cs         ← Create
│   ├── UserFormPageObject.cs         ← Create
│   ├── AdvancedPageObject.cs         ← Create
│   ├── MediaGalleryPageObject.cs     ← Create
│   ├── NavigationPageObject.cs       ← Create
│   └── ValidationPageObject.cs       ← Create
└── Tests/
    ├── CounterTests.cs               ← Review/Fix
    ├── TextInputTests.cs             ← Review/Fix
    ├── ToggleControlTests.cs         ← Review/Fix
    ├── SliderTests.cs                ← Review/Fix
    ├── ActivityIndicatorTests.cs     ← Review/Fix
    ├── PickerTests.cs                ← Create
    ├── NavigationTests.cs            ← Create
    └── FormValidationTests.cs        ← Create
```

### 6.2 Target Blazor Structure

```
samples/Brinell.Samples.Blazor.UITests/
├── Brinell.Samples.Blazor.UITests.csproj
├── xunit.runner.json
├── TestBase/
│   └── BlazorTestBase.cs
├── PageObjects/
│   ├── HomePage.cs                   ← Review/Fix
│   ├── CounterPage.cs                ← Review/Fix
│   ├── LoginPage.cs                  ← Review/Fix
│   ├── DashboardPage.cs              ← Review/Fix
│   ├── DataTablePage.cs              ← Create
│   ├── FormControlsPage.cs           ← Create
│   ├── UserFormPage.cs               ← Create
│   ├── NavigationPage.cs             ← Create
│   ├── MediaGalleryPage.cs           ← Create
│   ├── AdvancedPage.cs               ← Create
│   └── ValidationPage.cs             ← Create
└── Tests/
    ├── CounterTests.cs               ← Review/Fix
    ├── LoginTests.cs                 ← Review/Fix
    ├── NavigationTests.cs            ← Review/Fix
    ├── TableTests.cs                 ← Review/Fix
    ├── FormControlTests.cs           ← Create
    └── TextInputTests.cs             ← Create
```

---

## 7. Implementation Order

### Step-by-Step Implementation

| Step | Action | Builds After? |
|------|--------|---------------|
| **Phase 1: Audit** | | |
| 1 | Read existing MAUI files | - |
| 2 | Fix FluentAssertions in MAUI tests | ✅ |
| 3 | Read existing Blazor files | - |
| 4 | Fix FluentAssertions in Blazor tests | ✅ |
| **Phase 2: Page Objects** | | |
| 5 | Read XAML files for AutomationIds | - |
| 6 | Create/update MAUI page objects (8 files) | ✅ |
| 7 | Read Razor files for selectors | - |
| 8 | Create/update Blazor page objects (11 files) | ✅ |
| **Phase 3: Tests** | | |
| 9 | Fix/create MAUI tests (8 files) | ✅ |
| 10 | Fix/create Blazor tests (6 files) | ✅ |
| 11 | Final verification | ✅ |

---

## 8. Success Criteria

### 8.1 Build Criteria
- [ ] MAUI UITests builds with 0 errors
- [ ] Blazor UITests builds with 0 errors

### 8.2 Pattern Criteria
- [ ] No FluentAssertions anywhere
- [ ] All tests use control assertions (`.AssertTextEquals()`, etc.)
- [ ] One page object per sample app page
- [ ] All page objects inherit from correct base class
- [ ] All tests follow Arrange/Act/Assert pattern

### 8.3 Coverage Criteria
- [ ] MAUI: ~50 tests covering all control types
- [ ] Blazor: ~40 tests covering all control types
- [ ] Each page has corresponding page object
- [ ] Each control type has at least one test

### 8.4 Quality Criteria
- [ ] Tests are independent (no order dependencies)
- [ ] Tests use `WaitForDisplayed()` before interactions
- [ ] Page objects have workflow methods for common operations
- [ ] Tests have meaningful names: `Control_Action_ExpectedResult`

---

## 9. Key API Reference

### 9.1 MAUI Control Types

| Control | Usage |
|---------|-------|
| `ButtonControl` | `new(_context, this, "AutomationId")` |
| `LabelControl` | `new(_context, this, "AutomationId")` |
| `EntryControl` | Text input with `Enter()`, `Clear()`, `ClearAndEnter()` |
| `EditorControl` | Multi-line text with `Enter()`, `Clear()` |
| `SwitchControl` | `Toggle()`, `SetOn()`, `SetOff()`, `IsOn()` |
| `CheckBoxControl` | `Toggle()`, `Check()`, `Uncheck()`, `IsChecked()` |
| `SliderControl` | `SetValue()`, `GetValue()` |
| `ProgressBarControl` | `GetProgress()` |
| `PickerControl` | `SelectByIndex()`, `SelectByText()`, `GetSelectedText()` |
| `DatePickerControl` | `SetDate()`, `GetDate()` |
| `TimePickerControl` | `SetTime()`, `GetTime()` |
| `ActivityIndicatorControl` | `IsRunning()` |
| `ScrollViewControl` | `ScrollToTop()`, `ScrollToBottom()` |

### 9.2 Blazor Control Types

| Control | Usage |
|---------|-------|
| `ButtonControl` | `new(context, this, "#selector")` |
| `LabelControl` | `new(context, this, "#selector")` |
| `LinkControl` | `Click()`, `GetHref()` |
| `TextInputControl` | `Enter()`, `Clear()`, `ClearAndEnter()` |
| `TextAreaControl` | `Enter()`, `Clear()` |
| `CheckBoxControl` | `Toggle()`, `Check()`, `Uncheck()`, `IsChecked()` |
| `SelectControl` | `SelectByIndex()`, `SelectByText()`, `GetSelectedText()` |
| `RangeInputControl` | `SetValue()`, `GetValue()` |
| `ProgressControl` | `GetProgress()` |
| `TableControl` | `GetRowCount()`, `GetCellText()` |
| `ListControl` | `GetItemCount()`, `GetItemText()` |

### 9.3 Common Assertions

```csharp
// All controls
control.AssertExists();
control.AssertVisible();
control.AssertEnabled();
control.AssertDisabled();
control.AssertTextEquals("expected");
control.AssertTextContains("partial");
control.AssertTextEmpty();
control.AssertTextNotEmpty();

// Toggle controls
toggleControl.AssertOn();      // MAUI Switch
toggleControl.AssertOff();     // MAUI Switch
checkBox.AssertChecked();
checkBox.AssertNotChecked();

// Blazor HTML-specific
control.AssertHasClass("class-name");
control.AssertAttribute("attr", "value");
```

---

## 10. Notes

### 10.1 FluentAssertions Removal Pattern

Find and replace:
```csharp
// ❌ WRONG
using FluentAssertions;
...
result.Should().BeTrue();
text.Should().Be("expected");
count.Should().BeGreaterThan(0);

// ✅ CORRECT
// (no FluentAssertions import)
...
Assert.True(result);
Assert.Equal("expected", text);
Assert.True(count > 0);

// ✅ BETTER - use control assertions
control.AssertVisible();
control.AssertTextEquals("expected");
```

### 10.2 Page Object Constructor Pattern

**MAUI:**
```csharp
public MainPageObject(AppiumTestContext context) : base(context) { }
// Controls: new ControlType(_context, this, "AutomationId")
```

**Blazor:**
```csharp
public CounterPage(SeleniumTestContext context) : base(context)
{
    // Initialize properties in constructor
    CounterTitle = new LabelControl(context, this, "#counter-title");
}
```

### 10.3 Test Base Classes

```csharp
// MAUI
public class MyTests : MauiUITestBase // or MauiTestBase
{
    protected AppiumTestContext _context;
}

// Blazor  
public class MyTests : BlazorTestBase // or HtmlUITestBase
{
    protected SeleniumTestContext _context;
}
```

---

## 11. Estimated Timeline

| Phase | Duration | Output |
|-------|----------|--------|
| Phase 1: Audit & Fix | 2 hours | Both projects building |
| Phase 2: Page Objects | 5 hours | 19 page objects total |
| Phase 3: Tests | 4.5 hours | ~90 tests total |
| **TOTAL** | **~11.5 hours** | **Fully functional test suites** |

---

**Ready for Implementation**

*Plan created: January 5, 2026*
*Based on: Brinell Instruction Files, SPEC-006, Previous Plans*
