# Decision: remove the Windows interaction policy

## What went

The whole `RequirePointerInput` family and everything that existed to serve it:

| Removed | Was |
|---|---|
| `WindowsInteractionOptions`, `WindowsInteractionMode`, `WindowsInteractionConfig` | semantic/interactive modes and their four flags |
| `WindowsInteractionPolicyException` | the refusal type |
| `RequirePointerInput`, `RequireGlobalKeyboardInput`, `RequireClipboardInput`, `RequireForegroundActivation`, `CreatePolicyException` | the gates |
| Three `FlaUIMauiDriver` constructor overloads | thin wrappers passing `Semantic` |
| `framework.windowsInteraction` config, `FrameworkOptions.WindowsInteraction`, `MauiDriverOptions.WindowsInteraction` | the plumbing carrying it |
| Seven `catch (WindowsInteractionPolicyException) { throw; }` blocks | rethrows guarding a broad `catch` |
| Two swallow-and-return-false catches, in `ScrollHelper` and the gesture extensions | policy refusal treated as "no progress" |
| Three unit tests | assertions that the policy refused |

The pointer and keyboard methods themselves stay and simply do the input.

## Why it was safe

Measured before removing anything: across the Buttons, Text, Display and Toggle suites — 70
tests — the pointer-click fallback is **never reached**. UI Automation patterns handle every
click, so the gates were refusing input nothing asked for.

## What was given up

The policy did have one real virtue: it turned a silent fallback into a named error. Three
defects had reached a pointer click through a missing capability and surfaced later as unrelated
assertion failures — MAUI toggles exposing only `TogglePattern`, Android reporting
`SupportsSelectionItemPattern` for every view, and `ToBy` resolving Android ids as Windows ones.
A gate would have named each at the point of failure.

That is now traded for a smaller library, deliberately. The mitigation that remains is the click
ladder itself: a control that reaches `element.Click()` has already failed every pattern, and
that is the place to look when a Windows click misbehaves.

## Verification

- Solution builds clean.
- UI, Buttons + Text + Display + Toggle: **69 / 70** — unchanged. The one failure,
  `ProgressBar_Reset_ReturnsToInitialState`, also failed at baseline.
- Unit: **82 passed, 5 failed, 1 skipped** — the same five pre-existing failures
  (four `ContentDialog_ClickButton_*`, one `Enter_WithNullText_ReturnsPageWithoutAction`),
  with the three policy tests removed rather than broken.

Not measured: Collection, Container, Navigation, Selection, Range and DateTimes, which were out
of scope for this pass and already carry failures of their own.
