# 21d50d: Obsolete Code to Remove

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Previous:** [21d50c_TestUpdates.md](21d50c_TestUpdates.md)  
**Created:** December 2025

---

## Overview

This document lists all obsolete code to be removed. No backward compatibility - remove completely.

---

## FlaUITestContext.cs - Methods to DELETE

**File:** `Sources/UITestFramework/Oravey.UITestFramework.Wpf/Infrastructure/FlaUITestContext.cs`

### Element Operations (Lines 145-230)

**DELETE COMPLETELY:**

```csharp
// Line 145-148 - DELETE
public bool ElementExists(string automationId)
{
    return FindElement(automationId) != null;
}

// Line 150-154 - DELETE
public bool ElementIsVisible(string automationId)
{
    var element = FindElement(automationId);
    return element != null && !element.IsOffscreen;
}

// Line 156-160 - DELETE
public bool ElementIsEnabled(string automationId)
{
    var element = FindElement(automationId);
    return element?.IsEnabled ?? false;
}

// Line 162-174 - DELETE
public string GetElementText(string automationId)
{
    var element = FindElement(automationId);
    if (element == null) return string.Empty;
    
    var textBox = element.AsTextBox();
    if (textBox != null) return textBox.Text ?? string.Empty;
    
    var label = element.AsLabel();
    if (label != null) return label.Text ?? string.Empty;
    
    return element.Name ?? string.Empty;
}

// Line 176-196 - DELETE
public void ClickElement(string automationId)
{
    var element = FindElement(automationId);
    if (element != null)
    {
        var button = element.AsButton();
        if (button != null)
        {
            button.Invoke();
        }
        else
        {
            element.Click();
        }
    }
    else
    {
        throw new InvalidOperationException($"Element '{automationId}' not found for click operation.");
    }
}

// Line 198-214 - DELETE
public void EnterText(string automationId, string text)
{
    var element = FindElement(automationId);
    if (element != null)
    {
        var textBox = element.AsTextBox();
        if (textBox != null)
        {
            textBox.Text = string.Empty;
            textBox.Enter(text);
        }
    }
    else
    {
        throw new InvalidOperationException($"Element '{automationId}' not found for enter text operation.");
    }
}

// Line 216-230 - DELETE
public void ClearElement(string automationId)
{
    var element = FindElement(automationId);
    if (element != null)
    {
        var textBox = element.AsTextBox();
        if (textBox != null)
        {
            textBox.Text = string.Empty;
        }
    }
    else
    {
        throw new InvalidOperationException($"Element '{automationId}' not found for clear operation.");
    }
}
```

### Modal Handling (Lines 258-420)

**DELETE COMPLETELY:**

```csharp
// Line 258-293 - DELETE
public bool HasModalWindow()
{
    // ... entire method
}

// Line 301-313 - DELETE  
public bool ClickAndWaitForModal(string automationId, int timeoutMs = 3000)
{
    // ... entire method
}

// Line 320-373 - DELETE
public bool ClickElementAndWaitForModal(AutomationElement element, int timeoutMs = 3000)
{
    // ... entire method
}

// Line 378-420 - DELETE
private void DismissModalWindow()
{
    // ... entire method
}
```

---

## ModelManagementViewPage.cs - Methods to DELETE

**File:** `Sources/Tests/Oravey.Tools.Wpf.UITests/PageObjects/ModelManagementViewPage.cs`

### Has* Methods - DELETE (replaced by item controls)

```csharp
// Line 93-101 - DELETE
public bool HasEditUserModelButton()
{
    Log("HasEditUserModelButton()");
    return _context.ElementExists("EditUserModelButton");
}

// Line 107-111 - DELETE
public bool HasDeleteUserModelButton()
{
    Log("HasDeleteUserModelButton()");
    return _context.ElementExists("DeleteUserModelButton");
}

// Line 117-121 - DELETE
public bool HasTestUserModelButton()
{
    Log("HasTestUserModelButton()");
    return _context.ElementExists("TestUserModelButton");
}

// Line 127-131 - DELETE
public bool HasUserModelEnabledCheckbox()
{
    Log("HasUserModelEnabledCheckbox()");
    return _context.ElementExists("UserModelEnabledCheckbox");
}

// Line 139-143 - DELETE
public bool HasViewSystemModelButton()
{
    Log("HasViewSystemModelButton()");
    return _context.ElementExists("ViewSystemModelButton");
}

// Line 149-153 - DELETE
public bool HasCopySystemModelButton()
{
    Log("HasCopySystemModelButton()");
    return _context.ElementExists("CopySystemModelButton");
}

// Line 158-162 - DELETE
public bool HasTestSystemModelButton()
{
    Log("HasTestSystemModelButton()");
    return _context.ElementExists("TestSystemModelButton");
}
```

### Raw FlaUI Methods - DELETE (replaced by item controls)

```csharp
// Line 203-212 - DELETE (replace with UserModelsList.GetItemCount())
public int GetSystemModelItemCount() { ... }

// Line 218-227 - DELETE (replace with UserModelsList.GetItemCount())
public int GetUserModelItemCount() { ... }

// Line 233-243 - DELETE (replace with iteration over GetAllUserModelItems())
public IReadOnlyList<string> GetSystemModelNames() { ... }

// Line 249-259 - DELETE (replace with iteration over GetAllUserModelItems())
public IReadOnlyList<string> GetUserModelNames() { ... }

// Line 265-275 - DELETE (replace with GetSystemModelByName().Click())
public void ClickSystemModel(string modelName) { ... }

// Line 281-292 - DELETE (replace with GetSystemModelItem(index).ViewButton.Click())
public void ClickViewOnSystemModel(int index = 0) { ... }

// Line 298-310 - DELETE (replace with GetSystemModelItem(index).CopyButton.Click())
public void ClickCopyOnSystemModel(int index = 0) { ... }

// Line 316-328 - DELETE (replace with GetSystemModelItem(index).TestButton.Click())
public void ClickTestOnSystemModel(int index = 0) { ... }

// Line 361-372 - DELETE (replace with checking control visibility)
public bool IsEditButtonEnabled(int index = 0) { ... }

// Line 373-398 - REFACTOR: remove auto-dismiss, just click button
public void DeleteUserModel(string modelName) { ... }

// Line 403-416 - REFACTOR: remove auto-dismiss, just click button  
public void ClickDeleteOnUserModel(int index = 0) { ... }

// Line 421-435 - DELETE (replace with GetUserModelItem(index).EditButton.Click())
public void ClickEditOnUserModel(int index = 0) { ... }

// Line 441-455 - DELETE ENTIRELY (modal handling removed from page)
public bool ClickEditOnUserModelAndWaitForModal(int index = 0) { ... }

// Line 460-475 - DELETE (replace with GetSystemModelItem(index).TestButton.IsEnabled())
public bool IsTestButtonEnabled(int index = 0) { ... }

// Line 481-495 - DELETE (replace with GetSystemModelItem(index).StatusLabel.GetText())
public string GetModelStatus(int index = 0) { ... }
```

---

## ModelEditorDialogPage.cs - Code to UPDATE

**File:** `Sources/Tests/Oravey.Tools.Wpf.UITests/PageObjects/ModelEditorDialogPage.cs`

### Remove Thread.Sleep calls

```csharp
// Line 149-150 in EnableTextCapabilities() - DELETE the Sleep
System.Threading.Thread.Sleep(100);

// Line 159-160 in Save() - DELETE the Sleep  
System.Threading.Thread.Sleep(200);
```

---

## Entire Regions to DELETE in FlaUITestContext

```csharp
// DELETE this entire region (lines 143-231):
#region ITestContext element operations (for backward compatibility)
    // All methods in this region
#endregion

// DELETE: Remove modal methods entirely - no region marker but delete lines 258-420
```

---

## ITestContext Interface Update

**File:** `Sources/UITestFramework/Oravey.UITestFramework.Core/Abstractions/ITestContext.cs`

If `ITestContext` defines element operations, they must also be removed:

```csharp
// DELETE from interface (if present):
bool ElementExists(string automationId);
bool ElementIsVisible(string automationId);
bool ElementIsEnabled(string automationId);
string GetElementText(string automationId);
void ClickElement(string automationId);
void EnterText(string automationId, string text);
void ClearElement(string automationId);
```

**Keep only:**
```csharp
// KEEP in ITestContext:
string TestName { get; set; }
Platform Platform { get; }
ITestLogger? Logger { get; }
int DefaultTimeoutMs { get; }
int ShortTimeoutMs { get; }
int PollingIntervalMs { get; }
void SetLogger(ITestLogger logger);
void Log(string message);
void LogError(Exception ex, string context);
bool WaitFor(Func<bool> condition, int? timeoutMs = null, string description = "condition");
string? TakeScreenshot(string name);
```

---

## MAUI and HTML Context - Parallel Changes

If consistency is required, same methods should be removed from:

- `Sources/UITestFramework/Oravey.UITestFramework.Maui/Infrastructure/AppiumTestContext.cs`
- `Sources/UITestFramework/Oravey.UITestFramework.Html/Infrastructure/SeleniumTestContext.cs`

These files have the same element operation methods that bypass ControlObject pattern.

---

## Removal Order

Execute in this order to avoid breaking builds:

1. **Create new files first:**
   - `MessageBoxDialog.cs`
   - `UserModelItemControl.cs`
   - `SystemModelItemControl.cs`

2. **Update PageObjects to use new controls:**
   - Update `ModelManagementViewPage.cs` to use item controls
   - Update `ModelEditorDialogPage.cs` to remove sleeps

3. **Update tests:**
   - Update all test files to use new patterns
   - Replace `DeleteUserModel()` calls with explicit dialog handling

4. **Remove obsolete PageObject methods:**
   - Remove Has* methods
   - Remove raw FlaUI methods
   - Remove modal-handling methods

5. **Remove Context methods:**
   - Remove element operations from `FlaUITestContext.cs`
   - Update `ITestContext` interface if needed

6. **Build and test:**
   - Ensure all tests compile
   - Run all UI tests to verify

---

## Verification Checklist

After removal, verify:

- [ ] No compilation errors
- [ ] No references to deleted methods
- [ ] All UI tests still pass
- [ ] No `ClickElementAndWaitForModal` usage anywhere
- [ ] No `Context.ElementExists()` in PageObjects
- [ ] No `Thread.Sleep()` in test files (except internal framework code)
- [ ] MessageBox dialogs handled explicitly in tests

---

*Previous: [21d50c_TestUpdates.md](21d50c_TestUpdates.md)*
