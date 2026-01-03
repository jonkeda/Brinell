# PLAN-008e: Simulated Input Threading Issues

## Problem Statement

After implementing `InputSourceSimulated` and `KeyboardSimulated` for in-game keyboard simulation, the game crashes with:

```
System.InvalidOperationException: Operation is not valid due to the current state of the object.
   at Stride.Core.Collections.PoolListStruct`1.Remove(T item)
   at Stride.Input.InputEventPool`1.Pool.Enqueue(TEventType item)
   at Stride.Input.InputManager.Update(GameTime gameTime)
```

## Root Cause Analysis

The issue is a **threading violation**. The automation server processes commands on a background thread (the named pipe server thread), but Stride's input system expects input events to be queued from the game thread during the update loop.

When we call `_simulatedKeyboard.SimulateDown(key)` from the pipe handler thread, it modifies internal collections that are being accessed by the game's update loop, causing the thread-safety violation.

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

## Possible Solutions

### Solution 1: Queue Input Commands for Game Thread (Recommended)

Queue the simulated input commands and execute them on the game thread during the next Update():

```csharp
// In StrideUIHandler
private ConcurrentQueue<Action> _pendingInputActions = new();

private AutomationResponse SimulateKeyDown(string? keyName)
{
    // ...validation...
    _pendingInputActions.Enqueue(() => _simulatedKeyboard!.SimulateDown(key));
    return AutomationResponse.Ok(true);
}

// In AutomationGameSystem.Update() - called on game thread
while (_pendingInputActions.TryDequeue(out var action))
{
    action();
}
```

### Solution 2: Use SendInput with Better Focus Management

Keep using Windows `SendInput` API (current fallback) but improve focus management:

1. Ensure game window is focused
2. Wait for focus to be confirmed
3. Use SendInput
4. Add retry logic if input fails

### Solution 3: Expose Direct Position Control

For movement tests specifically, expose a `SetPlayerPosition()` command that bypasses input entirely:

```csharp
// In game
public void MovePlayer(float dx, float dz)
{
    _playerPosition.X += dx;
    _playerPosition.Z += dz;
    UpdatePositionDisplay();
}
```

## Current Status

- **Simulated Input**: Crashes game due to threading
- **SendInput Fallback**: Works when game has focus, but unreliable when running in parallel
- **Single Test Run**: Works with SendInput (1/1 passed when run alone)
- **Parallel Test Runs**: All 10 fail due to crash from simulated input attempt

## Immediate Fix

Remove the simulated input approach and rely solely on `SendInput` for now. The simulated input feature needs proper thread marshalling before it can work.

## Test Results

| Test | Result | Notes |
|------|--------|-------|
| Player_MoveNorth_PositionIncreases | PASS (single) / FAIL (batch) | Works alone with SendInput |
| Player_MoveEast_PositionChanges | FAIL | Game crashes on simulated input |
| Game_PressEscape_OpensSettings | FAIL | Game crashes on simulated input |
| All other keyboard tests | FAIL | Game crashes on simulated input |

## Next Steps

1. **Remove SimulateKey* calls** - Fall back to SendInput only
2. **Implement thread-safe input queue** - For future improvement
3. **Add ESC key handling** - Make sure ESC works with SendInput
4. **Re-run tests** - Verify SendInput works for all keyboard tests
