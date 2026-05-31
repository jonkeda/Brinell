# Presenter Message Placement And Step Status Text

This follows `10c presenter collapsible tree recents and aut placement.md`.

## Goals

- Move the transient run/status message below the workspace tree.
- Keep the message above the `Selection` expand/collapse button.
- Keep the workspace summary near the top, but avoid putting the run message above the tree.
- During automatic run delays, do not show `Waiting xxx ms before next step`.
- Show only the relevant step name while the runner is between steps.

## Layout Change

The tree should remain the main surface in the `Tree` tab. The status message belongs directly under it, where it reads as feedback for the selected/running item.

```markui
+--- Tree Tab ------------------------------------------------------+
|  MAUI  Appium  App ok  4 files  4 scenarios                       |
|                                                                  |
|  +--- Workspace Tree -------------------------------------------+ |
|  | v Brinell.Maui.Uat.Tests                                    | |
|  |   uat.config.md                                             | |
|  | > Scenarios                                                 | |
|  | > ExpectedFailures                                          | |
|  +--------------------------------------------------------------+ |
|                                                                  |
|  Greeting should contain "Hello, Alice!"                         |
|                                                                  |
|  [Selection v]                                                   |
+------------------------------------------------------------------+
```

## Status Text Rules

Use the status area for compact, actionable feedback.

Preferred examples:

- `Passed: 1/1 scenarios`
- `Running: Greeting should be visible`
- `Greeting should contain "Hello, Alice!"`
- `Failed: Greeting appears when a name is entered`

Avoid:

- `Waiting 1000 ms before next step: Greeting appears when a name is entered`
- Repeating the scenario name while the user needs to watch the current step
- Long timing/debug details in the visible message area

Timing details can remain in hidden automation text and diagnostics:

- `ExecutionTimingText`
- `DiagnosticsText`
- run detail traces

## Behavior

- When a step is executing, show the step text.
- When an inter-step delay is active, continue showing the upcoming or current step text only.
- The effective delay remains honored and recorded, but it is not exposed as the main visible status.
- After completion, show the compact scenario result, such as `Passed: 1/1 scenarios`.
- If the run fails, show a compact failure message and keep detailed error text in diagnostics.

## Suggested Implementation Notes

- Move `StatusSummaryLabel` from above the tree to the hidden-label/message area below the `WorkspaceTree` border and above `SelectionExpander`.
- Keep `WorkspaceSummaryLabel` above the tree.
- In `WaitBeforeNextStepAsync`, replace the visible status assignment with a step-name-focused message.
- Keep `ExecutionTimingText = FormatExecutionTiming(options)` so tests can still prove the delay happened.
- Do not remove delay diagnostics from `DiagnosticsText`.

## UI Tests

### Test 1: Message Is Below Tree

Setup:

- Start Presenter.
- Load the sample MAUI UAT workspace.

Assertions:

- Workspace summary is still visible above the tree.
- Status message automation is still available as `StatusSummaryLabel`.
- The visual order is tree, status message, `Selection`.

### Test 2: Auto Delay Message Shows Step Name Only

Setup:

- Select `Greeting appears when a name is entered`.
- Set delay to `1000`.
- Press `Run`.

Assertions:

- While the runner is paused between steps, `Status Summary` does not contain `Waiting 1000 ms`.
- `Status Summary` contains the current or next step text.
- `Execution Timing` still contains `Effective delay: 1000 ms`.

### Test 3: Completion Message Stays Compact

Setup:

- Run a selected scenario.

Assertions:

- On completion, `Status Summary` contains `Passed: 1/1 scenarios`.
- `Status Summary` does not contain timing trace text.

## Acceptance

- The status message appears beneath the tree and above the `Selection` button.
- Automatic waits no longer show `Waiting xxx ms` as the visible status.
- The visible status shows step names during execution and compact pass/fail text at completion.
- Delay timing remains recorded for automation and diagnostics.
