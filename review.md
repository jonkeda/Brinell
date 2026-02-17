# Brinell.Maui Review

## 1. Missing Control Objects

### Currently Implemented

| Category | Controls |
|---|---|
| **Buttons** | `Button`, `ImageButton`, `Link` |
| **Text** | `Entry`, `Editor`, `SearchBar` |
| **Toggle** | `Switch`, `CheckBox`, `RadioButton` |
| **Display** | `Label`, `Image`, `ActivityIndicator`, `ProgressBar` |
| **Range** | `Slider`, `Stepper` |
| **Selection** | `Picker` |
| **DateTime** | `DatePicker`, `TimePicker` |
| **Container** | `Grid`, `ScrollView`, `Expander`, `RefreshView`, `SwipeView` |
| **Collection** | `ListView`, `CollectionView`, `CarouselView`, `TableView`, `PaginatedList` |
| **Navigation** | `Menu`, `Toolbar`, `Tab`, `FlyoutItem` |
| **Media** | `WebView`, `MediaElement` |

This is a solid and comprehensive set. The framework covers all high-frequency MAUI controls. Below are controls that don't have a dedicated control object yet but are commonly used in MAUI apps:

### Missing (Worth Adding)

| Control | Priority | Rationale |
|---|---|---|
| `Span` (inside `FormattedString`) | Low | Useful for verifying rich/formatted text in Labels, but hard to target via automation. |
| `Border` | Low | Container like `Frame`, but `Frame` is deprecated in favor of `Border` in newer MAUI. Could reuse `Grid` approach or add a `Border` container wrapper. |
| `IndicatorView` | Low | Paired with `CarouselView` - tests might want to assert the current dot/indicator position. |
| `ContentView` | Low | Generic wrapper, useful as a scoped container for compound controls. Already partly handled by `ContainerBase`. |

### Not Missing (Correctly Omitted)

These MAUI controls don't need dedicated control objects:

- **Layout controls** (`StackLayout`, `FlexLayout`, `AbsoluteLayout`, `HorizontalStackLayout`, `VerticalStackLayout`) - Layout containers, not interactive. `Grid` is the only one that makes sense as a named scope.
- **`Frame`** - Deprecated in favor of `Border`. No special interaction needed beyond `ContainerBase`.
- **`BoxView`** - Visual-only, no interaction. `ControlBase` suffices.
- **Shapes** (`Line`, `Ellipse`, `Rectangle`, `Polygon`, `Polyline`) - Drawing primitives, not typically automated.
- **`Map`** - Highly specialized, rarely tested via UI automation.
- **`BlazorWebView`** - Would use Playwright/Blazor test stack, not Appium.

**Verdict:** The control coverage is very good. No critical controls are missing.

---

## 2. Improvement Areas

### 2.1 ~~Editor Uses `new` Keyword to Hide Base Methods~~ (Fixed)

`Editor<TScope>` used `new` on `Clear`, `SetText`, `ClearCore`, and `SetTextCore` to shadow `Entry<TScope>` base methods.

**Fixed:** `ClearCore` and `SetTextCore` in `Entry` are now `virtual`. `Editor` uses `override` instead of `new`. The redundant `Clear` and `SetText` `new` methods were removed from `Editor` since the base `Entry` methods now correctly dispatch to the overridden core methods via polymorphism.

### 2.2 ~~Empty `catch` Blocks in DatePicker and TimePicker~~ (Fixed)

Both `DatePicker.GetDateCore` and `TimePicker.GetTimeCore` had bare `catch` blocks wrapping both `ByAutomationId` and `ByXPath` child element searches.

**Fixed:** The `ByAutomationId` search was moved outside the try/catch (it doesn't need protection). The catch was narrowed to `WebDriverException` and only wraps the XPath fallback, which is the only call that may not be supported by all drivers.

### 2.3 ~~Empty `catch` Blocks in ContainerBase~~ (Fixed)

`ContainerBase` had three bare `catch` blocks: in `TryFindElement` (second-try after stale), `FindElements` (second-try after stale), and `TryGetContainerRoot`.

**Fixed:** The catch-all in `TryFindElement` was removed entirely (the `ElementNotFoundException` catch already handles the expected case; anything else should bubble up). The catch-all in `FindElements` was removed (second-try failures should propagate). `TryGetContainerRoot` was narrowed to `catch (ElementNotFoundException)`.

### 2.4 ~~Poll Method Uses Bare `catch`~~ (Fixed)

Both `ObjectBase.Poll` and `ControlBase.Poll` swallowed all exceptions during polling, including on the final check after timeout.

**Fixed:** The catch in the poll loop body was kept (polling genuinely expects transient failures like stale elements) and the comment was updated to explain the intent. The catch on the **final check** was removed so that if the condition is fundamentally broken after the timeout, the real exception propagates to the caller.

### 2.5 ~~ContainerBase Has Fewer Factory Methods Than PageObjectBase~~ (Fixed)

`ContainerBase` only had factory methods for `Label`, `CheckBox`, `Button`, and `Entry`.

**Fixed:** Added factory methods for all remaining control types: `Image`, `ProgressBar`, `ActivityIndicator`, `Switch`, `RadioButton`, `Editor`, `SearchBar`, `Picker`, `Slider`, `Stepper`, `DatePicker`, and `TimePicker`. Both `Locator` and `string` overloads are provided for each, matching the `PageObjectBase` pattern.

### 2.6 ~~CollectionView.Basic and CarouselView.Basic Are Plain Wrappers~~ (Fixed)

The single-type-parameter versions (`CollectionView<TScope>`, `CarouselView<TScope>`) extended `ControlBase` directly and lacked scroll capability.

**Fixed:** Both now extend `ScrollableControlBase<TScope>`, giving them `ScrollToTop`, `ScrollToEnd`, and `ScrollBy` methods out of the box.

### 2.7 ~~Grid Has No Container Scoping~~ (Fixed)

`Grid<TScope>` extended `ControlBase<TScope>` and couldn't be used as a scope for child elements.

**Fixed:** Added a new `Grid<TParent, TSelf>` variant that extends `ContainerBase<TParent, TSelf>`, providing full container scoping with factory methods for child controls. The simple `Grid<TScope>` is preserved for existence/visibility checks where scoping isn't needed.

### 2.8 ~~Toolbar and Menu Button Scope~~ (Fixed)

`Menu.ClickMenuItem` and `Toolbar.ClickToolbarItem` created a `Button<TScope>` with `MauiScope`, searching from the page root instead of within the menu/toolbar element.

**Fixed:** Both methods now use `RunWithElement` to find the menu/toolbar element first, then search for the child item within it using `element.FindElement`. This ensures items are scoped within their parent control.

---

## Summary

| Area | Severity | Action |
|---|---|---|
| Missing controls | None critical | `Border`, `IndicatorView` are nice-to-have |
| ~~Editor uses `new` instead of `override`~~ | ~~Medium~~ | Fixed: `ClearCore`/`SetTextCore` now `virtual` + `override` |
| ~~Empty `catch` blocks in DateTime controls~~ | ~~Low-Medium~~ | Fixed: narrowed to `WebDriverException`, moved `ByAutomationId` outside try/catch |
| ~~Empty `catch` blocks in ContainerBase~~ | ~~Low-Medium~~ | Fixed: removed catch-alls, narrowed `TryGetContainerRoot` to `ElementNotFoundException` |
| ~~Poll method bare `catch`~~ | ~~Low~~ | Fixed: final check now propagates exceptions, loop catch documented |
| ~~ContainerBase missing factory methods~~ | ~~Medium~~ | Fixed: added factory methods for all control types |
| ~~Basic collection variants not scrollable~~ | ~~Low~~ | Fixed: now extend `ScrollableControlBase` |
| ~~Grid not a container~~ | ~~Low~~ | Fixed: added `Grid<TParent, TSelf>` extending `ContainerBase` |
| ~~Menu/Toolbar scope issue~~ | ~~Low~~ | Fixed: items now searched within parent element |

All review items have been addressed. The Brinell.Maui framework is well-structured with clean separation between base classes and concrete controls, consistent use of the Is/Wait/Assert pattern, proper fluent chaining, and comprehensive control coverage.
