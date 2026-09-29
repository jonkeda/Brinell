# Presenter Platform Guide

`Brinell.Presenter` is a Windows desktop shell (a MAUI app) for loading and running UAT
workspaces, narrow enough to sit beside the app under test.

## Projects

- `srcnew/Brinell.Presenter`
- `testsnew/Brinell.Presenter.Bdd.Tests`: Presenter's own UAT suite

## A Minimal Run

Presenter needs three things built: Presenter, the app under test, and the UAT project that holds
the page objects. The WPF workspace is the cheapest: no Appium, no MAUI workload for the app.

```powershell
# Working directory: Brinell root
dotnet build samples\Brinell.Samples.Wpf.App\Brinell.Samples.Wpf.App.csproj
dotnet build testsnew\Brinell.Wpf.Uat.Tests\Brinell.Wpf.Uat.Tests.csproj
dotnet run --project srcnew\Brinell.Presenter\Brinell.Presenter.csproj -f net10.0-windows10.0.19041.0
```

Then, in Presenter (the toolbar is icon-only; hover for tooltips):

1. **Open** `testsnew\Brinell.Wpf.Uat.Tests`.
2. **Validate**, and fix anything the Diagnostics tab reports.
3. Select a scenario in the tree, for example *Home page is visible*.
4. **Run**, or **Next** to go one step at a time.

On first start Presenter opens the default sample workspace, which is usually not built yet:
the Diagnostics tab, not the tree, is the expected first screen.

## The Workspace

A workspace is a folder with `uat.config.md` and `.feature.md` scenario files. It is anchored on a
**UAT project and a fixture** (AD-018):

```markdown
## Runtime

| Field   | Value                             |
| ---     | ---                               |
| Project | ./Brinell.Samples.Todo.Uat.csproj |
| Fixture | TodoUatFixture                    |
```

- **Project**: Presenter asks MSBuild where the project's output goes, so the config holds no
  `bin/` paths. A bare assembly name under `## Assemblies` still works, and is shown as found by
  search.
- **Fixture**: Presenter creates it by name from the Pages assembly: a non-abstract class with a
  parameterless constructor whose name or full name is `Fixture`, or `Fixture` + `Fixture`. The
  xUnit runners take the fixture as a type argument and only check the name, so a suite can pass
  in xUnit and still name a fixture Presenter cannot find.
- **AppPath** is an optional override. The fixture finds its own app.

The **Config** tab edits the file in place: the project, a fixture picker that lists every
candidate with the reason an unusable one is unusable ("abstract", "no parameterless
constructor"), and the derived values (target, Pages assembly, app) with where each came from
and a **Pin** action that writes the value into the file. Save keeps rows it did not change
byte for byte, stays enabled while the config is invalid, and reloads the workspace keeping the
tree's expansion and selection. If the file changed on disk, Save offers **Keep mine** or
**Reload theirs**.

The run toolbar carries the session choices, stored per workspace: **Debug/Release** (for the
tests; the fixture picks the app's configuration), the **platform** (sets `APPIUM_PLATFORM`, and
offers only what the fixture declares with `[UatPlatforms]`), and **Build**, which shows
MSBuild's output verbatim. A run loads the Pages assembly into a context that is released when
the session ends, so you can rebuild without restarting Presenter.

To build the app with the tests, give the UAT project a reference that builds it without
referencing it:

```xml
<ProjectReference Include="..\..\samples\Brinell.Samples.Wpf.App\Brinell.Samples.Wpf.App.csproj"
                  ReferenceOutputAssembly="false" />
```

## The Shell

Top actions: **Open** (a split button; the dropdown lists recent folders), **Reload**,
**Validate**. Under them the run toolbar: **Run**, **Stop**, **Next**, and **Delay** in
milliseconds.

| Tab | Shows |
| --- | --- |
| Tree | a workspace summary, the Scenarios tree, the Steps list, the status message, and a Selection expander |
| Config | the config editor |
| Diagnostics | config, parse and bind results, and run timings |
| Discovery | the pages and controls the scenarios can name |
| Command Catalog | every phrase, by keyword |

Presenter checks each feature and step file as the xUnit gate and the IDEs do: against the suite's
own catalogs, loaded through `Brinell.Bdd.Workspace` from a shadow copy of its build. The
Diagnostics tab lists the gate's diagnostics, with severity and code. A suite that is not built yet
is only parsed, and Diagnostics says to build it. A file under an `ExpectedFailures` folder fails
on purpose: it is reported as "expected failure" and does not make the workspace need attention.
The config can be `uat.config.md` or, for a service suite, `bdd.config.md`.

Pick in a tree, step in a list. The **Scenarios** tree starts at the workspace folder and shows only
Markdown: folders with Markdown in them, `.md` files, and under each `.feature.md` its suite and
scenarios. `bin`, `obj` and non-Markdown files are hidden. A collapsed parent shows the worst
status of its children. The **Steps** list below it holds the picked scenario's steps with their
status; a `## Step:` call is a bold row of its own, with its steps indented under it. The step a
run is at is highlighted and kept in view. Hovering a row shows its full text, and after a failure
the message. Each section is an expander.

The button beside the Steps header opens the **Scenario Preview** the IDEs show, in a window of its
own: the whole feature file with prose, tables and Examples, bound against the suite. It follows
the picked scenario and its run: each step's result, and the step the run is at.

## Running

**Run** runs the selected node's scenarios until they finish, one fails, or **Stop** is pressed.
**Next** runs exactly one step, starting a session from the selection if there is none, and does
not wait. **Stop** cancels either.

| Selected | Runs |
| --- | --- |
| Workspace or folder | every scenario file under it |
| `.feature.md` file or suite | its scenarios |
| Scenario | that scenario |
| Config or other Markdown | nothing |

**Delay** is read once when the run starts and applied between steps, never before the first:
seven steps at 1000 ms take at least six seconds. A step is marked passed only after its command
completes. The status line under the tree shows the current step, or the result
(`Passed: 1/1 scenarios`); per-step timings are in Diagnostics.

## Supported Targets

The target comes from the fixture's base class.

| Target | App path environment |
| --- | --- |
| `MAUI` | `APPIUM_APP_PATH` |
| `WPF` | `WPF_APP_PATH` |
| `WINFORMS` | `WINFORMS_APP_PATH` |
| `BLAZOR` | `BLAZOR_APP_PATH` |
| `HTML` | `HTML_APP_PATH` |
| `STRIDE` | `STRIDE_APP_PATH` |

Presenter exports `AppPath`, when the config sets one, to the variable for the target.

For MAUI, Presenter places the app to its right, on the same monitor when there is room, by
setting `BRINELL_AUT_PLACE_RIGHT` and `BRINELL_AUT_PLACEMENT_RESULT_FILE`. A placement that
fails does not fail the run; Diagnostics shows the result.

## User Settings

`PresenterUserSettingsService` stores `LastOpenedFolder`, up to 10 `RecentFolders`, and the
per-workspace session choices. On start Presenter opens the last folder, then the first recent
folder that still exists, then the default sample workspace.

```text
%LOCALAPPDATA%/Brinell.Presenter/user-settings.json
```

```powershell
$env:BRINELL_PRESENTER_SETTINGS_PATH = "path\to\user-settings.json"
```

## Build

```powershell
# Working directory: Brinell root
dotnet build srcnew\Brinell.Presenter\Brinell.Presenter.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Presenter.Bdd.Tests\Brinell.Presenter.Bdd.Tests.csproj -v:minimal /nr:false
```
