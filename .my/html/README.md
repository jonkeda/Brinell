---
title: Brinell.Html upgrade — design set
description: Index for bringing Brinell.Html onto the generator, the container structure, and the base class structure already proven in Brinell.Maui
---

# Brinell.Html upgrade

`Brinell.Maui` was rebuilt around three things: `*.tpl.cs` / `*.gen.cs` control objects
produced by `Brinell.Generator`, a shared scope root (`RootedScopeBase`) with real
`ContainerObject`s and `CollectionObject`s under it, and a layered `Controls/Base`
hierarchy on top of `ControlObjectBase<TScope>`.

`Brinell.Html` has **none of the three**. It is where `Brinell.Maui` was before the
rewrite: hand-written public methods, no Core methods, a page base and a container base
that share nothing, and collections that return strings instead of scoped items.

This set plans the same upgrade for `Brinell.Html`, and — because HTML is the second
driver through the door — makes the generator driver-agnostic on the way so WinForms,
WPF, Stride and Blazor can follow without another fork.

> **Status: not started.** Nothing in this set is implemented. Every claim about current
> code was read out of the tree on 2026-09-09; every claim about what will happen after a
> change is a prediction and is marked as such.

## Documents

| File | What it covers |
|---|---|
| [gap-analysis.md](gap-analysis.md) | What `Brinell.Html` has today vs. `Brinell.Maui`, member by member, and which gaps actually block the generator |
| [upgrade-plan.md](upgrade-plan.md) | The phased work, in dependency order, with verification per phase |
| [open-questions.md](open-questions.md) | Four decisions that change the shape of the work and are yours, not mine |

Related, outside this folder:

- [`.my/fix/generator-and-html-base-defects.md`](../fix/generator-and-html-base-defects.md) —
  three defects found while writing this set, each shippable on its own and ahead of it.
  Its §1 supersedes Phase 0 step 2 of the plan.

## Scope

- `srcnew/Brinell.Html`, `srcnew/Brinell.Html.Playwright`, `tools/Brinell.Generator`,
  `tools/Scripts`, `testsnew/Brinell.Html.Tests`, `testsnew/Brinell.Html.UITests`.
- Out of scope: `Brinell.Blazor` (a separate driver that happens to test the same sample
  app), `Brinell.Uat`, and every other driver — though Phase 0 is deliberately built so
  they inherit it for free.
