# Presenter Collapsible Tree, Recents, And AUT Placement

This is a follow-up to `10 presenter tabbed tree redesign.md`, `10a presenter run controls and execution pacing.md`, and `10b presenter run delay investigation and ui tests.md`.

The next Presenter slice should improve workspace navigation and startup ergonomics.

## Goals

- Tree nodes are collapsible.
- The tree only shows Markdown files and folders that contain Markdown files.
- `Open` has a small dropdown button next to it.
- The dropdown shows the last 10 opened folders.
- The last opened folder is loaded automatically on startup.
- The application under test should open next to the Presenter when possible, not on top of it.

## Main Toolbar

`Open` becomes a split action: the main button opens the folder picker, and the dropdown opens recent folders.

```markui
+--- Brinell Presenter --------------------------------------+
| [#1 Open][v] [#2 Reload] [#3 Validate]                      |
|                                                            |
| +--- Run --------------------------------------------------+ |
| | [#4 Run] [#5 Stop] [#6 Next]   Delay <250__> ms          | |
| +----------------------------------------------------------+ |
+------------------------------------------------------------+
```

Open dropdown:

```markui
+--- Recent Folders ------------------------------+
| Brinell.Maui.Uat.Tests                          |
| E:\repos\Private\WairOfDots\Brinell\testsnew... |
| C:\work\mobile-uat                              |
| D:\scratch\presenter-demo                       |
+-------------------------------------------------+
```

## Collapsible Tree

The tree should represent the Markdown workspace, not every file on disk.

Visible node types:

- Workspace folder
- Child folders that contain at least one Markdown file below them
- Markdown files, including `.uat.md`, `uat.config.md`, and normal `.md`
- UAT suite nodes under `.uat.md`
- UAT scenario nodes under suites
- UAT step nodes under scenarios

Do not show:

- `.dll`
- `.json`
- `.cs`
- `.csproj`
- `bin`
- `obj`
- any non-Markdown file

Expected collapsed view:

```markui
+--[[Tree]]--[Config]--[Diagnostics]--[Discovery]--[Command Catalog]--+
| [v] Brinell.Maui.Uat.Tests                                          |
|   [ ] uat.config.md                                                 |
|   [>] Scenarios                                                     |
|   [>] ExpectedFailures                                              |
|                                                                     |
| [Selection v]                                                       |
+---------------------------------------------------------------------+
```

Expected expanded view:

```markui
+--[[Tree]]--[Config]--[Diagnostics]--[Discovery]--[Command Catalog]--+
| [v] Brinell.Maui.Uat.Tests                                          |
|   [ ] uat.config.md                                                 |
|   [v] Scenarios                                                     |
|     [v] main-page-greeting.uat.md                                   |
|       [v] MAUI Main Page Greeting                                   |
|         [>] Greeting appears when a name is entered                 |
|     [ ] main-page-validation.uat.md                                 |
|     [ ] user-form-basic-input.uat.md                                |
|   [>] ExpectedFailures                                              |
|                                                                     |
| [Selection v]                                                       |
+---------------------------------------------------------------------+
```

## Collapse Behavior

- Workspace starts expanded.
- First-level child folders start collapsed unless they contain only one Markdown path.
- Markdown files can expand when they have parsed UAT structure.
- Suite and scenario nodes can expand.
- Step nodes cannot expand.
- Collapsed parents keep their own status summary.
- If a child step fails under a collapsed parent, the parent icon should reflect failure.
- Selecting a collapsed parent still runs the runnable descendants below it.

Implementation note: keep the hierarchical model as the source of truth and rebuild the flattened visible list from expansion state. Do not permanently remove children from the model when a node is collapsed.

Suggested node state:

```text
WorkspaceNode
  Name
  NodeType
  Status
  IsExpanded
  CanExpand
  Children
```

## Markdown-Only Filtering

The loader should enumerate Markdown files only:

- `*.md`
- `*.uat.md` is a Markdown file with UAT structure
- `uat.config.md` remains visible

Folders should only appear if they contain at least one visible Markdown descendant.

This keeps the Presenter tree focused on UAT authoring and avoids clutter from build outputs or source code.

## Recent Folders

Persist the last 10 opened folders.

Rules:

- Opening a folder moves it to the top of the recent list.
- Duplicate paths are removed.
- Missing folders are skipped during startup and can be removed from the list.
- Recent folder display text should prefer the folder name, with full path available in tooltip/automation text.
- The dropdown should be disabled or empty-state friendly when no recent folders exist.

Suggested storage:

```text
PresenterUserSettings
  LastOpenedFolder
  RecentFolders[10]
```

Settings can initially be stored as a small JSON file under a Presenter-owned app data folder.

## Startup Folder Loading

On startup:

1. Try to load `LastOpenedFolder`.
2. If it does not exist, try the first existing recent folder.
3. If no recent folder exists, load the current default sample workspace.
4. If loading a recent folder fails validation, show the diagnostics tab but keep the folder selected.

This should make the Presenter reopen where the user was last working.

## AUT Placement

When the Presenter launches the application under test, it should try to position the AUT next to the Presenter window.

Preferred placement:

- Presenter on the left.
- AUT on the right.
- Both visible on the same monitor if there is enough space.
- If the screen is too small, keep the Presenter visible and place the AUT offset to the right/down.
- Do not obscure the Presenter as the default outcome.

Best-effort rules:

- Window placement failure should not fail the UAT run.
- Record placement attempt, target bounds, and result in diagnostics.
- If platform APIs cannot move the AUT window yet, expose a clear diagnostic such as `AUT placement: not supported`.
- Later, add user preferences for left/right monitor and window sizes.

Suggested diagnostic lines:

```text
AUT placement:
Presenter bounds: x=0 y=0 w=620 h=900
Requested AUT bounds: x=640 y=0 w=900 h=900
Result: moved
```

## UI Test Requirements

### Test 1: Tree Shows Markdown Only

Setup:

- Start Presenter with the sample MAUI UAT workspace.

Assertions:

- Tree contains `uat.config.md`.
- Tree contains `main-page-greeting.uat.md`.
- Tree contains `main-page-validation.uat.md`.
- Tree does not contain `.csproj`.
- Tree does not contain `.dll`.
- Tree does not contain `bin`.
- Tree does not contain `obj`.

### Test 2: Tree Nodes Collapse And Expand

Setup:

- Start Presenter.
- Ensure `Scenarios` is visible.

Assertions:

- Collapse `Scenarios`.
- Scenario file nodes under `Scenarios` are no longer visible.
- Expand `Scenarios`.
- Scenario file nodes become visible again.
- Selection and run behavior still work after re-expanding.

### Test 3: Collapsed Parent Runs Descendants

Setup:

- Collapse `Scenarios`.
- Select the collapsed `Scenarios` node.
- Set delay to `0`.
- Press `Run`.

Assertions:

- Runnable scenarios below `Scenarios` run.
- Diagnostics show selected node kind `Folder`.
- Diagnostics show scenario count greater than `1`.

### Test 4: Open Recent Dropdown Lists Last 10

Setup:

- Open more than 10 folders through the Presenter settings/service test seam.

Assertions:

- Dropdown shows 10 items.
- Most recently opened folder appears first.
- Reopening an older folder moves it to the top.
- Duplicate paths are not shown.

### Test 5: Last Folder Loads On Startup

Setup:

- Persist a valid `LastOpenedFolder`.
- Start Presenter.

Assertions:

- The persisted folder is loaded without pressing `Open`.
- Workspace summary corresponds to that folder.
- If the saved folder is missing, Presenter falls back to the next existing recent folder or default sample workspace.

### Test 6: AUT Opens Beside Presenter When Supported

Setup:

- Start Presenter.
- Select a runnable scenario.
- Press `Run`.

Assertions:

- Diagnostics contain `AUT placement`.
- Diagnostics show either `Result: moved` or `Result: not supported`.
- If moved is supported, AUT bounds do not overlap the Presenter bounds.
- Presenter remains visible after AUT launch.

## Automation Surface Needed

Add or expose:

- `OpenRecentButton`
- `RecentFoldersList`
- `WorkspaceTree` row expand/collapse by visible node text
- `WorkspaceTreeText` should reflect only currently visible rows
- `AllWorkspaceTreeText` may be added as a hidden diagnostic if tests need full model visibility
- `AutPlacementText` hidden automation label or diagnostics section

Recommended page-object additions:

```csharp
[UatName("Open Recent")]
public Button<PresenterPage> OpenRecentButton => Button("OpenRecentButton");

[UatName("Recent Folders")]
public Label<PresenterPage> RecentFolders => Label("RecentFoldersText");

[UatName("AUT Placement")]
public Label<PresenterPage> AutPlacement => Label("AutPlacementText");
```

## Acceptance

- Tree nodes can collapse and expand.
- Collapsed state changes the visible tree rows.
- The tree only shows Markdown files and useful containing folders.
- `Open` has an adjacent recent-folder dropdown button.
- The recent list stores and shows the last 10 opened folders.
- The last opened existing folder loads automatically on startup.
- Missing recent folders do not block startup.
- AUT launch attempts best-effort side-by-side placement.
- UI tests cover tree filtering, collapse/expand, recents, startup restore, and AUT placement diagnostics.
