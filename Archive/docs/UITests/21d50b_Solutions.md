# 21d50b: Solutions for UI Test Framework Issues

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Previous:** [21d50a_CurrentIssues.md](21d50a_CurrentIssues.md)  
**Created:** December 2025

---

## Overview

This document provides solutions for the issues identified in 21d50a. Solutions follow the established patterns in 21d1-21d17.

---

## Solution 1: Create MessageBoxDialog PageObject

**Fixes:** Issue 1, Issue 4

**Implementation:**

```csharp
// Location: Oravey.UITestFramework.Wpf/Controls/MessageBoxDialog.cs
public class MessageBoxDialog : PageBase
{
    public LabelControl MessageLabel { get; }
    public ButtonControl YesButton { get; }
    public ButtonControl NoButton { get; }
    public ButtonControl OkButton { get; }
    public ButtonControl CancelButton { get; }
    
    public MessageBoxDialog(FlaUITestContext context) 
        : base(context, "MessageBoxDialog")
    {
        MessageLabel = new LabelControl(context, this, "MessageBoxText");
        YesButton = new ButtonControl(context, this, "Yes");
        NoButton = new ButtonControl(context, this, "No");
        OkButton = new ButtonControl(context, this, "OK");
        CancelButton = new ButtonControl(context, this, "Cancel");
    }
    
    public override bool IsDisplayed()
    {
        // Check for any message box window
        return FindMessageBoxWindow() != null;
    }
    
    public string GetMessage() => MessageLabel.GetText();
    
    public void ClickYes()
    {
        YesButton.WaitClickable();
        YesButton.Click();
        Context.WaitFor(() => !IsDisplayed(), description: "MessageBox closed");
    }
    
    public void ClickNo()
    {
        NoButton.WaitClickable();
        NoButton.Click();
        Context.WaitFor(() => !IsDisplayed(), description: "MessageBox closed");
    }
    
    public void ClickOk()
    {
        OkButton.WaitClickable();
        OkButton.Click();
        Context.WaitFor(() => !IsDisplayed(), description: "MessageBox closed");
    }
    
    private Window? FindMessageBoxWindow()
    {
        // Find MessageBox window by class name (standard Windows MessageBox)
        var processId = _context.MainWindow.Properties.ProcessId.Value;
        var desktop = _context.Driver.Automation.GetDesktop();
        var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
        
        return windows.FirstOrDefault(w => 
            w.Properties.ProcessId.Value == processId &&
            w.ClassName == "#32770") as Window;  // Standard MessageBox class
    }
}
```

**Migration Steps:**
1. Create `MessageBoxDialog.cs` in UITestFramework.Wpf
2. Update `DeleteUserModel()` to not auto-dismiss
3. Update tests to explicitly handle the dialog
4. Remove `ClickElementAndWaitForModal()` from FlaUITestContext

**Test Usage:**
```csharp
// Before:
page.DeleteUserModel(testModelName);  // Hidden dismiss

// After:
page.ClickDeleteOnUserModel(testModelName);  // Just clicks delete button
var confirmDialog = new MessageBoxDialog(Context);
confirmDialog.WaitForDisplayed();
confirmDialog.GetMessage().Should().Contain("Are you sure");
confirmDialog.ClickYes();
```

---

## Solution 2: Create UserModelItemControl

**Fixes:** Issue 2, Issue 8

**Implementation:**

```csharp
// Location: Oravey.Tools.Wpf.UITests/Controls/UserModelItemControl.cs
public class UserModelItemControl : ControlBase
{
    public LabelControl Name { get; }
    public ButtonControl EditButton { get; }
    public ButtonControl DeleteButton { get; }
    public CheckBoxControl EnabledCheckBox { get; }
    public ButtonControl TestButton { get; }
    public LabelControl StatusLabel { get; }
    
    public UserModelItemControl(
        FlaUITestContext context, 
        PageBase page, 
        AutomationElement container)
        : base(context, page, container)
    {
        Name = new LabelControl(context, page, container, "UserModelName");
        EditButton = new ButtonControl(context, page, container, "EditUserModelButton");
        DeleteButton = new ButtonControl(context, page, container, "DeleteUserModelButton");
        EnabledCheckBox = new CheckBoxControl(context, page, container, "UserModelEnabledCheckbox");
        TestButton = new ButtonControl(context, page, container, "TestUserModelButton");
        StatusLabel = new LabelControl(context, page, container, "UserModelStatus");
    }
    
    public string GetName() => Name.GetText();
    
    public void Edit()
    {
        EditButton.Click();
    }
    
    public void Delete()
    {
        DeleteButton.Click();
        // Caller must handle confirmation dialog
    }
    
    public void ToggleEnabled()
    {
        EnabledCheckBox.Toggle();
    }
}
```

**PageObject Update:**
```csharp
public class ModelManagementViewPage : PageBase
{
    // Replace scattered methods with ItemsControl pattern
    public UserModelItemControl GetUserModelItem(int index)
    {
        var container = UserModelsList.GetItemContainer(index);
        if (container == null)
            throw new ElementNotFoundException($"User model at index {index} not found");
        return new UserModelItemControl(Context, this, container);
    }
    
    public UserModelItemControl GetUserModelByName(string name)
    {
        var items = GetAllUserModelItems();
        var item = items.FirstOrDefault(i => i.GetName() == name);
        if (item == null)
            throw new ElementNotFoundException($"User model '{name}' not found");
        return item;
    }
    
    public IReadOnlyList<UserModelItemControl> GetAllUserModelItems()
    {
        return UserModelsList.GetAllItemContainers()
            .Select(c => new UserModelItemControl(Context, this, c))
            .ToList();
    }
}
```

---

## Solution 3: Clean Up FlaUITestContext

**Fixes:** Issue 3

**Remove from FlaUITestContext:**
```csharp
// DELETE these methods - they bypass ControlObject pattern:
ElementExists()        // Use control.IsExists()
ElementIsVisible()     // Use control.IsVisible()
ElementIsEnabled()     // Use control.IsEnabled()
GetElementText()       // Use control.GetText()
ClickElement()         // Use button.Click()
EnterText()           // Use textBox.EnterText()
ClearElement()        // Use textBox.Clear()
HasModalWindow()      // Use MessageBoxDialog.IsDisplayed()
ClickAndWaitForModal()        // Remove
ClickElementAndWaitForModal() // Remove
DismissModalWindow()          // Remove
```

**Keep in FlaUITestContext (per 21d3):**
```csharp
// ITestContext implementation
TestName
Platform
Logger
DefaultTimeoutMs, ShortTimeoutMs, PollingIntervalMs
Log(), LogError()
WaitFor()
TakeScreenshot()

// WPF-specific (used by ControlBase)
MainWindow
FindElement()       // Used internally by controls
FindElements()      // Used internally by controls
FindElementByXPath() // Used internally by controls
```

---

## Solution 4: Replace Thread.Sleep with Proper Waits

**Fixes:** Issue 5

**Pattern 1: WaitFor with Condition**
```csharp
// Before:
page.AddUserModelButton.Click();
Wait(500);
dialog.IsDisplayed().Should().BeTrue();

// After:
page.AddUserModelButton.Click();
dialog.WaitForDisplayed();
```

**Pattern 2: BusyPageBase for Async Operations**
```csharp
// Before:
dialog.Save();
Wait(500);  // Wait for save to complete

// After:
public class ModelEditorDialogPage : BusyPageBase  // Inherit busy tracking
{
    protected override string BusyIndicatorId => "SaveProgressIndicator";
    
    public void SaveAndWait()
    {
        SaveButton.Click();
        WaitForNotBusy();  // Wait for progress indicator
        Context.WaitFor(() => !IsDisplayed(), description: "Dialog closed");
    }
}
```

**Pattern 3: WaitFor in Control Actions**
```csharp
// Before:
dialog.EnableTextCapabilities();
Thread.Sleep(100);  // Wait for checkbox binding

// After (in CheckBoxControl):
public override void Toggle()
{
    CheckClickable();  // Already waits
    var checkBox = element.AsCheckBox();
    var before = checkBox.IsChecked;
    checkBox.Toggle();
    Context.WaitFor(() => checkBox.IsChecked != before, 
        ShortTimeoutMs, "toggle state change");
}
```

---

## Solution 5: Throw on Action Failures

**Fixes:** Issue 6

**Update PageObject Methods:**
```csharp
// Before:
public void ClickEditOnUserModel(int index = 0)
{
    // ...
    if (index < editButtons.Length)  // Silent fail
    {
        editButtons[index].Click();
    }
}

// After:
public void ClickEditOnUserModel(int index = 0)
{
    var item = GetUserModelItem(index);  // Throws if not found
    item.EditButton.Click();
}
```

**Update ControlBase Pattern:**
```csharp
// In ControlBase actions - always throw
public virtual void Click()
{
    CheckClickable();  // Throws AssertionException if not clickable
    var element = FindElement();
    if (element == null)
        throw new ElementNotFoundException(
            $"Cannot click '{AutomationId}': element not found");
    element.Click();
    LogAction("Click");
}
```

---

## Solution 6: Consistent Naming Convention

**Fixes:** Issue 7

**Rename Methods:**
| Current | Renamed |
|---------|---------|
| `HasEditUserModelButton()` | Remove - use `GetUserModelItem(0).EditButton.IsExists()` |
| `GetUserModelItemCount()` | `GetUserModelCount()` |
| `ClickDeleteOnUserModel(index)` | `GetUserModelItem(index).DeleteButton.Click()` |
| `IsEditButtonEnabled(index)` | `GetUserModelItem(index).EditButton.IsEnabled()` |
| `DeleteUserModel(name)` | `GetUserModelByName(name).Delete()` |

---

## Migration Priority

| Priority | Solution | Effort | Impact |
|----------|----------|--------|--------|
| 1 | Solution 1 - MessageBoxDialog | Medium | Fixes MessageBox dismiss issue |
| 2 | Solution 5 - Throw on failures | Low | Better test diagnostics |
| 3 | Solution 4 - Replace Thread.Sleep | Medium | More reliable tests |
| 4 | Solution 2 - UserModelItemControl | High | Cleaner architecture |
| 5 | Solution 3 - Clean Context | Medium | Pattern compliance |
| 6 | Solution 6 - Naming | Low | Consistency |

---

## Implementation Plan

### Phase 1: Critical Fixes (MessageBox Issue)
1. Create `MessageBoxDialog` PageObject
2. Update `DeleteUserModel()` to not auto-dismiss
3. Update delete tests to use explicit dialog handling
4. Verify delete tests pass

### Phase 2: Architecture Cleanup
1. Create `UserModelItemControl` class
2. Refactor `ModelManagementViewPage` to use item controls
3. Update tests to use new patterns
4. Remove deprecated methods

### Phase 3: Context Cleanup
1. Deprecate element operation methods in FlaUITestContext
2. Update any remaining usages
3. Remove deprecated methods
4. Update documentation

---

*Previous: [21d50a_CurrentIssues.md](21d50a_CurrentIssues.md)*

