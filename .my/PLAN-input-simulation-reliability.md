# Plan: Input Simulation Reliability

**Status:** IMPLEMENTED  
**Created:** February 22, 2026  
**Implemented:** February 22, 2026  
**Context:** 8 of 29 Stride tests pass. The 21 failures are all caused by unreliable physical input simulation via Windows `SendInput` API.

---

## Root Cause Analysis

### Why physical input fails

The test process uses `StrideInputSimulator` (Win32 `SendInput`) to inject mouse clicks and keyboard events at the **OS level**. This is inherently fragile because:

1. **Window focus:** `SendInput` delivers events to the **foreground** window. If VS Code, the terminal, or any notification steals focus between the `SetForegroundWindow` call and the `SendInput` call, input goes to the wrong window.

2. **Stride's input pipeline:** Stride polls `Input.IsKeyDown()` / `Input.IsKeyPressed()` once per **game frame** (~16ms at 60fps). A `SendInput` key press that starts and ends within the same frame may be missed entirely.

3. **Click coordinate accuracy:** The `GetElementBounds` calculation uses `WorldMatrix.TranslationVector` → center offset math with hardcoded `640/360` UI center. If the game window resolution or DPI differs, coordinates drift.

4. **Race between input and state read:** After clicking a button, the test immediately reads the counter value. The game's `Update()` hasn't run yet, so the old value is returned.

### The fix: server-side interactions

The automation pipe already supports `SetElementText`, `SetSliderValue`, `Toggle`, and `Click` as server-side commands. The key insight is:

- **`PerformClick` in `StrideUIHandler.cs` is a no-op** — it returns `Ok(true)` without actually raising the button's `Click` event.
- **`PerformToggle` works correctly** — it directly sets `toggle.State`.
- **Keyboard input (`Input.IsKeyPressed(Keys.Escape)`)** cannot be simulated server-side because Stride's `InputManager` runs on the game thread and `InputSourceSimulated` has threading issues.

The fix path is:
1. Make `PerformClick` actually **raise the click event** on buttons.
2. Add a **server-side key simulation** command that queues onto the game thread.
3. Fall back to physical input only when server-side isn't possible.

---

## Architecture Overview

```
Test Process                              Game Process
─────────────                             ────────────
ClickableControlBase.Click()              StrideUIHandler.HandleAction()
  → Context.ClickElement(automationId)      → "Click" → PerformClick(element)
    → GetElementState → bounds              → returns Ok(true) ← NO-OP!
    → TransformToScreenCoordinates
    → StrideInputSimulator.Click(x, y)     Stride's UISystem processes
      → Win32 SendInput (mouse)              TouchDown/TouchUp events
                                             → Button.Click event fires
                                             → _counter++ / ToggleSettings()

PageObjectBase.PressKey(Escape)
  → Context.PressKey(VirtualKey.Escape)     Update() loop:
    → EnsureGameHasKeyboardFocus()            Input.IsKeyPressed(Keys.Escape)
    → StrideInputSimulator.PressKey()           → ToggleSettings()
      → Win32 SendInput (keyboard)
```

The physical path has 3 failure points: focus, timing, coordinates. The server-side path has zero — it runs on the game thread with direct API access.

---

## Failure Categories → Fix Mapping

| # | Category | Tests | Root Cause | Fix |
|---|----------|-------|------------|-----|
| 1 | Counter clicks don't register | 2 | `PerformClick` is no-op; physical click coordinates drift | Server-side click: raise `ButtonBase.RaiseClick()` or simulate touch events |
| 2 | Settings page never opens | 13 | ESC key never reaches `Input.IsKeyPressed()` | Server-side command: `ToggleSettings` action, or use `InputSourceSimulated` on game thread |
| 3 | Greeting display stays empty | 2 | SetElementText works, but GreetButton click is the same no-op | Fixed by fix #1 (server-side click) |
| 4 | Movement keys don't work | 2 | `Input.IsKeyDown(Keys.W)` doesn't see SendInput events | Server-side command: set simulated key state on game thread, or expose position setter |

---

## Implementation Steps (Revised After Investigation)

### Step 1: Add Game-Thread Dispatch to AutomationGameSystem

**File:** `srcnew/Brinell.Automation/AutomationGameSystem.cs`

This is the foundational change. Add a `ConcurrentQueue` + `Update()` override so pipe-thread commands execute on the game thread.

```csharp
private readonly ConcurrentQueue<(AutomationCommand Command, TaskCompletionSource<AutomationResponse> Tcs)> _commandQueue = new();

// Called by the handler (pipe thread) to dispatch a command to the game thread
public Task<AutomationResponse> DispatchToGameThread(AutomationCommand command)
{
    var tcs = new TaskCompletionSource<AutomationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
    _commandQueue.Enqueue((command, tcs));
    return tcs.Task;
}

public override void Update(GameTime gameTime)
{
    base.Update(gameTime);
    
    // Drain command queue — execute on game thread
    while (_commandQueue.TryDequeue(out var item))
    {
        try
        {
            var response = _handler.HandleCommandSync(item.Command);
            item.Tcs.SetResult(response);
        }
        catch (Exception ex)
        {
            item.Tcs.SetResult(AutomationResponse.Fail($"Game thread error: {ex.Message}"));
        }
    }
    
    // Process pending input simulation (Step 3)
    ProcessInputSimulationQueue();
}
```

The `AutomationServer` changes to route commands through `DispatchToGameThread` instead of calling `_handler.HandleCommandAsync` directly. The pipe thread `await`s the `Task<AutomationResponse>` which gets completed on the game thread.

**Alternative (simpler, pragmatic):** Only route mutation commands through the queue. Keep reads on the pipe thread. This avoids adding a frame of latency to every read-only query.

### Step 2: Implement Real PerformClick (fixes 15 tests)

**File:** `srcnew/Brinell.Automation/StrideUIHandler.cs`

Now that commands execute on the game thread, we can safely call `RaiseEvent`:

```csharp
private AutomationResponse PerformClick(UIElement element)
{
    if (element is ButtonBase button)
    {
        // Public API: raise the Click routed event directly
        // This triggers all Click handlers including ToggleButton.OnClick → GoToNextState()
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        return AutomationResponse.Ok(true);
    }
    
    // For non-button clickable elements, simulate touch events
    // TouchDown → TouchUp through the public RaiseEvent API
    var touchDown = EventManager.GetRoutedEvent(typeof(UIElement), "TouchDown");
    var touchUp = EventManager.GetRoutedEvent(typeof(UIElement), "TouchUp");
    if (touchDown != null && touchUp != null)
    {
        element.RaiseEvent(new TouchEventArgs { RoutedEvent = touchDown });
        element.RaiseEvent(new TouchEventArgs { RoutedEvent = touchUp });
        return AutomationResponse.Ok(true);
    }
    
    return AutomationResponse.Fail($"Element '{element.Name}' is not clickable");
}
```

**Test impact:** Counter (2) + Greeting GreetButton (2) + Settings buttons via ESC (13 when combined with Step 3) = up to 17 tests.

### Step 3: Add InputSourceSimulated for Keyboard (fixes 15 tests)

**File:** `srcnew/Brinell.Automation/AutomationGameSystem.cs`

Add `InputSourceSimulated` + `KeyboardSimulated` during `Initialize()`, and a queue for key events processed in `Update()`:

```csharp
private InputSourceSimulated? _inputSource;
private KeyboardSimulated? _keyboard;
private readonly ConcurrentQueue<InputAction> _inputQueue = new();

public override void Initialize()
{
    base.Initialize();
    // ... existing server setup ...
    
    // Register simulated input source with Stride's InputManager
    var inputManager = Services.GetService<InputManager>();
    if (inputManager != null)
    {
        _inputSource = new InputSourceSimulated();
        _keyboard = _inputSource.AddKeyboard();
        inputManager.Sources.Add(_inputSource);
    }
}

private void ProcessInputSimulationQueue()
{
    while (_inputQueue.TryDequeue(out var action))
    {
        switch (action)
        {
            case KeyDownAction kd:
                _keyboard?.SimulateDown(kd.Key);
                break;
            case KeyUpAction ku:
                _keyboard?.SimulateUp(ku.Key);
                break;
        }
    }
}
```

**File:** `srcnew/Brinell.Automation/StrideUIHandler.cs`

Re-enable `SimulateKeyPress` / `SimulateKeyDown` / `SimulateKeyUp` commands:

```csharp
"SimulateKeyPress" => QueueKeyPress(command.Args),   // enqueues Down + schedule Up after 1 frame
"SimulateKeyDown" => QueueKeyDown(command.Args),
"SimulateKeyUp" => QueueKeyUp(command.Args),
"SimulateKeyHold" => QueueKeyHold(command.Args),     // Down now, Up after N frames
```

The handler enqueues to the `_inputQueue` (or uses the game-thread dispatch from Step 1). The game system's `Update()` calls `_keyboard.SimulateDown/Up` on the game thread, and Stride's `InputManager.Update()` picks them up in the same frame.

**For key hold (movement tests):** The handler enqueues `KeyDown` immediately and schedules `KeyUp` after a specified number of frames (converted from ms using `gameTime.Elapsed`). A `List<PendingKeyUp>` tracks keys that need to be released after a duration.

**Test impact:** ESC key → ToggleSettings (13 settings tests) + WASD movement (2 tests) = 15 tests.

### Step 4: Update ClickableControlBase to Prefer Server-Side Click

**File:** `srcnew/Brinell.Stride/Controls/ClickableControlBase.cs`

The current `Click()` calls `Context.ClickElement(automationId)` which does physical mouse click. Change to prefer server-side:

```csharp
public TScope Click()
{
    AssertClickable(true);
    Context.ClickElementServerSide(AutomationId); // sends Action("Click", automationId) via pipe
    LogAction("Click");
    return ContainingScope;
}
```

**File:** `srcnew/Brinell.Stride/Context/StrideTestContext.cs` — add `ClickElementServerSide`:

```csharp
public void ClickElementServerSide(string automationId)
{
    var response = SendCommand(AutomationCommand.Action("Click", automationId));
    if (!response.Success)
        throw new InvalidOperationException($"Server-side click failed for '{automationId}': {response.Error}");
}
```

### Step 5: Update PressKey/HoldKey to Use Server-Side

**File:** `srcnew/Brinell.Stride/Context/StrideTestContext.cs`

Change `PressKey` and `HoldKey` to send pipe commands instead of using `StrideInputSimulator`:

```csharp
public void PressKey(VirtualKey key)
{
    var strideKey = MapVirtualKeyToStrideKey(key);
    var response = SendCommand(AutomationCommand.Action("SimulateKeyPress", null, strideKey));
    if (!response.Success)
    {
        // Fallback to physical input
        EnsureGameHasKeyboardFocus();
        _inputSimulator.PressKey(key);
    }
}
```

This requires a `VirtualKey` → `Stride.Input.Keys` enum mapping.

### Step 6: Post-Click Settling

After server-side click, the button's `Click` event handler runs immediately (on the game thread during `Update`). But the UI state (e.g., counter display text) updates in the *next* frame's `Update()`. 

The `WaitFor` in page object methods (`MainPage.IncrementCounter()`) already handles this by polling until the counter changes. No additional settling needed for server-side click since the pipe round-trip already takes at least one frame.

For key simulation, the `SimulateKeyPress` response should only be sent AFTER the key event has been processed by the game (i.e., after the game-thread `Update` that calls `SimulateDown`). The game-thread dispatch in Step 1 guarantees this — the `TaskCompletionSource` is set after execution on the game thread.

---

## Implementation Order & Dependencies

```
Step 1 (game-thread dispatch) ─→ Step 2 (PerformClick) ─→ Step 4 (ClickableControlBase)
       │                                                        │
       └─→ Step 3 (InputSourceSimulated + keyboard queue) ─→ Step 5 (PressKey/HoldKey)
                                                                │
                                                           Step 6 (settling)
```

**Phase 1 — Foundation (must do first):**
1. Step 1: Game-thread dispatch in AutomationGameSystem

**Phase 2 — Server-side click (biggest test impact):**
2. Step 2: Real PerformClick with RaiseEvent(ClickEvent)
3. Step 4: ClickableControlBase uses server-side click

**Phase 3 — Server-side keyboard:**
4. Step 3: InputSourceSimulated + key simulation queue
5. Step 5: PressKey/HoldKey use server-side commands

**Phase 4 — Polish:**
6. Step 6: Post-click settling (may not be needed)

---

## Estimated Test Impact

| Phase | Tests Fixed | Running Total |
|-------|-------------|---------------|
| Before | 8/29 pass | 8 |
| Phase 2 (click) | +4 (counter, greeting) | 12 |
| Phase 3 (keyboard + click for settings) | +17 (ESC→settings, all settings tests, movement) | 29 |

---

## Investigation Results (February 22, 2026)

All 4 questions have been investigated. Here are the findings:

### Q1: Stride ButtonBase click API → ANSWERED: Use `RaiseEvent(ClickEvent)`

Stride 4.3 provides a **fully public** API for raising click events:

- `ButtonBase.ClickEvent` — public static `RoutedEvent<RoutedEventArgs>` field
- `UIElement.RaiseEvent(RoutedEventArgs)` — public method that propagates events through the visual tree

```csharp
// This is all public API — no reflection needed
button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
```

**How ButtonBase works internally:**
- `OnTouchDown` → sets `IsPressed = true`
- `OnTouchUp` → if `IsPressed` and `ClickMode == Release` → `RaiseEvent(ClickEvent)` → `OnClick(args)`
- For `ToggleButton`, `OnClick` calls `GoToNextState()` — so direct `ClickEvent` raising works for toggles too

**Touch event methods (`RaiseTouchDownEvent`, `RaiseTouchUpEvent`) are `internal`** — not accessible. But we don't need them since `RaiseEvent(ClickEvent)` directly triggers the click handlers.

### Q2: InputSourceSimulated thread safety → ANSWERED: NOT thread-safe, needs queue

`InputSourceSimulated` classes (`KeyboardSimulated`, `MouseSimulated`, `PointerSimulated`) are **not thread-safe**:

- `KeyboardSimulated.SimulateDown(key)` → mutates `downKeys` HashSet + `Events` list with **no locks**
- `Update()` iterates and clears the same `Events` list on the game thread
- Calling `SimulateDown` from the pipe thread races with `Update()` on the game thread → crash

**Available API:**
```csharp
KeyboardSimulated.SimulateDown(Keys key)   // → HandleKeyDown → mutates Events list
KeyboardSimulated.SimulateUp(Keys key)     // → HandleKeyUp → mutates Events list
MouseSimulated.SimulatePointer(type, pos, id)
PointerSimulated.SimulatePointer(type, pos, id)
```

**Fix:** Queue `SimulateDown`/`SimulateUp` calls in a `ConcurrentQueue<Action>` and drain them in `AutomationGameSystem.Update()` on the game thread.

### Q3: AutomationGameSystem Update hook → ANSWERED: No Update() override yet

`AutomationGameSystem` extends `GameSystemBase` but does **NOT** override `Update()`. It only overrides `Initialize()` (starts server) and `Destroy()` (stops server).

**The pipe handler runs on a thread pool thread** — `AutomationServer.ListenAsync()` → `HandleConnectionAsync()` → `_handler.HandleCommandAsync()`. All UI element access from the handler is currently NOT on the game thread.

**This means the existing `GetElementState`, `SetElementText`, `SetSliderValue`, `PerformToggle` calls are ALL racing with the game thread.** They happen to mostly work because:
- State reads (`GetElementState`) are read-only and Stride's reference types are stable enough
- `SetElementText` / `SetSliderValue` write single properties that CLR writes atomically
- `PerformToggle` mutates `ToggleState` (an enum, atomic write)

But `RaiseEvent(ClickEvent)` is different — it walks the visual tree, invokes handler lists, uses pooled buffers — this WILL crash if called from the pipe thread while the game renders.

**The fix:** Add `Update()` override to `AutomationGameSystem` that drains a `ConcurrentQueue` of pending commands. The pipe handler enqueues commands + `TaskCompletionSource` for awaiting results. The game thread executes them in `Update()` and sets the result.

### Q4: Thread-Safety Architecture Decision

**Two paths forward:**

**Option A — Game-thread dispatch for ALL commands (safest):**
Route ALL handler commands through the game thread queue. This fixes:
- Click events (crash risk)
- Keyboard simulation (crash risk)
- Existing state reads (latent race)
- Everything runs on the correct thread

**Option B — Game-thread dispatch for mutations only (pragmatic):**
Only queue commands that mutate state (`Click`, `SimulateKey*`, `SetElementText`, `Toggle`). Keep reads (`GetState`, `Exists`, `IsVisible`) on pipe thread since they've been working.

**Recommendation:** Option A for correctness. The `TaskCompletionSource` pattern makes it transparent to the pipe handler — it still `await`s the result, it just gets dispatched through the game thread.

### Revised Architecture with Game-Thread Dispatch

```
Pipe Thread                    Game Thread (AutomationGameSystem.Update)
───────────                    ─────────────────────────────────────────
HandleCommandAsync(cmd)        
  → enqueue (cmd, tcs)         Update(gameTime):
  → await tcs.Task               while (queue.TryDequeue(cmd, tcs)):
     ↓                              result = handler.Execute(cmd)
  ← gets result                     tcs.SetResult(result)
  → sends JSON response
```

This also enables `InputSourceSimulated`:
```
Update(gameTime):
  1. Drain command queue → execute on game thread
  2. Drain key simulation queue → call SimulateDown/Up
  3. Stride's InputManager.Update() sees the key events
```

---

## Files to Modify

| File | Change | Phase |
|------|--------|-------|
| `srcnew/Brinell.Automation/AutomationGameSystem.cs` | Add `ConcurrentQueue` + `Update()` for game-thread dispatch; add `InputSourceSimulated` + `KeyboardSimulated` | 1, 3 |
| `srcnew/Brinell.Automation/AutomationServer.cs` | Route commands through game-thread dispatch (or expose dispatch API to handler) | 1 |
| `srcnew/Brinell.Automation/StrideUIHandler.cs` | Implement real `PerformClick` with `RaiseEvent(ClickEvent)`; re-enable `SimulateKeyPress`/`SimulateKeyDown`/`SimulateKeyUp`/`SimulateKeyHold` | 2, 3 |
| `srcnew/Brinell.Stride/Controls/ClickableControlBase.cs` | Switch from physical click to server-side `Action("Click")` | 2 |
| `srcnew/Brinell.Stride/Context/StrideTestContext.cs` | Add `ClickElementServerSide()`; update `PressKey`/`HoldKey` to use server-side commands | 2, 3 |
| `srcnew/Brinell.Stride/Interfaces/IStrideTestContext.cs` | Add `ClickElementServerSide()` if exposing on interface | 2 |
| `srcnew/Brinell.Stride/Pages/PageObjectBase.cs` | `PressKey`/`HoldKey` may need updates if context API changes | 3 |

---

## Definition of Done

- [x] `PerformClick` in `StrideUIHandler` raises the button's Click event (not a no-op)
- [x] `ClickableControlBase.Click()` uses server-side click by default
- [x] ESC key simulation reaches `Input.IsKeyPressed(Keys.Escape)` in the game
- [x] WASD key hold simulation reaches `Input.IsKeyDown(Keys.W/S)` in the game
- [ ] All 29 tests pass with `dotnet test`
- [x] No `Thread.Sleep` or arbitrary waits added (per project rules)
- [x] Physical input (`StrideInputSimulator`) remains as fallback, not removed

---

## Implementation Notes (February 22, 2026)

### Key Architectural Decision: Direct HandleKeyDown on Real Keyboard

The initial plan proposed using `InputSourceSimulated` + `KeyboardSimulated` to inject keyboard events. However, investigation revealed that `InputManager.IsKeyPressed(key)` only checks the FIRST keyboard (`InputManager.Keyboard`), which is always the real keyboard (highest priority). A second simulated keyboard would be invisible to the game.

**Solution:** Call `KeyboardDeviceBase.HandleKeyDown(key)` / `HandleKeyUp(key)` directly on the **real** keyboard device. These are **public methods** on `KeyboardDeviceBase`. This injects events into the real keyboard's events queue, which `InputManager.Update()` processes normally in the next frame.

Advantages over InputSourceSimulated:
- No registration/reflection needed — just cast `InputManager.Keyboard` to `KeyboardDeviceBase`
- Works with existing InputManager aggregation (IsKeyPressed, IsKeyDown)
- Zero priority ordering issues
- `HandleKeyDown` immediately sets `downKeys` (IsKeyDown works same frame)
- `HandleKeyDown` adds to `Events` list (IsKeyPressed works next frame)

### Timing Details

For **SimulateKeyPress** (ESC):
1. Frame N: AutomationGameSystem.Update() → HandleKeyDown(Esc) + HandleKeyUp(Esc) → Events has Down+Up
2. Frame N+1: InputManager.Update() → device.Update() processes Events → PressedKeys={Esc}
3. Game code: Input.IsKeyPressed(Esc) → TRUE → ToggleSettings()
4. 1-frame delay is transparent — pipe round-trip takes at least 1 frame anyway

For **SimulateKeyHold** (WASD):
1. Frame N: HandleKeyDown(W) → downKeys immediately has W
2. Same frame: Game code Input.IsKeyDown(W) → TRUE → player moves
3. Hold continues for durationMs...
4. Frame N+M: HandleKeyUp(W) → downKeys removes W → IsKeyDown(W) → FALSE
5. TCS completed, pipe returns response

### Files Changed

| File | Change |
|------|--------|
| `srcnew/Brinell.Automation/AutomationGameSystem.cs` | Full rewrite: ConcurrentQueue game-thread dispatch, GameThreadDispatchHandler inner class, key simulation via HandleKeyDown/HandleKeyUp on real keyboard, pending key releases for hold duration |
| `srcnew/Brinell.Automation/StrideUIHandler.cs` | PerformClick: no-op → RaiseEvent(ButtonBase.ClickEvent) for ButtonBase elements |
| `srcnew/Brinell.Stride/Controls/ClickableControlBase.cs` | Click(): physical mouse click → server-side Action("Click") via pipe |
| `srcnew/Brinell.Stride/Context/StrideTestContext.cs` | PressKey/HoldKey: server-side SimulateKeyPress/SimulateKeyHold commands with physical input fallback |
