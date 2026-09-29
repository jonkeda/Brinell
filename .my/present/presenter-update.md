# Presenter update: what the IDE work taught us

Status: **step 1 built** 2026-09-29; the owner's comments of the same day are in. Input for
[plan.md](plan.md); nothing here is built.

Presenter exists so a person can **click through a UAT scenario** without an IDE, for one of two
reasons:

- **Validate**: a tester walks a scenario against the real app, watches each step happen, and
  judges whether it did what the scenario says (a manual test with the typing done for them).
- **Show**: someone walks a third person (a product owner, a customer, a new colleague) through
  what a feature does, at the speaker's pace.

The shape: **Presenter is a narrow bar on the left or right of the screen, and the app being
presented fills the rest**. That app can be a MAUI or desktop window, or a browser.

Since Presenter was built, the IDE extensions got a Scenario Preview, step breakpoints, stepping
by BDD step, run results with green checks, and deleting old results. This doc answers two
questions: whether today's UI is easy enough (section 1), and how to improve it (section 2).

## 1. Is the current UI easy enough?

Not yet. It works as a *workspace inspector* for whoever built the suite; for the person clicking
through a scenario, these get in the way:

| # | Problem | Where | Owner's call |
| --- | --- | --- | --- |
| U1 | **The steps are hard to read.** A step is one truncated tree row among folders, files and suites; its full text is not reachable. | the tree's row template, `TailTruncation` | fix (P1) |
| U2 | Every button is an icon, with a tooltip. | `PresenterIconButton` | **keep**: icons keep the bar small |
| U3 | **Builder tools and presenting tools share one screen.** Build, Debug/Release, platform, Validate, Config, Discovery and Command Catalog are always there; only the run buttons matter while presenting. | rows 1-3 of `PresenterPage.xaml` | fix (P3) |
| U4 | **Reload and Validate are the same command** (both `ReloadWorkspace`), shown as two buttons. | `PresenterShellViewModel` ctor | fix (P3) |
| U5 | **The first screen is usually Diagnostics**, since the default sample workspace is not built. | the guide's "A Minimal Run" | fix (P3) |
| U6 | The failure message is only in the status line. | `StatusSummary` | **not a problem** |
| U7 | **No keyboard.** No Space or → for Next, no F5 for Run; a clicker (PageDown/PageUp) does nothing. | no `KeyboardAccelerator` | fix (P2) |
| U8 | **"Next" runs the next step now.** There is no "stop before this step", no "run from here" and no "run until here". | `NextStepCommand` | fix (P2) |
| U9 | Nothing is recorded: no report, no evidence, no green checks in the IDE preview. | `BddExecutionService` | **deferred** (P4) |
| U10 | **Hidden text mirrors** (`WorkspaceTreeText`, `StepListText`...) sit in the layout for Presenter's own UI tests. | row 2 of the Tree tab | fix (P5) |
| U11 | **Only a MAUI app is placed beside Presenter.** A browser or a WPF app opens wherever it opens. | `BRINELL_AUT_PLACE_RIGHT`, MAUI only | fix (P3) |

What works and stays: the icon buttons, the narrow window with the app placed beside it, the tree
showing only Markdown, status icons that roll up to parents, the Config tab's explained fixture
list, and releasing the build between runs.

## 2. How to improve it

### P1. Pick in a tree, step in a list

Split today's single tree in two, each in an expander:

- **Scenarios**: a tree of folders, feature files and their scenarios (no steps). Picking a
  scenario fills the list below; an outline shows its rows as children.
- **Steps**: a flat list of the picked scenario's steps with their status icons. The current step
  is highlighted and scrolled into view; a `## Step:` call is one row with its steps indented
  under it.

Every label, in both, shows its **full text on hover** (a tooltip), so truncation costs nothing.

The IDE's Scenario Preview is **too big to be the main panel** of a narrow bar, but it is worth
**opening on demand**: a button opens it for the picked scenario in a window of its own
(`BddPreview.HostPage` in a `WebView`), for prose, tables and Examples. It follows the run.

Fixes U1.

### P2. Pause before a step; run from here, run until here

Replace Next's "run one step now" with the stepping built for the IDEs:

| Button (icon) | Key | Does |
| --- | --- | --- |
| **Step** | Space, →, PageDown | runs the highlighted step, then stops before the next |
| **Into** | ↓ | at a `## Step:` call, goes inside it, running nothing |
| **Out** | ↑ | runs the rest of the call, stops after it |
| **Play** | F5 | runs on, with the Delay between steps, until a breakpoint, a failure or the end |
| **Stop** | Esc | ends the run and closes the app |
| **Restart** | Ctrl+R | the scenario again, from the top |

The run starts **paused before the first step**, so a speaker can say what is about to happen and
then press Space. A clicker works because it sends PageDown. A call is a position of its own, as
in the engine's `BddBreakpoints`.

A **context menu on a step** in the list:

- **Run until here**: from where the run is (or from the start), run on and pause before this
  step. The usual way to get to the interesting part of a demo.
- **Run from here**: start at this step, skipping the ones before it. Only safe when the app is
  already in the state those steps would have made, so it is offered while a run is paused, or
  says so. (Open question Q6.)
- **Toggle breakpoint**: the same `bdd-breakpoints.json` the IDEs write, so a breakpoint set in
  either IDE stops Play in Presenter too.

Presenter runs the engine in its own process, so the moves drive `BddStepExecutionSession`
directly; only the breakpoints are shared through the file.

Fixes U7 and U8.

### P3. The bar and its two modes

Presenter is a **narrow bar docked left or right** (a toggle, remembered), full height. The app
under test is placed in the rest of the screen, for every target:

| Target | How it is placed |
| --- | --- |
| MAUI | as today (`BRINELL_AUT_PLACE_RIGHT`), plus the left side |
| WPF, WinForms | the same placement, by window handle |
| Blazor, HTML | the browser is started with its window position and size (Playwright's launch options) |

Two modes, one toggle:

- **Present** (the default when a workspace loads cleanly): the scenario tree, the step list and
  the step bar.
- **Workspace**: Open, Build, configuration and platform, Config, Diagnostics, Discovery,
  Command Catalog. Where you land when the workspace has errors, with one line and one button
  ("Not built yet: **Build**") above the details. One **Reload**; Validate goes.

Fixes U3, U4, U5 and U11.

### P4. Record a run (deferred)

Later: write a Presenter run the way `[UatScenarioTheory]` does (a run folder under the suite's
`TestResults`, the `uat-scenario` record, a screenshot per step), so the IDE preview shows its
checks, deleting old results works on it, and a validation has evidence; plus a tester's
**Looks wrong** with a note. Not in this update.

### P5. Load and bind the way the IDEs do; smaller things

**Built 2026-09-29 (step 1).** Presenter bound every file against the spec catalog only
(`UatSpecCommandCatalog`), not the suite's own catalogs, so it could pass a step the gate fails and
missed the value checks (`UATA`), broken callables, `Using`s, heads and reserved names. (Step-file
calls did bind: `BddScenarioFiles.Load` brings the step files; the earlier draft of this doc was
wrong there.) Now `BddWorkspaceService` opens the suite with `BddSuite` (`Brinell.Bdd.Workspace`,
from a shadow copy) and checks each feature and step file with `BddFileAnalysis`, as the language
server does:

- the tree binds against the suite's catalogs, a call's steps included;
- the Diagnostics tab lists the gate's diagnostics with their severity and code;
- an unbuilt suite is parsed only, with one error naming why ("Build the UAT project first");
- a file under `ExpectedFailures` reports "expected failure", not an error;
- `bdd.config.md` is found, and the Config tab names the file it edits.

Tests: `BddWorkspaceServiceTests` (the same codes as `BddFileAnalysis` for every Todo file, a call
bound; the expected failure; the unbuilt suite); Presenter's UI scenarios pass.

Still open: a **run** loads the build through Presenter's own `BddRuntimeAssemblyResolver` and
binds with the live fixture's catalogs, which is what the xUnit gate does at run time too; only the
checking went through `Brinell.Bdd.Workspace`.
Smaller:
- Move the UI-test text mirrors out of the layout (a zero-size container, or read the same values
  through page objects). Fixes U10.
- The app icon (plan.md item 1): the files exist in `Resources/AppIcon`, still to check in the
  taskbar, Alt-Tab and the title bar.

## 3. Sketch: Present mode, docked left

```text
┌ Presenter ──────┐┌ the app under test (or a browser) ──────────┐
│ ⚙ ⇄ ☾          ││                                              │
│ ▾ Scenarios     ││                                              │
│   ▸ Todo        ││                                              │
│     ▾ TOD.01    ││                                              │
│       ● Add one ││                                              │
│       ○ Complete││                                              │
│ ▾ Steps         ││                                              │
│   ✓ Given the a…││                                              │
│   ▶ When I add …││  ← hover shows the full step                 │
│     Then the li…││                                              │
│ ▶ ⤼ ↘ ↗ ⏵ ■ ↺  ││                                              │
│ ⏸ Before step 2 ││                                              │
│ Delay 800 ms  ⧉ ││  ⧉ opens the Scenario Preview                │
└─────────────────┘└──────────────────────────────────────────────┘
```

⚙ switches to Workspace mode, ⇄ docks the bar on the other side.

## 4. Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | P5: load and bind through `Brinell.Bdd.Workspace` and `BddFileAnalysis`. **Done** 2026-09-29. | Presenter's diagnostics for TOD.01 equal the language server's; a `## Step:` call binds. |
| 2 | P1: scenario tree and step list in expanders, tooltips, the preview on demand. | TOD.01's steps read in full on hover; the preview opens and follows a run. |
| 3 | P2: pause-before stepping, keys, clicker, the step menu, shared breakpoints. | PR11 rewritten for it passes; a breakpoint set in VS stops Play; Run until here pauses at the step. |
| 4 | P3: the docked bar, placement for every target, Present and Workspace modes. | TOD.01 presented beside its app, and a Blazor scenario beside its browser, without help. |
| 5 | P5's smaller things, and [userjourney.md](userjourney.md) updated for the new UI. | The journeys pass. |
| - | P4, recording. | Deferred. |

## 5. Open questions

| # | Question | Default |
| --- | --- | --- |
| Q1 | Is a WebView acceptable for the preview window (WebView2 on the machine)? | **Agreed**: WebView; the renderer and its CSS are shared, and every Windows 11 machine has WebView2. |
| Q2 | Should the step also show as an overlay next to the app under test? | **Agreed**: not in the first cut. |
| Q3 | The tester's verdict: per step, or per scenario? | **Agreed**: per step, the scenario takes the worst. Moves with P4. |
| Q4 | A pure manual checklist without automation? | **Agreed**: no, Presenter always drives the app. |
| Q5 | Keep the Delay box, or slow / normal / fast? | **Agreed**: keep ms, add the three as presets. |
| Q6 | "Run from here" skips steps: allow it on a fresh run (the app may be in the wrong state), or only while paused in a run? | Only while paused in a run; otherwise the menu item is off and its tooltip says why. |
| Q7 | Dock side: left by default? | Left, as Presenter opens today. |
