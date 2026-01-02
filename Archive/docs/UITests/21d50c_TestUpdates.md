# 21d50c: Tests Requiring Updates

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Previous:** [21d50b_Solutions.md](21d50b_Solutions.md)  
**Next:** [21d50d_ObsoleteCode.md](21d50d_ObsoleteCode.md)  
**Created:** December 2025

---

## Overview

This document identifies all tests that need to be updated to follow the corrected patterns from 21d50b.

---

## Test Files Requiring Updates

### 1. ModelManagementCrudTests.cs

**Location:** `Sources/Tests/Oravey.Tools.Wpf.UITests/Tests/ModelManagementCrudTests.cs`

| Test Method | Issue | Required Change |
|-------------|-------|-----------------|
| `AddModel_FillForm_Save_ModelAppearsInList` | Uses `page.DeleteUserModel()` with auto-dismiss | Use explicit MessageBoxDialog handling |
| `EditModel_ModifyName_Save_NameUpdatedInList` | Uses `page.DeleteUserModel()` with auto-dismiss | Use explicit MessageBoxDialog handling |
| `EditModel_Cancel_NoChanges` | Uses `page.DeleteUserModel()` with auto-dismiss | Use explicit MessageBoxDialog handling |
| `DeleteModel_ConfirmYes_ModelRemovedFromList` | Uses `page.DeleteUserModel()` with auto-dismiss | Use explicit MessageBoxDialog handling |
| All tests | Uses `Wait(500)` throughout | Replace with WaitFor conditions |

**Example Current → Updated:**
```csharp
// BEFORE
page.DeleteUserModel(testModelName);
Wait(500);

// AFTER
var item = page.GetUserModelByName(testModelName);
item.DeleteButton.Click();
var confirmDialog = new MessageBoxDialog(Context);
confirmDialog.WaitForDisplayed();
confirmDialog.ClickYes();
Context.WaitFor(() => !page.UserModelNames.Contains(testModelName), description: "model deleted");
```

---

### 2. ModelManagementViewTests.cs

**Location:** `Sources/Tests/Oravey.Tools.Wpf.UITests/Tests/ModelManagementViewTests.cs`

| Test Method | Issue | Required Change |
|-------------|-------|-----------------|
| `NavigateToModelManagement()` helper | Uses `Thread.Sleep(500)` | Use `page.WaitForDisplayed()` or IsBusy tracking |
| All section visibility tests | Uses `Thread.Sleep(500)` | Use proper waits |

---

### 3. ModelManagementFunctionalTests.cs

**Location:** `Sources/Tests/Oravey.Tools.Wpf.UITests/Tests/ModelManagementFunctionalTests.cs`

| Test Method | Issue | Required Change |
|-------------|-------|-----------------|
| `NavigateToModelManagement()` helper | Uses `Thread.Sleep(500)` | Use proper waits |
| `NavigateAwayAndBack()` helper | Uses `Thread.Sleep(300)`, `Thread.Sleep(500)` | Use page ready waits |
| Multiple tests | Direct `Context.ElementExists()` calls | Use ControlObject pattern |

**Specific Methods to Update:**
- Line 44: `Thread.Sleep(500)`
- Line 54: `Thread.Sleep(300)`
- Line 57: `Thread.Sleep(500)`
- Line 101, 133, 161, 217: More `Thread.Sleep()` calls
- Line 381-383: `Context.ElementExists()` calls

---

### 4. ModelManagementBugTests.cs

**Location:** `Sources/Tests/Oravey.Tools.Wpf.UITests/Tests/ModelManagementBugTests.cs`

| Issue | Required Change |
|-------|-----------------|
| Uses `Wait(500)`, `Wait(2000)` | Replace with WaitFor conditions |
| Raw PageObject method calls | Use UserModelItemControl pattern |

---

## PageObject Files Requiring Updates

### 1. ModelManagementViewPage.cs

**Location:** `Sources/Tests/Oravey.Tools.Wpf.UITests/PageObjects/ModelManagementViewPage.cs`

**Methods to Refactor:**

| Method | Issue | New Approach |
|--------|-------|--------------|
| `IsDisplayed()` | Uses `_context.ElementIsVisible()` | Use ControlBase pattern |
| `IsDefaultModelsSectionVisible()` | Uses `_context.ElementExists()` | Create section control |
| `IsUserModelsSectionVisible()` | Uses `_context.ElementExists()` | Create section control |
| `IsSystemModelsSectionVisible()` | Uses `_context.ElementExists()` | Create section control |
| `HasEditUserModelButton()` | Uses `_context.ElementExists()` | Remove - use item control |
| `HasDeleteUserModelButton()` | Uses `_context.ElementExists()` | Remove - use item control |
| `HasTestUserModelButton()` | Uses `_context.ElementExists()` | Remove - use item control |
| `HasUserModelEnabledCheckbox()` | Uses `_context.ElementExists()` | Remove - use item control |
| `HasViewSystemModelButton()` | Uses `_context.ElementExists()` | Remove - use item control |
| `HasCopySystemModelButton()` | Uses `_context.ElementExists()` | Remove - use item control |
| `HasTestSystemModelButton()` | Uses `_context.ElementExists()` | Remove - use item control |
| `GetSystemModelItemCount()` | Raw FlaUI operations | Use ItemsControl pattern |
| `GetUserModelItemCount()` | Raw FlaUI operations | Use ItemsControl pattern |
| `GetSystemModelNames()` | Raw FlaUI operations | Use ItemsControl pattern |
| `GetUserModelNames()` | Raw FlaUI operations | Use ItemsControl pattern |
| `ClickSystemModel()` | Raw FlaUI operations | Use item control |
| `ClickViewOnSystemModel()` | Raw FlaUI operations | Use item control |
| `ClickCopyOnSystemModel()` | Raw FlaUI operations | Use item control |
| `ClickTestOnSystemModel()` | Raw FlaUI operations | Use item control |
| `IsEmptyStateVisible()` | Raw FlaUI operations | Create EmptyState control |
| `IsEditButtonEnabled()` | Raw FlaUI operations | Use item control |
| `DeleteUserModel()` | Uses `ClickElementAndWaitForModal()` | Just click, let test handle dialog |
| `ClickDeleteOnUserModel()` | Uses `ClickElementAndWaitForModal()` | Just click, let test handle dialog |
| `ClickEditOnUserModel()` | Raw FlaUI operations | Use item control |
| `ClickEditOnUserModelAndWaitForModal()` | Uses `ClickElementAndWaitForModal()` | Remove entirely |
| `IsTestButtonEnabled()` | Raw FlaUI operations | Use item control |
| `GetModelStatus()` | Raw FlaUI operations | Use item control |

---

### 2. ModelEditorDialogPage.cs

**Location:** `Sources/Tests/Oravey.Tools.Wpf.UITests/PageObjects/ModelEditorDialogPage.cs`

| Method | Issue | Required Change |
|--------|-------|-----------------|
| `IsDisplayed()` | Uses `_context.ElementIsVisible()` | Use SaveButton.IsExists() or similar |
| `Save()` | Uses `Thread.Sleep(200)` | Use WaitFor SaveButton.IsEnabled |
| `EnableTextCapabilities()` | Uses `Thread.Sleep(100)` | Use checkbox state wait |

---

## New Files Required

| File | Purpose |
|------|---------|
| `MessageBoxDialog.cs` | PageObject for Windows MessageBox handling |
| `UserModelItemControl.cs` | ControlObject for user model list items |
| `SystemModelItemControl.cs` | ControlObject for system model list items |
| `ConfirmDeleteDialog.cs` | (Optional) Specialized delete confirmation dialog |

---

## Test Migration Checklist

For each test file:

- [ ] Replace all `Wait(n)` / `Thread.Sleep(n)` with WaitFor conditions
- [ ] Replace `page.DeleteUserModel()` with explicit dialog handling
- [ ] Replace `Context.ElementExists()` with control.IsExists()
- [ ] Replace index-based operations with item control pattern
- [ ] Add assertions for dialog content where appropriate
- [ ] Verify all actions throw on failure (not silent fail)

---

## Estimated Impact

| File | Lines Changed | Effort |
|------|---------------|--------|
| ModelManagementCrudTests.cs | ~100 | High |
| ModelManagementViewTests.cs | ~30 | Medium |
| ModelManagementFunctionalTests.cs | ~50 | Medium |
| ModelManagementBugTests.cs | ~20 | Low |
| ModelManagementViewPage.cs | ~200 | High |
| ModelEditorDialogPage.cs | ~20 | Low |
| **New: UserModelItemControl.cs** | ~80 | Medium |
| **New: SystemModelItemControl.cs** | ~60 | Medium |
| **New: MessageBoxDialog.cs** | ~100 | Medium |

---

*Next: [21d50d_ObsoleteCode.md](21d50d_ObsoleteCode.md)*
