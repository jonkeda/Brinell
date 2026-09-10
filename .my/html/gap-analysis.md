---
title: Brinell.Html vs Brinell.Maui — gap analysis
description: What exists in each driver today, and which differences actually block the generator and the container structure
---

# Gap analysis

Read out of the tree on 2026-09-09. Signatures and counts are from the current working
tree, not from memory.

## 1. Headline

| | `Brinell.Maui` | `Brinell.Html` |
|---|---|---|
| Control files | 8 base + ~46 controls, all `.tpl.cs` + `.gen.cs` pairs | 28 hand-written `.cs`, **zero** templates |
| `*Core` methods | the entire public surface is generated from them | **essentially none** — public methods call `RunWithElement` inline |
| Scope root | `RootedScopeBase<TSelf, TSetResult>` — pages *and* containers | none; `HtmlPageObjectBase` and `ContainerBase` each derive from `ObjectBase` separately |
| Containers | `ContainerObjectBase` (caching root, child factories, readiness, scoped find) — 691 lines | `ContainerBase` — 73 lines, find + `IsReady` only |
| Collections | `CollectionObjectBase` + `ItemContainerBase` + `ItemStrategy` (~1080 lines) | none. `ListControl` / `TableControl` return `string?`, not scoped items |
| Control base chain | `ControlObjectBase<TScope>` → `ViewBase` → `Focusable` → `Clickable` → {`Toggle`, `Selector`, `Range`} | `ObjectBase` → `ControlBase` → `Control` → `Clickable` → `Focusable` → {…} |
| Unit tests | `Brinell.Maui.Tests` — 16 files | `Brinell.Html.Tests` — **`GlobalUsings.cs` only** |

## 2. The generator is already almost driver-agnostic

This is the good news, and it is what makes the plan cheap.

`ControlObjectGenerator` and its three member generators are pure Roslyn over source text.
Nothing in `Analysis/`, `Generation/` or `Generators/` references a MAUI type. The element
type is **detected**, not assumed — `ControlObjectAnalyzer.DetectElementType` returns the
first Core method's first parameter type, and candidate detection is
`firstParam.Type.ToString().Contains("Element")`, which `IHtmlElement` satisfies.

So a well-formed `Brinell.Html` `.tpl.cs` generates correctly **today**, with three
exceptions:

| Blocker | Where | Effect |
|---|---|---|
| `"IMauiElement"` fallback | `ControlObjectAnalyzer.cs:196`, `ControlObjectContext.ElementType` default | a template whose Core methods take no parameters emits a MAUI type into an HTML file |
| `CreateMaui.Bat` hardcodes the MAUI Controls folder | `tools/Scripts/CreateMaui.Bat` | no way to regenerate HTML except by invoking the CLI by hand |
| `SetGenerator` return type | `SetGenerator.Generate` uses `context.TypeParameters`, not `context.FluentReturnType` | a two-parameter class (`<TParent, TSelf>`) emits `public TParent, TSelf SetX(...)` — uncompilable |

The third is latent in MAUI only because **no** MAUI container or collection declares a
`Set*Core` (checked: all 12 container/collection templates, zero matches). HTML containers
will declare them, so it will bite. `ActionGenerator` and `IsWaitAssertGenerator` already
use `FluentReturnType` correctly; `SetGenerator` is the odd one out.

## 3. What generated code calls, and whether HTML has it

Generated bodies are fixed strings. Every helper below must exist on the HTML base with a
compatible signature or the `.gen.cs` will not compile.

| Emitted call | `ViewBase` (MAUI) | `ControlBase` (HTML) | Verdict |
|---|---|---|---|
| `RunDoWithElement(element => ...)` | yes | yes | ok |
| `RunDoWithElement(element => ..., timeoutMs)` | yes | yes | ok |
| `RunSetWithElement(value, element => ..., timeoutMs)` | yes | yes | ok |
| `RunGetWithElement(element => ..., timeoutMs)` | yes | yes | ok |
| `RunAssertWithElement(expected, getActual, comparer, message, timeoutMs)` | yes | yes | ok |
| `RunWaitWithElement(expected, element => bool, timeoutMs)` | `RunWaitWithElement<T>(T? expected, ...)` | **`RunWaitWithElement(Func<IHtmlElement,bool>, ...)` — no `expected`** | **must add overload** |
| `Locator` in generated assert messages | `protected Locator Locator` | `protected Locator Locator` | ok |

The runtime gap is exactly **one missing overload**. Everything else the generator emits
already has a home. That is a much smaller hole than the file counts suggest.

## 4. Structural gaps that are not generator problems

These are hand work; the generator neither helps nor hinders.

### 4.1 `ControlBase` bypasses `Brinell.Core`

`ViewBase<TScope> : ControlObjectBase<TScope>` — MAUI controls get `Locator`, `Scope` and
`Page` from `Brinell.Core.Abstractions.Controls.ControlObjectBase<TScope>`.

`ControlBase<TScope> : ObjectBase` — HTML re-declares `Locator` and `ContainingScope`
itself and exposes no `Page`. Anything in `Brinell.Core` that reasons about a control
object generically cannot see an HTML control.

### 4.2 `Control<TScope>` is a redundant layer with a shadowing bug

```csharp
// Controls/Control.cs
public TScope Click() => RunWithElement(element => element.Click());

// Controls/ClickableControlBase.cs : Control<TScope>
public new TScope Click() { ... }
```

`new`, not `override`. A `ButtonControl` held in a `Control<TScope>` variable calls the
unguarded base `Click` and skips whatever `ClickableControlBase.Click` adds. MAUI has no
equivalent layer — `ClickableControlBase` sits straight on `FocusableControlBase`.

`Control<TScope>` also duplicates `SendKeys`, `Clear` and `ScrollIntoView`, all of which
`ControlBase` already provides.

Note the chain order also differs from MAUI: HTML has `Clickable` → `Focusable`
(clickable is the parent), MAUI has `Focusable` → `Clickable`. Reparenting to the MAUI
order is part of the base work, and it is a real behaviour change for anything typed as
`ClickableControlBase`.

### 4.3 `RunWithElement` vs. the `Run*` family

`ControlBase` has both. `RunWithElement` is a bare `action(FindElement())` — no polling,
no logging, no visibility guard. `RunDoWithElement` is the real one, with `RunPoll`,
`EnsureVisible` and `[CallerMemberName]` logging.

Most existing HTML controls use the bare one — `ButtonControl.Submit`, all of
`SelectControl`, all of `ToggleControlBase`, all of `Control<TScope>`, all of
`RangeControlBase` — so **most of the HTML surface is unpolled and unlogged today**.
Converting to Core methods moves all of it onto `RunDoWithElement`. That is a behaviour
improvement, and it is also the single largest regression risk in this plan, because
timing and failure modes change under every existing test at once.

### 4.4 Page and container share nothing

MAUI: `PageObjectBase<TSelf> : RootedScopeBase<TSelf, TSelf>` and
`ContainerObjectBase<TParent, TSelf> : RootedScopeBase<TSelf, TSelf>`. One implementation
of root caching, `IsReady` / `WaitReady`, `TryFindElement`, `FindElements`, child
factories, `IsExists` / `IsVisible` / `Assert*`, and the whole `Run*` family.

HTML: `HtmlPageObjectBase` and `ContainerBase` both extend `ObjectBase` (which is only
`Poll` / `PollAsync`) and each implement readiness and finding separately. `ContainerBase`
has no root caching, no child factories, and no `Run*` family at all — so a container
cannot host a generated member.

### 4.5 No collections

`ListControl.GetItemTexts()` returns `IReadOnlyList<string?>`. There is no way to get a
scoped item you can chain into. `ItemStrategy`, `ItemContainerBase`, `ElementMatch` and the
whole `CollectionObjectBase` surface (`Item(index)`, `Item(key)`, `Items`,
`WaitItemCount`, `ScrollToItem`, `SelectItem`, `AssertItemCount`) have no HTML counterpart.

CSS makes the strategies *easier* here than in MAUI:
`ItemStrategy.ByLocator(Locator.ByCss("li"))` needs no indexed-id fallback and no
scroll-to-materialise dance, because the DOM does not drop offscreen elements out of the
tree the way Android does. `ScrollHelper` and the `IndexedIdItemStrategy` probably do not
need HTML ports at all.

### 4.6 A parallel async surface with no generator story

`Interfaces/Async/` declares nine `IHtmlAsync*` interfaces, implemented explicitly on the
bases and forwarding to the sync method through `Task.FromResult`. It is sync code wearing
a `Task`. The generator emits sync members only. See
[open-questions.md](open-questions.md) §2.

### 4.7 Multi-targeting

`srcnew/Directory.Build.props` sets `net8.0;net9.0;net10.0` and `TreatWarningsAsErrors`.
`Brinell.Html.csproj` inherits all three; `Brinell.Maui.csproj` overrides to `net10.0`
alone. Generated code therefore has to be warning-clean on **net8.0** too, which the MAUI
templates never had to be.

## 5. Control inventory and target base

| HTML control | Today | Target after upgrade |
|---|---|---|
| `ButtonControl`, `LinkControl` | `ClickableControlBase` | `.tpl.cs` on `Base.ClickableControlBase` |
| `TextInputControl`, `TextAreaControl` | `FocusableControlBase` (+ `IHtmlAsyncEditable`) | `.tpl.cs` on `Base.FocusableControlBase` |
| `CheckBoxControl`, `RadioButtonControl` | `ToggleControlBase` | `.tpl.cs` on `Base.ToggleControlBase` |
| `SelectControl`, `RadioGroupControl` | `SelectorControlBase` (3 abstract members) | `.tpl.cs` on `Base.SelectorControlBase` |
| `RangeInputControl`, `DateInputControl`, `TimeInputControl` | `RangeControlBase` | `.tpl.cs` on `Base.RangeControlBase` |
| `LabelControl`, `ProgressControl` | `ControlBase` | `.tpl.cs` on `Base.ViewBase` |
| `ListControl`, `List` | `ControlBase`, flat | `CollectionObjectBase<TParent, TSelf, TItem>` |
| `TableControl` | `ControlBase`, flat | `CollectionObjectBase` with a row `ItemContainerBase` |
| `ScrollContainerControl`, `TabContainerControl` | `ContainerBase` | `ContainerObjectBase<TParent, TSelf>` |

Two names in that table collide once collections land: `List<TScope>` (a control) and
`ListControl<TScope>`. Both are flat readers of `<li>` text; they should become one thing.

## 6. Consumers that will feel the change

| Project | What it uses |
|---|---|
| `testsnew/Brinell.Html.UITests` | 4 page objects, 7 test files, drives the Blazor sample at `localhost:5180` via Playwright |
| `testsnew/Brinell.Html.Uat.Tests` | 2 page objects + a self-hosted sample host |
| `srcnew/Brinell.Html.Playwright` | `PlaywrightHtmlElement` implements `IHtmlElement`; unaffected unless `IHtmlElement` changes |
| `srcnew/Brinell.Blazor` | separate driver, separate controls — **not** affected |

The UITests page objects construct controls as `new(this, "#css-id")`, so the
`(IHtmlScope<TScope>, string)` constructor must survive every phase.
