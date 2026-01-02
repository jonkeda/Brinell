# 21d50a: Current UI Test Framework Issues

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Next:** [21d50b_Solutions.md](21d50b_Solutions.md)  
**Created:** December 2025

---

## Overview

This document identifies issues in the current UITestFramework implementation and the Oravey.Tools.Wpf.UITests that deviate from the established patterns documented in 21d1-21d17.

---

## Issue 1: Modal/MessageBox Handling in FlaUITestContext

**Location:** `FlaUITestContext.cs` lines 258-420

**Problem:** Modal dialog detection and dismissal logic is implemented directly in `FlaUITestContext` instead of following the PageObject/ControlObject patterns.

**Violating Methods:**
- `HasModalWindow()` - Searches desktop for modal windows
- `ClickAndWaitForModal()` - Click with background task + modal wait
- `ClickElementAndWaitForModal()` - Same with element parameter
- `DismissModalWindow()` - Finds Yes/OK buttons and clicks them

**Why It's Wrong:**
1. Per 21d9 §9.9, modal dialogs should be PageObjects with their own controls (e.g., `ConfirmDialog`)
2. The Context should not contain UI-specific business logic (what button to click)
3. MessageBox "Yes" button detection is fragile - Windows localization changes button text
4. Modal dismiss logic is hidden inside Context, making tests non-transparent

---

## Issue 2: Raw FlaUI Operations in PageObjects

**Location:** `ModelManagementViewPage.cs` lines 200-490

**Problem:** PageObject methods use raw `_context.FindElement()` and direct FlaUI operations instead of ControlObjects.

**Violating Methods:**
```csharp
GetSystemModelItemCount()    // Raw FindAllDescendants
GetUserModelItemCount()      // Raw FindAllDescendants  
GetSystemModelNames()        // Raw FindAllDescendants + Select
GetUserModelNames()          // Raw FindAllDescendants + Select
ClickSystemModel()           // Raw FindFirstDescendant + Click
DeleteUserModel()            // Raw FindAllDescendants + index matching
ClickEditOnUserModel()       // Raw FindAllDescendants + index
```

**Why It's Wrong:**
1. Per 21d6, element operations should go through ControlObjects with Is/Wait/Check/Assert patterns
2. No wait-before-action pattern - methods assume elements are ready
3. No logging through ControlBase
4. Methods silently fail if element not found (no throw, just return)

---

## Issue 3: Context Has Too Many Element Operations

**Location:** `FlaUITestContext.cs` lines 100-230

**Problem:** Context implements generic element operations that belong in ControlBase.

**Problematic Methods:**
```csharp
ElementExists(automationId)      // Should be ControlBase.IsExists()
ElementIsVisible(automationId)   // Should be ControlBase.IsVisible()
ElementIsEnabled(automationId)   // Should be ControlBase.IsEnabled()
GetElementText(automationId)     // Should be ControlBase.GetText()
ClickElement(automationId)       // Should be ButtonControl.Click()
EnterText(automationId, text)    // Should be TextBoxControl.EnterText()
ClearElement(automationId)       // Should be TextBoxControl.Clear()
```

**Why It's Wrong:**
1. Per 21d3 §3.2, ITestContext should have: Log, WaitFor, TakeScreenshot, and configuration
2. Element operations bypass the Wait/Check/Assert pattern
3. No CheckClickable() or CheckEnabled() before actions
4. Duplicates functionality already in control classes

---

## Issue 4: Missing Dialog PageObject

**Location:** Multiple test files

**Problem:** No PageObject for MessageBox/confirmation dialogs. Tests rely on `ClickElementAndWaitForModal()` which auto-dismisses.

**Evidence:**
```csharp
// In ModelManagementViewPage.DeleteUserModel():
_context.ClickElementAndWaitForModal(deleteButtons[i]);  // Auto-dismisses MessageBox

// Should be:
deleteButton.Click();
var confirmDialog = new ConfirmDeleteDialog(Context);
confirmDialog.WaitForDisplayed();
confirmDialog.ConfirmButton.Click();  // Explicit, testable
```

**Why It's Wrong:**
1. Per 21d9 §9.9.2, dialogs should be explicit PageObjects
2. Tests can't verify dialog message or make negative tests (click No)
3. No visibility into what happened during delete
4. Different MessageBox types (Yes/No, OK/Cancel) need different handling

---

## Issue 5: Thread.Sleep Usage in Tests and Pages

**Location:** Throughout test code

**Problem:** `Wait(500)`, `Thread.Sleep(200)` scattered through tests and page methods.

**Examples:**
```csharp
// ModelManagementCrudTests.cs
Wait(500);  // Wait for data to load
Wait(500);  // Wait for list refresh

// ModelEditorDialogPage.cs
System.Threading.Thread.Sleep(200);  // Allow time for WPF bindings

// ModelManagementViewPage.EnableTextCapabilities()
System.Threading.Thread.Sleep(100);  // Allow time for checkbox state change
```

**Why It's Wrong:**
1. Per 21d8, should use IsBusy tracking for async operations
2. Per 21d7, should use `WaitFor()` with conditions, not fixed delays
3. Arbitrary waits are flaky - too short fails, too long slows tests
4. No visibility into what we're actually waiting for

---

## Issue 6: Methods That Return Void and Silently Fail

**Location:** `ModelManagementViewPage.cs`

**Problem:** Action methods return void and silently fail if element not found.

**Examples:**
```csharp
public void DeleteUserModel(string modelName)
{
    // ...
    Log($"DeleteUserModel - Model '{modelName}' not found");  // Just logs and returns!
}

public void ClickEditOnUserModel(int index = 0)
{
    // ...
    if (index < editButtons.Length)  // Silently does nothing if out of range
    {
        editButtons[index].Click();
    }
}
```

**Why It's Wrong:**
1. Per 21d6 §6.13, action methods should throw on precondition failure
2. Tests pass even when delete didn't happen
3. Debugging is harder - no exception stack trace

---

## Issue 7: Inconsistent Naming

**Location:** Various page objects

**Problem:** Method names don't follow the established conventions.

**Examples:**
| Actual | Expected (per 21d9 §9.4.2) |
|--------|----------------------------|
| `HasEditUserModelButton()` | `EditUserModelButton.IsExists()` |
| `GetUserModelItemCount()` | `UserModelsList.GetItemCount()` |
| `ClickDeleteOnUserModel()` | `GetUserModelItem(index).DeleteButton.Click()` |
| `IsEditButtonEnabled()` | `GetUserModelItem(index).EditButton.IsEnabled()` |

---

## Issue 8: No Item ControlObject for List Items

**Location:** `ModelManagementViewPage.cs`

**Problem:** User model list items have multiple controls (name, edit, delete, checkbox) but no ItemControl class.

**Current Approach:**
```csharp
// Scattered FindAllDescendants throughout the page
var nameElements = container.FindAllDescendants(cf => cf.ByAutomationId("UserModelName"));
var deleteButtons = container.FindAllDescendants(cf => cf.ByAutomationId("DeleteUserModelButton"));
// Manual index correlation
```

**Expected Approach (per 21d6 §6.10):**
```csharp
public class UserModelItemControl : ControlBase
{
    public LabelControl Name { get; }
    public ButtonControl EditButton { get; }
    public ButtonControl DeleteButton { get; }
    public CheckBoxControl EnabledCheckBox { get; }
    // ...
}

// In page:
public UserModelItemControl GetUserModelItem(int index) { ... }
```

---

## Summary of Pattern Violations

| Issue | Violated Document | Severity |
|-------|-------------------|----------|
| Modal handling in Context | 21d9 §9.9 | High |
| Raw FlaUI in PageObjects | 21d6 §6.4 | High |
| Context has element ops | 21d3 §3.2 | Medium |
| Missing dialog PageObject | 21d9 §9.9 | High |
| Thread.Sleep usage | 21d7, 21d8 | Medium |
| Silent failures | 21d6 §6.13 | High |
| Inconsistent naming | 21d9 §9.4 | Low |
| No item ControlObject | 21d6 §6.10 | Medium |

---

*Next: [21d50b_Solutions.md](21d50b_Solutions.md) - Proposed solutions for each issue*

