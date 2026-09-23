# Brinell Presenter Current UI And Execution Design

Status: current design target.

This replaces the archived `10`, `10a`, `10b`, `10c`, and `10d` Presenter notes. It is the single current source of truth for the Presenter workspace tree, run controls, execution pacing, recent folders, AUT placement, and visible status behavior.

## Goals

- Keep the Presenter narrow enough to sit beside the application under test.
- Make the workspace tree the primary interaction surface.
- Show Markdown UAT content clearly without build/source clutter.
- Let `Run` mean automatic execution and `Next` mean one-step execution.
- Make delay behavior trustworthy through timing diagnostics, not noisy visible text.
- Keep transient run messages below the tree and above selection details.
- Reopen the last useful workspace automatically.
- Launch the AUT beside the Presenter when supported.

## Main Layout

Top actions are compact:

- `Open` as a split button, with recent folders in the dropdown.
- `Reload`.
- `Validate`.

The run toolbar has no mode toggle. The clicked command defines the mode.

```markui
+--- Brinell Presenter --------------------------------------+
| [#1 Open][v] [#2 Reload] [#3 Validate]                      |
|                                                            |
| +--- Run ------------------------------------------------+  |
| | [#4 Run] [#5 Stop] [#6 Next]   Delay <250__> ms        |  |
| +--------------------------------------------------------+  |
|                                                            |
| +--[[Tree]]--[Config]--[Diagnostics]--[Discovery]--[Command Catalog]--+
| | MAUI  Appium  App ok  4 files  4 scenarios              | |
| |                                                          | |
| | v Brinell.Maui.Uat.Tests                                | |
| |   uat.config.md                                         | |
| | > Scenarios                                             | |
| | > ExpectedFailures                                      | |
| |                                                          | |
| | Greeting should contain "Hello, Alice!"                  | |
| |                                                          | |
| | [Selection v]                                           | |
| +----------------------------------------------------------+ |
+------------------------------------------------------------+
```

Responsive fallback for narrow widths:

```markui
+--- Run --------------------------------------------------+
| [#4 Run] [#5 Stop] [#6 Next]                             |
| Delay <250__> ms                                         |
+----------------------------------------------------------+
```

## Run Toolbar

Required controls:

- `RunButton`
- `StopButton`
- `NextButton`
- `DelayMillisecondsInput`
- `DelayMillisecondsLabel`

Delay input requirements:

- Numeric milliseconds only.
- Stable compact width.
- At most 5 visible characters.
- Adjacent visible `ms` label.
- Parsed once at run start into immutable run options.

## Execution Semantics

`Run` starts automatic execution for the selected runnable node and its runnable descendants. It keeps running until the scope completes, fails, is canceled, or `Stop` is pressed.

`Next` executes exactly one pending step. If no step session exists, it creates one from the selected runnable node and executes the first step. It does not use the automatic inter-step delay.

`Stop` cancels the active automatic run or active step session.

Run scope:

| Selected node | Run scope |
| --- | --- |
| Workspace folder | All runnable UAT Markdown files in the workspace |
| Child folder | Runnable UAT Markdown files inside that folder |
| UAT Markdown file | Scenarios in that file |
| Suite | Scenarios in that suite |
| Scenario | That scenario |
| Step | Owning scenario until step-level resume exists |
| Workflow config or non-runnable Markdown | No run target |

## Execution Pacing

Automatic run delay is applied between steps, not before the first step.

For a 7-step scenario and `Delay = 1000`, elapsed time should be at least about 6 seconds because there are 6 inter-step gaps.

Required ordering:

1. Mark the current step running.
2. Await the app command.
3. Mark the step passed only after command completion.
4. Await the configured delay before starting the next step.
5. Allow `Stop` to cancel before the next step begins.

Timing diagnostics should record:

- selected node kind
- selected node name
- scenario count
- step count
- effective delay
- run start/end timestamps
- per-step start/completion timestamps
- per-step wait start/completion timestamps

Suggested model:

```text
PresenterRunExecutionOptions
  SelectedNodeKind
  SelectedNodeName
  ScenarioCount
  StepCount
  EffectiveDelayMilliseconds
  StartedAt

PresenterStepTiming
  StepNumber
  StepText
  StartedAt
  CompletedAt
  WaitStartedAt
  WaitCompletedAt
  DelayAfterMilliseconds
```

## Visible Status Message

The visible status message belongs in the `Tree` tab below the workspace tree and above the `Selection` expander.

Show compact, actionable text:

- `Passed: 1/1 scenarios`
- `Running: Greeting should be visible`
- `Greeting should contain "Hello, Alice!"`
- `Failed: Greeting appears when a name is entered`

Do not show delay text as the main visible message:

- Avoid `Waiting 1000 ms before next step`.
- Avoid long timing traces.
- Avoid repeating the scenario name while a step is running.

During inter-step delay, keep showing the current or upcoming step name. Timing details remain in diagnostics and hidden automation text.

## Tree Tab Layout

The tree tab uses this vertical order:

1. Workspace summary.
2. Workspace tree.
3. Status message.
4. Selection expander.

The workspace tree should use available vertical space. When expanded content exceeds the available space, the tree scrolls internally. The status message and selection expander must stay below the tree, not overlay expanded rows.

```markui
+--[[Tree]]--[Config]--[Diagnostics]--[Discovery]--[Command Catalog]--+
| MAUI  Appium  App ok  4 files  4 scenarios                         |
|                                                                     |
| +--- Workspace Tree ----------------------------------------------+ |
| | v Brinell.Maui.Uat.Tests                                       | |
| |   uat.config.md                                                | |
| | v Scenarios                                                    | |
| |   v main-page-greeting.uat.md                                  | |
| |     v MAUI Main Page Greeting                                  | |
| |       > Greeting appears when a name is entered                | |
| |   main-page-validation.uat.md                                  | |
| | > ExpectedFailures                                             | |
| +-----------------------------------------------------------------+ |
|                                                                     |
| I enter "Alice" into Name                                           |
|                                                                     |
| [Selection ^]                                                       |
| Type: Scenario                                                      |
| Name: Greeting appears when a name is entered                       |
| File: main-page-greeting.uat.md                                     |
+---------------------------------------------------------------------+
```

## Tree Content

The tree starts at the selected workspace folder.

Visible node types:

- Workspace folder.
- Child folders with at least one Markdown descendant.
- Markdown files, including `.uat.md`, `uat.config.md`, and normal `.md`.
- UAT suite nodes under `.uat.md` files.
- UAT scenario nodes under suites.
- UAT step nodes under scenarios.

Hidden content:

- `.dll`
- `.json`
- `.cs`
- `.csproj`
- `bin`
- `obj`
- any non-Markdown file

Collapse behavior:

- Workspace starts expanded.
- First-level folders start collapsed unless they contain only one Markdown path.
- Markdown files expand when they have parsed UAT structure.
- Suite and scenario nodes can expand.
- Step nodes cannot expand.
- Collapsed parents keep aggregate child status.
- A failed child should make collapsed parent status show failure.
- Selecting a collapsed parent still runs runnable descendants.

Implementation shape:

```text
WorkspaceNode
  Name
  NodeType
  Status
  Icon
  Depth
  FilePath
  Scenario
  Step
  IsExpanded
  CanExpand
  Children
  Details
```

The hierarchical model is the source of truth. The visible tree can be a flattened indented list rebuilt from expansion state.

## Tabs

`Tree` is the main tab. The other tabs are compact diagnostic views.

```markui
+--[Tree]--[[Config]]--[Diagnostics]--[Discovery]--[Command Catalog]--+
| Runtime                                                            |
| Target: MAUI                                                       |
| Fixture: Appium                                                    |
| AppPath: ../../samples/Brinell.Samples.Maui.App/...                |
| WorkingDirectory: ../..                                            |
|                                                                    |
| Assemblies                                                         |
| #12 Pages      ../Brinell.Maui.UITests/bin/...                     |
| #12 Controls   ../../srcnew/Brinell.Maui/bin/...                   |
| #12 Commands   ../../srcnew/Brinell.Uat/bin/...                    |
+--------------------------------------------------------------------+
```

```markui
+--[Tree]--[Config]--[[Diagnostics]]--[Discovery]--[Command Catalog]--+
| #12 Config ok                                                       |
| #12 Parse ok                                                        |
| #12 Bind ok                                                         |
| Effective delay: 1000 ms                                            |
| Step 1: 10:42:01.100 -> 10:42:01.240, wait 1000 ms                  |
+--------------------------------------------------------------------+
```

```markui
+--[Tree]--[Config]--[Diagnostics]--[[Discovery]]--[Command Catalog]--+
| Pages                                                              |
| - Main                                                             |
|   - Name                                                           |
|   - Greet                                                          |
|   - Greeting                                                       |
| - User Form                                                        |
|   - First Name                                                     |
|   - Last Name                                                      |
|   - Email                                                          |
+--------------------------------------------------------------------+
```

```markui
+--[Tree]--[Config]--[Diagnostics]--[Discovery]--[[Command Catalog]]--+
| Given                                                              |
| - I am on the {page} page                                          |
|                                                                    |
| When                                                               |
| - I tap {control}                                                  |
| - I enter {value} into {control}                                   |
| - I select {value} from {control}                                  |
|                                                                    |
| Then                                                               |
| - {control} should contain {value}                                 |
| - {control} should be visible                                      |
+--------------------------------------------------------------------+
```

## Recent Folders

`Open` is a split action:

- Main `Open` opens the folder picker.
- Dropdown opens recent folders.

```markui
+--- Recent Folders ------------------------------+
| Brinell.Maui.Uat.Tests                          |
| E:\repos\Private\WairOfDots\Brinell\testsnew... |
| C:\work\mobile-uat                              |
| D:\scratch\presenter-demo                       |
+-------------------------------------------------+
```

Rules:

- Persist the last 10 opened folders.
- Opening a folder moves it to the top.
- Duplicate paths are removed.
- Missing folders are skipped on startup.
- Display folder name first; expose full path through tooltip or automation text.
- Empty recent list should be friendly and non-blocking.

Suggested settings:

```text
PresenterUserSettings
  LastOpenedFolder
  RecentFolders[10]
```

Startup folder loading:

1. Try `LastOpenedFolder`.
2. Try first existing recent folder.
3. Fall back to the default sample workspace.
4. If a recent folder loads with validation errors, keep it selected and show diagnostics.

## AUT Placement

When Presenter launches the AUT, it should try to place it beside Presenter.

Preferred behavior:

- Presenter left, AUT right.
- Same monitor when there is enough space.
- On small screens, keep Presenter visible and offset AUT right/down.
- Placement failure does not fail the UAT run.

Diagnostics:

```text
AUT placement:
Presenter bounds: x=0 y=0 w=620 h=900
Requested AUT bounds: x=640 y=0 w=900 h=900
Result: moved
```

If unsupported:

```text
AUT placement:
Result: not supported
```

## Automation Surface

Required or recommended automation names:

- `OpenButton`
- `OpenRecentButton`
- `RecentFoldersText`
- `ReloadButton`
- `ValidateButton`
- `RunButton`
- `StopButton`
- `NextButton`
- `DelayMillisecondsInput`
- `DelayMillisecondsLabel`
- `WorkspaceSummaryLabel`
- `WorkspaceTreeText`
- `AllWorkspaceTreeText`
- `SelectionDetailsText`
- `StatusSummaryLabel`
- `DiagnosticsText`
- `ExecutionTimingText`
- `RunScopeText`
- `AutPlacementText`

Tree row toggle buttons should have stable automation IDs derived from node kind and name.

## UI Tests

Required Presenter UI coverage:

1. Tree shows Markdown only.
2. Tree nodes collapse and expand.
3. Selecting a collapsed parent runs descendant scenarios.
4. Recent folders keep the last 10 and move duplicates to the top.
5. Last opened folder loads on startup, with missing-folder fallback.
6. `Run` honors delay wall-clock time.
7. Automatic run exposes timing diagnostics.
8. Automatic run shows step-name status, not `Waiting xxx ms`.
9. `Next` runs exactly one step and ignores automatic delay.
10. Selected scenario scope runs only that scenario.
11. AUT placement diagnostics report `moved`, `not supported`, or a readable failure.
12. Status message is visually below the tree and above `Selection`.

## Acceptance

- Only this active `10` document defines current Presenter UI direction.
- The archived folder contains the previous `10`, `10a`, `10b`, `10c`, and `10d` drafts.
- Top actions are `Open`, `Reload`, and `Validate`.
- `Open` has an adjacent recent-folder dropdown.
- Last opened workspace is restored automatically when possible.
- Tree shows Markdown files and containing folders only.
- Tree nodes can collapse and expand without losing child model state.
- `Run` executes automatic mode for selected runnable scope.
- `Next` executes one step.
- Delay is compact, labeled in `ms`, and honored between automatic steps.
- Visible status is below the tree and shows step names or compact pass/fail text.
- Timing details stay in diagnostics and automation text.
- AUT placement is best effort and diagnostic-rich.
- Presenter tests cover tree, recents, pacing, run scope, message placement, and AUT placement.
