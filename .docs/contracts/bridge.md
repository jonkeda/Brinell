# Contract: The gesture bridge (`GestureAutomation.Verbs`)

When a MAUI app has an action that plain UI Automation cannot express, the app under
test publishes a **gesture bridge**. This is the contract an app author must implement so
`Brinell.Maui.FlaUI` can drive it. Decision:
[AD-008](../decisions/ad-008-gestures-and-semantic-actions-go-through-ui-automation.md).

## What the app publishes

- A reference to `Brinell.Maui.AppSupport` and a call to `UseBrinellGestureBridge()` in
  app startup.
- Per element, in markup, the verbs it answers: `GestureAutomation.Verbs="..."`.
- The provider is compiled in only under `BRINELL_UIA_BRIDGE` (Debug) and active only
  when the launcher sets `BRINELL_UIA_BRIDGE=1`. It is **absent from shipping builds** -
  UI Automation has no per-caller authentication.

## How a verb behaves

- **Declared, never inferred.** An element is automatable because its markup says so.
- **Classified.** Every verb is a *read*, an *element action* or an *app action*. Adding
  a verb without classifying it fails `ContractTests`. The accessibility audit examines
  element actions.
- **Outcomes in values or failure codes, never `S_FALSE`.** Every success HRESULT reaches
  the client as `S_OK`. A refusal that must explain itself returns `S_OK` with the reason
  in the payload; a refusal that needs no explanation returns `BRINELL_E_DECLINED`. A
  failing call carries no payload.
- **Republished per page.** An app republishes its bridge between pages, so a verb the app
  did not answer is retried by the call - it is not a missing route
  ([AD-009](../decisions/ad-009-app-bugs-stay-failures.md)).

## Three admission tests before adding a verb

1. **Is it blocked?** If a real UI Automation pattern already answers it (`Invoke`,
   `Toggle`, `Value`, `SelectionItem`, `Scroll`), it stays there.
2. **Is the physical path the thing under test?** A test asserting typing fires
   `TextChanged` must type - on the mobile head, not the bridge.
3. **Does it stay a UI test?** Read what the user can see; if it needs the view model, the
   coverage belongs in `Brinell.Maui.Tests`.

## Broken when

- A verb duplicates an existing UI Automation pattern (test 1).
- A verb is added without a read/element-action/app-action classification.
- The bridge provider ships in a release build.
- A refusal is signalled with `S_FALSE` instead of a payload or `BRINELL_E_DECLINED`.
