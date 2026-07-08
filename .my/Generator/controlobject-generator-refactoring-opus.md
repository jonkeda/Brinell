# ControlObject Generator Refactoring Plan

**Date:** 2026-07-08
**Status:** proposed
**Purpose:** Refactor `Brinell.Generator` so it generates Brinell **ControlObjects**
from a base ControlObject definition, using a top-level `ControlObjectGenerator`
that registers and runs an `ActionGenerator` and an `IsWaitAssertGenerator`.

---

## 1. Goals

1. The generator should generate **ControlObjects** for Brinell.
2. A **base ControlObject definition** holds the Core methods for two families:
   - **Actions** (e.g. `ClickCore`, `HoverCore`, `SendKeysCore`, `ScrollByCore`)
   - **Is/Wait/Assert** state queries (e.g. `IsVisibleCore`, `IsEnabledCore`,
     `IsExistsCore`)
3. The generator reads those Core methods and generates the public methods.
4. The generators are named:
   - `ControlObjectGenerator`
   - `ActionGenerator`
   - `IsWaitAssertGenerator`
5. `ControlObjectGenerator` **registers** `ActionGenerator` and
   `IsWaitAssertGenerator`, then **runs** them over the analyzed source.
6. Tests and CLI are updated to the new names and composition.

---

## 2. Current State (Before)

Location: `tools/Brinell.Generator/` (library),
`tools/Brinell.Generator.Cli/` (CLI), `testsnew/Brinell.Generator.Tests/` (tests).

| Concern | Current type | Role |
| --- | --- | --- |
| Orchestrator | `MethodWrapperGenerator` | Analyze → generate → format → write |
| Analysis | `Analysis/CoreMethodAnalyzer` | Roslyn parse, find class, run handlers |
| Analysis (parallel) | `Analysis/PropertyMethodAnalyzer` | Group Is/Wait/Assert matches |
| Handler contract | `Handlers/IMethodHandler` | `Matches` / `Extract` / `GenerateWrapper` |
| Action wrappers | `Handlers/CoreMethodHandler` | `*Core` → public action wrapper |
| Is/Wait/Assert | `Handlers/IsPropertyHandler` | `Is*Core` → `Is*` / `Wait*` / `Assert*` |
| Assembly | `Generation/WrapperGenerator` | Build compilation unit / class |
| Assembly (parallel) | `Generation/PropertyWrapperGenerator` | Delegate to handler |
| Formatting | `Generation/CodeFormatter` | Format output |
| Writing | `Writers/CsWriter` | Low-level C# emission |
| Options | `Models/GeneratorOptions`, `HandlerOptions`, `MethodInfo`, `PropertyMethodGroup` | Config + portable metadata |

### Observed patterns the generator reproduces

**Action** (from `ClickableControlBase.cs`):

```csharp
protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null) { ... }
// generates:
public TScope Click(int? timeoutMs = null)
    => RunDoWithElement(element => { EnsureClickableCore(element); ClickCore(element); }, timeoutMs);
```

**Is/Wait/Assert** (from `ControlBase.cs`):

```csharp
protected virtual bool? IsVisibleCore(IMauiElement? element) { ... }
// generates:
public bool   IsVisible();
public bool   WaitVisible(bool? expected, int? timeoutMs = null);
public TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
```

### Problems

- Two parallel analysis/assembly paths (`CoreMethod*` vs `Property*`) that do the
  same job with different names.
- The public API is called "MethodWrapper", not "ControlObject", which hides the
  intent (generating ControlObjects).
- `IMethodHandler` mixes "how to match" and "how to emit"; there is no single
  place that registers the action vs Is/Wait/Assert families.

---

## 3. Target State (After)

### 3.1 New generator types

```
ControlObjectGenerator
 ├── registers ActionGenerator          (Action family:   *Core → public action)
 └── registers IsWaitAssertGenerator    (State family:    Is*Core → Is/Wait/Assert)
```

`ControlObjectGenerator` is the single public entry point. It owns the analyzer,
the assembly, and the formatter, and it holds an ordered list of registered
member generators.

#### `IMemberGenerator` (replaces `IMethodHandler`)

A member generator both recognizes and emits a family of methods:

```csharp
public interface IMemberGenerator
{
    // Does this generator handle this Core method?
    bool Matches(MethodDeclarationSyntax method);

    // Portable metadata for generation.
    MethodInfo Extract(MethodDeclarationSyntax method);

    // Emit the public member(s) for one Core method.
    string Generate(MethodInfo coreMethod, ControlObjectContext context);
}
```

`ControlObjectContext` carries the containing type name, type parameters
(e.g. `<TScope>`), and the element parameter type so the same generators can be
reused across platforms (`IMauiElement`, `IWpfElement`, `IHtmlElement`, ...).

#### `ActionGenerator` (from `CoreMethodHandler`)

- Matches `protected virtual <void|T> XxxCore(IElement element, ...)` where the
  name ends in `Core` and is **not** an `Is*Core` state query.
- Emits the public action wrapper using `RunDoWithElement` and the appropriate
  `Ensure*Core` guard.

#### `IsWaitAssertGenerator` (from `IsPropertyHandler`)

- Matches `protected virtual bool? Is*Core(IElement? element)`.
- Emits the trio: `Is*()`, `Wait*(bool?, int?)`, `Assert*(bool?, string?, int?)`.

### 3.2 `ControlObjectGenerator` responsibilities

```csharp
public sealed class ControlObjectGenerator
{
    private readonly List<IMemberGenerator> _generators = new();
    private readonly ControlObjectAnalyzer _analyzer = new();   // renamed CoreMethodAnalyzer
    private readonly ControlObjectBuilder  _builder  = new();   // renamed WrapperGenerator
    private readonly CodeFormatter         _formatter = new();

    public ControlObjectGenerator Register(IMemberGenerator generator)
    {
        _generators.Add(generator);
        return this;
    }

    public string Generate(string sourceCode, GeneratorOptions options)
    {
        var (classDecl, root) = _analyzer.FindTarget(sourceCode, options.TargetClassName);
        var context = _analyzer.BuildContext(classDecl, root);

        var members = new List<string>();
        foreach (var method in _analyzer.CoreMethods(classDecl))
        foreach (var generator in _generators)
        {
            if (!generator.Matches(method)) continue;
            var info = generator.Extract(method);
            members.Add(generator.Generate(info, context));
            break; // first matching generator wins
        }

        var unit = _builder.BuildCompilationUnit(classDecl, members, context, options);
        return _formatter.Format(unit);
    }

    public void GenerateToFile(string input, string output, GeneratorOptions options) { ... }

    // Convenience default wiring.
    public static ControlObjectGenerator CreateDefault() =>
        new ControlObjectGenerator()
            .Register(new IsWaitAssertGenerator())  // register state family first
            .Register(new ActionGenerator());       // then the broader action family
}
```

> **Registration order matters.** `IsWaitAssertGenerator` must be registered
> before `ActionGenerator`, because `Is*Core` returns `bool?` and would otherwise
> be captured by a permissive action matcher. Alternatively, `ActionGenerator`
> explicitly excludes the `Is*Core` shape.

### 3.3 Renames / consolidation

| Before | After | Notes |
| --- | --- | --- |
| `MethodWrapperGenerator` | `ControlObjectGenerator` | Top-level coordinator + registry |
| `IMethodHandler` | `IMemberGenerator` | `GenerateWrapper` → `Generate` |
| `Handlers/CoreMethodHandler` | `Generators/ActionGenerator` | Action family |
| `Handlers/IsPropertyHandler` | `Generators/IsWaitAssertGenerator` | State family |
| `Analysis/CoreMethodAnalyzer` | `Analysis/ControlObjectAnalyzer` | Single analysis path |
| `Analysis/PropertyMethodAnalyzer` | *(removed)* | Folded into analyzer + registry loop |
| `Generation/WrapperGenerator` | `Generation/ControlObjectBuilder` | Assembly |
| `Generation/PropertyWrapperGenerator` | *(removed)* | Folded into registry loop |
| `Models/PropertyMethodGroup` | *(removed)* | No longer needed |
| `Models/HandlerOptions` | `Models/MemberGeneratorOptions` | Same fields, renamed |
| `Generation/CodeFormatter`, `Writers/CsWriter` | *(unchanged)* | Reused as-is |
| `ControlObjectContext` | *(new)* | Carries type name, type params, element type |

Folders: rename `Handlers/` → `Generators/`. Keep `Analysis/`, `Generation/`,
`Models/`, `Writers/`.

---

## 4. CLI Changes (`tools/Brinell.Generator.Cli/Program.cs`)

Current CLI wires `new MethodWrapperGenerator()` with a hard-coded
`new CoreMethodHandler()` handler list. Update to use the registry:

```csharp
var generator = ControlObjectGenerator.CreateDefault();     // Action + IsWaitAssert
generator.GenerateToFile(inputFile, outputFile, options);
```

Optional flag to select families (keeps single-family generation possible):

```
--members actions        only ActionGenerator
--members state          only IsWaitAssertGenerator
--members all (default)  both, via CreateDefault()
```

Wiring for a filtered run:

```csharp
var gen = new ControlObjectGenerator();
if (members is "all" or "state")   gen.Register(new IsWaitAssertGenerator());
if (members is "all" or "actions") gen.Register(new ActionGenerator());
```

Keep existing `--input/-i`, `--output/-o`, `--class/-c`. Update the usage banner
text from "Method Wrapper Generator" to "ControlObject Generator".

---

## 5. Test Changes (`testsnew/Brinell.Generator.Tests/`)

### 5.1 Rename / rewrite

| Current test file | Action |
| --- | --- |
| `MethodWrapperGeneratorTests.cs` | Rename → `ControlObjectGeneratorTests.cs`; swap `MethodWrapperGenerator` + handler list for `ControlObjectGenerator.CreateDefault()` |
| `Handlers/CoreMethodHandlerTests.cs` | Move → `Generators/ActionGeneratorTests.cs`; rename type under test |
| `IsPropertyHandlerTests.cs` | Move → `Generators/IsWaitAssertGeneratorTests.cs` |
| `Analysis/CoreMethodAnalyzerTests.cs` | Rename → `Analysis/ControlObjectAnalyzerTests.cs` |
| `Integration/IntegrationTests.cs` | Update to `ControlObjectGenerator.CreateDefault()`; golden files below |
| `Fixtures/SampleCodeFixtures.cs` | Keep; add a combined action + `Is*Core` fixture |

### 5.2 New tests

- `Generators/ActionGeneratorTests.cs`: `Matches`, `Extract` (skips element,
  keeps `timeoutMs`), `Generate` emits `RunDoWithElement` + `Ensure*Core`, and
  **rejects** `Is*Core`.
- `Generators/IsWaitAssertGeneratorTests.cs`: emits all three of `Is*`, `Wait*`,
  `Assert*` from a single `Is*Core`.
- `ControlObjectGeneratorTests.cs`: registration + run; a class with **both**
  action and state Core methods produces action wrappers **and** the
  Is/Wait/Assert trio in one output; registration-order dedup (a `bool? Is*Core`
  is handled by `IsWaitAssertGenerator`, not `ActionGenerator`).

### 5.3 Golden files (`TestData/`)

- Keep `Input/SimpleClickableClass.input.cs`, `MultiMethodClass.input.cs`,
  `ControlBase.input.cs`.
- Regenerate `Expected/*.expected.cs` from `CreateDefault()` output.
- Add `Input/MixedControl.input.cs` (action + `Is*Core`) and its expected file to
  prove combined generation.

### 5.4 Assertions to preserve

Existing string assertions still hold and should keep passing:
`public TScope Click`, `RunDoWithElement`, `partial class`, `<TScope>`,
`where TScope`, `namespace Brinell.Maui.Controls`, `auto-generated`, plus the
`ThrowsWhenClassNotFound` behavior.

---

## 6. Migration Steps

1. Add `ControlObjectContext`, `IMemberGenerator`, and
   `MemberGeneratorOptions` (rename of `HandlerOptions`).
2. Port `CoreMethodHandler` → `ActionGenerator` (add `Is*Core` exclusion in
   `Matches`).
3. Port `IsPropertyHandler` → `IsWaitAssertGenerator`.
4. Rename `CoreMethodAnalyzer` → `ControlObjectAnalyzer`; add `BuildContext`,
   `CoreMethods`, `FindTarget`. Remove `PropertyMethodAnalyzer`.
5. Rename `WrapperGenerator` → `ControlObjectBuilder`; remove
   `PropertyWrapperGenerator` and `PropertyMethodGroup`.
6. Add `ControlObjectGenerator` with `Register` + `CreateDefault`; delete
   `MethodWrapperGenerator`.
7. Update CLI wiring and usage banner.
8. Rename/rewrite tests; regenerate golden files.
9. Update `.my/Generator/*` docs to reference the new names.

Per user preference, **no backward-compatibility shims** — delete the old types
rather than keeping aliases.

---

## 7. Verification

From the Brinell root:

```powershell
dotnet build tools\Brinell.Generator\Brinell.Generator.csproj -v:minimal /nr:false
dotnet build tools\Brinell.Generator.Cli\Brinell.Generator.Cli.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Generator.Tests\Brinell.Generator.Tests.csproj -v:minimal /nr:false
```

Smoke test the CLI against a real base:

```powershell
dotnet run --project tools\Brinell.Generator.Cli -- `
  -i srcnew\Brinell.Maui\Controls\ClickableControlBase.cs `
  -o srcnew\Brinell.Maui\Controls\ClickableControlBase.g.cs
```

---

## 8. Open Questions

- Should `ActionGenerator` infer the guard (`EnsureClickableCore`,
  `EnsureSettableCore`, `EnsureVisibleCore`) from the source base class, or take
  it from `MemberGeneratorOptions`? Current code hard-codes `EnsureClickableCore`.
- Do we generate into `*.g.cs` partial classes checked into `srcnew/`, or run the
  generator as a build step? This plan assumes explicit `*.g.cs` files.
- Element type is currently `IMauiElement`-specific in several strings; confirm
  `ControlObjectContext.ElementType` fully removes that coupling before reusing
  the generators for WPF/WinForms/HTML bases.
