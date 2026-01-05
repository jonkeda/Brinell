# PLAN-UI-TESTS-IMPLEMENTATION3

**Comprehensive UI Test Implementation - Phase 3**

**Created:** January 5, 2026  
**Status:** In Progress  
**Supersedes:** PLAN-UI-TESTS-IMPLEMENTATION2 (partial implementation)  
**Reference:** PLAN-UI-TESTS-IMPLEMENTATION (target scope: 184 tests)

---

## 1. Current State Assessment

### 1.1 MAUI Tests - Current Status

**Status: ✅ Building Successfully (96 tests)**

| File | Tests | Status |
|------|-------|--------|
| ClickTests6.cs | 8 | ✅ Fixed |
| CollectionControlTests.cs | 9 | ✅ Fixed |
| ControlStateTests6.cs | 15 | ✅ Fixed |
| CounterTests6.cs | 8 | ✅ Fixed |
| RangeControlTests6.cs | 14 | ✅ Fixed |
| SelectionControlTests.cs | 13 | ✅ Fixed |
| TextInputTests6.cs | 16 | ✅ Fixed |
| ToggleControlTests6.cs | 13 | ✅ Fixed |
| **TOTAL** | **96** | **Building** |

**Page Objects:**
| File | Status |
|------|--------|
| MainPageObject6.cs | ✅ Working |
| DataGridPageObject6.cs | ✅ Working |

### 1.2 Blazor Tests - Current Status

**Status: ❌ BROKEN (13 build errors - LabelControl not found)**

| File | Tests (Est.) | Status |
|------|--------------|--------|
| ClickTests6.cs | ~8 | ❌ Needs fix |
| ControlStateTests6.cs | ~10 | ❌ Needs fix |
| CounterTests6.cs | ~8 | ❌ Needs fix |
| DateTimeControlTests.cs | ~8 | ❌ Needs fix |
| FormControlTests.cs | ~10 | ❌ Needs fix |
| InteractionTests.cs | ~10 | ❌ Needs fix |
| MediaControlTests.cs | ~8 | ❌ Needs fix |
| NavigationTests6.cs | ~8 | ❌ Needs fix |
| NavTabTests.cs | ~8 | ❌ Needs fix |
| TableListTests.cs | ~10 | ❌ Needs fix |
| TextInputTests6.cs | ~12 | ❌ Needs fix |
| **TOTAL (est.)** | **~100** | **BROKEN** |

**Page Objects with Errors:**
| File | Issue |
|------|-------|
| AdvancedPage.cs | Uses `LabelControl` (not in Blazor) |
| DataTablePage.cs | Uses `LabelControl` (not in Blazor) |
| FormControlsPage.cs | Uses `LabelControl` (not in Blazor) |
| MediaPage.cs | Uses `LabelControl` (not in Blazor) |
| NavMenuPage.cs | Uses `LabelControl` (not in Blazor) |

### 1.3 Gap Analysis vs PLAN-UI-TESTS-IMPLEMENTATION

| Category | Target | MAUI Current | MAUI Gap | Blazor Current | Blazor Gap |
|----------|--------|--------------|----------|----------------|------------|
| Foundation | 7 | 8 (Click) | ✅ | 0 | 9 |
| Text Input | 12 | 16 | ✅ | 0 | 9 |
| Toggle | 10 | 13 | ✅ | 0 | 8 |
| Range | 13 | 14 | ✅ | 0 | 8 |
| Selection | 11 | 13 | ✅ | 0 | 9 |
| Collection | 12 | 9 | 3 missing | 0 | 9 |
| Container | 12 | 0 | 12 missing | 0 | N/A |
| Navigation | 10 | 0 | 10 missing | 0 | 7 |
| Display | 10 | 0 | 10 missing | 0 | 10 |
| Gestures | 6 | 0 | 6 missing | 0 | N/A |
| Advanced | N/A | 0 | N/A | 0 | 12 |
| **TOTAL** | **103** MAUI / **81** Blazor | **96** | ~7 | **0** | ~81 |

---

## 2. Lessons Learned

### 2.1 Critical Findings from Previous Attempts

1. **Blazor has NO `LabelControl`** - Must use `ButtonControl.GetTextAsync()` or read text differently
2. **All Blazor methods are async** - Use `*Async()` suffix pattern
3. **FluentAssertions must be removed** - Use xUnit `Assert.*` or control `Assert*Async()` methods
4. **Don't assume API exists** - Research actual source files first
5. **Page Objects must match actual app controls** - Check .razor/.xaml files

### 2.2 Verified Blazor Control APIs

Based on `src/Brinell.Blazor/ControlObject6/Controls/`:

| Control | Key Methods |
|---------|-------------|
| **ButtonControl** | `ClickAsync()`, `IsVisibleAsync()`, `IsEnabledAsync()`, `GetTextAsync()` |
| **CheckBoxControl** | `IsCheckedAsync()`, `CheckAsync()`, `UncheckAsync()`, `ToggleAsync()`, `SetCheckedAsync()`, `AssertCheckedAsync()` |
| **RadioButtonControl** | `IsCheckedAsync()`, `SelectAsync()`, `GetGroupNameAsync()` |
| **InputControl** | `GetTextAsync()`, `EnterAsync()`, `ClearAsync()`, `ClearAndEnterAsync()` |
| **TextAreaControl** | `GetTextAsync()`, `EnterAsync()`, `ClearAsync()`, `AppendAsync()` |
| **SelectControl** | `GetSelectedTextAsync()`, `SelectByTextAsync()`, `SelectByIndexAsync()`, `GetItemsAsync()` |
| **DateInputControl** | `GetValueAsync()`, `SetValueAsync()` |
| **TimeInputControl** | `GetValueAsync()`, `SetValueAsync()` |
| **RangeControl** | `GetValueAsync()`, `SetValueAsync()`, `GetMinAsync()`, `GetMaxAsync()` |
| **ProgressControl** | `GetValueAsync()`, `GetMaxAsync()`, `GetPercentageAsync()` |
| **TableControl** | `GetRowCountAsync()`, `GetCellTextAsync()`, `ClickRowAsync()` |
| **ListControl** | `GetItemCountAsync()`, `GetItemTextAsync()`, `ClickItemAsync()` |
| **NavMenuControl** | `IsOpenAsync()`, `ToggleAsync()`, `ClickItemAsync()` |
| **TabControl** | `SelectTabAsync()`, `GetSelectedTabIndexAsync()`, `GetTabCountAsync()` |
| **ImageControl** | `IsVisibleAsync()`, `GetSrcAsync()`, `GetAltAsync()` |
| **VideoControl** | `PlayAsync()`, `PauseAsync()`, `GetDurationAsync()` |
| **AudioControl** | `PlayAsync()`, `PauseAsync()` |
| **LinkControl** | `ClickAsync()`, `GetHrefAsync()`, `GetTargetAsync()` |
| **IFrameControl** | `GetSrcAsync()` |

### 2.3 Test Framework Rules

1. **xUnit Assert** for simple assertions: `Assert.True()`, `Assert.Equal()`, `Assert.NotNull()`
2. **Control Assert methods** for state: `AssertVisibleAsync()`, `AssertCheckedAsync()`, `AssertTextAsync()`
3. **NO FluentAssertions** - Remove `using FluentAssertions;` and `.Should().*` patterns
4. **Async tests** for Blazor: `[Fact] public async Task TestName()`

---

## 3. Implementation Plan

### Phase 1: Fix Blazor Build Errors (Priority 1)

**Goal:** Get Blazor tests building (0 errors)

#### 3.1.1 Fix Page Objects - Remove LabelControl

| File | Fix Required |
|------|--------------|
| AdvancedPage.cs | Replace `LabelControl` with `ButtonControl` or remove |
| DataTablePage.cs | Replace `LabelControl` with `ButtonControl` or remove |
| FormControlsPage.cs | Replace `LabelControl` with `ButtonControl` or remove |
| MediaPage.cs | Replace `LabelControl` with `ButtonControl` or remove |
| NavMenuPage.cs | Replace `LabelControl` with `ButtonControl` or remove |

**Pattern to use:**
```csharp
// Instead of: public LabelControl StatusLabel => new(...);
// Use ButtonControl to read text or just verify existence:
public ButtonControl StatusButton => new(Context, "StatusButton", this);
// Or create a generic element control if just reading text
```

#### 3.1.2 Fix Test Files - Remove FluentAssertions

Replace all FluentAssertions patterns:
- `using FluentAssertions;` → remove
- `.Should().BeTrue()` → `Assert.True(...)`
- `.Should().Be(expected)` → `Assert.Equal(expected, actual)`
- `.Should().BeNull()` → `Assert.Null(...)`
- `.Should().NotBeNull()` → `Assert.NotNull(...)`

### Phase 2: Complete Missing MAUI Tests (Priority 2)

#### 3.2.1 Container Control Tests

**File:** `Tests/ContainerControlTests6.cs`

Need to verify which container controls exist in sample app:

| Control | Sample App Location | Tests |
|---------|---------------------|-------|
| ScrollView | MainPage (MainScrollView) | 4 |
| Frame | AdvancedPage | 2 |
| SwipeView | DataGridPage | 3 |
| RefreshView | DataGridPage | 3 |

**Total: ~12 tests**

#### 3.2.2 Navigation Control Tests

**File:** `Tests/NavigationControlTests6.cs`

| Control | Tests |
|---------|-------|
| Shell FlyoutItem navigation | 4 |
| TabBar navigation | 4 |
| Back navigation | 2 |

**Total: ~10 tests**

#### 3.2.3 Display Control Tests

**File:** `Tests/DisplayControlTests6.cs`

| Control | Sample App Location | Tests |
|---------|---------------------|-------|
| ActivityIndicator | MainPage | 4 |
| Image | MediaGalleryPage | 3 |
| WebView | MediaGalleryPage | 3 |

**Total: ~10 tests**

#### 3.2.4 Gesture Control Tests

**File:** `Tests/GestureControlTests6.cs`

| Gesture | Sample App Location | Tests |
|---------|---------------------|-------|
| TapGesture | AdvancedPage | 2 |
| PanGesture | AdvancedPage | 2 |
| SwipeGesture | AdvancedPage | 2 |

**Total: ~6 tests**

### Phase 3: Complete Blazor Tests (Priority 3)

#### 3.3.1 Fix and Verify Existing Tests

Once build errors are fixed, verify existing test files work:

| File | Target Tests |
|------|--------------|
| ClickTests6.cs | 8 |
| ControlStateTests6.cs | 10 |
| CounterTests6.cs | 8 |
| TextInputTests6.cs | 12 |

#### 3.3.2 Complete Missing Test Categories

| Category | File | Tests |
|----------|------|-------|
| Toggle | ToggleControlTests6.cs (create) | 8 |
| Range | RangeControlTests6.cs (create) | 8 |
| Selection | SelectionControlTests6.cs (create) | 9 |
| Table/List | TableListTests.cs (fix) | 10 |
| Navigation | NavigationTests6.cs (fix) | 7 |
| Media | MediaControlTests.cs (fix) | 10 |
| Advanced | InteractionTests.cs (fix) | 12 |

---

## 4. Detailed Task Breakdown

### 4.1 Phase 1 Tasks (Blazor Build Fix)

| # | Task | Est. Time |
|---|------|-----------|
| 1.1 | Read AdvancedPage.cs - identify LabelControl usage | 5 min |
| 1.2 | Fix AdvancedPage.cs - replace/remove LabelControl | 10 min |
| 1.3 | Fix DataTablePage.cs - replace/remove LabelControl | 10 min |
| 1.4 | Fix FormControlsPage.cs - replace/remove LabelControl | 10 min |
| 1.5 | Fix MediaPage.cs - replace/remove LabelControl | 10 min |
| 1.6 | Fix NavMenuPage.cs - replace/remove LabelControl | 10 min |
| 1.7 | Build and verify 0 errors | 5 min |
| 1.8 | Fix any test files using LabelControl | 15 min |
| 1.9 | Remove FluentAssertions from all test files | 30 min |
| 1.10 | Final build verification | 5 min |
| **TOTAL** | | **~110 min** |

### 4.2 Phase 2 Tasks (MAUI Completion)

| # | Task | Est. Time |
|---|------|-----------|
| 2.1 | Research AdvancedPage.xaml controls | 10 min |
| 2.2 | Create AdvancedPageObject6.cs | 15 min |
| 2.3 | Research NavigationDemoPage.xaml controls | 10 min |
| 2.4 | Create NavigationPageObject6.cs | 15 min |
| 2.5 | Research MediaGalleryPage.xaml controls | 10 min |
| 2.6 | Create MediaPageObject6.cs | 15 min |
| 2.7 | Create ContainerControlTests6.cs | 30 min |
| 2.8 | Create NavigationControlTests6.cs | 25 min |
| 2.9 | Create DisplayControlTests6.cs | 25 min |
| 2.10 | Create GestureControlTests6.cs | 20 min |
| 2.11 | Build and verify | 5 min |
| **TOTAL** | | **~180 min** |

### 4.3 Phase 3 Tasks (Blazor Completion)

| # | Task | Est. Time |
|---|------|-----------|
| 3.1 | Verify/fix ClickTests6.cs | 15 min |
| 3.2 | Verify/fix ControlStateTests6.cs | 15 min |
| 3.3 | Verify/fix CounterTests6.cs | 15 min |
| 3.4 | Verify/fix TextInputTests6.cs | 15 min |
| 3.5 | Create ToggleControlTests6.cs | 20 min |
| 3.6 | Create RangeControlTests6.cs | 20 min |
| 3.7 | Create SelectionControlTests6.cs | 20 min |
| 3.8 | Fix TableListTests.cs | 20 min |
| 3.9 | Fix NavigationTests6.cs | 20 min |
| 3.10 | Fix MediaControlTests.cs | 20 min |
| 3.11 | Fix InteractionTests.cs | 20 min |
| 3.12 | Build and verify | 5 min |
| **TOTAL** | | **~205 min** |

---

## 5. File Structure

### 5.1 Final MAUI Structure

```
samples/Brinell.Samples.Maui.UITests.ControlObject6/
├── Pages/
│   ├── MainPageObject6.cs              ✅ Exists
│   ├── DataGridPageObject6.cs          ✅ Exists
│   ├── AdvancedPageObject6.cs          ❌ To Create
│   ├── NavigationPageObject6.cs        ❌ To Create
│   └── MediaPageObject6.cs             ❌ To Create
└── Tests/
    ├── ClickTests6.cs                  ✅ 8 tests
    ├── CollectionControlTests.cs       ✅ 9 tests
    ├── ControlStateTests6.cs           ✅ 15 tests
    ├── CounterTests6.cs                ✅ 8 tests
    ├── RangeControlTests6.cs           ✅ 14 tests
    ├── SelectionControlTests.cs        ✅ 13 tests
    ├── TextInputTests6.cs              ✅ 16 tests
    ├── ToggleControlTests6.cs          ✅ 13 tests
    ├── ContainerControlTests6.cs       ❌ To Create (~12 tests)
    ├── NavigationControlTests6.cs      ❌ To Create (~10 tests)
    ├── DisplayControlTests6.cs         ❌ To Create (~10 tests)
    └── GestureControlTests6.cs         ❌ To Create (~6 tests)

CURRENT: 96 tests
TARGET:  ~134 tests
```

### 5.2 Final Blazor Structure

```
samples/Brinell.Samples.Blazor.UITests.ControlObject6/
├── PageObjects/
│   ├── HomePage6.cs                    ✅ Exists
│   ├── CounterPage6.cs                 ✅ Exists
│   ├── LoginPage6.cs                   ✅ Exists
│   ├── FormControlsPage.cs             ⚠️ Needs Fix (LabelControl)
│   ├── DataTablePage.cs                ⚠️ Needs Fix (LabelControl)
│   ├── AdvancedPage.cs                 ⚠️ Needs Fix (LabelControl)
│   ├── NavMenuPage.cs                  ⚠️ Needs Fix (LabelControl)
│   └── MediaPage.cs                    ⚠️ Needs Fix (LabelControl)
└── Tests/
    ├── ClickTests6.cs                  ⚠️ Needs Fix
    ├── ControlStateTests6.cs           ⚠️ Needs Fix
    ├── CounterTests6.cs                ⚠️ Needs Fix
    ├── DateTimeControlTests.cs         ⚠️ Needs Fix
    ├── FormControlTests.cs             ⚠️ Needs Fix
    ├── InteractionTests.cs             ⚠️ Needs Fix
    ├── MediaControlTests.cs            ⚠️ Needs Fix
    ├── NavigationTests6.cs             ⚠️ Needs Fix
    ├── NavTabTests.cs                  ⚠️ Needs Fix
    ├── TableListTests.cs               ⚠️ Needs Fix
    ├── TextInputTests6.cs              ⚠️ Needs Fix
    ├── ToggleControlTests6.cs          ❌ To Create
    ├── RangeControlTests6.cs           ❌ To Create
    └── SelectionControlTests6.cs       ❌ To Create

CURRENT: 0 (build broken)
TARGET:  ~81 tests
```

---

## 6. Implementation Order

### Recommended Order:

| Step | Task | Tests Added | Cumulative |
|------|------|-------------|------------|
| **Phase 1: Blazor Build Fix** | | | |
| 1 | Fix Blazor PageObjects (remove LabelControl) | 0 | 96 MAUI |
| 2 | Remove FluentAssertions from Blazor tests | 0 | 96 MAUI |
| 3 | Verify Blazor build succeeds | 0 | 96 MAUI |
| **Phase 2: MAUI Completion** | | | |
| 4 | Create AdvancedPageObject6.cs | 0 | 96 MAUI |
| 5 | Create ContainerControlTests6.cs | 12 | 108 MAUI |
| 6 | Create NavigationPageObject6.cs | 0 | 108 MAUI |
| 7 | Create NavigationControlTests6.cs | 10 | 118 MAUI |
| 8 | Create MediaPageObject6.cs | 0 | 118 MAUI |
| 9 | Create DisplayControlTests6.cs | 10 | 128 MAUI |
| 10 | Create GestureControlTests6.cs | 6 | 134 MAUI |
| **Phase 3: Blazor Completion** | | | |
| 11 | Fix existing Blazor test files | ~60 | 134 MAUI + ~60 Blazor |
| 12 | Create missing Blazor test files | ~21 | 134 MAUI + ~81 Blazor |
| **TOTAL** | | **~215 tests** | |

---

## 7. Success Criteria

### 7.1 Phase 1 Complete When:
- [ ] Blazor project builds with 0 errors
- [ ] All PageObjects use only valid control types
- [ ] No FluentAssertions in any test file

### 7.2 Phase 2 Complete When:
- [ ] MAUI builds with 0 errors
- [ ] ~134 MAUI tests exist
- [ ] All MAUI control categories have tests

### 7.3 Phase 3 Complete When:
- [ ] Blazor builds with 0 errors
- [ ] ~81 Blazor tests exist
- [ ] All Blazor control categories have tests

### 7.4 Final Success:
- [ ] Combined ~215 tests (134 MAUI + 81 Blazor)
- [ ] Both projects build successfully
- [ ] xUnit Assert used throughout (no FluentAssertions)
- [ ] All tests use verified control APIs

---

## 8. Notes

### 8.1 Key API Differences

| Concept | MAUI | Blazor |
|---------|------|--------|
| Method suffix | Sync (e.g., `IsVisible()`) | Async (e.g., `IsVisibleAsync()`) |
| Test pattern | `[Fact] public void` | `[Fact] public async Task` |
| Label/Text | `LabelControl` exists | No LabelControl - use `ButtonControl.GetTextAsync()` |
| Navigation | FlyoutItem clicks | NavLink clicks |
| Context | `MauiTestContext` | `BlazorTestContext` |
| Page base | `PageObjectBase` | `AsyncPageObjectBase` |

### 8.2 Common Patterns

**MAUI Test:**
```csharp
[Fact]
public void Control_Action_Result()
{
    _page.WaitLoaded(true);
    _page.SomeControl.DoAction();
    Assert.True(_page.SomeControl.SomeCheck());
}
```

**Blazor Test:**
```csharp
[Fact]
public async Task Control_Action_Result()
{
    await _page.WaitLoadedAsync(true);
    await _page.SomeControl.DoActionAsync();
    Assert.True(await _page.SomeControl.SomeCheckAsync());
}
```

---

## 9. Research Tasks Before Implementation

Before implementing, verify these controls exist in sample apps:

### MAUI Sample App
- [ ] AdvancedPage.xaml - Frame, SwipeView, Gestures
- [ ] NavigationDemoPage.xaml - Shell navigation controls
- [ ] MediaGalleryPage.xaml - Image, WebView controls

### Blazor Sample App
- [ ] FormControls.razor - actual control test-ids
- [ ] DataTable.razor - actual table structure
- [ ] Advanced.razor - interaction elements
- [ ] Navigation.razor - nav controls

---

**Status:** Ready to Begin Phase 1

**Estimated Total Time:** ~495 minutes (~8 hours)

---

*Plan created: January 5, 2026*
