# Plan: Remove ALL Physical Input from Stride Framework

**Status:** IMPLEMENTED ✅  \n**Created:** February 22, 2026  \n**Completed:** February 22, 2026  \n**Result:** All 6 phases implemented. 29/29 tests pass. Build: 0 errors, 0 warnings. Zero SendInput references remain.", "oldString": "**Status:** PLANNING  \n**Created:** February 22, 2026  
**Predecessor:** PLAN-eliminate-physical-input.md (partially migrated)  
**Goal:** Zero physical Win32 SendInput calls. Every input operation flows through the automation pipe.

---

## Current State

29/29 tests pass. The primary paths (Click, SetText, PressKey, HoldKey) are already server-side. But physical input still leaks through:

| # | Location | Physical Operation | Used by Tests? |
|---|----------|--------------------|----------------|
| 1 | `StrideTestContext.TypeText()` | `_inputSimulator.TypeText()` + `EnsureGameHasFocus()` | No |
| 2 | `StrideTestContext.PressKey()` | Physical fallback when server-side fails | No (server works) |
| 3 | `StrideTestContext.HoldKey()` | Physical fallback when server-side fails | No (server works) |
| 4 | `StrideTestContext.EnsureGameHasFocus()` | Win32 focus P/Invoke + Alt key SendInput | Only by #1 and fallbacks |
| 5 | `StrideTestContext.EnsureGameHasKeyboardFocus()` | Physical mouse click at window center | Only by fallbacks |
| 6 | `StrideTestContext.ForceForegroundWindow()` | P/Invoke + NativeSendInput Alt key | Only by #4 |
| 7 | `EditText.SelectAll()` | Physical HotKey(Ctrl+A) fallback | No (server works) |
| 8 | `EditText.Copy/Cut/Paste()` | Physical HotKey(Ctrl+C/X/V) | No |
| 9 | `EditText.Undo/Redo()` | Physical HotKey(Ctrl+Z/Y) | No |
| 10 | `ClickableControlBase.DoubleClick()` | `Input.DoubleClick(bounds)` | No |
| 11 | `ClickableControlBase.RightClick()` | `Input.RightClick(bounds)` | No |
| 12 | `ClickableControlBase.Hover()` | `Input.MoveTo(bounds)` | No |
| 13 | `ClickableControlBase.LongPress()` | `Input.MoveTo() + Input.Click()` | No |
| 14 | `ComboBox.Open()` | `Input.Click(bounds)` | No |
| 15 | `ListBox.DoubleClickItem()` | `Input.DoubleClick(bounds)` | No |
| 16 | `Panel.ClickAt()` | `Input.Click(x, y)` | No |
| 17 | `Slider.SetValue()` | Physical click fallback | No (server works) |
| 18 | `PageObjectBase.ClickAt()` | `Input.Click(x, y)` | No |
| 19 | `PageObjectBase.TypeText()` | Delegates to context TypeText | No |
| 20 | `StrideTestFixtureBase.DisposeAsync()` | `ReleaseAllModifiers()` | Safety only |
| 21 | `StrideUITestBase` constructor | `ReleaseAllModifiers()` | Safety only |

**Key insight:** NONE of the remaining physical operations are actually called by any test. The 29 tests only use Click (server-side), SetText (server-side), PressKey (server-side primary), and HoldKey (server-side primary).

---

## Phase 1: Remove Physical Fallbacks

Remove the physical fallback code from methods that already have a working server-side primary path. If server-side fails, throw instead of falling back to unreliable physical input.

### 1.1 — `StrideTestContext.PressKey()` — Remove fallback

**File:** `srcnew/Brinell.Stride/Context/StrideTestContext.cs`

```csharp
// BEFORE:
public void PressKey(VirtualKey key)
{
    var strideKeyName = MapVirtualKeyToStrideKeyName(key);
    var response = SendCommand(AutomationCommand.Action("SimulateKeyPress", null, strideKeyName));
    if (response.Success)
        return;
    // Fallback to physical input
    EnsureGameHasKeyboardFocus();
    _inputSimulator.PressKey(key);
}

// AFTER:
public void PressKey(VirtualKey key)
{
    var strideKeyName = MapVirtualKeyToStrideKeyName(key);
    var response = SendCommand(AutomationCommand.Action("SimulateKeyPress", null, strideKeyName));
    if (!response.Success)
        throw new InvalidOperationException($"Server-side key press failed for '{key}': {response.Error}");
}
```

### 1.2 — `StrideTestContext.HoldKey()` — Remove fallback

Same pattern: throw on failure instead of falling back.

### 1.3 — `EditText.SelectAll()` — Remove fallback

```csharp
// AFTER:
public TScope SelectAll()
{
    var response = Context.SendCommand(
        AutomationCommand.Action("SelectAll", AutomationId));
    if (!response.Success)
        throw new InvalidOperationException($"Server-side SelectAll failed for '{AutomationId}': {response.Error}");
    return ContainingScope;
}
```

### 1.4 — `Slider.SetValue()` — Remove fallback

Throw on failure instead of falling back to physical click.

---

## Phase 2: Server-Side Clipboard & Undo/Redo

Replace physical HotKey-based clipboard operations with server-side commands. Stride's `EditText` has a `Text` property we can read/write, and the undo/redo concept can be handled by tracking previous text.

### 2.1 — Add server-side handlers in `StrideUIHandler`

**File:** `srcnew/Brinell.Automation/StrideUIHandler.cs`

Add to `HandleAction` switch:
- `"GetText"` → return `editText.Text` (already implicit in GetState, but useful standalone)
- `"Undo"` → not natively supported in Stride EditText — **throw NotSupported** for now
- `"Redo"` → same, throw NotSupported

For clipboard, the server-side game doesn't have clipboard access (it's an OS concept). Two options:
- **Option A:** Keep physical HotKey for clipboard but route the key presses through server-side `SimulateKeyPress` for Ctrl+C/V/X instead of physical SendInput
- **Option B:** Implement server-side: `Copy` reads `editText.Text` selection and the client puts it on clipboard via `Clipboard.SetText()`

**Decision: Option A** — Route Ctrl+C/V/X as server-side key combos. The game still processes them as normal input. This eliminates physical SendInput while keeping clipboard working.

Need new server-side command: `"SimulateKeyCombination"` that does HandleKeyDown(Ctrl) → HandleKeyDown(C) → HandleKeyUp(C) → HandleKeyUp(Ctrl) on the game thread.

### 2.2 — Add `SimulateKeyCombination` command to `AutomationGameSystem`

**File:** `srcnew/Brinell.Automation/AutomationGameSystem.cs`

Add handling for `"SimulateKeyCombination"` in `TryHandleKeySimulation()`:
- Args: `["LeftCtrl", "C"]` (array of Stride key names)
- Behavior: KeyDown all keys in order, schedule KeyUp all keys in reverse order after MinKeyPressDuration

### 2.3 — Rewrite `EditText` clipboard/undo/redo methods

**File:** `srcnew/Brinell.Stride/Controls/EditText.cs`

```csharp
// Copy — server-side Ctrl+C
public TScope Copy()
{
    var response = Context.SendCommand(
        AutomationCommand.Action("SimulateKeyCombination", null, "LeftCtrl", "C"));
    if (!response.Success)
        throw new InvalidOperationException($"Server-side Copy failed: {response.Error}");
    return ContainingScope;
}

// Same pattern for Cut (Ctrl+X), Paste (Ctrl+V), Undo (Ctrl+Z), Redo (Ctrl+Y)
```

### 2.4 — Remove `ReleaseAllModifiers()` calls from EditText

Since we're no longer doing physical hotkeys, no safety net needed.

---

## Phase 3: Server-Side TypeText

Replace physical `KEYEVENTF_UNICODE` character injection with server-side text input routed through Stride's input system.

### 3.1 — Add `SimulateTextInput` command to `AutomationGameSystem`

**File:** `srcnew/Brinell.Automation/AutomationGameSystem.cs`

Stride's `InputManager` has `TextInput` events. The `KeyboardDeviceBase` may support injecting text input directly. However, for `TypeText()` the simplest approach is:

- **Option A:** Inject each character as a text input event into Stride's input pipeline
- **Option B:** Just use `SetElementText` which already works — `TypeText` is only called from `PageObjectBase.TypeText()` which nobody uses

**Decision: Option B** — Remove `TypeText` from the context entirely. It has zero callers in tests. Callers who need to type text should use `SetElementText()` or the control's `Enter()`/`SetText()` methods.

### 3.2 — Remove `TypeText()` from `StrideTestContext`

Remove the method and its `EnsureGameHasFocus()` call.

### 3.3 — Remove `TypeText()` from `IStrideTestContext` interface

### 3.4 — Remove `TypeText()` helper from `PageObjectBase`

---

## Phase 4: Server-Side DoubleClick, RightClick, Hover

These are less critical (no test uses them) but should be server-side for consistency.

### 4.1 — For Click-variant actions, Stride's UI doesn't have native DoubleClick/RightClick routed events

Stride's `ButtonBase.ClickEvent` is the only routed event for clicks. There is no `DoubleClickEvent` or `RightClickEvent` in Stride UI.

**Two approaches:**
- **Raise Click twice** for DoubleClick (semantically equivalent for buttons)
- **For RightClick**: There is no standard handler in Stride UI. Most Stride games don't use right-click on UI elements.

### 4.2 — Rewrite `ClickableControlBase.DoubleClick()` as server-side

```csharp
public TScope DoubleClick(int? timeoutMs = null)
{
    AssertClickable(true, timeoutMs: timeoutMs);
    // Server-side: raise Click event twice
    var cmd = AutomationCommand.Action("Click", AutomationId);
    Context.SendCommand(cmd);
    Context.SendCommand(cmd);
    LogAction("DoubleClick");
    return ContainingScope;
}
```

### 4.3 — Rewrite `ClickableControlBase.RightClick()` — throw NotSupported

Stride UI has no right-click concept. Throw `NotSupportedException`.

### 4.4 — Rewrite `ClickableControlBase.Hover()` — throw NotSupported

Stride UI doesn't have CSS-like hover states in the automation model. No server-side equivalent.

### 4.5 — Rewrite `ClickableControlBase.LongPress()` — throw NotSupported

Same — no touch/long-press concept in Stride UI automation.

### 4.6 — Rewrite `ComboBox.Open()` as server-side Click

```csharp
public TScope Open()
{
    if (!IsOpen())
        Context.ClickElement(AutomationId);
    return ContainingScope;
}
```

### 4.7 — Rewrite `ListBox.DoubleClickItem()` as server-side

Route through `SelectByIndex` + server-side Click. Or raise click event twice.

### 4.8 — Rewrite or remove `Panel.ClickAt()`

Positional clicking has no server-side equivalent. Throw `NotSupportedException` — no test uses it.

### 4.9 — Remove `PageObjectBase.ClickAt()` helper

Same — throw NotSupported since no test calls it.

---

## Phase 5: Remove Focus Management & StrideInputSimulator

Once all input operations are server-side, no need for physical focus or input simulation.

### 5.1 — Remove `EnsureGameHasFocus()` from `StrideTestContext`

No callers remain after Phase 1-4 removes the methods that depended on it.

### 5.2 — Remove `EnsureGameHasKeyboardFocus()` from `StrideTestContext`

Same.

### 5.3 — Remove `ForceForegroundWindow()` and all its P/Invoke declarations

The entire P/Invoke block: `SetForegroundWindow`, `GetForegroundWindow`, `AttachThreadInput`, `GetWindowThreadProcessId`, `ShowWindow`, `BringWindowToTop`, `SetFocus`, `NativeSendInput`, and related structs/constants.

### 5.4 — Remove `EnsureGameHasFocus()` from `IStrideTestContext`

### 5.5 — Remove `ReleaseAllModifiers()` calls

Once no physical keys are pressed, there's nothing to release:
- `StrideUITestBase` constructor
- `StrideTestFixtureBase.DisposeAsync()`

### 5.6 — Evaluate `StrideInputSimulator` for deletion

If no code references it after all phases:
- Remove `StrideInputSimulator.cs`
- Remove `Input` property from `IStrideTestContext` and `StrideTestContext`
- Remove `_inputSimulator` field

**Keep it only if** there's a valid use case for physical input that we can't replace (e.g., `TakeScreenshot` already uses GDI+ not SendInput, so it's unrelated).

---

## Phase 6: Cleanup

### 6.1 — Remove `TypeText` from interface and all delegate methods
### 6.2 — Remove any `using` directives that become unused
### 6.3 — Remove P/Invoke for `GetWindowRect` from StrideTestContext only if no longer needed (it's still used by `GetWindowRectWithFallback` for window info — check if `GetWindowInfo` pipe command is sufficient)
### 6.4 — Run build + verify 29/29 tests pass
### 6.5 — Update PLAN-eliminate-physical-input.md to point to this plan as successor

---

## Implementation Order

```
Phase 1 (Remove fallbacks)          — Low risk, removes dead code paths
Phase 2 (Server-side key combos)    — New SimulateKeyCombination command
Phase 3 (Remove TypeText)           — Zero callers, pure deletion
Phase 4 (Server-side DoubleClick+)  — New server-side impls or NotSupported
Phase 5 (Remove focus + simulator)  — Large deletion of unused code
Phase 6 (Cleanup)                   — Compile, test, verify
```

## Success Criteria

- [ ] Zero references to `SendInput` remain in `srcnew/Brinell.Stride/`
- [ ] Zero references to `_inputSimulator` remain (or `StrideInputSimulator.cs` deleted)
- [ ] Zero `EnsureGameHasFocus`/`EnsureGameHasKeyboardFocus` calls remain
- [ ] Zero `ReleaseAllModifiers` calls remain
- [ ] `StrideInputSimulator.cs` deleted or emptied
- [ ] All focus P/Invoke (`SetForegroundWindow`, etc.) removed from `StrideTestContext`
- [ ] `IStrideTestContext.Input` removed
- [ ] `IStrideTestContext.TypeText` removed
- [ ] `IStrideTestContext.EnsureGameHasFocus` removed
- [ ] 29/29 tests pass
- [ ] Build: 0 errors, 0 warnings
