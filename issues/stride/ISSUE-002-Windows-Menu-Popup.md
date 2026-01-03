# ISSUE-002: Windows Menu Popup During Tests

**Created:** January 3, 2026  
**Status:** Resolved  
**Severity:** Medium  
**Category:** Input Simulation  

---

## Summary

The Windows Start menu appears during test execution, indicating that the Windows key or Alt key is being triggered unintentionally.

---

## Symptoms

- Windows Start menu appearing during test runs
- System menus opening unexpectedly
- Test execution interrupted by UI popups

---

## Root Cause

The `HoldKey` method in `StrideInputSimulator.cs` calls `KeyDown()`, waits, then `KeyUp()`. If a test fails or crashes between `KeyDown()` and `KeyUp()`, the key remains held.

The Windows Start menu appears when:
- Windows key is pressed
- Alt is pressed alone (opens system menu)
- Shift+F10 opens context menu

---

## Analysis

Looking at `StrideInputSimulator.cs`:
- The `HoldKey` method had no try/finally protection
- Keys could remain pressed after test failure
- Alt key is close to arrow keys; may be stray input
- No cleanup of modifier keys on test dispose

---

## Solution Implemented

### Fix 1: Added `ReleaseAllModifiers()` Method

```csharp
public void ReleaseAllModifiers()
{
    KeyUp(VirtualKey.Shift);
    KeyUp(VirtualKey.Control);
    KeyUp(VirtualKey.Alt);
    KeyUp(VirtualKey.LeftWindows);
    KeyUp(VirtualKey.RightWindows);
}
```

### Fix 2: Added LeftWindows/RightWindows to VirtualKey Enum

```csharp
LeftWindows = 0x5B,
RightWindows = 0x5C,
```

### Fix 3: Wrapped Key Operations in Try/Finally

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

### Fix 4: Called `ReleaseAllModifiers()` in Test Dispose

Added to `StrideUITestBase.DisposeAsync()` before closing the game.

---

## Verification

- [ ] Run tests and verify no Windows menu popups
- [ ] Verify Alt key doesn't open system menu
- [ ] Verify modifier keys are released after test failures

---

## Related Files

- `src/Brinell.Stride/Input/StrideInputSimulator.cs`
- `src/Brinell.Stride/Input/VirtualKey.cs`
- `src/Brinell.Stride/Infrastructure/StrideUITestBase.cs`
