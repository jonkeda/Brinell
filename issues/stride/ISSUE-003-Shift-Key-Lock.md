# ISSUE-003: Shift Key Gets Locked After Test Run

**Created:** January 3, 2026  
**Status:** Resolved  
**Severity:** High  
**Category:** Input Simulation  

---

## Summary

The Shift key remains "pressed" after test execution completes, affecting system-wide keyboard behavior.

---

## Symptoms

- Shift key appears locked after test run
- Typing produces uppercase letters unexpectedly
- Shortcuts behave as if Shift is held
- Persists until manually pressing Shift key

---

## Root Cause

Looking at test cleanup in `StrideUITestBase.DisposeAsync()`:
- **NO key release cleanup was performed**
- If a test calls `KeyDown(Shift)` and fails before `KeyUp(Shift)`, Shift stays pressed
- Windows Sticky Keys feature may be triggered by repeated Shift presses
- Keyboard input may interfere with Windows accessibility features

---

## Investigation Notes

The tests primarily use `VirtualKey.W`, `VirtualKey.A`, `VirtualKey.S`, `VirtualKey.D` for movement, not Shift directly. However:
- Some keyboard layouts interpret key combinations differently
- The keyboard simulation may be triggering Windows accessibility features
- Incomplete cleanup on test failure leaves keys in pressed state

---

## Solution Implemented

### Fix 1: Added Modifier Key Cleanup on Test Dispose

```csharp
// In StrideUITestBase.DisposeAsync()
public async ValueTask DisposeAsync()
{
    try
    {
        // Release any stuck modifier keys
        TestContext.ReleaseAllModifiers();
    }
    finally
    {
        await CloseGameAsync();
    }
}
```

### Fix 2: Added `ReleaseAllModifiers()` Method

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

### Fix 3: Try/Finally Protection for Key Operations

All key holding operations now use try/finally to ensure cleanup:

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

public void PressKeyCombination(VirtualKey modifier, VirtualKey key)
{
    try
    {
        KeyDown(modifier);
        PressKey(key);
    }
    finally
    {
        KeyUp(modifier);
    }
}
```

---

## Verification

- [ ] Run full test suite
- [ ] Verify Shift key is not stuck after completion
- [ ] Verify Shift key is not stuck after test failure
- [ ] Verify no other modifier keys are stuck

---

## Related Files

- `src/Brinell.Stride/Input/StrideInputSimulator.cs`
- `src/Brinell.Stride/Infrastructure/StrideUITestBase.cs`
- `src/Brinell.Stride/Infrastructure/StrideTestContext.cs`
