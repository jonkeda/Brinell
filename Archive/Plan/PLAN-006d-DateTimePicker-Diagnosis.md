# PLAN-006d: DateTimePicker Root Cause Diagnosis

## Problem Statement
DateTimePicker tests are failing. Expected dates are not being set correctly.

**Observed Failures:**
- Expected `2025-06-15` but got `2026-01-25`
- Expected `2026-01-15` but got `2026-01-26`  
- Expected `2025-08-20` but got `2026-01-25`
- Expected today's date but got different date

**Pattern:** The returned dates (2026-01-25, 2026-01-26) appear to be the control's default/unchanged value, suggesting the `SetDate()` method isn't actually changing the date.

## Hypotheses

### H1: Value Pattern Actually Works
The current code tries Value pattern first, then falls back to keyboard. Maybe Value pattern works but we're not using it correctly in `SetDate()`.

**Test:** Check if Value pattern is available and try setting with formatted date string.

### H2: Focus Not Being Acquired
`element.Focus()` may not actually give keyboard focus to the control, so keyboard input goes elsewhere.

**Test:** Check `HasKeyboardFocus` after calling Focus().

### H3: HOME Key Not Positioning Correctly
The HOME key may not position to the month segment as expected.

**Test:** Observe which segment is active after pressing HOME.

### H4: Auto-Advance Timing Issues
Typing digits too fast may not trigger auto-advance between segments.

**Test:** Add delays between segment typing.

### H5: Segment Selection/Replacement Not Working
The DateTimePicker may require selecting the segment before typing replaces it.

**Test:** Try using Ctrl+A or different selection methods per segment.

### H6: Wrong Date Format
The control may expect a different date format (e.g., dd/MM/yyyy vs MM/dd/yyyy).

**Test:** Check the current value format and match it.

## Diagnostic Results

### H1 Result: ❌ FAILED
- Value pattern IS available and NOT read-only
- But `SetValue()` doesn't actually change the date
- Tried formats: `6/15/2025`, `06/15/2025`, `2025-06-15`, `06-15-2025` - all failed
- Value remained unchanged after SetValue calls

### H2 Result: ✅ Focus works
- `HasKeyboardFocus` is `True` after calling `Focus()`

### H3 Result: ⚠️ CRITICAL FINDING
- HOME key goes to **DAY segment**, not month!
- Value changed from `03-Jan-26` to `01-Jan-26` (day reset to 01)
- This proves segment order is: **Day → Month → Year**

### H4 Result: ⚠️ PARTIAL SUCCESS
- Typing works, but goes to WRONG segments
- After typing "06": `06-Jan-26` (went to day)
- After typing "15": `15-Jan-26` (went to day again)

### H5 Result: ❌ FAILED
- Ctrl+A + Type resulted in garbled date: `15-Feb-06`

### H6 Result: ✅ ROOT CAUSE IDENTIFIED
- **Current value format is `dd-MMM-yy`** (e.g., `03-Jan-26`)
- **Segment order is: Day → Month → Year** (NOT Month → Day → Year)
- Code assumed US locale MM/dd/yyyy but actual is dd-MMM-yy
- Month is TEXT format (Jan, Feb, etc.) NOT numeric!

## ROOT CAUSE
The DateTimePicker has format `dd-MMM-yy` with segments in Day → Month → Year order.
The month segment expects **3-letter month names** (Jan, Feb, etc.), NOT numbers.
Current code types numeric month which doesn't work.

## Solution - IMPLEMENTED ✅

The fix required two key changes:

### 1. Use Arrow Keys Instead of Typing
- WinForms DateTimePicker doesn't accept typed input reliably
- UP/DOWN arrow keys work to increment/decrement each segment
- Read current value, calculate difference, press UP/DOWN appropriate times

### 2. Set Segments in Correct Order: Year → Month → Day
- UI segment order after Click: Year → Day → Month
- But we must set in order: Year → Month → Day (skip Day, set Month, go back to Day)
- This prevents day-of-month clamping (e.g., setting day 31 when month only has 30 days)

### Implementation
```csharp
// 1. Click to focus, set Year (segment 0)
// 2. RIGHT, RIGHT to Month (segment 2), set Month
// 3. LEFT to Day (segment 1), set Day
// 4. TAB to confirm
```

**Result: 74/74 tests passing (100%)**
