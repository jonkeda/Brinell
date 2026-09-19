# Architectural Decisions

This page records active decisions that should guide new Brinell work.

## AD-001: Core Stays Platform-Neutral

`Brinell.Core` owns contracts and shared utilities. Platform element types stay
in platform projects.

## AD-002: Page Objects Own Structure

Tests should describe user intent. Page objects expose meaningful operations and
controls; they do not leak locator plumbing into test methods.

## AD-003: Controls Own Repeated Interaction Behavior

If the same interaction pattern appears in multiple tests or pages, move it into
a Brinell control or shared platform helper.

## AD-004: Wait For State

Do not fix tests by adding arbitrary sleeps or longer delays. Wait for concrete
UI state, navigation completion, busy sentinel changes, text, visibility,
enabled state, request observation, or another observable condition.

On MAUI, the framework does the waiting, and does it one way (2026-09-19,
`.my/stale-readiness/design.md`):

- **One unit per public call.** A call on a control, page, container, collection or
  row is one log entry/exit pair and one failure, with the action inside it.
- **Only the call's poll waits.** Readiness, lookup, visibility, scrolling and
  settling are single attempts inside that poll. Nothing below it sleeps or loops on
  its own, and the caller's `timeoutMs` is the whole budget.
- **A call waits for its scope chain.** Each attempt first asks the scope the control
  stands in, which asks its parent: a row, its collection, the page. A dialog or popup
  shown over a page answers for itself.
- **Every attempt finds its element again.** A replaced element is a signal
  (`StaleElementException`), never a reason to wait on the old one.
- **Non-`Try` members wait; `Try*` members answer about now.**
- **Inside a Core method, wait for an action's effect with `Confirm`**, which never
  repeats the action.

A test that needs more time than the default passes its own `timeoutMs`. It does not
add a delay.

## AD-005: Physical Input Is Opt-In

Routine actions use semantic control APIs and UI Automation patterns.

**MAUI on Windows uses no physical input at all.** `Brinell.Maui.FlaUI` has no code that
moves the pointer, types, writes the clipboard or takes the foreground. Every action is a
UI Automation pattern or a verb the app answers through the gesture bridge (AD-008), and an
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

## AD-006: xUnit Assert Only

Use xUnit `Assert`. Do not add FluentAssertions.

## AD-007: Shared Artifact Layout

Screenshots, logs, traces, UAT output, and runner reports should use the shared
`TestResults/<run-id>/suites/<suite>/` layout.

## AD-008: Gestures And Semantic Actions Go Through UI Automation

Where a MAUI app offers no UI Automation route for an action - gestures, a Shell
flyout, a menu or toolbar item that cannot be activated through the tree, setting a
date without typing - the app under test publishes a **gesture bridge**: a custom UI
Automation pattern on a raw-view-only sidecar element, declared per element in markup
with `GestureAutomation.Verbs` from `Brinell.Maui.AppSupport`. Tests reach it through
`IMauiDriver` and `IMauiElement`, never through coordinates.

**The bridge is a hard requirement for MAUI on Windows.** The driver has no physical
fallback (AD-005), so an app without `UseBrinellGestureBridge()` gets only what plain UI
Automation patterns offer, and every other action throws.

**The bridge must never become the app's primary automation surface.** Three tests
before any verb is added:

1. **Is it blocked?** If a real UI Automation pattern already answers it -
   `InvokePattern`, `TogglePattern`, `ValuePattern`, `SelectionItemPattern`,
   `ScrollPattern` - it stays there. The bridge is for what UI Automation cannot
   express.
2. **Is the physical path the thing under test?** A test asserting that typing
   fires `TextChanged` must type. The bridge is for arranging state and for actions
   with no semantic route, not for skipping the behaviour under test. Such a test
   runs on the mobile head only - Windows has no physical path to exercise.
3. **Does it stay a UI test?** Read what the user can see. If an assertion needs the
   view model, the coverage belongs in `Brinell.Maui.Tests`.

Rules that follow from how the bridge behaves, each measured:

- **Declared, never inferred.** An element is automatable because its markup says
  so, not because it looks drivable.
- **Absent from shipping builds.** UI Automation has no per-caller authentication, so
  the provider is compiled in only when `BRINELL_UIA_BRIDGE` is defined (Debug) and
  turned on only when the launcher sets `BRINELL_UIA_BRIDGE=1` and the app calls
  `UseBrinellGestureBridge()`.
- **Outcomes travel in values or failure codes, never in `S_FALSE`.** UI Automation
  delivers every success HRESULT to the client as `S_OK`. A refusal that must explain
  itself returns `S_OK` with the reason in the payload; one that needs no explanation
  returns `BRINELL_E_DECLINED`. A failing call carries no payload.
- **Every verb is classified** as a read, an element action or an app action; the
  accessibility audit examines element actions, and adding a verb without deciding
  fails `ContractTests`.
- **Instrumenting an element is the moment to ask whether it is gesture-only.** The
  accessibility audit lists every instrumented element that nothing but a pointer can
  reach; that list is a backlog for the app, not a licence.

## AD-009: App Bugs Stay Failures

The framework waits for the app to *reach* a state. It never waits out, retries
through or swallows a *defect*. A MAUI UI test found the Todo sample's lost
`CanExecuteChanged` because it failed; a framework that had absorbed that failure
would have shipped the bug.

- **An action that may have run is never repeated.** Its effect is confirmed
  (`Confirm`); an effect that never shows, or an element replaced before it could be
  read, fails the call.
- **An action that did not happen may be asked again, within the budget.** A Core
  method throws `ElementNotReadyException` only before it acts: its guards come first,
  and a driver raises it when nothing was performed (no bridge target answered, the
  item is disabled). A command that never re-enables still fails, as "disabled", when
  the budget runs out.
- **Some failures end the call at once:** the app process exits or the driver session
  is lost (`AppUnavailableException`), or a page's busy signal is missing or unreadable
  (`ScopeNotReadyException`, a configuration error).
- **A budget the caller set is never extended**: a scroll below the call gets what is
  left of it. No other element is used in place of the one asked for - a row whose
  element now shows another item is found again by its key - and an error is never
  treated as absence: an exception every attempt raised is reported with its type and
  count.
- **Success after trouble is reported.** A call that passed after its element was
  replaced three or more times, or after using more than half its budget, logs a
  near-miss warning (thresholds: `MauiTestContextOptions.NearMiss`). Set `BRINELL_CALL_LOG` to a folder to write every context's calls
  to CSV and list them.

Proven on 2026-09-19 (`.my/stale-readiness/plan.md`, step 8): with the bug put back,
the Todo journey TOD.04.4 failed 3 times out of 3, naming the dialog that never
appeared.

## AD-010: MAUI Ahead Of Core, On Purpose

`Brinell.Maui` owns the interfaces its call model needs (`IMauiElement`,
`IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext`) and no longer
implements Core's element, driver, scope and page interfaces. Core was not changed to
get there. The MAUI stack is where the model was designed and proven; the other stacks
keep Core's shapes until the proven shape moves down to Core, one stack at a time, as
its own project.

Until then, expect the MAUI types to differ from their Core counterparts. Do not add
adapters between the two: a bridge now would have to be undone in the move down.
