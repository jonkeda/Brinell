---
title: Generator Changes for Containers and Collections
description: What Brinell.Generator needs before ContainerObjectBase and CollectionObjectBase can be generated
status: design proposal
---

# Generator Changes

Companion to [container-and-collection-design.md](container-and-collection-design.md).
Answers: *do we need changes or additions to the generator?*

## Short answer

**Yes — two blocking changes, one addition.** The generator today assumes every generatable
type has exactly one type parameter named `TScope` and that public members return it. Containers
have two (`TParent`, `TSelf`) and must return `TSelf`. Nothing generates correctly until that
assumption is lifted.

## G1 (blocking) — the fluent return type is hardcoded

Two different hardcodings, both wrong for containers.

### `IsWaitAssertGenerator` emits the literal string `TScope`

[IsWaitAssertGenerator.cs:228](../../tools/Brinell.Generator/Generators/IsWaitAssertGenerator.cs#L228):

```csharp
writer.WriteLine($"public TScope Assert{propertyName}({paramPrefix}...)");
```

Five such sites (lines 228, 275, 298, 369, and the `Assert` in the comparison-variant path).
`TScope` is a literal in the format string — the name of the type parameter is never read from
the analyzed class. On `ContainerObjectBase<TParent, TSelf>` this emits `public TScope AssertX(...)`
referencing a type parameter that does not exist. **Compile error.**

### `ActionGenerator` uses the whole type-parameter list

[ActionGenerator.cs:122-124](../../tools/Brinell.Generator/Generators/ActionGenerator.cs#L122-L124):

```csharp
var returnTypeStr = context.TypeParameters.TrimStart('<').TrimEnd('>');
if (string.IsNullOrEmpty(returnTypeStr))
    returnTypeStr = "void";
```

For `<TScope>` this yields `TScope` — correct by luck. For `<TParent, TSelf>` it yields the
string `TParent, TSelf`, emitting `public TParent, TSelf Click(...)`. **Compile error.**

### Fix

Add a resolved fluent-return-type to `ControlObjectContext` and use it at every site:

```csharp
public sealed class ControlObjectContext
{
    ...
    /// <summary>
    /// The type parameter public members return for fluent chaining.
    /// Controls return the containing scope; containers and collections return themselves.
    /// </summary>
    public string FluentReturnType { get; init; } = "TScope";
}
```

Resolution rule in `ControlObjectAnalyzer.BuildContext` — last-wins, cheapest first:

1. `[FluentReturn(nameof(TSelf))]` on the class, when present — explicit and unambiguous.
2. Otherwise, if the type parameter list contains `TSelf`, use `TSelf`.
3. Otherwise, the single type parameter.
4. Otherwise `void`.

Rule 2 alone covers every type in this design, so the attribute in rule 1 is optional
sugar — but worth having, since it makes the intent greppable and survives a rename.

Then replace the five literals with `{context.FluentReturnType}` and the `ActionGenerator`
computation with `context.FluentReturnType`.

**Do not preserve the old output.** The `.gen.cs` files are build artifacts, not source:
change the generator, then regenerate every template from scratch with
[tools/Scripts/CreateMaui.Bat](../../tools/Scripts/CreateMaui.Bat) and take whatever comes out.
Rule 3 happens to reproduce today's output for single-`TScope` controls, but that is a
convenience, not a constraint to design around — if a better resolution rule changes some
existing output, take the change and update the affected call sites.

## G2 (blocking) — `RunDoWithElement` / `RunWaitWithElement` do not exist on containers

The generated bodies call helpers that live on `ControlBase<TScope>`:

```csharp
return RunDoWithElement(element => { ClickCore(element); }, timeoutMs);
```

`ContainerObjectBase` deliberately does **not** derive from `ControlBase` (that is the whole
point of §3.1 in the main design), so it inherits none of these.

Rather than teach the generator a second emission shape, give `ContainerObjectBase` the same
protected helper surface, returning `TSelf` instead of `TScope`:

| Helper | On `ControlBase<TScope>` | On `ContainerObjectBase<TParent,TSelf>` |
|---|---|---|
| `RunDoWithElement` | `TScope` | `TSelf` |
| `RunWaitWithElement` | `bool` | `bool` |
| `RunAssertWithElement` | `TScope` | `TSelf` |
| `RunWait` / `RunDo` / `Run` | `bool` / `TScope` | `bool` / `TSelf` |

The element these operate on is `ContainerRoot`, not a searched child. With identical names and
shapes, **the generator emits the same text for both** and G1 alone makes it compile. That is
the cheapest correct design: one emission path, two host hierarchies.

Extract the helper bodies into a shared internal helper class to avoid a second copy of the
polling loop — the `RunPoll` implementation in
[ControlBase.cs:74-120](../../srcnew/Brinell.Maui/Controls/ControlBase.cs#L74-L120) is
non-trivial (logging, stale tolerance, last-exception rethrow) and must not be duplicated by hand.

## G3 (addition, non-blocking) — generate the scope factory methods

§4.8 of the main design: `ContainerBase` carries ~30 hand-written `protected` factories
(`Label`, `Button`, `Entry`, …), `PageObjectBase` needs the same list and lacks it, and the
current file already carries `// Note: Picker control is not yet implemented` and comments about
controls that moved to `Brinell.Maui.Extensions`. That drift is the symptom.

Add a **`ScopeFactoryGenerator`** that scans the control namespaces and emits one partial per
scope base:

```csharp
// ScopeFactories.gen.cs  — applied to both page and container bases
public partial class ContainerObjectBase<TParent, TSelf>
{
    public Label<TSelf>  Label(string locator)  => new(this, locator);
    public Label<TSelf>  Label(Locator locator) => new(this, locator);
    public Button<TSelf> Button(string locator) => new(this, locator);
    ...
}
```

This is a **new generator kind** — it is driven by a set of control types, not by `*Core`
methods on one class, so it does not implement `IMemberGenerator` (whose `Matches`/`Extract`
are per-method). It plugs in at the CLI level alongside `ControlObjectGenerator`.

Non-blocking: the factories can stay hand-written for the Grid/CollectionView milestone and be
generated later. Worth doing before the remaining controls are converted, so the list is
generated once rather than hand-copied twice.

## What needs no change

- **`IMemberGenerator` contract** — unchanged; `Matches`/`Extract`/`Generate` still fit.
- **Core-method matching rules** — `protected virtual` + element first parameter still applies
  verbatim to container Core methods.
- **`SetGenerator`** — reads `ReturnType` from the method itself, never assumes `TScope`.
- **`ControlObjectBuilder`** — `ReconstructClassSignature` already preserves generics and
  constraints, so `<TParent, TSelf> where … where …` round-trips correctly today.
- **`CsWriter` / writers** — untouched.

## Ordering

| # | Change | Blocking? |
|---|---|---|
| 1 | `FluentReturnType` on context + analyzer resolution | yes |
| 2 | Replace 5 literal `TScope` sites in `IsWaitAssertGenerator` | yes |
| 3 | Replace return-type computation in `ActionGenerator` | yes |
| 4 | Regenerate every `.tpl.cs` via `CreateMaui.Bat`; review, then build | yes |
| 5 | Shared `Run*` helpers on `ContainerObjectBase` | yes |
| 6 | `[FluentReturn]` attribute | no |
| 7 | `ScopeFactoryGenerator` | no |

## Verification

Regeneration is a full re-emit, not a diff-check. From the Brinell root:

```powershell
tools\Scripts\CreateMaui.Bat
```

The script builds the CLI in Release, then walks `srcnew\Brinell.Maui\Controls` with
`SearchOption.AllDirectories` — **all 30 `.tpl.cs` files across 9 folders**, not just
`Controls\Base`. Every `.gen.cs` beside them is rewritten. (The `convert-control` skill
describes the script as covering `Controls/Base`; it actually covers the whole `Controls`
tree.)

Current template distribution, so the blast radius is known up front:

| Folder | Templates |
|---|---|
| `Base` | 6 |
| `Navigation` | 7 |
| `Display` | 5 |
| `Text`, `Toggle` | 3 each |
| `DateTimes`, `Range` | 2 each |
| `Buttons`, `Selection` | 1 each |

Then:

```powershell
dotnet build srcnew\Brinell.Maui\Brinell.Maui.csproj -v:minimal /nr:false
dotnet build Brinell.sln -v:minimal /nr:false
```

Review the regenerated diff for intent rather than requiring it to be empty — the question is
"is each changed signature the one we want?", not "did anything change?". The compiler catches
signature changes that break callers, which is the real gate.

> **Note:** `srcnew/Brinell.Maui.Extensions` does not currently build at `HEAD` (34 errors:
> missing `ElementActivator`, missing `FindElementWithWait`, obsolete `RunAssert` overloads),
> from the in-flight `ControlBase` refactor. That is unrelated to this design but it blocks the
> solution build and `Brinell.Maui.Tests`, so it has to be fixed before step 4 can be verified
> end to end.

Then add generator unit tests in
[testsnew/Brinell.Generator.Tests](../../testsnew/Brinell.Generator.Tests) covering:

- a single-`TScope` class emits `TScope` (rule 3)
- a `<TParent, TSelf>` class emits `TSelf` (rule 2)
- an explicit `[FluentReturn]` overrides both (rule 1)
- a zero-type-parameter class emits `void`
