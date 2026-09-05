# Plan: make ContentDialog the popup scope

## Goal

Remove locator-specific popup searches from the neutral driver contract. Resolve the active dialog
once as `ContentDialog`'s container root, then use normal container-scoped lookups for buttons,
prompt input, and dismissal.

## Contract

- Replace `FindPopupElement` and `TryFindPopupElement` with
  `IMauiElement? TryFindActiveDialogRoot()`.
- Keep the operation immediate. Waiting remains in the control/container polling infrastructure.
- Keep platform mechanics behind each driver:
  - FlaUI searches sibling top-level windows for the WinUI `ContentDialog` root.
  - Appium searches the normal tree for the platform's active native dialog root.

## Control

- Override `ContentDialog.FindContainerRootElement` to use the active-dialog-root capability and
  throw `ElementNotFoundException` when no dialog is active.
- Expose dialog children as ordinary control objects scoped to that root.
- Compose interaction and state through those controls, for example
  `dialog.DialogButton("OK").Click().WaitExists(false)`.
- Do not add dialog-specific interaction or waiting methods to the container.
- Remove popup-window and parent-scope fallback methods.

## Verification

1. Update semantic tests to expose only an active dialog root and assert button lookup is scoped to
   that root.
2. Run the focused `ContentDialogControlTests`.
3. Build the affected MAUI, Appium, and FlaUI projects through the narrow solution/test build.
4. Run the focused Windows dialog UI test when the local test app/driver is available.