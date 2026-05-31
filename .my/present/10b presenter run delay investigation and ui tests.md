# Presenter Run Delay Investigation And UI Tests

This is a follow-up to `10a presenter run controls and execution pacing.md`.

Observed behavior: with the `Greeting appears when a name is entered` scenario selected, pressing `Run` checks off every visible step almost instantly. It does not feel like the configured delay is being honored.

The screenshot shows the selected scenario and its steps all marked passed:

- `Greeting appears when a name is...`
- `Greeting should be visible`
- `Greeting should contain "Hello,...`
- `I am on the Main page`
- `I clear Name`
- `I enter "Alice" into Name`
- `I tap Greet`
- `Name should be enabled`

## Problem Statement

`Run` should be automatic mode, but automatic mode still needs believable pacing.

When delay is nonzero, the user should be able to see steps progress one by one. If the runner marks every step passed in a flash, the user cannot trust that the app actions were executed or that the delay input is connected.

## Investigation Targets

Check these in order.

### 1. Confirm The Effective Delay Value

The delay input may not be the value used by the run loop.

Possible causes:

- The `Entry.Text` binding to `ExecutionDelayMilliseconds` does not update before `Run` executes.
- Invalid or partial numeric input falls back to the old value.
- The UI shows a delay value, but the viewmodel is still using `0` or the default.
- The current run captures no fixed delay value, so edits during a run can create confusing behavior.

Required diagnostics:

- At run start, capture `RunStartedAt`.
- Capture `RequestedDelayMilliseconds` from the viewmodel.
- Capture `EffectiveDelayMilliseconds` used for that run.
- Add the effective delay to `DiagnosticsText` immediately when the run starts, not only after completion.

### 2. Confirm The Delay Is In The Correct Place

Current intended behavior is delay between steps. That means the first step can run immediately, then each next step waits.

For the current greeting scenario with 7 steps and `Delay = 1000`, total elapsed time should be at least 6 seconds, because there are 6 gaps between 7 steps.

If the total is near zero, `Task.Delay` was skipped or canceled.

If the total is only around one second, the delay is probably applied once per scenario instead of once per step.

### 3. Confirm The UI Can Render The Running/Waiting State

Even when `Task.Delay` is awaited, the UI may visually jump straight to the final passed state if the status changes are not flushed before command execution or if the tree item status is updated too late.

Required visible states:

- Before each command: the current step row is `Running`.
- Between commands: the status summary shows `Waiting {N} ms before next step`.
- Passed checkmark is applied only after the command completes.

### 4. Confirm The Selected Node Scope

The user selected the scenario node `Greeting appears when a name is entered`.

Expected run scope:

- Only that scenario runs.
- All steps under that scenario run in order.
- Delay applies between each visible step.

Add diagnostics for:

- selected node kind
- selected node name
- scenario count in run scope
- step count in run scope

## Proposed Fix Direction

- Capture a per-run immutable `RunExecutionOptions` object when `Run` starts.
- Parse delay from the input into that options object before creating the execution session.
- Use the captured delay for the entire run.
- Record timing for every step and every inter-step wait.
- Update `DiagnosticsText` at run start, after every step, and after every wait.
- If delay is greater than zero, keep a visible waiting state between steps.

Suggested model:

```text
PresenterRunExecutionOptions
  SelectedNodeKind
  SelectedNodeName
  ScenarioCount
  StepCount
  EffectiveDelayMilliseconds
  StartedAt
```

Suggested timing record:

```text
PresenterStepTiming
  StepNumber
  StepText
  StartedAt
  CompletedAt
  WaitStartedAt
  WaitCompletedAt
  DelayAfterMilliseconds
```

## UI Test Requirements

The current Presenter UAT smoke test is insufficient because it only asserts the final `Passed` state.

Add UI tests that prove pacing from the outside.

### Test 1: Auto Run Honors Delay Wall Clock

Purpose: prove `Run` waits between steps.

Setup:

- Open Presenter.
- Select the `Greeting appears when a name is entered` scenario node.
- Set `Delay` to `1000`.
- Press `Run`.

Assertions:

- Final status contains `Passed`.
- Elapsed wall-clock time from pressing `Run` to final `Passed` is at least `6000 ms` for the 7-step scenario.
- Diagnostics contain `Effective delay: 1000 ms`.
- Diagnostics contain one timing line per executed step.
- Diagnostics contain at least 6 delay/wait records.

Notes:

- Allow a small tolerance only below the expected duration, such as `-250 ms`.
- Do not use arbitrary sleeps in the test. Use polling for final state while measuring elapsed wall-clock time.

### Test 2: Auto Run Shows Waiting State

Purpose: prove the UI visibly enters the inter-step wait state.

Setup:

- Select the `Greeting appears when a name is entered` scenario node.
- Set `Delay` to `2000`.
- Press `Run`.

Assertions:

- `Status Summary` eventually contains `Waiting 2000 ms before next step`.
- While the run is active, not all visible scenario steps are already passed.
- Final status eventually contains `Passed`.

Notes:

- This test catches the "lightning run" bug better than a final-state-only test.
- The assertion should poll for the waiting status immediately after pressing `Run`.

### Test 3: Next Ignores Auto Delay And Runs One Step

Purpose: keep `Next` behavior distinct from `Run`.

Setup:

- Select the `Greeting appears when a name is entered` scenario node.
- Set `Delay` to `2000`.
- Press `Next`.

Assertions:

- Exactly one pending step transitions to passed.
- The run does not continue automatically to the next step.
- `Status Summary` does not show the inter-step auto wait message.

### Test 4: Delay Input Is The Value Used By Run

Purpose: catch binding or parse failures.

Setup:

- Set delay to `1234`.
- Press `Run`.

Assertions:

- Diagnostics contain `Effective delay: 1234 ms`.
- The measured gap between at least two adjacent step starts is at least `1234 ms`.

### Test 5: Selected Scenario Scope Is Honored

Purpose: prove selecting the scenario node does not run the whole workspace.

Setup:

- Select the `Greeting appears when a name is entered` scenario node.
- Set delay to `0`.
- Press `Run`.

Assertions:

- Diagnostics contain selected node kind `Scenario`.
- Diagnostics contain selected node name `Greeting appears when a name is entered`.
- Diagnostics contain scenario count `1`.
- Steps from unrelated scenarios are not marked passed by this run.

## Automation Surface Needed

Existing:

- `RunButton`
- `StopButton`
- `NextButton`
- `DelayMillisecondsInput`
- `StatusSummaryLabel`
- `WorkspaceTreeText`
- `SelectionDetailsText`

Add or expose:

- `DiagnosticsText` as a page-object control.
- A way to select a tree node by text in the real `WorkspaceTree`, not only via hidden text.
- A compact observable run trace, exposed either through `DiagnosticsText` or a hidden automation label such as `ExecutionTimingText`.
- Optional hidden automation label: `RunScopeText`.

Recommended page-object additions:

```csharp
[UatName("Diagnostics")]
public Label<PresenterPage> Diagnostics => Label("DiagnosticsText");

[UatName("Execution Timing")]
public Label<PresenterPage> ExecutionTiming => Label("ExecutionTimingText");

[UatName("Run Scope")]
public Label<PresenterPage> RunScope => Label("RunScopeText");
```

## Acceptance

- `Run` with `Delay = 1000` on the 7-step greeting scenario takes at least about 6 seconds.
- `Status Summary` shows a waiting state between steps when delay is nonzero.
- Diagnostics show the effective delay at run start and after completion.
- Diagnostics show per-step timing and inter-step wait timing.
- UI tests fail if delay is skipped.
- UI tests fail if the delay input value is not the value used by the run.
- UI tests fail if selecting the greeting scenario runs unrelated scenarios.
