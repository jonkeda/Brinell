# Callable phrases: design

For [callable-phrases.md](callable-phrases.md), implementation steps 1-5 and 7 plus global phrase
files. Module phrase files (step 6) and page tracking (step 8) are not designed here; section 12
says what they will plug into. This fixes the shapes the parser, the binder, the runner, the
Presenter and the `brinell-data` tool share, so the steps can be built one at a time.

## 1. The pieces

```text
  Foo.uat.md  ## Phrase: ...           Scenarios/*.phrases.md  # Phrases: ...
        |                                        |
  UatMarkdownParser.Parse               UatMarkdownParser.ParsePhrases
        |  UatDocument.Callables                 |  UatPhrasesDocument.Callables
        +------------------+---------------------+
                           |
              UatScenarioFiles.Load(path)         one loader for the gate, the spec gate,
                           |                      the tool and the Presenter
                  UatLoadedScenarioFile  (document + callables it can see + diagnostics)
                           |
       UatCallableSet.Prepare(catalogs)           once per file: structure, inferred and declared
                           |                      types, problems (A10: marked, not thrown)
                           v
  UatBinder.Bind(document, catalogs, callables)
     step -> catalog.Match + callables.Match -> one selection rule (no shadowing)
     C# phrase  -> UatStepInvocation                               (as today)
     callable   -> check values -> expand -> bind inner steps -> UatStepInvocation { Call = frame }
                   diagnostics from inside -> relocated to the call (or the call's table row)
                           |
  run: UatStepExecutionSession runs the flat list; Call frames group the trace
```

Nothing new runs at run time: a callable exists only at bind time, and what it leaves behind is
ordinary invocations with a frame that says where they came from.

## 2. Parsing

### Sections

In a `.uat.md` file, two new level-2 headings, anywhere a `## Scenario:` may be:

```markdown
## Phrase: a project {name} with {hours} unbilled hours
## Phrase for each row: projects with unbilled hours for {user}
```

A section is, in order: an optional `Parameters` table, then steps (code fences dropped, as for
scenarios today). The `Parameters` table is recognised by its first column header, `Parameter`;
its columns are `Parameter`, `Type` and an optional `Example`.

Under a step inside a phrase section, a line that is exactly `<table>` or `<doc string>` (indented
like a table) marks the call's block passed through. Anywhere else it is `UAT024`.

A phrase file is a markdown file named `*.phrases.md`:

```markdown
# Phrases: Projects

## Phrase: a project {name} with {hours} unbilled hours
...
```

It has one `# Phrases:` heading and only phrase sections. `## Scenario:`, `## Background`,
`## Metadata` and `## Data:` in it are `UAT028`. A `.uat.md` with phrase sections still needs at
least one scenario (`UAT008`, as today).

### Model

```csharp
public enum UatCallableKind { Once, EachRow }
public enum UatPassedBlock { None, Table, DocString }

public sealed record UatCallable(
    UatCallableKind Kind,
    string Phrase,                                   // heading text, {x} placeholders
    IReadOnlyList<UatCallableParameterDeclaration> Declared,
    IReadOnlyList<UatStep> Steps,                    // <x> left in; UatStep.Passes marks <table>
    UatSourceLocation Source);

public sealed record UatCallableParameterDeclaration(string Name, string TypeName, string? Example, UatSourceLocation Source);

// added
UatStep.Passes : UatPassedBlock                      // init, default None
UatDocument.Callables : IReadOnlyList<UatCallable>   // default []
public sealed record UatPhrasesDocument(string Title, IReadOnlyList<UatCallable> Callables, UatSourceLocation Source);
UatMarkdownParser.ParsePhrases(string markdown, string? filePath) : (UatPhrasesDocument?, diagnostics)
```

- **Outline substitution moves** from the parser into `internal static class UatOutline`
  (`Substitute(step, values)`, `Placeholders(step)`), so the parser's outline expansion and the
  binder's call expansion are one piece of code. `Placeholders` also reads table cells and doc
  strings, as `FindParameters` does now.
- The parser checks only what it can see in the text: an empty heading (`UAT025`), a section with
  no steps (`UAT026`), a malformed `Parameters` table (`UAT027`: missing `Type`, duplicate or empty
  `Parameter`). Everything that needs the catalog is a *callable problem* (section 5), not a parse
  error.

## 3. Loading: one loader

Five places parse and bind a scenario file today: `UatScenarioTestBase.BindUatFile`,
`UatSpecFormatTestBase`, `UatScenarioCheck.CheckFile` (the tool), and the Presenter's
`UatWorkspaceService` and `UatExecutionService`. Each gets callables through one call:

```csharp
public static class UatScenarioFiles
{
    public static UatLoadedScenarioFile Load(string filePath);
}

public sealed record UatLoadedScenarioFile(
    UatDocument? Document,
    UatCallableSet Callables,                         // the file's own + the global phrase files'
    IReadOnlyList<UatDiagnostic> Diagnostics);        // parse diagnostics of the file and the phrase files
```

- **Where global phrase files are.** The scenario root is the nearest folder named `Scenarios`
  that contains the file (the folder itself included); if there is none, the file's own folder.
  Global phrase files are the `*.phrases.md` directly in that root. Module files in subfolders are
  ignored until step 6.
- The loader caches parsed phrase files by path and last-write time, so a suite of forty
  scenario files reads each phrase file once.
- **A phrase file that does not parse** makes its diagnostics part of every file that loads it,
  as errors. They are reported once per file, which is noisy but never silent.
- `UatBinder.Bind(document, catalogs)` keeps working, with no callables but the document's own.
  The new overload is `Bind(document, catalogs, callables)`.
- **Copying.** The gate reads `bin/.../Scenarios`. Suites copy `Scenarios\**\*.uat.md`; they now
  also copy `Scenarios\**\*.phrases.md`. The sample and test csproj files get the line; the web
  Todo suite links the MAUI suite's phrase files as it links its scenarios. The UAT guide says so.
  A phrase file that is not copied makes calls fail with `UATB001` in the gate while the tool
  (which reads the project folder) passes; the tool reports a warning when a `*.phrases.md` in
  the project has no `CopyToOutputDirectory` item (cheap: it already reads the csproj).

## 4. Matching a call

```csharp
public sealed class UatCallableSet
{
    public IReadOnlyList<UatCallable> Callables { get; }
    public UatPreparedCallables Prepare(UatCatalogSet catalogs);  // section 5, cached per catalogs
}
```

- **A callable has no keyword.** Its heading is matched for `Given`, `When` and `Then` alike: it
  is compiled once into a `UatCommandPattern` per keyword (same `Compile`, so the same `{x}` and
  whitespace rules), with a new `Callable` property and no handler.
- **One selection rule.** The binder matches a step against the current app's catalog and against
  the prepared callables, and picks with the rule that `UatCommandCatalog.Match` already uses
  (exact beats parameterised, then highest specificity). That rule moves into a static
  `UatCommandCatalog.Select(matches)` so both sources go through it. Two candidates left is
  `UATB002`, whatever their source: a callable and a C# phrase with the same text are ambiguous,
  as are two callables. **No shadowing**, as the scopes doc decides.
- **Subjects.** A callable whose heading starts with `{user}` takes part in subject binding like a
  C# phrase declared with `{user}` first (AD-015): `Eva creates a project "Riverside"` binds to it
  only in that form. As a subject does, the call makes Eva's app current for its inner steps.
- **`I` as a value.** When the call's `{user}` is `I`, the binder substitutes the current app's
  user name before expanding, so `<user> logs <hours> hours` reads `Daan logs 8 hours`, not
  `I logs 8 hours`. With no current app it is `UATB007`, as for any `{user}` phrase.

## 5. Preparing: types and problems

`Prepare` runs once per file (per catalog set), before the file is bound, and gives each callable:

```csharp
public sealed record UatPreparedCallable(
    UatCallable Callable,
    IReadOnlyDictionary<string, Type> Types,          // heading params and, per row, columns
    UatPassedBlock Passes,                            // Table if any step passes <table>, ...
    Type? BlockType,                                  // what the passed table binds to, if known
    UatPhraseProblem? Problem);                       // first problem; the callable is broken
```

### Structural checks (text only)

- A `{x}` in the heading that no `<x>` in the body uses; for a `Once` callable a `<x>` in the body
  that is not in the heading (`UATC001`). For `EachRow`, a body `<x>` not in the heading is a
  column.
- `EachRow` with a step that `Passes` a table (`UATC007`); a declared parameter that names a
  heading placeholder *and* is used as a column cannot happen, since the heading decides.
- A `Parameters` row that names no parameter, an unknown type name, or an example that does not
  read as its type (`UATC003`).

### Inference: binding the body with the placeholders in

The body is bound once, in **inference mode**, against the suite head's catalog set with a fresh
app tracker, as if it were a scenario. Two small hooks make that possible:

- `UatValueContext.Placeholders` (an optional sink). When `UatValues.TryRead` is given text that is
  exactly `<x>` (quotes allowed) and the context has a sink, it records `(x, type)` and succeeds
  without reading. Because every value goes through `TryRead` - step arguments, list items, table
  cells bound to records - the one hook covers every row of the plan's inference table.
- In inference mode the binder does not resolve a `{user}` argument that is a placeholder; it
  records `(x, UatUserRole)`.

What it yields:

| The body step | Effect |
| --- | --- |
| binds, values fit | types recorded |
| binds, a **literal** value does not fit (`When I log 8,5 hours`) | broken: `UATC008`, carrying the inner code and message |
| binds to a broken C# phrase | broken: `UATC008` with that phrase's problem |
| binds to nothing, or is ambiguous | no types from it; **not** broken. It may bind at a call (another app, later a page). The call reports it |
| calls another callable | that callable is prepared first (recursively), and its types flow through; a cycle is `UATC004` on every callable in it |
| passes `<table>` | the inner parameter's type becomes `BlockType` |

A placeholder inside a longer argument (`"<name> copy"`) never reaches the sink whole, so it
records nothing: it is text, and the value is checked when the call expands.

### Merging types

A parameter's recorded types are merged by `UatValues.Describe` kind: text gives way to anything;
`whole number` and `number` merge to `whole number`; two other different kinds (a date and a
number, two enums) are `UATC002`, "no value is both". A declared type must equal the merged one
when both exist (`UATC002`); a declared type alone is used as is; neither is `string`.

### Type names

`UatValues.TypeNamed(name, known)` reverses `Describe`: `text`, `whole number` (int), `number`
(decimal), `true/false`, `date`, `date and time`, `time`, `duration`, `user`, `list of <name>`,
`table` (`UatTable`). A C# name (`TodoStatus`, `ServerTodoRow`, `Money`) is looked up among the
types the catalog's phrases already use (parameter types, their element and member types, and the
registered argument types). That closed set needs no assembly scan, and a type no phrase uses
cannot be passed anywhere anyway.

## 6. Binding a call

`BindStep` gets a `UatCallFrame? frame`. When the selected match is a callable:

1. **Broken?** Report the prepared problem at the call, naming the callable's file and line
   (`Step 'X': the phrase 'Y' from Projects.phrases.md:3 ...`), as AD-016 does for a C# phrase.
2. **Depth and cycle.** A frame chain already holding this callable is `UATC004`; a fourth level
   is `UATC005`.
3. **Blocks.** `Passes == Table` and no table under the call: `UATA007`. A table under a `Once`
   callable that passes none: `UATB004`. `EachRow` with no table or no rows: `UATA007`. Same for
   doc strings.
4. **Values.** Heading values from the match (raw, quotes kept), `I` replaced (section 4). For
   `EachRow`, each row's cells matched to columns by the record rule (case and spaces ignored,
   `UatTableBinding`'s normaliser); an unknown column or a body column with no cell is `UATA006`
   at the table's header row. Each value is read with `UatValues.TryRead` as its prepared type:
   a problem is reported at the call's line (a row's line, per row) with the parameter's name and
   the fix. No expansion happens after a value problem, so it is reported once.
5. **Passed table.** When `BlockType` is known, the call's table is checked with
   `UatTableBinding.Check` now, so a bad cell is reported at its row of the call even when an
   inner step fails for another reason.
6. **Expand.** For each row (one for `Once`): substitute the values into copies of the body steps
   with `UatOutline.Substitute`, set a passed block from the call, and bind each copy with a new
   frame. Inner invocations are appended to the scenario's list in order.
7. **Page rules** (section 9) on the invocations just bound.

```csharp
public sealed record UatCallFrame(
    UatCallable Callable,
    UatStep Call,                                     // the calling step as written
    int? Row,                                         // 1-based, EachRow only
    IReadOnlyDictionary<string, string> Values,
    UatCallFrame? Parent)
{
    public UatSourceLocation Location { get; }        // the row's line for EachRow, else the call's
    public int Depth { get; }
}
```

## 7. Where diagnostics point

Rule: **a diagnostic raised inside a call is reported in the scenario file.** When binding with a
frame, the binder rewrites each diagnostic from an inner step:

- If its location is already in the outermost caller's file (a cell of a table passed through
  `<table>` keeps the call's `UatTable`, so its rows keep their lines), it stays.
- Otherwise its location becomes the outermost frame's `Location`, and the inner positions are
  kept on a new `UatDiagnostic.Within : IReadOnlyList<UatSourceLocation>` (outermost first).
- The message gets the chain: `in "a project {name} ..." (Projects.phrases.md:3), step 4: And I
  log <hours> hours on the work order`. `UatDiagnosticsFormatter` prints `Within` indented under
  the diagnostic.
- **Fixes.** A relocated `Fix` is kept; `ApplyFixes` already skips a fix whose `Original` is not
  on the target line. So a value written whole in the call is fixed in the call, and a value that
  only appears in the body is left alone, as the plan asks.

Preparation problems also exist on their own: the tool's `validate` reports each broken callable
once as a warning at its heading (section 11), and the gate reports it only at a call.

## 8. Invocations, running and the trace

```csharp
UatStepInvocation.Call : UatCallFrame?   // init; null for a step written in the scenario
```

- **The list stays flat.** The runner, which runs invocations one by one and stops at the first
  failure, does not change; a failing row stops the scenario because its steps are ordinary
  steps. Consumers that ignore `Call` see the inner steps as if they were written out, which is
  right for all of them except the displays below.
- **Invocation fields.** `ScenarioName` and `FromBackground` come from the scenario as today;
  `Step` is the substituted inner step, with its body location in `Step.Source`.
- **Result JSON** (`__result.json` with the runtime trace) adds `call`, the frame chain outermost
  first: `[{ "phrase": "...", "source": "Projects.uat.md:14", "row": 2 }]`.
- **Formatted results** (`UatDiagnosticsFormatter.FormatResults`, the Presenter's copy of it): a run
  of consecutive invocations with the same frame prints the call once, then its steps indented,
  then `row n (values)` headers for `EachRow`, as the plan's trace shows.
- **Presenter.** `UatStepLoadResult` gets `Depth` and `CallText`; the step list indents inner
  steps under a non-runnable header row for the call, and the header's line number is the call's,
  so clicking it opens the scenario, not the phrase file. The live step pointer moves over inner
  steps as over any others.

## 9. Page rules without page tracking

`UatCommandPattern.NamesPage` (bool) is set where the engine registers its page verbs
(`I am on the {page} page`, `I should be on the {page} page`, their `{user}` forms, and the spec
catalog's twins). The rules read the invocations a call just bound:

- If any names a page and the last does not: `UATC006`, "ends on a page it does not say".
- `EachRow`: if any names a page, the last one's `{page}` must equal the first one's: `UATC006`.

These are checked per call, since the inner steps bind per call. When page tracking lands (step
8), it walks the same flat list; frames need nothing more.

## 10. Codes

| Code | When | Where |
| --- | --- | --- |
| `UAT024` | `<table>` / `<doc string>` outside a phrase section, or not under a step | parse |
| `UAT025` | `## Phrase:` with no text | parse |
| `UAT026` | a phrase section with no steps | parse |
| `UAT027` | a malformed `Parameters` table | parse |
| `UAT028` | a phrase file with a non-phrase section, or without one `# Phrases:` heading | parse |
| `UATC001` | a heading `{x}` never used, or a body `<x>` not in the heading (`Once`) | callable |
| `UATC002` | a parameter's types cannot agree (inferred kinds, or declared vs inferred) | callable |
| `UATC003` | a `Parameters` row: no such parameter, unknown type, or an example that does not read | callable |
| `UATC004` | a cycle | callable, and at the call |
| `UATC005` | deeper than three calls | call |
| `UATC006` | ends on a page it does not say; a row that ends on another page than it starts | call |
| `UATC007` | `EachRow` passes `<table>` | callable |
| `UATC008` | a body step with a literal value that does not fit, or bound to a broken C# phrase | callable |

Call-time value and table problems keep their `UATA0xx` and `UATB0xx` codes, relocated
(section 7). "Callable" codes are reported at every call and by the tool at the heading. This
deviates from the plan, which continued the `UATA` range: a separate letter keeps the callable
problems findable in a failure list.

## 11. The tool

- **`validate`** loads each scenario with `UatScenarioFiles.Load`, so calls are checked as in the
  gate. It also checks each `*.phrases.md` under the scenario root on its own: parse errors, and
  each broken callable as a warning at its heading. `--fix` works unchanged (section 7).
- **`phrases`** lists callables after the C# phrases, with the file and line, the types
  (`a project {name: text} with {hours: number} unbilled hours`), `for each row (name: text,
  hours: number)` for `EachRow`, and broken ones as warnings. `--keyword` does not filter
  callables out, since they take every keyword.
- **`snippet`** is unchanged: it suggests a C# method. A tester who wants a callable writes one.
- The copy warning from section 3.

## 12. Not designed here

- **Module phrase files** (plan step 6): the loader's "only the root" rule becomes "the root and
  the file's module folder", and `UatCallableSet` gets a scope per callable. Matching and binding
  do not change.
- **Page tracking** (plan step 8): walks the flat invocation list; `UatCallFrame` already says
  which steps came from a call.
- **`## Background` as a callable**: not done; it keeps its own code.

## 13. Build order and tests

| Plan step | Build | Tests (new file `UatCallableTests`, plus the tool's) |
| --- | --- | --- |
| 1 | `UatOutline`; sections, markers, `ParsePhrases`, `UatDocument.Callables`; `UatScenarioFiles`; `Select`; callable matching; expansion with text types; frames; relocation; `Within`; switch the five callers | parse cases and codes; a call binds to its inner steps in order; a typo in the body is reported at the call with `Within`; ambiguity with a C# phrase; a global phrase file is seen and a subfolder one is not |
| 2 | `Placeholders` sink; inference mode; merge; `TypeNamed`; `Parameters` | inferred decimal, user, record column; `8,5` in a call gives `UATA003` at the call with a fix; a date/number conflict is broken and only its calls fail; a declared type that disagrees |
| 3 | passed `<table>` / `<doc string>`, `BlockType` | a bad cell is reported at the call's row; missing table; table to a callable without one |
| 4 | `EachRow`: columns, rows, row frames | two rows, two expansions; unknown and missing columns; a bad cell at its row; per-row page rule |
| 5 | cycle, depth, page rules, `NamesPage`; trace formatting, result JSON, Presenter rows | a cycle through two callables; a fourth level; the formatted trace; a run where row 2 fails stops the scenario |
| 7 | tool `validate`, `phrases`, the copy warning | `ScenarioValidationTests`: a call error in a scenario, a broken callable warning, `phrases` output |

Plus one sample use, so the whole path runs against real apps: a Todo scenario uses a global
`Scenarios/Todo.phrases.md` callable and an `EachRow` callable (both the MAUI and web heads), and
the csproj files copy the phrase file. Docs at the end: a Callable Phrases section in
`uat-phrases-and-flows.md`, the codes in its diagnostics table, `uat-template-guide.md` for the
file format and copying, an AD-017 for the no-logic line and the no-shadowing rule, and the
changelog.

## 14. As built (2026-09-27)

Plan steps 1-5 and 7 and global phrase files are built; steps 6 (module files) and 8 (pages with
tracking) wait as planned. Where the code differs from the sections above:

- **Files.** Preparation is its own file, `UatPreparedCallables.cs` (with `UatCallableSet`);
  `UatCallables.cs` holds the model, `UatCallFrame` and `UatOutline`. `TypeNamed` lives on
  `UatPreparedCallables`, not `UatValues`, because it looks C# names up among the catalogs' types.
- **Loader.** `UatLoadedScenarioFile.Bind(catalogs)` binds with the file's callables, so callers
  do not pass the set around.
- **Presenter** (section 8). Still one row per invocation, not a header row per call: the run
  pointer moves by index over invocations, and a header would shift it. An inner step's row is
  indented by its depth, carries the call's line (clicking opens the scenario), and
  `UatStepLoadResult.CallText` holds the call as written.
- **Messages.** A relocated diagnostic keeps `Within` and also ends with
  `(in 'phrase' at file:line)`, so a one-line report still names the body step. `UATB002` now
  lists the candidates, since one may be a callable in another file.
- **Fixes.** `8,5` has no fix (not one right answer, as AD-016 decided); a date in a call or in a
  per-row cell does, and `--fix` rewrites it in the call.
- **Phrase files hold no prose**, like scenario files: anything but a phrase section is `UAT028`.
- **Not tested separately:** "row 2 fails, the scenario stops". The runner is unchanged and
  stops at the first failing invocation, which a row's steps are.
- **Tool.** `phrases` prints a callable's declaration as a full path.

Tests: `UatCallableTests` (21: 17 facts and a four-case theory), two in `ScenarioValidationTests`. Sample: `Scenarios/Todo.phrases.md`
with `the synced list has todos` (passes `<table>`) and `each todo shows its status` (per row),
used by TOD.01.2 and run on the MAUI and web heads.
