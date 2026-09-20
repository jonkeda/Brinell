# AD-005: Physical Input Is Opt-In

Routine actions use semantic control APIs and UI Automation patterns.

**MAUI on Windows uses no physical input at all.** `Brinell.Maui.FlaUI` has no code that
moves the pointer, types, writes the clipboard or takes the foreground. Every action is a
UI Automation pattern or a verb the app answers through the gesture bridge
([AD-008](ad-008-gestures-and-semantic-actions-go-through-ui-automation.md)), and an
action with neither throws `NotSupportedException` - or `GestureUnavailableException` for a
gesture - naming the route the app would have to offer. There is no setting that brings
physical input back, and no test of real input runs on Windows; such a test belongs on the
mobile head, where Appium injects input inside the device.

**WPF and WinForms still fall back to real input**, with no semantic route behind it. There,
real mouse, keyboard, clipboard and foreground-window use pass through one gate,
`PhysicalInput`, controlled by `BRINELL_BACKGROUND_MODE`:

| Value | Physical input |
| --- | --- |
| unset | performed |
| `1`, or any other value | refused: the call throws `PhysicalInputRefusedException` |
| `audit` | performed, and every use recorded (`BRINELL_PHYSICAL_INPUT_LOG` names a file) |
| `0`, `false`, `off`, `allow` | performed |

Refusing by default would fail their suites rather than quieten them. The variable has no
effect on MAUI. The history and the removal plan are in `.my/bridge/no-physical-input.md`.

**Broken when:** `Brinell.Maui.FlaUI` gains pointer/keyboard/clipboard/foreground code, or a
setting is added to re-enable physical input on MAUI.
