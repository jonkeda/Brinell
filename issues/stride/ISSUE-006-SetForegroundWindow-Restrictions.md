# ISSUE-006: SetForegroundWindow() Doesn't Grant Focus

**Created:** January 3, 2026  
**Status:** In Progress  
**Severity:** High  
**Category:** Windows API / Focus Management  

---

## Summary

Calling `SetForegroundWindow()` completes successfully but the window doesn't actually receive keyboard focus due to Windows focus-stealing restrictions.

---

## Symptoms

- `SetForegroundWindow()` returns true (success)
- Window appears to come to front visually
- But keyboard input still goes to another window
- Tests fail because keys aren't received by game

---

## Root Cause

Windows restricts focus stealing as a security/usability feature. `SetForegroundWindow()` only works if:

1. The calling process is already the foreground process
2. The user just interacted with the calling process  
3. There's no foreground window
4. The window is responding to a system request

Test processes typically don't meet these conditions because:
- VS Code (test runner) is the foreground process
- No recent user interaction with test process
- Another window is already in front

---

## Windows Focus Stealing Prevention

From Microsoft documentation:
> An application cannot force a window to the foreground while the user is working with another window.

This is why programmatic focus changes are unreliable for automated testing.

---

## Workarounds Attempted

### 1. AllowSetForegroundWindow
```csharp
AllowSetForegroundWindow(ASFW_ANY);
```
Doesn't help because caller isn't foreground.

### 2. AttachThreadInput
```csharp
AttachThreadInput(GetCurrentThreadId(), GetWindowThreadProcessId(hwnd), true);
SetForegroundWindow(hwnd);
AttachThreadInput(GetCurrentThreadId(), GetWindowThreadProcessId(hwnd), false);
```
Occasionally works but unreliable.

### 3. SwitchToThisWindow
```csharp
SwitchToThisWindow(hwnd, true);
```
Deprecated and equally unreliable.

---

## Solution

**Use physical mouse click instead of `SetForegroundWindow()`:**

```csharp
private void EnsureGameHasKeyboardFocus()
{
    // Don't use SetForegroundWindow - Windows restrictions make it unreliable
    
    var (x, y, width, height) = GetWindowRect();
    
    if (width > 0 && height > 0)
    {
        var centerX = x + width / 2;
        var centerY = y + height / 2;
        
        // SendInput mouse click is always reliable
        _inputSimulator.Click(centerX, centerY);
        Thread.Sleep(200);
    }
}
```

A simulated mouse click via `SendInput` is treated as user interaction, which bypasses focus stealing prevention.

---

## Related Issues

- [ISSUE-004-Keyboard-Input-Focus](ISSUE-004-Keyboard-Input-Focus.md)
- [ISSUE-005-GetForegroundWindow-Unreliable](ISSUE-005-GetForegroundWindow-Unreliable.md)

---

## Related Files

- `src/Brinell.Stride/Infrastructure/StrideTestContext.cs`
