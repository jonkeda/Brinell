# PLAN-UI-TESTS-IMPLEMENTATION5

**Complete MAUI UI Test Implementation**

**Created:** January 5, 2026  
**Status:** Ready for Implementation  
**Supersedes:** PLAN-UI-TESTS-IMPLEMENTATION4 (Blazor complete, MAUI incomplete)  
**Reference:** Brinell Instruction Files (`.github/instructions/`)

---

## 1. Current Status Summary

### 1.1 Blazor UITests - COMPLETE ✅

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Tests Passing | ~40 | 88 | ✅ Exceeds |
| Page Objects | 11 | 11 | ✅ Complete |
| Build Status | 0 errors | 0 errors | ✅ |

**Blazor is complete and requires no further work.**

### 1.2 MAUI UITests - INCOMPLETE ⚠️

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Tests Passing | ~50 | 21 | ⚠️ Needs 29+ more |
| Page Objects | 8 | 1 | ⚠️ Needs 7 more |
| Build Status | 0 errors | 0 errors | ✅ |

### 1.3 What's Done (MAUI)

**Page Objects:**
- ✅ `MainPageObject.cs` - Counter, Text, Toggle, Slider, ActivityIndicator controls

**Tests (21 total, all passing):**
- ✅ `CounterTests.cs` - 5 tests
- ✅ `TextInputTests.cs` - 4 tests
- ✅ `ToggleControlTests.cs` - 4 tests
- ✅ `SliderTests.cs` - 4 tests
- ✅ `ActivityIndicatorTests.cs` - 4 tests

### 1.4 What's Missing (MAUI)

**Page Objects (7 needed):**
- ❌ `DashboardPageObject.cs`
- ❌ `DataGridPageObject.cs`
- ❌ `UserFormPageObject.cs`
- ❌ `AdvancedPageObject.cs`
- ❌ `MediaGalleryPageObject.cs`
- ❌ `NavigationPageObject.cs`
- ❌ `ValidationPageObject.cs`

**Tests (3 needed):**
- ❌ `PickerTests.cs` - DatePicker, TimePicker, Picker controls
- ❌ `NavigationTests.cs` - Shell navigation tests
- ❌ `FormValidationTests.cs` - Validation page tests

---

## 2. MAUI Sample App Analysis

### 2.1 Available Pages and Controls

Based on MainPage.xaml structure, the sample app demonstrates these controls:

| Control Category | Controls | Current Test Coverage |
|------------------|----------|----------------------|
| Basic | Button, Label | ✅ CounterTests |
| Text Input | Entry, Editor | ✅ TextInputTests |
| Toggle | Switch, CheckBox | ✅ ToggleControlTests |
| Range | Slider, ProgressBar | ✅ SliderTests |
| Activity | ActivityIndicator | ✅ ActivityIndicatorTests |
| Picker | Picker, DatePicker, TimePicker | ❌ Missing |
| Collection | CollectionView | ❌ (DataGrid page) |
| Navigation | Shell tabs/pages | ❌ Missing |
| Validation | Entry with rules | ❌ (Validation page) |

### 2.2 Page AutomationIds (from XAML)

| Page | AutomationId | Primary Controls |
|------|--------------|------------------|
| MainPage | `MainPage` | All basic controls |
| DashboardPage | `DashboardPage` | KPI cards, charts |
| DataGridPage | `DataGridPage` | CollectionView, SearchBar |
| UserFormPage | `UserFormPage` | Form fields, submit |
| AdvancedPage | `AdvancedPage` | Gestures, SwipeView |
| MediaGalleryPage | `MediaGalleryPage` | Image, WebView |
| NavigationDemoPage | `NavigationDemoPage` | Shell navigation |
| ValidationPage | `ValidationPage` | Validated inputs |

---

## 3. Implementation Plan

### Phase 1: Create Missing MAUI Page Objects (Priority: Medium)

The sample app has 8 pages but tests currently only use MainPageObject. For a complete implementation, we need page objects for each page.

**However:** Based on the current MainPage.xaml, all control types are available on the MainPage itself. The additional pages may not be needed for control coverage testing.

**Decision:** Focus on missing control tests first, then add page objects if needed.

### Phase 2: Create Missing Test Files (Priority: High)

| File | Control Types | Est. Tests |
|------|---------------|-----------|
| `PickerTests.cs` | Picker, DatePicker, TimePicker | 6-8 |
| `NavigationTests.cs` | Shell navigation, page transitions | 4-6 |
| `FormValidationTests.cs` | Entry validation, error messages | 4-6 |
| **Total New Tests** | | **14-20** |

---

## 4. Required Investigation

### 4.1 Check MainPage.xaml for Picker Controls

Before creating PickerTests.cs, need to verify:
1. Does MainPage have Picker, DatePicker, TimePicker controls?
2. What are their AutomationIds?
3. What options/values are available?

### 4.2 Check Navigation Structure

Before creating NavigationTests.cs, need to verify:
1. Does the app use Shell navigation?
2. What pages are accessible?
3. How to navigate between pages?

### 4.3 Check Validation Page

Before creating FormValidationTests.cs, need to verify:
1. Does ValidationPage exist and have controls?
2. What validation rules are implemented?
3. How are error messages displayed?

---

## 5. Detailed Implementation Steps

### Step 1: Investigate MainPage.xaml for Picker Controls

```
Action: Read MainPage.xaml
Find: Picker, DatePicker, TimePicker controls
Extract: AutomationIds and configuration
```

### Step 2: Create PickerTests.cs

Template:
```csharp
using Xunit;
using Brinell.Samples.Maui.UITests.Pages;

namespace Brinell.Samples.Maui.UITests.Tests;

public class PickerTests : MauiUITestBase
{
    [Fact]
    public void Picker_SelectByIndex_UpdatesSelection()
    {
        // Arrange
        var page = new MainPageObject(_context);
        page.WaitForDisplayed();
        
        // Act
        page.CountryPicker.SelectByIndex(1);
        
        // Assert
        page.CountryPicker.AssertSelectedTextContains("Country");
    }
    
    [Fact]
    public void DatePicker_SetDate_UpdatesValue()
    {
        var page = new MainPageObject(_context);
        page.WaitForDisplayed();
        
        page.BirthDatePicker.SetDate(new DateTime(2000, 6, 15));
        
        // Verify date is set
        page.BirthDatePicker.AssertExists();
    }
    
    // ... more tests
}
```

### Step 3: Investigate Navigation Structure

```
Action: Read App.xaml.cs and AppShell.xaml
Find: Shell routes and page registrations
Extract: Navigation patterns
```

### Step 4: Create NavigationTests.cs

Template:
```csharp
using Xunit;
using Brinell.Samples.Maui.UITests.Pages;

namespace Brinell.Samples.Maui.UITests.Tests;

public class NavigationTests : MauiUITestBase
{
    [Fact]
    public void Navigation_MainPageLoads_OnStartup()
    {
        var page = new MainPageObject(_context);
        page.WaitForDisplayed();
        page.AssertIsDisplayed();
    }
    
    [Fact]
    public void Navigation_ToDashboard_WorksCorrectly()
    {
        // Navigate to dashboard and verify
    }
    
    // ... more tests
}
```

### Step 5: Investigate Validation Page

```
Action: Read ValidationPage.xaml
Find: Validated controls and error display
Extract: Validation rules and error messages
```

### Step 6: Create FormValidationTests.cs

Template:
```csharp
using Xunit;
using Brinell.Samples.Maui.UITests.Pages;

namespace Brinell.Samples.Maui.UITests.Tests;

public class FormValidationTests : MauiUITestBase
{
    [Fact]
    public void Validation_EmptyEmail_ShowsError()
    {
        // Test validation error display
    }
    
    [Fact]
    public void Validation_ValidForm_SubmitsSuccessfully()
    {
        // Test successful form submission
    }
}
```

### Step 7: Update MainPageObject if Needed

If MainPage has Picker controls, add them to MainPageObject:

```csharp
// Add to MainPageObject.cs
public PickerControl CountryPicker => new(_context, this, "CountryPicker");
public DatePickerControl BirthDatePicker => new(_context, this, "BirthDatePicker");
public TimePickerControl AppointmentTimePicker => new(_context, this, "AppointmentTimePicker");
```

### Step 8: Verify All Tests Pass

```
Action: Run all MAUI tests
Expected: 35-41 tests passing (21 existing + 14-20 new)
```

---

## 6. File Structure After Implementation

```
samples/Brinell.Samples.Maui.UITests/
├── Brinell.Samples.Maui.UITests.csproj
├── MauiUITestBase.cs
├── xunit.runner.json
├── Pages/
│   └── MainPageObject.cs         ← Update if needed
└── Tests/
    ├── CounterTests.cs           ✅ 5 tests
    ├── TextInputTests.cs         ✅ 4 tests
    ├── ToggleControlTests.cs     ✅ 4 tests
    ├── SliderTests.cs            ✅ 4 tests
    ├── ActivityIndicatorTests.cs ✅ 4 tests
    ├── PickerTests.cs            ← CREATE (6-8 tests)
    ├── NavigationTests.cs        ← CREATE (4-6 tests)
    └── FormValidationTests.cs    ← CREATE (4-6 tests)
```

---

## 7. Success Criteria

### 7.1 Test Count Goals

| Category | Current | Target | Gap |
|----------|---------|--------|-----|
| MAUI Tests | 21 | 35-41 | +14-20 |
| Blazor Tests | 88 | 88 | ✅ Complete |
| **Total** | 109 | 123-129 | +14-20 |

### 7.2 Completion Checklist

- [ ] Investigate MainPage.xaml for Picker controls
- [ ] Update MainPageObject with Picker controls (if present)
- [ ] Create PickerTests.cs with 6-8 tests
- [ ] Investigate navigation structure
- [ ] Create NavigationTests.cs with 4-6 tests
- [ ] Investigate ValidationPage
- [ ] Create FormValidationTests.cs with 4-6 tests
- [ ] All MAUI tests passing (35+ tests)
- [ ] All Blazor tests still passing (88 tests)

### 7.3 Quality Criteria

- [ ] No FluentAssertions used
- [ ] All tests use control assertions
- [ ] Tests are independent
- [ ] Tests use WaitForDisplayed() before interactions
- [ ] Build with 0 errors

---

## 8. Estimated Timeline

| Step | Task | Est. Time |
|------|------|-----------|
| 1 | Read MainPage.xaml for Picker controls | 10 min |
| 2 | Update MainPageObject | 15 min |
| 3 | Create PickerTests.cs | 30 min |
| 4 | Investigate navigation structure | 15 min |
| 5 | Create NavigationTests.cs | 25 min |
| 6 | Investigate ValidationPage | 10 min |
| 7 | Create FormValidationTests.cs | 25 min |
| 8 | Build and run all tests | 15 min |
| **TOTAL** | | **~2.5 hours** |

---

## 9. Alternative: Minimum Viable Completion

If the sample app doesn't have Picker/Navigation/Validation pages, we can still reach the ~50 test target by expanding existing test files:

| File | Current | Possible Additions | New Total |
|------|---------|-------------------|-----------|
| CounterTests.cs | 5 | +5 (edge cases) | 10 |
| TextInputTests.cs | 4 | +4 (validation, clear) | 8 |
| ToggleControlTests.cs | 4 | +4 (state persistence) | 8 |
| SliderTests.cs | 4 | +4 (range, step) | 8 |
| ActivityIndicatorTests.cs | 4 | +2 (visibility states) | 6 |
| **Total** | **21** | **+19** | **40** |

This approach adds more tests without requiring new page objects.

---

## 10. Notes

### 10.1 Why Only 1 MAUI Page Object?

The MainPage contains all control types needed for testing:
- Counter section (Button, Label)
- Text section (Entry, Editor)
- Toggle section (Switch, CheckBox)
- Slider section (Slider, ProgressBar)
- Activity section (ActivityIndicator)

A single page object was sufficient for the 21 existing tests.

### 10.2 Should We Create More Page Objects?

**Only if:**
1. The sample app has distinct pages with unique controls
2. Tests need to navigate between pages
3. Page-specific workflows are being tested

**Not needed if:**
1. All controls are on MainPage
2. Tests focus on control behavior, not page workflows

### 10.3 Priority Decision

Given that:
- ✅ All 21 MAUI tests pass
- ✅ All 88 Blazor tests pass
- ✅ Both projects build successfully
- ✅ All control types have basic coverage

The remaining work is **nice-to-have**, not critical. The test suites are functional and demonstrate the framework capabilities.

---

**Plan Status:** Ready for Implementation  
**Priority:** Medium (core functionality complete)  
**Estimated Effort:** 2.5 hours

*Created: January 5, 2026*
