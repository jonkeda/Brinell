# MAUI Platform Guide

Brinell supports MAUI through shared MAUI controls plus driver adapters.

## Projects

- `srcnew/Brinell.Maui`
- `srcnew/Brinell.Maui.Appium`
- `srcnew/Brinell.Maui.FlaUI`
- `srcnew/Brinell.Maui.CommunityToolkit`
- `testsnew/Brinell.Maui.Tests`
- `testsnew/Brinell.Maui.UITests`
- `testsnew/Brinell.Maui.Uat.Tests`

## Driver Choices

| Driver | Use when |
| --- | --- |
| FlaUI | Windows MAUI desktop automation |
| Appium | Android, iOS, or Appium-backed Windows automation |

## Rules

- Prefer automation IDs and semantic control APIs.
- Wait for page readiness after navigation.
- Keep Appium capability setup in fixtures/options.
- Physical input is refused by default on Windows; see below.

## Background Execution On Windows

A MAUI run through FlaUI does not take the machine. You can start a full run and keep
working in your editor: nothing types into your window, the pointer does not move,
and the app stays behind whatever you are using.

That is the default. Three things make it true:

- **No physical input.** Every real mouse, keyboard, clipboard or foreground call goes
  through `PhysicalInput` and is refused. A refused call throws, naming the semantic
  route to use instead. `BRINELL_BACKGROUND_MODE=0` allows real input for a run - see
  [AD-005](../architecture/decisions.md#ad-005-physical-input-is-opt-in).
- **The window cannot be activated.** Invoking a WinUI button through
  `InvokePattern` activates its window, which is not physical input and so is not
  caught by the policy. The driver marks the app's window `WS_EX_NOACTIVATE`, which
  stops it.
- **A watchdog puts it back.** From the moment the app launches, any of its windows
  that becomes the foreground is sent behind and your window restored. The one
  expected occurrence is launch itself, once per test collection, for a few
  milliseconds. `ForegroundWatchdogTests` fails if navigating the app takes the
  foreground.

**Do not move the window off-screen to get the same effect.** `BRINELL_AUT_PLACE=offscreen`
exists, but UI Automation's `IsOffscreen` counts the monitor, so controls plainly inside
their page report themselves invisible and "scroll into view" waits time out.

## Gestures And Semantic Actions

Some actions have no UI Automation route on Windows: swipes, pull-to-refresh, a Shell
flyout, a menu item that is not in the tree until a context menu opens, setting a
picker's date without typing. For these the app under test publishes a **gesture
bridge** - see [AD-008](../architecture/decisions.md#ad-008-gestures-and-semantic-actions-go-through-ui-automation).

In the app, with `Brinell.Maui.AppSupport`:

```csharp
builder.UseBrinellGestureBridge();   // in MauiProgram, before the first page loads
```

```xml
<SwipeView AutomationId="TestSwipeView"
           uia:GestureAutomation.Verbs="SwipeRight,CloseFlyout" />
```

In a test:

```csharp
if (driver.SupportsGesture("TestSwipeView", MauiGesture.SwipeRight))
{
    driver.PerformGesture("TestSwipeView", MauiGesture.SwipeRight);   // throws if refused
}
```

Most control objects use the bridge on their own when the app declares the verb -
`Entry.SetText`, `Picker.SelectByText`, `ShellFlyout.Open`, a `ScrollView` revealing a child -
and use UI Automation patterns when it does not. They ask first and take one route; they
never try one and fall back to another.

- **`DatePicker.SetDate` and `TimePicker.SetTime` have no second route.** Without the
  `SetDate`/`SetTime` verb they throw, naming it. The WinUI calendar and clock flyout walks
  they used to fall back to were removed.
- **Long press and swipe** use the `LongPress` and `Swipe*` verbs where the element declares
  them, and real pointer input otherwise - which the quiet default refuses.
- **Control objects never see UI Automation.** They ask `IMauiElement` in terms of what they
  do (`Checked`, `SetRangeValue`, `OpenDropdown`, `SupportsInvoke`), and the Windows element
  answers from the patterns. The `*PatternElement` interfaces are gone from MAUI.

- **It is test instrumentation, not a product feature.** The bridge is compiled in only
  in Debug and turned on only when the test driver asks. A Release build contains none
  of it.
- **Declare only what has no other route.** The three admission tests in AD-008 decide.
- **Read the accessibility audit.** Every run writes `accessibility-audit.md` listing the
  instrumented elements nothing but a pointer can reach. Those are accessibility defects in
  the app; the bridge lets tests reach them, it does not fix them.

When a verb is not answered, see
[Troubleshooting](../guides/troubleshooting.md#gesture-bridge-problems).

## Run Artifacts

Besides screenshots and logs, a Windows UI run writes to
`TestResults/<run-id>/suites/<suite>/attachments/`:

| File | What it is |
| --- | --- |
| `accessibility-audit.md` | Instrumented elements and what each offers without a pointer |
| `test-timings.md` | Every test class with count, total, mean and slowest test, against `timing-baseline.json`; classes that have become markedly slower are marked |
| `test-timings.csv` | The raw per-test durations, written as the run goes |
| `test-timings-baseline-candidate.json` | Copy over `testsnew/Brinell.Maui.UITests/timing-baseline.json` to refresh the baseline from a good run |

## Run

See [MAUI Run Guide](../run/MAUI.md) and
[MAUI Android Run Guide](../run/maui-android.md).
