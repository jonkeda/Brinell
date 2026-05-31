# Brinell Presenter Tabbed Tree Redesign

This document defines the next Presenter UI redesign after the first workspace/config pass.

The goal is to make the left-side Presenter more compact and navigable by replacing stacked sections with a tab control and a single tree-driven workspace view.

## Main Changes

Top area:

- Show only the workspace action buttons: `Open`, `Reload`, `Validate`.
- Remove the separate workspace label/name area from the top.
- The loaded folder name should live in the tree root instead.

Run area:

- Keep one compact run panel.
- Include run buttons, mode, delay, and progress.
- Do not show a separate ready line.
- Do not show progress in the run panel.
- Use one `Run` button. It runs from the selected tree node and only executes the runnable items below that node in the hierarchy.

Main content:

- Add a tab control.
- First tab is a tree view of the loaded workspace and UAT structure.
- Other tabs expose config, diagnostics, discovery, and command catalog.

Run behavior:

- If a folder is selected, run runnable UAT files below that folder.
- If a UAT Markdown file is selected, run scenarios in that file.
- If a suite is selected, run scenarios in that suite.
- If a scenario is selected, run that scenario.
- If a step is selected, run from that step only if step-level execution is supported; otherwise select the scenario.

## Status Icons

Tree rows should show an icon for status and then the node name. Do not repeat status text in every row.

Icon meanings:

- `#8`: passed.
- `#9`: running.
- `#10`: waiting.
- `#11`: failed.
- `#12`: skipped.
- `#13`: canceled.
- `#14`: folder.
- `#15`: markdown file.
- `#16`: workflow/config file.
- `#17`: suite.
- `#18`: scenario.
- `#19`: step.

Status meaning should be available through tooltip and accessibility text.

## Proposed MarkUI

```markui
+--- Brinell Presenter --------------------------------------+
| [#1 Open] [#2 Reload] [#3 Validate]                         |
|                                                            |
| +--- Run --------------------------------------------------+ |
| | [#4 Run] [#6 Stop] [#7 Next]                             | |
| | Mode <Step v>   Delay [- 250 +] ms                      | |
| +----------------------------------------------------------+ |
|                                                            |
| +--[[Tree]]--[Config]--[Diagnostics]--[Discovery]--[Catalog]--+
| |                                                          | |
| | - #14 Brinell.Maui.Uat.Tests                            | |
| |   - #16 uat.config.md                                   | |
| |   - #14 Scenarios                                       | |
| |     - #15 main-page-greeting.uat.md                     | |
| |       - #17 MAUI Main Page Greeting                     | |
| |         - #8 Greeting appears when a name is entered    | |
| |           - #8 Given I am on the Main page              | |
| |           - #8 When I clear Name                        | |
| |           - #9 And I enter "Alice" into Name            | |
| |           - #10 And I tap Greet                         | |
| |           - #10 Then Greeting should contain "Hello..." | |
| |     - #15 main-page-validation.uat.md                   | |
| |       - #17 MAUI Main Page Greeting Validation          | |
| |         - #10 Empty name shows validation message       | |
| |     - #15 user-form-basic-input.uat.md                  | |
| |       - #17 MAUI User Form Basic Input                  | |
| |         - #10 User can enter basic profile information  | |
| |   - #14 ExpectedFailures                                | |
| |     - #15 main-page-missing-control.uat.md              | |
| |       - #17 MAUI Missing Control Diagnostics            | |
| |         - #10 Missing control reports available controls| |
| |                                                          | |
| | [Selection ^]                                            | |
| | Type: Scenario                                           | |
| | Name: Greeting appears when a name is entered            | |
| | File: main-page-greeting.uat.md                          | |
| | Status: Running                                          | |
| +----------------------------------------------------------+ |
+------------------------------------------------------------+
```

## Config Tab

```markui
+--[Tree]--[[Config]]--[Diagnostics]--[Discovery]--[Catalog]--+
| Runtime                                                      |
| Target: MAUI                                                |
| Fixture: Appium                                             |
| AppPath: ../../samples/Brinell.Samples.Maui.App/...          |
| WorkingDirectory: ../..                                     |
|                                                              |
| Assemblies                                                   |
| #8 Pages      ../Brinell.Maui.UITests/bin/...                |
| #8 Controls   ../../srcnew/Brinell.Maui/bin/...              |
| #8 Commands   ../../srcnew/Brinell.Uat/bin/...               |
+--------------------------------------------------------------+
```

## Diagnostics Tab

```markui
+--[Tree]--[Config]--[[Diagnostics]]--[Discovery]--[Catalog]--+
| #8 Config ok                                                 |
| #8 Parse ok                                                  |
| #8 Bind ok                                                   |
| #11 AppPath missing: ...                                     |
|                                                              |
| [Selected Diagnostic ^]                                      |
| Code: Runtime AppPath                                        |
| Message: Runtime AppPath is required for local execution.    |
+--------------------------------------------------------------+
```

## Discovery Tab

```markui
+--[Tree]--[Config]--[Diagnostics]--[[Discovery]]--[Catalog]--+
| Pages                                                        |
| - Main                                                       |
|   - #19 Name                                                 |
|   - #19 Greet                                                |
|   - #19 Greeting                                             |
| - User Form                                                  |
|   - #19 First Name                                           |
|   - #19 Last Name                                            |
|   - #19 Email                                                |
+--------------------------------------------------------------+
```

## Command Catalog Tab

```markui
+--[Tree]--[Config]--[Diagnostics]--[Discovery]--[[Catalog]]--+
| Given                                                        |
| - I am on the {page} page                                    |
|                                                              |
| When                                                         |
| - I tap {control}                                            |
| - I enter {value} into {control}                             |
| - I select {value} from {control}                            |
|                                                              |
| Then                                                         |
| - {control} should contain {value}                           |
| - {control} should be visible                                |
+--------------------------------------------------------------+
```

## Implementation Notes

Use a single selected node model for the tree:

```text
WorkspaceNode
  Name
  Type
  Status
  Icon
  Children
  Details
```

Suggested node types:

- Folder.
- File.
- MarkdownFile.
- WorkflowConfig.
- Suite.
- Scenario.
- Step.

The tree tab should become the default working view. The other tabs are investigation surfaces.

The selection expander at the bottom should show details for the selected tree node and should stay in the tree tab only.

The tree should mirror the selected folder on disk first. UAT structure is nested under each `.uat.md` file. Do not create artificial top-level groups such as separate `Folders and files` or `Suites` roots.

## Acceptance

The redesign is done when:

- The top no longer shows a separate workspace name label.
- `Open`, `Reload`, and `Validate` remain at the top.
- Run controls are grouped in one compact run panel.
- The run panel has one `Run` action.
- `Run` executes the selected node and its runnable descendants.
- A tab control exists with `Tree`, `Config`, `Diagnostics`, `Discovery`, and `Command Catalog`.
- The tree mirrors the selected folder, child folders, files, workflow config, UAT Markdown files, suites, scenarios, and steps.
- Status is shown as an icon plus node name.
- Selecting a tree node updates the bottom selection expander.
