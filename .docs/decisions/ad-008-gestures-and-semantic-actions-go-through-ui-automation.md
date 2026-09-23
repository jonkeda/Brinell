# AD-008: Gestures And Semantic Actions Go Through UI Automation

Where a MAUI app offers no UI Automation route for an action - gestures, a Shell
flyout, a menu or toolbar item that cannot be activated through the tree, setting a
date without typing - the app under test publishes a **gesture bridge**: a custom UI
Automation pattern on a raw-view-only sidecar element, declared per element in markup
with `GestureAutomation.Verbs` from `Brinell.Maui.AppSupport`. Tests reach it through
`IMauiDriver` and `IMauiElement`, never through coordinates.

**The bridge is a hard requirement for MAUI on Windows.** The driver has no physical
fallback ([AD-005](ad-005-physical-input-is-opt-in.md)), so an app without
`UseBrinellGestureBridge()` gets only what plain UI Automation patterns offer, and every
other action throws.

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

See [bridge](../contracts/bridge.md) for the verb contract an app must publish.
