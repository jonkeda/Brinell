# Proposal: one UAT file per journey - TOD.02 Create a todo

Status: implemented (see [AD-012](../../.docs/decisions/ad-012-test-data-is-typed-validated-and-chosen-by-the-scenario.md) and [Test Data](../../docs/guides/test-data.md))
Date: 2026-09-24
Target: `samples/Todo/tests/Brinell.Samples.Todo.Uat`
Related:

- [samples/Todo/journeys.md](../../samples/Todo/journeys.md) - pyramid backbone (tiers)
- [todo-user-journeys.md](../../samples/Todo/tests/Brinell.Samples.Todo.Uat/todo-user-journeys.md) - UAT checklist
- [UAT template guide](../../docs/guides/uat-template-guide.md) - file grammar
- [UatMarkdownParser.cs](../../srcnew/Brinell.Uat/UatMarkdownParser.cs) - what the parser really accepts

---

## 1. Idea

Today each scenario is its own file (`create-a-todo.uat.md`, `create-needs-title.uat.md`, ...),
and the journey id sits in the file's `Journey` metadata. The proposal: **one `.uat.md` file per
journey** (`TOD.02`). Each subtask (`TOD.02.1`, `TOD.02.2`, ...) gets a header, with a `gherkin`
code block under it. A reviewer reads the file top to bottom the same way as the journey
checklist.

## 2. What the parser allows today

From `UatMarkdownParser`:

| Rule | Consequence for the proposal |
| --- | --- |
| Exactly one `# UAT:` heading per file | The file title is the journey: `# UAT: TOD.02 Create a todo` |
| Any number of `## Scenario:` / `## Scenario Outline:` sections | A file per journey needs no parser change |
| `## Background` steps are put in front of every scenario | The shared "open the Add form" steps are written once |
| Code fences are dropped before parsing | A `gherkin` block under a heading parses the same as bare steps |
| `@tag` lines must come right before a scenario heading | Per-subtask ids go in tags: `@TOD.02.1` |
| Other level-2 headings -> `UAT022`; prose -> `UAT021`/`UAT011` | A separate `## TOD.02.1 ...` header, or explaining text, **is rejected** |

So the heading for each subtask **has to be** the `## Scenario:` heading. The proposal uses it
that way: `## Scenario: TOD.02.1 Add opens an empty edit form`. This works with the current
parser.

## 3. The proposed file

`Scenarios/TOD.02-create-a-todo.uat.md` replaces `create-a-todo.uat.md` and
`create-needs-title.uat.md`.

````markdown
# UAT: TOD.02 Create a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Create |
| Target | MAUI |
| Journey | TOD.02 |
| Tags | maui, create |

## Background

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
```

@TOD.02.1 @maui @create
## Scenario: TOD.02.1 Add opens an empty edit form with status Open

```gherkin
Then Title should equal ""
And Status should show status "Open"
And Title Error should not be visible
```

@TOD.02.2 @smoke @maui @create
## Scenario: TOD.02.2 Saving a valid title returns to the list, which shows the new todo

```gherkin
When I set Title to "Write the report"
And I tap Save
Then I should be on the Todo List page
And Todos should have todo "Write the report"
```

@TOD.02.3 @smoke @maui @create @validation
## Scenario Outline: TOD.02.3 An empty or whitespace title shows "Title is required" and does not save

```gherkin
When I set Title to "<title>"
And I tap Save
Then Title Error should be visible
And Title Error should contain "Title is required"
And I should be on the Todo Edit page
```

### Examples

| title |
| --- |
|  |
|    |

@TOD.02.5 @maui @create
## Scenario: TOD.02.5 A due date is optional; switching it off clears it

```gherkin
When I check Has Due Date
Then Due Date should be visible
When I uncheck Has Due Date
Then Due Date should not be visible
When I set Title to "No deadline"
And I tap Save
Then I should be on the Todo List page
And Todos should have todo "No deadline"
```
````

Notes on the file:

- **TOD.02.4** (100/101 characters) and **TOD.02.6** (local storage, pending) are `[!]` in the
  checklist. They are covered by unit and integration tests, so they get no heading here. The file
  covers only the UAT part of the journey. The checklist stays the full index.
- **TOD.02.3** uses a `Scenario Outline` to cover both empty and whitespace. Each row runs as its
  own scenario (`... [1]`, `... [2]`) and keeps the `@TOD.02.3` tag. "Does not save" is shown by
  staying on the Edit page. That avoids a "should not have todo" phrase, which does not exist.
- **Background** holds the three navigation steps that all four scenarios shared. The fixture's
  `StartTest` reset runs before each scenario (`BeforeScenario`), so every scenario still begins
  from `three-todos` on the list.

## 4. Traceability

| Level | Where it is written | Read by |
| --- | --- | --- |
| Journey `TOD.02` | `# UAT:` title + `Journey` metadata | people, UAT report |
| Subtask `TOD.02.x` | `@TOD.02.x` tag + prefix in the scenario name | people, UAT report, `--tags` filtering |

The scenario name starts with the id, so the per-scenario result JSON and the failure screenshot
(`FormatEvidenceScenarioName`) show the id without extra work. The `JourneyCoverage` report reads
xUnit traits by reflection and does not read UAT files today, so this proposal does not change it.
If UAT should count in that report later, the `@TOD.xx.y` tags are the input to use.

## 5. Gaps to settle before moving the files

| # | Gap | Effect | Proposal |
| --- | --- | --- | --- |
| 1 | `UatScenarioTestBase` gives **one theory case per file** and throws on the first failing scenario | The whole journey is one xUnit test. When 02.1 fails, 02.2-02.5 never run. | Enumerate `(file, scenario)` pairs as theory data, or run every scenario and fail at the end with all failures. Either one is a small change in `Brinell.Uat`. |
| 2 | Not verified: `Title should equal ""` against an empty `Entry` | 02.1 may need a different phrase | Try it; if it fails, add `{control} should be empty` |
| 3 | Not verified: `I check` / `I uncheck` on `Switch` | 02.5 depends on it | Check that `Switch` exposes the `Control.Check` commands; if not, add them to the control, not to the scenario |
| 4 | Not verified: `I set Title to ""` / whitespace through `SetText` | Outline rows for 02.3 | If the empty row does not work, use `I clear Title` in a separate scenario |
| 5 | Tag lines take `@TOD.02.1` without trouble (no spaces). Scenario settings pick only `uat-*` tags. | None | - |

Gap 1 is the only one that affects the design. The others are phrase checks that come up when the
file is run.

## 6. Optional: a real journey heading (parser change)

If the heading must be a plain `## TOD.02.1 ...` with the `Scenario:` line inside the gherkin
block (closer to ordinary `.feature` syntax):

````markdown
## TOD.02.1 Add opens an empty edit form with status Open

```gherkin
@maui @create
Scenario: Add opens an empty edit form
  Then Title should equal ""
  And Status should show status "Open"
```
````

This needs three parser changes: accept `## TOD.xx.y` headings as subtask sections, accept
`Scenario:` / `Scenario Outline:` / `Examples:` without `##`, and put the subtask id on the
scenario. It gives little that section 3 does not already give. **Recommendation: start with
section 3 (no parser change). Consider this only if the plain-heading layout turns out to matter
to reviewers.**

## 7. Rollout

1. Fix gap 1 in `UatScenarioTestBase` (per-scenario reporting).
2. Add `Scenarios/TOD.02-create-a-todo.uat.md`. Delete `create-a-todo.uat.md` and
   `create-needs-title.uat.md`.
3. Run only the UAT project and fix the phrase gaps (2-4) in controls or phrases, not in the
   scenario text.
4. Tick `TOD.02.1` and `TOD.02.5` in `todo-user-journeys.md`. Tick the journey line when all UAT
   subtasks pass.
5. Do the same for the other journeys. `cancel-a-new-todo.uat.md` (TOD.04.3) and
   `set-status-on-a-new-todo.uat.md` (TOD.05.1) move into `TOD.04-...` and `TOD.05-...`.

End state: `Scenarios/` holds one file per journey, `TOD.01-...` to `TOD.10-...`, and each file
lists its subtasks in the same order as the checklist.
