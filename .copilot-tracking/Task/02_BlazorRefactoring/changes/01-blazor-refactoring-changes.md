<!-- markdownlint-disable-file -->
# Release Changes: Blazor Refactoring to srcnew/testsnew

**Related Plan**: 01-blazor-refactoring-plan.instructions.md
**Implementation Date**: 2026-02-23

## Summary

Migrate Blazor controls and tests from old `src/Brinell.Blazor/ControlObject6/` and `tests/Brinell.Blazor.Tests.ControlObject6/` to new `srcnew/Brinell.Blazor/` and `testsnew/Brinell.Blazor.Tests/` architecture, inheriting from `srcnew/Brinell.Html` base classes with the CRTP `<TScope>` pattern.

## Changes

### Added

### Modified

* `srcnew/Brinell.Html/Interfaces/IHtmlElement.cs` — Added `Evaluate<T>(string)` and `Evaluate(string)` methods
* `srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs` — Implemented `Evaluate<T>()` and `Evaluate()` via `_locator.EvaluateAsync` sync bridge
* `srcnew/Brinell.Blazor/Brinell.Blazor.csproj` — Replaced `Brinell.Core` reference with `Brinell.Html` + `Brinell.Html.Playwright`
* `testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj` — Added `Brinell.Html` + `Brinell.Html.Playwright` project references
* `srcnew/Brinell.Html.Playwright/Brinell.Html.Playwright.csproj` — Added `InternalsVisibleTo` for `Brinell.Blazor` to expose `InternalPage`

### Added

* `srcnew/Brinell.Blazor/Context/BlazorTestContext.cs` — Composition wrapper around `PlaywrightTestContext` with `WaitForBlazorReady` extension
* `srcnew/Brinell.Blazor/Pages/BlazorPageObjectBase.cs` — Thin CRTP base inheriting `HtmlPageObjectBase<TSelf>`
* `srcnew/Brinell.Blazor/Testing/BlazorTestFixtureBase.cs` — Fixture base overriding `CreateContextAsync` to create `BlazorTestContext`
* `srcnew/Brinell.Blazor/Controls/ButtonControl.cs` — Thin inheritor of `Html.Controls.Buttons.ButtonControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/LinkControl.cs` — Thin inheritor of `Html.Controls.Buttons.LinkControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/CheckBoxControl.cs` — Thin inheritor of `Html.Controls.Toggle.CheckBoxControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/RadioButtonControl.cs` — Thin inheritor of `Html.Controls.Toggle.RadioButtonControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TextInputControl.cs` — Thin inheritor of `Html.Controls.Text.TextInputControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TextAreaControl.cs` — Thin inheritor of `Html.Controls.Text.TextAreaControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/SelectControl.cs` — Thin inheritor of `Html.Controls.Selection.SelectControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/DateInputControl.cs` — Thin inheritor of `Html.Controls.DateTime.DateInputControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TimeInputControl.cs` — Thin inheritor of `Html.Controls.DateTime.TimeInputControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/RangeInputControl.cs` — Thin inheritor of `Html.Controls.Range.RangeInputControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/ListControl.cs` — Thin inheritor of `Html.Controls.Collection.ListControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TableControl.cs` — Thin inheritor of `Html.Controls.Collection.TableControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/ProgressControl.cs` — Thin inheritor of `Html.Controls.Display.ProgressControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TabContainerControl.cs` — Dual-generic inheritor of `Html.Controls.Container.TabContainerControl<TParent, TScope>`
* `srcnew/Brinell.Blazor/Controls/MediaControlBase.cs` — Abstract base for Audio/Video with 15+ media methods (Play, Pause, Seek, Volume, Mute, assertions)
* `srcnew/Brinell.Blazor/Controls/AudioControl.cs` — Thin inheritor of `MediaControlBase<TScope>`
* `srcnew/Brinell.Blazor/Controls/VideoControl.cs` — `MediaControlBase<TScope>` + `GetPoster()` method
* `srcnew/Brinell.Blazor/Controls/ImageControl.cs` — Image-specific control with load detection, natural dimensions, source/alt assertions
* `srcnew/Brinell.Blazor/Controls/IFrameControl.cs` — Cross-frame interaction via JS Evaluate into contentDocument
* `srcnew/Brinell.Blazor/Controls/NavMenuControl.cs` — Navigation menu with FindElements-based item discovery and NavigateTo
* `testsnew/Brinell.Blazor.Tests/Mocks/MockHtmlFactory.cs` — Central mock factory for `IHtmlTestContext` and `IHtmlElement` mocking
* `testsnew/Brinell.Blazor.Tests/Controls/ButtonControlTests.cs` — 8 tests for ButtonControl (Click, IsExists, IsVisible, IsEnabled, GetText)
* `testsnew/Brinell.Blazor.Tests/Controls/CheckBoxControlTests.cs` — 9 tests for CheckBoxControl (IsChecked, Check, Uncheck, Toggle, Click)
* `testsnew/Brinell.Blazor.Tests/Controls/LinkControlTests.cs` — 6 tests for LinkControl (Click, GetText, Href, IsExists, IsVisible, IsEnabled)
* `testsnew/Brinell.Blazor.Tests/Controls/RadioButtonControlTests.cs` — 7 tests for RadioButtonControl (IsChecked, Select, Click)
* `testsnew/Brinell.Blazor.Tests/Controls/DateInputControlTests.cs` — 6 tests for DateInputControl (GetDate, SetDate, GetMin, GetMax)
* `testsnew/Brinell.Blazor.Tests/Controls/TimeInputControlTests.cs` — 6 tests for TimeInputControl (GetTime, SetTime, GetMin, GetMax)
* `testsnew/Brinell.Blazor.Tests/Controls/TextInputControlTests.cs` — 7 tests for TextInputControl (SetText, GetValue, TypeText, Focus)
* `testsnew/Brinell.Blazor.Tests/Controls/TextAreaControlTests.cs` — 6 tests for TextAreaControl (SetText, GetValue, AppendText, Clear)
* `testsnew/Brinell.Blazor.Tests/Controls/ProgressControlTests.cs` — 6 tests for ProgressControl (GetValue, GetMax, GetPercentage)

### Modified

* `testsnew/Brinell.Blazor.Tests/GlobalUsings.cs` — Activated commented usings, added `Brinell.Html.Interfaces`, `Brinell.Html.Pages`, `Brinell.Blazor.Tests.Mocks`
* `testsnew/Brinell.Blazor.Tests/Controls/ListControlTests.cs` — 7 tests for ListControl (ItemCount, GetItemText, GetItemTexts, IsExists)
* `testsnew/Brinell.Blazor.Tests/Controls/TableControlTests.cs` — 9 tests for TableControl (RowCount, ColumnCount, GetCellText, GetHeaderText, GetRowTexts, IsExists)
* `testsnew/Brinell.Blazor.Tests/Controls/SelectControlTests.cs` — 7 tests for SelectControl (SelectByValue, GetSelectedValue, SelectMultiple, SelectByText, IsExists, Click)
* `testsnew/Brinell.Blazor.Tests/Controls/RangeInputControlTests.cs` — 10 tests for RangeInputControl (GetNumericValue, SetNumericValue, GetMin, GetMax, GetStep, GetValue, SetValue, IsExists)
* `testsnew/Brinell.Blazor.Tests/Controls/TabContainerControlTests.cs` — 7 tests for TabContainerControl (TabCount, SelectTab by index/text, out-of-range handling)
* `testsnew/Brinell.Blazor.Tests/Controls/AudioControlTests.cs` — 16 tests for AudioControl (Play, Pause, IsPaused, IsPlaying, IsEnded, GetCurrentTime, Seek, GetDuration, GetVolume, SetVolume, IsMuted, Mute, Unmute, GetSource, IsExists)
* `testsnew/Brinell.Blazor.Tests/Controls/VideoControlTests.cs` — 10 tests for VideoControl (Play, Pause, IsPaused, GetCurrentTime, Seek, GetSource, GetPoster, IsPlaying, GetVolume, IsExists)
* `testsnew/Brinell.Blazor.Tests/Controls/ImageControlTests.cs` — 12 tests for ImageControl (GetSource, GetAltText, IsLoaded, GetNaturalWidth/Height, AssertSource, AssertSourceContains, AssertAltText)
* `testsnew/Brinell.Blazor.Tests/Controls/IFrameControlTests.cs` — 12 tests for IFrameControl (GetSource, GetTitle, GetName, ClickInside, FillInside, GetTextInside, ElementExistsInside, AssertSource)
* `testsnew/Brinell.Blazor.Tests/Controls/NavMenuControlTests.cs` — 14 tests for NavMenuControl (GetItemCount, GetItems, NavigateTo, NavigateToIndex, HasItem, GetItemHref, AssertItemCount, AssertHasItem)
* `testsnew/Brinell.Blazor.Tests/Context/BlazorTestContextTests.cs` — 7 tests for BlazorTestContext (ForPage, CurrentUrl, PageTitle, Timeouts, Context self-ref, NavigateTo, DefaultLocatorStrategy)

### Removed

* `srcnew/Brinell.Blazor/Context/Placeholder.cs` — Deleted placeholder file
* `srcnew/Brinell.Blazor/Controls/Placeholder.cs` — Deleted placeholder file
* `srcnew/Brinell.Blazor/Pages/Placeholder.cs` — Deleted placeholder file
* `srcnew/Brinell.Blazor/Testing/Placeholder.cs` — Deleted placeholder file

## Additional or Deviating Changes

## Release Summary

**Total files affected:** 49 (5 modified, 40 added, 4 removed)

### Files Created (40)

**Infrastructure (3):**
* `srcnew/Brinell.Blazor/Context/BlazorTestContext.cs` — Composition wrapper around PlaywrightTestContext
* `srcnew/Brinell.Blazor/Pages/BlazorPageObjectBase.cs` — Thin CRTP base page
* `srcnew/Brinell.Blazor/Testing/BlazorTestFixtureBase.cs` — Test fixture base

**Inherited Controls (14):**
* `srcnew/Brinell.Blazor/Controls/ButtonControl.cs`
* `srcnew/Brinell.Blazor/Controls/LinkControl.cs`
* `srcnew/Brinell.Blazor/Controls/CheckBoxControl.cs`
* `srcnew/Brinell.Blazor/Controls/RadioButtonControl.cs`
* `srcnew/Brinell.Blazor/Controls/TextInputControl.cs`
* `srcnew/Brinell.Blazor/Controls/TextAreaControl.cs`
* `srcnew/Brinell.Blazor/Controls/SelectControl.cs`
* `srcnew/Brinell.Blazor/Controls/DateInputControl.cs`
* `srcnew/Brinell.Blazor/Controls/TimeInputControl.cs`
* `srcnew/Brinell.Blazor/Controls/RangeInputControl.cs`
* `srcnew/Brinell.Blazor/Controls/ListControl.cs`
* `srcnew/Brinell.Blazor/Controls/TableControl.cs`
* `srcnew/Brinell.Blazor/Controls/ProgressControl.cs`
* `srcnew/Brinell.Blazor/Controls/TabContainerControl.cs`

**Blazor-Only Controls (6):**
* `srcnew/Brinell.Blazor/Controls/MediaControlBase.cs` — Abstract base for Audio/Video (15+ media methods)
* `srcnew/Brinell.Blazor/Controls/AudioControl.cs`
* `srcnew/Brinell.Blazor/Controls/VideoControl.cs`
* `srcnew/Brinell.Blazor/Controls/ImageControl.cs`
* `srcnew/Brinell.Blazor/Controls/IFrameControl.cs`
* `srcnew/Brinell.Blazor/Controls/NavMenuControl.cs`

**Test Infrastructure (1):**
* `testsnew/Brinell.Blazor.Tests/Mocks/MockHtmlFactory.cs`

**Test Files (16):**
* `testsnew/Brinell.Blazor.Tests/Controls/ButtonControlTests.cs` (8 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/CheckBoxControlTests.cs` (9 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/LinkControlTests.cs` (6 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/RadioButtonControlTests.cs` (7 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/DateInputControlTests.cs` (6 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/TimeInputControlTests.cs` (6 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/TextInputControlTests.cs` (7 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/TextAreaControlTests.cs` (6 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/ProgressControlTests.cs` (6 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/ListControlTests.cs` (7 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/TableControlTests.cs` (9 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/SelectControlTests.cs` (7 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/RangeInputControlTests.cs` (10 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/TabContainerControlTests.cs` (7 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/AudioControlTests.cs` (16 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/VideoControlTests.cs` (10 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/ImageControlTests.cs` (12 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/IFrameControlTests.cs` (12 tests)
* `testsnew/Brinell.Blazor.Tests/Controls/NavMenuControlTests.cs` (14 tests)
* `testsnew/Brinell.Blazor.Tests/Context/BlazorTestContextTests.cs` (7 tests)

### Files Modified (5)
* `srcnew/Brinell.Html/Interfaces/IHtmlElement.cs` — Added Evaluate<T>() and Evaluate()
* `srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs` — Implemented Evaluate methods
* `srcnew/Brinell.Blazor/Brinell.Blazor.csproj` — Updated project references
* `testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj` — Updated project references
* `srcnew/Brinell.Html.Playwright/Brinell.Html.Playwright.csproj` — Added InternalsVisibleTo
* `testsnew/Brinell.Blazor.Tests/GlobalUsings.cs` — Activated and added global usings

### Files Removed (4)
* `srcnew/Brinell.Blazor/Context/Placeholder.cs`
* `srcnew/Brinell.Blazor/Controls/Placeholder.cs`
* `srcnew/Brinell.Blazor/Pages/Placeholder.cs`
* `srcnew/Brinell.Blazor/Testing/Placeholder.cs`

### Validation
* Solution build: **0 errors, 0 warnings**
* Tests: **172 passed, 0 failed, 0 skipped**
* No blocking issues
