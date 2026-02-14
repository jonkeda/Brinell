# MAUI UI Test Failures — Root Cause Analysis #2

**Date:** June 2025
**Status:** Open
**Scope:** 26 remaining test failures after RCA #1 fixes (Stepper, Picker, SearchBar, FindElement)

---

## Summary

After resolving all 5 root causes in [RCA #1](ISSUE-maui-uitest-failures-rca.md) (reducing failures from 68 → 26), this document analyzes the remaining 26 test failures across 9 root causes.

| Category | Root Causes | Tests | Fixable? |
|----------|-------------|-------|----------|
| Framework bugs | RC-1 TimePicker, RC-2 DatePicker, RC-5 Editor, RC-6 ListContainer | 14 | Yes |
| Platform limitations | RC-3 Display Controls, RC-4 WebView2 | 7 | Partial |
| Test-level issues | RC-7 Android Diagnostics, RC-8 Debug, RC-9 ListView | 5 | Skip/Fix tests |

---

## Root Cause #1: TimePicker — SetTime uses text input on a non-text control

**Tests affected (6):**
| Test | Error |
|------|-------|
| `TimePicker_SetTime_ChangesTime` | Expected: 14, Actual: 9 |
| `TimePicker_GetHours_ReturnsHours` | Expected: 10, Actual: 9 |
| `TimePicker_GetMinutes_ReturnsMinutes` | Expected: 45, Actual: 0 |
| `TimePicker_AssertTime_PassesWithCorrectTime` | RunAssert timeout — time didn't change |
| `TimePicker_AssertTime_PassesWithTolerance` | RunAssert timeout — time didn't change |
| `TimePicker_DumpElementStructure` | `LocatorNotSupportedException` on XPath |

**Status:** 🔴 Not Fixed

### Analysis

`SetTimeCore()` in `MauiTimePickerControl.cs` (line ~210) does:
```csharp
element.Click();
element.Clear();
element.SendKeys(time.ToString(@"hh\:mm"));
element.SendKeys(Keys.Enter);
```

**Problem:** Windows MAUI TimePicker renders as a WinUI `TimePickerFlyoutPresenter` — a **button** (FlyoutButton) that opens a popup with hour/minute/AM-PM spinners. It is NOT a text input field.

- `Clear()` does nothing useful (no ValuePattern on the button)
- `SendKeys("14:30")` types into whatever has focus, not into the picker
- The time stays at its initial binding value (9:00 AM from ViewModel)
- `GetTimeCore()` correctly reads via FlyoutButton Name parsing — returning `9:00` consistently

The `TimePicker_DumpElementStructure` diagnostic test fails separately because it attempts `Locator.ByXPath(".//*")` which is unsupported by FlaUI.

### Fix Strategy

**Option A (Recommended): Use ValuePattern if available**
```csharp
protected override void SetTimeCore(IMauiElement element, TimeSpan time)
{
    // WinUI TimePicker may support Value pattern
    if (element is IValuePatternElement valueElement)
    {
        valueElement.SetValue(time.ToString(@"hh\:mm"));
        return;
    }
    // ... fallback
}
```

**Option B: Interact with the flyout popup**
1. Click the FlyoutButton to open the popup
2. Find hour/minute spinner elements in the popup
3. Use ScrollPattern or keyboard arrows to set values
4. Click Accept/OK button

**Option C: Use Keyboard.Type with explicit focus management**
After clicking to open the flyout, the hour field gets focus. Use Up/Down arrow keys to set hour value, Tab to minutes, set minutes, Enter to confirm.

### Files

| File | Relevance |
|------|-----------|
| [MauiTimePickerControl.cs](../srcnew/Brinell.Maui/Controls/DateTime/MauiTimePickerControl.cs) | `SetTimeCore()` needs platform-specific rewrite |
| [TimePickerControlTests.cs](../testsnew/Brinell.Maui.UITests/Tests/DateTime/TimePickerControlTests.cs) | All tests call `SetTime()` before assertion |
| [TimePickerDiagnosticTests.cs](../testsnew/Brinell.Maui.UITests/Tests/DateTime/TimePickerDiagnosticTests.cs) | Uses unsupported XPath locator |

---

## Root Cause #2: DatePicker — Clear() throws + Min/Max not exposed via UIA

**Tests affected (4):**
| Test | Error |
|------|-------|
| `DatePicker_SetDate_ChangesDate` | Exception at `FlaUIMauiElement.Clear()` line 214 |
| `DatePicker_AssertDate_PassesWithCorrectDate` | Same Clear() exception |
| `DatePicker_GetMinimumDate_ReturnsMin` | Returns null — MinimumDate attribute not available |
| `DatePicker_GetMaximumDate_ReturnsMax` | Returns null — MaximumDate attribute not available |

**Status:** 🔴 Not Fixed

### Analysis

**SetDate failure:** `SetDateCore()` in `MauiDatePickerControl.cs` (line ~220) calls:
```csharp
element.Click();
element.Clear();  // ← THROWS HERE
element.SendKeys(date.ToString("yyyy-MM-dd"));
element.SendKeys(Keys.Enter);
```

Windows MAUI DatePicker renders as a WinUI `CalendarDatePicker`. Like TimePicker, it's a button that opens a calendar flyout — NOT a text input. `Clear()` fails because:
- `ValuePattern.IsSupported` is false (it's not an edit control)
- The fallback `Focus() + Ctrl+A + Delete` doesn't make sense for a date picker

**Min/Max failure:** `GetMinimumDate()` reads `MinimumDate` or `Minimum` UIA attributes. These are MAUI-level properties (`MinimumDate="1900-01-01"` in XAML) that WinUI's CalendarDatePicker does not expose through Windows UI Automation properties.

### Fix Strategy

**SetDate:**
- **Option A:** Use the calendar flyout — click to open, navigate month/year, click the target date
- **Option B:** If ValuePattern is available on the date text child, use it directly
- **Option C:** Convert to keyboard-based entry — some WinUI date pickers accept typed input when the text portion has focus

**Min/Max:**
- These attributes are not exposed by WinUI through UIA. Options:
  - Return null and mark tests as platform-known-limitation
  - Use custom automation properties if the app sets them
  - Hardcode fallback to MAUI metadata (not practical for a framework)

### Files

| File | Relevance |
|------|-----------|
| [MauiDatePickerControl.cs](../srcnew/Brinell.Maui/Controls/DateTime/MauiDatePickerControl.cs) | `SetDateCore()` and `GetMinimumDate/GetMaximumDate` |
| [FlaUIMauiElement.cs](../srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs) | `Clear()` at line 214 — throws on non-text controls |
| [DatePickerControlTests.cs](../testsnew/Brinell.Maui.UITests/Tests/DateTime/DatePickerControlTests.cs) | Tests calling SetDate and GetMin/Max |

---

## Root Cause #3: Display Controls — ProgressBar/ActivityIndicator not found via AutomationId

**Tests affected (3):**
| Test | Error |
|------|-------|
| `ProgressBar_IsVisible_ReturnsTrue` | `Assert.True()` Failure — `IsVisible()` returns False |
| `ActivityIndicator_IsExists_ReturnsTrue` | `Assert.True()` Failure — `IsExists()` returns False |
| `ActivityIndicator_IsVisible_ReflectsState` | `Assert.True()` Failure — `IsVisible()` returns False |

**Status:** 🟡 Investigation Needed

### Analysis

**ProgressBar** (`VolumeProgress` on MainPage):
- XAML: `<ProgressBar AutomationProperties.Name="VolumeProgress" ...>` in BasicsView.xaml
- WinUI renders `ProgressBar` as a `ProgressBar` UIA control type
- The AutomationId may not be set correctly — `AutomationProperties.Name` sets the **Name** UIA property, not **AutomationId**
- The framework locates by `AutomationId` (via `ByAutomationId` locator strategy) but the XAML may need `AutomationProperties.AutomationId` instead

**ActivityIndicator** (`WebLoadingIndicator` on MediaGalleryPage):
- XAML: `ActivityIndicator` with `AutomationProperties.Name="WebLoadingIndicator"` or similar
- WinUI renders `ActivityIndicator` as `ProgressRing` but the element may not be in the visual tree when `IsRunning="False"` (WinUI collapses inactive indicators)
- When `IsRunning` binding is false, the element doesn't exist in the UIA tree at all

**Both issues likely share the same root cause:** The XAML uses `AutomationProperties.Name` but the framework searches by `AutomationId`. WinUI maps these differently:
- `AutomationProperties.AutomationId` → UIA AutomationId property
- `AutomationProperties.Name` → UIA Name property

### Fix Strategy

1. **XAML fix:** Ensure sample app XAML uses `AutomationId="..."` (MAUI shorthand) which maps to `AutomationProperties.AutomationId`
2. **Framework fix:** Make `FindElement` also search by Name as fallback
3. **ActivityIndicator-specific:** When `IsRunning=false`, the element may be collapsed — tests should account for this

### Files

| File | Relevance |
|------|-----------|
| [ProgressBarControlTests.cs](../testsnew/Brinell.Maui.UITests/Tests/Display/ProgressBarControlTests.cs) | Test using `Page.VolumeProgress` |
| [ActivityIndicatorControlTests.cs](../testsnew/Brinell.Maui.UITests/Tests/Display/ActivityIndicatorControlTests.cs) | Tests using `Page.WebLoadingIndicator` |
| [BasicsView.xaml](../samples/Brinell.Samples.Maui.App/Views/BasicsView.xaml) | ProgressBar AutomationId definition |
| [MediaGalleryView.xaml](../samples/Brinell.Samples.Maui.App/Views/MediaGalleryView.xaml) | ActivityIndicator definition |
| [MainPage.cs](../testsnew/Brinell.Maui.UITests/Pages/MainPage.cs) | `VolumeProgress` = `Control("VolumeProgress")` |
| [MediaGalleryPage.cs](../testsnew/Brinell.Maui.UITests/Pages/MediaGalleryPage.cs) | `WebLoadingIndicator` = `ActivityIndicator("WebLoadingIndicator")` |

---

## Root Cause #4: WebView2 — Platform limitation (already documented)

**Tests affected (4):**
| Test | Error |
|------|-------|
| `WebView_IsVisible_ReturnsTrue` | `Assert.True()` — IsVisible returns False |
| `WebView_GetUrl_ReturnsUrl` | `Assert.NotNull()` — GetUrl returns null |
| `WebView_CanGoBack_ReturnsState` | `Assert.True()` — returns False |
| `WebView_CanGoForward_ReturnsState` | `Assert.True()` — returns False |

**Status:** 🟠 Platform Limitation — documented in RCA #1

### Analysis

WebView2 on Windows does not expose URL, visibility, or navigation state through standard UIA automation properties. The WebView2 UIA element exists but only as an opaque container. This is a known limitation of the WebView2 control's UIA implementation.

### Recommendation

- Skip these tests on Windows with `[Trait("Platform", "NotWindows")]` or `Skip` reason
- Or mark `WebView_IsExists` as the only valid Windows test (it passes)
- Track WebView2 UIA improvements in future WinUI SDK releases

---

## Root Cause #5: Editor Clear() — ClearWithFallback doesn't fully clear text

**Tests affected (1):**
| Test | Error |
|------|-------|
| `Editor_Clear_RemovesText` | Expected: `""`, Actual: `"to clear"` |

**Status:** 🔴 Not Fixed

### Analysis

`MauiEditorControl.ClearCore()` uses `INestedTextElement.ClearWithFallback()`:
```csharp
if (element is INestedTextElement textElement)
{
    textElement.ClearWithFallback();
    return;
}
element.Clear();
```

The test enters `"Some text to clear"` then calls `Clear()`. After clearing, `GetText()` returns `"to clear"` — the text is only partially cleared.

**Likely issue:** The `ClearWithFallback()` method's implementation may:
1. Set the Value pattern on the wrong element in the nested structure (only clearing the first edit child)
2. Use Ctrl+A+Delete but the Editor (multi-line TextBox on WinUI) may not select all text properly if there are multiple lines or the focus is wrong
3. The Editor's nested text structure means the parent Value.SetValue("") doesn't propagate to the actual text content

The remaining text `"to clear"` is the tail end of `"Some text to clear"`, suggesting Ctrl+A selected only the visible/first portion.

### Fix Strategy

1. Investigate `ClearWithFallback()` implementation in `FlaUIMauiElement.cs`
2. For Editor controls, may need to find the actual inner TextBox/RichEditBox element and clear it
3. Alternative: Triple-click to select all (works in multi-line), then Delete
4. Or use the `Document` text pattern if available on the inner element

### Files

| File | Relevance |
|------|-----------|
| [MauiEditorControl.cs](../srcnew/Brinell.Maui/Controls/Text/MauiEditorControl.cs) | `ClearCore()` override |
| [FlaUIMauiElement.cs](../srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs) | `Clear()` and `ClearWithFallback()` implementation |
| [EditorControlTests.cs](../testsnew/Brinell.Maui.UITests/Tests/Text/EditorControlTests.cs) | Clear test at line 84 |

---

## Root Cause #6: ListContainer — Task items not found or interactions fail

**Tests affected (3):**
| Test | Error |
|------|-------|
| `TaskItem_ByIndex_Exists` | `AssertExists()` on `Task_2` fails — element not found |
| `TaskList_AddTask` | Expected count: 3, Actual: 2 — add button doesn't create new item |
| `TaskItem_DeleteButton_IsClickable` | `AssertClickable()` timeout — delete button not found |

**Status:** 🔴 Not Fixed

### Analysis

`MauiListControl` uses prefix-based item discovery. The `TaskList` is configured with prefix `"Task_"` and scoped to `"TaskListFrame"`:
```csharp
TaskList = new MauiListControl<ContainerDemoPage, TaskItemContainer>(
    this, "TaskListFrame", (scope, index) => new TaskItemContainer(scope, index),
    "Task_");
```

`GetItemCount()` iterates `Task_0`, `Task_1`, `Task_2`, ... checking `IsExists()` until not found (max 100).

**Possible issues:**
1. **AutomationId pattern mismatch:** The sample app XAML items may use a different AutomationId pattern than `Task_0`, `Task_1`, etc. CollectionView/ListView on WinUI uses data templates where AutomationId binding may not produce the expected pattern.
2. **`TaskItem_ByIndex_Exists`:** Finds `Task_0` and `Task_1` (passing) but `Task_2` doesn't exist. The initial data may only have 2 items, not 3.
3. **`TaskList_AddTask`:** The `NewTaskEntry.Enter()` + `AddTaskButton.Click()` flow may work but the new item's AutomationId may not follow the `Task_` prefix pattern (e.g., it may be dynamically generated differently).
4. **`TaskItem_DeleteButton_IsClickable`:** If `Task_0` itself is not found (because only `TaskItem_ByIndex_Exists` establishes that at least index 0 exists), the DeleteButton lookup within a non-existent container scope would fail.

### Fix Strategy

1. **Diagnostic first:** Dump the UIA tree of the ContainerDemoPage to see actual AutomationIds of list items
2. **XAML check:** Verify sample app DataTemplate binds `AutomationId` correctly: `AutomationProperties.AutomationId="{Binding ..., StringFormat='Task_{0}'}"` (or similar)
3. **Framework check:** Ensure `MauiListControl.GetItemCount()` handles the case where items exist but with different naming
4. **Add task flow:** Verify the Entry/Button controls are found and the binding creates new items with correct AutomationIds

### Files

| File | Relevance |
|------|-----------|
| [ListContainerTests.cs](../testsnew/Brinell.Maui.UITests/Tests/ListContainerTests.cs) | All 3 failing tests |
| [MauiListControl.cs](../srcnew/Brinell.Maui/Controls/MauiListControl.cs) | `GetItemCount()`, `Item()` prefix-based discovery |
| [ContainerDemoPage.cs](../testsnew/Brinell.Maui.UITests/Pages/ContainerDemoPage.cs) | TaskList definition with `"Task_"` prefix |
| [TaskItemContainer.cs](../testsnew/Brinell.Maui.UITests/Containers/TaskItemContainer.cs) | Container scoping pattern |

---

## Root Cause #7: Android Diagnostic Tests — Wrong platform

**Tests affected (3):**
| Test | Error |
|------|-------|
| `Diagnostic_TestLocatorStrategies` | `FlaUIMauiDriver` constructor fails at line 51 |
| `Diagnostic_TestButtonFind` | Same constructor failure |
| `Diagnostic_DumpPageSource_Android` | Same constructor failure |

**Status:** 🟢 Not a Bug — Test Scope Issue

### Analysis

These tests are **Android-specific diagnostic tools** that:
1. Use reflection to access `_rawDriver` field expecting an `AppiumDriver`
2. Use `MobileBy.AccessibilityId`, `AndroidUIAutomator`, and Android XPath patterns
3. Test Android-specific locator strategies (resource-id, content-desc)

When run on Windows with FlaUI, the `MauiTestContext` doesn't have an `_rawDriver` field containing an `AppiumDriver`, so the reflection returns null or the constructor creates a new `FlaUIMauiDriver` that tries to launch the app again and fails.

The test class `DiagnosticTests` uses `IClassFixture<AppiumFixture>` instead of `[Collection("Appium")]`, which means it creates its own scope.

### Recommendation

- Add `[Trait("Platform", "Android")]` to these tests
- Use conditional skip: `Skip.When(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))`
- Or move to a separate Android-specific test project

---

## Root Cause #8: Debug_FindMainPage — Page AutomationId not in UIA tree

**Tests affected (1):**
| Test | Error |
|------|-------|
| `Debug_FindMainPage` | `Assert.True(elements.Count > 0)` — finds 0 elements |

**Status:** 🟢 Not a Bug — Expected Behavior

### Analysis

The test searches for `AutomationId="MainPage"` in the UIA tree. On Windows MAUI:
- Pages don't have their own UIA element with an AutomationId
- The MAUI Shell/NavigationPage structure doesn't expose individual page AutomationIds through WinUI UIA
- The page's content elements are directly under the Window, not wrapped in a "MainPage" container

The test also uses `Task.Delay(5000)` which violates anti-pattern rules.

### Recommendation

- This is a diagnostic test — mark with `Skip` reason or `[Trait("Category", "Diagnostic")]`
- The framework already handles page verification via `IsLoaded()` / `WaitReady()` patterns

---

## Root Cause #9: ListView_IsVisible — NavigateToContainerDemo fails

**Tests affected (1):**
| Test | Error |
|------|-------|
| `ListView_IsVisible_ReturnsTrue` | Exception at `AppiumFixture.NavigateToContainerDemo()` (line 71) |

**Status:** 🟡 Investigation Needed

### Analysis

`NavigateToContainerDemo()` clicks `ContainersTab` then calls `WaitReady(5000)`. The exception means either:
1. `ContainersTab.Click()` didn't navigate (tab might not exist or is already selected)
2. `WaitReady(5000)` timed out because the page title element wasn't found within 5 seconds

Note: `ListContainerTests` also calls `NavigateToContainerDemo()` in its constructor and **some** of those tests pass (GetItemCount, GetAllItems), meaning navigation works in other contexts. This suggests a test ordering/race condition — if `ListContainerTests` runs first and navigates away, `ListViewControlTests` may find the tab in a different state.

### Fix Strategy

1. Verify `ContainersTab` AutomationId exists in the MAUI Shell XAML
2. Increase `WaitReady` timeout or add retry logic
3. Consider making navigation idempotent (check if already on the page)

---

## Fix Priority Matrix

| Priority | Root Cause | Tests | Effort | Impact |
|----------|-----------|-------|--------|--------|
| **P1** | RC-1 TimePicker SetTime | 6 | High | Core feature |
| **P1** | RC-2 DatePicker SetDate/Boundaries | 4 | High | Core feature |
| **P2** | RC-5 Editor Clear | 1 | Medium | Text control reliability |
| **P2** | RC-6 ListContainer items | 3 | Medium | Container pattern |
| **P3** | RC-3 Display controls | 3 | Low-Medium | XAML/naming fix |
| **P3** | RC-9 ListView navigation | 1 | Low | Test robustness |
| **Skip** | RC-4 WebView2 | 4 | N/A | Platform limitation |
| **Skip** | RC-7 Android diagnostics | 3 | N/A | Wrong platform |
| **Skip** | RC-8 Debug test | 1 | N/A | Diagnostic only |

---

## Recommended Fix Order

1. **RC-3 Display Controls** — Likely a quick XAML AutomationId fix
2. **RC-5 Editor Clear** — Investigate ClearWithFallback, targeted fix
3. **RC-6 ListContainer** — Diagnostic dump + possible XAML fix
4. **RC-1 TimePicker** — Requires WinUI TimePicker popup interaction research
5. **RC-2 DatePicker** — Similar to TimePicker, calendar flyout interaction
6. **RC-9 ListView** — Navigation robustness improvement
7. **RC-4, RC-7, RC-8** — Mark as Skip with appropriate reasons

---

## Test Inventory

| # | Test Name | Root Cause | Category |
|---|-----------|-----------|----------|
| 1 | `TimePicker_SetTime_ChangesTime` | RC-1 | Framework |
| 2 | `TimePicker_GetHours_ReturnsHours` | RC-1 | Framework |
| 3 | `TimePicker_GetMinutes_ReturnsMinutes` | RC-1 | Framework |
| 4 | `TimePicker_AssertTime_PassesWithCorrectTime` | RC-1 | Framework |
| 5 | `TimePicker_AssertTime_PassesWithTolerance` | RC-1 | Framework |
| 6 | `TimePicker_DumpElementStructure` | RC-1 | Diagnostic |
| 7 | `DatePicker_SetDate_ChangesDate` | RC-2 | Framework |
| 8 | `DatePicker_AssertDate_PassesWithCorrectDate` | RC-2 | Framework |
| 9 | `DatePicker_GetMinimumDate_ReturnsMin` | RC-2 | Framework |
| 10 | `DatePicker_GetMaximumDate_ReturnsMax` | RC-2 | Framework |
| 11 | `ProgressBar_IsVisible_ReturnsTrue` | RC-3 | XAML/Framework |
| 12 | `ActivityIndicator_IsExists_ReturnsTrue` | RC-3 | XAML/Framework |
| 13 | `ActivityIndicator_IsVisible_ReflectsState` | RC-3 | XAML/Framework |
| 14 | `WebView_IsVisible_ReturnsTrue` | RC-4 | Platform |
| 15 | `WebView_GetUrl_ReturnsUrl` | RC-4 | Platform |
| 16 | `WebView_CanGoBack_ReturnsState` | RC-4 | Platform |
| 17 | `WebView_CanGoForward_ReturnsState` | RC-4 | Platform |
| 18 | `Editor_Clear_RemovesText` | RC-5 | Framework |
| 19 | `TaskItem_ByIndex_Exists` | RC-6 | Framework |
| 20 | `TaskList_AddTask` | RC-6 | Framework |
| 21 | `TaskItem_DeleteButton_IsClickable` | RC-6 | Framework |
| 22 | `Diagnostic_TestLocatorStrategies` | RC-7 | Wrong Platform |
| 23 | `Diagnostic_TestButtonFind` | RC-7 | Wrong Platform |
| 24 | `Diagnostic_DumpPageSource_Android` | RC-7 | Wrong Platform |
| 25 | `Debug_FindMainPage` | RC-8 | Diagnostic |
| 26 | `ListView_IsVisible_ReturnsTrue` | RC-9 | Navigation |
