---
title: Containers and Collections — design set
description: Index for the Brinell.Maui ContainerObject / CollectionObject work
---

# Containers and Collections

Design and rollout for treating a container as a `ContainerObject` (a PageObject sibling
that holds `ControlObject`s and other `ContainerObject`s, scoping searches inside itself),
and for collections that hand out real scoped list items. Scoped to `Brinell.Maui`.

> **Status: partially implemented.** The framework layer, the generator change, the sample
> demo page, and Phases 0–1 of the rollout are done and verified against the real Windows
> app. Phases 2–6 are not started. Each document states its own status.

## Documents

| File | What it covers | Status |
|---|---|---|
| [container-and-collection-design.md](container-and-collection-design.md) | The design: what was broken (verified), the bases, migration, resolved decisions | implemented |
| [generator-changes.md](generator-changes.md) | The `Brinell.Generator` fluent-return-type change | implemented |
| [sample-app-ui-tests-design.md](sample-app-ui-tests-design.md) | The `GridCollectionDemo` page and its end-to-end coverage | implemented; §8 records what was blocked |
| [common-controls-rollout-plan.md](common-controls-rollout-plan.md) | Rolling the bases out to every container and collection control | Phases 0–1 done, 2–6 open |
| [uncovered-areas-plan.md](uncovered-areas-plan.md) | Media, Navigation, and the data-management scenario — the areas lost with `UITests2` | not started |

Related, outside this folder:

- [`.my/fixes/waitexists-absence-assertions.md`](../fixes/waitexists-absence-assertions.md) —
  `AssertExists(false)` and friends throw instead of reporting absence. Blocks clean
  empty-state assertions in both plans.

## What is built

| Thing | Where |
|---|---|
| `ContainerObjectBase`, `CollectionObjectBase`, `ItemContainerBase`, `ItemStrategy`, `ScrollHelper` | `srcnew/Brinell.Maui/Containers/` |
| Container controls on the new bases: `Grid`, `Border`, `ContentView`, `ScrollView`, `ContentDialog` | `srcnew/Brinell.Maui/Controls/` |
| Automation handlers the app under test must register | `samples/Brinell.Maui.AppSupport/` |
| `GridCollectionDemo` page + `AutomationProbe` page | `samples/Brinell.Samples.Maui.App/` |
| 28 container/collection UI tests, 3 probe tests | `testsnew/Brinell.Maui.UITests/` |
| 32 container/collection unit tests | `testsnew/Brinell.Maui.Tests/ContainerCollectionTests.cs` |

`srcnew/Brinell.Maui/Controls/ContainerBase.cs` was deleted in Phase 1.
`Controls/List.cs` survives until rollout Phase 3 re-bases its remaining consumers.

## Two facts that shape everything here

**Windows needs automation handlers.** Stock MAUI layouts and content containers map to
WinUI panels with no `AutomationPeer`, so their `AutomationId` is invisible to UI
Automation and a container object targeting one will not resolve — with no diagnostic
beyond `ElementNotFoundException`. The app under test must register the handlers from
`Brinell.Maui.AppSupport`, either by project reference or by copying the sources. Measured
in rollout Phase 0; 10 of 13 layout types are addressable once registered, and
`SwipeView`/`RefreshView` must **not** be (overriding their peers collapses the whole UIA
tree).

**Item templates use repeating ids.** Rows share one set of `AutomationId`s and scoping
keeps them distinct. Unique per-row ids would make item-scoping tests pass without testing
anything.

## Deleted

`testsnew/Brinell.Maui.UITests2` and `samples/Brinell.Samples.Maui.App2` were removed after
Phase 1 — a stale parallel copy, never in the solution, unable to navigate, with 14 of ~250
tests discovered and 12 of those failing. The areas they nominally covered are picked up by
[uncovered-areas-plan.md](uncovered-areas-plan.md).

The `samples/` folder here holds the original staged reference files. They were adapted into
the live projects and are kept only as a record; the live code is authoritative.

## Reading order

1. `container-and-collection-design.md` §3 — what was broken, and what already worked
2. `common-controls-rollout-plan.md` §3 Phase 0 — the Windows automation finding
3. `sample-app-ui-tests-design.md` §8 — the two limits that constrain every collection test
   (deep virtualized scrolling, absence assertions)
4. `common-controls-rollout-plan.md` §6 — the five resolved design decisions
