# Presenter Config Edit Screen

Status: Proposed — **partly superseded by [02](02-project-anchored-workspace.md)**; the field model and delivery plan below are revised there
Date: 2026-09-22
Area: `srcnew/Brinell.Presenter`, `srcnew/Brinell.Uat`, `testsnew/Brinell.Presenter.Uat.Tests`
Related:

- [Anchoring the workspace on the UAT project](02-project-anchored-workspace.md) — **read this first**: most fields below become derived, and the Assemblies table goes away
- [Study: projects instead of binaries](01-assembly-and-app-path-resolution.md) — the measurements behind that change
- [Presenter current UI and execution design](../../10%20presenter%20tabbed%20tree%20redesign.md) — the shell this screen lives in; where we disagree, 10 wins on layout
- [Running the Todo app in Presenter](../../guide/todo-app-presenter-setup.md) — the three-blocker story this screen exists to end
- [Minimal Presenter run](../../guide/README.md) — the fixture-name trap
- [Presenter platform guide](../../../../docs/platform-guides/presenter.md)
- [UAT markdown grammar](../../02%20uat%20markdown%20grammar.md)

---

## Why

Opening a workspace whose `uat.config.md` is wrong is the normal first experience,
not the exceptional one. Today Presenter can only *describe* the problem:
`UatWorkspaceConfigInspector.Inspect` produces good diagnostics, the Config tab
renders them as a read-only `Label`
([PresenterPage.xaml:360](../../../../srcnew/Brinell.Presenter/Views/PresenterPage.xaml#L360)),
and then you leave the app, hand-edit a Markdown table, come back, and reload.

The Todo setup guide is three pages of exactly that loop — a fixture name that
matches no type, a `Pages` assembly that does not contain the fixture, a stale
`win10-x64` RID folder. Every one of those is a single field that Presenter
already knows how to validate and could have offered a correct value for.

This design turns the Config tab from a report into an editor: the same
diagnostics, attached to the field that caused them, with a picker where a value
can be discovered and a **Save** that writes the Markdown back.

### What it must not become

- Not a general Markdown editor. The file stays hand-editable; this edits the
  *known* fields and preserves everything else byte-for-byte.
- Not a modal wizard that blocks the tree. The Presenter's job is to sit narrow
  beside the app under test; a full-screen settings dialog fights that.
- Not a second source of truth. `UatConfigParser` stays the only reader, and the
  new writer round-trips through it.

## Scope

In scope: the six sections `UatConfigParser` understands — `Runtime`,
`Assemblies`, `Discovery`, `Reporting`, `Settings`, `Skip Rules` — plus creating
a `uat.config.md` that does not exist yet.

Out of scope: editing `.uat.md` scenarios, editing `testsettings.json`,
Presenter's own user settings (theme, recent folders — those are
`PresenterUserSettings`, a different file with a different lifetime).

---

## Where it lives

The Config tab, in place. It is already the tab you land on when a workspace
fails to load, and it already carries the config's identity in the tab strip.

```markui
+--- Brinell Presenter --------------------------------------+
| [#1 Open][v] [#2 Reload] [#3 Validate]           [#4 Theme] |
| +--- Run ------------------------------------------------+ |
| | [Run] [Stop] [Next]   Delay <250__> ms                 | |
| +--------------------------------------------------------+ |
| +--[Tree]--[[Config]]--[Diagnostics]--[Discovery]--[Cmd]--+ |
| | uat.config.md            3 problems   * unsaved        | |
| |                                                        | |
| | v Runtime                                              | |
| |   Target        [MAUI            v]                    | |
| |   Fixture       <TodoUatFixture____> [#5 Find]         | |
| |     ! no type named 'FlaUI' in the Pages assembly      | |
| |   AppPath       <../../src/...exe__> [#6 Browse]       | |
| |     ! not found: E:\...\win10-x64\...exe               | |
| |   WorkingDir    <../../../..______> [#7 Browse]  ok    | |
| |                                                        | |
| | > Assemblies (3)                                       | |
| | > Discovery                                            | |
| | > Reporting                                            | |
| | > Settings                                             | |
| | > Skip Rules (0)                                       | |
| |                                                        | |
| | [#8 Save] [#9 Revert]        [#10 Open in editor]      | |
| +--------------------------------------------------------+ |
+------------------------------------------------------------+
```

Sections are collapsible and collapsed by default except the first one holding a
problem. At quarter-screen width labels sit above their editors instead of
beside them; nothing scrolls horizontally.

Each collapsible section header follows the WP3 "Route A" rule already in
`PresenterPage.xaml`: the header contains a real `Button`, because the toolkit
`Expander` publishes no ExpandCollapse or Invoke pattern on Windows and would
otherwise be unreachable from the UAT suite that drives this app.

---

## Fields

### Runtime

| Field | Editor | Validation | Assist |
| --- | --- | --- | --- |
| `Target` | `Picker` over `UatTargetRegistry.SupportedTargets` | closed set — cannot be invalid | — |
| `Fixture` | `Entry` + **Find** | resolved by reflection over the Pages assembly | fixture picker, below |
| `AppPath` | `Entry` + **Browse** | `File.Exists` on the resolved path | file picker rooted at the workspace |
| `WorkingDirectory` | `Entry` + **Browse** | `Directory.Exists`, empty is legal | folder picker |

Paths are shown as written (relative), with the resolved absolute path as the
inline validation line beneath. That mirrors `Inspect`, which already carries
both — `UatWorkspaceConfigLoadResult` has `AppPath` and `ResolvedAppPath` side by
side — and it is what makes a stale RID folder obvious at a glance.

A picker always writes back a path **relative to the workspace folder** when the
chosen file is under the solution root, absolute otherwise. Configs stay portable
by default.

### The fixture picker

This is the feature that earns the screen. Presenter instantiates the fixture by
reflecting over the Pages assembly for a non-abstract type with a parameterless
constructor whose name matches `Fixture` or `Fixture + "Fixture"`
([UatExecutionService.cs:191](../../../../srcnew/Brinell.Presenter/Services/UatExecutionService.cs#L191)).
The xUnit runners resolve it completely differently, so a workspace can pass its
own suite and still fail here. That asymmetry is documented twice in the guides
because people keep hitting it.

**Find** applies Presenter's own rule, in Presenter's own words:

```markui
+--- Fixture in Brinell.Samples.Todo.Uat.dll ----------+
| (o) TodoUatFixture        parameterless, scans pages |
| ( ) TodoListFixture       parameterless              |
| ( ) UatFixtureBase        abstract - not usable      |
|                                                      |
| Searched: bin/Debug/net10.0-windows/...Uat.dll       |
| [Use] [Cancel]                                       |
+------------------------------------------------------+
```

Rules:

- Candidates come from the assembly currently named by the `Pages` row, not from
  a guess. Change `Pages`, and the candidate list changes with it.
- Unusable types are listed with the reason rather than hidden. "There are no
  fixtures here" and "the fixtures here are all abstract" are different problems.
- If the Pages assembly does not exist, the picker says *build it first* and
  names the project — it does not silently return an empty list.
- A type carrying `[TestModuleScan]` is annotated as such, since that attribute
  is what page discovery actually needs.

### Assemblies

A small editable table, the only repeating section that matters day to day.

```markui
| Kind      | Assembly                                   |    |
| [Pages v] | <bin/Debug/net10.0-windows/...Uat.dll_>[..]| x |
| [Contr v] | <../../../../srcnew/...Brinell.Maui.dll>[..]| x |
| [Comm  v] | <../../../../srcnew/...Brinell.Uat.dll_>[..]| x |
| [+ Add]                                                   |
```

- `Kind` is a `Picker` over `Pages`, `Controls`, `Commands` — the kinds the
  runtime consumes — but accepts a typed value so an unknown kind in an existing
  file survives editing.
- Resolution reuses `UatWorkspaceConfigInspector.ResolveAssemblyPath`, including
  its fallback search under the solution root. When the fallback is what found
  the file, say so: *"not at the configured path; found under
  `srcnew/Brinell.Maui/bin/Debug/net10.0/`"* with a **Use this path** action.
  Silent fallback resolution is how a config drifts and then fails on someone
  else's machine.
- Removing the last `Pages` row is allowed but flagged — `Inspect` already treats
  an empty assembly list as an error.

### Discovery, Reporting, Settings, Skip Rules

| Section | Fields | Editor |
| --- | --- | --- |
| Discovery | `RequireExplicitUatAttributes`, `AllowNameInference` | `CheckBox` |
| Reporting | `ScreenshotOnFailure`, `IncludeRuntimeTrace` | `CheckBox` |
| Reporting | `OutputDirectory` | `Entry`, with `$(BrinellTestResults)` left literal |
| Settings | `Root`, `DefaultFile`, `LocalFile`, `ScenarioConvention` | `Entry` |
| Skip Rules | `Tag`, `EnvironmentVariable` rows | table, like Assemblies |

Every one of these has a default in `UatConfig` (`UatSettingsConfig.Default`,
`UatDiscoverySettings`'s optional parameters). A field left at its default and
absent from the file **stays absent** — the editor shows the default as
placeholder text, greyed, and only writes a row once the value differs. Saving
must not turn a six-line config into a forty-line one.

`OutputDirectory` deserves a note in the UI: it is expanded against the artifact
provider at parse time, so the resolved directory shown beneath it is
informational, and `$(BrinellTestResults)` is a token, not a broken path.

---

## State and validation

Three states, one indicator in the tab header:

| State | Header | Save | Run |
| --- | --- | --- | --- |
| Clean | `uat.config.md` | disabled | normal |
| Dirty | `uat.config.md  * unsaved` | enabled | prompts to save first |
| Invalid | `uat.config.md  3 problems` | **enabled** | blocked, as today |

Save stays enabled while invalid. Half-fixing a config and saving is a legitimate
move, and refusing to write is how an editor loses people's work. What is blocked
is *running* an invalid config, which is already true.

Validation runs on every field change, not on Save, and it is the same
`UatWorkspaceConfigInspector.Inspect` the rest of the app uses — against the
in-memory edited config rather than the file on disk. That needs one refactor:
`Inspect(string workspacePath)` reads the file itself, so it gains an
`Inspect(string workspacePath, UatConfig config)` overload and the existing
signature becomes a two-line wrapper over `ParseFile`. No second validation
implementation, ever — the diagnostics on this screen must be the same strings
the Diagnostics tab prints.

Field-level attribution: diagnostics today are flat strings. Give
`UatWorkspaceConfigLoadResult` a diagnostic record carrying a section and field
name (`Runtime`/`AppPath`, `Assemblies`/row index) alongside the message, so the
editor can place it. The flat list the Diagnostics tab renders is then a
projection of the same records.

External change: if the file on disk changes while the editor is dirty — someone
edits it in VS Code, which the **Open in editor** button actively encourages —
Save must not clobber it. Hold the modification timestamp read at load; on Save,
if it moved, show *"changed on disk"* with **Keep mine** / **Reload theirs**. No
merge attempt.

---

## Writing the file

There is no config writer anywhere in the tree today — `UatConfigParser` is
read-only. This is the one genuinely new piece of engine code, and it belongs
next to the parser in `Brinell.Uat`, not in Presenter: a headless tool or an
`--init` command will want it too.

```csharp
public static class UatConfigWriter
{
    // Rewrites only the rows the edit produced; every other byte survives.
    public static string Apply(string markdown, UatConfigEdit edit);

    public static string Create(UatConfigTemplate template);
}
```

Preservation rules, in priority order:

1. **Unknown sections and prose survive untouched.** A config with a `## Notes`
   heading, a comment, or a section a newer Brinell added keeps them, in place.
2. **Unchanged rows are not rewritten.** Column padding, alignment and cell
   spacing are per-row properties of the existing text.
3. **A changed row keeps its row position** and matches the column widths of its
   neighbours, so a save produces a one-line diff, not a reformatted table.
4. **A new row is appended** to its section's table; a new section is appended in
   the parser's canonical order (`Runtime`, `Assemblies`, `Discovery`,
   `Reporting`, `Settings`, `Skip Rules`).
5. **A removed row leaves no blank line.**
6. Line endings and trailing newline follow the file as found.

The verification that matters: `Parse(Apply(markdown, edit))` equals the edited
config, **and** `Apply(markdown, noOpEdit)` returns `markdown` unchanged, for
every config in the repo. That second property is the cheap one to test and the
one that keeps hand-written configs hand-editable. Drive it from the real files
under `samples/` and `testsnew/`.

Save writes through a temporary file and replaces, so a crash mid-write cannot
leave a truncated config.

---

## Startup: when there is no config

`Inspect` currently returns a not-loaded result whose only diagnostic is *"uat.config.md
was not found"*. That is the first thing many people see, because Presenter
reopens `LastOpenedFolder` or falls back to `testsnew/Brinell.Maui.Uat.Tests`
before anything is built.

Give that state one action:

```markui
+--- Config ------------------------------------------+
| No uat.config.md in                                  |
| samples\Todo\tests\Brinell.Samples.Todo.Uat          |
|                                                      |
| Target for this workspace  [MAUI v]                  |
| [#1 Create uat.config.md]                            |
+------------------------------------------------------+
```

**Create** writes a minimal, honest config for the chosen target — `Target` set,
the other required fields present and empty — then opens the editor with the
first empty field focused and its diagnostic already showing. The generated file
contains only required fields; defaults stay implicit, per the rule above.

It does not guess `AppPath` or `Fixture`. A wrong guess that looks authoritative
costs more than an empty field next to a **Browse** button. It *may* pre-fill
`Pages` when exactly one `*.dll` under the workspace's own `bin/` contains a
usable fixture — one candidate, no ambiguity, and the fixture picker is right
there to correct it.

---

## Commands and AutomationIds

Presenter is driven by its own UAT suite, so every control here needs an
`AutomationId` and a page-object member from day one — no test may reach for the
driver.

| Control | AutomationId | Notes |
| --- | --- | --- |
| Section header buttons | `ConfigSection{Name}Button` | Route A: real `Button` inside the `Expander` header |
| Target | `ConfigTargetPicker` | `Picker` |
| Fixture | `ConfigFixtureInput` | `Entry` |
| Fixture find | `ConfigFixtureFindButton` | opens the candidate list |
| Fixture candidates | `ConfigFixtureCandidateList` | `CollectionView`, item ids `ConfigFixtureCandidate{Index}` |
| AppPath | `ConfigAppPathInput` / `ConfigAppPathBrowseButton` | |
| WorkingDirectory | `ConfigWorkingDirectoryInput` / `ConfigWorkingDirectoryBrowseButton` | |
| Assembly rows | `ConfigAssemblyKind{Index}` / `ConfigAssemblyPath{Index}` / `ConfigAssemblyRemove{Index}` | |
| Add assembly | `ConfigAssemblyAddButton` | |
| Booleans | `ConfigRequireExplicitUatAttributes`, `ConfigAllowNameInference`, `ConfigScreenshotOnFailure`, `ConfigIncludeRuntimeTrace` | `CheckBox` |
| Settings entries | `ConfigSettingsRootInput`, `ConfigSettingsDefaultFileInput`, `ConfigSettingsLocalFileInput`, `ConfigSettingsScenarioConventionInput` | |
| Save / Revert | `ConfigSaveButton` / `ConfigRevertButton` | |
| Create | `ConfigCreateButton` | no-config state only |
| Per-field diagnostic | `ConfigDiagnostic{Section}{Field}` | `Label`, empty when valid |
| Header state | `ConfigStateLabel` | "3 problems" / "unsaved" / clean |

Every control type this needs already exists in `Brinell.Maui`: `Entry`,
`Picker`, `CheckBox`, `Button`, `Label`, `CollectionView`. Nothing new to
generate.

## Tests

Presenter dogfoods the UAT runner, so the screen arrives with scenarios, not just
unit tests.

`srcnew/Brinell.Uat` unit tests (`Brinell.Uat.Tests`):

- round-trip: `Parse(Apply(md, edit))` matches the edit, per section;
- identity: `Apply(md, noOp)` is byte-identical, over every config in the repo;
- preservation: unknown section, prose between tables, CRLF, missing trailing
  newline, ragged column widths;
- defaults: setting a field to its default value does not add a row; clearing a
  non-default row removes it.

`Brinell.Presenter.Uat.Tests` — new `PresenterConfigEditor` page object, new
scenarios:

- `edit-fixture-name.uat.md` — open a workspace with a bad fixture, see the
  diagnostic on the field, use **Find**, pick the real type, save, reload, run.
  This is the Todo guide's blocker 1, as an executable test.
- `create-missing-config.uat.md` — empty folder, **Create**, editor opens with
  required fields flagged.
- `external-change-conflict.uat.md` — dirty editor, file changes on disk, both
  branches of the prompt.

The first of those is the acceptance test for the whole design: if a newcomer can
fix the Todo workspace without leaving Presenter, the screen worked.

---

## Delivery

| Slice | Contents | Done when |
| --- | --- | --- |
| 1 | `UatConfigWriter` + unit tests, no UI | round-trip and identity green over every repo config |
| 2 | `Inspect` overload, field-attributed diagnostics | Diagnostics tab output unchanged |
| 3 | Runtime section editable, Save/Revert, dirty state | Todo config fixable in-app except `Pages` |
| 4 | Assemblies table, fallback-path action | blockers 2 and 3 fixable in-app |
| 5 | Fixture picker | `edit-fixture-name.uat.md` green |
| 6 | Remaining sections, Create, conflict handling | all three scenarios green |

Slices 1 and 2 are invisible and independently verifiable; do not skip ahead to
the UI on top of a writer that has not been proven to leave hand-written files
alone.

## Open questions

1. **Does Save imply Reload?** A saved config that now resolves should probably
   reload the workspace — but reload collapses the tree and drops selection.
   Leaning: save, then offer reload in the header rather than doing it.
2. **Revert granularity** — whole file, or per field? Whole file is simpler and
   matches the dirty/clean model; per field is what people actually want after a
   mis-click in a picker.
3. **Should the raw Markdown stay visible?** A read-only "source" toggle beneath
   the editor costs little and keeps the file format honest — it is the thing
   that stops the editor from feeling like a black box. Not in the slices above.
4. **`ScenarioConvention` has real syntax** (`scenarios/{ScenarioId}.json`). A
   plain `Entry` accepts nonsense. Worth validating the token set, or worth
   leaving until someone breaks it?
