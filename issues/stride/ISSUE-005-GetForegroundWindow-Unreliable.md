# ISSUE-005: GetForegroundWindow() Reports False Focus State

**Created:** January 3, 2026  
**Status:** In Progress  
**Severity:** High  
**Category:** Windows API / Focus Management  

---

## Summary

`GetForegroundWindow()` returns the game window handle, but keyboard input still goes to another window. The API lies about actual keyboard focus.

---

## Symptoms

- Logs show "Game window already has focus" 
- But keyboard input goes to VS Code or other windows
- Tests fail despite focus check passing

---

## Root Cause

Windows has TWO distinct types of focus:

1. **Foreground Window** - The window visually in front (what `GetForegroundWindow()` returns)
2. **Keyboard Focus** - The window that receives keyboard input

A window can be foreground WITHOUT having keyboard focus when:
- The window was activated programmatically (not by user click)
- The test runner process (VS Code) retains keyboard focus
- Windows blocks focus stealing for security reasons

---

## Technical Details

```csharp
// This check is insufficient:
if (GetForegroundWindow() == _gameWindowHandle)
{
    Log("Game window already has focus");  // FALSE! Only visual focus
    return true;  // Keyboard input still goes elsewhere
}
```

The `GetForegroundWindow()` API reports the window that's visually on top, but this doesn't guarantee keyboard input will be received by that window.

---

## Solution

Never trust `GetForegroundWindow()` for keyboard focus verification. Instead, always perform a physical click on the game window before sending keyboard input:

```csharp
private void EnsureGameHasKeyboardFocus()
{
    // Don't check GetForegroundWindow() - it lies about keyboard focus
    
    // Get window position
    var (x, y, width, height) = GetWindowRect();
    
    if (width > 0 && height > 0)
    {
        var centerX = x + width / 2;
        var centerY = y + height / 2;
        
        // Physical click guarantees keyboard focus
        _inputSimulator.Click(centerX, centerY);
        Thread.Sleep(200); // Wait for focus to establish
    }
}
```

---

## Key Insight

**Physical Click = Guaranteed Keyboard Focus**

The ONLY reliable way to ensure keyboard focus on Windows is to send a mouse click to the target window. This is because Windows grants keyboard focus to windows that receive user-initiated (or simulated) mouse clicks.

---

## Related Issues

- [ISSUE-004-Keyboard-Input-Focus](ISSUE-004-Keyboard-Input-Focus.md)
- [ISSUE-006-SetForegroundWindow-Restrictions](ISSUE-006-SetForegroundWindow-Restrictions.md)

---

## Related Files

- `src/Brinell.Stride/Infrastructure/StrideTestContext.cs`
- `src/Brinell.Stride/Input/StrideInputSimulator.cs`
