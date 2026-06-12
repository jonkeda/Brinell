# SPEC-025: MAUI Control UI Tests

**Status:** Draft | **Priority:** Medium | **Depends on:** SPEC-024 (MAUI Control Objects)

## Overview

Comprehensive UI tests for all 24 MAUI control objects. Tests validate control functionality against `Brinell.Samples.Maui.App` using Appium automation along with FlaUI on Windows.

## Scope

- UI integration tests for all 24 MAUI control objects (one test class per control)
- Page objects for sample app pages (UserFormPage, MediaGalleryPage, etc.)
- Control factory methods on `MauiPageObjectBase`
- Sample app updates to add missing control demonstrations

**Out of scope:** Unit tests (separate project), Blazor/WPF/WinForms tests, visual regression, mobile-specific tests.

## Test Organization

### By Category

| Category | Controls | Test Files | Sample App Page |
|----------|----------|------------|-----------------|
| Display | Label, ProgressBar, ActivityIndicator, Image | 4 | Basics, Media |
| Toggle | CheckBox, Switch, RadioButton | 3 | Forms |
| Text | Editor, SearchBar | 2 | Forms |
| Selection | Picker | 1 | Forms |
| Range | Slider, Stepper | 2 | Forms |
| DateTime | DatePicker, TimePicker | 2 | Forms |
| Container | ScrollView, Expander, RefreshView, SwipeView | 4 | Containers, Toolkit, Gestures |
| Collection | ListView, CollectionView | 2 | Lists |
| Navigation | Menu, Toolbar | 2 | Navigation |
| Media | WebView, MediaElement | 2 | Media |
| Buttons | ImageButton, Link | 2 | New ControlShowcasePage |

### Test Pattern

Each test class follows the existing pattern from `ButtonControlTests.cs`:

```csharp
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "ControlName")]
public class ControlNameControlTests
{
    private readonly AppiumFixture _fixture;
    private PageName Page => _fixture.PageProperty;

    // State tests: IsExists, IsVisible, IsEnabled
    // Interaction tests: control-specific methods
    // Assertion tests: Assert* methods
    // Fluent chaining tests: demonstrate TScope pattern
}
```

### Test Priorities

| Priority | Controls | Rationale |
|----------|----------|-----------|
| P1 (Core) | Label, ProgressBar, CheckBox, Switch, RadioButton, Editor, SearchBar, Slider, Stepper | Most common controls |
| P2 (Selection) | Picker, DatePicker, TimePicker | Selection/DateTime inputs |
| P3 (Container) | ScrollView, Expander, ListView, CollectionView | Container/collection patterns |
| P4 (Specialized) | WebView, MediaElement, ImageButton, Link, Menu, Toolbar | Less common controls |

## Infrastructure Requirements

### New Page Objects

- **UserFormPage** — Exposes: Editor, SearchBar, Switch, CheckBox, RadioButton, Picker, DatePicker, TimePicker, Slider, Stepper
- **MediaGalleryPage** — Exposes: Image, ActivityIndicator, WebView, CollectionView
- **ControlShowcasePage** (new) — Exposes: Expander, SwipeView, RefreshView, ImageButton, Link

### Control Factory Methods

Add to `MauiPageObjectBase`: `Editor()`, `SearchBar()`, `Switch()`, `CheckBox()`, `RadioButton()`, `Picker()`, `DatePicker()`, `TimePicker()`, `Slider()`, `Stepper()`, `Image()`, `ActivityIndicator()`, `ProgressBar()`, `WebView()`, `ScrollView()`

### AppiumFixture Updates

Add new page objects and navigation methods: `NavigateToUserForm()`, `NavigateToMediaGallery()`, `NavigateToControlShowcase()`

## File Structure

```
testsnew/Brinell.Maui.UITests/
├── Tests/
│   ├── Display/        (4 test files)
│   ├── Toggle/         (3 test files)
│   ├── Text/           (2 test files)
│   ├── Selection/      (1 test file)
│   ├── Range/          (2 test files)
│   ├── DateTime/       (2 test files)
│   ├── Container/      (4 test files)
│   ├── Collection/     (2 test files)
│   ├── Navigation/     (2 test files)
│   ├── Media/          (2 test files)
│   └── Buttons/        (2 test files)
└── Pages/
    ├── UserFormPage.cs (new)
    ├── MediaGalleryPage.cs (new)
    └── ControlShowcasePage.cs (new, if needed)
```

## Sample App Gaps

Controls needing sample app additions:

| Control | Current Support | Action Needed |
|---------|----------------|---------------|
| Expander | Not in app | Add to Toolkit tab |
| SwipeView | Not in app | Add to Gestures tab |
| RefreshView | Not in app | Add to Gestures tab |
| ImageButton | Not in app | Add to ControlShowcasePage |
| Link/Hyperlink | Not in app | Add to ControlShowcasePage |

## Task Status

**0/33 tasks complete** — all pending.

| Phase | Tasks | Status |
|-------|-------|--------|
| 1. Infrastructure (factories, pages, fixture) | 4 | 🔲 Pending |
| 2. P1 Core Control Tests | 9 | 🔲 Pending |
| 3. P2 Selection & DateTime Tests | 3 | 🔲 Pending |
| 4. P3 Container & Collection Tests | 6 | 🔲 Pending |
| 5. P4 Specialized Tests | 6 | 🔲 Pending |
| 6. Sample App Updates | 3 | 🔲 Pending |
| 7. Verification (build + smoke test) | 2 | 🔲 Pending |

## Related

- SPEC-024: MAUI Control Objects (prerequisite — completed)
- SPEC-026: UI Test Fixes (fixes may affect test reliability)
- SPEC-029: FlaUI Windows Driver Fixes (affects Windows test execution)
