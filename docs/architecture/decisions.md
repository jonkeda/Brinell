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

## AD-005: Physical Input Is Opt-In

Routine actions use semantic control APIs and UI Automation patterns. Real mouse,
keyboard, clipboard and foreground-window use all pass through one gate,
`PhysicalInput`, and are only for tests whose subject *is* physical input.

The gate is controlled by `BRINELL_BACKGROUND_MODE`:

| Value | Physical input |
| --- | --- |
| unset | the stack's default - **refused** for MAUI through FlaUI, allowed for WPF and WinForms |
| `1`, or any other value | refused: the call throws `PhysicalInputRefusedException` |
| `audit` | performed, and every use recorded (`BRINELL_PHYSICAL_INPUT_LOG` names a file) |
| `0`, `false`, `off`, `allow` | performed |

The MAUI FlaUI stack declares quiet as its default because every path it needs has
a route that does not take the machine. WPF and WinForms do not, because they still
fall back to real input with no semantic route behind it; refusing by default would
fail their suites rather than quieten them.

A test that exercises real input on purpose says so: `[PhysicalInputFact]`, which
skips itself with a reason when input is refused, and the `PhysicalInput=Deliberate`
trait.

## AD-006: xUnit Assert Only

Use xUnit `Assert`. Do not add FluentAssertions.

## AD-007: Shared Artifact Layout

Screenshots, logs, traces, UAT output, and runner reports should use the shared
`TestResults/<run-id>/suites/<suite>/` layout.

## AD-008: Gestures And Semantic Actions Go Through UI Automation

Where a MAUI app offers no UI Automation route for an action - gestures, a Shell
flyout, a menu item that is not in the tree, setting a date without typing - the
app under test publishes a **gesture bridge**: a custom UI Automation pattern on a
raw-view-only sidecar element, declared per element in markup with
`GestureAutomation.Verbs` from `Brinell.Maui.AppSupport`. Tests reach it through
`IMauiDriver` and `IMauiElement`, never through coordinates.

**The bridge must never become the app's primary automation surface.** Three tests
before any verb is added:

1. **Is it blocked?** If a real UI Automation pattern already answers it -
   `InvokePattern`, `TogglePattern`, `ValuePattern`, `SelectionItemPattern`,
   `ScrollPattern` - it stays there. The bridge is for what UI Automation cannot
   express.
2. **Is the physical path the thing under test?** A test asserting that typing
   fires `TextChanged` must type. The bridge is for arranging state and for actions
   with no semantic route, not for skipping the behaviour under test.
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
