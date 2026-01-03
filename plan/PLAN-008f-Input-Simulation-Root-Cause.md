# PLAN-008f: Input Simulation Root Cause Analysis & Fix Design

## Executive Summary

Stride UI tests require keyboard input simulation to test player movement (WASD) and settings navigation (ESC). The current implementation has multiple failure modes causing tests to pass individually but fail in batch.

---

## Root Cause Analysis

### Issue 1: Simulated Keyboard Crashes Game ✅ RESOLVED

**Symptom:** `InvalidOperationException` in `PoolListStruct.Remove()` during input processing.

**Root Cause:** `InputSourceSimulated.SimulateDown()` called from pipe handler thread, but Stride expects game thread.

**Resolution:** Removed simulated keyboard code, using Windows `SendInput` instead.

---

### Issue 2: `GetForegroundWindow()` Lies About Focus

**Symptom:** Logs show "Game window already has focus" but keyboard input goes elsewhere.

**Root Cause:** Windows has TWO types of focus:
- **Foreground Window** - The window visually in front
- **Keyboard Focus** - The window receiving keyboard input

A window can be foreground WITHOUT having keyboard focus when:
- The window was activated programmatically (not by user click)
- The test runner process (VS Code) retains keyboard focus
- Windows blocks focus stealing for security

---

### Issue 3: `SetForegroundWindow()` Restrictions

**Symptom:** `SetForegroundWindow()` call completes but window doesn't get focus.

**Root Cause:** Windows restricts focus stealing. It only works if:
- Calling process is already foreground
- User just interacted with the calling process  
- There's no foreground window
- It's responding to a system request

Test processes typically don't meet these conditions.

---

### Issue 4: Click-to-Focus Not Triggering

**Symptom:** No "Clicking center" logs appearing despite code being present.

**Root Cause:** `GetWindowInfo()` returning null, causing click code to be skipped.

**Investigation Needed:** Why is GetWindowInfo returning null?

---

### Issue 5: Tests Pass Solo, Fail in Batch

**Symptom:** Same test passes when run alone, fails when run with others.

**Root Cause:** Even with sequential execution (`parallelizeTestCollections: false`):
- Previous game process may still be closing
- New game window doesn't immediately get focus  
- Focus state is unpredictable between tests

---

## Design: Reliable Input Simulation

### Core Principle: Physical Click = Guaranteed Keyboard Focus

The ONLY reliable way to get keyboard focus on Windows is to **send a mouse click to the window**.

### Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                      TEST PROCESS                              │
├────────────────────────────────────────────────────────────────┤
│  1. Query game for window position via pipe                   │
│  2. Use Win32 GetWindowRect as fallback                       │
│  3. Calculate center point of window                          │
│  4. SendInput: mouse click at center                          │
│  5. Wait 200ms for focus to establish                         │
│  6. SendInput: keyboard events                                │
└───────────────────────────┬────────────────────────────────────┘
                            │ Named Pipe / Win32 API
                            ▼
┌────────────────────────────────────────────────────────────────┐
│                      GAME PROCESS                              │
├────────────────────────────────────────────────────────────────┤
│  • Receives click → Window gets keyboard focus                │
│  • Receives keyboard → Processes WASD/ESC                     │
│  • Reports window info via pipe                               │
└────────────────────────────────────────────────────────────────┘
```

### Key Design Decisions

1. **Always Click Before Keyboard Input**
   - Never trust `GetForegroundWindow()`
   - Always click the window before any keyboard input
   - This guarantees keyboard focus

2. **Multiple Window Info Sources**
   - Primary: Query game via pipe (`GetWindowInfo`)
   - Fallback: Win32 `GetWindowRect()` using window handle
   - This ensures we always have coordinates

3. **Adequate Timing**
   - 200ms after click before keyboard input
   - 50ms hold time for `PressKey` (game at 60fps = 16ms/frame)
   - Ensures at least 2-3 frames for input detection

4. **Per-Input Focus**
   - Call `EnsureGameHasKeyboardFocus()` before EVERY keyboard action
   - Don't assume focus persists between actions

---

## Implementation Steps

### Step 1: Add GetWindowRect Fallback

**Goal:** Get window coordinates even if pipe query fails.

**Implementation:**
```csharp
private (int x, int y, int width, int height)? GetWindowRectFallback()
{
    if (_gameWindowHandle == IntPtr.Zero) return null;
    
    if (GetWindowRect(_gameWindowHandle, out RECT rect))
    {
        return (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
    return null;
}

[DllImport("user32.dll")]
private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

[StructLayout(LayoutKind.Sequential)]
private struct RECT { public int Left, Top, Right, Bottom; }
```

**Test:** Run single test, verify logs show valid window coordinates.

---

### Step 2: Fix EnsureGameHasKeyboardFocus

**Goal:** Guarantee click happens before keyboard input.

**Implementation:**
```csharp
private void EnsureGameHasKeyboardFocus()
{
    // Try to get window rect (fallback to Win32 API if pipe fails)
    var (x, y, width, height) = GetWindowRect();
    
    if (width > 0 && height > 0)
    {
        var centerX = x + width / 2;
        var centerY = y + height / 2;
        Log($"Clicking game window center at ({centerX}, {centerY})");
        _inputSimulator.Click(centerX, centerY);
        Thread.Sleep(200); // Ensure focus is established
    }
    else
    {
        Log("ERROR: Cannot determine window position for focus click");
    }
}

private (int x, int y, int width, int height) GetWindowRect()
{
    // Try pipe query first
    var windowInfo = GetWindowInfo();
    if (windowInfo != null && windowInfo.WindowWidth > 0)
    {
        return (windowInfo.WindowX, windowInfo.WindowY, 
                windowInfo.WindowWidth, windowInfo.WindowHeight);
    }
    
    // Fallback to Win32 API
    var fallback = GetWindowRectFallback();
    if (fallback.HasValue)
    {
        return fallback.Value;
    }
    
    return (0, 0, 0, 0);
}
```

**Test:** Run single test, verify "Clicking game window center" appears in logs.

---

### Step 3: Test Single Test

**Goal:** Verify focus mechanism works.

**Test Command:**
```
dotnet test --filter "Player_MoveNorth_PositionIncreases" --no-build
```

**Expected:** Test passes, position changes from (0.0, 0.0).

---

### Step 4: Test All Keyboard Tests

**Goal:** Verify batch execution works.

**Test Command:**
```
dotnet test --filter "Player_Move|PressEscape|OpenAndClose|Settings" --no-build
```

**Expected:** All 10 tests pass.

---

## Additional Issues (If Tests Still Fail)

### Settings Page Tests

If settings tests fail after focus fix:
- Check if ESC key is being detected (game logs)
- Verify settings overlay visibility state
- Check `SettingsOverlay` automation ID exists

### Reset Button Test

Expected: 80, Actual: 25
- Check game's reset handler
- Verify slider default values in code

---

## Success Criteria

| Criteria | Status |
|----------|--------|
| Single movement test passes | ⬜ |
| Single ESC test passes | ⬜ |
| All 10 tests pass in batch | ⬜ |
| No "keyboard focus" warning logs | ⬜ |
| Shift key doesn't get stuck | ⬜ |
