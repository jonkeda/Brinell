# MAUI Control Architecture Plan

**Date:** 2026-08-29
**Scope:** `srcnew/Brinell.Maui`, `srcnew/Brinell.Maui.FlaUI`, `srcnew/Brinell.Maui.Appium`,
`srcnew/Brinell.Maui.Extensions`, `samples/Brinell.Samples.Maui.App`,
`testsnew/Brinell.Maui.UITests`, `tools/Brinell.Generator`.

**Not in scope:** Blazor, Html, Wpf, WinForms, Stride, NativeAndroid. They are named only
where a decision must not close the door on them.

---

## 0. What Brinell is for

Everything below serves three product goals. When a design decision is contested, decide it
against these:

- **(a) One interface for writing UI tests.** A test author learns one vocabulary —
  `Click`, `SetText`, `SelectItem`, `WaitX`, `AssertX` — and it holds across every platform
  Brinell targets.
- **(b) Less flaky tests through common handlers.** Waiting, polling, retry, and logging
  live in one place (`ViewBase.RunPoll` and the `Run*` family) rather than in each test.
  A test never sleeps; it waits on observed state.
- **(c) MAUI-specific: write a functional test once, run it unchanged on Windows, Android
  and iOS** — where the app's functionality is genuinely the same on all three.

Goal (c) is the sharpest constraint in this document, and it is what makes the
platform-neutrality rules in section 3 non-negotiable rather than stylistic. Any
platform difference that reaches the test body is a failure of (c). Section 4.5 covers the
one case where a difference is real and must be absorbed *below* the test.

---

## 1. Goal

One MAUI control-object layer that:

- is **generated** — every control object is a `.tpl.cs` template plus a generated
  `.gen.cs` public surface (goal 2);
- runs on **Windows (FlaUI), Android and iOS (Appium)** from the same test code (goals 3, 5, 6);
- is exercised end to end by the **sample app -> page/control objects -> UI tests** chain (goal 4);
- gives each control object responsibility for **exactly one MAUI view** (goal 8);
- has **no buck-passing static helpers** — `ElementClicker` and `ElementSearch` are
  dissolved into the control objects and the element layer (goals 9, 10);
- keeps the shape reusable by **other Brinell platforms** later (goal 12) and open to
  **custom controls** outside this repo (goal 13);
- has a **generator that covers the method shapes real controls need** (goal 14), with
  **interfaces** (goal 15) and a **control-object hierarchy** (goal 16) updated where the
  conversion proves them wrong;
- has an **adapter seam** for the case where one MAUI view genuinely behaves differently
  per OS (goal 17), so that difference never reaches a test body.

Basics first. Full method coverage per control is explicitly deferred (goal 11).

---

## 2. Where we are today

Facts established by reading the tree, not assumptions.

**Generation is half-rolled-out.** Under `srcnew/Brinell.Maui/Controls` there are 30
`.tpl.cs` templates with 30 matching `.gen.cs` files, and **28 hand-written controls with
no template at all**: all of `Container/` (Border, BoxView, ContentView, Frame, Grid,
IsoPaneView, RefreshView, ScrollView, SwipeView), all of `Collection/` (CarouselView,
CollectionView, IndicatorView, ListView, TableView), all of `Shapes/` (7), all of
`Media/` (4), plus `Buttons/Button.cs`, `Graphics/GraphicsView.cs`,
`Dialogs/ContentDialog.cs`.

**The generator contract already exists and works.** `ControlObjectGenerator.CreateDefault()`
registers `IsWaitAssertGenerator`, `SetGenerator`, `ActionGenerator` in that order; a
`*Core` method is picked up only when it is `protected virtual` and takes the element
first. `.claude/skills/convert-control/SKILL.md` documents the contract accurately. This
plan does **not** redesign the generator; it extends it where the remaining 28 controls
need it.

**The two helpers to remove.**

- `Controls/Internal/ElementClicker.cs` — `TryClick` walks
  SelectionItem -> Invoke -> LegacyIAccessible -> `element.Click()`, swallowing failures;
  `TryActivateContainingListItemOrElement` re-finds the containing `ListItem` and clicks
  that instead. Called from `ToggleControlBase.tpl.cs`, `ContentDialog.cs`,
  `TabMenu.tpl.cs`, `CollectionObjectBase.cs`, and five files in `Brinell.Maui.Extensions`.
- `Controls/Internal/ElementSearch.cs` — bounds checks, visible-first search, child
  search by automation id / control type, containing-list-item lookup, and a `WaitUntil`
  spin. Called from `PageObjectBase.cs`, `TabMenu.tpl.cs`, and four files in
  `Brinell.Maui.Extensions`.

Both are `internal static` on `Brinell.Maui`, so an external custom control cannot use
them — which is the clearest sign the behaviour is in the wrong place.

**Platform coverage is uneven at the element layer.** `FlaUIMauiElement` implements
`IMauiElement` plus seven capability interfaces (`IInvokePatternElement`,
`ISelectionItemPatternElement`, `ILegacyIAccessiblePatternElement`, `IRangePatternElement`,
`IExpandCollapsePatternElement`, `INestedTextElement`, `ITogglePatternElement`).
`AppiumMauiElement` implements `IMauiElement` **only**. Every control that reaches for a
capability therefore silently degrades on Android and iOS — and the degradation is hidden
inside `ElementClicker`'s `catch { return false; }`.

**Tests are Windows-only in practice.** `Brinell.Maui.UITests.csproj` targets
`net10.0-windows` and references `Brinell.Maui.FlaUI` directly; the sample app's only
`TargetFrameworks` entry is `net10.0-windows10.0.19041.0`. `MauiFixture` already knows the
Android and iOS app paths and capabilities, and `MauiDriverFactory` already routes
Windows->FlaUI and Android/iOS->Appium by reflection. The wiring exists; the projects are
not built for it.

---

## 3. The split of responsibility

Five layers. Each answers exactly one question. This is the core of goal 7.

| Layer | Question it answers | Knows about |
|---|---|---|
| **Driver** (`FlaUIMauiDriver`, `AppiumMauiDriver`) | How do I reach the app and its tree? | The automation technology |
| **Element** (`FlaUIMauiElement`, `AppiumMauiElement`) | How do I do *this primitive* on *this platform*? | The automation technology |
| **Control object** (`Button<TScope>`, `Entry<TScope>`, ...) | What does *this one MAUI view* mean? | One MAUI view type |
| **Container / collection** (`ContainerObjectBase`, `CollectionObjectBase`) | How are child controls scoped? | Structure, not widgets |
| **Page object** | What is on this screen and how do I get there? | The app |

Three rules fall out of the table, and they are the ones to enforce in review:

1. **A control object never branches on platform.** If behaviour differs between Windows
   and Android, the difference is a capability on the element, not an `if` in the control.
2. **An element never knows what a MAUI view means.** It exposes primitives and
   capabilities. "A CheckBox toggles by Invoke when Toggle is unavailable" is control
   knowledge, not element knowledge.
3. **A control object owns one MAUI view.** Compound behaviour is expressed by overriding
   `FindElement`/`*Core` in that control — never by a shared static helper reaching across
   the tree.

### 3.1 Capability negotiation replaces the helpers

Today `ElementClicker.TryClick` is a platform-strategy chain living in a static class,
reachable by no one outside the assembly. The same logic belongs in **two** places:

- **The fallback ladder itself** -> `ClickableControlBase.ClickCore`, as a `protected virtual`
  method a subclass can override. That is precisely goal 9: the control decides how it is
  clicked.
- **The capability probes** -> already interfaces (`IInvokePatternElement` etc.); they move
  from `Brinell.Maui/Interfaces` to `Brinell.Core/Interfaces` alongside the existing
  `IRangePatternElement`, so WPF/WinForms can reuse them (goal 12) and external control
  authors can implement against them (goal 13).

Concretely, `ClickCore` becomes the ladder:

```csharp
protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
{
    EnsureClickableCore(element);

    if (TryInvokePattern(element)) return;   // protected virtual, overridable per control
    element.Click();                          // element-level primitive, throws on failure
}
```

Two behaviour changes are deliberate and must be called out in the changelog:

- **Failures stop being swallowed.** `TryClick`'s `catch { return false; }` turns a broken
  click into a later, unrelated assertion failure. The ladder catches only the
  *capability-unsupported* case and lets real failures throw.
- **`TryActivateContainingListItemOrElement` does not survive as a general helper.** Its
  one legitimate user is row activation inside a collection, so it becomes
  `CollectionObjectBase.ActivateItemCore(IMauiElement itemRoot)` — `protected virtual`,
  overridable by a concrete collection whose rows activate differently.

### 3.2 `ElementSearch` splits three ways

| Current member | Goes to | Why |
|---|---|---|
| `HasUsableBounds`, `ContainsCenter` | `IMauiElement` extension methods in `Brinell.Core` (public) | Pure geometry over the element contract; every platform wants it |
| `FirstVisible`, `FindVisibleByAutomationId`, `FindVisibleByName` | `IMauiElementScope` extension methods (public) | Scope-level search; belongs to the scope contract |
| `FindChildByAutomationId`, `FindChildByControlType` | `ViewBase.FindChildCore(...)`, `protected virtual` | Compound-control knowledge — how *this* control finds its inner part |
| `FindContainingListItems` | `CollectionObjectBase` (private) | Only collections have rows |
| `WaitUntil` | Delete | `ViewBase.RunPoll` already does this, with logging |

`RoundButton` and `IconCommandButton` in `Brinell.Maui.Extensions` are the proof case: both
call `ElementSearch.FindChildByAutomationId` to reach a native inner button. After the
split each overrides `FindChildCore` — which is exactly how an out-of-repo custom control
will do it (goal 13).

### 3.3 What "generated" means for compound controls

Goal 8 and goal 2 pull in opposite directions if the generator is asked to understand
compound structure. It is not. The rule:

> The template holds the *how* (`*Core` methods, including overrides of `FindElement` and
> `FindChildCore`); the generator emits the *what* (public `Click` / `GetText` / `WaitX` /
> `AssertX`). Compound behaviour lives entirely in overridden `*Core` methods, so a
> compound control is generated exactly like a simple one.

No generator change is needed for compound controls. Generator work in this plan is
limited to the gaps the remaining 28 controls actually hit (section 5, phase 3).

---

## 4. Cross-platform strategy

### 4.1 Capability, not platform

```
Control object  --asks-->  "does this element support Toggle?"
                            |
        +-------------------+-------------------+
   FlaUIMauiElement                      AppiumMauiElement
   Supports... = true (UIA pattern)      Supports... = false -> control falls back
```

A control asks a capability question; it never asks "am I on Android". This keeps
`MauiPlatform` out of `Controls/` — where a grep today already finds **zero** platform
branches, a property worth preserving.

### 4.2 The Appium element gap

`AppiumMauiElement` implements no capability interface. Rather than implement all seven,
implement the two that the basics need and let the rest report unsupported:

- `ITogglePatternElement` — Android exposes `checked` on checkbox/switch; iOS exposes
  `value`. Needed by `CheckBox`, `Switch`, `RadioButton`.
- `ISelectionItemPatternElement` — needed by `Picker` and collection rows.

`IInvokePatternElement`, `ILegacyIAccessiblePatternElement` and `INestedTextElement` are
UIA-shaped; on Appium they report `Supports... == false` and the control's ladder falls
through to `element.Click()` / `element.Text`, which is the correct mobile behaviour.

### 4.3 Test projects per platform

`Brinell.Maui.UITests` targets `net10.0-windows` and hard-references the FlaUI driver, so
it cannot host mobile runs. Split responsibility rather than multi-target one project:

```
Brinell.Maui.UITests.Shared        net10.0          page objects, containers, test bodies
   |- Brinell.Maui.UITests         net10.0-windows  + Brinell.Maui.FlaUI    [Trait Platform=Windows]
   +- Brinell.Maui.UITests.Mobile  net10.0          + Brinell.Maui.Appium   [Trait Platform=Android/iOS]
```

Test bodies live once, in Shared. The platform projects supply only a fixture and a driver
reference. `MauiFixture` already reads `APPIUM_PLATFORM` and already computes Android/iOS
app paths, so the fixture split is small.

Tests that genuinely cannot run on a platform get `[Trait("Platform", ...)]` rather than a
runtime skip, so a mobile run's pass count is honest.

### 4.4 Sample app

`samples/Brinell.Samples.Maui.App` must add `net10.0-android` and `net10.0-ios` to
`TargetFrameworks`. `Platforms/Android/` already exists; `Platforms/iOS/` does not and must
be added. Windows-only automation mappers under `Platforms/Windows/Handlers/` stay
Windows-conditional. **The XAML must not change** — if a page needs an `AutomationId` to be
reachable on Android, that is a fix to the shared XAML, benefiting all platforms.

### 4.5 The adapter seam (goal 17)

Capability negotiation (4.1) handles *"this platform lacks a pattern"*. It does not handle
*"this view is genuinely a different shape on this OS"* — a MAUI `Picker` opening an inline
dropdown on Windows but a modal wheel on iOS, a `DatePicker` rendering three spinners on
Android and one field on Windows. Section 3 forbids an `if` on platform inside a control,
so that difference needs somewhere legitimate to live. That is the adapter.

**Three tiers, cheapest first. Reach for the next only when the previous cannot express it.**

| Tier | Mechanism | Use when | Cost |
|---|---|---|---|
| 1 | **Capability probe** (4.1) — `Supports... == false` and the ladder falls through | The *primitive* is missing but the shape is the same | Free; already exists |
| 2 | **Element-level normalization** — the platform element hides the difference behind an existing contract member | The difference is in *how a value is read or written*, not in the interaction sequence | Small; no new type |
| 3 | **Control behavior adapter** — a per-platform strategy object the control resolves once | The *interaction sequence itself* differs (extra open/confirm step, different subtree) | A new type per divergent control |

**Tier 3, concretely.** The control keeps owning its one MAUI view (goal 8); the adapter
owns only the divergent sequence:

```csharp
public interface IControlBehavior<TControl>
{
    bool AppliesTo(MauiPlatform platform);
}

// In the template — still one control object, still generated.
public partial class Picker<TScope> : Base.SelectorControlBase<TScope>
{
    protected virtual IPickerBehavior Behavior => Behaviors.Resolve<IPickerBehavior>(Context);

    protected override void SelectByTextCore(IMauiElement element, string? text, int? timeoutMs = null)
        => Behavior.SelectByText(this, element, text, timeoutMs);
}
```

Four rules keep this from becoming a platform-branch dumping ground:

1. **The adapter is resolved, never queried.** A control asks the registry for its behavior
   once; it never writes `if (Platform == ...)`. The single `switch` on platform lives in
   the registry, in `Brinell.Maui`, and nowhere else.
2. **A control has no adapter until a divergence is proven by a failing test.** The default
   is no adapter and one shared implementation. Adding one speculatively costs goal (a).
3. **The adapter is below the generated surface.** It is called from `*Core` methods, so
   `.gen.cs` and the public API are identical on every platform — which is exactly what
   goal (c) requires.
4. **Adapters are registrable from outside `Brinell.Maui`**, so a custom control (goal 13)
   can supply its own per-platform behavior without patching this repo.

**Why not just multi-target the control?** `#if ANDROID` in a control object would compile
the test-facing type differently per platform, breaking goal (a) — the same test code would
have a different API surface depending on build. The adapter keeps one type, one API, and
varies only the sequence behind it.

**Where this lands.** Tiers 1 and 2 are phase 2. Tier 3 is phase 6, and deliberately not
earlier: building an adapter framework before Android has actually run would be guessing at
which controls diverge. Phase 4's mobile smoke run produces that list.

---

## 4.6 What holds goal (a) in place: the Core interfaces

Sections 3–4.5 describe how a control is built. None of it, on its own, stops two controls
from growing different names for the same idea — which is how goal (a) is lost in practice,
one reasonable-looking method at a time.

The constraint already exists and is underused. `Brinell.Core/Interfaces` holds 20+
control-object contracts — `IClickableControlObject`, `ISelectorControlObject`,
`ITextControlObject`, `IToggleControlObject`, `IRangeControlObject`, and so on — and
`Brinell.Wpf`, `Brinell.WinForms`, `Brinell.Stride` and `Brinell.NativeAndroid` already
implement the same ones. That set *is* the common vocabulary of goal (a), across platforms,
today.

So the rule for every new behavior method, and the thing to check in review:

> A public member on a control object either implements a `Brinell.Core` interface, or it
> is a deliberate MAUI-specific extension. There is no third category. If a method is
> broadly useful, it belongs in the interface, so WPF and WinForms are held to the same
> name.

This makes the generator and the interfaces one system rather than two: the interface
declares the vocabulary, the `*Core` method supplies the behavior, the generator emits the
member that satisfies the contract. It is also the honest test for goal 16 — if a converted
control cannot satisfy its interface without hand-written members, either the hierarchy or
the generator is wrong, and §3.1 says which.

Practical consequence for phase 3: convert a control, then diff its generated public surface
against the interface it claims to implement. A missing member is a generator gap; an extra
one is either a vocabulary addition worth pushing into `Brinell.Core`, or scope creep.

---

## 4.7 Where flakiness is actually caught (goal b)

Goal (b) — fewer flaky tests through common handlers — is stated as an outcome but has no
mechanism in the phases above. It needs one, because the phases *remove* two things that
were papering over flakiness.

**What phase 1 takes away.** `ElementClicker.TryClick` returns `false` on failure and
`ElementSearch.WaitUntil` spins silently. Both convert a real failure into a later,
unrelated assertion failure — the classic profile of a flaky test. Removing them is correct
and will make some currently-green tests fail. Each such failure is a bug that was already
there; treat it as a finding, not a regression (this is already in §6, and it is the most
likely source of surprise in the whole plan).

**What replaces them.** `ViewBase.RunPoll` is the single wait/retry/log handler and already
does the job properly: it polls on observed state, logs entry and exit with timing, and
rethrows the last exception on timeout instead of swallowing it. The rule that follows —
and it is the whole of goal (b) — is that **no control object waits any other way**. No
`SpinUntil`, no `Task.Delay`, no bespoke retry loop. `AGENTS.md` already forbids arbitrary
sleeps in tests; this extends the same rule to the control layer, where it is enforceable
by grep.

**The cheap tier that catches this.** `Brinell.Maui.Tests` already mocks `IMauiElement` with
Moq, and `SemanticControlTestsBase.CreateInvokableElement` already casts a mock to
`IInvokePatternElement` to simulate a capability being present. That is exactly the harness
the capability ladders need — a supported and an unsupported variant of each — and it runs
in seconds with no device.

This matters for sequencing: **the ladders in phases 1 and 2 should get their unit tests in
the same phase, not deferred to the mobile run.** A capability negotiation bug found on an
Android emulator costs an order of magnitude more to diagnose than the same bug found by a
mocked element. Phase 2 already says this; phase 1 should too.

**A cross-platform-specific flake source.** Timeouts that are adequate on a Windows desktop
are often not on an emulator. Resist the reflex to raise the default globally — that slows
every failing test on every platform. Timeouts belong in `TimeoutSettings` per platform,
which `MauiTestContext` already carries, so a test body never names a duration and goal (c)
survives.

---

## 5. Phases

Each phase ends green. Per the repo's testing guidance, run the narrowest UI tier that can
falsify the change; reserve the full suite for phase completion. Note the suite has
**pre-existing failures unrelated to this work** (DatePicker, TimePicker, Image,
ProgressBar, Stepper, Switch) — establish the baseline before calling anything a regression.

### Phase 1 — Dissolve the helpers (goals 9, 10)

No new features; behaviour-preserving except where section 3.1 says otherwise.

1. Move the capability interfaces from `Brinell.Maui/Interfaces` to
   `Brinell.Core/Interfaces`, next to `IRangePatternElement`. Keep the names.
2. Add public extension methods for the geometry/search primitives per the 3.2 table.
3. Fold `ElementClicker.TryClick` into `ClickableControlBase.ClickCore` +
   `protected virtual bool TryInvokePattern(IMauiElement)`.
4. Move `TryActivateContainingListItemOrElement` into
   `CollectionObjectBase.ActivateItemCore`.
5. Rewrite the ~10 call sites: `ToggleControlBase.tpl.cs`, `TabMenu.tpl.cs`,
   `ContentDialog.cs`, `PageObjectBase.cs`, `CollectionObjectBase.cs`, and the five
   `Brinell.Maui.Extensions` controls (`GenericBrowser`, `SelectionList`, `EditableField`,
   `RoundButton`, `IconCommandButton`).
6. Delete `Controls/Internal/ElementClicker.cs` and `Controls/Internal/ElementSearch.cs`.
7. Unit-test the new `ClickCore` ladder in `Brinell.Maui.Tests` — supported and unsupported
   variant of each capability — using the existing
   `SemanticControlTestsBase.CreateInvokableElement` pattern (§4.7). Same phase, not
   deferred: this is the tier that makes the phase-1 behaviour change safe to land.
8. Housekeeping: rename the stale `Brinell.Maui.UITests.Pages2` namespace to
   `...UITests.Pages` in `Pages/AppShellPage.cs` and `MauiFixture.cs` (see §7.1).

**Done when:** neither file exists, `grep -r "ElementClicker\|ElementSearch" srcnew` is
empty, the ladder has mocked coverage of both branches, and the UI suite matches the
pre-change baseline.

**Deliberately deferred:** `ExpandHelper` and `GestureHelper` are the same smell but have
fewer users (`Expander`, `SwipeView`, `RefreshView`, `CarouselView`). Phase 5.

### Phase 2 — Close the Appium capability gap (goals 3, 6) ✅

1. ✅ `AppiumMauiElement` implements `ITogglePatternElement` and
   `ISelectionItemPatternElement`. Toggle state reads Android's `checked` and iOS's `value`;
   support is decided by whether the attribute is actually present, not by control type,
   since that is the only signal that works across drivers.
2. ✅ Unsupported capabilities stay **unimplemented** rather than implemented-and-returning-false.
   Not implementing the interface is the report: the control's `is` test misses and it falls
   through. The four UIA-shaped capabilities are deliberately absent on mobile.
3. ✅ `CapabilityNegotiationTests` (6 tests) covers both branches of the toggle ladder against
   a mocked `IMauiElement` — capability present, capability absent, and capability advertised
   but declining.

**Neither platform exposes a toggle *command*, only toggle state**, so `TogglePattern()` taps
and then verifies the state moved. Reporting success without that check is the failure mode
that made `LegacyIAccessible` unusable in the Windows click ladder (phase 1) — a pattern that
claims success while the control does not move.

#### Found and fixed while here

- **`SetToggleStatePattern` was dead code.** Both drivers implemented it; nothing called it.
  `SetCheckedCore` went through `ToggleCore` instead, so setting a state depended on the
  state read beforehand still holding when the toggle landed. It now prefers the platform's
  set-state command — idempotent — and falls back to toggling where none exists, which is
  every mobile platform. A pre-existing unit test asserted this behaviour and had been
  failing; it now passes.
- **Two `CheckBoxControlTests` stubbed only `TryFindElement`**, leaving `FindElement` null so
  the control raised `NullReferenceException`. A test-setup defect, not a product one.

**Done:** unit suite 77 passed / 6 failed (from 62/8 at the session baseline — the 2 recovered
here plus 13 new tests). UI suite excluding phase-7 parked tests: 137 passed / 1 failed.

### Phase 3 — Generate the remaining 28 controls, extending the generator as needed (goals 2, 14, 15, 16)

Order by risk, simplest first, following `.claude/skills/convert-control/SKILL.md`:

| Batch | Controls | Expected generator gaps |
|---|---|---|
| 3a | Shapes (7), `BoxView`, `Border`, `Frame`, `ContentView` | none — read-only surface |
| 3b | `Button`, `GraphicsView`, `IndicatorView` | none |
| 3c | `ScrollView`, `Grid`, `IsoPaneView`, `RefreshView`, `SwipeView` | gesture `*Core` shape |
| 3d | `ListView`, `TableView`, `CarouselView`, `CollectionView` | generic self-referencing type params |
| 3e | `Media/` (4), `ContentDialog` | out-of-tree element (popup HWND) |

Fix generator limits **only when a batch actually hits one**, and record each in
`.my/Generator/generator-gaps-plan.md` rather than here. Batch 3d is the known hard case:
`CollectionView<TParent, TSelf, TItem>` is self-referencing, and the generator has not yet
emitted into a three-parameter generic partial — verify with one control before committing
to the batch.

#### 3.1 Extending the generator, and the methods with it (goal 14)

Today three generators cover three shapes: `Is*Core`/`Get*Core` (trio), `Set*Core`
(setter), and everything else (action). **Both sides are in scope here** — where a method
signature has to change to become generatable, change it. Signatures are not fixed points;
this matters most as behavior methods are added later, where an ad-hoc shape per control
would defeat goal (a).

**What the four non-virtual `*Core` methods in the tree actually tell us.** A sweep finds
exactly four, and they are not one problem but three:

| Method | Why it is non-virtual | Verdict |
|---|---|---|
| `SelectorControlBase.GetItemTextsCore` | Documented: a generated `Wait`/`Assert` pair would compare `IReadOnlyList<string>` with `==` — reference equality, unsatisfiable | **Real generator gap.** Fix the generator, then make it virtual |
| `SelectorControlBase.GetItemElementsCore` | Documented: a generated wrapper would leak `IMauiElement` into the public API | **Correct as-is.** Needs a way to say so |
| `ViewBase.WaitVisibleCore` | Not a generation candidate — it is a `Wait*` helper, not a `Get*`/`Is*` source | Correct; no change |
| `TabViewControl.IsSelectedCore` (Toolkit) | No stated reason | **Accidental.** It hand-writes `IsSelected`/`WaitSelected`/`AssertSelected` — the exact trio the generator emits |

So my earlier framing of this as silent API loss was wrong: the two `SelectorControlBase`
methods are deliberate and documented, and `GetItemTexts` is hand-written to compensate.
But the *mechanism* is still wrong in all three failing cases, and the fix differs per row.

**Fix 1 — collection-valued getters (`GetItemTextsCore`).** The blocker is comparison
semantics, not the collection itself. `Comparison` already has `Equals | Contains |
StartsWith | EndsWith | Empty`; a sequence-aware set makes collection getters generatable:

| New comparison | Generates | Meaning |
|---|---|---|
| `SequenceEquals` | `AssertItemTexts(IReadOnlyList<string>)` | Element-wise equality, order significant |
| `HasItem` | `AssertItemTextsContains("Blue")` | Membership, not substring |
| `Count` | `AssertItemTextsCount(3)` | Cardinality without materializing |

With those, `GetItemTextsCore` becomes `protected virtual` with
`[GenerateComparisons(Comparison.SequenceEquals | Comparison.HasItem | Comparison.Count)]`,
the hand-written `GetItemTexts` is deleted, and every selector gains the assertions for
free. Note `Contains` means substring for strings and membership for sequences — hence the
separate `HasItem` name rather than overloading it.

**Fix 2 — deliberate non-generation (`GetItemElementsCore`).** Intent should be declared,
not implied by a missing keyword. Add `[SkipGeneration("reason")]`; the method stays
`protected virtual` (so derived controls can still override it — which today they cannot)
and the generator skips it with the reason preserved in the source. This separates
"deliberately hidden" from "accidentally missed", which a bare `protected` cannot do.

**Fix 3 — the accident (`TabViewControl.IsSelectedCore`).** Make it `protected virtual`,
delete the three hand-written members, let the generator emit them. This is phase 5's
Toolkit conversion, and it is a good sign the Toolkit was chosen as the first external
consumer — it surfaced this.

**The general fix: make a near-miss loud.** Once intent is declarable via
`[SkipGeneration]`, silence is no longer an acceptable default. A method ending in `Core`
that takes an element first but fails the modifier check should fail generation with a
message naming the method and telling the author to add `virtual` or `[SkipGeneration]`.
Do this **after** fixes 1–3, so the build does not break on the cases just resolved. This
is the change that scales: every future behavior method either generates or says why not.

**Remaining shapes, added only when a batch demands one:**

| Shape | Example | Status |
|---|---|---|
| Action with a return value | a `*Core` returning `bool` that is not `Is*` | No generator; `ActionGenerator` assumes `void` |
| Indexed / parameterized getter | `GetItemAtCore(element, int index)` | Extra params are copied, but collision rules are untested |
| Async `*Core` | `Task`-returning | **No instance exists in the codebase** — do not build for it |

Resist building the full matrix up front: goal 11 says basics first, and an unused
generator is untested code.

#### 3.2 Interfaces and hierarchy (goals 15, 16)

Conversion is the forcing function for both. Change them when a batch proves the need — not
in a speculative redesign pass:

- **Interfaces.** Phase 1 already relocates the capability interfaces to `Brinell.Core`.
  Beyond that, expect pressure from batch 3d: `IMauiContainer`, `IMauiContainerObject`, and
  `IMauiScope` overlap, and collections may need a contract the current set does not name.
  Split or rename only with a converted control demonstrating the need.
- **Hierarchy.** The current chain is `ViewBase` -> `FocusableControlBase` ->
  `ClickableControlBase`, with `RangeControlBase`, `SelectorControlBase`, `ToggleControlBase`
  branching off. Batches 3a and 3e will test whether it fits: Shapes and Media are neither
  focusable nor clickable in any meaningful sense, so if `ViewBase` proves too thin or
  `FocusableControlBase` too eagerly inherited, that is the evidence for a change.
  A likely outcome is a sibling base for non-interactive display surfaces — decide it with
  batch 3a in hand, not now.

**Done when:** the generator handles every shape the converted controls need; every `*Core`
method in the MAUI tree is either `protected virtual` or carries `[SkipGeneration]`; a
near-miss fails generation with a named error; and `GetItemTexts` is generated rather than
hand-written.

The 8 controls whose conversion depends on phase 5 dissolving the helpers moved to
**phase 5b**; "nothing outside `Internal/`" is that phase's exit criterion, not this one.

#### Status: generator work complete, 20 of 28 controls converted

**Generator (§3.1) — done.** All three fixes plus the loud near-miss error, covered by 11 new
tests (`SilentSkipAndCollectionTests`); generator suite 115 passing.

The validation earned its place immediately: enabling it failed generation on **three**
near-misses, including `ViewBase.WaitVisibleCore`, which the §3.1 analysis had classified as
"not a candidate". The analysis was wrong and the tool was right — precisely the outcome that
justifies a machine check over a manual sweep.

| Fix | Outcome |
|---|---|
| `[SkipGeneration("reason")]` | `GetItemElementsCore` and `WaitVisibleCore` now declare intent and stay `virtual` — the latter was previously un-overridable |
| `SequenceEquals` / `HasItem` / `Count` | `GetItemTextsCore` generates; the hand-written `GetItemTexts` is deleted, and every selector gains `AssertItemTexts`, `AssertItemTextsHasItem`, `AssertItemTextsCount` |
| Loud near-miss | A `*Core` that misses the contract fails generation naming the method and the missing modifier |

**Converted (20):** all 7 Shapes; `BoxView`, `Border`, `Frame`, `ContentView`, `Grid`,
`IsoPaneView`, `SwipeView`; `Button`, `GraphicsView`, `IndicatorView`, `ListView`;
`HybridWebView`, `BlazorWebView`.

`SwipeView` is the one that carried real behaviour: its five gesture members became `*Core`
methods, which also begins dissolving `GestureHelper` ahead of phase 5. Generated API is
identical to the hand-written one, parameterized `Swipe` included.

**Remaining 8 → moved to phase 5b.** Each needs Core extraction rather than a rename, and
three of them touch code phase 5 is about to change. See that phase for the breakdown.

**Verified:** solution build unchanged (same 2 pre-existing `PresenterPage` errors);
`Brinell.Maui.Tests` 77 passed / 6 failed (unchanged); `Brinell.Generator.Tests` 115 passed;
UI suite excluding phase-7 parked tests 137 passed / 1 failed (unchanged).

### Phase 4 — Sample app and tests on three platforms (goals 3, 4)

1. Add `net10.0-android` and `net10.0-ios` to the sample app; add `Platforms/iOS/`.
2. Split `Brinell.Maui.UITests` into Shared + Windows + Mobile per 4.3.
3. Bring up a **basics-only** mobile smoke set — Button, Label, Entry, CheckBox, Switch —
   not the full suite (goal 11).
4. Wire Android into CI using the existing `run-android-tests.ps1` and `start-appium.ps1`.

**iOS is deferred — here is how it gets done when the time comes.** There is no macOS
machine available, and iOS cannot be built or run without one: the `net10.0-ios` target
needs Xcode, and Appium's XCUITest driver needs a real macOS host for both the simulator
and `WebDriverAgent`. Three routes, cheapest first:

| Route | What it costs | What it gets |
|---|---|---|
| **Hosted macOS CI runner** (GitHub Actions `macos-latest`) | Free-tier minutes on public repos; billed at a multiplier on private | Full build + simulator run; no hardware. **The default choice** |
| **Cloud device farm** (BrowserStack / Sauce Labs / LambdaTest) | Per-minute subscription | Real devices, no infrastructure; a build still has to come from somewhere |
| **A Mac on the desk or a Mac mini** | Hardware | Local iterate-and-debug, which the other two do badly |

The important part is that **the code lands now and only the run waits**. Everything in
phases 1–3 is platform-neutral by construction, and the phase 4 project split puts test
bodies in `.Shared`, so enabling iOS later is: add `net10.0-ios` to the sample app, add
`Platforms/iOS/`, add an iOS fixture, point CI at a macOS runner. If the Appium capability
work (phase 2) has an iOS branch that no one has executed, mark it explicitly —
`// iOS: unverified` — so a later reader knows it is a design intent, not a tested path.

Until then, state the status plainly: **iOS is architecturally supported and unverified.**
Do not report a green iOS run that never happened.

**Done when:** the Windows suite still matches baseline, and the mobile smoke set passes
on an Android emulator. iOS: projects and traits present, run deferred.

#### Status: builds for all three platforms; mobile run blocked on hardware

**The sample app now builds for Windows, Android and iOS.** iOS was expected to be
unbuildable here; it is not — Windows cross-compiles the `net10.0-ios` head fine. Only
*running* it needs a Mac, so the routes in the table above still apply to execution, not to
compilation.

| Target | Build |
|---|---|
| `net10.0-windows10.0.19041.0` | ✅ 0 errors |
| `net10.0-android` | ✅ 0 errors |
| `net10.0-ios` | ✅ 0 errors |

**`Platforms/iOS/`** added (`AppDelegate`, `Program`). `Brinell.Maui.AppSupport` is now
multi-targeted, with its WinUI automation peers excluded by file on non-Windows rather than
wrapped in `#if` — those files are meant to be copied into an app under test, and a reader
should not have to strip preprocessor directives to follow them.

**Two shared-code fixes were needed**, both in `GlobalUsings.cs` and neither platform-specific
in effect:

- `Microsoft.UI.Xaml.Automation.Peers` is now `#if WINDOWS`.
- `Microsoft.Maui.Platform` is no longer imported globally at all. On iOS it also defines
  `ContentView`, which collided with `Microsoft.Maui.Controls.ContentView` across every view
  in the app. Nothing in the app used it — the Windows handlers had already moved to
  AppSupport — so removing it is a simplification, not a workaround.

**The test split turned out simpler than planned.** §4.3 proposed a three-project split with a
`.Shared` project. That was unnecessary: **no test source is coupled to Windows at all** — the
coupling was entirely the csproj (`net10.0-windows` TFM plus the FlaUI reference), and
`MauiTestFixtureBase` already selects the platform at runtime from `APPIUM_PLATFORM`.

So `Brinell.Maui.UITests.Mobile` **links** the existing test sources rather than moving them:

```
testsnew/Brinell.Maui.UITests          net10.0-windows  → Brinell.Maui.FlaUI
testsnew/Brinell.Maui.UITests.Mobile   net10.0          → Brinell.Maui.Appium
                                                          (Compile Include=..\UITests\**\*.cs)
```

One set of page objects, containers and test bodies; two heads differing only by driver. The
Windows project's layout is untouched, so the existing suite and its tooling are unaffected.
Both projects are in `Brinell.sln` and `srcnew/Brinell.sln`.

**The smoke set needed no new code.** Tests already carry a `Control` trait, so the basics set
is a filter:

```
--filter "Control=Button|Control=Label|Control=Entry|Control=CheckBox|Control=Switch"
```

27 tests; **26 pass on Windows**, the 1 failure being the phase-7 `Switch_ClickTwice_TogglesOff`.

`run-android-tests.ps1` was repointed at the mobile head — it referenced the Windows project,
which could never have hosted an Android run.

#### The Android run: executed, and it found five real defects

An emulator (`Medium_Phone`, API 36) plus Appium 3.1.2 with `uiautomator2` was brought up and
the smoke set run against it. **No test passes on Android yet**, but the run did its job: it
converted "architecturally supported and unverified" into a specific, ordered list of
blockers, four of which are now fixed.

| # | Defect | Status |
|---|---|---|
| 1 | `APPIUM_PLATFORM` was **read by nothing** — only named in an error message. Every run selected Windows and demanded FlaUI | ✅ fixed in `BrinellMauiConfiguration.Load` |
| 2 | `Assembly.Load("Appium.WebDriver")` used the **package** name; the assembly is `Appium.Net` | ✅ fixed in `MauiDriverFactory` |
| 3 | Android Debug builds default to **Fast Deployment**, leaving assemblies out of the APK; the app aborted at startup | ✅ `EmbedAssembliesIntoApk=true` |
| 4 | `appWaitActivity = "*"` never matched MAUI's hashed activity name | ✅ replaced with `appWaitPackage` |
| 5 | `ShellContent` uses the `ControlTypeAndName` locator, which the Appium driver rejects outright | ⏳ **phase 6** |

Defects 1 and 2 are the important ones: **together they made any Appium run impossible**, and
neither could surface on Windows. They are why mobile had never worked, and they were
invisible to every existing test.

Two smaller shared-XAML/page-object fixes came out of it, both of the kind §4.4 prescribes:
`AppShell` gained an `AutomationId`, and `AppShellPage` now overrides `RequiresLoadedPage` —
the first real instance of the escape hatch [RCA-002](rca/rca-002-page-precondition-discarded-slow-failures.md)
added, since Shell renders as native Android chrome and has no addressable root there.

**Defect 5 is the phase 6 evidence this plan has been waiting for.** §4.5 said the list of
genuinely divergent controls could not be known until Android ran. It now has its first two
entries, both concerning Shell navigation:

- **`ShellContent` locator strategy.** `ControlTypeAndName` is a UIA concept. On Android a tab
  is a `content-desc`. Same control, different addressing — a tier-3 adapter, not a fix.
- **Tab overflow.** Android's `BottomNavigationView` shows 5 tabs plus **More**; the rest are
  behind that menu. `DisplayTab` is not merely addressed differently, it is *not on screen*.
  Windows shows all 10. A test that clicks a tab must mean "reach that tab" on both.

Both belong below the test body, exactly as §4.5 requires. Neither was guessed — the UI
hierarchy dump is the evidence.

**A sub-plan proposes removing the cause rather than adapting to it:**
[sample-app-navigation-redesign.md](sample-app-navigation-redesign.md). ⏸ **Parked
mid-implementation, uncommitted, Windows currently regressed 117/21 vs 137/1** — see that
document's §3.5 for exactly what is built and the defects it exposed.

**To resume, follow [plan-sample-app-recovery-and-phase4.md](plan-sample-app-recovery-and-phase4.md).**
It keeps the hub design, reverts to the green baseline, and reapplies it behind a build flag
with a manual app-launch gate before every test run — the discipline whose absence, not the
design, is what broke the first attempt. It also carries phase 4 to completion on Android. The sample app exists
to exercise control objects, not to demonstrate Shell; replacing Shell with a flat hub page
makes "open a page" one uniform action on all three platforms and also removes the navigation
stack that caused RCA-001. Shell control objects stay supported and gain a dedicated test
page — Shell becomes a *subject* rather than the mechanism every other test depends on.

That sub-plan and the tier-3 adapter are not alternatives: the adapter remains the right
answer for user apps that navigate by Shell. It changes where the evidence for it comes from.

**Status: Android is now verified as *reaching the app*** — Appium connects, installs,
launches, and drives the real UI (runs take ~30 s, not milliseconds). It is **not** verified
as passing tests, and must not be reported as such. iOS remains build-only.

The phase 2 `AppiumMauiElement` capability code still has not executed: the runs get as far as
Shell navigation and stop there, before any control-level capability is exercised.

### Phase 5 — Extension points, proven by an external consumer (goals 12, 13) ✅

1. ✅ **Dissolve `ExpandHelper` and `GestureHelper`.** Done — `Controls/Internal/` is now
   **empty and removed**, completing what phase 1 started.

   | Helper | Where it went | Why there |
   |---|---|---|
   | `ExpandHelper` | `Expander.tpl.cs` `*Core` methods | Every member was Expander-specific; it had exactly one consumer |
   | `GestureHelper` swipes | `MauiElementGestureExtensions` (public) | Arithmetic over `Rect` plus `Swipe` — element knowledge, and now reachable by an out-of-repo control |
   | `GestureHelper.IsRefreshing` | `RefreshView.IsRefreshingCore` | Not a gesture at all: refreshing is what that one control *means* |

   `Expander` and `RefreshView` were converted to `.tpl.cs` in the process, so both are now
   generated — and both generated APIs match their hand-written originals member for member.
   That takes phase 5b's remaining count from 8 to 7.

   **One thing did not move as planned.** The swipe extensions belong in `Brinell.Core` by the
   same logic that put the geometry helpers there — but they must swallow
   `WindowsInteractionPolicyException`, which is defined in `Brinell.Maui` and is `sealed`, so
   Core cannot catch it by type. The alternatives were catching `InvalidOperationException`
   broadly (which would hide real faults) or lifting the exception into Core speculatively.
   They stay in `Brinell.Maui`, public, with the constraint recorded in the file: when a
   second platform needs swipes, that is the moment to lift the exception and these with it.
2. **Treat `Brinell.Maui.CommunityToolkit` as the first external consumer.** It is a
   separate assembly with a single control (`TabViewControl`), which makes it the ideal
   proof: convert it to `.tpl.cs` by running the CLI against *its own* folder, using only
   the public surface of `Brinell.Maui`. Anything it cannot reach is a real gap in goal 13,
   found by compiler error rather than by review. `Brinell.Maui.Extensions` is the second
   consumer and a larger one (7 controls), so run the Toolkit first.
   It has already earned its place: `TabViewControl.IsSelectedCore` is `protected` without
   `virtual` and hand-writes the `IsSelected`/`WaitSelected`/`AssertSelected` trio the
   generator would emit (§3.1, fix 3). Converting it is mostly deleting that.
3. Document the custom-control path from what step 2 actually required: derive from
   `ViewBase`/`ClickableControlBase`, declare `*Core` methods, run
   `Brinell.Generator.Cli --input <folder>` — which already accepts any path, so no tooling
   change is needed for out-of-repo controls.
4. Confirm nothing a custom control needs is `internal` to `Brinell.Maui`. Step 2 is what
   makes this claim credible rather than aspirational; it is also why phase 1 makes the
   extension methods public.
5. Record what generalizes to WPF/WinForms (capability interfaces, the Core/generated split,
   the five-layer table) versus what is MAUI-specific (the ladder contents).

**Done when:** the Toolkit control is generated using only public API, and every `internal`
that blocked it has been made public or deliberately kept with a recorded reason.

#### ✅ Done — goal 13 proven, not asserted

`Brinell.Maui.CommunityToolkit` is a **separate assembly**. `TabViewControl` was converted to
`.tpl.cs`, the CLI was run against its own folder exactly as an out-of-repo consumer would
(`--input srcnew/Brinell.Maui.CommunityToolkit/Controls`), and the project **compiled with no
changes to `Brinell.Maui`**.

**Nothing was blocked by an `internal`.** That claim is now backed by a compiler rather than a
review: had anything the control needed been assembly-private, the build would have failed.
Phase 1's decision to make the geometry and search helpers public is what paid off here.

The conversion was mostly deletion, as §3.1 fix 3 predicted: `IsSelectedCore` was `protected`
without `virtual` and the class hand-wrote the `IsSelected`/`WaitSelected`/`AssertSelected`
trio the generator emits. Making it `protected virtual` and deleting the three members
produced an identical public API.

**The custom-control path, as actually exercised** (step 3), is three steps and no tooling
change:

1. Derive from `ViewBase` / `ClickableControlBase` (or a capability base).
2. Declare `protected virtual *Core` methods; add `[SkipGeneration("reason")]` where a member
   deliberately should not be public.
3. Run `Brinell.Generator.Cli --input <your folder>` — it already accepts any path.

**Verified:** solution build unchanged (same 2 pre-existing `PresenterPage` errors);
`Brinell.Maui.Tests` 77 passed / 6 failed, unchanged; UI test project builds clean.

**Step 5 (what generalizes) — deferred with a reason.** The gesture-extension finding above is
the concrete evidence for it: the Core/generated split, the capability interfaces and the
five-layer table generalize, but a helper that must catch a *platform-specific* exception
cannot move to `Brinell.Core` without lifting that exception too. Recording the general rule
is worth doing when a second platform actually adopts this shape, not before — otherwise it is
a guess dressed as documentation.

### Phase 5b — Convert the 8 controls that were waiting on phase 5 (goal 2) ✅

Phase 3 converted 20 of 28. These 8 were held back because each needs `*Core` extraction
rather than a rename, and several touch code phase 5 changes — converting before that would
mean converting twice.

| Control | Why it waited | Order |
|---|---|---|
| `RefreshView` | Coupled to `GestureHelper`, dissolved in phase 5 | 1 |
| `ScrollView` | Hand-written `ScrollForward` / `ScrollBack` / `ScrollTo` ×2, no Core | 1 |
| `TableView` | Hand-written `GetIntent` / `HasIntent`, no Core | 1 |
| `WebView`, `MediaElement` | Partial Core coverage under a large hand-written surface (136 and 213 lines) | 2 |
| `ContentDialog` | Resolves elements through `FindPopupElement`, outside the normal scope; also the `RequiresLoadedPage` case from [RCA-002](rca/rca-002-page-precondition-discarded-slow-failures.md) | 2 |
| `CarouselView` | Self-referencing 3-parameter generic **and** `GestureHelper`-coupled | 3 |
| `CollectionView` | `CollectionView<TParent, TSelf, TItem>` — the known hard case | 3 |

**Order matters.** Groups 1 and 2 are ordinary conversions. Group 3 is the batch-3d risk the
plan has flagged from the start: the generator has never emitted into a self-referencing
three-parameter generic partial. **Prove it on `CarouselView` before starting
`CollectionView`** — if the generator cannot express it, that is a generator gap to record in
`.my/Generator/generator-gaps-plan.md`, not a control to force.

Two things make this phase cheaper than it looks:

- The phase 3 generator work removed the three blockers these would otherwise hit —
  collection-valued getters, declared skips, and silent near-misses now all have answers.
- `SwipeView` is the worked example: five hand-written gesture members became `*Core`
  methods, `GestureHelper` calls moved inside them, and the generated API came out identical
  to the hand-written one.

**Every conversion must diff its generated public surface against the original** (skill
step 9). A member that silently disappears is the failure mode this whole phase exists to
prevent — and it is now a generation error rather than silence, so the check is cheap.

**Done when:** `find srcnew/Brinell.Maui/Controls -name '*.cs' ! -name '*.tpl.cs'
! -name '*.gen.cs'` returns nothing outside `Internal/`, or any control left hand-written has
a recorded reason naming the generator limitation that blocks it.

#### ✅ Done — 6 converted, 1 recorded as deliberately hand-written

`RefreshView` was already done in phase 5 (it came free with dissolving `GestureHelper`), so
7 remained. That query now returns **only `ContentDialog`**.

| Control | Outcome |
|---|---|
| `ScrollView`, `TableView` | Converted. `TableView` gains `WaitIntent`/`AssertIntent` for free |
| `WebView`, `MediaElement` | Converted. Both had **public methods duplicating their own Core verbatim** — `GetUrl`/`GetUrlCore` and `IsPlaying`/`IsPlayingCore` were identical bodies. The conversion deleted the duplication |
| `CarouselView`, `CollectionView` | Converted — see below |
| `ContentDialog` | **Deliberately hand-written**, reason recorded below |

**The batch-3d risk is resolved.** The generator emits correctly into a self-referencing
three-parameter generic partial — `ResolveFluentReturnType` already preferred `TSelf`, which
was the hard part. `CarouselView` proved it before `CollectionView` was touched, as the plan
required.

**But it surfaced two real gaps in `ContainerObjectBase`**, both now fixed:

1. **No absence-tolerant helpers.** `[AbsenceTolerant]` emits calls to
   `RunWaitWithOptionalElement` / `RunAssertWithOptionalElement`, which existed on `ViewBase`
   but not on the container base — so any container with an absence-tolerant getter failed to
   compile. Both added, mirroring `ViewBase`.
2. **No parameterless `TryFindElement()`.** The generator emits one call shape for "this
   object's element"; containers only had `TryFindElement(Locator)`. Added as an alias for
   `TryGetContainerRoot()`, so each base decides what its own element is and the generator
   need not know which base it is generating for.

Fixing these in the base rather than special-casing the generator is what keeps §3's rule
intact: the generator emits one shape, the hierarchy supplies the meaning.

**`ContentDialog` stays hand-written.** Its public surface is two element factories
(`DialogButton`, `PromptInput`) and one orchestration method that walks six private fallbacks
across scoped, popup-window and parent-scope lookups. **Not one member takes an element
first**, so nothing meets the Core contract — converting it would produce an empty `.gen.cs`
and add a file without adding API. This is a control the generator correctly has nothing to
say about, not a generator limitation to fix.

**Verified:** solution build unchanged (same 2 pre-existing `PresenterPage` errors);
`Brinell.Maui.Tests` 77 passed / 6 failed, unchanged; `Brinell.Generator.Tests` 115 passed.

### Phase 6 — Control behavior adapters, driven by real divergence (goal 17)

Only now, with an Android run in hand, is the list of genuinely divergent controls known.

**Phase 4's run supplied the first two entries, both in Shell navigation:**

| Divergence | Windows | Android | Tier |
|---|---|---|---|
| `ShellContent` addressing | `ControlTypeAndName` (a UIA concept) | tab is a `content-desc`; the driver rejects that strategy outright | 3 |
| Tab reachability | all 10 tabs visible | `BottomNavigationView` shows 5 + **More**; the rest need the overflow opened first | 3 |

The second is the more interesting one, and the better argument for the adapter: the tab is
not merely *addressed* differently, it is **not on screen**. A test that says "click the
Display tab" must mean "reach the Display tab" on both platforms — which on Android means
opening an overflow menu first. That sequence is precisely what a tier-3 behavior owns, and
it cannot be expressed as a capability probe or an attribute rename.

1. Take the failures from phase 4's mobile smoke set and sort each into a tier from 4.5.
   Expect most to be tier 1 or 2 — a missing capability or a differently-named attribute —
   and only a few to need tier 3.
2. Build the adapter registry and `IControlBehavior<T>` seam **only if step 1 produces at
   least one tier-3 control.** If it produces none, record that and stop: the capability
   model was sufficient, and an unused framework is a liability.
3. Implement adapters for the tier-3 controls found, no others.
4. Add the rule to review guidance: a platform difference is fixed at the lowest tier that
   can express it, and a new tier-3 adapter needs a failing test naming the divergence.

**Done when:** every divergence found on Android is absorbed below the test body — the same
test source passes on Windows and Android with no platform conditionals in the test project.
That is goal (c), demonstrated rather than asserted.

### Phase 7 — The 28 parked control failures (deferred)

**Deliberately last, and parked rather than ignored:** no `[Skip]`, no deletion, no filter
that hides them. They keep failing visibly until this phase addresses them. Filtering them
out of a run (`--filter "FullyQualifiedName!~DatePicker"`) is a way to read *other* results
while working, never a way to close them.

These are the failures remaining after [RCA-001](rca/rca-001-container-module-tests-navigation-stack.md)
and [RCA-002](rca/rca-002-page-precondition-discarded-slow-failures.md) recovered 13 tests.
`AGENTS.md` lists them as one undifferentiated group. **They are not one problem** — a first
diagnostic pass separates them into at least three:

| Group | Tests | Failure signature | Diagnosis |
|---|---|---|---|
| `DatePickerTests`, `TimePickerTests` | 13 | not yet diagnosed | unknown — do this first |
| `StepperTests` | 11 | `ElementNotFoundException: AutomationId:TestStepper` | **element absent from the UIA tree** |
| `ProgressBarTests`, `ImageTests` | 3 | `AssertionException` on a found element | **element present, value wrong** |
| `SwitchTests.Switch_ClickTwice_TogglesOff` | 1 | state after two clicks | separate; pre-dates this work |

The Stepper and ProgressBar/Image groups are *opposite* problems, and conflating them would
send the fix in the wrong direction — which is why the table records signatures rather than
just counts.

**Stepper — a platform addressability limitation, not a Brinell defect.** `TestStepper`
carries an `AutomationId` in `RangeView.xaml`, yet is never found; the `Slider` on the same
page passes every test. MAUI's Windows Stepper maps to a WinUI control whose automation peer
does not surface the id. The repo's established remedy is to wrap the control in
`AutomationContainer` (already used in `CollectionModuleView.xaml` and the probe page).

That remedy is deferred deliberately, because it is a **sample-app XAML change driven by a
Windows limitation**, and §4.4 says the sample app should not be reshaped to suit one
platform's test framework. `AutomationContainer` is platform-neutral markup, so the change is
probably legitimate — but it should be made once, with the Android picture visible, not
speculatively against Windows alone.

**Why the whole phase is last.** Every control here is in the phase 3 conversion set, and
DatePicker/TimePicker are the most likely tier-3 adapter candidates in the entire control set
— Windows renders a calendar flyout, Android a spinner dialog, iOS a wheel. Fixing them
against Windows now risks encoding Windows assumptions into exactly the controls where §4.5's
adapter seam is most needed.

**What this phase must establish before any fix:**

1. **Whether DatePicker/TimePicker are one bug or many.** Undiagnosed so far.
   RCA-002 is the precedent for assuming too little: six "control" classes shared one cause.
2. **Whether each is control logic or test/page-object setup.** An empty exception message
   with a sub-millisecond duration means a constructor throw, not a control defect.

**Done when:** every test passes on Windows, or each remaining failure is a recorded,
understood platform limitation with a named reason — never a silent skip. `AGENTS.md`'s
known-failures list is updated to match what is actually true.

---

## 6. Risks

| Risk | Why it matters | Response |
|---|---|---|
| Phase 1 changes behaviour, not just structure | `TryClick` swallowed failures; removing that surfaces tests that were passing for the wrong reason | Expect new failures in phase 1 and treat each as a real bug found, not a regression to paper over |
| Batch 3d may exceed the generator | Self-referencing three-parameter generics are untested in the generator | Prototype `IndicatorView` (3b) and one collection before committing the batch; fall back to hand-written with a recorded reason |
| Android reveals missing `AutomationId`s | Windows UIA often finds elements by name where Android needs an explicit id | Fix in shared XAML, never with a platform branch in a control object |
| iOS unverifiable without a macOS runner | Goal 3 says "supportable", not "verified" | Land projects + traits; state plainly that iOS is unverified |
| Scope creep into full method coverage | Goal 11 says basics only | Any new `*Core` beyond what a basics test needs is out of scope for this plan |
| Building the adapter framework too early | Without an Android run, which controls diverge is a guess; an unused framework is untested code | Phase 6 is gated on phase 4 producing at least one tier-3 control |
| Generator grows shapes nothing uses | Same failure mode as above, in the generator | Add a generator only when a batch demands it; the loud near-miss error (3.1) is the general fix |
| iOS design paths never executed | Phase 2's iOS branches are written but unrun until a macOS host exists | Mark them `// iOS: unverified`; never report iOS as passing |

---

## 7. Decisions

Previously open, now settled:

1. **`Brinell.Maui.UITests2` — resolved, it is gone.** The directory no longer exists. What
   remains is a stale namespace: `Pages/AppShellPage.cs` still declares
   `namespace Brinell.Maui.UITests.Pages2` and `MauiFixture.cs` still has
   `using Brinell.Maui.UITests.Pages2`. Rename both to `...UITests.Pages` as part of phase 1
   cleanup — a one-line change that removes a misleading signal before the phase 4 split.
2. **Community Toolkit is the first external consumer.** Moved out of phase 3 and into
   phase 5, step 2, where converting it using only public API is the test that proves
   goal 13.
3. **iOS is deferred, not dropped.** No macOS machine is available on this one. Phase 4
   documents the three routes to enabling it (hosted macOS runner, cloud device farm, or
   local hardware) and confirms that nothing in phases 1–3 needs to change to make it work
   later. Status until then: **architecturally supported, unverified.**

## 8. Still open

1. **Which controls actually diverge per OS.** Unanswerable until Android runs; that is the
   input to phase 6 and the reason it is last.
2. **Whether the base hierarchy needs a non-interactive sibling.** Batch 3a (Shapes, BoxView)
   is the evidence; decide with it in hand.
3. **Whether the `Comparison` additions should land in `Brinell.Core` or stay MAUI-local.**
   `Comparison` is in `Brinell.Core`, and WPF, WinForms and Html each have their own
   `GetItemTextsCore` with the same `IReadOnlyList<string>` shape — so `SequenceEquals`,
   `HasItem` and `Count` (§3.1, fix 1) would benefit all of them. But those platforms are
   out of scope here and their generators are not wired up. Land the enum values in
   `Brinell.Core` (they are inert until a generator reads them) and leave the other
   platforms' adoption to whoever owns them.

---

## 9. What this plan does not cover

Stated so the boundary is explicit rather than discovered later:

- **Full method coverage per control** — goal 11, deliberately deferred. Basics only.
- **Other Brinell platforms.** §4.6 and §8.3 note where MAUI work touches shared contracts
  in `Brinell.Core`, but converting WPF/WinForms/Html to the generated format is separate
  work. The five-layer table and the capability model are written to be reusable there;
  nothing here obliges those platforms to adopt them on any timeline.
- **Performance.** Nothing here measures test-suite runtime. If phase 1's removal of the
  swallowed-failure paths changes suite duration materially, that is worth a look, but it
  is not a goal.
- **The recorder/scraper tooling** under `tools/Brinell.Scraper`, which generates control
  objects from scraped DOM. It consumes the same control-object format, so §3.1's stricter
  contract may affect it — worth a check during phase 3, but out of scope to change.
