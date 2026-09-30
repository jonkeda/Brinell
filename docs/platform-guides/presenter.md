# Presenter Platform Guide

`Brinell.Presenter` is a Windows desktop shell (a MAUI app) for loading and running UAT
workspaces: a narrow bar docked to one side of the screen, with the app under test (or its browser)
placed in the rest.

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
Workspace mode with "Not built yet" and the Diagnostics tab is the expected first screen.

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
- **The suite runs in Presenter's process**, not in a test host. A fixture finds its files (the
  config, the app, its solution) from its own assembly
  (`Path.GetDirectoryName(typeof(MyFixture).Assembly.Location)`), never from
  `AppContext.BaseDirectory`: that is Presenter's folder here. Presenter resolves the suite's
  assemblies, its native libraries (`runtimes\<rid>\native`) and ASP.NET Core for it.

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

The top row starts with **Open** (a workspace folder), in both modes. Then the two modes, a pair of
buttons:

- **Present**: the scenarios, the steps and the moves, nothing else. A workspace that loads cleanly
  opens in it.
- **Workspace**: adds the recent folders, the build choices and
  **Build**, the tabs and the Selection panel. A workspace with problems opens in it, and says
  "Not built yet" above **Build** when the suite has no build.

**Reload** (both modes) reloads and checks the workspace. At the top right, **Dock** moves the bar
to the other side of the screen (remembered), and **Settings** opens the Settings page (the same
button closes it): the pause between steps of each speed, the App window, When a run ends, and the
theme. The run toolbar is under the
steps: **Run**, **Next**, **Into**, **Out**, **Stop**, **Restart**, and **Speed**.

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

A run pauses **before** a step, so what is about to happen can be said before it happens. The
moves are those of the IDEs' stepping (AD-022), on keys a presentation clicker sends too:

| Button | Keys | Does |
| --- | --- | --- |
| **Run** | F5 | runs the selection's scenarios; from a pause, goes on. Stops at a breakpoint, a failure or the end |
| **Next** (Step) | Space, Right, Page Down | with no run on, starts one paused before the first step; else runs the highlighted step (a `## Step:` call whole) and pauses before the next |
| **Into** | Down | at a call, goes inside it without running anything |
| **Out** | Up | inside a call, runs the rest of it and pauses after it |
| **Stop** | Esc | ends the run and closes the app |
| **Restart** | Ctrl+R | the scenario again, paused before its first step |

The keys work anywhere in the Presenter window except in a text box. A click on a step sets or
clears its breakpoint. A right-click offers **Run until here** (runs on and pauses before it), **Run from here** (only while a
run is paused before it: skips the steps in between, marked skipped, and runs on; the app must
already be where they lead) and **Toggle breakpoint**. The preview's paused bar has the same moves.

Breakpoints are the IDEs' own file, `%LOCALAPPDATA%\Brinell\bdd-breakpoints.json`: a red dot in the
list marks one, a breakpoint set in Visual Studio or VS Code stops Run in Presenter, and one set in
Presenter stops a debugged run in the IDEs. A breakpoint can be on a step or a call of the feature
file, not on a step inside a step file.

| Selected | Runs |
| --- | --- |
| Workspace or folder | every scenario file under it |
| `.feature.md` file or suite | its scenarios |
| Scenario | that scenario |
| Config or other Markdown | nothing |

**Speed** cycles **Fast**, **Normal** and **Slow**, with a gauge for each; the pause of each is set on
the Settings page (250, 800 and 1500 ms at first), and both are remembered. The pause is read when a
run starts or goes on, and applied between steps, never before the first: seven steps at 1000 ms take
at least six seconds. A step is marked passed only after its command completes. The
status line under the list shows the current step, where the run is paused, or the result
(`Passed: 1/1 scenarios`); per-step timings are in Diagnostics.
### Record

**Record** (the dot at the end of the run toolbar) records each run on video while it is on
([Narrated Recordings](../guides/narrated-recordings.md)). A scenario that a video script of the
suite has a scene for is narrated by it: its intro, each step's line, its outro; any other runs
silently, with its beats. The video is made when the run ends, however it ends, and **Watch**
opens the last one. It needs ffmpeg; without it, Record says how to get it and stays off.

On the Settings page, **Recording**: the Recordings folder (the suite's `TestResults\recordings`
when empty), the voice when the script names none, the beat after a step, and **Speak aloud**,
which also plays each line while the run waits for it.

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

Presenter shows the app under test as the **App window** setting says, for every target (a MAUI, WPF
or WinForms window, or a browser):

| App window | The app |
| --- | --- |
| Beside the bar (default) | in front of other windows, in the rest of the bar's monitor |
| Maximized, other monitor | filling the largest monitor without the bar; beside the bar when there is one monitor |
| Maximized | filling the bar's monitor, with the bar on top while a run is on |
| Where it opens | in front of other windows, not moved |
| Background | behind everything, as in a test run |

It tells the launcher to leave the window shown (`BRINELL_AUT_WINDOW=shown`: the MAUI driver no longer
sends it behind, and a browser is not headless and opens at `BRINELL_AUT_BOUNDS`). It notes the
top-level windows before a run starts, and once the app's window opens it moves it, without taking
the focus; it tries again after the first step, for an app that opens late. Presenter keeps the keys:
when the app it showed takes the focus (WPF and WinForms do, for real input), Presenter takes it
back after the step. A placement that fails does not fail the run; Diagnostics shows the result.

**When a run ends**: **Close the app** (default) closes it when the run passes, fails or is stopped;
**Keep it open** leaves it where it is until the next run starts, **Stop** is pressed with no run on
("Closed the app"), another workspace is opened or built, or Presenter closes.

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
