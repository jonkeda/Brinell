# Design: fewer projects

Status: **decided**, 2026-09-30 (Q1 to Q3 answered); not built. The question: there are a lot of
`Brinell.Bdd.*` projects, and a lot of projects altogether; which should be combined?

## Answer

**Fourteen projects go (98 to 84):** eight small merges and removals, and the six UAT suites into
their UI test projects. Most of the rest are separate for a reason that a merge would break: a
different target framework, a different process, a package a consumer should not get, or a
sample that teaches a structure.

The `Brinell.Bdd.*` family is twelve projects. Only **one merge is worth doing there**: the
preview into the workspace library. The others are an engine, its xUnit head, an executable, a
Visual Studio extension and the assembly Visual Studio makes it keep apart.

Merging saves little build time (the solution builds in about 3.5 minutes; the projects proposed
here are small). What it buys is fewer things to open, name, reference and keep in the solution.

## What there is

| Where | Projects | Lines of C# | What |
| --- | --- | --- | --- |
| `srcnew/` | 25 | 84,800 | The libraries, and Presenter |
| `tools/` | 11 | 32,100 | The Bdd tools, the generator, the data validator, the Scraper |
| `samples/` | 25 | 27,200 | The sample apps; the Todo sample is 17 of them |
| `testsnew/` | 37 | 46,100 | A unit, a UI and a UAT test project per platform, and one per library |
| | **98** | 190,200 | |

No project outside Brinell references any of these, so a merge breaks nothing in Construction.
Every library in `srcnew/` is also a NuGet package (`GeneratePackageOnBuild`), but the packages
are not used yet (Q1), so a package id can go.

## When a project earns its place

A project stays separate when one of these holds. Each row below is checked against them.

| # | Reason | Example |
| --- | --- | --- |
| R1 | **Another target framework** | `net48` for the Visual Studio extension; a Windows 10 SDK framework for the window recorder; `netstandard2.0` for an analyzer |
| R2 | **Another process or loader** | An executable; an assembly an app under test loads; one Visual Studio loads out of process; one found at run time in a `recorders` folder |
| R3 | **A dependency its users should not get** | Appium, FlaUI, Playwright, xUnit, Markdig |
| R4 | **A layer others build on without the rest** | `Brinell.Core` under everything; `Brinell.Bdd` without xUnit |
| R5 | **A sample that teaches a structure** | The Todo sample's tiers |

None of them holds for a project that is separate only because it was written at another time.

## The Bdd family

| Project | Lines | Why it is separate | Verdict |
| --- | --- | --- | --- |
| `Brinell.Bdd` | 8,400 | The engine: parse, bind, run. No xUnit, no UI (R4, AD-021) | Keep |
| `Brinell.Uat` | 3,400 | The xUnit head on the engine: scenario theories, fixtures (R3, AD-021). 18 projects use it | Keep |
| `Brinell.Bdd.Workspace` | 1,200 | Loads a suite for every tool | **Takes in Preview** |
| `Brinell.Bdd.Preview` | 1,100 | Renders a feature file as HTML. Used by the same two hosts as Workspace (the language server, Presenter), and already depends on what Workspace depends on | **Merge into Workspace** |
| `Brinell.Bdd.LanguageServer` | 4,300 | An executable, and the MCP server (R2) | Keep |
| `Brinell.Bdd.VisualStudio` | 2,600 | The extension, `net48` (R1) | Keep |
| `Brinell.Bdd.VisualStudio.CodeLens` | 110 | Visual Studio loads a CodeLens provider in another process: it must be its own assembly (R2) | Keep |
| `Brinell.Bdd.Tests`, `Brinell.Uat.Tests`, `...LanguageServer.Tests` | 9,700 | One per library | Keep |
| `Brinell.Bdd.Workspace.Tests`, `Brinell.Bdd.Preview.Tests` | 860 | One per library | **Merge, with their libraries** |

After the merge: ten. The family reads as **engine, xUnit head, tooling library, language server,
extension**, and a test project for each.

`Brinell.Bdd.Workspace` sits in `tools/` but is a library that `srcnew/Brinell.Presenter`
references. It moves to `srcnew/` with the merge, so `tools/` holds only things that run.

Not proposed: **`Brinell.Bdd` and `Brinell.Uat` as one.** It would put xUnit under the engine,
which AD-021 rules out, and a tool that only reads feature files would get a test framework.

## The other merges

| # | Merge | From, to | Why it is safe | Saves |
| --- | --- | --- | --- | --- |
| 1 | `Brinell.Bdd.Preview` into `Brinell.Bdd.Workspace`, and their tests | 4 to 2 | Above | 2 |
| 2 | `Brinell.Recording.Windows` (270 lines) and `Brinell.Narrator.Windows` (74 lines) into one `Brinell.Narrator.Windows` | 2 to 1 | The same target framework, both found at run time in the `recorders` folder, and the recorder is used by nothing but the narrator's Windows package | 1 |
| 3 | Delete `Brinell.Maui.Extensions` | 1 to 0 | It holds 11 lines of global usings and no code; only `Brinell.Maui.Tests` references it. Its docs (`docs/controls/Shell.md`, `ShellContent.md`, `MAUI_EXTENSIONS_PROJECT_MIGRATION.md`) describe controls that are no longer there | 1 |
| 4 | `Brinell.Generator.Cli` (170 lines) into `Brinell.Generator` | 2 to 1 | Only the Cli and the tests use the library; a test project can reference an executable | 1 |
| 5 | `Brinell.Stride.Tests` (usings only) and `Brinell.Automation.Tests` (31 lines) into one | 2 to 1 | One has no tests, the other three | 1 |
| 6 | `Brinell.Mocking.Analyzers.Tests` (86 lines) into `Brinell.Mocking.Tests` | 2 to 1 | The analyzer ships in the Mocking package | 1 |
| 7 | `ClaudeSwitcher` out of the repository | 1 to 0 | It is not Brinell and is only in the old root `Brinell.sln` | 1 |
| 8 | Each `X.Uat.Tests` into its `X.UITests`, for six platforms (below) | 12 to 6 | A UI test and a UAT scenario drive the same page objects | 6 |
| | | | **Total** | **14** |

The Scraper (`Brinell.Scraper`, `Brinell.Scraper.Tests`) is not touched here: changing it is the
next piece of work (Q2).

Also a rename, not a merge: **`Brinell.Automation` to `Brinell.Stride.Automation`**. It is the
server a Stride game embeds, its pipe is already called `Brinell.Stride.Automation`, and the
name `Brinell.Automation` promises more than it is.

## The UAT suites move into their UI test projects

Each platform has three test projects: `X.Tests` (unit), `X.UITests` (UI tests and the page
objects) and `X.Uat.Tests` (the config, the feature files and one test class). The UAT projects
are small:

| UAT project | Lines of C# |
| --- | --- |
| `Brinell.Maui.Uat.Tests` | 48 |
| `Brinell.Wpf.Uat.Tests` | 52 |
| `Brinell.WinForms.Uat.Tests` | 74 |
| `Brinell.Stride.Uat.Tests` | 81 |
| `Brinell.Blazor.Uat.Tests` | 103 |
| `Brinell.Html.Uat.Tests` | 140 |

Folding each into its `X.UITests` saves **six** projects: the feature files and `uat.config.md`
move beside the page objects they name.

**Decided: yes (Q3).** UI tests and UAT scenarios are tightly connected: a scenario is only as
good as the page objects it names, and a page object changes for both. What the move has to
take care of:

- Presenter's default workspace is `testsnew/Brinell.Maui.Uat.Tests`, by name: it becomes
  `testsnew/Brinell.Maui.UITests`, where Presenter's own UAT suite and the sample script
  (`sample-tour.video.md`) then find it.
- A UAT run and a UI run are filtered apart today by project; merged, by trait
  (`Category=UAT`, which the UAT test classes already carry). The tiers in `AGENTS.md` get the
  filter.
- The guides teach a UAT project of its own (`docs/guides/uat-template-guide.md`): they change
  to "the UAT suite lives in the UI test project", and keep the separate project as an option.
- **The Todo sample follows** (`Todo.Uat` into `Todo.UITests`, `Todo.Web.Uat` into
  `Todo.Web.UITests`, `Todo.Mixed.Uat` into `Todo.Mixed.UITests`): a sample that taught a
  separate UAT project would teach what Brinell no longer does. That is three more projects,
  counted in a step of its own because the sample's journey coverage reads its projects by name
  (Q5).

## Looked at, and kept

| Candidate | Why it stays |
| --- | --- |
| `Brinell.Recording` into `Brinell.Narrator` | The Playwright and Appium drivers record too, and would then depend on the Bdd engine (R4) |
| `Brinell.Recording` into `Brinell.Core` | Possible (800 lines, and Core already has `Core.Recording`), but it puts ffmpeg and run-time assembly loading in the layer everything depends on. One project is not worth that |
| `Brinell.Uia.Contracts` into `Brinell.Maui.FlaUI` | The app under test loads it too (R2) |
| `Brinell.Mocking.Analyzers` into `Brinell.Mocking` | An analyzer must be `netstandard2.0` (R1) |
| `Brinell.Maui.CommunityToolkit` into `Brinell.Maui`, `Brinell.Blazor` into `Brinell.Html` | A control source is a package of its own, so a suite that does not use it does not get it; the next vendor suite follows the same shape |
| `Brinell.Maui.Appium`, `Brinell.Maui.FlaUI`, `Brinell.Html.Playwright` | A driver and its heavy dependency (R3), and `FlaUI` is Windows only (R1) |
| `Brinell.TestData` into `Brinell.Core` | It has no dependency at all, on purpose: an app's test-support project uses it without Brinell's UI layer. The cost showed once: `AssemblyFile` could not be shared with it (AD-025) |
| One test project for every library | "Each test project beside its namesake" is the solution's layout (`.my/solution/plan.md`); a focused run is one project |
| `Brinell.Maui.UITests.Mobile` (13 lines of its own) | Another head on the same tests: Android, serial |
| The Todo sample's other projects | Its three UAT projects follow the framework (Q5); the rest stay. The sample teaches the tiers (R5). `Todo.Mixed.UITests`, `Todo.Mixed.Uat` and `Todo.TestSupport.Apps` (510 lines together) could be two; not worth a change to a teaching sample |

## Order of work

Each step is one commit, verified by the same tests passing as before (a mechanical move).

| Step | What | Verified by |
| --- | --- | --- |
| 1 | Delete `Brinell.Maui.Extensions` and its three stale docs; the empty `Brinell.Stride.Tests` takes `Automation.Tests`' tests | `Brinell.Maui.Tests`, the Stride tests |
| 2 | `Generator.Cli` into `Generator`; `Mocking.Analyzers.Tests` into `Mocking.Tests` | Their tests; the generator run from the docs |
| 3 | `Recording.Windows` into `Narrator.Windows` | `Brinell.Recording.Tests`, `Brinell.Narrator.Tests`; one recording from Presenter and one from `dotnet test` (the `recorders` folder is how they are found) |
| 4 | `Bdd.Preview` into `Bdd.Workspace`, moved to `srcnew/`, with their tests | The two test projects' tests in one; the language server's tests; Presenter's tests; the preview in VS Code |
| 5 | Rename `Brinell.Automation` to `Brinell.Stride.Automation` | The Stride tests and sample |
| 6 | ClaudeSwitcher out | The root `Brinell.sln` loads |
| 7 | The six UAT projects into their UI test projects, one platform a commit, Maui last (Presenter's default workspace and its UAT suite move with it) | Each platform's UAT suite, from `dotnet test` with the trait filter and from Presenter |
| 8 | If Q5 is yes: the Todo sample's three UAT projects into their UI test projects | The Todo UAT suites; the journey coverage; the Todo video from Narrator mode |

After each: `srcnew/Brinell.sln`, the CI project list, `docs/architecture/structure.md` and
`AGENTS.md`'s verification commands name the projects, and are updated in the same commit.

## Open questions

| # | Question | Default if not answered |
| --- | --- | --- |
| Q1 | Are the NuGet packages used anywhere by id? | **Answered: not yet used.** Package ids can change |
| Q2 | The Scraper: archive, delete, or leave? | **Answered: changing the Scraper is the next step**, so it is left out of this design |
| Q3 | The six UAT projects into their UI test projects? | **Answered: yes.** UI tests and UAT are tightly connected |
| Q4 | `Brinell.Recording` into `Brinell.Core` after all? | No |
| Q5 | Does the Todo sample follow Q3 (three more projects, 98 to 81)? | Yes, as step 8: the sample should show what the framework does |
