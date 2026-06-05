# Update Plan: Background-Safe FlaUI Input for Brinell MAUI

Date: 2026-06-03

Source RCA: `Brinell/.my/rca/2026-06-03-fla-ui-input-focus-rca.md`

## Objective

Make Brinell's Windows MAUI/FlaUI automation semantic-first and background-safe by default, so routine UI tests do not steal foreground focus, move the user's mouse, type into the active desktop, or mutate the user's clipboard.

Tests that truly require physical desktop input should still be possible, but only through an explicit interactive mode.

## Key Decision

Do not solve this by sending raw Win32 click messages as the primary strategy.

FlaUI can reach Win32 APIs such as `SendMessage`, and raw messages can sometimes work for HWND-backed controls. They are not a reliable general solution for MAUI/WinUI3 because many controls are not individual HWND-backed windows, and modern controls often depend on UI Automation providers, routed input, focus state, composition, or framework event plumbing.

The preferred interaction order is:

1. UI Automation patterns: `Invoke`, `SelectionItem`, `Toggle`, `Value`, `RangeValue`, `ExpandCollapse`, `Scroll`, `ScrollItem`, `LegacyIAccessible.DoDefaultAction`.
2. Brinell-controlled semantic fallbacks that do not use global input.
3. Physical pointer/keyboard input only when interactive mode is enabled.
4. Raw Win32 messages only as a narrow diagnostic or HWND-specific escape hatch, not as the MAUI adapter's default click model.

## Interaction Modes

Introduce a Windows interaction policy with two named modes.

### Semantic Mode

Default target mode for local developer runs.

- `AllowForegroundActivation = false`
- `AllowPointerInput = false`
- `AllowGlobalKeyboardInput = false`
- `AllowClipboardInput = false`
- UIA pattern actions are allowed.
- Unsupported actions fail with actionable errors.

### Interactive Mode

Compatibility mode for tests that intentionally use the active desktop.

- `AllowForegroundActivation = true`
- `AllowPointerInput = true`
- `AllowGlobalKeyboardInput = true`
- `AllowClipboardInput = true`
- Preserves current behavior as closely as possible.
- Recommended for dedicated VM, CI desktop session, Windows Sandbox, or separate RDP/user session.

### Environment Variables

Add:

- `BRINELL_WINDOWS_INTERACTION_MODE=semantic|interactive`
- `BRINELL_ALLOW_FOREGROUND_ACTIVATION=true|false`
- `BRINELL_ALLOW_POINTER_INPUT=true|false`
- `BRINELL_ALLOW_GLOBAL_KEYBOARD_INPUT=true|false`
- `BRINELL_ALLOW_CLIPBOARD_INPUT=true|false`

Resolution rule:

1. Start from named mode defaults.
2. Apply granular overrides.
3. If no mode is specified, default to semantic mode for Brinell's Windows MAUI driver.

## Phase 1: Options and Policy Plumbing

Add `WindowsInteractionOptions` in the Brinell MAUI layer so it can be passed through `MauiDriverOptions` without making tests reference the FlaUI implementation directly.

Target files:

- `Brinell/srcnew/Brinell.Maui/MauiDriverOptions.cs`
- `Brinell/srcnew/Brinell.Maui/MauiDriverFactory.cs`
- `Brinell/srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs`

Tasks:

- Add `WindowsInteractionOptions` with the four allow flags and a `FromEnvironment()` parser.
- Add `WindowsInteractionOptions WindowsInteraction { get; set; }` to `MauiDriverOptions`.
- Extend `MauiDriverOptions.FromEnvironment()` to parse the new environment variables.
- Add FlaUI driver constructors that accept `WindowsInteractionOptions`.
- Keep existing constructors as compatibility overloads that delegate to semantic defaults.
- Update `MauiDriverFactory.CreateFlaUIDriver()` to pass `options.WindowsInteraction` through reflection.
- Add unit coverage for env parsing, named modes, granular overrides, and invalid values.

Exit criteria:

- A test can create a Windows MAUI driver in semantic mode without setting any legacy input env vars.
- Existing callers using old constructors still compile and run.

## Phase 2: Centralize Physical Input

Move every foreground, mouse, keyboard, and clipboard operation behind driver-owned helpers.

Target files:

- `Brinell/srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs`
- `Brinell/srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs`

Add helpers:

- `EnsureForegroundForPhysicalInput()`
- `RequirePointerInput(string action)`
- `RequireGlobalKeyboardInput(string action)`
- `RequireClipboardInput(string action)`
- `PointerClick(...)`
- `PointerHover(...)`
- `PointerLongPress(...)`
- `PointerSwipe(...)`
- `GlobalType(...)`
- `GlobalTypeSimultaneously(...)`
- `SetClipboardTextForInput(...)`

Rules:

- Helpers throw clear `InvalidOperationException` messages when policy blocks an action.
- Physical input helpers may bring the AUT to the foreground only if `AllowForegroundActivation` is enabled.
- UIA pattern helpers must not call foreground activation.
- Avoid catching and swallowing policy exceptions in ways that hide the real reason an action failed.

Example error:

```text
This action requires global keyboard input, but BRINELL_ALLOW_GLOBAL_KEYBOARD_INPUT is not enabled. Prefer SetText()/ValuePattern or run with BRINELL_WINDOWS_INTERACTION_MODE=interactive.
```

Exit criteria:

- No direct `Mouse.*`, `Keyboard.*`, clipboard, or `SetForeground()` calls remain in `FlaUIMauiElement`.
- `FlaUIMauiDriver` is the only place where physical desktop input is performed.

## Phase 3: Make Semantic Actions Foreground-Free

Remove unconditional foreground activation from UIA pattern operations.

Target methods:

- `FlaUIMauiElement.Click()`
- `InvokePattern()`
- `SelectItemPattern()`
- `DoDefaultActionPattern()`
- `TogglePattern()`
- `SetRangeValue()`
- `Expand()`
- `Collapse()`
- `SelectItemByText()`
- `SelectItemByIndex()`
- `ScrollIntoView()`
- `FlaUIMauiDriver.GetScreenshot()`
- `FlaUIMauiDriver.NavigateBack()` pattern path

Tasks:

- Let `Click()` try UIA patterns without foregrounding.
- Keep pointer fallback only through the guarded driver helper.
- Change screenshot capture to avoid foregrounding.
- Keep `NavigateBack()` semantic-first by invoking a discovered back button when possible.
- Gate the `Alt+Left` fallback behind global keyboard and foreground policy.

Exit criteria:

- A successful `InvokePattern`, `SelectionItemPattern`, `TogglePattern`, `ValuePattern.SetValue`, or screenshot capture does not call `SetForeground`, `Focus`, `Mouse`, or `Keyboard`.

## Phase 4: Fix Text Entry Semantics

Make text APIs use `ValuePattern.SetValue` where possible and reserve global keystrokes for interactive mode.

Target files:

- `Brinell/srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/ControlBase.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/Text/Entry.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/Text/Editor.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/Generated/EditableField.cs`

Tasks:

- For `TextInputMethod.SetValue`, try value-pattern targets only; do not silently fall back to global keyboard in semantic mode.
- For `Clear()`, use `ValuePattern.SetValue(string.Empty)` or nested text value targets first.
- Gate Ctrl+A/Delete, End/Backspace, paste, and ordinary key typing behind policy.
- Remove `ControlBase.SendKeysCore()`'s unconditional `element.Click()` focus step, or gate it behind interactive input.
- Make `Entry.SetText()` and `Editor.SetText()` the preferred semantic APIs in docs and tests.
- Decide whether `Entry.Enter()` should become semantic-first on Windows or remain a typing API that requires interactive mode when value patterns are not available.
- Treat clipboard paste as opt-in because it mutates the developer clipboard.

Exit criteria:

- `SetText()` and successful `TextInputMethod.SetValue` do not foreground the AUT or use physical keyboard input.
- Keyboard-style methods fail clearly in semantic mode when no UIA value pattern is available.

## Phase 5: Remove Unsafe Control-Layer Fallbacks

Make higher-level controls respect the same semantic/interactive split.

Target files:

- `Brinell/srcnew/Brinell.Maui/Controls/ElementActivator.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/Buttons/Button.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/ClickableControlBase.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/FocusableControlBase.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/Range/Slider.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/DateTime/DatePicker.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/DateTime/TimePicker.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/List.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/ScrollableControlBase.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/SwipeableControlBase.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/RefreshableControlBase.cs`
- `Brinell/srcnew/Brinell.Maui/Controls/Collection/CarouselView.cs`

Tasks:

- Remove the pointer-disabled to keyboard-activation fallback in `ElementActivator`; it replaces one unsafe global input path with another.
- Keep `Button.Click()` semantic-first through selection/invoke/legacy patterns.
- Treat `Button.Press()` as explicitly keyboard/interactive.
- Gate `Focus()`, `Blur()`, `Hover()`, `LongPress()`, `DoubleClick()`, `RightClick()`, and `Swipe()` in semantic mode unless a UIA pattern can satisfy them.
- Prefer `RangeValuePattern` for sliders before keyboard fallback.
- Prefer picker/list selection patterns before pointer or keyboard fallback.
- Make physical gesture failures honest: if the app surface is tap-only and does not expose an invokable automation peer, semantic mode should say so.

Exit criteria:

- High-level Brinell controls cannot bypass the driver policy by indirectly calling global input.
- Semantic mode failures point to the control capability gap rather than silently using the user's desktop.

## Phase 6: App Surface Hardening

Update BodyCam and Brinell sample MAUI surfaces where tests need background-safe activation but the UI is implemented as a gesture-only visual container.

Targets:

- BodyCam settings cards and page-object targets.
- Brinell MAUI sample controls used by Windows UI tests.

Tasks:

- Prefer native `Button`, `MenuItem`, `ToolbarItem`, `CheckBox`, `Switch`, `Entry`, and picker controls for test-critical interactions.
- If a visual card needs to look custom, include an accessible native button or expose an automation peer that supports invoke.
- Retarget page objects to invokable child controls when a visual wrapper does not expose useful UIA patterns.
- Keep the `AdvancedSettingsCard` shared-fixture navigation failure separate; it is not part of the focus/input policy fix.

Exit criteria:

- Main BodyCam flows can run in semantic mode without pointer or keyboard input for navigation and settings edits that have semantic equivalents.

## Phase 7: Test Strategy

Add tests at three levels.

### Unit Tests

Targets:

- Env parsing and option defaults.
- Policy guard behavior.
- `ElementActivator` semantic-only activation order.
- Text entry fallback behavior.
- Slider/date/time/list control fallback behavior where unit-testable.

Assertions:

- Semantic mode blocks pointer input.
- Semantic mode blocks global keyboard input.
- Semantic mode blocks clipboard input.
- Interactive mode allows guarded physical paths.
- UIA pattern success does not request foreground activation.

### FlaUI Adapter Tests

If a dedicated FlaUI test project does not exist, add a focused one or place tests near existing Windows adapter coverage.

Assertions:

- `Click()` invokes supported UIA patterns before pointer fallback.
- `SendKeys(..., SetValue)` does not type globally when value pattern succeeds.
- `SendKeys(..., Keys)` throws policy error in semantic mode.
- `Clear()` uses value pattern before keyboard fallback.
- Dropdown item selection does not call raw FlaUI `.Click()` outside the Brinell policy.

### Windows UI Smoke Tests

Run a small MAUI sample/BodyCam slice with:

```powershell
$env:BRINELL_WINDOWS_INTERACTION_MODE = "semantic"
$env:BRINELL_ALLOW_POINTER_INPUT = "false"
$env:BRINELL_ALLOW_GLOBAL_KEYBOARD_INPUT = "false"
$env:BRINELL_ALLOW_FOREGROUND_ACTIVATION = "false"
$env:BRINELL_ALLOW_CLIPBOARD_INPUT = "false"
```

Suggested smoke flows:

- App launches or attaches.
- Main page existence assertions pass.
- Invokable navigation button opens a page.
- `SetText()` updates an entry/editor.
- Toggle/switch path works through `TogglePattern` when available.
- Screenshot capture works without foregrounding.

Exit criteria:

- Semantic smoke tests pass without moving the mouse.
- Any remaining failures identify missing UIA patterns or app accessibility gaps.

## Phase 8: Documentation and Rollout

Update Brinell docs to make interaction types explicit.

Target docs:

- `Brinell/docs/guides/best-practices.md`
- `Brinell/docs/guides/troubleshooting.md`
- `Brinell/docs/guides/interface-usage-guide.md`
- `Brinell/docs/run/WPF.md`
- `Brinell/docs/run/WinForms.md`
- `Brinell/docs/run/WINDOWS-TEST-RESULTS.md`

Tasks:

- Document semantic mode and interactive mode.
- Add a "background-safe Windows tests" guide.
- Recommend `SetText()` over `Enter()` for non-keystroke text setting.
- Explain that `Press()`, `Submit()` via Enter, swipe, hover, long-press, double-click, right-click, and keyboard shortcuts are interactive unless backed by a UIA pattern.
- Explain why raw Win32 `SendMessage` is not the default click strategy for MAUI/WinUI.
- Add troubleshooting examples for policy failures.

Exit criteria:

- A developer can tell from docs whether a Brinell action is background-safe.
- Interactive-mode opt-in is clear and easy to set for VM/CI runs.

## Rollout Sequence

1. Add options and policy plumbing while preserving old constructors.
2. Centralize physical input and route all FlaUI element paths through guards.
3. Remove foregrounding from semantic UIA pattern paths.
4. Update text entry paths to prefer `ValuePattern.SetValue`.
5. Update high-level controls so they cannot bypass policy.
6. Add tests for semantic mode and interactive mode.
7. Retarget BodyCam/Brinell sample page objects to semantic-safe controls.
8. Update docs and migration guidance.
9. Run semantic smoke suite locally.
10. Run interactive compatibility suite in an isolated desktop/session.

## Acceptance Criteria

- `Button.Click()` on a UIA-invokable control does not foreground the app.
- `SetText()` on a UIA value-capable text control does not use global keyboard input.
- Screenshot capture does not bring the app to the front.
- No physical mouse/keyboard/clipboard path is available in semantic mode without a policy error.
- Interactive mode keeps old-style behavior available for tests that truly need desktop input.
- Raw Win32 message sending is not used as the default MAUI click implementation.
- BodyCam's main UI and settings smoke tests can run in semantic mode for flows with semantic app surfaces.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Some MAUI/WinUI controls report UIA pattern success but do not execute app commands. | Retarget to native invokable child controls, improve app accessibility peers, or mark the action interactive. |
| Existing tests rely on `Enter()` as physical typing. | Keep interactive mode and document `SetText()` as the semantic replacement. |
| `ElementActivator` currently hides fallback failures. | Stop swallowing policy exceptions when they are the root cause; preserve actionable messages. |
| Some controls have no semantic automation surface. | Make failures explicit and fix the app control surface instead of silently moving the user's desktop. |
| Changing defaults may surprise CI. | Roll out with explicit env vars in CI scripts and document interactive compatibility mode. |

## Open Questions

- Should packaged Brinell default to semantic mode immediately, or should only this repo's test fixtures opt into semantic mode first?
- Should `Entry.Enter()` remain "simulate typing" semantically, or become "set text" on Windows when a value pattern is available?
- Should `Button.Press()` be renamed or documented as keyboard-specific to avoid confusion with semantic `Click()`?
- Is a dedicated `Brinell.Maui.FlaUI.Tests` project worth adding for adapter-level unit tests?
- Which BodyCam visual card controls should be converted to native invokable controls first?
