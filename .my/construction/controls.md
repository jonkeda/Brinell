# Construction UI test migration - controls

Source of truth for the ControlObjects the migration builds. Every control ships as
`.tpl.cs` + generated `.gen.cs` under `Exact.Core.Construction.UITest/Controls/` (in the
Construction repo) unless noted otherwise. Follow the
[maui-control](../../.github/skills/maui-control/SKILL.md) skill to add new controls and
[convert-control](../../.github/skills/convert-control/SKILL.md) to convert loose ones.

State column: `-` = not started, `exists` = already present in the repo, `convert` =
convert loose class to `.tpl.cs`/`.gen.cs`, `new` = write from scratch, `wip`, `done`.

Source XAML lives under `Exact.Core.Construction/Controls/` (shared library) and
`Exact.Construction/Modules/<Module>/Views/` (module views).

---

## Seeds (already present in the repo)

These 8 controls already exist in `Exact.Core.Construction.UITest/Controls/<Folder>/` as
`.tpl.cs` + `.gen.cs` pairs, namespaced `Exact.Core.Construction.UITest.Controls.<Folder>`
and referencing Brinell through the shared props. Nothing to create; phase 0 just confirms
they build.

| Id | Brinell control | Source folder | Purpose |
| --- | --- | --- | --- |
| S1 | `PageSearchControl<TParent>` | `Search/` | Entry + Search button + Cancel button + busy indicator |
| S2 | `InlineAlertControl<TScope>` | `Messaging/` | Inline alert with message + optional action button |
| S3 | `LoadingOverlayControl<TScope>` | `Loading/` | Overlay shown while a page is busy |
| S4 | `PageTitleBarControl<TSelf>` | `TitleBar/` | Page title + back button + optional secondary label |
| S5 | `PageHeaderActionsControl<TSelf>` | `HeaderActions/` | Header action bar (`refresh`, `add`, …) |
| S6 | `EmptyStateControl<TSelf>` | `Lists/` | Empty-state message + CTA |
| S7 | `FavoriteToggleControl<TScope>` | `Favorites/` | Star / favorite toggle button |
| S8 | `PageableCollectionControl<TScope, TItem>` | `Collection/` | Paged collection with load-more sentinel |

Phase 0 exit gate: all 8 build under the existing library without regenerating `.gen.cs`.

---

## Page pattern objects

Not ControlObjects but page-object bases. Live under
`Exact.Core.Construction.UITest/PagePatterns/`.

| Id | Object | Purpose | State |
| --- | --- | --- | --- |
| P1 | `OverviewPagePatternObject<TSelf>` | Base for `OverviewPagePattern` XAML: TitleBar, Search, Loading, EmptyState, Error, StatusText | exists |
| P2 | `DetailPagePatternObject<TSelf>` | Base for `DetailPagePattern` XAML: TitleBar, HeaderActions, content scroll | new |
| P3 | `EditPagePatternObject<TSelf>` | Base for `EditPagePattern` XAML: TitleBar, HeaderActions (Save/Cancel), FormContainer content | new |

`P3` is the same object referred to as `T4` in the PoC control list - it's built during
the PoC phase because Todos needs it.

---

## Todos PoC controls

Read from `Exact.Construction/Modules/Todos/Views/*.xaml`. These land during phase 1 so
phase 2 can wire the Todos test suite.

| Id | Construction XAML | Brinell control | Base | Notes | State |
| --- | --- | --- | --- | --- | --- |
| T1 | `search:PageSearchView` | S1 | Component | Exists - already listed above | exists |
| T2 | `messaging:InlineAlertView` | S2 | Component | Exists | exists |
| T3 | `loading:LoadingOverlay` | S3 | Component | Exists | exists |
| T4 | `pagePatterns:EditPagePattern` root of `TodoEditorView` | `EditPagePatternObject<TSelf>` | PageObjectBase | Same as P3 | new |
| T5 | `controls:FormContainer` | `FormContainerControl<TScope>` | Component | Wraps VerticalStackLayout of form sections | new |
| T6 | `formSections:FormSection` | `FormSectionControl<TScope>` | Component | Title + expand/collapse + content | new |
| T7 | `text:TextFormInputView` | `TextFormInputControl<TScope>` | Component | Label + Entry (`Value`), error label, required marker | new |
| T8 | `note:NoteFormInputView` | `NoteFormInputControl<TScope>` | Component | Label + Editor (multiline) | new |
| T9 | `dateTime:DateTimeFormInputView` | `DateTimeFormInputControl<TScope>` | Component | DatePicker + TimePicker composite; nullable | new |
| T10 | `selection:SelectionFormInputView` | `SelectionFormInputControl<TScope>` | Component | Tap → opens selection sheet; renders label of chosen items | new |
| T11 | inline `CollectionView` with `TodoListItemViewModel` | `TodoListCollection` + `TodoItemContainer` | Collection / ItemContainerBase | **Page-object side, not library.** Lives under `Exact.Construction.UITests/Modules/Todos/Containers/` | new |

Build order for T4-T10: T4 → T5 → T6 → T7 → T8 → T9 → T10.

For T11 do **not** add to the library. Bind `Name`, `PriorityText`, `DueDateText`,
`CompletionText`, `AssigneesText` from the inline `DataTemplate`. It lives under
`Exact.Construction.UITests/Modules/Todos/Containers/`. Mirror the `ContactListCollection` /
`ContactItemContainer` split already in `Modules/Contacts/Containers/`.

---

## Shared library controls (used by more than one module)

Ordered by priority. Add during the phase that first needs them, not eagerly.

### P1 - convert existing loose controls

Currently hand-written in `Exact.Construction.UITests/Controls/`. Convert with
[convert-control](../../.github/skills/convert-control/SKILL.md) into `.tpl.cs`+`.gen.cs`
under `Exact.Core.Construction.UITest/Controls/Loose/` (rename the folder when a
better home appears - e.g. `Buttons/`, `Navigation/`).

| Id | Source class | Brinell control | Base | State |
| --- | --- | --- | --- | --- |
| C6 | `RoundButtonControl.cs` | `RoundButtonControl<TScope>` | Button | convert |
| C7 | `TabMenuItemControl.cs` | `TabMenuItemControl<TScope>` | Component (icon + label + tap) | convert |
| C8 | `SearchOverlay.cs` | `SearchOverlayControl<TScope>` | Container (modal overlay) | convert |
| C9 | `ToggleRow.cs` | `ToggleRowControl<TScope>` | Component (label + Switch) | convert |

### P2 - new library controls (added when a P2 module needs them)

| Id | Construction control | Brinell control | Base | Notes | State |
| --- | --- | --- | --- | --- | --- |
| C10 | `Cells/BaseCollectionCell` + specialisations | one control per cell shape actually consumed by a test (e.g. `TwoLineCellControl`, `IconCellControl`) | Component | Do **not** try to model every cell up front - add on demand | - |
| C11 | `FilterSheet/*` | `FilterSheetControl<TParent>` | Container (modal sheet, root scoped to the modal window) | Confirm root-scope approach on the first module that uses it (Contacts, phase 3) before landing the control | - |
| C12 | `QuickActions/*` | `QuickActionsControl<TScope>` | Component | Row of quick-action buttons; likely paired with SwipeView | - |
| C13 | `DisplayFields/*` | one control per shape actually consumed (`LabeledDisplayFieldControl`, `MoneyDisplayFieldControl`, …) | Component | Same on-demand rule as C10 | - |

### P3 - later

| Id | Construction control | Brinell control | Notes | State |
| --- | --- | --- | --- | --- |
| C14 | `RichContent/*` | `RichContentControl<TScope>` | Read-only rich text renderer | - |
| C14b | `RichEditor/*` | `RichEditorControl<TScope>` | Interactive rich text editor; verify what's actually testable through UIA before shape-choosing | - |
| C15 | `Settings/*` | per shape used by Settings tests | Settings-only cells; add when Settings module lands | - |

---

## Additional form input controls (surface later)

`Exact.Core.Construction/Controls/FormInputs/` also contains `Boolean/`, `Number/`,
`RichText/` and `Date/` folders that Todos does not use. Add them as sibling controls of
T7-T10 the first time a module test needs them.

| Construction control | Brinell control | First likely consumer | State |
| --- | --- | --- | --- |
| `Boolean/*` | `BooleanFormInputControl<TScope>` | Contacts filter | - |
| `Number/*` | `NumberFormInputControl<TScope>` | Hours, Materials | - |
| `Date/*` | `DateFormInputControl<TScope>` | Planning | - |
| `RichText/*` | `RichTextFormInputControl<TScope>` | Journal | - |

---

## Cross-cutting notes for every control

- **AutomationId shape**: read the XAML. Construction consistently uses the outer
  `AutomationId` on the view (e.g. `TodoEditor_Name`) with **fixed inner ids** on the
  interactive parts (`SearchText`, `Search`, `Cancel`, `Toggle`, …). Constants live at the
  top of the `.tpl.cs`, exactly like `PageSearchControl.tpl.cs`.
- **No physical input.** `Click`/`SetText`/`SelectItem` only. If a control has no UIA route,
  admit the gesture bridge per AD-008 before falling back to it. Never expose raw pointer
  moves.
- **Constructor pair.** Every control takes `(IMauiScope<TParent> parent, Locator locator)`
  and `(IMauiScope<TParent> parent, string locatorValue = "<default>")`. See
  `PageSearchControl.tpl.cs` for the shape.
- **Generic parameter name.** `TParent` for containers whose parent is a page; `TScope` for
  components that can host inside any scope. Match the existing seed controls' use.
- **`.gen.cs` is generated, not written.** Do not edit `.gen.cs` by hand. Run the generator
  as the last step in every control loop.
- **Unit tests.** Add one focused test alongside the existing seed-control tests only for
  controls with non-trivial internal behaviour (T6 expand/collapse, T9 date+time split, C11
  modal admission). Pure passthroughs skip the unit test.

---

## Progress table

| Id | Control | Priority | State | Merged in phase |
| --- | --- | --- | --- | --- |
| S1 | PageSearchControl | seed | exists | 0 |
| S2 | InlineAlertControl | seed | exists | 0 |
| S3 | LoadingOverlayControl | seed | exists | 0 |
| S4 | PageTitleBarControl | seed | exists | 0 |
| S5 | PageHeaderActionsControl | seed | exists | 0 |
| S6 | EmptyStateControl | seed | exists | 0 |
| S7 | FavoriteToggleControl | seed | exists | 0 |
| S8 | PageableCollectionControl | seed | exists | 0 |
| P1 | OverviewPagePatternObject | seed | exists | 0 |
| P2 | DetailPagePatternObject | new | - | 3 (first module needing it) |
| P3 | EditPagePatternObject | new | - | 1 (= T4) |
| T4 | EditPagePatternObject | PoC | - | 1 |
| T5 | FormContainerControl | PoC | - | 1 |
| T6 | FormSectionControl | PoC | - | 1 |
| T7 | TextFormInputControl | PoC | - | 1 |
| T8 | NoteFormInputControl | PoC | - | 1 |
| T9 | DateTimeFormInputControl | PoC | - | 1 |
| T10 | SelectionFormInputControl | PoC | - | 1 |
| T11 | TodoListCollection + TodoItemContainer | PoC | - | 2 (page-object side) |
| C6 | RoundButtonControl | P1 | - | 3 |
| C7 | TabMenuItemControl | P1 | - | 3 |
| C8 | SearchOverlayControl | P1 | - | 3 |
| C9 | ToggleRowControl | P1 | - | 3 |
| C10 | Cells (on demand) | P2 | - | 4+ |
| C11 | FilterSheetControl | P2 | - | 4 |
| C12 | QuickActionsControl | P2 | - | 4 |
| C13 | DisplayFields (on demand) | P2 | - | 4+ |
| C14 | RichContentControl | P3 | - | 6 |
| C14b | RichEditorControl | P3 | - | 6 |
| C15 | Settings/* | P3 | - | 6 |
