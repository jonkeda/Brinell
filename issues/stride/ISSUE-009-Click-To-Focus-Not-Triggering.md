# ISSUE-009: Click-to-Focus Not Being Executed

**Created:** January 3, 2026  
**Status:** In Progress  
**Severity:** High  
**Category:** Focus Management  

---

## Summary

The click-to-focus code exists but is not being executed. No "Clicking center" log messages appear despite the code being present.

---

## Symptoms

- No "Clicking center" or "Clicking game window" logs appearing
- Focus click code is bypassed
- `GetWindowInfo()` returning null, causing click code to be skipped

---

## Root Cause

The `GetWindowInfo()` method returns null, which causes the focus click code to be skipped:

```csharp
private void EnsureGameHasFocus()
{
    var windowInfo = GetWindowInfo();
    if (windowInfo == null)
    {
        // This branch is being taken
        // Click code never executes
        return;
    }
    
    // Click code here is never reached
    var centerX = windowInfo.WindowX + windowInfo.WindowWidth / 2;
    var centerY = windowInfo.WindowY + windowInfo.WindowHeight / 2;
    _inputSimulator.Click(centerX, centerY);
}
```

---

## Investigation Needed

1. Why is `GetWindowInfo()` returning null?
2. Is the pipe communication failing?
3. Is the game not responding to window info requests?

---

## Solution

### Add Win32 GetWindowRect Fallback

Don't rely solely on pipe communication for window position:

```csharp
private (int x, int y, int width, int height)? GetWindowRectFallback()
{
    if (_gameWindowHandle == IntPtr.Zero) return null;
    
    if (GetWindowRect(_gameWindowHandle, out RECT rect))
    {
        return (rect.Left, rect.Top, 
                rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
    return null;
}

private void EnsureGameHasFocus()
{
    // Try pipe query first
    var windowInfo = GetWindowInfo();
    int x, y, width, height;
    
    if (windowInfo != null && windowInfo.WindowWidth > 0)
    {
        x = windowInfo.WindowX;
        y = windowInfo.WindowY;
        width = windowInfo.WindowWidth;
        height = windowInfo.WindowHeight;
    }
    else
    {
        // Fallback to Win32 API
        var fallback = GetWindowRectFallback();
        if (!fallback.HasValue)
        {
            Log("ERROR: Cannot determine window position");
            return;
        }
        (x, y, width, height) = fallback.Value;
    }
    
    var centerX = x + width / 2;
    var centerY = y + height / 2;
    Log($"Clicking game window center at ({centerX}, {centerY})");
    _inputSimulator.Click(centerX, centerY);
    Thread.Sleep(200);
}
```

### Win32 API Declarations

```csharp
[DllImport("user32.dll")]
private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

[StructLayout(LayoutKind.Sequential)]
private struct RECT 
{ 
    public int Left, Top, Right, Bottom; 
}
```

---

## Verification

After implementing the fix:

1. Run single test
2. Verify "Clicking game window center" appears in logs
3. Verify coordinates are valid (not 0,0)

---

## Related Issues

- [ISSUE-004-Keyboard-Input-Focus](ISSUE-004-Keyboard-Input-Focus.md)
- [ISSUE-005-GetForegroundWindow-Unreliable](ISSUE-005-GetForegroundWindow-Unreliable.md)

---

## Related Files

- `src/Brinell.Stride/Infrastructure/StrideTestContext.cs`
