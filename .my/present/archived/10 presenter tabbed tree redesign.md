# Brinell Presenter Tabbed Tree Redesign

This is the current layout target for `Brinell.Presenter`.

The Presenter is expected to sit next to the application under test, often on the left side of a laptop screen. The UI should be narrow, fast to scan, and driven by the loaded workspace tree.

## Layout Goals

- Top actions only show `Open`, `Reload`, and `Validate`.
- The loaded workspace name is not shown as a separate top label.
- The loaded folder appears as the root of the tree.
- The run area contains one `Run` button, `Stop`, `Next`, execution mode, and delay.
- The run area does not show a `Ready` line.
- The run area does not show a progress bar or `3 / 7 steps` text.
- The main content uses tabs: `Tree`, `Config`, `Diagnostics`, `Discovery`, and `Command Catalog`.
- The tree mirrors the selected folder first, then child folders and files.
- UAT Markdown structure is nested under each `.uat.md` file.
- Tree rows show a status/type icon and a name. They do not repeat status text.
- The bottom of the tree tab shows a `Selection` expander with details for the selected node.

## Tree Hierarchy

The tree is not a separate test-only hierarchy. It starts from the selected workspace folder on disk.

Expected node types:

- `Folder`
- `File`
- `MarkdownFile`
- `WorkflowConfig`
- `Suite`
- `Scenario`
- `Step`

Not needed as tree node types for the first version:

- `Diagnostic`
- `Page`
- `Control`
- `Command`

Those details still belong in the diagnostics, discovery, and command catalog tabs.

## Run Behavior

The `Run` button starts at the selected tree node and only runs runnable items below that node.

| Selected node | Run scope |
| --- | --- |
| Workspace folder | All runnable UAT Markdown files in the workspace |
| Child folder | Runnable UAT Markdown files inside that folder |
| UAT Markdown file | Scenarios in that file |
| Suite | Scenarios in that suite |
| Scenario | That scenario |
| Step | The owning scenario, until step-level resume is supported |
| Workflow config or non-UAT file | No run target |

This keeps the mental model simple: select the part of the workspace you care about, then press `Run`.

## Icon Legend

Action icons:

- `#1`: folder open.
- `#2`: refresh.
- `#3`: check circle.
- `#4`: play.
- `#5`: stop square.
- `#6`: step forward.

Tree and status icons:

- `#10`: waiting.
- `#11`: running.
- `#12`: passed.
- `#13`: failed.
- `#14`: skipped.
- `#15`: canceled.
- `#20`: folder.
- `#21`: markdown file.
- `#22`: workflow config file.
- `#23`: normal file.
- `#24`: suite.
- `#25`: scenario.
- `#26`: step.

Tooltips and automation names should expose the full status text. The visible rows should stay compact.

## Main Screen

```markui
+--- Brinell Presenter --------------------------------------+
| [#1 Open] [#2 Reload] [#3 Validate]                         |
|                                                            |
| +--- Run --------------------------------------------------+ |
| | [#4 Run] [#5 Stop] [#6 Next]                             | |
| | Mode <Step v>   Delay [- 250 +] ms                      | |
| +----------------------------------------------------------+ |
|                                                            |
| +--[[Tree]]--[Config]--[Diagnostics]--[Discovery]--[Command Catalog]--+
| |                                                          | |
| | - #20 Brinell.Maui.Uat.Tests                            | |
| |   - #22 uat.config.md                                   | |
| |   - #20 Scenarios                                       | |
| |     - #21 main-page-greeting.uat.md                     | |
| |       - #24 MAUI Main Page Greeting                     | |
| |         - #11 Greeting appears when a name is entered   | |
| |           - #12 Given I am on the Main page             | |
| |           - #12 When I clear Name                       | |
| |           - #11 And I enter "Alice" into Name           | |
| |           - #10 And I tap Greet                         | |
| |           - #10 Then Greeting should contain "Hello"    | |
| |     - #21 main-page-validation.uat.md                   | |
| |       - #24 MAUI Main Page Greeting Validation          | |
| |         - #10 Empty name shows validation message       | |
| |   - #20 ExpectedFailures                                | |
| |     - #21 main-page-missing-control.uat.md              | |
| |       - #24 MAUI Missing Control Diagnostics            | |
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

## Diagnostics Tab

```markui
+--[Tree]--[Config]--[[Diagnostics]]--[Discovery]--[Command Catalog]--+
| #12 Config ok                                                       |
| #12 Parse ok                                                        |
| #12 Bind ok                                                         |
| #13 AppPath missing: ...                                            |
+--------------------------------------------------------------------+
```

## Discovery Tab

```markui
+--[Tree]--[Config]--[Diagnostics]--[[Discovery]]--[Command Catalog]--+
| Pages                                                              |
| - Main                                                             |
|   - #26 Name                                                       |
|   - #26 Greet                                                      |
|   - #26 Greeting                                                   |
| - User Form                                                        |
|   - #26 First Name                                                 |
|   - #26 Last Name                                                  |
|   - #26 Email                                                      |
+--------------------------------------------------------------------+
```

## Command Catalog Tab

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

## Implementation Notes

Presenter should keep one selected node model:

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
  Children
  Details
```

The first implementation can render the tree with a flattened, indented list if the MAUI stack does not provide a native tree control. The model should still be hierarchical so a later native tree control or virtualized tree can use the same data.

## Acceptance

- `02`, old `10`, and `10a` are archived.
- Fresh `10` is the single current Presenter layout document.
- Presenter top actions are `Open`, `Reload`, and `Validate`.
- Presenter has one `Run` action.
- `Run` executes the selected node and runnable descendants.
- The tree starts with the selected folder and mirrors child folders/files.
- UAT suites, scenarios, and steps are nested below their `.uat.md` files.
- Tabs exist for `Tree`, `Config`, `Diagnostics`, `Discovery`, and `Command Catalog`.
- The tree tab has a bottom `Selection` expander.
