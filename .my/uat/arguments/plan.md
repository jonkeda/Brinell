# UAT phrases and arguments: plan

Status: **done** 2026-09-27 (section 10). Design: [design.md](design.md). Decision: AD-016.

A phrase argument today is a string that `UatReflectionRuntime.ConvertArgument` turns into a
scalar at **run time**, with whatever `Convert.ChangeType` accepts. Tables go through
`UatTableBinding`, which has its own rules (ISO dates, `today`, `+2d`). Nothing checks an argument
before a scenario runs, and the two paths do not agree.

This plan makes arguments **typed by the method, written in one invariant format, and checked at
bind time**, so the xUnit gate and the `brinell-data` tool can reject a bad value without starting
an app.

## 1. What changes, in one example

```csharp
[UatPhraseClass]
public sealed class AssignPhrases(TodoApi api) : UatPhraseClassBase
{
    // Inferred phrase: "When I assign {todo} to {assignee} due {due}"
    public void When_I_assign_TODO_to_ASSIGNEE_due_DUE(string todo, UatUserRole assignee, DateOnly due) { ... }

    // Inferred phrase: "Given the server has todos" - the record list comes from the step's table
    public void Given_the_server_has_todos(IReadOnlyList<TodoRow> todos) { ... }

    // Inferred phrase: "Then the todos {titles} should be open" - an inline list
    public void Then_the_todos_TITLES_should_be_open(string[] titles) { ... }
}

public sealed record TodoRow(string Title, TodoStatus Status, DateOnly? DueDate = null, decimal Hours = 0);
```

```gherkin
Given the server has todos
  | Title            | Status     | Due Date    | Hours    |
  | ---------------- | ---------- | ----------- | -------- |
  | Buy milk         | Open       | 12-May-2026 | 0.5      |
  | Plan the party   | In Progress|             | 1,250.75 |
When I assign "Buy milk" to Eva due 14-May-2026
Then the todos "Buy milk", "Plan the party" and "Pay rent" should be open
```

The validator reports, before anything runs:

```text
Scenarios/TOD.08-assign.uat.md:14: UATA002 When I assign "Buy milk" to Eva due 2026-05-14: 'due' is a date; write 14-May-2026.
Scenarios/TOD.08-assign.uat.md:15: UATA005 When I assign "Buy milk" to Carol due 14-May-2026: 'assignee' is a user; Carol is not in the Apps table (Alice, Bob, Eva).
```

## 2. Decisions

| # | Decision | Point |
| --- | --- | --- |
| A1 | **The method's parameter decides the type.** A phrase says `{name}`; the parameter `name` of the bound method says what it is. No `{name:int}` in phrase text. The catalog carries the parameter types, so the Presenter, the discovery report and the validator can show `{due: date}`. | 5 |
| A2 | **Arguments are converted at bind time**, by one converter (`UatValues`), for step text, Examples cells and table cells alike. A value that does not fit is a bind diagnostic (`UATA0xx`) with the file and line, not a run-time exception. The runtime receives converted values. | 5, 11 |
| A3 | **One invariant format, whatever the machine culture.** Dates `dd-MMM-yyyy` with English month abbreviations (`12-May-2026`; `3-jan-2027` is read too). Numbers use `.` for decimals and `,` for thousands (`1,250.75`). No other form is read; a near miss gets a diagnostic that shows the fix. | 3, 4 |
| A4 | **Phrases can be inferred from the method name**, with parameters encoded in CAPITALS (section 4). A keyword attribute without text (`[UatWhen]`) asks for the inferred phrase; one with text declares its own. Words only: a phrase with punctuation (`read-only`, `"`) needs text. | 1, 7 |
| A5 | **Records and classes are arguments.** A record parameter is bound from the step's table: a `Field | Value` table is one record, any other table is a list of records. A type with a registered converter can also be written inline. | 2, 8 |
| A6 | **Arrays are arguments.** `T[]`, `List<T>`, `IReadOnlyList<T>` and `IEnumerable<T>`. Scalars inline (`"A", "B" and "C"`), records as table rows. | 6, 8 |
| A7 | **A user is an argument of type `UatUserRole`.** Its text is a name from the `Apps` table; it is checked at bind time and arrives as the user with their row (apps, role, other columns). `{user}` (the subject, AD-015) is passed as a `UatUserRole` too when the method asks for one. | 10 |
| A8 | **The validator applies every rule** in this plan to `*.uat.md` files, through the same code as the xUnit gate (the AD-012 rule: one engine). | 11 |
| A9 | **JSON test data is not affected.** Test sets are files for machines and keep ISO dates and JSON numbers. The format in A3 is for Markdown that people read. | 3 |
| A10 | **A broken phrase does not break the catalog.** A problem found while building it (a bad method name, a parameter the phrase does not name, two table parameters, a duplicate, a control-verb property on a phrase method) marks that one phrase broken, with its diagnostic. Everything else loads. A step that binds to a broken phrase fails at that moment, with the phrase's problem and the method that declares it. A broken phrase nobody uses costs nothing; the validator lists it as a warning. | change 1 |

## 3. Values: how each type is written

One reader and one writer (`UatValues.TryRead(text, type, context, out value, out problem)` and
`UatValues.Write(value)`). The writer is used in failure messages and in reports, so what the
engine prints can be pasted back into a scenario.

| Type | Written as | Examples | Not accepted (diagnostic shows the fix) |
| --- | --- | --- | --- |
| `string` | As is. Quotes optional and removed; needed for an empty value, leading or trailing spaces, or a `, ` inside a list item. `\"` is a quote inside quotes. | `Buy milk`, `"Buy milk"`, `""`, `"say \"hi\""` | |
| `int`, `long`, `short` | Digits, optional `-`, optional `,` between groups of three | `7`, `-3`, `1,250` | `1.250` (reads as a decimal), `12,50` (bad grouping) |
| `decimal`, `double`, `float` | As above plus `.` and decimals | `0.5`, `1,250.75`, `-0.25` | `1,5` ("comma decimal? write 1.5"), `1.250,75` |
| `bool` | `true` or `false`, any case | `true` | `yes`, `1` (a phrase says yes/no in its words) |
| enum | The member name, any case, spaces allowed between its words | `Open`, `In Progress` for `InProgress` | unknown name: lists the names, with "did you mean" |
| `DateOnly`, date part of `DateTime` | `d-MMM-yyyy`: one or two-digit day, English month in any case. Written back as `dd-MMM-yyyy`. | `12-May-2026`, `03-Jan-2027`, `3-jan-2027` | `2026-05-12`, `12/05/2026`, `12-mei-2026`, `12-May-26` |
| `DateTime`, `DateTimeOffset` | `dd-MMM-yyyy HH:mm`, optional `:ss`, 24 hours, in the test clock's time zone | `12-May-2026 14:30` | `12-May-2026 2:30 PM` |
| `TimeOnly` | `HH:mm`, optional `:ss` | `09:15` | |
| `TimeSpan` | Units `d h m s`, largest first | `90m`, `1h 30m`, `2d` | `01:30:00` |
| Relative date | `today`, `tomorrow`, `yesterday`, `+3d`, `-1d`, resolved against the pinned test clock (existing) | `+2d` | |
| `Guid` | Standard form | | |
| Nullable, nullable reference | `null` (unquoted) or an empty table cell; `""` stays an empty string | `null` | `null` for a non-nullable parameter |
| `UatUserRole` | A user or app name from the `Apps` table | `Eva`, `Daan's PC` | a name the table does not have |
| Registered type | Whatever its converter reads (section 6) | `1,250.00 EUR` | |
| `T[]` and lists | Items separated by `, ` with an optional last `and`; each item written as `T` | `"A", "B" and "C"`, `1, 2, 3` | |
| Record / class | A table under the step (section 5), or inline when it has a converter | | |

Notes:

- **Why `, ` separates list items:** a thousands comma has no space after it, so
  `1,250.50, 3 and 7` is `[1250.50, 3, 7]` without quoting.
- **Why `1,5` is refused:** this team writes Dutch. Today `double.Parse("1,5", InvariantCulture)`
  returns `15`. Grouping must be exactly three digits, so the mistake becomes a diagnostic.
- **Why relative dates stay:** a fixed date in a scenario ages (an "overdue" todo stops being
  overdue). `today` and `+3d` are values, not formats; they are the only other date form.
- The current `UatTableBinding.Convert` and `Show` (ISO) and `UatReflectionRuntime.ConvertArgument`
  are replaced by `UatValues`. The existing scenarios use only relative dates, so nothing in them
  changes.

## 4. Phrases from method names

Today a phrase-class method named `WhenIRestartTheApp` becomes `I restart the app`, but a method
with parameters needs an attribute. The encoding below closes that gap and works for fixture
methods too.

**Which methods are inferred**

A keyword attribute **without text** (`[UatWhen]`) means "the phrase is the method name". The
attribute gives the keyword, so the name **does not need to start with `Given`, `When` or
`Then`**. When it does, and the prefix is the attribute's keyword, the prefix is dropped.

| Attributes | Method name | Result |
| --- | --- | --- |
| `[UatWhen]` | `I_restart_the_app` / `IRestartTheApp` | When `I restart the app` |
| `[UatWhen]` | `When_I_restart_the_app` / `WhenIRestartTheApp` | When `I restart the app` (prefix dropped) |
| `[UatThen]` | `The_todo_TITLE_should_be_synced` | Then `the todo {title} should be synced` |
| `[UatThen]` | `When_I_restart_the_app` | broken, `UATA013`: the name says When, the attribute says Then |
| `[UatGiven]` + `[UatWhen]` | `The_device_is_offline` | Given and When `the device is offline` |
| none | `WhenIRestartTheApp` | When `I restart the app` (what phrase classes do today) |
| none | `RestartTheApp` | no phrase: nothing says which keyword |
| only attributes with text | any | no inferred phrase: the attributes are the phrases |

A prefix counts only as a whole word: `When_...` or `When` followed by a capital (`WhenI...`), so
`Whenever_...` and `Thenceforth...` are names, not prefixes. An empty attribute on a control-verb
method is not inferred (control verbs keep declaring `{control}` and their `CommandId` in text) and
marks it broken.

**Rules for the name**

1. Words are separated by `_` (preferred) or by PascalCase. Literal words become lowercase; `I`
   stays `I`.
2. A word in **CAPITALS equal to a parameter name** (ignoring case) is that parameter's
   placeholder: `TODO` is `{todo}`, `DUEDATE` is `{dueDate}`.
3. `USER` is the subject `{user}` (AD-015); it needs no parameter.
4. Every parameter that is not injected (`UatExecutionContext`, `UatStepInvocation`,
   `CancellationToken`, settings, the table-bound record) must appear exactly once (`UATA010`).
5. A capitalised word that is no parameter stays a literal (`PDF` in `When_I_export_the_PDF`).
6. A parameter cannot be named `i`, and two placeholders cannot touch in PascalCase
   (`TITLESTATUS`); use `_` (`UATA011`).

A name that breaks rule 4 or 6 does not stop the catalog: the phrase is marked broken (A10).

| Method | Phrase |
| --- | --- |
| `WhenIRestartTheApp()` | `I restart the app` |
| `When_I_open_the_todo_TITLE(string title)` | `I open the todo {title}` |
| `WhenIOpenTheTodoTITLE(string title)` | `I open the todo {title}` |
| `Then_the_todo_TITLE_should_show_status_STATUS(string title, TodoStatus status)` | `the todo {title} should show status {status}` |
| `When_USER_assigns_TODO_to_ASSIGNEE(string todo, UatUserRole assignee)` | `{user} assigns {todo} to {assignee}` |
| `Given_the_server_answers_after_SECONDS_seconds(int seconds)` | `the server answers after {seconds} seconds` |
| `Then_the_todo_TITLE_should_have_a_read_only_status` | not encodable (`read-only`): use `[UatThen]` |

**Why capitals:** they separate a placeholder from a literal word with the same spelling
(`Then_the_title_should_be_TITLE` is `the title should be {title}`), they survive both naming
styles, and SpecFlow used the same convention for its method-name steps, so it is familiar.

**The reverse direction** is what the validator's snippets use (section 7): for an unbound step
`When I archive "Buy milk" on 12-May-2026` it proposes
`public void When_I_archive_P1_on_P2(string p1, DateOnly p2)`, with the types read from the values.
The author renames `P1` and `P2`.

**A `{user}` twin** is always declared, never inferred (inferring it would bring back the verb
guessing that named-steps-plan removed). The empty attribute keeps the inferred `I` form next to it:

```csharp
[UatWhen]                                               // I assign {todo} to {assignee}
[UatWhen("{user} assigns {todo} to {assignee}")]
public void When_I_assign_TODO_to_ASSIGNEE(string todo, UatUserRole assignee) { ... }
```

Two empty attributes of one keyword on one method are a duplicate phrase, marked broken.

## 5. Records and arrays in scenario files

A record parameter takes the step's table. A step has at most one table, so a method has at most
one record or record-list parameter (`UATA012`). Columns match constructor parameters or
properties without case and spaces (`Due Date` is `DueDate`), as `UatTableBinding` does today.

**One record: a vertical table**, recognised by its header `Field | Value`:

```gherkin
When I create the todo
  | Field    | Value              |
  | -------- | ------------------ |
  | Title    | Write the report   |
  | Status   | In Progress        |
  | Due Date | 12-May-2026        |
  | Hours    | 1,250.75           |
```

**A list of records: a horizontal table**, one row per item:

```gherkin
Given the server has todos
  | Title          | Status      | Due Date    |
  | -------------- | ----------- | ----------- |
  | Buy milk       | Open        | 12-May-2026 |
  | Plan the party | In Progress | +3d         |
```

**A list of scalars: inline**, or a one-column table when it is long:

```gherkin
Then the todos "Buy milk", "Plan the party" and "Pay rent" should be open
Then the todos should be open
  | Title          |
  | -------------- |
  | Buy milk       |
  | Plan the party |
```

**A record with a converter: inline**, like a scalar (`Money`, `Address` with a `Parse`).

**Checking against a table** stays as it is: a `UatTable` parameter plus `AssertContains`,
`AssertExactly` or `AssertDoesNotContain`. They now read cells with `UatValues`, and their failure
tables print values with `UatValues.Write`, so a date shows as `12-May-2026`.

**Not in this plan:** nested records in one table (`Address.City` columns), more than one table per
step, inline record literals such as `(Title: "Buy milk", Due: 12-May-2026)`. A nested record uses
a converter, or a separate step.

**In a Scenario Outline** an Examples cell is substituted first and converted after, so each
Examples row is checked on its own and a diagnostic names the row.

## 6. Also worth taking from Cucumber and SpecFlow

Decided 2026-09-27: as proposed.

| Feature | There | Here | Decision |
| --- | --- | --- | --- |
| Custom parameter types | `defineParameterType`, SpecFlow `[StepArgumentTransformation]` | `Money`, `Duration`, a todo by its title | **Adopt.** `[UatArgumentType]` on a static `Parse(string)` or a class implementing `IUatArgumentConverter<T>` (DI-aware, for a lookup). The validator runs converters with no app. |
| Lookup by key | Transformations that load an entity | `the todo "Buy milk"` reaching a `TodoDto` | **Adopt as a converter** over the scenario's test set: a record type from the test data resolves inline by its key only through a converter the app registers (Q4). |
| Data table to object / set | SpecFlow `CreateInstance`, `CreateSet`, `CompareToSet` | Section 5 | **Adopt** (A5). |
| Doc strings | `"""` block under a step | Long text, JSON bodies | **Adopt.** A `"""` block under a step goes to a `string` parameter, or is read as JSON into a record parameter. Needs a parser change. |
| Snippets for unbound steps | Cucumber prints a step definition | Section 4 | **Adopt** in the validator. |
| Optional text and alternatives | `{int} todo(s)`, `color/colour` | `1 todo`, `2 todos` | **Never** (Q5). A phrase that needs both forms gets two keyword attributes. |
| Built-in `{int}`, `{string}`, `{}` | Cucumber expressions | | **Skip.** The parameter is the type (A1). |
| Regex step definitions | Cucumber, SpecFlow | | **Skip.** Phrases are text; regex steps cannot be listed or checked. |
| Localised keywords | `# language: nl` | | **Skip for now.** If it comes, the value formats stay invariant. |
| Overlap check | SpecFlow reports ambiguous steps at run time | | **Adopt a light version:** the validator reports two phrases of one keyword that bind the same scenario step (the UATB002 case), before a run. |

## 7. The validator CLI

The `brinell-data` tool (`tools/Brinell.DataValidator`) already reads `uat.config.md`, loads the
suite's assemblies and prints `file:line: CODE path: message`. It gets the UAT rules.

| Command | Does |
| --- | --- |
| `validate [file] [--format json]` | As today, plus every `Scenarios/**/*.uat.md` (or one file): parse (`UAT0xx`), bind (`UATB0xx`), arguments (`UATA0xx`), for each Examples row. |
| `validate --fix` | Rewrites what has one right answer: `2026-05-12` to `12-May-2026`, `In_Progress` to `In Progress`. Never guesses `1,5`, and leaves `3-jan-2027` alone (it is valid). |
| `phrases [--keyword When] [--format json]` | The catalog per head, with types: `When I assign {todo: text} to {assignee: user} due {due: date}`, and where each is declared. Broken phrases (A10) are listed as warnings with their problem; they do not change the exit code. |
| `snippet <file>:<line>` | For an unbound step, the method stub of section 4. |

It builds the catalog from the `Pages`, `Controls` and `Commands` assemblies of the config, as the
gate does, and the `Apps` table for `UatUserRole` values. Exit codes as today (0, 1, 2).

**Risk:** a MAUI suite's page assembly is `net10.0-windows`; the tool is `net10.0`. Loading it for
reflection may need the tool to target `net10.0-windows` on Windows, or a load context that resolves
from the assembly's folder. Step 7 starts with a spike on the Todo MAUI suite.

**Broken phrases in the validator.** `validate` reports a broken phrase at every step that uses it,
at the step's line, with the declaring method: `TOD.08-assign.uat.md:14: UATA010 When I assign
"Buy milk" to Eva: the phrase from AssignPhrases.When_I_assign_TODO_to_ASSIGNEE does not name
parameter 'due'.` An unused broken phrase is a warning only.

**Diagnostics**

| Code | Meaning |
| --- | --- |
| `UATA001` | A value does not fit the parameter's type; names the type, the format and an example. |
| `UATA002` | A date or time not in the invariant format; shows the value rewritten. |
| `UATA003` | A number with a comma decimal or bad grouping. |
| `UATA004` | An enum value that is not a member; lists them, with "did you mean". |
| `UATA005` | A `UatUserRole` name not in the `Apps` table, or a suite without one. |
| `UATA006` | A table column that is not a field of the record, or a required field with no column. |
| `UATA007` | A record parameter with no table under the step. |
| `UATA010` | A method name that does not name every parameter once (section 4, rule 4). |
| `UATA011` | A method name that cannot be encoded (`i`, touching placeholders). |
| `UATA012` | More than one table-bound parameter on one method. |
| `UATA013` | A phrase declared wrongly: two empty attributes of one keyword on one method, a name prefix that is not the attribute's keyword, an empty attribute on a control verb, or a control-verb property on a phrase method (AD-011, today an exception). |

`UATA010`-`013` belong to a phrase, not a step (A10): they are found when the catalog is built and reported when a step binds to that phrase.

## 8. Steps

| # | Step | Verification |
| --- | --- | --- |
| 1 | `UatValues` in `Brinell.Uat`: read and write for every row of section 3, lists, quotes and escapes. Unit tests only. | New `UatValuesTests`: each type, each near miss and its message, a round trip `Write` then `TryRead`, the same results with `CurrentCulture` set to `nl-NL`. |
| 2 | Broken phrases (A10): `UatCommandPattern` gets a `Problem`; the catalog builders catch per phrase instead of throwing (`UatPhraseMethods.GetPhrases` stops throwing); the binder reports the problem when a step binds to it. Typed catalog: `UatCommandPattern` carries the parameter types; the binder converts arguments and reports `UATA001`-`005`; `UatStepInvocation` carries the values; `ConvertArgument` goes. `UatUserRole` (A7). | `UatBrokenPhraseTests`: a fixture with one broken phrase still binds its other phrases; the step that uses the broken one fails with its code and method. `UatArgumentTests`: a bad date fails at bind time with the line; `Eva` arrives as a `UatUserRole` with her app; `{user}` passed as `UatUserRole`. Uat.Tests stay green. |
| 3 | Records and lists (section 5): a record parameter takes the table; lists inline and from a one-column table; `UatTableBinding` reads and writes through `UatValues`. | Tests for vertical, horizontal and one-column tables, `UATA006`/`007`/`012`; the failure table prints `12-May-2026`. Todo UAT suites green (their relative dates keep working). |
| 4 | Method-name inference (section 4) for fixture and phrase-class methods: a parameterless constructor on `UatGivenAttribute`/`UatWhenAttribute`/`UatThenAttribute` (in `Brinell.Core`, so `Phrase` becomes nullable until inferred), and `UATA010`/`011`/`013` as broken phrases. | Tests for each row of the section 4 table; `[UatWhen]` next to a `{user}` twin gives both phrases; `[UatWhen] I_restart_the_app` needs no prefix; `[UatThen] When_...` and two `[UatWhen]` mark the phrase broken. |
| 5 | Converters: `[UatArgumentType]` and `IUatArgumentConverter<T>`; doc strings in the parser. | A `Money` converter inline and in a table cell; a doc string into a string and into a record. |
| 6 | Samples: a Todo phrase that takes a record list, one that takes a scalar list and a `UatUserRole` in the mixed suite; at least one inferred phrase with parameters. | Todo web UAT, MAUI UAT and mixed UAT green. |
| 7 | Validator: spike loading a MAUI page assembly; then `validate` for scenarios, `--fix`, `phrases`, `snippet`. | `Brinell.DataValidator.Tests`: a scenario folder with one error of each `UATA` code gives those lines; `--fix` rewrites an ISO date; clean Todo suites give exit 0. |
| 8 | Docs: new AD-016 (arguments are typed by the method, written invariantly, checked at bind time); `invariants.md`; the guide's "Phrase Patterns", "Custom Root Phrases" and "Tables" sections replaced by an "Arguments" section; `test-data.md` tool commands; CHANGELOG. | Links valid. |

Steps 1-3 are the core and can ship alone. Steps 4, 5 and 7 each depend only on 1-2.

## 9. Answers

| # | Question | Answer |
| --- | --- | --- |
| Q1 | Is a `userRole` a person (`Eva`) or a role (`Planner`)? | A person from the `Apps` table, carrying an optional `Role` column. Resolving a role name to its one user can come later if scenarios need it. |
| Q2 | How does an inferred method get its `{user}` twin? | It is never inferred: the author adds a keyword attribute with the `{user}` text. An empty attribute next to it (`[UatWhen]`) is the inferred phrase (section 4). |
| Q3 | Keep `dd` strict (`03-Jan-2027`), or also read `3-Jan-2027`? | Read `3-jan-2027` too: one or two digits, month in any case. Written back as `03-Jan-2027`. |
| Q4 | Should test-data records resolve inline by key (`the todo "Buy milk"`) out of the box? | Only through a converter the app registers; the engine does not guess keys. |
| Q5 | Optional text and alternatives in phrases (`todo(s)`, `a/an`)? | Never. People add a keyword attribute per form. |
| Q6 | `DateTimeOffset` in which time zone? | The test clock's zone, which the fixture pins; the validator only checks the format. |

## 10. Progress

| Step | Status | Evidence |
| --- | --- | --- |
| 1 | Done | `UatValues` (read, write, lists, quotes, relative dates) and `UatArgumentTypes`. `UatValuesTests`: 59, including the `nl-NL` culture and round trips. |
| 2 | Done | `UatCommandPattern.Parameters`/`Problem`, `UatCommandCatalog.Unmatchable`/`ArgumentTypes`, `RawArguments` on the match and the invocation; the binder runs `UatArguments.Check`; the runtime reads with `UatArguments.Read`; `ConvertArgument` and the throwing `UatPhraseMethods.GetPhrases` are gone. `UatUserRole`, `IUatClock` (`UatExecutionContext.Clock`, read lazily). `UatArgumentTests` (14), `UatReflectionRuntimeTests` rewritten for a broken phrase. |
| 3 | Done | `UatTableBinding.Bind(Type, context)` and `Check`: record lists, `Field \| Value`, one-column lists; cells through `UatValues`; failure tables print `12-Mar-2026`. `UatTableBindingTests` updated. |
| 4 | Done | `UatPhraseDeclarations` (empty attributes, name prefixes, capitals, `UATA010`-`013`); `UatPhraseAttribute` takes an empty phrase and the keyword attributes have parameterless constructors. |
| 5 | Done | `[UatArgumentType]`, `IUatArgumentConverter<T>`; `"""` doc strings in the parser (`UAT023` when unclosed), to a string or JSON into a record. |
| 6 | Done | Todo: `ITodoArrangement : IUatClock`; `the server also has todos` takes `IReadOnlyList<ServerTodoRow>`; `[UatWhen] I_open_the_todo_TITLE` and `[UatThen] The_list_should_show_TITLES(string[])` on both heads; TOD.01.2 uses one record table with `In Progress` and the inline list; the mixed phrases take `UatUserRole`. Web UAT 33, MAUI UAT 32, mixed UAT 4. |
| 7 | Done | Spike: the tool loads the `net10.0-windows` MAUI suite; `Brinell.Core` needed `xunit.core`, which the tool now resolves from the suite folder. `validate` (scenarios), `--fix`, `phrases`, `snippet`; per-head catalogs for an `IUatAppsHost`. Todo MAUI, web and mixed suites validate clean. `ScenarioValidationTests` (6), `UatScenarioCheckTests` (4). |
| 8 | Done | AD-016, `invariants.md`, `.docs/README.md`; `uat-phrases-and-flows.md` (Arguments, Phrases From Method Names, Broken Phrases); `uat-template-guide.md`; `test-data.md`; CHANGELOG. |

Other suites: Uat 249, Core 27, Maui 186 (1 skipped), Html 39, Blazor 173, Presenter.Uat 49,
DataValidator 16; `Brinell.sln` builds.
