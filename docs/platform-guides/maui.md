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
| Appium | Android and iOS. The Appium driver refuses any other platform. |

## Rules

- Prefer automation IDs and semantic control APIs.
- Wait for page readiness after navigation.
- Keep Appium capability setup in fixtures/options.
- On Windows the app under test must host the gesture bridge; there is no physical
  input to fall back on. See below.

## Background Execution On Windows

A MAUI run through FlaUI does not take the machine. You can start a full run and keep
working in your editor: nothing types into your window, the pointer does not move,
and the app stays behind whatever you are using.

That is not a setting; there is no other mode. Three things make it true:

- **No physical input.** The Windows driver has no code that clicks, types, writes the
  clipboard or takes the foreground. An action with no UI Automation pattern and no
  bridge verb throws `NotSupportedException`, naming the route the app would have to
  offer. `BRINELL_BACKGROUND_MODE` has no effect on MAUI - see
  [AD-005](../architecture/decisions.md#ad-005-physical-input-is-opt-in).
- **The window cannot be activated.** Invoking a WinUI button through
  `InvokePattern` activates its window, which is not physical input. The driver marks
  the app's window `WS_EX_NOACTIVATE`, which stops it.
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
flyout, a menu item that is not in the tree until a context menu opens, a toolbar item
whose Invoke pattern reports success and does nothing, setting a picker's date without
typing. For these the app under test publishes a **gesture bridge** - see
[AD-008](../architecture/decisions.md#ad-008-gestures-and-semantic-actions-go-through-ui-automation).

**On Windows the bridge is required, not optional.** Without it only plain UI Automation
patterns work - invoke, toggle, select, value, range, expand/collapse, scroll, and reads -
and everything else throws.

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
- **Long press and swipe** use the `LongPress` and `Swipe*` verbs, and throw
  `GestureUnavailableException` where the element does not declare them. `DoubleClick` is the
  `DoubleTap` verb; a raw `Click` is the `Tap` verb.
- **Toolbar items** (`ToolbarButton`) are raised through `IMauiElement.InvokeToolbarItem`, and
  menu items through `IMauiDriver.InvokeMenuItem`. On Android and iOS a toolbar item is tapped.
- **App-level questions go to `Context.AppElement`**: the Shell flyout (`OpenFlyout`,
  `CloseFlyout`, `IsFlyoutOpen`), the alert on screen (`ReadAlert`), the active dialog
  (`TryFindActiveDialog`) and targets reached by id (`TryFindDeclared`). Control objects call
  `IMauiElement` only, never the driver.
- **No Windows route at all:** `RightClick` (use `InvokeMenuItem` for the item you wanted),
  `Hover`, and typing key by key with `TextInputMethod.Keys` (use `SetValue`). A test of
  per-keystroke behaviour runs on the mobile head.
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

## Virtualized Collections

On Windows, `CollectionObjectBase.Item(int)` uses the logical data-source index reported by the
nearest UI Automation `PositionInSet`; recycled row containers do not change that meaning.
`ScrollToItem(int)` asks the app to materialize that logical index, and content searches revisit
each recycled window until the app-reported item count is reached.

Android and iOS currently publish no equivalent logical-index source. Their collection item APIs
therefore retain positional semantics over the rows currently exposed by Appium.

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
