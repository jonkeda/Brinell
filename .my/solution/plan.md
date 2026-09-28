# One solution with every project, in folders

Status: **steps 1-4 done** 2026-09-28; step 5 changed, step 6 open. Date: 2026-09-28.

What happened:
- **Steps 1-3.** `srcnew/Brinell.sln` was regenerated with the layout of section 3: 91 projects
  (every one but ClaudeSwitcher), the two VSIX projects with Build unticked, the solution items,
  and a `VS Code` folder of the extension's files. A clean `dotnet build` of it takes about 3.5
  minutes on this machine, and every new project built. One fix was needed: JourneyCoverage
  referenced the three Todo UI test projects with `SetTargetFramework`, which made a solution
  build compile each of them twice at once into one `obj` folder (a file-lock race). The
  attribute is gone; `SkipGetTargetFrameworkProperties` already builds them for their own TFM.
- **Step 4.** CI no longer builds the Generator and DataValidator tests separately; the docs say
  the solution is complete.
- **Step 5, changed.** The root `Brinell.sln` is **kept**: tests (`BridgeGatingTests`,
  `StrideUatFixture`, `BddConfigWriterTests`, the Presenter's inspector tests) and
  `BddWorkspaceConfigInspector` find the repository root by it. `docs/architecture/structure.md`
  says to open `srcnew/Brinell.sln` instead. Deleting it first needs another root marker (Q6).
- **Step 6** (a check that no project is left out) is not done.

## 1. Goal

`srcnew/Brinell.sln` holds **every project in the repository**, in solution folders that show
how Brinell is built: core, platforms, the BDD engine, tools and samples, each test project
beside the project it is named after. It
is the one solution to open, the one CI builds, and the one the docs name. Nothing is built by
hand outside it except where a build cannot run under `dotnet build` (section 4).

## 2. What holds today

| Fact | Where |
| --- | --- |
| The repository has **92 projects**; `srcnew/Brinell.sln` has **53**. The 39 it lacks are listed in section 3. | `git ls-files '*.csproj'` against `dotnet sln list`, at c2abfaa |
| Its folders are `Source`, `Tests` (with `Unit` and `UI`) and `Samples`. Every project in `srcnew/` sits flat under `Source`. | `srcnew/Brinell.sln` |
| A second solution, **`Brinell.sln` at the root**, holds 64 projects: another slice, with the Todo sample, the generator, the Scraper and `ClaudeSwitcher`, but none of the Bdd tools. It has folders named after the old layout (`src`, `tests`, `Maui`, `Wpf`, ...). No workflow or doc uses it except to say it is different. | `Brinell.sln`, `docs/run/build-and-test.md` |
| Two `.slnx` files stay useful for focused work: the Todo sample's, and the VSIX's (built with Visual Studio's MSBuild in CI and release). | `samples/Todo/Brinell.Samples.Todo.slnx`, `tools/Brinell.Bdd.VisualStudio/Brinell.Bdd.VisualStudio.slnx` |
| CI builds `srcnew/Brinell.sln`, then builds `Brinell.Generator.Tests` and `Brinell.DataValidator.Tests` separately **because they are not in it**, then tests a fixed list of projects one by one. So adding projects to the solution adds build time, not test runs. | `.github/workflows/ci.yml` lines 41-77 |
| CI already restores the MAUI workload for the solution (the MAUI sample app has an Android target), so the Todo app's Android target needs nothing new. | `ci.yml` line 41 |
| The VSIX and its CodeLens project target `net48` with the VS SDK, and cannot build under `dotnet build`. | `ci.yml` line 121 |

## 3. The layout

Projects marked **new** are not in the solution today. Folder names sort in this order in
Visual Studio because of their number (Q2).

Each test project sits in the same folder as the project it is named after, so a folder holds a
part of Brinell and everything that tests it. There is no separate tests folder.

```text
Brinell.sln  (srcnew/)
├── 1 Core
│     Brinell.Core               Brinell.Core.Tests
│     Brinell.Automation         Brinell.Automation.Tests
│     Brinell.Uia.Contracts      Brinell.Uia.Tests, Brinell.Uia.TestHost
│     Brinell.TestData           Brinell.TestData.Tests
│     Brinell.Mocking            Brinell.Mocking.Tests
│     Brinell.Mocking.Analyzers  Brinell.Mocking.Analyzers.Tests
├── 2 Platforms
│   ├── Maui       Brinell.Maui               Brinell.Maui.Tests, Brinell.Maui.UITests,
│   │                                         Brinell.Maui.UITests.Mobile (new),
│   │                                         Brinell.Maui.Uat.Tests
│   │              Brinell.Maui.FlaUI, Brinell.Maui.Appium (new),
│   │              Brinell.Maui.CommunityToolkit, Brinell.Maui.Extensions
│   ├── Web        Brinell.Html               Brinell.Html.Tests, Brinell.Html.UITests,
│   │                                         Brinell.Html.Uat.Tests
│   │              Brinell.Html.Playwright
│   │              Brinell.Blazor             Brinell.Blazor.Tests, Brinell.Blazor.UITests,
│   │                                         Brinell.Blazor.Uat.Tests
│   ├── Desktop    Brinell.Wpf                Brinell.Wpf.Tests, Brinell.Wpf.UITests,
│   │                                         Brinell.Wpf.Uat.Tests
│   │              Brinell.WinForms           Brinell.WinForms.Tests, Brinell.WinForms.UITests,
│   │                                         Brinell.WinForms.Uat.Tests
│   ├── Android    Brinell.NativeAndroid (new)  Brinell.NativeAndroid.Tests (new)
│   └── Stride     Brinell.Stride             Brinell.Stride.Tests, Brinell.Stride.UITests,
│                                             Brinell.Stride.Uat.Tests
├── 3 Bdd
│     Brinell.Bdd                Brinell.Bdd.Tests
│     Brinell.Uat                Brinell.Uat.Tests
│     Brinell.Presenter          Brinell.Presenter.Bdd.Tests
├── 4 Tools
│   ├── Bdd IDE    Brinell.Bdd.Workspace (new)       Brinell.Bdd.Workspace.Tests (new)
│   │              Brinell.Bdd.Preview (new)         Brinell.Bdd.Preview.Tests (new)
│   │              Brinell.Bdd.LanguageServer (new)  Brinell.Bdd.LanguageServer.Tests (new)
│   │              Brinell.Bdd.VisualStudio, Brinell.Bdd.VisualStudio.CodeLens (new; not
│   │              built by the solution, section 4)
│   ├── Data       Brinell.DataValidator (new)       Brinell.DataValidator.Tests (new)
│   ├── Generator  Brinell.Generator, Brinell.Generator.Cli (new)
│   │                                                Brinell.Generator.Tests (new)
│   └── Scraper    Brinell.Scraper (new, Q3)         Brinell.Scraper.Tests (new)
├── 5 Samples
│   ├── Apps       Brinell.Samples.Maui.App, .Maui.ShellApp (new), .Blazor.App, .Wpf.App,
│   │              .WinForms.App, .Stride.App, .Shared, Brinell.Maui.AppSupport (new)
│   └── Todo
│       ├── src    Api, App, Contracts, Core, Infrastructure, Web (all new)
│       └── tests  UnitTests, IntegrationTests, JourneyCoverage, TestSupport,
│                  TestSupport.Apps, UITests, Web.UITests, Mixed.UITests,
│                  Uat, Web.Uat, Mixed.Uat (all new)
└── Solution Items
      ../global.json, ../Directory.Build.props, ../Directory.Packages.props,
      Directory.Build.props, Directory.Packages.props, ../.editorconfig,
      ../AGENTS.md, ../CHANGELOG.md, ../.github/workflows/ci.yml, release.yml
```

Why this shape:
- **A test beside what it tests.** Opening `Platforms/Maui` shows the driver and every tier that
  checks it: unit (`.Tests`), UI (`.UITests`) and UAT (`.Uat.Tests`). The tier is in the name, so
  Test Explorer still filters by it, and the commands in AGENTS.md name projects, as before.
- **Namesake rules.** A `Brinell.X.UITests` or `Brinell.X.Uat.Tests` goes with `Brinell.X`, the
  platform it drives, not with `Brinell.Uat`. `Brinell.Uia.Tests` and its test host go with
  `Brinell.Uia.Contracts`; `Brinell.Presenter.Bdd.Tests` with the Presenter;
  `Brinell.Maui.UITests.Mobile` with Maui.
- **The Todo sample keeps its own tree**, as in its `.slnx`: it is an app with its tests, and it
  is what a reader copies.
- **The VS Code extension** (`tools/Brinell.Bdd.VsCode`, TypeScript) is not an MSBuild project. It
  goes into `4 Tools/Bdd IDE` as a solution folder of files (`package.json`, `src/*.ts`), so it is
  visible and searchable (Q4).

**Not added: `tools/ClaudeSwitcher`.** It is a WPF tool that switches between Claude profiles, not
part of Brinell. It stays out of the solution; moving it out of the repository is a separate question
(Q3).

## 4. What the solution does not build

A project can be in the solution with **Build** unticked in every solution configuration, and
`dotnet build` then skips it (as Visual Studio does). Two projects need that:

| Project | Why | Built by |
| --- | --- | --- |
| `Brinell.Bdd.VisualStudio` | `net48` + VS SDK: needs Visual Studio's MSBuild | the VSIX `.slnx`, in CI's `ide-extensions` job and in release, as now |
| `Brinell.Bdd.VisualStudio.CodeLens` | the same | the same |

In Visual Studio they can still be built by right-click > Build, or by opening the VSIX
`.slnx` to F5 them. Everything else builds under `dotnet build srcnew/Brinell.sln`, which step 1
checks for each new project before it is added.

## 5. Steps

Each step is one commit, verified as listed. The layout only moves projects between folders
and adds projects; no project file changes.

| # | Step | Done when |
| --- | --- | --- |
| 1 | **Check** that each new project builds from the solution's point of view: `dotnet build` each one from the Brinell root (the root `global.json`, not `samples/global.json`), with the workloads CI restores. Note the build time CI gains. A project that fails is fixed, or left out with the reason written here. | A table of the 38 projects: builds, time, or why not. |
| 2 | **Folders.** Create the folders of section 3 and move the 53 existing projects into them (`dotnet sln` cannot nest folders two deep; edit `NestedProjects` by a script, or use Visual Studio). Add the solution items. | `dotnet build srcnew/Brinell.sln` builds the same projects as before; the Unit tests CI runs pass. |
| 3 | **Add** the new projects into their folders; untick Build for the two VSIX projects in every configuration. | `dotnet build srcnew/Brinell.sln` succeeds; the solution lists 91 projects (92 less ClaudeSwitcher); Visual Studio opens it with no unloaded project. |
| 4 | **CI and docs.** Drop CI's separate builds of `Brinell.Generator.Tests` and `Brinell.DataValidator.Tests` (the solution builds them now). Update the comment above the `ide-extensions` job. In `docs/run/build-and-test.md`, `AGENTS.md` and `.github/copilot-instructions.md`: the solution is complete; drop "It is not a complete inventory". | CI green on the branch. |
| 5 | **The root `Brinell.sln`** (Q1): delete it, and say so in the CHANGELOG under **Changed**. | No doc or script names it. |
| 6 | **Keep it complete.** A test in `Brinell.Core.Tests` (or a CI step) that lists every `*.csproj` under the repository and fails when one is neither in the solution nor on a short list of exceptions (`ClaudeSwitcher`). | The check fails on a project added only to disk, and passes on the branch. |

After each step, from the Brinell root:
- `dotnet build srcnew\Brinell.sln -v:minimal /nr:false`;
- the unit suites CI runs;
- open the solution in Visual Studio 2026: no unloaded projects, and the folders as in section 3.

## 6. Open questions

| # | Question | Default if not answered |
| --- | --- | --- |
| Q1 | The root `Brinell.sln`: delete it, or make it the one solution (and move `srcnew/Brinell.sln` there)? | Keep `srcnew/Brinell.sln`: CI, the docs and AGENTS.md all name it. Delete the root one, which holds nothing the complete one lacks. |
| Q2 | Number the top folders (`1 Core` ... `5 Samples`) so they sort in build order, or plain names (sorted alphabetically: Bdd, Core, Platforms, Samples, Tools)? | Numbered. |
| Q3 | `tools/Brinell.Scraper` (dormant since 2026-05, an owner decision in the cleanup plan) and `tools/ClaudeSwitcher` (not Brinell): in the solution or not? | Scraper and its tests in, under `4 Tools/Scraper`, if they build (step 1); ClaudeSwitcher out. |
| Q4 | The VS Code extension as a solution folder of files: useful, or noise? | Add it, with only `package.json`, `README.md` and `src/`. |
| Q6 | The root `Brinell.sln` is the repository-root marker for several tests and the Presenter. Move them to another marker (`global.json` beside `AGENTS.md`) and delete it, or keep it as a marker? | Keep it for now. |
| Q5 | The two VSIX projects: in the solution with Build unticked (section 4), or left out as today? | In, unticked, so the whole product is in one tree. |
