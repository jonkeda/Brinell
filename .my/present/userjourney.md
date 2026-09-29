# Brinell Presenter User Journeys

Status: **not run**; updated 2026-09-29 for the new UI ([presenter-update.md](presenter-update.md): the step list,
the moves, the modes and the docked bar). A manual checklist; item 6 of [plan.md](plan.md).

Check a journey only after all its subtasks pass. Use `[!]` for a failed check
and `[-]` when a check does not apply; add a defect reference or reason beside it.

## PR01 Start Presenter and restore a workspace

- [ ] **Journey passed**
  - [ ] **PR01.1** Starting Presenter with no saved settings loads the bundled sample workspace without asking anything.
  - [ ] **PR01.2** Starting Presenter after a folder was opened earlier reopens that same folder automatically.
  - [ ] **PR01.3** When the last opened folder no longer exists, the first still-existing recent folder opens instead.
  - [ ] **PR01.4** Recent folders that no longer exist are dropped and are not offered again after a restart.
  - [ ] **PR01.5** When neither a saved folder nor the sample workspace can be found, the status explains the sample workspace was not found and the summary reports no workspace config.
  - [ ] **PR01.6** A restored workspace that has configuration errors still opens, in Workspace mode, and its diagnostics are shown instead of the tree.
  - [ ] **PR01.7** The Presenter window opens as a narrow full-height bar on the side it was last docked to (left the first time), leaving the rest of the screen for the application under test.

## PR02 Open a workspace folder

- [ ] **Journey passed**
  - [ ] **PR02.1** `Open` shows a folder picker.
  - [ ] **PR02.2** Choosing a folder that contains UAT Markdown loads it, and the summary shows target, fixture, app state, file count and scenario count.
  - [ ] **PR02.3** After a clean load the status reports the file and scenario counts and that config, parse and bind are ok.
  - [ ] **PR02.4** Canceling the picker keeps the current workspace loaded and reports that the open was canceled.
  - [ ] **PR02.5** Canceling the picker when no workspace is loaded reports that no workspace was selected.
  - [ ] **PR02.6** Opening a folder without `uat.config.md` still loads it, reports config missing, and opens the diagnostics view.
  - [ ] **PR02.7** Opening a folder with no `.uat.md` files reports that no UAT files were found.
  - [ ] **PR02.8** `Reload` reloads and checks the current folder, in either mode, and is unavailable until a workspace is loaded.

## PR03 Reopen from recent folders

- [ ] **Journey passed**
  - [ ] **PR03.1** The control beside `Open` shows and hides the recent folder list, and its arrow reflects the open state.
  - [ ] **PR03.2** A folder opened through the picker appears at the top of the recent list, shown by folder name with its full path available.
  - [ ] **PR03.3** Reopening a folder already in the list moves it to the top without creating a duplicate entry.
  - [ ] **PR03.4** The list keeps at most the ten most recently opened folders.
  - [ ] **PR03.5** Choosing a recent folder loads it and closes the recent list.
  - [ ] **PR03.6** Choosing a recent folder that has since been deleted reports that it was not found and refreshes the list.
  - [ ] **PR03.7** With no history, the recent list shows a friendly empty message rather than blocking the user.

## PR04 Browse the Markdown workspace tree

- [ ] **Journey passed**
  - [ ] **PR04.1** The tree root is the opened workspace folder.
  - [ ] **PR04.2** Markdown files in the workspace are listed, including `uat.config.md` and plain `.md` files.
  - [ ] **PR04.3** Non-Markdown files and anything under `bin` or `obj` never appear in the tree.
  - [ ] **PR04.4** Each `.feature.md` file with parsed content shows its suite and its scenarios; the steps are in the Steps list, not the tree.
  - [ ] **PR04.5** The workflow config appears first, folders before Markdown files, and an `ExpectedFailures` folder appears last.
  - [ ] **PR04.6** Scenario rows, and the step rows of the Steps list, start in the waiting state.
  - [ ] **PR04.7** A Markdown file that could not be parsed or bound shows that diagnostics are available instead of scenarios.
  - [ ] **PR04.8** Loading a workspace selects the first scenario and expands its parents so the selection is visible.

## PR05 Expand and collapse tree nodes

- [ ] **Journey passed**
  - [ ] **PR05.1** The workspace root starts expanded.
  - [ ] **PR05.2** A top-level folder holding more than one Markdown path starts collapsed; one holding a single Markdown path starts expanded.
  - [ ] **PR05.3** The control on a row expands and collapses that row, and its arrow reflects the state.
  - [ ] **PR05.4** Scenario rows have no working expand control.
  - [ ] **PR05.5** Collapsing a parent hides its descendants, and re-expanding shows the same rows with their previous statuses.
  - [ ] **PR05.6** A collapsed parent whose descendant failed shows a failed state on the parent row.
  - [ ] **PR05.7** The tree uses the available height and scrolls internally, keeping the status message and selection below it rather than overlaid.

## PR06 Inspect the selection details

- [ ] **Journey passed**
  - [ ] **PR06.1** The selection panel sits below the tree and below the status message, and its control expands and collapses it.
  - [ ] **PR06.2** Selecting any row shows its type and name.
  - [ ] **PR06.3** A selected row inside the workspace shows its path relative to the workspace folder.
  - [ ] **PR06.4** Selecting a scenario also shows its suite, scenario status, step count and any tags.
  - [ ] **PR06.5** The Selection panel is only in Workspace mode, and starts collapsed.
  - [ ] **PR06.6** The details update as a run progresses, following the current status.
  - [ ] **PR06.7** With nothing selected the panel reports that there is no selection.

## PR07 Read configuration, diagnostics, discovery and commands

- [ ] **Journey passed**
  - [ ] **PR07.1** In Workspace mode the Tree, Config, Diagnostics, Discovery and Command Catalog views are all reachable and the current one is marked; Present mode shows only the tree and the steps.
  - [ ] **PR07.2** Config shows the config path, target, fixture, app path, working directory, and whether each resolved path exists.
  - [ ] **PR07.3** Config lists every registered assembly and whether it was found.
  - [ ] **PR07.4** Diagnostics lists parse and bind results per UAT file, with the message for each problem.
  - [ ] **PR07.5** Discovery shows the configured target, fixture and registered assemblies, or states that `uat.config.md` was not found.
  - [ ] **PR07.6** The command catalog lists the available step phrases, grouped by keyword, with the command each maps to.
  - [ ] **PR07.7** Switching between views does not lose the tree selection or an active run.

## PR08 Set the execution delay

- [ ] **Journey passed**
  - [ ] **PR08.1** The delay input shows a default value with a visible `ms` label beside it.
  - [ ] **PR08.2** The input accepts digits only and at most five characters.
  - [ ] **PR08.3** A delay entered before pressing `Run` is the delay actually applied during that run.
  - [ ] **PR08.4** Changing the delay while a run is in progress does not change the pacing of that run.
  - [ ] **PR08.5** An empty or non-numeric delay runs without waiting between steps rather than failing.
  - [ ] **PR08.6** The effective delay used by a run is reported in the diagnostics.

## PR09 Run a scenario automatically

- [ ] **Journey passed**
  - [ ] **PR09.1** `Run` with a scenario selected starts execution and the status names what is starting.
  - [ ] **PR09.2** Each step is shown as running and then passed, in order, and the tree icons follow along.
  - [ ] **PR09.3** While running, the status shows the current step text, not a waiting or timing message.
  - [ ] **PR09.4** The delay is applied between steps only, so a seven-step scenario at 1000 ms takes at least about six seconds.
  - [ ] **PR09.5** The scenario row shows passed once all its steps pass, and the status reports the passed scenario count.
  - [ ] **PR09.6** Diagnostics record the run scope and a start and completion time for each step.
  - [ ] **PR09.7** Pressing `Run` with no workspace loaded reports that no workspace is loaded.
  - [ ] **PR09.8** Running the same scenario again resets its steps to waiting before execution starts.

## PR10 Choose the run scope from the tree selection

- [ ] **Journey passed**
  - [ ] **PR10.1** Running with the workspace root selected runs every runnable scenario in the workspace.
  - [ ] **PR10.2** Running with a child folder selected runs only the scenarios below that folder.
  - [ ] **PR10.3** Running with a Markdown file selected runs only the scenarios in that file.
  - [ ] **PR10.4** Running with a suite selected runs only the scenarios in that suite.
  - [ ] **PR10.5** Running with a scenario selected runs only that scenario and leaves the others waiting.
  - [ ] **PR10.6** Run until here on a step of the Steps list runs its scenario from the first step and pauses before that step.
  - [ ] **PR10.7** Running with the workflow config or a Markdown file without scenarios selected reports that there is nothing runnable below the selection.
  - [ ] **PR10.8** The reported run scope matches the selected node, its kind, and the scenario and step counts actually run.

## PR11 Step through a scenario

- [ ] **Journey passed**
  - [ ] **PR11.1** `Next` with a scenario selected and no run on starts a session and pauses before the first step, running nothing; the step is highlighted and the status says what is next.
  - [ ] **PR11.2** `Next` again runs exactly the highlighted step and pauses before the following one.
  - [ ] **PR11.3** At a `## Step:` call's row, `Next` runs the call whole and pauses after it; `Into` goes inside it without running anything; inside, `Out` runs the rest of it and pauses after it.
  - [ ] **PR11.4** `Next`, `Into` and `Out` do not apply the automatic delay; `Run` from a pause goes on with the delay.
  - [ ] **PR11.5** Completing the last step marks the scenario passed and ends the session.
  - [ ] **PR11.6** A step that fails marks the remaining steps skipped, marks the scenario failed, and shows the failure message, also on hover over the step.
  - [ ] **PR11.7** `Next` with nothing runnable selected reports that there is no runnable scenario.
  - [ ] **PR11.8** `Next` is unavailable until a workspace is loaded; `Into` only at a call, `Out` only inside one.
  - [ ] **PR11.9** The keys do the same: Space, Right and Page Down step, Down goes in, Up goes out, F5 runs, Escape stops, Ctrl+R restarts; a presentation clicker's forward button steps. The keys do nothing in the Delay box.
  - [ ] **PR11.10** `Restart` starts the scenario again, paused before its first step.
## PR12 Stop an active run

- [ ] **Journey passed**
  - [ ] **PR12.1** `Stop` during an automatic run halts it before the next step begins.
  - [ ] **PR12.2** The status reports that the run was stopped.
  - [ ] **PR12.3** A step that was running is marked canceled and steps not yet reached are not marked passed.
  - [ ] **PR12.4** Scenarios later in the run scope are not started after a stop.
  - [ ] **PR12.5** `Stop` while paused ends the run, and the following `Next` starts a new one, paused before the first step.
  - [ ] **PR12.6** `Stop` when nothing is running leaves the workspace usable and does not raise an error.
  - [ ] **PR12.7** After stopping, `Run` can be started again without restarting Presenter.

## PR13 Handle a failing scenario

- [ ] **Journey passed**
  - [ ] **PR13.1** The failing step is marked failed and is visually distinguishable from a passed step.
  - [ ] **PR13.2** The steps after the failure are marked skipped rather than left waiting.
  - [ ] **PR13.3** The scenario is marked failed and its parent rows show the failure.
  - [ ] **PR13.4** The status names the failed scenario and shows the failure message.
  - [ ] **PR13.5** Diagnostics list the result of each executed step together with the failure message.
  - [ ] **PR13.6** A multi-scenario run stops at the failure and reports how many scenarios passed before it.
  - [ ] **PR13.7** Scenarios under `ExpectedFailures` fail in the same visible way and are listed last in the tree.
  - [ ] **PR13.8** Re-running after a failure clears the previous statuses before the new run starts.

## PR14 Handle an unusable workspace configuration

- [ ] **Journey passed**
  - [ ] **PR14.1** A workspace without `uat.config.md` shows the problem on load, and attempting to run reports that the config was not found.
  - [ ] **PR14.2** A missing or unsupported runtime target is reported with the list of supported targets.
  - [ ] **PR14.3** A missing runtime fixture is reported as an error.
  - [ ] **PR14.4** A missing app path, or one pointing at a file that does not exist, shows the app as missing and reports the resolved path when running.
  - [ ] **PR14.5** A missing or unregistered assembly is reported with a message explaining that the project must be built first.
  - [ ] **PR14.6** A fixture that cannot be found in the pages assembly is reported by name.
  - [ ] **PR14.7** After a failed start, Presenter remains usable: the config can be fixed, reloaded and run without restarting.

## PR15 Launch and place the application under test

- [ ] **Journey passed**
  - [ ] **PR15.1** Starting a run on a MAUI workspace launches the application under test.
  - [ ] **PR15.2** Presenter stays visible and the application is placed beside it, on the other side of the screen, rather than on top of it; also for a WPF, WinForms or browser target.
  - [ ] **PR15.3** The placement outcome is reported in the diagnostics.
  - [ ] **PR15.4** A run whose app opens no window (a headless browser) is not placed, and still runs the scenario.
  - [ ] **PR15.5** A placement failure is reported readably and does not fail the run.
  - [ ] **PR15.6** Finishing or stopping a run closes the application under test, and the next run starts a fresh instance.

## PR16 Present and Workspace modes, and the dock

- [ ] **Journey passed**
  - [ ] **PR16.1** A workspace that loads cleanly opens in Present mode: the mode buttons, Reload, Dock, theme, the run toolbar, the Scenarios tree, the Steps list and the status; nothing else.
  - [ ] **PR16.2** Workspace mode adds Open and the recent folders, the build choices and Build, the tabs and the Selection panel; the current mode's button is filled.
  - [ ] **PR16.3** A workspace that is not built opens in Workspace mode and says "Not built yet" above Build; building it and reloading shows the steps bound.
  - [ ] **PR16.4** Reloading keeps the mode, unless the workspace now has problems.
  - [ ] **PR16.5** Dock moves the bar to the other side of its monitor, full height, and the next start opens it there.

## PR17 Pick a scenario, read its steps, open the preview

- [ ] **Journey passed**
  - [ ] **PR17.1** Picking a scenario in the tree fills the Steps list, and its header names the scenario.
  - [ ] **PR17.2** Each step shows its keyword and text; a `## Step:` call is a bold row with its steps indented under it.
  - [ ] **PR17.3** Hovering a truncated row shows its full text; after a failure, also the message.
  - [ ] **PR17.4** Both sections collapse and expand; a collapsed one gives its room to the other.
  - [ ] **PR17.5** The preview button opens the Scenario Preview in a window of its own, the same as in the IDEs, and it follows the picked scenario and the run.
  - [ ] **PR17.6** While a run is paused, the preview's paused bar shows the step, and its buttons move the run as the toolbar does.

## PR18 Breakpoints, run until here, run from here

- [ ] **Journey passed**
  - [ ] **PR18.1** A right-click on a step offers Run until here, Run from here and Toggle breakpoint.
  - [ ] **PR18.2** Toggle breakpoint marks the step with a red dot; `Run` pauses before it.
  - [ ] **PR18.3** A breakpoint on a `## Step:` call pauses at the call, before its first step.
  - [ ] **PR18.4** A breakpoint set in Visual Studio or VS Code shows in Presenter and stops `Run` there; one set in Presenter shows in the IDEs.
  - [ ] **PR18.5** Run until here runs on from where the run is (or starts one) and pauses before the step.
  - [ ] **PR18.6** Run from here is off, and says why, unless a run is paused before the step; when on, it skips the steps in between (marked skipped) and runs on.
