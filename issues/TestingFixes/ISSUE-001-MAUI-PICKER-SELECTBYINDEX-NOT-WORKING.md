# ISSUE-001: MAUI PickerControl SelectByIndex Not Working

**Status:** Fixed  
**Priority:** P1  
**Component:** Brinell.Maui / PickerControl  
**Date Created:** January 5, 2026  
**Date Fixed:** January 5, 2026  

---

## 1. Summary

The `PickerControl.SelectByIndex()` method in the MAUI framework does not properly select items in the Picker control on Windows. After calling `SelectByIndex()`, the selection does not update, and the associated label (bound to the picker's selection) still shows "No color selected".

---

## 2. Symptoms

### Test Failures
- `PickerTests.ColorPicker_SelectByIndex_UpdatesSelection` - **FAILED**
- `PickerTests.ColorPicker_SelectDifferentColors_UpdatesLabel` - **FAILED**

### Error Messages
```
Assert.Contains failure: "Red" not found in "No color selected"
Assert.Contains failure: "Green" not found in "No color selected"
Assert.Contains failure: "Blue" not found in "No color selected"
```

### Observed Behavior
1. Test calls `_mainPage.ColorPicker.SelectByIndex(0)` to select "Red"
2. Test checks `_mainPage.SelectedColorLabel.GetText()` 
3. Label still shows "No color selected" instead of "Selected: Red"

---

## 3. Root Cause Analysis

### 3.1 Current Implementation

The `SelectorControlBase.SelectByIndex()` method in [SelectorControlBase.cs](../../src/Brinell.Maui/Controls/Base/SelectorControlBase.cs):

```csharp
public virtual void SelectByIndex(int index)
{
    LogAction("SelectByIndex", index.ToString());
    
    var element = WaitForElementVisible();
    if (element == null)
        throw new InvalidOperationException($"Element '{AutomationId}' not visible for selection.");
    
    // Open the selector
    element.Click();
    Thread.Sleep(PickerOpenDelayMs); // Wait for picker to open
    
    PerformSelectByIndex(index);
}

protected virtual void PerformSelectByIndex(int index)
{
    // Try to find items and select by index
    var items = _context.Driver.Driver.FindElements(By.XPath("//*[@clickable='true']"));
    if (index < items.Count)
    {
        items[index].Click();
    }
}
```

### 3.2 Root Cause

**The XPath selector `//*[@clickable='true']` is incorrect for Windows MAUI Picker popups.**

On Windows MAUI with WinAppDriver:
1. Clicking the Picker opens a **ComboBox dropdown** or **Popup** window
2. The dropdown items are rendered as `ListItem` elements, NOT generic clickable elements
3. The items may be in a different window context (popup window)
4. The XPath query finds ALL clickable elements on the page, not just picker items

### 3.3 Windows MAUI Picker Control Tree

When a MAUI `Picker` is clicked on Windows:
```
Window (Main App)
└── Picker (ComboBox-like)
    └── [Dropdown/Popup appears as overlay or separate window]
        └── ListItem[0]: "Red"
        └── ListItem[1]: "Green"
        └── ListItem[2]: "Blue"
        └── ListItem[3]: "Yellow"
        └── ListItem[4]: "Purple"
```

The `FindElements(By.XPath("//*[@clickable='true']"))` returns elements from the main window, not the popup items.

---

## 4. Possible Fixes

### Fix Option 1: Use Windows-Specific Locator Strategy (Recommended)

Update `PerformSelectByIndex()` to use proper WinAppDriver locators for ComboBox items:

```csharp
protected override void PerformSelectByIndex(int index)
{
    // For Windows MAUI, items appear as ListItem in a popup
    // Wait for popup to appear
    Thread.Sleep(300);
    
    // Find items in the popup using Name or ControlType
    var items = _context.Driver.Driver.FindElements(
        By.XPath("//ListItem | //List/ListItem | //ComboBox/ListItem"));
    
    if (index >= 0 && index < items.Count)
    {
        items[index].Click();
    }
    else
    {
        throw new InvalidOperationException(
            $"Index {index} out of range. Found {items.Count} items.");
    }
}
```

### Fix Option 2: Use Windows Automation API Patterns

Use UI Automation patterns for selection:

```csharp
protected override void PerformSelectByIndex(int index)
{
    // Use SendKeys to navigate (Down arrow) and Enter to select
    var element = FindElement();
    for (int i = 0; i <= index; i++)
    {
        element.SendKeys(Keys.Down);
        Thread.Sleep(50);
    }
    element.SendKeys(Keys.Enter);
}
```

### Fix Option 3: Use SelectByText Instead

If text is known, use `SelectByText()` which searches for elements with matching text:

```csharp
// In test:
_mainPage.ColorPicker.SelectByText("Red");

// Implementation uses text matching which is more reliable
```

### Fix Option 4: Platform-Specific Override in PickerControl

Override in `PickerControl.cs` specifically for Windows MAUI:

```csharp
protected override void PerformSelectByIndex(int index)
{
    // Windows MAUI specific: Use keyboard navigation
    var element = FindElement();
    
    // Press Down arrow 'index + 1' times (first down opens and selects first item)
    for (int i = 0; i <= index; i++)
    {
        _context.Driver.Driver.Keyboard.SendKeys(Keys.ArrowDown);
        Thread.Sleep(50);
    }
    
    // Confirm selection
    _context.Driver.Driver.Keyboard.SendKeys(Keys.Enter);
    Thread.Sleep(200);
}
```

---

## 5. Verification Plan

After implementing fix:

1. **Unit Test:** Verify `PickerControl.SelectByIndex()` updates selection
2. **UI Test:** Run `PickerTests` to confirm:
   - `ColorPicker_SelectByIndex_UpdatesSelection` passes
   - `ColorPicker_SelectDifferentColors_UpdatesLabel` passes
3. **Manual Test:** Verify picker selection visually works

---

## 6. Related Files

- [SelectorControlBase.cs](../../src/Brinell.Maui/Controls/Base/SelectorControlBase.cs) - Base implementation
- [PickerControl.cs](../../src/Brinell.Maui/Controls/PickerControl.cs) - Picker control
- [PickerTests.cs](../../samples/Brinell.Samples.Maui.UITests/Tests/PickerTests.cs) - Failing tests
- [MainPage.xaml](../../samples/Brinell.Samples.Maui.App/MainPage.xaml) - MAUI Picker definition

---

## 7. Decision

**Recommended Approach:** Fix Option 4 (Platform-Specific Override)

**Rationale:**
1. Keyboard navigation is reliable across Windows MAUI versions
2. Doesn't require knowing the exact popup structure
3. Simulates user behavior (pressing down arrow to select)
4. Works even if popup structure changes between .NET versions

---

## 8. Fix Applied

### Changes Made

**File: [SelectorControlBase.cs](../../src/Brinell.Maui/Controls/Base/SelectorControlBase.cs)**

Updated `PerformSelectByIndex()` method to:
1. First try to find `ListItem` elements in the popup and click directly
2. Fall back to keyboard navigation using `Actions.SendKeys()` with Arrow Down and Enter keys

```csharp
protected virtual void PerformSelectByIndex(int index)
{
    var driver = _context.Driver.Driver;
    
    // First, try to find ListItem elements in the popup
    try
    {
        var items = driver.FindElements(By.XPath("//ListItem | //List/ListItem"));
        if (items.Count > index)
        {
            items[index].Click();
            Thread.Sleep(200);
            return;
        }
    }
    catch { /* Fall through to keyboard navigation */ }
    
    // Fallback: Use Actions for keyboard navigation
    var actions = new OpenQA.Selenium.Interactions.Actions(driver);
    for (int i = 0; i <= index; i++)
    {
        actions.SendKeys(Keys.ArrowDown).Perform();
        Thread.Sleep(50);
    }
    actions.SendKeys(Keys.Enter).Perform();
    Thread.Sleep(200);
}
```

**File: [PickerTests.cs](../../samples/Brinell.Samples.Maui.UITests/Tests/PickerTests.cs)**

Added new tests to validate SelectByIndex and SelectByText functionality:
- `ColorPicker_SelectByIndex_UpdatesSelection`
- `ColorPicker_SelectByText_UpdatesSelection`
- `ColorPicker_SelectDifferentColors_UpdatesLabel`

---

## 9. Notes

- The MAUI Picker on Windows behaves differently from Android/iOS
- WinAppDriver may not properly expose popup windows in element tree
- Consider adding integration tests for different platforms
