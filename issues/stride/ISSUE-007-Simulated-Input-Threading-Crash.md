# ISSUE-007: Simulated Input Causes Game Crash (Threading Violation)

**Created:** January 3, 2026  
**Status:** Resolved  
**Severity:** Critical  
**Category:** Threading / Game Engine  

---

## Summary

Using Stride's `InputSourceSimulated` and `KeyboardSimulated` for in-game keyboard simulation crashes the game with a threading violation error.

---

## Symptoms

Game crashes with:
```
System.InvalidOperationException: Operation is not valid due to the current state of the object.
   at Stride.Core.Collections.PoolListStruct`1.Remove(T item)
   at Stride.Input.InputEventPool`1.Pool.Enqueue(TEventType item)
   at Stride.Input.InputManager.Update(GameTime gameTime)
```

---

## Root Cause

**Threading violation.** The automation server processes commands on a background thread (the named pipe server thread), but Stride's input system expects input events to be queued from the game thread during the update loop.

When `_simulatedKeyboard.SimulateDown(key)` is called from the pipe handler thread, it modifies internal collections that are being accessed by the game's update loop, causing a thread-safety exception.

---

## Architecture Issue

```
Test Process                          Game Process
     |                                     |
     |  -- pipe command -->               |
     |                          [Pipe Handler Thread]
     |                              SimulateDown() <-- WRONG THREAD!
     |                                     |
     |                          [Game Thread]
     |                              Update()
     |                              InputManager.Update()
     |                                  ^-- CRASH: collection modified
```

---

## Possible Solutions

### Solution 1: Queue Input Commands for Game Thread (Future)

Queue the simulated input commands and execute them on the game thread during the next Update():

```csharp
// In StrideUIHandler
private ConcurrentQueue<Action> _pendingInputActions = new();

private AutomationResponse SimulateKeyDown(string? keyName)
{
    _pendingInputActions.Enqueue(() => _simulatedKeyboard!.SimulateDown(key));
    return AutomationResponse.Ok(true);
}

// In AutomationGameSystem.Update() - called on game thread
while (_pendingInputActions.TryDequeue(out var action))
{
    action();
}
```

### Solution 2: Use Windows SendInput (Current)

Keep using Windows `SendInput` API which works from any thread:

```csharp
// SendInput works from test process
_inputSimulator.PressKey(VirtualKey.W);
```

This requires proper focus management but avoids threading issues.

---

## Resolution

**Removed simulated keyboard code, using Windows `SendInput` instead.**

The simulated input feature needs proper thread marshalling before it can work. For now, `SendInput` with proper focus management is the reliable solution.

---

## Test Results After Fix

| Test | Result | Notes |
|------|--------|-------|
| Player_MoveNorth_PositionIncreases | PASS (single) | Works alone with SendInput |
| Game crashes | FIXED | No longer crashes |

---

## Future Work

If native in-game input simulation is needed (e.g., for headless testing), implement the thread-safe input queue pattern described in Solution 1.

---

## Related Files

- `src/Brinell.Stride.Automation/StrideUIHandler.cs`
- `src/Brinell.Stride.Automation/AutomationGameSystem.cs`
- `src/Brinell.Stride/Input/StrideInputSimulator.cs`
