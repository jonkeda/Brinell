# Combining projects: what can, and what should not

Status: **plan**, not started. Date: 2026-10-29.

Asked: combine `Bruinet.Bdd` with `Bruinet.Bdd.Workspace`, `Bruinet.Narrator` with `Bruinet.Recording`, and
maybe others. Builds on [AD-026](../../.docs/decisions/ad-026-a-project-earns-its-place.md) and its
2026-09-30 analysis (`git show a8b3fc75:.my/projects/design.md`), which took the count from 98 to 84.

## Advice

**Neither merge asked for is a good idea**, and for Bdd and Workspace it cannot be done at all:

| Asked | What the references say | Verdict |
| --- | --- | --- |
| `Bdd` + `Bdd.Workspace` | `Bdd` is below `Narrator` and `Uat` (both reference it); `Workspace` is above them (it references `Narrator`, `Uat` and `TestData`). One project would reference itself through `Narrator`: a cycle. To make it work, `Narrator`, `Uat` and `TestData` would have to go in too. | **No.** The result puts xUnit and the video narrator under the parser (AD-021) and Markdig, globbing and the preview renderer into every UI test project. |
| `Narrator` + `Recording` | `Recording` is used by `Narrator`, `Bruinet.Appium` and `Bruinet.Html.Playwright`; `Narrator` references `Bdd`. Merged, the two drivers depend on the Bdd engine (about 10,000 lines and Markdig) to record a screen. | **No.** AD-026 rules it out for this reason. |

**What is worth doing is small:** three Todo UAT projects folded into their UI test projects (AD-026
already decided it; it waits for a yes), and a solution filter per family so a family opens on its
own. The rest of the 116 projects each pass AD-026's test (section 3). One more merge is possible
with an amendment to AD-026: `Recording` into `Core` (section 4); I would leave it.

**Timing:** merge before the first release. Nothing is tagged, so a package that disappears breaks no
one; after a release, a removed package id is a breaking change (`BREAKING-CHANGES-POLICY.md`).

## 1. Where the count comes from

116 projects (84 at the last pass), outside Frainet: `srcnew` 35, `testsnew` 41, `samples` 32, `tools` 3,
`Scraper.Sites` 5. 38 came since 2026-09-30, each with a reason: the `Mocking.Data` family (9), MauiUnit and
its head (8), `Bruinet.Appium` (2), the Scraper's test sites (5), `Cli` replacing three tools, and
the Sql and Android samples. The growth is features, not projects that nothing justifies.
`.my/ScraperEx/spike0` and `manual-run` are two scratch projects outside the solution; they go when
that topic closes.

## 2. The rule applied

A project stays when one of AD-026's reasons holds: R1 another target framework, R2 another process
or loader, R3 a dependency its users should not get, R4 a layer others build on without the rest,
R5 a sample that teaches a structure. A merge is safe when it makes no cycle, adds no package to the
absorbing project's users, keeps the target framework, and no consumer of the absorbed project gets
heavier.

## 3. Every library checked

The libraries with one dependent in `srcnew` or `tools` (the only ones a merge could take in):

| Project | Its one dependent | Why it stays |
| --- | --- | --- |
| `Html.Playwright` | `Blazor` | Playwright (R3); ten projects use it |
| `Maui.FlaUI` | `Presenter` | FlaUI, Windows only (R1, R3) |
| `Mocking.Analyzers` | `Mocking` | An analyzer is `netstandard2.0` (R1) |
| `Mocking.Data.EntityFrameworkCore` | `…SqlServer` | The SqlServer adapter carries a provider package other EF users should not get (R3) |
| `Scraper` | `Cli` | Playwright (R3); a library with its own tests, and `tools/` holds what runs (AD-026) |
| `Stride` | `Stride.Automation` | The driver side and the server a game embeds run in two processes (R2) |
| `Uia.Contracts` | `Maui.FlaUI` | The app under test loads it too (R2) |
| `MauiUnit` | `Maui.MauiUnit` | A component-test framework on its own, usable without Bruinet (R4) |

Also kept: `Core.Legacy` (Html, MAUI and Android never take it, AD-029), `Narrator.Windows` (a Windows
SDK framework, found at run time), `TestData` (no dependency at all, on purpose), the three
`Mocking.Data` satellites (net48, ASP.NET Core, a provider each), the control sources
(`Maui.CommunityToolkit`, `Blazor`: a suite that does not use one does not get it) and one test
project per library (a focused run is one project).

## 4. What to do

| # | Step | Saves | Check |
| --- | --- | --- | --- |
| 1 | **Todo's UAT projects into their UI test projects** (AD-026's step 8, Q5): `Todo.Uat` into `Todo.UITests`, `Todo.Web.Uat` into `Todo.Web.UITests`, `Todo.Mixed.Uat` into `Todo.Mixed.UITests`. The features and `uat.config.md` move beside the page objects they name (`Todo.Uat` already references `Todo.UITests`); the journey coverage report reads the projects by name and changes with them; the guides that name the old projects (`features-below-the-ui.md`, `test-data.md`, `uat-template-guide.md`, `narrated-recordings.md`, `visual-studio-preview.md`) too. | 3 | The same tests and scenarios pass, filtered apart by `Category=UAT`; the coverage report's counts match |
| 2 | **A solution filter per family** (`Bdd.slnf`: `Bdd`, `Uat`, `Workspace`, `Narrator`, `Narrator.Windows`, `Recording`, `Presenter`, `Cli` and their tests; likewise Maui, Html, Mocking): open a family without the other hundred. The solution already has folders; a filter is a file, no project moves. | 0 | Each filter opens and builds in Visual Studio |
| 3 | **Optional: `Recording` into `Core`**, with an AD-026 amendment. See below. | 2 | `dotnet build Bruinet.sln`; the Core, Narrator and driver tests unchanged |

`Recording` into `Core` is technically clean: 854 lines, no package, `Recording` references only `Core`,
and `Narrator`, `Appium` and `Html.Playwright` already do. The cost is the layering AD-026 names:
the ffmpeg encoder and the run-time loading of recorders would sit in the layer everything builds
on. Its tests need the Windows recorder, so they would move to `Narrator.Tests`, which already has
it. For one project (two with the tests) I would not amend an AD; the plan lists it so the choice is
yours.

## 5. Open questions

| # | Question | Leaning |
| --- | --- | --- |
| Q1 | Step 1, Todo's UAT projects: yes? | Yes: AD-026 decided it, and a sample that teaches a separate UAT project teaches what the framework no longer does. |
| Q2 | `Recording`: leave, into `Core`, or into `Narrator`? | Leave. Into `Core` works but needs the amendment; into `Narrator` breaks the drivers. |
| Q3 | Solution filters (step 2)? | Yes: the cheap way to what the merges were for. |
| Q4 | `Todo.Mixed.UITests`, `Todo.Mixed.Uat` and `Todo.TestSupport.Apps` (about 500 lines) as two projects? | No: a teaching sample, and not worth the change (the 2026-09-30 view). |
| Q5 | Fold `Sql.Spike.Tests` into `Mocking.Data.Tests` when the sql-replay work ends? | Decide then: it references the web stack, which the data tests should not take. |

## 6. Risks

- **A merge that looks safe is not.** Section 2's four checks come first for every project; the first
  asked merge fails the cycle check.
- **Names in scripts and docs.** The journey coverage report, Presenter's default workspace and the
  guides name test projects; step 1 searches each name before moving a file.
- **A removed package.** Not yet a break (no release); after one, a removed id is.
