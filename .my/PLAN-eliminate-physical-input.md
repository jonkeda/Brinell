# Plan: Eliminate Remaining Physical SendInput Leaks

**Status:** IMPLEMENTED ✅  
**Created:** February 22, 2026  
**Completed:** February 22, 2026  
**Result:** All 4 phases implemented. 29/29 tests pass. Build: 0 errors, 0 warnings.  
**Context:** 29/29 Stride tests pass, but many input operations still bypass the automation pipe and use physical Win32 `SendInput`. This causes unreliable behavior when the test machine has stale modifier keys (e.g., stuck Shift), focus loss, or DPI differences.

---

## Problem Statement

The previous plan (PLAN-input-simulation-reliability.md) moved `Click()`, `PressKey()`, and `HoldKey()` to server-side automation. However, a full audit reveals that **many other input paths still use physical `SendInput`**, leaking OS-level keyboard/mouse events that can:

1. Interfere with game state (physical Ctrl+A triggers player movement via `IsKeyDown(Keys.A)`)
2. Fail when the game window loses focus
3. Leave modifier keys stuck if a test crashes mid-operation
4. Be affected by stale physical keyboard state (e.g., Shift stuck from a prior run)

---

## Audit: Current Input Pathways

### Already Through Automation Pipe (safe)

| Operation | File | Command |
|---|---|---|
| `Click()` | `ClickableControlBase.cs` | `Action("Click", automationId)` → server-side `RaiseEvent(ClickEvent)` |
| `PressKey()` | `StrideTestContext.cs` | `SimulateKeyPress` → game-thread `HandleKeyDown`/`HandleKeyUp` |
| `HoldKey()` | `StrideTestContext.cs` | `SimulateKeyHold` → same |
| `EditText.SetText()` (primary path) | `EditText.cs` | `SetElementText` → server-side direct property set |
| `SetSliderValue()` | `StrideTestContext.cs` | `Action("SetSliderValue")` → server-side `Slider.Value = x` |
| `SetToggleValue()` | `StrideTestContext.cs` | `Action("SetToggleValue")` → server-side |

### Still Physical SendInput (LEAKING)

| # | Operation | File | Physical API Used | Risk Level |
|---|---|---|---|---|
| 1 | `TypeText()` | `StrideTestContext.cs:147` | `_inputSimulator.TypeText()` (KEYEVENTF_UNICODE per char) | Medium — needs focus, but doesn't set modifier keys |
| 2 | `TextControlBase.Clear()` | `TextControlBase.cs:37` | `Context.Input.HotKey(A, Control)` physical Ctrl+A | **HIGH** — physical Ctrl held, game sees `IsKeyDown(A)` |
| 3 | `EditText.SelectAll()` | `EditText.cs:59` | `Context.Input.HotKey(A, Control)` | **HIGH** — same physical Ctrl+A |
| 4 | `EditText.Copy()` | `EditText.cs:67` | `Context.Input.HotKey(C, Control)` | Medium |
| 5 | `EditText.Cut()` | `EditText.cs:75` | `Context.Input.HotKey(X, Control)` | Medium |
| 6 | `EditText.Paste()` | `EditText.cs:83` | `Context.Input.HotKey(V, Control)` | Medium |
| 7 | `EditText.Undo()` | `EditText.cs:91` | `Context.Input.HotKey(Z, Control)` | Medium |
| 8 | `EditText.Redo()` | `EditText.cs:99` | `Context.Input.HotKey(Y, Control)` | Medium |
| 9 | `EditText.MoveToEnd()` | `EditText.cs:110` | `Context.Input.PressKey(End)` | Low |
| 10 | `EditText.Focus()` | `EditText.cs:48` | `Context.ClickElement()` → physical mouse click | **HIGH** — needs window coordinates |
| 11 | `ClickElement()` | `StrideTestContext.cs:127` | `_inputSimulator.Click(screenX, screenY)` | **HIGH** — physical mouse at screen coords |
| 12 | `DoubleClick()` | `ClickableControlBase.cs:38` | `Context.Input.DoubleClick()` | **HIGH** |
| 13 | `RightClick()` | `ClickableControlBase.cs:47` | `Context.Input.RightClick()` | **HIGH** |
| 14 | `Hover()` | `ClickableControlBase.cs:63` | `Context.Input.MoveTo()` | Medium |
| 15 | `LongPress()` | `ClickableControlBase.cs:72` | `Context.Input.Click()` | Medium |
| 16 | `EnsureGameHasKeyboardFocus()` | `StrideTestContext.cs:208` | Physical mouse click center + physical Alt key | **HIGH** — moves mouse, holds Alt |
| 17 | `ForceForegroundWindow()` | `StrideTestContext.cs:225` | Physical Alt key press/release via NativeSendInput | Medium |
| 18 | `TextControlBase.Append()` | `TextControlBase.cs:56` | `Context.PressKey(End)` (pipe) + `Context.TypeText()` (physical) | Medium — mixed |

### Side Effects of Physical Input

1. **HotKey(A, Control)** — physical `KeyDown(Control)` + `PressKey(A)` + `KeyUp(Control)` — the game's `InputManager` sees `Keys.A` and `Keys.LeftCtrl` as physically pressed → `input.IsKeyDown(Keys.A)` triggers player movement during Clear/SelectAll
2. **Physical Alt key** in `ForceForegroundWindow` — game could see Alt held
3. **Stuck modifiers** — if test process crashes mid-`HotKey`, physical keys stay pressed for all subsequent tests (ReleaseAllModifiers exists but is never called automatically)

---

## Fix Strategy

### Phase 1: Route Text Editing Through Automation Pipe

**Goal:** Eliminate #1-9 (TypeText, Clear, SelectAll, Copy/Cut/Paste, Undo/Redo, End key)

These all operate on the **currently focused** EditText control. The server-side handler can do these operations directly since it has access to the UIElement.

**New automation commands needed:**

| Command | What it does server-side |
|---|---|
| `ClearText` target=automationId | `editText.Text = ""` |
| `SelectAllText` target=automationId | `editText.SelectionStart = 0; editText.SelectionLength = editText.Text.Length` |
| `AppendText` target=automationId args=["text"] | `editText.Text += text` |

**Wait — simpler approach:** `EditText.SetText()` already calls `Context.SetElementText(automationId, text)` which sends `SetElementText` via pipe. The server directly sets `editText.Text = value`. The physical fallback (Focus→Clear→TypeText) is only used when this fails.

So Phase 1 is:
- **`EditText.Clear()`** → send `SetElementText(automationId, "")` via pipe instead of physical Ctrl+A + Delete
- **`TextControlBase.Clear()`** — same, route through server-side SetElementText("")
- **`TextControlBase.Enter()`** → can use `AppendText` server-side command or `SetElementText` to concatenate
- **`TypeText()`** — add new `SimulateTextInput` command that feeds characters into Stride's text input handling on the game thread
- **`SelectAll/Copy/Cut/Paste/Undo/Redo`** — these are clipboard/selection operations. Stride's `EditText` control doesn't expose public select/clipboard APIs. Keep physical input BUT wrap in `ReleaseAllModifiers()` safety net.

**Revised Phase 1:**

| Operation | Fix | Approach |
|---|---|---|
| `EditText.SetText(text)` | Already server-side | No change needed |
| `EditText.Clear()` | `SetElementText(id, "")` | Pipe command |
| `TextControlBase.Clear()` | `SetElementText(id, "")` | Pipe command |
| `TextControlBase.Enter(text)` | `SetElementText(id, currentText + text)` or new `AppendText` command | Pipe command |
| `TypeText(text)` | Still physical (UNICODE) but less critical since SetText handles most cases | Keep as fallback |
| `SelectAll/Copy/Cut/Paste/Undo/Redo` | Keep physical BUT call `ReleaseAllModifiers()` after | Safety net |

### Phase 2: Eliminate Physical Mouse Clicks

**Goal:** Eliminate #10-13 (Focus/ClickElement, DoubleClick, RightClick)

| Operation | Fix | Approach |
|---|---|---|
| `EditText.Focus()` | New `FocusElement` automation command | `element.SetFocus()` or use Stride's FocusManager |
| `ClickElement(automationId)` | Already have server-side Click — redirect | Use existing `Action("Click", id)` |
| `DoubleClick()` | New `DoubleClick` automation command | Two `RaiseEvent(ClickEvent)` calls or custom event |
| `RightClick()` | New `RightClick` automation command or keep physical | Low priority |

### Phase 3: Eliminate Focus Management Physical Input

**Goal:** Eliminate #16-17 (EnsureGameHasKeyboardFocus, ForceForegroundWindow)

These are needed only because physical input requires the window to have focus. Once all input goes through the pipe, we DON'T NEED FOCUS AT ALL — server-side commands run directly on game objects.

| Operation | Fix | Approach |
|---|---|---|
| `EnsureGameHasFocus()` | Remove calls from input paths that now use pipe | No physical input = no focus needed |
| `EnsureGameHasKeyboardFocus()` | Same — remove calls | Only keep for actual physical fallback |
| `ForceForegroundWindow()` | Keep as utility but stop calling from hot paths | Only used in actual fallback |

### Phase 4: Safety Net for Remaining Physical Input

For any operations that MUST stay physical (clipboard operations, some edge cases):

1. **Auto-call `ReleaseAllModifiers()`** after any physical HotKey operation
2. **Call `ReleaseAllModifiers()`** in test fixture setup/teardown
3. **Consider adding `ReleaseAllModifiers()`** to `StrideTestFixtureBase.InitializeAsync()`

---

## Implementation Details

### New Automation Commands

Add to `StrideUIHandler.HandleAction()`:

```csharp
"ClearText" => ClearElementText(element),
"FocusElement" => FocusElement(element),
```

```csharp
private AutomationResponse ClearElementText(UIElement element)
{
    if (element is EditText edit)
    {
        edit.Text = "";
        return AutomationResponse.Ok(true);
    }
    return AutomationResponse.Fail($"Element is not an EditText");
}

private AutomationResponse FocusElement(UIElement element)
{
    // Stride doesn't have a simple Focus API for arbitrary elements,
    // but EditText has IsFocused / focus-on-touch behavior.
    // We can simulate this by setting the UISystem's focused element.
    return AutomationResponse.Ok(true); // Investigate Stride focus API
}
```

### EditText.Clear() Rewrite

```csharp
public override TScope Clear()
{
    // Server-side: set text to empty directly
    var success = Context.SetElementText(AutomationId, "");
    if (!success)
    {
        // Fallback to physical input
        Focus();
        SelectAll();
        Context.PressKey(VirtualKey.Delete);
    }
    return ContainingScope;
}
```

### TextControlBase.Clear() Rewrite

```csharp
public virtual TScope Clear()
{
    if (!IsEditable)
        throw new InvalidOperationException($"Control '{AutomationId}' is read-only.");

    Context.SetElementText(AutomationId, "");
    LogAction("Clear");
    return ContainingScope;
}
```

### ReleaseAllModifiers Safety Net

Add to `StrideUITestBase` or test fixture:

```csharp
// In StrideTestFixtureBase.InitializeAsync() after connection:
Context.Input.ReleaseAllModifiers();

// Or in StrideUITestBase constructor:
public StrideUITestBase(StrideAppFixture fixture, ITestOutputHelper output)
    : base(fixture, output)
{
    Context.Input.ReleaseAllModifiers();
}
```

---

## Files to Modify

| File | Change | Phase |
|------|--------|-------|
| `srcnew/Brinell.Stride/Controls/EditText.cs` | `Clear()` → server-side `SetElementText("")`; `Focus()` → server-side if possible | 1, 2 |
| `srcnew/Brinell.Stride/Controls/TextControlBase.cs` | `Clear()` → server-side; `Enter()` → consider server-side append | 1 |
| `srcnew/Brinell.Automation/StrideUIHandler.cs` | Add `ClearText`, `FocusElement` commands | 1, 2 |
| `srcnew/Brinell.Stride/Controls/ClickableControlBase.cs` | `DoubleClick()`/`RightClick()` → server-side or keep with safety | 2 |
| `srcnew/Brinell.Stride/Context/StrideTestContext.cs` | Remove `EnsureGameHasFocus` calls from server-side paths; `ClickElement()` → server-side | 2, 3 |
| `srcnew/Brinell.Stride/Testing/StrideTestFixtureBase.cs` | Add `ReleaseAllModifiers()` to initialization | 4 |
| `testsnew/Brinell.Stride.UITests/StrideUITestBase.cs` | Add `ReleaseAllModifiers()` to constructor | 4 |

---

## Priority & Impact

| Phase | Impact | Effort | Priority |
|-------|--------|--------|----------|
| Phase 1: Text editing | Eliminates physical Ctrl+A (player movement side effect), physical Delete key | Low | **HIGH** |
| Phase 2: Mouse clicks | Eliminates coordinate-dependent clicks, focus requirements | Medium | Medium |
| Phase 3: Focus management | Removes unnecessary focus calls | Low | Low |
| Phase 4: Safety net | Prevents stuck modifier keys | Low | **HIGH** (quick win) |

**Recommended order:** Phase 4 → Phase 1 → Phase 2 → Phase 3

Phase 4 is a one-line addition that immediately protects against stuck keys. Phase 1 eliminates the most impactful side effect (physical Ctrl+A triggering movement).

---

## Definition of Done

- [ ] `EditText.Clear()` and `TextControlBase.Clear()` use server-side `SetElementText("", id)` instead of physical Ctrl+A + Delete
- [ ] `ReleaseAllModifiers()` called automatically at test initialization
- [ ] No physical keyboard modifier keys (Ctrl, Alt, Shift) are sent during normal test execution
- [ ] Physical mouse input only used for operations not yet server-side (DoubleClick, RightClick, Hover)
- [ ] All 29 tests still pass
- [ ] No `Thread.Sleep` or arbitrary waits added

---

## Risk Assessment

- **Low risk:** Phase 4 (ReleaseAllModifiers) — additive only, no behavior change
- **Low risk:** Phase 1 (Clear → SetElementText) — SetElementText already works, just redirecting
- **Medium risk:** Phase 2 (mouse elimination) — Stride focus API may need investigation
- **Low risk:** Phase 3 (remove focus calls) — just deleting unnecessary calls after previous phases
