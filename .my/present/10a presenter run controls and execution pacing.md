# Presenter Run Controls And Execution Pacing

This is a follow-up to `10 presenter tabbed tree redesign.md`.

The current Presenter shape is close, but the run area and tree growth need another UI pass. The main goal is to make execution feel obvious and trustworthy while keeping the narrow Presenter layout compact.

## Goals

- Remove the execution mode toggle.
- Treat `Run` as automatic execution.
- Treat `Next` as step execution.
- Keep delay controls next to the run buttons.
- Make delay compact and clearly labeled in milliseconds.
- Let the tree list grow when expanding collections.
- Ensure step completion reflects real command completion and delay timing.

## Run Toolbar

The run toolbar should be a single compact row when space allows:

```markui
+--- Run --------------------------------------------------+
| [#4 Run] [#5 Stop] [#6 Next]   Delay [250  ] ms          |
+----------------------------------------------------------+
```

Responsive fallback for narrow windows:

```markui
+--- Run --------------------------------------------------+
| [#4 Run] [#5 Stop] [#6 Next]                             |
| Delay [250  ] ms                                         |
+----------------------------------------------------------+
```

## Behavior Changes

### No Mode Toggle

Remove the visible mode selector entirely.

The user should not have to choose between automatic mode and step mode before running. The command they press defines the mode.

### Run Means Auto Mode

`Run` starts the selected runnable scope and executes steps automatically until the selected scope finishes, fails, is canceled, or the user presses `Stop`.

`Run` must honor the configured delay between visible step actions.

### Next Means Step Mode

`Next` executes exactly one pending step from the selected runnable scope.

If no run session exists yet, `Next` should create a step session from the selected runnable node and execute the first pending step.

If a run session is paused after a step, `Next` should execute the next pending step.

### Delay Input

Move the delay input next to the run buttons.

The delay input should:

- Be small enough for at most 5 characters.
- Accept numeric millisecond values.
- Show an `ms` label immediately after the input.
- Keep a stable width so the toolbar does not jump while editing.

Recommended automation names:

- `RunButton`
- `StopButton`
- `NextButton`
- `DelayMillisecondsInput`
- `DelayMillisecondsLabel`

## Tree Expansion Issue

When the user expands a collection or folder node, the tree list should visibly grow or scroll to reveal the newly visible children.

Current problem:

- Clicking a collection expands its data, but the visible tree list size does not get bigger.
- The lower button or selection expander stays at the same vertical level.
- This makes the tree feel like it did not actually expand.

Expected behavior:

- Expanded child rows become visible immediately.
- The tree content area uses available vertical space.
- The `Selection` expander moves down when content grows, until the tab reaches its available height.
- After available height is filled, the tree area scrolls internally.
- The `Selection` expander remains below the tree list, not overlaying or pinning over newly expanded rows.

Suggested layout shape:

```markui
+--[[Tree]]--[Config]--[Diagnostics]--[Discovery]--[Command Catalog]--+
| + Tree list fills remaining vertical space                         |
| | - #20 Workspace                                                   |
| |   - #20 Collection                                                |
| |     - #21 scenario.uat.md                                         |
| |       - #24 Suite                                                 |
| |       - #25 Scenario                                              |
| |       - #26 Step                                                  |
| +------------------------------------------------------------------+ |
| [Selection ^]                                                       |
+--------------------------------------------------------------------+
```

Implementation note: the tree list should be in a layout row that can expand, such as a star-sized grid row. The selection expander should live in an auto-sized row below it.

## Execution Pacing Issue

When `Run` starts, the target app opens, but the Presenter checks off steps so quickly that it does not look like the actions were actually executed with the requested delay.

This needs verification and probably a runtime/UI timing fix.

Expected behavior:

- A step changes to running before its command executes.
- The app action is awaited.
- The configured delay is awaited after the command or before the next command.
- The step changes to passed only after the command completes successfully.
- The next step does not start until the delay has elapsed.
- `Stop` can interrupt before the next step begins.

Diagnostics should make this believable:

- Record per-step started and completed timestamps.
- Record the effective delay value used for the run.
- Show the latest running step long enough to be seen when delay is nonzero.
- Prefer a minimum visible running-state duration if the runtime command completes instantly.

## Acceptance

- The run area no longer has a mode toggle.
- `Run` performs automatic execution.
- `Next` performs one-step execution.
- Delay is next to the run buttons.
- Delay input is compact and supports at most 5 visible characters.
- Delay input has an adjacent `ms` label.
- Expanding a collection or folder visibly grows the tree content or enables tree scrolling.
- The selection expander moves with the tree layout and does not mask expanded rows.
- Step status updates are tied to awaited command completion.
- Auto-run honors the configured delay between steps.
- Presenter diagnostics expose enough timing information to prove the delay was applied.
