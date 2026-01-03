# PLAN-008c: Stride Input Simulation Issues

**Created:** January 3, 2026
**Status:** In Progress

---

## Reported Issues

1. **Windows menu popup appears during tests** - Windows key or Alt key being triggered
2. **Shift key gets locked after test run** - Key not being released on test cleanup

---

## Root Cause Analysis

### Issue 1: Windows Menu Popup

The Windows Start menu appears when the Windows key is pressed, OR when Alt is pressed alone.

Looking at `StrideInputSimulator.cs`:
- The `HoldKey` method calls `KeyDown()`, waits, then `KeyUp()`
- If a test fails or crashes between `KeyDown()` and `KeyUp()`, the key remains held
- The Windows key (VK_LWIN = 0x5B) is not in VirtualKey enum, but Alt (0x12) IS
- Pressing Alt alone opens the system menu, Shift+F10 opens context menu

**Likely Cause:** 
- Tests that use `PressKey(VirtualKey.Escape)` or keyboard input
- If test fails before cleanup, keys may stay pressed
- Alt key is close to arrow keys on keyboard; may be stray input

### Issue 2: Shift Key Locked

Looking at test cleanup in `StrideUITestBase.DisposeAsync()`:
- **NO key release cleanup is performed**
- If a test calls `KeyDown(Shift)` and fails before `KeyUp(Shift)`, Shift stays pressed
- The `HoldKey()` method in tests like `MoveNorth(300)` uses WASD keys, not modifiers

**But wait:** The tests use `VirtualKey.W`, `VirtualKey.A`, etc., not Shift directly.

**Alternative Cause:**
- Some keyboard layouts interpret key combinations differently
- Windows Sticky Keys feature may be triggered
- The keyboard input may be interfering with Windows accessibility features

---

## Solution

### Fix 1: Add Key Cleanup on Test Dispose

Add a method to release all potentially stuck modifier keys in `StrideInputSimulator`:

```csharp
public void ReleaseAllModifiers()
{
    KeyUp(VirtualKey.Shift);
    KeyUp(VirtualKey.Control);
    KeyUp(VirtualKey.Alt);
    // KeyUp for Windows key: 0x5B (VK_LWIN)
}
```

Call this in `StrideUITestBase.DisposeAsync()` before closing the game.

### Fix 2: Add LeftWin/RightWin to VirtualKey Enum

Add Windows key codes to prevent accidental triggers:

```csharp
LeftWindows = 0x5B,
RightWindows = 0x5C,
```

### Fix 3: Wrap Key Operations in Try/Finally

Ensure `KeyUp` always happens even if test fails:

```csharp
public void HoldKey(VirtualKey key, int durationMs)
{
    try
    {
        KeyDown(key);
        Thread.Sleep(durationMs);
    }
    finally
    {
        KeyUp(key);
    }
}
```

---

## Implementation Checklist

- [x] Add `ReleaseAllModifiers()` method to `StrideInputSimulator`
- [x] Add LeftWindows/RightWindows to `VirtualKey` enum
- [x] Call `ReleaseAllModifiers()` in test dispose
- [x] Add try/finally to `HoldKey()` method
- [x] Add try/finally to `PressKeyCombination()` method
- [x] Add try/finally to `HotKey()` methods
- [ ] Test that keys are properly released after test failures

---

## Priority

**HIGH** - This affects developer experience and can cause issues outside of tests.
