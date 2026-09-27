# UAT phrases and arguments: design

For [plan.md](plan.md). This fixes the shapes that the binder, the runtime, the Presenter and the
validator share, so the steps can be built one at a time without reopening them.

## 1. The pieces

```text
            method + attributes
                    |
     UatPhraseDeclarations.Of(method)          one place that turns a method into phrases:
                    |                           attribute text, empty attribute, name prefix,
                    v                           and the problems of each (A10)
            UatDeclaredPhrase[]  --------------------------------------------+
                    |                                                          |
      catalog.Register(..., parameters, problem)                               |
                    v                                                          |
            UatCommandPattern  (phrase, parameters with types, Problem)        |
                    |                                                          |
  bind:  UatBinder -> problem? -> UATA010-013 at the step                      |
                   -> UatArguments.Check(pattern, step, context) -> UATA001-007|
                    |                                                          |
  run:   handler -> UatArguments.Read(parameter, invocation, context)          |
                    |                                                          |
                 UatValues.TryRead / Write   (scalars, lists, dates, numbers)  |
                                                                               |
  validator: same catalog, same binder; `phrases` lists patterns and the  <---+
             declarations that could not become a pattern
```

## 2. Values: `UatValues`

```csharp
public static class UatValues
{
    public static bool TryRead(string text, Type type, UatValueContext context, out object? value, out UatValueProblem? problem);
    public static object? Read(string text, Type type, UatValueContext context); // throws UatValueException
    public static string Write(object? value);
    public static bool IsInline(Type type, UatValueContext context);            // a scalar, a list of scalars, or a registered type
    public static string Describe(Type type);                                     // "date", "number", "text", "user", "list of date", ...
}

public sealed record UatValueContext(DateTimeOffset Now, IReadOnlyList<UatAppConfig> Apps, UatArgumentTypes Types, IServiceProvider? Services = null);
public sealed record UatValueProblem(string Code, string Message);
```

- **Only one parser per type**, used for step text, Examples cells (after substitution) and table
  cells. `UatTableBinding` calls it for cells and `Write` for its failure tables.
- **Culture:** every parse and format passes `CultureInfo.InvariantCulture` and an explicit pattern.
  Numbers are checked with a regex first (`^-?\d{1,3}(,\d{3})*(\.\d+)?$` or plain digits), then the
  commas are removed and the rest parsed, so `1,5` never reaches `double.Parse`.
- **Dates:** `^\d{1,2}-[A-Za-z]{3}-\d{4}$`, month looked up in a fixed English table (case-insensitive).
  A value that parses as ISO or `dd/MM/yyyy` produces `UATA002` with the rewritten value, which is
  also what `--fix` writes.
- **Lists:** split on `, ` and a last ` and `, outside quotes. Each item is read as the element type.
  A quoted item keeps its commas.
- **Strings:** surrounding quotes removed, `\"` unescaped. `UatCommandPattern.TryMatch` keeps
  removing outer quotes as today, so a quoted list item is recognised by `UatValues` only when the
  whole value is not one quoted string; to keep that possible the matcher stops stripping quotes
  itself and `UatValues` does it (a `string` target strips them, a list target splits first).
  Arguments of patterns without typed parameters are stripped by the binder as before, so handlers
  that read `invocation.Arguments` see the same strings.

## 3. Typed patterns

```csharp
public sealed record UatPhraseParameter(string Name, Type Type, UatParameterSource Source, bool Optional);
public enum UatParameterSource { Phrase, Table, DocString }

public sealed record UatPhraseProblem(string Code, string Message, string Declaration);

// UatCommandPattern gains:
public IReadOnlyList<UatPhraseParameter> Parameters { get; }   // empty: untyped, arguments stay strings
public UatPhraseProblem? Problem { get; }

// UatCommandCatalog gains:
public IReadOnlyList<UatBrokenDeclaration> Unmatchable { get; } // broken declarations with no text to match
```

- `Parameters` lists what the step supplies: placeholders (`Phrase`), the table-bound record or
  list (`Table`) and a doc-string parameter (`DocString`). Injected parameters (context, invocation,
  token, `UatTable`, settings) are not listed.
- **Untyped patterns** (the built-in page steps, patterns registered by hand, the spec catalog's
  page steps) keep today's behaviour: string arguments, no checks.
- **Control verbs** list their value parameter with the control method's parameter type from the
  declaring interface or class. The runtime converts the value to that type before invoking by
  name, so `SetLevel(int level)` works; every built-in verb takes `string`, so nothing changes for
  them.
- **The spec catalog** (Presenter) gets the control verbs with types from `Brinell.Core`, as the
  runtime does. App phrases in the Presenter come from the runtime catalog, which is typed.

## 4. Declarations and broken phrases: `UatPhraseDeclarations`

```csharp
internal sealed record UatDeclaredPhrase(UatEffectiveStepKeyword Keyword, string? Phrase,
    IReadOnlyList<UatPhraseParameter> Parameters, UatPhraseProblem? Problem);

internal static class UatPhraseDeclarations
{
    public static IReadOnlyList<UatDeclaredPhrase> Of(MethodInfo method, bool inferWithoutAttribute, UatArgumentTypes types);
    public static IReadOnlyList<UatDeclaredPhrase> OfControlVerb(MethodInfo method);
}
```

It replaces `UatPhraseMethods.GetPhrases` (which throws) and `TryInferPhraseFromMethodName`, and
is used by the root phrases, the phrase classes, `UatDiscovery` and the control verbs.

- `inferWithoutAttribute` is true for phrase classes (today's behaviour) and false for the
  fixture: a public fixture method that happens to start with `When` does not become a phrase. On
  the fixture an empty attribute is needed.
- Each problem stays on its phrase. When the phrase text exists (a parameter the phrase does not
  name, a control-verb property on a phrase method) the pattern is registered with its `Problem`
  and matches steps as usual; the binder turns the problem into the step's diagnostic. When there
  is no text (an empty attribute on a control verb, a name that cannot be split), the declaration
  goes to `Unmatchable` and only the validator's `phrases` shows it.
- A duplicate (two empty attributes of one keyword) keeps the first and marks it broken, so a step
  that uses it learns why.
- The binder reports a broken phrase with the step's location and the declaration:
  `UATA010 Step 'When I assign "x" to Eva': the phrase from AssignPhrases.When_I_assign_TODO_to_ASSIGNEE does not name parameter 'due'.`
- `UatDiscovery`'s `UATD004` (control-verb property on a phrase method) becomes the phrase's problem
  with code `UATA013`, and discovery keeps going.

## 5. Binding: `UatArguments.Check`

After a pattern is chosen and the `{user}` rules have run, for a typed pattern:

1. `Problem` set: the problem, at the step. Stop.
2. Each `Phrase` parameter: `UatValues.TryRead(argument, type, context)`. The problem, if any,
   names the parameter and the step.
3. A `Table` parameter: a table must be there (`UATA007`), its columns must be fields (`UATA006`),
   and every cell must read as its field's type (`UATA001`-`004`, at the row's line).
4. A `DocString` parameter of a record type: the JSON must deserialize.

The bind-time context has the `Apps` table, the argument types, and `Now = DateTimeOffset.UtcNow`:
relative dates only need to be well formed at bind time. Converters with a parameterless
constructor run at bind time; converters that need services are checked at run time only.

The binder keeps passing strings in `UatStepInvocation.Arguments`. Values are read again at run
time, with the scenario's clock and services; the second read cannot fail on format, because the
first one passed.

## 6. Running: `UatArguments.Read`

`UatReflectionRuntime.BuildRootPhraseArguments` asks, per parameter, in this order: injected
types; a `Phrase` parameter (`UatValues.Read`); a `Table` parameter (`UatTableBinding.Bind(type)`,
or a one-column table for a list of scalars); a `DocString` parameter (the text, or JSON into the
record). `ConvertArgument` goes.

**The clock.** `UatExecutionContext.Now` (new, `DateTimeOffset?`) is what relative dates resolve
against. `UatScenarioTestBase` sets it from the fixture when the fixture implements the new
`IUatClock` (`DateTimeOffset Now { get; }`), and otherwise leaves it null, which reads as
`DateTimeOffset.UtcNow`. The Todo fixtures implement `IUatClock` with the test set's `Now`.

**Users.** A `UatUserRole` parameter reads its name through the `Apps` table in the context
(`UatExecutionContext.Apps`, set by `UatRuntime.ConfigureContext`). `{user}` is already rewritten to
the user's name by the binder; a `UatUserRole user` parameter gets that user.

```csharp
public sealed record UatUserRole(string Name, string? Role, IReadOnlyList<UatAppConfig> Apps)
{
    public override string ToString() => Name;
}
```

`Role` is the `Role` column of the user's first app row, when the table has one.

## 7. Argument types: `UatArgumentTypes`

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class UatArgumentTypeAttribute : Attribute;          // the type has static Parse(string)

public interface IUatArgumentConverter<T> { T Read(string text); } // throws FormatException on a bad value

public sealed class UatArgumentTypes
{
    public static UatArgumentTypes Discover(IEnumerable<Type> scannedTypes);
    public bool TryRead(string text, Type type, IServiceProvider? services, out object? value, out string? problem, out bool deferred);
}
```

- A type marked `[UatArgumentType]` is read with its `Parse(string)`.
- A converter class found in the scanned types (the composition scan, the fixture's assembly) is
  created with `ActivatorUtilities` from the scenario's services at run time. At bind time it is
  created only when it has a parameterless constructor; otherwise the check is deferred.
- A registered type is inline (it can be a placeholder and a table cell).

## 8. Method names: `UatPhraseInference`

```csharp
internal static class UatPhraseInference
{
    public static bool TryInfer(MethodInfo method, UatEffectiveStepKeyword? attributeKeyword,
        Func<ParameterInfo, bool> isStepParameter, out UatEffectiveStepKeyword keyword, out string phrase, out UatPhraseProblem? problem);
}
```

- Split into words on `_`, then PascalCase within each part; a run of capitals is one word
  (`TODOTo` is `TODO`, `To`).
- Keyword prefix: the first word, if it is exactly `Given`, `When` or `Then`.
- A word of two or more capitals (digits allowed) equal to a step parameter's name, ignoring case,
  is its placeholder; `USER` is `{user}`; other words are lowercased, `I` stays.
- `isStepParameter` decides which parameters must be named: not injected, not the table or doc-string
  parameter (a record, a list of records, or a list of scalars that is not in the name).

## 9. Doc strings

`UatStep` gains `string? DocString`. The parser reads a `"""` line after a step (and after its
table, if any) up to the closing `"""`, and removes the opening line's indentation from each line.
Outline values are substituted into it as into cells.

## 10. The validator

`brinell-data` builds the catalog without creating the fixture: the fixture type is named by the
config (`Runtime: Fixture`), found in the `Pages` assembly, and read by type only.

- `UatReflectionRuntime.ForValidation(Type fixtureType)` builds the same patterns with no handlers:
  pages from the composition catalog, phrase classes, root phrases by type, control verbs.
- The `Apps` table comes from the config, so `{user}` and `UatUserRole` are checked.
- The spike decides the load context for a `net10.0-windows` assembly. The plan: a load context per
  run that resolves from the assembly's folder and falls back to the default context for shared
  framework assemblies.
- `--fix` rewrites only values that `UatValues` reports with a `Fix` (ISO and slash dates, enum
  spellings), in place, keeping the line otherwise as written.

## 11. As built (2026-09-27)

Where the code differs from the sections above:

- **Quotes (section 2).** The matcher still strips outer quotes into `Arguments`, and keeps the
  text as written in `RawArguments` (on `UatCommandMatch` and `UatStepInvocation`). Typed
  parameters read `RawArguments`, so a quoted list item survives.
- **Fixes (section 10).** `Fix` is set only where there is one answer: an ISO date and a full
  month name (`12-March-2026`). An enum's closest name is a "did you mean", not a fix. The
  diagnostic carries it as `UatDiagnostic.Fix` (`UatTextFix`).
- **Names.** `UatUnmatchableDeclaration` (not `UatBrokenDeclaration`);
  `UatReflectionRuntime.ForChecking(Type, TestCompositionCatalog?)` (not `ForValidation`);
  `UatScenarioCheck` holds `CatalogsFor`, `CheckFile`, `ApplyFixes`, `DescribePhrases`, `Snippet`.
- **Clock (section 6).** `UatRuntime.ConfigureContext` sets `UatExecutionContext.Clock`, which is
  read only when a value needs the time; `Now` stays for a context built by hand.
- **Doc-string parameters (section 3).** A required `string` parameter the phrase does not name
  is the doc-string parameter; a record or list parameter the phrase does not name takes the table,
  or a JSON doc string when there is no table. An optional scalar the phrase does not name is left
  to its default (`timeoutMs`).
- **Per-head catalogs in the tool.** For an `IUatAppsHost`, `ModulesFor` is asked of an
  uninitialized fixture (`RuntimeHelpers.GetUninitializedObject`); a head whose modules need a
  started fixture binds against the suite's catalog.
- **Finding the fixture and the apps.** The fixture is looked for in the `Pages` assemblies and in
  the UAT project's own output assembly. An `AppsFile` that the build copies (the mixed suite's
  `apps.md`) is read from the project's `bin` when it is not beside the config.
- **Known gap.** The tool checks the files in the suite's own `Scenarios` folder; a suite that links
  them from another project (the Todo web suite) is checked by running the tool in the project that
  owns them.
