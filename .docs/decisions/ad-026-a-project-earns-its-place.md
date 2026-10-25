# AD-026: A Project Earns Its Place

Bruinell has many projects (84). A project is separate only when one of these holds:

| # | Reason | Example |
| --- | --- | --- |
| R1 | **Another target framework** | `net48` for the Visual Studio extension; a Windows 10 SDK framework for `Bruinell.Narrator.Windows`; `netstandard2.0` for an analyzer |
| R2 | **Another process or loader** | An executable (one with a command line is `bruinell`, AD-028); an assembly an app under test loads (`Bruinell.Uia.Contracts`); one Visual Studio loads out of process (the CodeLens provider); one found at run time in a `recorders` folder |
| R3 | **A dependency its users should not get** | Appium, FlaUI, Playwright, xUnit, Markdig |
| R4 | **A layer others build on without the rest** | `Bruinell.Core` under everything; `Bruinell.Bdd` without xUnit (AD-021); `Bruinell.TestData` with no dependency at all; `Bruinell.Appium` under the MAUI and native Android Appium stacks, so neither takes the other |
| R5 | **A sample that teaches a structure** | The Todo sample's tiers |

What follows from it:

| Decision | What holds |
| --- | --- |
| **A platform's UAT suite lives in its UI test project** | `uat.config.md`, `Features/` and `Uat/` (the fixture, the scenario test class) are in `Bruinell.<Platform>.UITests`, beside the page objects the scenarios name. A UAT run is the filter `Category=UAT`. |
| **One tooling library for the Bdd tools** | `Bruinell.Bdd.Workspace` loads a suite and renders a file (namespace `Bruinell.Bdd.Preview`); the language server and Presenter use it. It is a library, so it is in `srcnew/`; `tools/` holds what runs. |
| **A control source is a package of its own** | `Bruinell.Maui.CommunityToolkit`, `Bruinell.Blazor`: a suite that does not use it does not get it. |
| **One test project per library**, beside its namesake | A focused run is one project. |

**Why:** a project that is separate only because it was written at another time is one more
thing to open, name, reference and keep in the solution, and a scenario is only as good as the
page objects it names: a page object changes for its UI tests and its scenarios together.
Evidence (at a8b3fc75): [design.md](https://github.com/exact-mobile-apps/test-tooling/blob/a8b3fc75/.my/projects/design.md).

**Ruled out:**
- a `Bruinell.<Platform>.Uat.Tests` project beside a platform's UI tests;
- `Bruinell.Bdd` and `Bruinell.Uat` as one (xUnit under the engine, AD-021);
- `Bruinell.Recording` inside `Bruinell.Narrator` (the drivers would depend on the Bdd engine) or inside `Bruinell.Core` (ffmpeg and run-time loading under everything);
- `Bruinell.TestData` inside `Bruinell.Core`; a driver merged with its platform package;
- a new project for code that none of R1 to R5 sets apart.

**Broken when:**
- a project is added whose only reason is when it was written;
- a platform's feature files and its page objects are in different projects;
- a tool loads a suite or renders a file without `Bruinell.Bdd.Workspace` (AD-022).
