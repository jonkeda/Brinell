using System.Diagnostics;
using System.Globalization;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms DateTimePicker control wrapper.
/// Handles date/time selection through UI Automation.
/// Uses keyboard input for reliable date setting on WinForms.
/// </summary>
public class DateTimePickerControl : ControlBase
{
    public DateTimePickerControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public DateTimePickerControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get the currently displayed date/time as text.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element != null)
        {
            // Try to get the value pattern first
            var valuePattern = element.Patterns.Value.PatternOrDefault;
            if (valuePattern != null)
            {
                return valuePattern.Value.Value ?? string.Empty;
            }
            
            // Fall back to Name property
            return element.Name ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Set the date/time value using the Value pattern.
    /// Format depends on the control's configuration (e.g., "MM/dd/yyyy").
    /// </summary>
    public virtual void SetValue(string value)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            // First try the Value pattern
            var valuePattern = element.Patterns.Value.PatternOrDefault;
            if (valuePattern != null && !valuePattern.IsReadOnly.Value)
            {
                valuePattern.SetValue(value);
                LogAction("SetValue", value);
                return;
            }
            
            // Fall back to keyboard input for WinForms DateTimePicker
            // Focus the control and type the date
            element.Focus();
            _context.WaitFor(() => element.Properties.HasKeyboardFocus.ValueOrDefault, 1000, "control focused");
            
            // Select all and type the new value
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(value);
            
            // Press Enter to confirm
            Keyboard.Press(VirtualKeyShort.ENTER);
            
            // Wait for value to be applied
            _context.WaitFor(() => GetText().Contains(value) || !string.IsNullOrEmpty(GetText()), 1000, "value applied");
            
            LogAction("SetValue (keyboard)", value);
        }
    }

    /// <summary>
    /// Set the date value using arrow keys for reliable WinForms DateTimePicker interaction.
    /// WinForms DateTimePicker doesn't accept typed input reliably, but UP/DOWN arrows work.
    /// Segment order after Click is: Year → Day → Month.
    /// We set in order: Year → Month → Day to avoid day-of-month clamping issues.
    /// </summary>
    public virtual void SetDate(DateTime date)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element == null) return;
        
        // Click to focus and select the first segment (Year)
        element.Click();
        _context.WaitFor(() => element.Properties.HasKeyboardFocus.ValueOrDefault, 1000, "control focused");
        
        // Small delay to let UI settle
        Thread.Sleep(50);
        
        // Segment order after Click: Year(0) → Day(1) → Month(2)
        // We set in order: Year → Month → Day to avoid day clamping
        
        // 1. Set Year (segment 0 - already selected after click)
        var current = GetDateTime() ?? DateTime.Today;
        AdjustSegment(current.Year, date.Year);
        
        // 2. Skip to Month segment (RIGHT twice: Year → Day → Month)
        Keyboard.Press(VirtualKeyShort.RIGHT);
        Thread.Sleep(30);
        Keyboard.Press(VirtualKeyShort.RIGHT);
        Thread.Sleep(50);
        current = GetDateTime() ?? DateTime.Today;
        AdjustSegment(current.Month, date.Month);
        
        // 3. Go back to Day segment (LEFT once: Month → Day)
        Keyboard.Press(VirtualKeyShort.LEFT);
        Thread.Sleep(50);
        current = GetDateTime() ?? DateTime.Today;
        AdjustSegment(current.Day, date.Day);
        
        // Tab out to confirm
        Keyboard.Press(VirtualKeyShort.TAB);
        
        // Wait for value to be applied
        _context.WaitFor(() => 
        {
            var result = GetDateTime();
            return result.HasValue && result.Value.Date == date.Date;
        }, 2000, $"date set to {date:yyyy-MM-dd}");
        
        LogAction("SetDate", date.ToString("yyyy-MM-dd"));
    }
    
    /// <summary>
    /// Adjust a segment by pressing UP or DOWN arrows.
    /// Waits briefly after each key press to allow UI to update.
    /// </summary>
    private void AdjustSegment(int currentValue, int targetValue)
    {
        var diff = targetValue - currentValue;
        var key = diff > 0 ? VirtualKeyShort.UP : VirtualKeyShort.DOWN;
        var count = Math.Abs(diff);
        
        for (int i = 0; i < count; i++)
        {
            Keyboard.Press(key);
            Thread.Sleep(30); // Brief delay between key presses
        }
    }
    
    /// <summary>
    /// Type a date segment - kept for backward compatibility but not used.
    /// </summary>
    private void TypeDateSegment(string value)
    {
        foreach (char c in value)
        {
            Keyboard.Type(c.ToString());
        }
    }

    /// <summary>
    /// Set the date and time value.
    /// </summary>
    public virtual void SetDateTime(DateTime dateTime)
    {
        SetDate(dateTime.Date);
    }

    /// <summary>
    /// Try to parse the current value as a DateTime.
    /// </summary>
    public virtual DateTime? GetDateTime()
    {
        var text = GetText();
        if (DateTime.TryParse(text, out var result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// Get the current date value. Throws if not parseable.
    /// </summary>
    public virtual DateTime GetDate()
    {
        var dateTime = GetDateTime();
        if (dateTime == null)
        {
            ThrowCheckFailed("GetDate", $"Element '{AutomationId}' has invalid date format: '{GetText()}'.");
        }
        return dateTime!.Value;
    }

    /// <summary>
    /// Wait for the displayed text to match.
    /// </summary>
    public bool WaitForText(string expected, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => GetText() == expected,
            timeoutMs,
            $"text = '{expected}'");
        LogWait($"Text={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Open the calendar dropdown (if available).
    /// </summary>
    public virtual void OpenCalendar()
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var expandPattern = element.Patterns.ExpandCollapse.PatternOrDefault;
            expandPattern?.Expand();
            LogAction("OpenCalendar");
        }
    }

    /// <summary>
    /// Close the calendar dropdown.
    /// </summary>
    public virtual void CloseCalendar()
    {
        var element = FindElement();
        if (element != null)
        {
            var expandPattern = element.Patterns.ExpandCollapse.PatternOrDefault;
            expandPattern?.Collapse();
            LogAction("CloseCalendar");
        }
    }
}
