# AD-016: Phrase Arguments Are Typed By The Method

A phrase says `{name}`; the bound method's parameter `name` says what the value is. The engine
reads every value a scenario writes - a placeholder on the step line, an Examples cell, a table
cell - with one reader, `BddValues`, in one invariant format whatever the machine culture:

| Kind | Written as |
| --- | --- |
| Date | `12-May-2026` (`3-jan-2027` is read too; English months), or relative: `today`, `+2d` |
| Date and time | `12-May-2026 14:30` |
| Time | `08:30`, 24-hour |
| True/false | `true` or `false`; `on` or `off` for a switch |
| Number | `1,250.75`: `.` for decimals, `,` between groups of three |
| Enum | the member name, any case, spaces allowed (`In Progress`) |
| List | `"A", "B" and "C"`, or a one-column table |
| Record, list of records | the table under the step (`Field | Value` for one record) |
| User | a name from the `Apps` table, as `BddUserRole` |
| App type | `[BddArgumentType]` with `Parse(string)`, or an `IBddArgumentConverter<T>` |

- **Checked at bind time.** A value that does not fit is a `UATA0xx` diagnostic at the step's
  or row's line before any app starts; the step runs with the same value read again against the
  scenario's clock (`IBddClock`) and services.
- **A phrase can be the method name.** An empty keyword attribute (`[BddWhen]`) makes the name
  the phrase, parameters in capitals: `I_open_the_todo_TITLE(string title)` is
  `I open the todo {title}`. A phrase class method named `Given`/`When`/`Then...` needs no
  attribute. A `{user}` form is always declared in text (AD-015).
- **A broken phrase does not break the catalog.** A parameter the phrase does not name, a name
  that cannot be read, two table parameters, a duplicate, a control-verb property on a phrase
  method: the phrase is marked (`UATA010`-`UATA013`), everything else loads, and only a step that
  binds to it fails, naming the declaring method.
- **One checker.** The xUnit gate and the `bruinet data` tool run the same parser, binder and
  reader (as AD-012 does for test data); the tool builds the catalog from the fixture's type
  without starting it, and `--fix` rewrites only values with one right answer.

**Why:** a value written wrongly (`1,5`, `12/05/2026`) used to reach the phrase as whatever
`Convert.ChangeType` made of it on that machine - `1,5` became `15` - and only when the scenario
ran. Typing by the parameter keeps the phrase text plain and the type in one place; a single
invariant format reads the same in Amsterdam and in CI; checking at bind time lets a person or a
model fix a scenario without a device. A phrase that is broken should cost the scenarios that use
it, not the suite.

**Ruled out:** types in phrase text (`{due:date}`: the parameter already says it); optional words
and alternatives in phrases (`todo(s)`, `a/an`: declare a second keyword attribute); regex phrases
(they cannot be listed or checked); culture-dependent formats; inferring a `{user}` form from a
method name (AD-015); throwing while the catalog is built.

**Broken when:** a scenario value is read with `Convert.ChangeType`, `DateTime.Parse` or the
current culture; a bad value first fails when the step runs; one broken phrase stops other
phrases from binding; the tool and the gate disagree about a file.
