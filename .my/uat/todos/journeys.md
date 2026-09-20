# Todo (TOD) — user journeys and UAT scenarios

This folder holds the **executable side** of the Todo journeys: the user-journey
map plus the `.uat.md` scenarios that prove the UI-facing subtasks. It is the
concrete companion to:

- the full journey backbone and pyramid allocation in
  [samples/Todo/journeys.md](../../../samples/Todo/journeys.md);
- the one-scenario walkthrough in
  [samples/Todo/uat-walkthrough.md](../../../samples/Todo/uat-walkthrough.md);
- the phrase catalog in
  [docs/guides/uat-phrases-and-flows.md](../../../docs/guides/uat-phrases-and-flows.md).

Scenarios live in [scenarios/](scenarios/). Each is traceable to a `TOD.xx.y`
subtask and tagged.

> **Presentation note.** In these spec files the steps are shown inside a fenced
> ` ```gherkin ` block so they render as a step list instead of collapsing into
> one paragraph. The Brinell parser wants **bare** step lines (each starting with
> `Given`/`When`/`Then`/`And`/`But`, no fence, no `-` bullet): a list marker or a
> code fence would fail `TryParseStep`. When these move into a runnable
> `Brinell.Samples.Todo.Uat.Tests` project, drop the fence lines and keep the
> steps bare.

---

## The journeys, and what is executable as UAT

Only the **UI-facing** subtasks (tier **H** in the backbone) become UAT. Rule
tables (Unit) and wire/persistence checks (Integration/Contract) do **not** — a
UAT that re-checks a validation matrix through the app is a slower copy of a unit
test. Each journey below lists the scenario file and whether it runs on the
**built-in** phrase vocabulary or needs a **custom** phrase (the escape hatch).

| Journey | Subtask | Scenario | Vocabulary |
| --- | --- | --- | --- |
| TOD.01 See my todos | TOD.01.1 list loads | [01-list-loads.uat.md](scenarios/01-list-loads.uat.md) | built-in |
| TOD.01 See my todos | TOD.01.3 empty state | [02-empty-list.uat.md](scenarios/02-empty-list.uat.md) | built-in |
| TOD.01 See my todos | TOD.01.4 filter | [03-filter-open.uat.md](scenarios/03-filter-open.uat.md) | built-in |
| TOD.02 Create | TOD.02.2 create happy path | [04-create-todo.uat.md](scenarios/04-create-todo.uat.md) | built-in |
| TOD.02 Create | TOD.02.3 title required | [05-create-needs-title.uat.md](scenarios/05-create-needs-title.uat.md) | built-in |
| TOD.03 Read | TOD.03.1 open detail | [06-read-detail.uat.md](scenarios/06-read-detail.uat.md) | custom: open row by title |
| TOD.04 Edit | TOD.04.2 edit round trip | [07-edit-todo.uat.md](scenarios/07-edit-todo.uat.md) | custom: open row by title |
| TOD.05 Status | TOD.05.1 cycle status | [08-change-status.uat.md](scenarios/08-change-status.uat.md) | custom: press Status Next |
| TOD.06 Delete | TOD.06.1/2 delete + confirm | [09-delete-todo.uat.md](scenarios/09-delete-todo.uat.md) | custom: confirm dialog |

"custom" means the scenario uses a phrase that is not one of the 18 built-ins in
[UatReflectionRuntime.CreateCommandCatalog](../../../srcnew/Brinell.Uat/UatReflectionRuntime.cs).
Those phrases would be added as `[UatPhrase]` methods on a Todo phrase class or on
the fixture root — see [Custom phrases needed](#custom-phrases-needed) below. The
scenarios are written so the phrase text is stable even before the method exists.

---

## The lexicon these scenarios rely on

Page and control names come from the Todo page objects in
[samples/Todo/tests/Brinell.Samples.Todo.UITests/Pages](../../../samples/Todo/tests/Brinell.Samples.Todo.UITests/Pages),
resolved by `[UatName]` or inference. The names used across the scenarios:

**Todo List** page:

| Name | Member | Kind |
| --- | --- | --- |
| Add | `AddButton` | toolbar button |
| Sync | `SyncButton` | toolbar button |
| Filter | `Filter` | picker (All / Open / In progress / Done) |
| Last Sync | `LastSync` | label |
| State | `State` | Loading / Empty / Error / rows |

**Todo Edit** page:

| Name | Member | Kind |
| --- | --- | --- |
| Title | `Title` | entry |
| Title Error | `TitleError` | label |
| Notes | `Notes` | editor |
| Has Due Date | `HasDueDate` | switch |
| Due Date | `DueDate` | date picker |
| Status | `Status` | status component |
| Save | `SaveButton` | toolbar button |
| Cancel | `CancelButton` | toolbar button |

**Todo Detail** page:

| Name | Member | Kind |
| --- | --- | --- |
| Title | `Title` | label |
| Status | `Status` | status component |
| Due | `Due` | label |
| Notes | `Notes` | label |
| Sync State | `SyncState` | label |
| Edit | `EditButton` | toolbar button |
| Delete | `DeleteButton` | toolbar button |

---

## Custom phrases needed

Four scenarios need a phrase the built-in set does not cover. Each is a small
`[UatPhrase]` method on a Todo phrase class (`[UatPhraseClass]`) or on the fixture
root; the runtime already discovers those
([UatReflectionRuntime](../../../srcnew/Brinell.Uat/UatReflectionRuntime.cs)).

| Phrase used in scenarios | Suggested method | Notes |
| --- | --- | --- |
| `When I open the todo {title}` | `OpenTodo(string title)` | taps the list row whose title matches; returns to detail |
| `When I advance Status` | `AdvanceStatus()` | presses the Status component's Next button |
| `Then Status should read {value}` | `AssertStatus(string value)` | reads the status component's text |
| `When I confirm the dialog` / `When I dismiss the dialog` | `ConfirmDialog()` / `DismissDialog()` | answers the native confirmation |
| `Then I should not see {text}` | `AssertTextAbsent(string text)` | no control on the page shows the text (delete removed the row) |

These are intentionally domain phrases (a row is "a todo", not "a control"), which
is exactly the case the escape hatch exists for. Until they are implemented, the
four scenarios that use them will fail to **bind** (`UATB001`), which is the
correct, visible signal — not a silent skip.

---

## Data assumptions

The scenarios are **self-contained** wherever the built-in vocabulary allows:
they create the todo they later read, so no seeded database is required. The
empty-state scenario (TOD.01.3) is the exception — it needs the app launched
against an empty store (the backbone's `empty.json` seed). That seeding is a
fixture concern, recorded here as the scenario's `Requires: EmptyStore` tag, not
something the `.uat.md` can express.
