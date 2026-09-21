# Todo UAT - plan: user journeys + Todo.Uat on the new UAT structure

Goal: two connected deliverables for the Todo showcase.

1. **A user-journey checklist** for the Todo app, written in the format of
   `construction/.github/prompts/generate-module-user-journeys.prompt.md` - a plain,
   executable checklist of user-visible behaviour, not a tier-allocation table.
2. **`Brinell.Samples.Todo.Uat`** - the phase 6 project that plan.md left optional -
   implementing those journeys as `.uat.md` scenarios on the **new `[UatStep]`
   attribute-driven UAT structure** (AD-011), bound to the Todo page objects that already
   exist.

Status: plan / ready to execute.
Date: 2026-09-21
Area: `samples/Todo/` (journeys doc + new `Brinell.Samples.Todo.Uat` project).

Related:

- [plan.md](plan.md) - the showcase plan; phase 6 (Gherkin/UAT specs) was deferred here.
- [samples/Todo/journeys.md](../../samples/Todo/journeys.md) - the pyramid **allocation**
  backbone (`TOD.xx.y` + tiers). The behaviour inventory for deliverable 1 comes from here.
- [samples/Todo/uat-walkthrough.md](../../samples/Todo/uat-walkthrough.md) - already
  describes one Todo UAT end to end. This plan makes it real and reconciles it with the new
  structure.
- `.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md` - the new UAT vocabulary
  is `[UatStep]` on `Brinell.Core` interfaces, discovered by the engine; custom verbs are
  `[UatStep]` on app control methods.
- Reference UAT project: [testsnew/Brinell.Maui.Uat.Tests](../../testsnew/Brinell.Maui.Uat.Tests)
  (`uat.config.md`, `Scenarios/*.uat.md`, `Runtime/*`).

---

## 0. What the two artifacts are, and how they differ from journeys.md

`journeys.md` is a **tier-allocation** document: a table per journey mapping each subtask to
the lowest pyramid tier that can prove it. It is the input to the coverage report. It is *not*
the checklist the construction prompt describes.

The construction prompt produces a different artifact: a **pure, executable journey
checklist** - `## <PREFIX>NN Title`, one `- [ ] **Journey passed**` parent, and
`- [ ] **<PREFIX>NN.n** observable action or result` subtasks, with `[!]`/`[-]` conventions
for fail/not-applicable. It contains *only* journeys and subtasks: no tiers, no matrix, no
implementation notes.

So deliverable 1 is a **new** document, not an edit of `journeys.md`. The two coexist:
`journeys.md` decides *where* a behaviour is tested; the journey checklist states *what* the
user must be able to do and is what a tester (or the UAT scenarios) executes.

---

## Part A - the user-journey checklist

### A.1 Output location and name

Following the prompt's `<module>/<module-kebab-case>-user-journeys.md` convention, adapted to
this repo:

```text
samples/Todo/todo-user-journeys.md
```

Linked from `samples/Todo/README.md` (next to the existing links to `journeys.md` and
`uat-walkthrough.md`).

### A.2 Numbering and ids - the one deviation to settle

The prompt says "shortest clear uppercase module prefix" (it would pick `T` or `TODO`) with
`## T01` / `T01.1`. But `journeys.md`, every `[Trait("Journey", "TOD.xx.y")]`, and the
coverage report already use `TOD.<journey>.<subtask>`.

**Decision for this plan:** keep the existing `TOD` prefix and the dotted `TOD.NN.n` subtask
ids in the checklist, and adopt the prompt's *structure* (headings, parent checkbox, nested
checkboxes, `[!]`/`[-]` legend). This keeps one id space across `journeys.md`, the traits, the
report and the new checklist. The only divergence from the prompt is the id punctuation, which
is deliberate and worth a one-line note in the doc.

### A.3 Source of behaviour (the prompt's "private inventory")

The prompt requires the inventory be built from source before writing, and never invented from
UI code. Here the source is already distilled: the behaviour inventory is the `TOD.01`-`TOD.10`
journeys in `journeys.md`, cross-checked against the running app (the app is fully built,
phases 0-8). No new discovery is required; the work is **reformatting the existing 10 journeys
into the checklist shape** and confirming each subtask is a single observable action/outcome.

The 10 existing journeys already fit the prompt's "6-15 journeys, 4-9 subtasks each" guidance
(they range 4-9). They map directly:

| Journey | Title | Subtasks |
| --- | --- | --- |
| TOD.01 | See my todos | 6 |
| TOD.02 | Create a todo | 6 |
| TOD.03 | Read a todo | 4 |
| TOD.04 | Edit a todo | 5 |
| TOD.05 | Change status | 5 |
| TOD.06 | Delete a todo | 5 |
| TOD.07 | Sync with the backend | 9 |
| TOD.08 | Things go wrong | 8 |
| TOD.09 | Persistence and startup | 4 |
| TOD.10 | Look and accessibility | 4 |

TOD.07 (9) is at the ceiling; leave it, since its subtasks are one goal (sync). Subtasks phrased
with a tier reason in `journeys.md` (e.g. "…, U for the rule; H for the Switch") get rewritten
as pure user-visible statements (the tier reason drops out - it lives in `journeys.md`).

### A.4 Steps to produce it

1. Copy the prompt's output legend verbatim (the "Check a journey only after all its subtasks
   pass. Use `[!]` … `[-]` …" block) and the `# Todo User Journeys` title.
2. For each `TOD.NN` journey: heading `## TOD.NN Title`, one `- [ ] **Journey passed**`, then
   each subtask as `- [ ] **TOD.NN.n** <observable statement>` - the statement taken from the
   `journeys.md` "Subtask" column with the tier rationale stripped.
3. Add the one-line note explaining the `TOD.NN.n` id punctuation deviation (A.2).
4. Validate per the prompt's Step 6: ids unique/sequential/nested, every journey 4-9 subtasks,
   no tiers or matrices leaked in, Markdown clean.

Output contains **only** the legend + journeys. No inventory, no coverage map (that is
`journeys.md`'s job), matching the prompt's Step 5 constraints.

---

## Part B - `Brinell.Samples.Todo.Uat` on the new structure

### B.1 How the new structure changes what we build

Under AD-011 the built-in step vocabulary is `[UatStep]` on `Brinell.Core` interfaces and is
discovered by the engine - **nothing to author** for the common verbs. What a project supplies is:

- **`uat.config.md`** - app path + which assemblies carry pages/controls/commands.
- **`Scenarios/*.uat.md`** - the Given/When/Then files.
- **A fixture** (`TFixture`) that launches the app in the right state.
- **A scenario runner** deriving from `UatScenarioTestBase<TFixture>`.
- **Custom Todo verbs** *only where a journey needs a phrase the built-ins do not cover* -
  added as `[UatStep]` on the Todo ControlObjects (the headline feature of the new structure).

Control/page **names** still resolve by inference (`AllowNameInference`) or `[UatName]`, exactly
as `uat-walkthrough.md` already shows; that mechanism is unchanged by AD-011.

### B.2 Verb coverage - built-in vs custom

Mapping the journeys' actions to phrases. Built-ins come free from `Brinell.Core`; the rest are
new `[UatStep]` on Todo ControlObjects.

| Journey action | Phrase | Source |
| --- | --- | --- |
| tap Add / Sync / Save / Cancel / Edit / Delete / Retry | `I tap {control}` | built-in |
| enter / set title, notes | `I set {control} to {value}`, `I enter {value} into {control}` | built-in |
| clear title | `I clear {control}` | built-in |
| choose a filter | `I select {value} from {control}` | built-in (Picker; Windows only - see B.6) |
| toggle the due-date switch | `I check {control}` / `I uncheck {control}` | built-in |
| error/label text | `{control} should contain {value}`, `{control} should equal {value}` | built-in |
| control visible / hidden / enabled | `{control} should be visible` / `… not be visible` / `… be enabled` | built-in |
| **advance status (Next)** | `I advance {control}` | **custom** `[UatStep]` on `TodoStatus` (`Advance`) |
| **assert a status** | `{control} should show status {value}` | **custom** `[UatStep]` on `TodoStatus` (`AssertStatus`) |
| **wait for a list state** (Empty/Error/Loading/Content) | `{control} should be in state {value}` | **custom** `[UatStep]` on `TodoListState` |
| **pending marker present/absent** | `{control} should be visible` / `… not be visible` on the row's mark | built-in (the mark is a control member) |
| **assert a row exists / gone** | `{control} should contain {value}` on the list, or a custom `{control} should have row {value}` | **custom** if a row-presence phrase reads better than a text-contains |

Custom verbs live next to the method they call, e.g. in
`samples/Todo/tests/Brinell.Samples.Todo.UITests/Controls/TodoStatus.tpl.cs`:

```csharp
[UatStep(UatEffectiveStepKeyword.When, "I advance {control}")]
public TScope Advance() { … }

[UatStep(UatEffectiveStepKeyword.Then, "{control} should show status {value}")]
public TScope AssertStatus(string? expected, string? message = null, int? timeoutMs = null) { … }
```

These reference `Brinell.Core.Testing` (already available; the UITests project references Core)
and are discovered because the controls are reachable from the page objects the runtime scans.
This is the concrete demonstration the new structure is for: **a project-specific verb added
with one attribute, no engine edit, no phrase table.**

> Keep custom verbs to the few that read better than a built-in. Prefer a built-in where one
> fits (e.g. assert the pending mark with `should be visible`, not a new verb).

### B.3 Project layout

```text
samples/Todo/tests/Brinell.Samples.Todo.Uat/
  Brinell.Samples.Todo.Uat.csproj      net10.0-windows; refs Todo.UITests (pages+controls),
                                        Brinell.Uat, Brinell.Maui, Brinell.Mocking
  uat.config.md                         points at the Todo app + the UITests page assembly
  Runtime/
    TodoUatCollection.cs                [CollectionDefinition] over the fixture
    TodoUatScenarioTests.cs             UatScenarioTestBase<TodoUatFixture>, [Theory] over Scenarios
    TodoUatFixture.cs                   the hermetic fixture (see B.4)
  Scenarios/
    create-needs-title.uat.md           TOD.02.3   (the walkthrough's scenario)
    create-todo.uat.md                  TOD.02.2
    read-todo.uat.md                    TOD.03.1
    edit-todo.uat.md                    TOD.04.2
    cancel-dirty-edit.uat.md            TOD.04.4
    change-status.uat.md                TOD.05.1 / .05.5
    delete-todo.uat.md                  TOD.06.1 / .06.2
    filter-list.uat.md                  TOD.01.4  (Windows-only, Picker)
    empty-list.uat.md                   TOD.01.3
    server-error-retry.uat.md           TOD.08.1 / .08.2
  TestSettings/
    testsettings.json
  ExpectedFailures/                     (optional) one intentionally-failing file to show diagnostics
```

Add the project to `Brinell.Samples.Todo.slnx`.

### B.4 The fixture - reconciling UAT with Todo's hermetic backend (the crux)

The reference `MauiFixture` just launches an app. Todo needs more before launch: a seeded
SQLite DB, a WireMock backend, a pinned clock and the `TODO_*` launch settings. That work
already exists in `TodoAppFixtureBase` / `TodoAppFixture`
([tests/Brinell.Samples.Todo.UITests/TodoAppFixtureBase.cs](../../samples/Todo/tests/Brinell.Samples.Todo.UITests/TodoAppFixtureBase.cs)),
which derive from `MauiTestFixtureBase` and set everything in `CreateTestContextOptions()`
before the base launches. `UatScenarioTestBase<TFixture>` takes any `TFixture` and hands it to
the runtime, which uses the fixture's launched context.

**Plan: `TodoUatFixture` extends the existing hermetic `TodoAppFixture`.** It inherits the
backend + DB + launch-settings setup unchanged; the UAT layer sits on top. Two things to wire:

1. **Per-scenario isolation.** The Todo hermetic tier resets between tests with `StartTest()`
   (network online, backend overrides cleared, scenario server state restored, list on screen).
   The UAT base has a `BeforeScenario(UatBoundScenario)` hook - override it to call the
   fixture's `StartTest()` so each `.uat.md` scenario starts from the known state.
2. **`RuntimeValidation`** returns `new(Target: "MAUI", Fixture: "Appium")` (or "FlaUI" for the
   Windows head), matching `uat.config.md`.

This means **no new Brinell framework change** is needed for Windows: everything rides on the
existing Todo fixture and the shipped UAT engine. (Contrast B1 in plan.md, which is only for
Android intent extras - out of scope here; this project is Windows-first like the walkthrough.)

### B.5 `uat.config.md`

As drafted in `uat-walkthrough.md` §4, verified against the real reference config
([testsnew/Brinell.Maui.Uat.Tests/uat.config.md](../../testsnew/Brinell.Maui.Uat.Tests/uat.config.md)):

- `Target = MAUI`, `Fixture = Appium` (or the Windows FlaUI fixture the Todo tests use - match
  what `TodoAppFixtureBase` launches).
- `AppPath` → the Todo app's win-x64 exe.
- `Pages` → `Brinell.Samples.Todo.UITests.dll` (carries the page objects **and** the custom
  `[UatStep]` controls).
- `Commands` → `Brinell.Uat.dll`.
- `Discovery`: `RequireExplicitUatAttributes = false`, `AllowNameInference = true` - so
  `AddButton` resolves as **Add**, etc.
- `Skip Rules`: `live-api → BRINELL_UAT_LIVE_API` (a couple of live scenarios can be tagged and
  skipped by default, mirroring the UITests live tier).

Verify the `Pages` assembly path's TFM segment against the actual build output before running
(the reference uses `net10.0-windows7.0`; the Todo UITests may differ).

### B.6 Which journeys become scenarios (and which cannot, honestly)

The point of the phase is to show the same journey text driving the app through page objects, so
pick journeys whose behaviour is **UI-observable and hermetic** (WireMock + seeded DB, no real
device state):

- **Include** (hermetic, Windows): TOD.01.3/.01.4, TOD.02.2/.02.3, TOD.03.1, TOD.04.2/.04.4,
  TOD.05.1/.05.5, TOD.06.1/.06.2, TOD.08.1/.08.2. These cover create, read, edit, status,
  delete, empty/error states and filtering.
- **Tag `live-api` and skip by default**: any scenario that asserts the real wire (TOD.07.4
  style). Optional; only if a live UAT is wanted alongside the UITests live tier.
- **Do not author** as UAT: the manual-only (`TOD.08.6`, `TOD.10.4`), the pure-unit rule tables
  (`TOD.05.2` Overdue theory), and Android-blocked behaviours. The journey checklist still lists
  them; the UAT project just does not claim them. Note this in the project README so the gap is
  explicit, not silent (per AGENTS.md: list gaps, don't work around them).

Filtering (TOD.01.4) uses a `Picker`, which is Windows-reachable but not on Android
(`PickerGap` in plan.md phase 7); keep it Windows-only and say so.

### B.7 Reconcile the existing walkthrough

`uat-walkthrough.md` already documents this project as if it exists. After implementation:

- Confirm every phrase it shows (`I tap {control}`, `I clear {control}`,
  `{control} should contain {value}`, `{control} should be visible`) resolves from the shipped
  `[UatStep]` catalog - they do (all built-in).
- Update the one place that predates AD-011 if it implies a hand-written command table; the
  phrases now come from `Brinell.Core` discovery, not a registered list.
- Point the walkthrough's file references at the real `Scenarios/*.uat.md` once created.

---

## C. Phases

Each ends green in the smallest scope that can falsify it; the full Todo UI/UAT run happens at
the end. One UI/UAT process at a time (per AGENTS.md and the Todo suite's own rule).

| Phase | Deliverable | Done when |
| --- | --- | --- |
| **A. Journey checklist** | `samples/Todo/todo-user-journeys.md` (Part A) | Validates against the prompt's Step 6; linked from the Todo README. No code. |
| **B0. Custom verbs** | `[UatStep]` on `TodoStatus` (`Advance`, `AssertStatus`) and `TodoListState` (state) in the UITests project | `Brinell.Samples.Todo.UITests` builds; a `Brinell.Uat.Tests`-style discovery check (or a tiny xUnit test in the Uat project) shows the custom phrases in the catalog. |
| **B1. Project skeleton** | `Brinell.Samples.Todo.Uat` project, `uat.config.md`, `TodoUatFixture`, `TodoUatCollection`, `TodoUatScenarioTests`, one scenario (`create-needs-title.uat.md`) | The single scenario runs green on Windows through the real app. |
| **B2. Scenario set** | The B.6 hermetic scenarios | All included scenarios green; per-scenario `StartTest()` isolation holds across a full run (run the project 2-3x for stability, as the UITests phase did). |
| **B3. Reconcile + close** | Update `uat-walkthrough.md`; add a "UAT tier" note + gap list to `samples/Todo/README.md`; optional `ExpectedFailures` file | Walkthrough matches reality; gaps (manual/unit/Android/live) listed; README links the journey checklist. |

Phase 6-live and Android UAT are explicitly **out of scope** here (Windows hermetic only),
matching the walkthrough and avoiding the Picker/intent-extra work.

---

## D. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| The UAT runtime cannot see the custom `[UatStep]` on Todo controls (discovery scans page-reachable types only) | The custom controls are exposed through the page objects the runtime already scans (`TodoStatus` via detail/edit pages, `TodoListState` via the list page). Add a discovery assertion in B0 before writing scenarios. |
| Per-scenario isolation: UAT runs several scenarios against one launched app; residual state leaks between them | Override `BeforeScenario` to call the existing `StartTest()`; it already leaves any dialog/edit/detail and restores the list + filter + server state. Prove with a 2-3x full run (the UITests phase 4/5 did this). |
| Name inference resolves a control to the wrong member (e.g. two "Save"-like members) | `uat-walkthrough.md` shows `[UatName]` pins the name; apply it where inference is ambiguous. The page objects already have distinct members. |
| `Fixture` value in `uat.config.md` must match what the Todo fixture actually launches (FlaUI vs Appium) | Set `RuntimeValidation` and the config `Fixture` to the head `TodoAppFixtureBase` uses on Windows; verify against a first run rather than assuming "Appium". |
| Picker-based filter scenario is not portable | Keep TOD.01.4 Windows-only and list it as an Android gap, don't force it through a bridge. |
| Coverage report expects `Journey`/`Pyramid` traits; `.uat.md` files carry tags, not xUnit traits | The `[Theory]` runner method can carry `[Trait("Pyramid","UiHermetic")]`; per-file journey ids stay in the scenario `Metadata`/tags. Decide in B1 whether the coverage report should also read UAT (optional - the report today reads the three test assemblies). |

---

## E. Open decisions

| # | Question | Recommendation |
| --- | --- | --- |
| E1 | Journey checklist id punctuation: prompt's `T01.1` vs existing `TOD.01.1` | Keep `TOD.NN.n` for one id space with `journeys.md`/traits/report; note the deviation in the doc (A.2). |
| E2 | Does the coverage report ingest UAT scenarios too? | Out of scope for this plan; leave the report on the three code assemblies. Revisit if UAT becomes a claimed tier. |
| E3 | Live and Android UAT | Out of scope: Windows hermetic only, matching the walkthrough. Tag any live scenario `live-api` and skip by default. |
| E4 | Custom verb surface | Add only `I advance {control}`, `{control} should show status {value}`, and a list-state phrase; assert the pending mark and rows with built-ins where they read well. |

---

## F. Verification (from the Brinell root)

```powershell
# Journey checklist: markdown lint / link check only (no build).

# Custom verbs compile with the app-side controls:
dotnet build samples\Todo\tests\Brinell.Samples.Todo.UITests\Brinell.Samples.Todo.UITests.csproj -v:minimal /nr:false

# The UAT project (build then run the scenarios, Windows head, one process):
dotnet build samples\Todo\Brinell.Samples.Todo.slnx -v:minimal /nr:false
dotnet test samples\Todo\tests\Brinell.Samples.Todo.Uat\Brinell.Samples.Todo.Uat.csproj -v:minimal /nr:false
```

Rebuild the Todo **app** before the UAT run (the test project builds the tests, not the app it
launches). Run the UAT project 2-3 times to confirm per-scenario isolation, as the UITests
phases did.
