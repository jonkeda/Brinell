---
title: Plan — upgrade Brinell.Html to the generator, container and base structure
description: Phased, individually shippable work bringing Brinell.Html onto the Brinell.Maui architecture
---

# Plan: upgrade `Brinell.Html`

**Status:** not started. Nothing below is implemented.
**Scope:** `srcnew/Brinell.Html`, `tools/Brinell.Generator`, `tools/Scripts`,
`testsnew/Brinell.Html.Tests`, `testsnew/Brinell.Html.UITests`.
**Builds on:** [gap-analysis.md](gap-analysis.md). Read §2 and §3 before Phase 0 —
they say precisely how small the generator gap is.

---

## 0. Shape of the work

Three strands, in dependency order:

```
Phase 0  generator becomes driver-agnostic         (tools/, no HTML changes)
   ↓
Phase 1  ControlBase gains the missing runtime      (one overload + reparent)
   ↓
Phase 2  Controls/Base/ hierarchy as .tpl.cs        (the base class structure)
   ↓
Phase 3  leaf controls converted to .tpl.cs         (the generator, at scale)
   ↓
Phase 4  RootedScopeBase + ContainerObjectBase      (the container structure)
   ↓
Phase 5  CollectionObjectBase + ItemContainerBase   (collections)
   ↓
Phase 6  async surface resolved                     (see open-questions §2)
   ↓
Phase 7  test parity                                (Brinell.Html.Tests is empty today)
```

Phases 0–3 are the "generator" half of the request; 4–5 are the "container and base class
structure" half. 1 and 2 straddle both: the base classes are what generated members land
on, and they are also what containers will reuse.

Each phase ends green. Do not start the next one on a red build.

---

## Phase 0 — make the generator driver-agnostic

**Why first:** every later phase runs the generator. Doing this after Phase 3 means
regenerating everything twice and reviewing the churn twice.

**Files:** `tools/Brinell.Generator/`, `tools/Scripts/`, `testsnew/Brinell.Generator.Tests/`.

1. **Remove the `IMauiElement` default.** `ControlObjectContext.ElementType` and
   `ControlObjectAnalyzer.DetectElementType` both fall back to the literal
   `"IMauiElement"`. Make detection failure an explicit error rather than a wrong default:
   a template with no parameterised Core method has nothing to generate anyway, so throw
   with the class name and the reason. Update the doc comments on
   `ControlObjectContext.ElementType` and `MethodInfo.Parameters` that name `IMauiElement`.

2. **Fix `SetGenerator`'s return type.** See
   [`.my/fix/generator-and-html-base-defects.md`](../fix/generator-and-html-base-defects.md) §1
   for the full proposal — this step is a summary of it.

   > **Corrected.** An earlier draft of this step said to change
   > `context.TypeParameters` to `context.FluentReturnType`, matching `ActionGenerator`.
   > That parses but still does not compile: `ContainerObjectBase<TParent, TSelf>` closes
   > `RootedScopeBase<TSelf, TParent>`, so its `RunSetWithElement` returns `TParent` while
   > `FluentReturnType` is `TSelf`. The fix has to settle what a container's setter
   > returns, not just which existing property to read.

   The proposal: make `ContainerObjectBase.SetResult` return `Self`, delete the now-unused
   `TSetResult` type parameter, and then `SetGenerator` can use `FluentReturnType` like the
   other two generators. Free today — the `RootedScopeBase` overload of
   `RunSetWithElement` has no callers, and every live call site is a single-`TScope`
   control. Add a `SetGeneratorTests` case for a `<TParent, TSelf>` class asserting the
   emitted signature is `public TSelf Set...`.

3. **Parameterise the generation script.** Replace `tools/Scripts/CreateMaui.Bat` with a
   script taking the driver as an argument, plus thin wrappers so existing muscle memory
   and docs keep working:

   ```
   tools/Scripts/Generate.Bat <driver>     # driver = Maui | Html
   tools/Scripts/CreateMaui.Bat            # → Generate.Bat Maui
   tools/Scripts/CreateHtml.Bat            # → Generate.Bat Html
   ```

   Input path becomes `srcnew\Brinell.<driver>\Controls`. The CLI already accepts
   `;`-separated files and folders and always writes `<name>.gen.cs` beside the input, so
   nothing else changes.

4. **Regenerate MAUI and confirm a zero diff.** This is the whole verification for the
   phase: `CreateMaui.Bat` then `git diff --stat srcnew/Brinell.Maui` must be empty. If
   step 1 or 2 changed MAUI output, one of them is wrong.

**Verify**

```powershell
dotnet test testsnew\Brinell.Generator.Tests\Brinell.Generator.Tests.csproj -v:minimal /nr:false
tools\Scripts\CreateMaui.Bat
git diff --stat srcnew\Brinell.Maui      # must be empty
dotnet build srcnew\Brinell.Maui\Brinell.Maui.csproj -v:minimal /nr:false
```

**Ships:** a generator any driver can use. No HTML file changes yet.

---

## Phase 1 — `ControlBase` gains the missing runtime

**Why:** generated bodies call five helpers. HTML has four. Until the fifth exists, no
`.gen.cs` compiles.

**Files:** `srcnew/Brinell.Html/Controls/ControlBase.cs`, `srcnew/Brinell.Html/ObjectBase.cs`.

1. **Add the `expected`-taking `RunWaitWithElement<T>` overload**, mirroring
   `ViewBase.tpl.cs:190`:

   ```csharp
   protected bool RunWaitWithElement<T>(T? expected, Func<IHtmlElement, bool> coreOperation,
       int? timeoutMs = null, [CallerMemberName] string? caller = null)
   ```

   The `expected` value exists to reach `RunPoll`'s log line (the existing overload passes
   `null` there and loses it). Keep the existing overload — hand-written callers use it.

2. **Reparent `ControlBase<TScope>` to `ControlObjectBase<TScope>`.** Drop the local
   `Locator` and take it from `Brinell.Core`; pass the scope through as `IElementScope`.
   This is what gives HTML controls a `Page`, which generated assert messages and the
   logger both want. `ObjectBase` stays as the base for pages until Phase 4 replaces it.

3. **Decide `RunWithElement`'s fate now, remove it in Phase 3.** Mark it
   `[Obsolete]`-in-comment (not the attribute — `TreatWarningsAsErrors` is on) and stop
   using it in new code. Every call site is converted in Phases 2–3; delete it at the end
   of Phase 3.

4. **Port the guard hooks the MAUI bases assume:** `EnsureReadyForActionCore(IHtmlElement)`
   as a `protected virtual` no-op. `ViewBase` calls it from `ResolveReadyElement`;
   HTML's `RunDoWithElement` should do the same so subclasses have one place to add
   pre-action checks.

**Verify**

```powershell
dotnet build srcnew\Brinell.Html\Brinell.Html.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false
```

The UITests need the Blazor sample running (`BLAZOR_APP_URL`, default
`http://localhost:5180`). **Record the pass/fail baseline before touching anything** —
`Brinell.Html.Tests` is empty, so UITests are the only signal this driver has, and a
pre-existing failure misread as a regression will cost a day.

**Ships:** an HTML base that generated code can compile against. Public API unchanged.

---

## Phase 2 — the base class structure

**Why:** this is where the shape of every control is decided. Do it before converting
leaves, not after.

**Target hierarchy**, mirroring `srcnew/Brinell.Maui/Controls/Base/`:

```
ControlObjectBase<TScope>            (Brinell.Core)
└── ViewBase<TScope>                 .tpl.cs — find, visible, enabled, exists, text, attribute, scroll
    └── FocusableControlBase<TScope> .tpl.cs — focus, blur, has-focus
        └── ClickableControlBase<TScope>  .tpl.cs — click, double, right, hover
            └── ToggleControlBase<TScope> .tpl.cs — checked, check, uncheck, toggle
        └── SelectorControlBase<TScope>   .tpl.cs — select by value/text/index, selected value
        └── RangeControlBase<TScope>      .tpl.cs — min, max, step, value
```

1. **Move `ControlBase.cs` → `Controls/Base/ViewBase.tpl.cs`** with `git mv` to keep
   history. Make the class `partial`. Keep the name `ControlBase` **or** rename to
   `ViewBase` — see [open-questions.md](open-questions.md) §1; the rest of this plan
   assumes `ViewBase`, with a `ControlBase` type alias retained for one release.

2. **Convert its public surface to Core methods.** `ControlBase` already has
   `GetTextCore`, `GetAttributeCore`, `ScrollIntoViewCore`, `IsVisibleCore`,
   `IsEnabledCore`, `IsExistsCore`, `SendKeysCore` — but most are `protected` without
   `virtual`, which the generator silently skipped before and now **errors** on
   (`FindSilentlySkippedCoreMethods`). Make each `protected virtual`, then delete the
   hand-written `IsVisible` / `WaitVisible` / `AssertVisible` / `GetText` / `WaitText` /
   `AssertText*` / `IsEnabled` / … wrappers — the generator emits all of them.

   `AssertTextContains` / `StartsWith` / `EndsWith` / `Empty` come back from
   `[GenerateComparisons(Comparison.Equals | Comparison.Contains | Comparison.StartsWith | Comparison.EndsWith | Comparison.Empty)]`
   on `GetTextCore`. Diff the generated names against the five hand-written ones; they
   must match exactly.

3. **Delete `Control<TScope>`.** Fold `SendKeys` / `Clear` / `ScrollIntoView` into
   `ViewBase` Core methods and move `Click` to `ClickableControlBase` only. This removes
   the `public new TScope Click()` shadow (gap-analysis §4.2) — a real behaviour fix, and
   the one change in this plan most likely to alter what an existing test does. Call it
   out in the commit.

4. **Reorder the chain to `Focusable` → `Clickable`.** HTML currently has it inverted.
   Anything declared `ClickableControlBase` keeps its focus members (they move up, not
   away), but `FocusableControlBase` loses click members. `TextInputControl`,
   `SelectControl`, `RadioGroupControl` and the range controls sit on `Focusable` and
   **do** click today via inheritance — check each before flipping.

5. **Name every guard `Ensure*`.** `EnsureSettableCore`, `EnsureEnabledCore`,
   `EnsureVisibleCore` already comply; `ActionGenerator` skips the prefix, so they stay
   `protected virtual` and emit nothing.

6. **Generate and diff the public surface.** For each base:
   `grep -E "public (TScope|bool|string|int)" Foo.gen.cs` against the original class.
   Every member the old class exposed must appear. Report anything dropped.

**Verify**

```powershell
tools\Scripts\CreateHtml.Bat
dotnet build srcnew\Brinell.Html\Brinell.Html.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false
```

Watch the net8.0 leg specifically — `TreatWarningsAsErrors` plus multi-targeting means
generated code that is clean on net10.0 can still fail the build (gap-analysis §4.7).

**Ships:** the base class structure, generated. Six `.tpl.cs` + six `.gen.cs` under
`Controls/Base/`.

---

## Phase 3 — convert the leaf controls

**Why:** mechanical once Phase 2 lands. Follow the `convert-control` skill
(`.claude/skills/convert-control/SKILL.md`) — it is written for MAUI but every rule is
driver-neutral except the type names.

Order, easiest first, one commit each:

| Batch | Controls | Notes |
|---|---|---|
| 3a | `ButtonControl`, `LinkControl` | `Submit` → `SubmitCore`; smallest possible pilot |
| 3b | `LabelControl`, `ProgressControl` | pure `Get*Core`, no actions |
| 3c | `CheckBoxControl`, `RadioButtonControl` | toggle behaviour moves into `ToggleControlBase` Core methods |
| 3d | `TextInputControl`, `TextAreaControl` | `SetText` → `SetTextCore`; watch `TypeText` vs `Fill` semantics |
| 3e | `RangeInputControl`, `DateInputControl`, `TimeInputControl` | `Get/SetValue` are `string?` today; keep them string-typed, do not retype to `double`/`DateTime` in this phase |
| 3f | `SelectControl`, `RadioGroupControl` | the three `abstract` members become `protected virtual *Core`; `SelectByText`'s option-walk stays hand-written in the template |

For every control:

1. `git mv Foo.cs Foo.tpl.cs`, class becomes `partial`.
2. Reparent to `Base.*`.
3. Every public method either becomes a `protected virtual *Core` or stays hand-written
   with a stated reason.
4. Guards fold **into** Core bodies — the generator injects none.
5. `int? timeoutMs = null` on any Core whose public form had it.
6. Regenerate, diff the public surface, build.

**Then delete `RunWithElement`** (Phase 1 step 3) and confirm no call sites remain.

**Verify per batch**

```powershell
tools\Scripts\CreateHtml.Bat
dotnet build srcnew\Brinell.Html\Brinell.Html.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj --filter "FullyQualifiedName~ButtonControlTests" -v:minimal /nr:false
```

Scope the filter to the batch. Run the whole UITests project only at the end of Phase 3.
`dotnet test` takes **one** `--filter`; use `|` for OR.

**Ships:** every leaf control generated. `Brinell.Html/Controls/` is `.tpl.cs` + `.gen.cs`
throughout, matching `Brinell.Maui/Controls/`.

---

## Phase 4 — the container structure

**Why:** this is the half of the MAUI upgrade that has no HTML counterpart at all. It is
also what makes page objects composable instead of flat.

**New folder:** `srcnew/Brinell.Html/Containers/`, mirroring
`srcnew/Brinell.Maui/Containers/`.

1. **`RootedScopeBase<TSelf, TSetResult>`** — port from
   `Brinell.Maui/Containers/ContainerObjectBase.cs` (it is declared in that file, lines
   22–~120). Root caching (`CacheContainerRoot`, `IsCachedRootValid`, `InvalidateCache`),
   scoped `TryFindElement` / `FindElement` / `FindElements`, `IsReady` / `WaitReady`,
   `IsExists` / `IsVisible` / `WaitExists` / `WaitVisible` / `AssertExists` /
   `AssertVisible`, and the `Run*` family including `RunSetWithElement` returning
   `TSetResult`.

   **HTML simplification:** MAUI caches the root because re-finding through UI Automation
   is expensive and roots go stale. In the DOM, `TryFindElement` is cheap and Playwright
   locators re-resolve on use. Start with `CacheContainerRoot => false` for HTML and turn
   it on only if a measurement says it matters. Do not port the staleness dance
   speculatively.

2. **`ContainerObjectBase<TParent, TSelf> : RootedScopeBase<TSelf, TSelf>`** implementing
   `IHtmlContainer<TParent, TSelf>` (which already exists and already has the right shape).
   Bring the child factories across — `Child<TControl>(string)`, `Label`, `Button`,
   `TextInput`, `CheckBox` — with CSS selectors instead of automation ids.

3. **Reparent `HtmlPageObjectBase<TSelf>` to `RootedScopeBase<TSelf, TSelf>`**, matching
   `PageObjectBase<TSelf>`. Keep `Url`, `CompareUrl`, `GetTitle`, `IsLoaded`,
   `TakeScreenshot` — those are HTML-specific and have no MAUI analogue. Delete the
   readiness code that `RootedScopeBase` now provides.

   MAUI's busy-signal machinery (`BusySignalPolicy`, `BusyAutomationId`, `IsBusy`,
   `WaitIdle`, `ProbeReadiness`) is **not** in scope here. The HTML equivalent is
   Blazor-circuit / network-idle readiness, which `BlazorSampleTestBase` currently does by
   hand in the test project. Moving it into the driver is worth doing, but as its own
   piece of work after this plan lands.

4. **Retire `ContainerBase<TParent, TScope>`.** Reparent `ScrollContainerControl` and
   `TabContainerControl` onto `ContainerObjectBase`; keep `ContainerBase` as a
   `[Obsolete]`-in-comment shim for one release if anything outside the repo uses it.

5. **`ComponentObjectBase<TParent, TSelf>`** — the thin "container that is a reusable
   component" wrapper. Port it once `ContainerObjectBase` is proven.

**Verify**

```powershell
dotnet build srcnew\Brinell.Html\Brinell.Html.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false
```

Add one UITest that scopes a control inside a container and proves the scoping is real —
two elements with the same selector in different containers, resolved to different
elements. Without that test the container structure is unverified regardless of what
compiles.

**Ships:** pages and containers on one root. Containers can host generated members.

---

## Phase 5 — collections

**Why:** `ListControl` and `TableControl` return strings today; there is no scoped item.

1. **`IItemStrategy` + `ItemStrategy.ByLocator` / `Within`** — port from
   `Brinell.Maui/Containers/ItemStrategy.cs`. **Do not port `ByIndexedId`** (a MAUI
   workaround for controls without addressable children) or `ScrollHelper` (a workaround
   for offscreen elements leaving the Android tree). Neither problem exists in the DOM;
   see the memory note on Android offscreen elements for why they exist at all.

2. **`ItemContainerBase<TCollection, TSelf>`** — an item is a container rooted at its own
   element, with `Index`. Port as-is; it is driver-neutral apart from the element type.

3. **`CollectionObjectBase<TParent, TSelf, TItem>`** — `Item(index)`, `Item(string key)`,
   `Item(Locator key)`, `TryItem*`, `Items`, `ToList`, `GetItemCount`, `IsEmpty`,
   `WaitItemCount`, `WaitAnyItem`, `AssertItemCount`, `AssertEmpty`, `FindItem`,
   `ItemWhere`, `SelectItem`.

   `ScrollToItem` / `ScrollToTop` / `ScrollToEnd` become one-liners over
   `element.ScrollIntoView()` rather than the MAUI scroll machinery.

4. **Rebuild the collection controls on it:**
   - `ListControl<TParent, TSelf, TItem>` over `ItemStrategy.ByLocator(Locator.ByCss("li"))`.
   - `TableControl<TParent, TSelf, TRow>` over `tbody tr`, with a `TableRow` item exposing
     cells by index and by header text.
   - **Delete `List<TScope>`** — it and `ListControl` are the same thing (gap-analysis §5).

5. **`ElementMatch`** — port only if `Item(Locator key)` needs it; it may be simpler in
   CSS-land.

**Verify:** the `DataTablePage` page object and `DataTable.razor` sample page already
exist. Rewrite `DataTablePage` to use the new `TableControl` and assert on scoped rows —
that is the end-to-end proof.

**Ships:** collections that hand out chainable, scoped items.

---

## Phase 6 — resolve the async surface

Blocked on [open-questions.md](open-questions.md) §2. Whichever way it goes, it is a
single mechanical pass over `Interfaces/Async/` and the explicit implementations on each
base, and it must happen before the async members and the generated sync members drift.

---

## Phase 7 — test parity

`testsnew/Brinell.Html.Tests` contains one file: `GlobalUsings.cs`. Every phase above has
been verified by UI tests against a live browser, which is slow and cannot cover the
generic plumbing at all.

Port the four `Brinell.Maui.Tests` shapes against a mock `IHtmlElement` / `IHtmlScope`:

| Maui test | HTML equivalent |
|---|---|
| `FluentChainingTests` | every generated member returns the containing scope; a chain stays in scope |
| `PageScopingTests` | a control resolves through its page's scope, not the document root |
| `ContainerCollectionTests` | container scoping is real; collection indexing and keying resolve the right item |
| `Semantic/` | comparison variants (`Contains`, `StartsWith`, `Empty`) behave as named |

`Brinell.Mocking` exists and is referenced by `Brinell.Mocking.Tests`; check whether it
already provides an `IHtmlElement` fake before writing one.

This phase is last in dependency order but **should be started during Phase 2** — the
first generated base is the first thing worth a unit test, and having it makes Phases 3–5
much cheaper to verify.

---

## Risks, honestly

| Risk | Why it is real | Mitigation |
|---|---|---|
| Silent behaviour change from `RunWithElement` → `RunDoWithElement` | most of the HTML surface is unpolled today; adding polling and visibility guards changes timing and failure modes everywhere at once | Phase 3 in six small batches, UITests filtered per batch, baseline recorded first |
| The `public new Click` fix changes dispatch | anything typed as `Control<TScope>` gets different behaviour | it is a bug fix, but call it out; grep for `Control<` in tests before Phase 2 |
| `Focusable` / `Clickable` reorder | members move between types; source-compatible for concrete controls, not for anything typed against the bases | Phase 2 step 4 lists the affected controls; check each |
| One test project as the only signal | `Brinell.Html.Tests` is empty and UITests need a live browser and a running sample app | start Phase 7 during Phase 2, not after Phase 6 |
| net8.0 leg | `TreatWarningsAsErrors` + generated code the MAUI templates never had to satisfy | build all three TFMs every phase, not just net10.0 |

## What this plan deliberately does not do

- **Does not touch `Brinell.Blazor`.** It is a separate driver that happens to target the
  same sample app.
- **Does not port MAUI's busy-signal / readiness probe machinery.** Named in Phase 4 step 3
  as follow-on work.
- **Does not rename controls** (`ButtonControl` → `Button`). That is
  [open-questions.md](open-questions.md) §1 and would be a breaking change to every page
  object in two test projects.
- **Does not add new controls.** HTML has 28 objects against MAUI's ~46; closing that gap
  (`iframe`, `dialog`, `details`, `video`, file input, drag-drop) is a separate plan and
  is much easier once the templates exist.
