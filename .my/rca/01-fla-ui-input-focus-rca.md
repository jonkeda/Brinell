# RCA: FlaUI Tests Steal Focus, Move Mouse, and Bring App to Front

Date: 2026-06-03

## Current Status Log

- BodyCam app startup crash has been fixed locally. The root cause was Windows `SecureStorage` throwing during DI-time API key lookup, combined with eager realtime client construction.
- Focused app startup/UI checks passed after that fix:
  - `SettingsButton_Exists`: 1 passed.
  - `MainPage` UI slice: 34 passed.
  - `Navigation` UI slice: 3 passed.
- Settings UI tests were retargeted locally from the old `ConnectionSettingsPage` flow to the current LLM Providers flow.
- Retargeted settings classes that passed:
  - `ApiKeyTests`: 6 passed.
  - `ProviderTests`: 4 passed.
  - `AzureSettingsTests`: 5 passed.
  - `ModelSelectionTests`: 5 passed.
- Last settings hub run before stop:
  - `SettingsHubTests`: 8 passed, 1 failed.
  - Failing test: `AdvancedSettingsCard_Click_OpensAdvancedPage`.
  - Failure was `ElementNotFoundException` for `AdvancedSettingsCard` after navigating around the shared fixture. This is separate from the focus/input RCA.
- Test run was stopped at user request.
- No BodyCam test/app processes were running when this RCA investigation started.
- Local worktree is dirty. Relevant local edits include BodyCam startup fixes, UI test page-object/test updates, and this RCA.

## User Problem

The current Windows UI test behavior is disruptive:

1. Test clicks and text entry should not use the real system mouse/keyboard when avoidable.
2. The developer should be able to keep working while tests run.
3. The app/test windows should not repeatedly jump to the front.

## Findings

Brinell's Windows MAUI driver is implemented on FlaUI/UIA, but it still uses foregrounding and global input fallbacks in core paths.

### Foregrounding Is Unconditional in Many Paths

`Brinell/srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs`

- `EnsureRootWindowFocused()` is defined at line 133.
- It calls `_rootElement.SetForeground()` at line 153.
- It falls back to `_rootElement.Focus()` at line 160.
- `GetScreenshot()` calls `EnsureRootWindowFocused()` at line 422.
- `NavigateBack()` calls `EnsureRootWindowFocused()` at line 520.

`Brinell/srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs`

- `Click()` calls `_driver.EnsureRootWindowFocused()` at line 169.
- UIA pattern wrappers also foreground:
  - `TogglePattern()` at line 571.
  - `InvokePattern()` at line 622.
  - `DoDefaultActionPattern()` at line 660.
  - `SelectItemPattern()` at line 698.

This explains the repeated app-to-front behavior even when the action could be handled by a UI Automation pattern.

### Real Mouse Input Still Exists

`FlaUIMauiElement.ClickWithPointerFallback()` has a guard:

- Pointer fallback checks `PointerGesturesEnabled()` at line 194.
- It moves and clicks the system mouse at lines 202-205.
- `PointerGesturesEnabled()` reads `BRINELL_ALLOW_POINTER_INPUT` at line 518.

That guard is helpful, but it is incomplete. Other paths still use physical pointer APIs:

- `Hover()` moves the mouse at line 261.
- `LongPress()` uses `Mouse.Position`, `Mouse.Down`, and `Mouse.Up` at lines 269-272.
- `Swipe()` uses mouse wheel/drag at lines 330-356.
- Picker/list fallback paths call FlaUI element `.Click()` directly at lines 944 and 989.

### Real Keyboard Input Still Exists

`FlaUIMauiElement.SendKeys()` uses global keyboard input:

- `Keyboard.Type(text)` at line 215.
- Clipboard paste plus `Ctrl+V` at line 220.
- `Keyboard.Type(text)` fallback for `TextInputMethod.SetValue` at line 227.

Other keyboard paths:

- `Clear()` uses `Ctrl+A` and `Delete` at lines 240-241.
- `Submit()` sends `Enter` at line 506.
- `ClearWithFallback()` uses `Ctrl+A`, `Delete`, `End`, and `Backspace` at lines 1098-1115.
- `NavigateBack()` falls back to `Alt+Left` in the driver at line 527.
- `Refresh()` sends `F5` at line 542.

This explains why tests can interfere with active typing or fail with `Access is denied` from `SendInput` on locked-down desktops.

### Higher-Level Controls Can Hide Physical Input

`Brinell/srcnew/Brinell.Maui/Controls/ControlBase.cs`

- `SendKeysCore()` clicks the element first at line 435, then sends keys.

`Brinell/srcnew/Brinell.Maui/Controls/FocusableControlBase.cs`

- `FocusCore()` calls `element.Click()` at line 62.

`Brinell/srcnew/Brinell.Maui/Controls/ElementActivator.cs`

- `TryActivate()` is pattern-first, then calls `element.Click()` at line 37.
- If pointer input is disabled, it falls back to keyboard activation at lines 42-43.
- Keyboard activation calls `element.SendKeys(key)` at line 56.

So a test author may write `Button.Click()` or `Entry.Enter()` and still indirectly trigger foregrounding or global input.

### Attach Mode Exists but Does Not Solve Isolation

Brinell can attach by process or window handle:

- `APPIUM_ATTACH_TO_RUNNING`, `APPIUM_PROCESS_NAME`, and `APPIUM_WINDOW_HANDLE` are read in `MauiTestFixtureBase.cs` lines 91-93.
- `MauiDriverFactory` creates a FlaUI driver from window handle, process, or app path at lines 56-77.

This helps launch/attach control, but it does not make raw keyboard or mouse input safe in the background. Windows `SendInput` targets the active desktop/window focus. If tests use real keyboard/mouse, the developer cannot reliably keep working in the same desktop session.

## Root Cause

The Windows driver currently mixes two interaction models:

- Semantic UI Automation operations: `InvokePattern`, `SelectionItemPattern`, `ValuePattern.SetValue`, `ScrollPattern`, etc.
- Physical desktop input fallbacks: foreground window activation, `Mouse.*`, and `Keyboard.*`.

The physical model is available from core paths and is sometimes used implicitly. Because `EnsureRootWindowFocused()` is called before semantic operations too, even successful UIA interactions still pull the app forward.

## Proposed Fix

### 1. Add an Explicit Windows Interaction Policy

Introduce a small options object, for example:

```csharp
public sealed class WindowsInteractionOptions
{
    public bool AllowForegroundActivation { get; init; }
    public bool AllowPointerInput { get; init; }
    public bool AllowGlobalKeyboardInput { get; init; }
    public bool AllowClipboardInput { get; init; }
}
```

Read it from environment variables and expose it through `MauiDriverOptions`:

- `BRINELL_ALLOW_FOREGROUND_ACTIVATION`
- `BRINELL_ALLOW_POINTER_INPUT`
- `BRINELL_ALLOW_GLOBAL_KEYBOARD_INPUT`
- `BRINELL_ALLOW_CLIPBOARD_INPUT`

Recommended defaults for local developer runs:

- Foreground activation: disabled.
- Pointer input: disabled.
- Global keyboard input: disabled.
- Clipboard input: disabled.

Recommended compatibility mode:

- `BRINELL_WINDOWS_INTERACTION_MODE=interactive`
- Enables old behavior for tests that truly require desktop input.

### 2. Make UIA/Semantic Actions Foreground-Free

Change these operations so they do not call `EnsureRootWindowFocused()`:

- `InvokePattern()`
- `SelectItemPattern()`
- `DoDefaultActionPattern()`
- `TogglePattern()`
- `ValuePattern.SetValue`
- `ScrollItemPattern.ScrollIntoView`
- `ScrollPattern.Scroll`
- `Capture.Element` screenshots

Only call foreground activation when the driver is about to use global keyboard or pointer input and `AllowForegroundActivation` is enabled.

### 3. Centralize Physical Input Guards

Replace scattered calls to `Mouse.*`, `Keyboard.*`, and `SetForeground()` with driver helper methods:

- `TryPointerClick(...)`
- `TryPointerHover(...)`
- `TryGlobalKeyInput(...)`
- `TryBringToForeground(...)`

Each helper should enforce the interaction policy and fail with an actionable message when disabled.

Example failure:

> This action requires global keyboard input, but `BRINELL_ALLOW_GLOBAL_KEYBOARD_INPUT` is not enabled. Prefer `SetText()`/`ValuePattern` or run with `BRINELL_WINDOWS_INTERACTION_MODE=interactive`.

### 4. Prefer Semantic Text Entry

For text controls:

- Keep `Entry.SetText()` pattern-first through `ValuePattern.SetValue`.
- Make `Entry.Enter()` either:
  - use `ValuePattern.SetValue` by default on Windows, or
  - require explicit interactive mode when it must simulate keystrokes.
- Make `Clear()` use `ValuePattern.SetValue(string.Empty)` only unless interactive mode is enabled.
- Make clipboard paste opt-in because it mutates the developer clipboard.

BodyCam test hygiene already moved the flaky USB and Azure entry tests toward `SetText()`. That should be the preferred pattern in Brinell docs.

### 5. Restrict Physical Gestures to Interactive Mode

For `Hover`, `LongPress`, `Swipe`, double-click, right-click, picker fallback `.Click()`, and mouse-wheel scrolling:

- Use UIA patterns when available.
- If no semantic path exists, fail in semantic mode.
- Allow the old gesture in interactive mode only.

This makes failures honest: a tap-only MAUI surface without an invokable automation peer is not background-testable until the app exposes an invokable control.

### 6. Improve App/Test Surfaces for Semantic Automation

For MAUI controls that are currently `Frame`/`Grid` plus `TapGestureRecognizer`, background-safe UI automation may not expose `InvokePattern`.

Recommendations:

- Prefer native `Button`/`MenuItem`/`ToolbarItem` controls for things tests must activate.
- If visual design needs a card, put an actual accessible button inside the card or expose an automation peer that supports invoke.
- In Brinell page objects, prefer controls that activate through UIA patterns.

### 7. Provide a Fully Isolated Runner Option

Even with semantic-first automation, some UI tests will eventually need physical input. To guarantee the developer can keep working while those tests run, run them outside the active desktop:

- Separate Windows VM.
- Windows Sandbox.
- Dedicated RDP session/user session.
- CI agent with an interactive desktop.

This is the only robust answer for tests that must use `SendInput`.

## Suggested Implementation Plan

1. Add `WindowsInteractionOptions` and environment parsing.
2. Remove unconditional `EnsureRootWindowFocused()` from semantic UIA pattern methods.
3. Route all physical input through guarded driver helpers.
4. Change text entry defaults to prefer `ValuePattern.SetValue`; make global keyboard opt-in.
5. Update Brinell tests/docs to distinguish:
   - Semantic/background-safe actions.
   - Interactive/desktop-input actions.
6. Update BodyCam UI tests to use semantic-safe helpers and native invokable controls where needed.
7. Add a Brinell test that runs with physical input disabled and asserts no pointer/keyboard fallback is attempted.

## Expected Outcome

After the fix:

- Most MAUI UI tests can run without moving the user's mouse.
- Tests that use UIA patterns should no longer bring the app to the front.
- Text entry through `SetText()` should not steal active typing focus.
- Tests requiring true physical input will fail clearly unless interactive mode is enabled.
- Developers can continue working during semantic-mode runs, with the caveat that fully physical gesture tests still require an isolated desktop/session.
