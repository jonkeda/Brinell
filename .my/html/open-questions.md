---
title: Open questions — Brinell.Html upgrade
description: Four decisions that change the shape of the work
---

# Open questions

Four decisions that change what gets built. Each has a recommendation and the cost of the
alternative. None of them blocks Phase 0.

## 1. Control naming: keep the `Control` suffix?

HTML names controls `ButtonControl<TScope>`, `SelectControl<TScope>`, `LabelControl<TScope>`.
MAUI names them `Button<TScope>`, `Picker<TScope>`, `Label<TScope>`. Same idea, different
convention.

Likewise the base: HTML's root is `ControlBase<TScope>`, MAUI's is `ViewBase<TScope>`.

**Recommendation: rename the bases (`ControlBase` → `ViewBase`), keep the leaf `Control`
suffix.** The bases are internal to the framework and renaming them costs nothing outside
the driver. The leaves are typed into every page object in `Brinell.Html.UITests` and
`Brinell.Html.Uat.Tests`, and `Button` would collide awkwardly with `ButtonControl` during
any transition period.

Cost of full alignment: a breaking rename across 6 page objects and 7 test files, plus
whatever is outside this repo. Cost of no alignment: the two drivers keep reading
differently forever.

**Affects:** Phase 2 step 1, Phase 3.

## 2. What happens to the `IHtmlAsync*` interfaces?

`srcnew/Brinell.Html/Interfaces/Async/` declares nine interfaces implemented explicitly on
the bases. Every implementation is `Task.FromResult(SyncMethod(...))` — synchronous work in
an async wrapper. The generator emits sync members only and has no async story.

Three options:

| Option | What it means | Cost |
|---|---|---|
| **Delete** | drop `Interfaces/Async/` and the explicit implementations | breaks any consumer awaiting them; `HtmlAsyncExtensions.cs` goes too |
| **Keep as-is** | generated sync members, hand-written async forwarders alongside | the forwarders must be maintained by hand forever and will drift from the generated surface |
| **Generate** | teach the generator an async member family | real work in `tools/Brinell.Generator`, and it only pays off if a driver ever does genuinely async I/O |

**Recommendation: delete**, unless something outside this repo consumes them. They provide
no concurrency — `Task.FromResult` over a blocking call is strictly worse than the
blocking call, because it looks like it yields and does not. `PlaywrightTestContext` does
have genuinely async navigation, but that lives on the context, not on controls.

If they must stay, keep them **exactly** as thin forwarders and never let a Core method
exist only in async form.

**Affects:** Phase 6. Decide before Phase 3 finishes so the leaf conversions know whether
to carry the explicit implementations across.

## 3. Should Phase 0 be generalised further, for the other four drivers?

`Brinell.Wpf`, `Brinell.WinForms`, `Brinell.Stride` and `Brinell.Blazor` all have the same
hand-written `Controls/` shape HTML has. None uses the generator.

Phase 0 as written makes the generator driver-agnostic and gives it a
`Generate.Bat <driver>` entry point, which is all any of them would need. The question is
whether to prove that now by converting one control in a second driver, or to let HTML be
the only proof.

**Recommendation: let HTML be the proof.** It is the widest driver after MAUI and exercises
containers, collections, selectors, ranges and toggles. If the generator handles HTML
unchanged, it will handle WPF. Converting a fifth driver speculatively adds review surface
for no new information.

**Affects:** Phase 0 scope only.

## 4. Should the HTML container root be cached?

`ContainerObjectBase` in MAUI caches the container root element (`CacheContainerRoot => true`)
with a staleness check, because re-finding through UI Automation is expensive and roots go
stale on Android.

Neither pressure exists in the DOM. Playwright re-resolves locators on use, and finding by
CSS is cheap.

**Recommendation: port the caching machinery but default `CacheContainerRoot` to `false`
for HTML**, and turn it on per-container only if a measurement asks for it. Porting the
mechanism costs little and keeps the two drivers structurally identical; defaulting it on
would import a workaround for a problem HTML does not have, along with its staleness bugs.

**Affects:** Phase 4 step 1.
