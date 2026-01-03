# PLAN-005b: WinForms Test Fixes

**Created:** January 3, 2026  
**Status:** In Progress  
**Previous:** [PLAN-005: WinForms Update](PLAN-005-WinForms-Update.md)

---

## 1. Test Run Summary

**Test Results:** 64 passed, 197 failed, 24 skipped (285 total)  
**Duration:** ~170 seconds

### Issue Categories

| Issue | Count | Controls Affected |
|-------|-------|-------------------|
| Password field text retrieval | 6+ | PasswordBoxControl (via TextBoxControl) |
| NumericUpDown text pattern | 15+ | NumericUpDownControl |
| TrackBar RangeValue pattern | 3 | TrackBarControl |
| DateTimePicker format parsing | 4 | DateTimePickerControl |
| ComboBox items empty | 5 | SelectorControlBase (ComboBox) |
| CheckBox toggle state | 3 | ToggleControlBase |
| RichTextBox clear incomplete | 1 | RichTextBoxControl |

---

## 2. Root Cause Analysis

### 2.1 Password Field Failures

**Error:**
```
FlaUI.Core.Exceptions.MethodNotSupportedException : Text from element 
'AutomationId:txtPassword' cannot be retrieved because it is set as password.
```

**Root Cause:** `TextControlBase.AppendText()` reads `textBox.Text` to append, but password fields don't expose their text via UI Automation for security.

**Fix:** Override `AppendText` and `Enter` in PasswordBoxControl to use keyboard-based input instead of reading/writing the Text property directly.

### 2.2 NumericUpDown Failures

**Error:**
```
FlaUI.Core.Exceptions.MethodNotSupportedException : AutomationElement 
'AutomationId:nudPort, ControlType:spinner' supports neither ValuePattern or TextPattern
```

**Root Cause:** NumericUpDown is a spinner control, not a TextBox. The `TextControlBase.SetText()` method tries to use `AsTextBox().Text` which fails. The actual edit field is a child element.

**Fix:** NumericUpDownControl needs to:
1. Find the child edit element within the spinner
2. Use the RangeValue pattern if available
3. Fallback to keyboard input for setting values

### 2.3 TrackBar Failures

**Error:**
```
CheckFailedException : Could not set trackbar value: RangeValue pattern not available.
```

**Root Cause:** WinForms TrackBar doesn't reliably expose RangeValue pattern in all scenarios. Current code throws if pattern is unavailable.

**Fix:** Add keyboard-based fallback in `TrackBarControl.SetValue()`:
1. Focus the control
2. Calculate required steps from current to target value
3. Send arrow key presses to increment/decrement

### 2.4 DateTimePicker Parsing Failures

**Error:**
```
CheckFailedException : Could not parse '03-Jan-26' as a date/time value.
```

**Root Cause:** The DateTimePicker is configured with `Format = Short` which uses the system locale format (e.g., `dd-MMM-yy`). The parser only handles US formats (`MM/dd/yyyy`).

**Fix:** 
1. Add more date format patterns including locale-aware formats
2. Use `DateTime.TryParse` with `CultureInfo.CurrentCulture` as fallback
3. Add explicit formats for common locale patterns

### 2.5 ComboBox Item Selection Failures

**Error:**
```
CheckFailedException : Item 'Admin' not found in element 'cmbRole'. Available items:
```

**Root Cause:** ComboBox items are empty when retrieved. This happens because:
1. FlaUI requires the dropdown to be expanded to see items in some cases
2. The Items collection may not be populated until interaction

**Fix:** In `SelectorControlBase.GetItems()` and `SelectByText()`:
1. Expand the ComboBox first
2. Wait briefly for items to populate
3. Collapse after selection

### 2.6 CheckBox Toggle Failures

**Error:**
```
Expected page.IsRememberMeChecked() to be true, but found False.
```

**Root Cause:** `ToggleControlBase.SetChecked()` uses `element.Click()` but the click might not be registering properly, or state isn't being read correctly after click.

**Fix:**
1. Use the Toggle pattern if available instead of Click
2. Add verification loop after state change
3. Increase delay for state propagation

### 2.7 RichTextBox Clear Incomplete

**Error:**
```
Expected result to be empty, but found "\n".
```

**Root Cause:** `TextControlBase.Clear()` sets `textBox.Text = string.Empty` but RichTextBox may preserve a trailing newline.

**Fix:** Override `Clear()` in `RichTextBoxControl` to handle this edge case by:
1. Select all text
2. Delete via keyboard
3. Or set Text to empty and verify

---

## 3. Implementation Plan

### 3.1 PasswordBoxControl - Use Keyboard Input

```csharp
// Override Enter to use keyboard input for password fields
public override void Enter(string password)
{
    var element = WaitForElementVisible();
    element?.Focus();
    System.Threading.Thread.Sleep(50);
    
    // Use keyboard typing instead of Text property
    element?.Patterns.Value.PatternOrDefault?.SetValue(password);
    // OR use FlaUI keyboard input
    // Keyboard.Type(password);
    
    LogAction("Enter", "[password]");
}
```

### 3.2 NumericUpDownControl - Find Edit Child

```csharp
// Find the edit child element within the spinner
private AutomationElement? GetEditElement()
{
    var element = FindElement();
    if (element == null) return null;
    
    // Find the edit child
    var edit = element.FindFirstDescendant(cf => 
        cf.ByControlType(ControlType.Edit));
    
    return edit;
}

public void SetValue(decimal value)
{
    var edit = GetEditElement();
    if (edit != null)
    {
        var textBox = edit.AsTextBox();
        textBox.Text = value.ToString(CultureInfo.InvariantCulture);
        return;
    }
    
    // Fallback to RangeValue pattern...
}
```

### 3.3 TrackBarControl - Keyboard Fallback

```csharp
public void SetValue(int value)
{
    // Try RangeValue pattern first...
    
    // Fallback to keyboard navigation
    var current = GetValue();
    var diff = value - current;
    
    element!.Focus();
    var key = diff > 0 ? "{RIGHT}" : "{LEFT}";
    
    for (int i = 0; i < Math.Abs(diff); i++)
    {
        System.Windows.Forms.SendKeys.SendWait(key);
        System.Threading.Thread.Sleep(10);
    }
}
```

### 3.4 DateTimePickerControl - Flexible Parsing

```csharp
public DateTime GetDateTime()
{
    var text = GetText();
    
    // Try culture-specific parsing first
    if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var result))
    {
        return result;
    }
    
    // Try additional formats
    var formats = new[]
    {
        "dd-MMM-yy", "dd-MMM-yyyy",  // 03-Jan-26
        "d/M/yyyy", "dd/MM/yyyy",     // UK formats
        "MM/dd/yyyy", "M/d/yyyy",     // US formats
        "yyyy-MM-dd",                  // ISO format
    };
    
    foreach (var format in formats)
    {
        if (DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, 
            DateTimeStyles.None, out result))
        {
            return result;
        }
    }
    
    ThrowCheckFailed("GetDateTime", $"Could not parse '{text}'");
}
```

### 3.5 SelectorControlBase - Expand ComboBox

```csharp
public override IReadOnlyList<string> GetItems()
{
    var element = FindElement();
    if (element == null) return new List<string>();

    var comboBox = element.AsComboBox();
    if (comboBox != null)
    {
        // Expand to populate items
        comboBox.Expand();
        System.Threading.Thread.Sleep(100);
        
        var items = comboBox.Items.Select(item => item?.ToString() ?? string.Empty).ToList();
        
        comboBox.Collapse();
        return items.AsReadOnly();
    }
    // ...
}
```

### 3.6 ToggleControlBase - Use Toggle Pattern

```csharp
public virtual void SetChecked(bool value)
{
    var element = WaitForElementVisible();
    var checkBox = element!.AsCheckBox();
    
    if (checkBox != null)
    {
        var currentState = checkBox.IsChecked ?? false;
        
        if (currentState != value)
        {
            // Use Toggle pattern if available
            var togglePattern = element!.Patterns.Toggle.PatternOrDefault;
            if (togglePattern != null)
            {
                togglePattern.Toggle();
            }
            else
            {
                element!.Click();
            }
            
            System.Threading.Thread.Sleep(150);
            
            // Verify state changed
            var newState = checkBox.IsChecked ?? false;
            if (newState != value)
            {
                // Retry once
                element!.Click();
                System.Threading.Thread.Sleep(150);
            }
        }
    }
}
```

### 3.7 RichTextBoxControl - Complete Clear

```csharp
public override void Clear()
{
    var element = WaitForElementVisible();
    var textBox = element!.AsTextBox();
    
    if (textBox != null)
    {
        textBox.Text = string.Empty;
        System.Threading.Thread.Sleep(50);
        
        // Verify and retry if needed
        var remaining = textBox.Text;
        if (!string.IsNullOrEmpty(remaining))
        {
            // Use keyboard to clear
            element.Focus();
            System.Windows.Forms.SendKeys.SendWait("^a{DELETE}");
            System.Threading.Thread.Sleep(50);
        }
        
        LogAction("Clear");
    }
}
```

---

## 4. Files to Modify

| File | Change |
|------|--------|
| `Controls/PasswordBoxControl.cs` | Override Enter/Clear to use keyboard/Value pattern |
| `Controls/NumericUpDownControl.cs` | Find edit child element for text operations |
| `Controls/TrackBarControl.cs` | Add keyboard fallback for SetValue |
| `Controls/DateTimePickerControl.cs` | Add flexible date parsing with locale support |
| `Controls/Base/SelectorControlBase.cs` | Expand ComboBox before getting/selecting items |
| `Controls/Base/ToggleControlBase.cs` | Use Toggle pattern, add verification |
| `Controls/RichTextBoxControl.cs` | Override Clear with verification |

---

## 5. Validation

After fixes, run:
```powershell
dotnet build src/Brinell.WinForms
dotnet test samples/Brinell.Samples.WinForms.UITests --verbosity minimal
```

**Target:** 90%+ test pass rate (256+ of 285 tests)

---

*Previous: [PLAN-005: WinForms Update](PLAN-005-WinForms-Update.md)*
