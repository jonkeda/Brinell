---
title: Proposed fixes — generator return types and the Brinell.Html click path
description: Three defects found while planning the Brinell.Html upgrade, each shippable on its own, ahead of that plan
---

# Proposed fixes

Three defects surfaced while writing [`.my/html/`](../html/README.md). None was the
subject of that work; all three are independent of it and can ship first. Two are in
`tools/Brinell.Generator` and affect every driver; one is in `srcnew/Brinell.Html`.

**Status:** proposal. Nothing here is implemented. Every "today" claim was read out of the
tree on 2026-09-09; every "after" claim is a prediction.

> **This document corrects [`.my/html/upgrade-plan.md`](../html/upgrade-plan.md) Phase 0
> step 2.** That step proposed fixing `SetGenerator` by switching it to
> `context.FluentReturnType`. That is wrong, and §1 below explains why. The plan has been
> amended to point here.

---

## 1. `SetGenerator` emits a return type that cannot compile on a container

**Severity: latent.** No currently generated file is affected. It fires the first time a
container or collection declares a `Set*Core`, which [Phase 4 of the HTML
plan](../html/upgrade-plan.md) does deliberately.

### Symptom

`SetGenerator.Generate` builds the return type from the raw type parameter list:

```csharp
// tools/Brinell.Generator/Generators/SetGenerator.cs:124
var returnTypeStr = context.TypeParameters.TrimStart('<').TrimEnd('>');
```

For a control that is `<TScope>` this yields `TScope` and is correct. For a container that
is `<TParent, TSelf>` it yields the string `TParent, TSelf`, and the emitted member is:

```csharp
public TParent, TSelf SetActiveTab(string name)   // does not parse
```

`ActionGenerator` (line 127) and `IsWaitAssertGenerator` (line 204) both use
`context.FluentReturnType` here. `SetGenerator` is the only one that does not.

### Why the obvious fix is wrong

Switching line 124 to `context.FluentReturnType` makes it *parse*, and it still would not
compile — because a container's setter is deliberately not fluent-typed:

| Class | `RunSetWithElement` returns | `FluentReturnType` | Agree? |
|---|---|---|---|
| `ViewBase<TScope>` (control) | `TScope` — the containing scope | `TScope` | yes |
| `PageObjectBase<TSelf> : RootedScopeBase<TSelf, TSelf>` | `TSelf` | `TSelf` | yes |
| `ContainerObjectBase<TParent, TSelf> : RootedScopeBase<TSelf, TParent>` | **`TParent`** | `TSelf` | **no** |

`ContainerObjectBase.cs:690` is `protected override TParent SetResult => Parent;`, and the
helper is documented as *"Sets a value on the container, returning the parent scope."* So
`FluentReturnType` would emit `public TSelf SetX(...)` around a `return` of type `TParent`.

The generator also cannot infer `TParent` from the base clause: it sees
`Grid<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>`, and the fact that
`ContainerObjectBase` closes `RootedScopeBase<TSelf, TParent>` lives in another file the
single-file Roslyn pass never reads. Any inference rule here would be a guess.

So the real question is not a generator question. It is: **what should a container's
generated setter return?**

### Proposed fix — make it `TSelf`, everywhere

1. `ContainerObjectBase.cs:690` — `protected override TParent SetResult => Parent;`
   becomes `protected override TSelf SetResult => Self;`
2. Delete the `TSetResult` type parameter from `RootedScopeBase<TSelf, TSetResult>`.
   `RunSetWithElement` returns `TSelf`; the abstract `SetResult` property disappears with
   it (`PageObjectBase.cs:47` already returns `Self`).
3. `SetGenerator.cs:124` becomes `var returnTypeStr = context.FluentReturnType;`,
   matching the other two generators.

**Blast radius today: zero.** Every `RunSetWithElement` call site in the tree is a
`ViewBase`-derived control with a single `TScope` — `RangeControlBase`, `ToggleControlBase`,
`DatePicker`, `TimePicker`, `Entry`, `SearchBar`, `Switch`. The `RootedScopeBase` overload
and `SetResult` have **no callers at all**; they are dead code waiting for the first
container setter, which is why this can be changed for free right now and not later.

**Why `TSelf` is also the better answer, not just the cheaper one.** Within one container,
`ActionGenerator` already returns `TSelf` and `IsWaitAssertGenerator`'s `Assert*` already
returns `TSelf`. Only the setter would leave the scope. `tabs.SetActiveTab("x").Button("save").Click()`
works if the setter returns the container and does not if it returns the parent, and a user
who wants out can always write `.Parent`.

**The counter-argument, stated fairly.** For `ItemContainerBase`, `TParent` is the owning
collection, so `SetResult => Parent` makes `grid.Item(0).SetQuantity(3).Item(1)` chain
across rows. If that pattern is wanted, keep the current runtime and instead add a
`[SetReturn("TParent")]` class attribute as a sibling to the existing
`[FluentReturn]` (`Brinell.Core/Interfaces/FluentReturnAttribute.cs`), defaulting to
`FluentReturnType`. That is more machinery for a case nothing exercises yet, which is why
it is the alternative and not the recommendation.

### Test

Add to `testsnew/Brinell.Generator.Tests/Generators/SetGeneratorTests.cs`, alongside the
existing `Generate_UsesRunSetWithElementAndForwardsTimeout`:

- a `<TParent, TSelf>` class with a `SetXCore`, asserting the emitted signature is
  `public TSelf SetX(` — this test fails today with `public TParent, TSelf SetX(`.
- the existing single-parameter case, asserting it is unchanged.

### Verify

```powershell
dotnet test testsnew\Brinell.Generator.Tests\Brinell.Generator.Tests.csproj -v:minimal /nr:false
tools\Scripts\CreateMaui.Bat
git diff --stat srcnew\Brinell.Maui      # must be empty
dotnet build srcnew\Brinell.Maui\Brinell.Maui.csproj -v:minimal /nr:false
```

The empty diff is the real check: if any `.gen.cs` changes, the claim that no live code
depends on the old behaviour is false.

---

## 2. The generator defaults an element type to `IMauiElement`

**Severity: low today, wrong output for any non-MAUI driver.**

### Symptom

Two places hard-code the MAUI element type as a fallback:

```csharp
// tools/Brinell.Generator/Analysis/ControlObjectAnalyzer.cs:196
return "IMauiElement";      // when no Core method has a first parameter

// tools/Brinell.Generator/Models/ControlObjectContext.cs:25
public string ElementType { get; init; } = "IMauiElement";
```

Everything else in the generator is driver-neutral — `DetectElementType` reads the first
Core method's first parameter, and candidate detection is
`firstParam.Type.ToString().Contains("Element")`, which `IHtmlElement`, `IWpfElement` and
the rest all satisfy. These two lines are the only MAUI knowledge in the tool.

For a `Brinell.Html` template whose Core methods take no element parameter, the generator
would silently emit `IMauiElement` into an HTML file. The failure is a confusing build
error in the wrong project, not a generator error.

### Proposed fix

Make detection failure explicit rather than defaulted:

1. `DetectElementType` throws when no Core method supplies a first parameter, naming the
   class and the reason. Such a template has nothing to generate anyway — the existing
   `FindSilentlySkippedCoreMethods` already takes the position that a near-miss should be
   an error rather than silence, and this is the same principle.
2. Drop the initialiser on `ControlObjectContext.ElementType` (it becomes a required
   `init`), and update the doc comment that names `IMauiElement` as the example, along
   with the matching comment on `MethodInfo.Parameters`.

### Blast radius

None expected: every MAUI template has at least one Core method with an element parameter,
or it would not generate anything today. The regeneration diff proves it.

### Verify

Same four commands as §1 — the empty `git diff` over `srcnew/Brinell.Maui` is the check.

---

## 3. `Brinell.Html` clicks through a shadowed, unpolled path

**Severity: live.** This one affects the shipping HTML driver today.

### Symptom

Two defects in one method.

```csharp
// srcnew/Brinell.Html/Controls/Control.cs:22
public TScope Click() => RunWithElement(element => element.Click());

// srcnew/Brinell.Html/Controls/ClickableControlBase.cs:20  (: Control<TScope>)
public new TScope Click()
{
    return RunWithElement(element => { EnsureEnabledCore(element); element.Click(); });
}
```

**(a) `new`, not `override`.** The derived `Click` hides rather than replaces the base one,
so dispatch depends on the static type of the reference. Anything holding a `ButtonControl`
as `Control<TScope>` gets the base `Click`, silently skipping `EnsureEnabledCore`. Nothing
in the repo does this today — `Control<TScope>` appears only in the two declarations above
— so this is a trap rather than an active bug, but it is a trap with no upside.

**(b) `RunWithElement`, not `RunDoWithElement`.** `RunWithElement` is a bare
`action(FindElement())`: no polling, no `EnsureVisible`, no `[CallerMemberName]` logging.
`RunDoWithElement` is the real helper and does all three. The inconsistency is visible
inside a single file — in `ClickableControlBase`, `DoubleClick` uses `RunDoWithElement`
while `Click`, `RightClick` and `Hover` use `RunWithElement`. **Click is less robust than
double-click on the same control.**

The same pattern runs through `ButtonControl.Submit`, all of `SelectControl`, all of
`ToggleControlBase`, all of `RangeControlBase` and all of `Control<TScope>`.

### Proposed fix — the narrow version

This document proposes only the part that is a defect, not the restructuring:

1. Delete `Control<TScope>` and move `Click` to `ClickableControlBase` alone, dropping the
   `new`. `SendKeys`, `Clear` and `ScrollIntoView` move down to `ControlBase`, which
   already has `SendKeysCore` and `ScrollIntoViewCore`.
2. Switch `Click`, `RightClick` and `Hover` to `RunDoWithElement`, matching `DoubleClick`.

Steps 1 and 2 are separately revertable; do them as two commits.

**What is deliberately not in this fix.** Converting the rest of the driver off
`RunWithElement`, reordering `Clickable`/`Focusable` to match MAUI, and deleting
`RunWithElement` entirely all belong to
[`.my/html/upgrade-plan.md`](../html/upgrade-plan.md) Phases 1–3, where they are staged in
small batches against a recorded test baseline. Doing them here would be the same
behaviour change with none of the staging.

### Blast radius

Step 1 is source-compatible for every concrete control; only a variable typed as
`Control<TScope>` would break, and there are none.

Step 2 changes timing and failure modes for every click in `Brinell.Html.UITests`: clicks
begin to poll, to wait for visibility, and to log. A click that currently throws
immediately on a not-yet-rendered element will instead wait for it — mostly a fix, but it
is a real behaviour change and a slow test may get slower before it gets greener.

### Verify

`testsnew/Brinell.Html.Tests` contains only `GlobalUsings.cs`, so `Brinell.Html.UITests`
is the only signal this driver has. It needs the Blazor sample running
(`BLAZOR_APP_URL`, default `http://localhost:5180`).

**Record the baseline before changing anything** — with no unit tests, a pre-existing
failure read as a regression will cost more than the fix.

```powershell
# baseline first, on an unchanged tree
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false

# then, after each commit
dotnet build srcnew\Brinell.Html\Brinell.Html.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj --filter "FullyQualifiedName~ButtonControlTests|FullyQualifiedName~CheckBoxControlTests" -v:minimal /nr:false
```

`dotnet test` takes **one** `--filter`; `|` is OR. Build all three TFMs — `Brinell.Html`
inherits `net8.0;net9.0;net10.0` and `TreatWarningsAsErrors` from
`srcnew/Directory.Build.props`.

---

## Suggested order

| | Fix | Ships | Depends on |
|---|---|---|---|
| 1 | §2 element-type default | generator only, no output change | — |
| 2 | §1 set return type | generator + `ContainerObjectBase`, no output change | §2 (same regenerate/diff cycle) |
| 3 | §3 HTML click path | behaviour change in the HTML driver | — (independent) |

§1 and §2 are one regeneration cycle and one empty-diff check, so they are naturally one
sitting. §3 is unrelated to both and can go first or last depending on whether a browser
and the sample app are to hand.

All three are prerequisites-in-spirit for [`.my/html/`](../html/README.md) but only §1 is a
hard one: Phase 4 of that plan introduces the first container setter, which is exactly what
trips it.
