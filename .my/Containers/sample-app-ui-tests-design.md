---
title: MAUI Sample Container and Collection Test Integration
description: Design for adding container and collection demo pages to Brinell.Samples.Maui.App and end-to-end coverage to Brinell.Maui.UITests
status: implemented (2 of 25 tests skipped - see section 8.1)
scope: samples/Brinell.Samples.Maui.App, testsnew/Brinell.Maui.UITests
---

# MAUI Sample Container and Collection Test Integration

## 1. Goal

Add an executable sample surface and UI tests that prove the new Brinell.Maui
container and collection behavior end to end:

1. Add a Grid and CollectionView demo page to `Brinell.Samples.Maui.App`.
2. Add page objects, container objects, and tests to `Brinell.Maui.UITests`.
3. Verify nested scoping, fluent container returns, repeating item-template
   AutomationIds, collection mutation, and virtualized item discovery.

This document turns the staged reference files in [samples/](samples/) into an
implementation plan for the non-`2` sample and UI-test projects requested here.
It does not preserve the older `ContainerBase` or `List<TScope,TItem>` APIs.

## 2. Prerequisites

Implement the framework and generator work in this order before porting the
sample page:

1. Complete [generator-changes.md](generator-changes.md).
2. Complete the `ContainerObjectBase`, `CollectionObjectBase`, and
   `ItemContainerBase` migration in
   [container-and-collection-design.md](container-and-collection-design.md).
3. Run `tools\Scripts\CreateMaui.Bat` and verify generated controls compile.
4. Run the focused Brinell.Maui unit tests, including the staged
   `ContainerCollectionUnitTests.cs` coverage.

The sample page object intentionally uses the proposed bases and should not be
weakened to compile against the legacy APIs. Repeating row AutomationIds are an
acceptance condition, not a detail to work around with generated unique IDs.

## 3. Sample Application Design

### 3.1 Page shape

Use one Shell page named `GridCollectionPage`. It hosts a
`GridCollectionDemoView` containing two test sections:

| Section | Purpose | Required behaviors |
|---|---|---|
| Product form | Real nested container hierarchy | Grid child lookup, nested options scope, text input, checkbox state, add command |
| Products | Collection as a scope | Collection-level controls, typed rows, repeating row IDs, delete, clear, reset, bulk add, virtualization |

Keep the CollectionView directly in a Grid or another bounded layout. Do not
place it in a StackLayout or in the page's outer ScrollView. A suitable page
shape is a two-row Grid: an `Auto` product-form row and a `*` collection row.
The CollectionView owns scrolling for its rows.

Use `AutomationContainer` only as the platform bridge that exposes a scope root
to Windows UI Automation. The Brinell page object must model the logical Grid,
nested options container, collection, and rows as real container objects.

### 3.2 Stable automation contract

The following IDs form a test contract and must not be renamed without updating
the page objects:

| Scope | AutomationIds |
|---|---|
| Page | `GridCollectionPage`, `PageTitle` |
| Form root | `ProductFormContainer` |
| Form children | `ProductFormTitle`, `ProductNameEntry`, `ProductPriceEntry`, `ProductAddButton` |
| Nested options root | `ProductOptionsContainer` |
| Nested options children | `ProductInStockCheckBox`, `ProductInStockCaption` |
| Collection root | `ProductListContainer` |
| Collection controls | `ProductListTitle`, `ProductListEmptyLabel`, `ProductCountLabel`, `ProductClearButton`, `ProductResetButton`, `ProductBulkAddButton` |
| Repeating row root | `ProductRow` |
| Repeating row children | `ProductSelectedCheckBox`, `ProductNameLabel`, `ProductPriceLabel`, `ProductStockLabel`, `ProductDeleteButton` |

Every rendered row must use exactly the same row and child IDs. Do not add an
index, database ID, or product name to an AutomationId.

### 3.3 View model and deterministic state

Seed three products in a fixed order: Keyboard, Mouse, and Monitor. Add commands
for add, delete, clear, reset, and bulk add. `ResetDemoCommand` must restore the
three seed products and reset form fields and selection.

The reset command is required because `[Collection("Maui")]` shares one fixture
and one Shell instance across test classes. Shell may retain page instances, so
navigation alone does not guarantee clean state. Every test constructor must
navigate to the page and invoke reset before making assertions.

Expose both of these observable states:

- logical product count through `ProductCountLabel`;
- currently discoverable item roots through the collection object API.

Do not equate those values under virtualization. A 63-item data source may have
only a small number of realized row roots in the automation tree.

### 3.4 Files

Add these files:

```text
samples/Brinell.Samples.Maui.App/
  Pages/GridCollectionPage.xaml
  Pages/GridCollectionPage.xaml.cs
  Views/GridCollectionDemoView.xaml
  Views/GridCollectionDemoView.xaml.cs
  ViewModels/GridCollectionDemoViewModel.cs
```

Adapt the staged files rather than copying their `App2`, `Views2`, or
`ViewModels2` destinations. Match the namespaces already used by the live
project. Add compiled `x:DataType` declarations to the view and item template.

Update `AppShell.xaml` with a ShellContent entry:

- title: `Containers`;
- route: `GridCollectionPage`;
- AutomationId: `GridCollectionTab`;
- icon: an existing valid `.png` resource reference.

Update the sample `.csproj` only where explicit MauiXaml/DependentUpon entries
are required by its current project style. Do not add per-project package
versions.

## 4. UI-Test Design

### 4.1 Page-object composition

Add the following types under the existing `Brinell.Maui.UITests` project:

```text
Pages/GridCollectionDemoPage.cs
Containers/ProductFormContainer.cs
Containers/ProductOptionsContainer.cs
Containers/ProductCollection.cs
Containers/ProductRow.cs
Tests/Container/GridContainerTests.cs
Tests/Collection/ProductCollectionTests.cs
```

Use the staged `GridCollectionDemoPage.cs` as the behavioral reference, split by
ownership as shown above. Construct the top-level form and collection once in
the page constructor so their root caches survive between calls. A row receives
an already discovered `IMauiElement` root and its index; it must never locate
itself from the page by a unique ID.

Keep domain interactions on the owning object:

- `ProductFormContainer.FillProduct(...)` fills the form and returns itself;
- `ProductCollection.ByName(...)` locates a row by visible content;
- `ProductCollection.Reset()` invokes the reset control and waits for the
  logical seeded state.

### 4.2 Navigation and fixture integration

Update `AppShellPage` with `GridCollectionTab`. Update `MauiFixture` with a
cached `GridCollectionDemoPage` property and
`NavigateToGridCollectionDemo()`. Navigation must:

1. click `GridCollectionTab`;
2. wait for `GridCollectionDemoPage.IsLoaded()`;
3. reset the demo;
4. wait until `ProductCountLabel` reports three products.

Keep `[Collection("Maui")]`, xUnit `Assert`, the existing timeout constant, and
the current fixture lifecycle. Do not add sleeps; wait for count, visibility,
or row materialization changes.

### 4.3 Container tests

`GridContainerTests` must cover:

1. Form children resolve inside `ProductFormContainer`.
2. A page-level control cannot be found through the form scope.
3. Nested options controls resolve inside `ProductOptionsContainer`.
4. The nested scope cannot find a sibling from its parent.
5. A child control action returns `ProductFormContainer`.
6. A container assertion returns the same container instance.
7. `Parent` exits one scope level at a time.
8. Cache invalidation permits subsequent child resolution.
9. Filling and submitting the form changes logical count from three to four.
10. The nested checkbox can be toggled without leaving its scope.

These tests prove both UI interaction and the compile-time fluent API. Do not
replace the typed `Assert.Same` checks with looser existence assertions.

### 4.4 Collection tests

`ProductCollectionTests` must cover:

1. `Item(0)`, the indexer, and `TryItem` return typed rows with correct indices.
2. Out-of-range `TryItem` returns null and `Item` throws the documented error.
3. Rows 0-2 return Keyboard, Mouse, and Monitor despite repeating child IDs.
4. All controls in a row resolve relative to that row.
5. Selecting or deleting one row does not act on another row.
6. A row cannot find collection-level controls.
7. Collection-level title, count, empty state, and command controls resolve.
8. Clear shows the empty state; reset restores the seed state.
9. Delete shifts remaining logical rows without app-side reindexing.
10. `FindItem`, `ItemWhere`, and `ByName` search by row content.
11. Collection assertions and actions return the collection instance.
12. Bulk add reports logical count 63 through `ProductCountLabel`.
13. `ScrollToItem(60)` materializes the off-screen row without a fixed delay.
14. Content search can find `Bulk Product 55` after scrolling.

Do not assert that `GetItemCount()` equals 63 unless the final framework
contract explicitly counts the data source rather than realized UI elements.
Use `ProductCountLabel` for logical count and row APIs for materialization.

## 5. Implementation Order

1. Land generator and framework prerequisites with focused unit tests.
2. Port and adapt the view model, view, and host page.
3. Register the Shell tab and build the sample app.
4. Add page/container objects and fixture navigation.
5. Add deterministic container tests, then collection tests.
6. Run each UI-test class independently before running both classes together.
7. Run both classes together to prove fixture reset and order independence.

## 6. Verification

Working directory: Brinell root.

First compile the changed projects:

```powershell
dotnet build samples\Brinell.Samples.Maui.App\Brinell.Samples.Maui.App.csproj -f net10.0-windows10.0.19041.0 -v:minimal /nr:false
dotnet build testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj -v:minimal /nr:false
```

Then follow [the MAUI run guide](../../docs/run/MAUI.md) to launch the sample
and run one UI-test class at a time:

```powershell
dotnet test testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj --filter FullyQualifiedName~GridContainerTests -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj --filter FullyQualifiedName~ProductCollectionTests -v:minimal /nr:false
```

Finally run a filter containing both new classes. The combined run is the
regression check for shared-fixture state leakage.

## 7. Acceptance Criteria

Implementation is complete when:

- the Containers tab opens the new page in `Brinell.Samples.Maui.App`;
- the page uses a bounded CollectionView and compiled bindings;
- all rows use repeating AutomationIds with no app-side reindexing;
- nested containers cannot escape to parent or page scope implicitly;
- fluent actions return their owning container or collection;
- typed row lookup distinguishes the three seed rows;
- deleting a row preserves correct scope for shifted rows;
- an off-screen bulk row is found through state-based scrolling;
- each new test class passes alone and both pass together;
- no arbitrary sleeps, obsolete ListView/TableView, or compatibility shims are
  introduced.

## 8. Implementation Status

Implemented and passing on Windows/FlaUI: **23 of 25 tests**, both classes together
(`GridContainerTests` 10/10, `ProductCollectionTests` 13/15). The two exceptions are
skipped for the reason in 8.1, which was re-diagnosed by measurement after the first
explanation proved wrong.

Met in full: sample page and Shell tab, bounded CollectionView with compiled bindings,
repeating row AutomationIds with no reindexing, nested scoping with no implicit escape,
fluent returns, typed row lookup across the three seed rows, delete-and-shift scoping,
per-class and combined runs, and no sleeps or shims.

### 8.1 Blocked: deep scrolling a virtualized CollectionView

Two acceptance items — 4.4 requirement 13 (`ScrollToItem(60)`) and requirement 14
(finding `Bulk Product 55`) — are **not met**. Both tests are `[Fact(Skip = ...)]`.

**The original diagnosis in this section was wrong, and has been corrected by
measurement.** It claimed the cause was a missing scroll primitive and proposed adding one.
The primitive was built; it works; the tests still fail, for a different reason.

#### What was built

`IElement.TryScrollContent(verticalSteps, horizontalSteps)` — a default interface method
returning false, so no platform adapter was forced to change — implemented in
`FlaUIMauiElement` against `_element.Patterns.Scroll.Pattern`, walking to the nearest
scrollable ancestor when the element itself does not scroll. `ScrollHelper` gained
`TryScrollForward`/`TryScrollBack`, and `CollectionObjectBase.TryMaterializeMore` now tries
the scroll pattern **before** falling back to a pointer swipe.

This is a genuine improvement independent of the two skipped tests: materialization is now
UI Automation first, so it is not gated by `BRINELL_WINDOWS_ALLOW_POINTER_INPUT`.

One real bug surfaced while building it. Reading `VerticalScrollPercent` immediately after
`Scroll(...)` returns the **pre-scroll** value — the property does not update
synchronously. The first implementation therefore reported "no progress" while rows were
demonstrably realizing. Fixed by polling for the change (`WaitForScrollChange`, 500ms
budget). Worth knowing for any future scroll work.

#### The actual blocker: row recycling

A probe (`ScrollPatternProbeTests`, kept as a diagnostic) measured the ground truth on a
63-row list:

| Measurement | Value |
|---|---|
| `VerticallyScrollable` | **True** — the pattern is supported |
| `VerticalScrollPercent` after repeated scrolling | **100** — fully at the end |
| Realized rows at 0% | 10 |
| Realized rows at 100% | **30 of 63** |

Scrolling works perfectly and reaches the end of the list. MAUI's CollectionView simply
**recycles row containers**: only ~30 are in the automation tree at any moment, and which
30 depends on scroll position. Index 60 is therefore never simultaneously present with
index 0.

That is a data-model mismatch, not a missing capability. `Item(int)` addresses a position
in the *realized window*, and no scroll primitive can make a recycled row exist. Expressing
requirement 13 would need an API that scrolls and re-resolves as the window moves — for
example a `ScrollUntil(predicate)` that returns the row it stopped on rather than an index.
That is a design change to `CollectionObjectBase`, outside this document.

**Do not attempt to unblock these two tests by adding more scroll primitives.** The
scrolling is not the problem.

### 8.2 Framework limitation found: asserting absence — **FIXED**

Generated `Assert*` and `Wait*` members resolved the element before comparing, so
`AssertVisible(false)`, `AssertExists(false)`, and `WaitExists(false)` raised
`ElementNotFoundException` instead of reporting absence.

This has since been fixed — see
[`.my/fixes/waitexists-absence-assertions.md`](../fixes/waitexists-absence-assertions.md)
§8. An `[AbsenceTolerant]` attribute marks the presence/visibility queries, and the
generator emits null-tolerant helpers for them while value assertions keep the strict path.

`Clear_ShowsEmptyState_ResetRestoresSeed` no longer needs the `IsExists()` workaround and
now reads `Page.Products.EmptyLabel.AssertExists(false)`.

### 8.3 Deviation: collection height

The design specifies a two-row Grid with the collection in a `*` row. That alone left
the third seed row unrealized on a short window, so the CollectionView also carries
`HeightRequest="320"`. The layout is otherwise as specified: the collection is bounded,
owns its scrolling, and sits outside any StackLayout or page-level ScrollView.
