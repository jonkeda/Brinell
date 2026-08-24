---
title: Containers and Collections — design set
description: Index for the Brinell.Maui ContainerObject / CollectionObject design
---

# Containers and Collections

Design for treating a container as a `ContainerObject` (a PageObject sibling that holds
`ControlObject`s and other `ContainerObject`s, scoping searches inside itself), and for
collections that hand out real scoped list items. Scoped to `Brinell.Maui`.

> **Status: design only — not implemented.** No file under `srcnew/` or `testsnew/` has been
> modified. Everything here is a proposal plus reference code awaiting a go-ahead.

## Documents

| File | What it covers |
|---|---|
| [container-and-collection-design.md](container-and-collection-design.md) | The design: what is broken (verified), the proposed bases, migration, resolved decisions |
| [generator-changes.md](generator-changes.md) | What `Brinell.Generator` needs first — two blocking changes, one addition |
| [sample-app-ui-tests-design.md](sample-app-ui-tests-design.md) | How to add the demo page to `Brinell.Samples.Maui.App` and end-to-end coverage to `Brinell.Maui.UITests` |

## Destinations when implementing

The sample app page and the tests are meant to become part of the actual MAUI codebase —
they are staged here, not intended to live here. **Move them only on an explicit instruction
to start implementing.**

| Staged file | Destination |
|---|---|
| [samples/GridCollectionDemoView.xaml](samples/GridCollectionDemoView.xaml) | `samples/Brinell.Samples.Maui.App2/Views2/GridCollectionDemoView.xaml` |
| — (new, write at implementation time) | `samples/Brinell.Samples.Maui.App2/Views2/GridCollectionDemoView.xaml.cs` |
| [samples/GridCollectionDemoViewModel.cs](samples/GridCollectionDemoViewModel.cs) | `samples/Brinell.Samples.Maui.App2/ViewModels2/GridCollectionDemoViewModel.cs` |
| — (new, write at implementation time) | `samples/Brinell.Samples.Maui.App2/Pages2/GridCollectionPage.xaml` + `.xaml.cs` |
| [samples/GridCollectionDemoPage.cs](samples/GridCollectionDemoPage.cs) | split → `testsnew/Brinell.Maui.UITests2/Pages2/GridCollectionDemoPage.cs` and `Containers2/{ProductFormContainer,ProductOptionsContainer,ProductCollection,ProductRow}.cs` |
| [samples/GridContainerTests.cs](samples/GridContainerTests.cs) | `testsnew/Brinell.Maui.UITests2/Tests2/Container/GridContainerTests.cs` |
| [samples/CollectionViewTests.cs](samples/CollectionViewTests.cs) | `testsnew/Brinell.Maui.UITests2/Tests2/Collection/ProductCollectionTests.cs` |
| [samples/ContainerCollectionUnitTests.cs](samples/ContainerCollectionUnitTests.cs) | `testsnew/Brinell.Maui.Tests/ContainerCollectionTests.cs` |
| [samples/VerifiedDefectRecordTests.cs](samples/VerifiedDefectRecordTests.cs) | delete on implementation — it records the *old* behaviour, which will no longer exist |

Also required at implementation time, not staged here:

- register the new page in the sample app's tab/navigation shell, next to `ContainersPage`
- add `GridCollectionDemoPage` + `NavigateToGridCollectionDemo()` to
  `testsnew/Brinell.Maui.UITests2/MauiFixture.cs`

Everything except `VerifiedDefectRecordTests.cs` targets the **proposed** bases and does not
compile until migration steps 1–5 land. Each file repeats its destination in a header comment.

## Verification status

`VerifiedDefectRecordTests.cs` was compiled and run against the current `Brinell.Core` +
`Brinell.Maui` — **4 passed, 0 failed**. It pins three defects and one already-correct behaviour:

| Test | Records |
|---|---|
| `Works_ControlInContainer_IsScopedAndReturnsContainer` | Already correct — controls in a container are element-scoped and return the container. Do not regress. |
| `Defect_ContainerOwnAction_ReturnsPage` | §3.1 — the container's own inherited members return the page |
| `Defect_RepeatingRowId_AllIndexesCollapseToSameRow` | §3.2 — row roots resolve page-wide, so repeating template ids collapse |
| `Defect_GetItemCount_CapsAt100` | §3.2 — sequential probing, silently truncated at 100 |

Running these required an isolated project referencing only `Brinell.Core` and `Brinell.Maui`:
`Brinell.Maui.Extensions` does not build at `HEAD` (34 errors, from the in-flight `ControlBase`
refactor — unrelated to this design), and it blocks `Brinell.Maui.Tests`. **That must be fixed
before implementation starts**, or the ported unit tests cannot run.

## Approach

Two decisions shape the whole plan:

- **No backward compatibility.** `ContainerBase` and `List<TScope,TItem>` are replaced and
  deleted — no `[Obsolete]` shims. The MAUI blast radius is 2 source files and a handful of test
  files, all inside this repo, with no external callers.
- **Regenerate, don't preserve.** `.gen.cs` files are build artifacts. Change the generator, run
  `tools\Scripts\CreateMaui.Bat` over all 30 templates, take the output.

## Reading order

1. `container-and-collection-design.md` §3 — what is actually broken, and what already works
2. `generator-changes.md` — the generator is a prerequisite, not a follow-up
3. `samples/GridCollectionDemoPage.cs` — what the API feels like at the call site
4. `container-and-collection-design.md` §6 and §8 — migration and the four resolved decisions
