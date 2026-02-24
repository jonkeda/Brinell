<!-- markdownlint-disable-file -->
# Release Changes: HTML/Blazor Async Migration

**Related Plan**: 01-html-async-migration-plan.instructions.md
**Implementation Date**: 2026-02-24

## Summary

Add async interfaces and implementations to the Brinell HTML/Playwright stack, enabling fully async test authoring while preserving the existing sync API unchanged.

## Changes

### Added

* srcnew/Brinell.Html/Interfaces/Async/IAsyncHtmlElement.cs — Async mirror of IHtmlElement + IElement, all members return Task/Task&lt;T&gt;
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncControlObject.cs — Async mirror of ControlBase public API (IsExists, Wait*, Assert*, GetText, GetAttribute)
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncClickable.cs — Async Click, SendKeys, Clear, ScrollIntoView, DoubleClick, RightClick, Hover
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncFocusable.cs — Async Focus, Blur, HasFocus
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncToggle.cs — Async IsChecked, SetChecked, WaitChecked, AssertChecked, Check, Uncheck, Toggle
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncEditable.cs — Async SetText, GetValue, TypeText, AssertValue, WaitValue, AppendText
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncSelector.cs — Async SelectByValue, SelectByText, GetSelectedValue, SelectMultiple
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncRange.cs — Async GetMin, GetMax, GetStep, GetValue, SetValue
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncScrollable.cs — Async ScrollTo, ScrollToTop
* srcnew/Brinell.Html/HtmlAsyncExtensions.cs — Extension methods bridge (~50 methods) for all async interfaces, providing *Async suffix API surface

### Modified

* srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs — Replaced 2 Thread.Sleep(100) with WaitHelper.Pause(100), added using Brinell.Core.Utilities; added async methods (NavigateToAsync, NavigateBackAsync, GoForwardAsync, RefreshAsync, TakeScreenshotAsync, SaveScreenshotAsync, ResetAppStateAsync, WaitReadyAsync)
* srcnew/Brinell.Html/ObjectBase.cs — Added PollAsync method with Task.Delay polling
* srcnew/Brinell.Html/Controls/ControlBase.cs — Added IHtmlAsyncControlObject&lt;TScope&gt; explicit impl, TryFindAsyncElement, FindAsyncElement, RunWithElementAsync, RunAssertAsync
* srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs — Added IAsyncHtmlElement explicit impl (29 async methods with native await)
* srcnew/Brinell.Html/Controls/Control.cs — Added IHtmlAsyncClickable&lt;TScope&gt; explicit impl (7 async methods: Click, SendKeys, Clear, ScrollIntoView, DoubleClick, RightClick, Hover)
* srcnew/Brinell.Html/Controls/FocusableControlBase.cs — Added IHtmlAsyncFocusable&lt;TScope&gt; explicit impl (Focus, Blur, HasFocus)
* srcnew/Brinell.Html/Controls/ToggleControlBase.cs — Added IHtmlAsyncToggle&lt;TScope&gt; explicit impl (IsChecked, SetChecked, WaitChecked, AssertChecked, Check, Uncheck, Toggle)
* srcnew/Brinell.Html/Controls/Text/TextInputControl.cs — Added IHtmlAsyncEditable&lt;TScope&gt; explicit impl (SetText, GetValue, TypeText, AssertValue, WaitValue, AppendText)
* srcnew/Brinell.Html/Controls/SelectorControlBase.cs — Added IHtmlAsyncSelector&lt;TScope&gt; explicit impl (SelectByValue, SelectByText, GetSelectedValue, SelectMultiple)
* srcnew/Brinell.Html/Controls/RangeControlBase.cs — Added IHtmlAsyncRange&lt;TScope&gt; explicit impl (GetMin, GetMax, GetStep, GetValue, SetValue)
* srcnew/Brinell.Html/Controls/ScrollableControlBase.cs — Added IHtmlAsyncScrollable&lt;TScope&gt; explicit impl (ScrollTo, ScrollToTop)
* srcnew/Brinell.Html/Controls/Buttons/ButtonControl.cs — Added SubmitAsync method
* srcnew/Brinell.Html/Controls/Buttons/LinkControl.cs — Added GetHrefAsync, AssertHrefAsync methods
* srcnew/Brinell.Html/Controls/Display/LabelControl.cs — Added IsTextContainingAsync, WaitTextContainingAsync, AssertTextContainingAsync methods
* srcnew/Brinell.Html/Controls/ContainerBase.cs — Added WaitReadyAsync method
* srcnew/Brinell.Html/Controls/Container/ScrollContainerControl.cs — Added ScrollToTopAsync method
* srcnew/Brinell.Html/Controls/Container/TabContainerControl.cs — Added SelectTabAsync(int), SelectTabAsync(string) methods
* srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs — Added WaitLoadedAsync, AssertLoadedAsync, WaitTitleAsync, AssertTitleAsync methods
* testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs — Added NavigateToPageAsync method
* testsnew/Brinell.Html.UITests/Tests/Controls/ButtonControlTests.cs — Added 3 async test methods (Button_Click_Async, IsVisible_Async, AssertEnabled_Async)
* testsnew/Brinell.Html.UITests/Tests/Pages/CounterPageTests.cs — Added 2 async test methods (MultipleIncrements_Async, ResetAfterIncrements_Async)
* testsnew/Brinell.Html.UITests/Tests/Controls/CheckBoxControlTests.cs — Added 3 async test methods
* testsnew/Brinell.Html.UITests/Tests/Controls/SelectControlTests.cs — Added 3 async test methods
* testsnew/Brinell.Html.UITests/Tests/Controls/TextInputControlTests.cs — Added 3 async test methods
* testsnew/Brinell.Html.UITests/Tests/Pages/LoginPageTests.cs — Added 2 async test methods
* testsnew/Brinell.Html.UITests/Tests/Scenarios/LoginFlowTests.cs — Added 2 async test methods

### Removed

## Additional or Deviating Changes

## Release Summary

**Total files affected**: 30 (10 new, 20 modified, 0 removed)

### Files Created

* srcnew/Brinell.Html/Interfaces/Async/IAsyncHtmlElement.cs — Async mirror of IHtmlElement + IElement
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncControlObject.cs — Base async control interface
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncClickable.cs — Async clickable interface
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncFocusable.cs — Async focusable interface
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncToggle.cs — Async toggle interface
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncEditable.cs — Async editable interface
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncSelector.cs — Async selector interface
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncRange.cs — Async range interface
* srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncScrollable.cs — Async scrollable interface
* srcnew/Brinell.Html/HtmlAsyncExtensions.cs — Extension method bridge (~50 methods)

### Files Modified (Source)

* srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs — Thread.Sleep removal + async navigation/screenshot methods
* srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs — IAsyncHtmlElement explicit impl (29 methods)
* srcnew/Brinell.Html/ObjectBase.cs — PollAsync method
* srcnew/Brinell.Html/Controls/ControlBase.cs — IHtmlAsyncControlObject impl + async helpers
* srcnew/Brinell.Html/Controls/Control.cs — IHtmlAsyncClickable impl
* srcnew/Brinell.Html/Controls/FocusableControlBase.cs — IHtmlAsyncFocusable impl
* srcnew/Brinell.Html/Controls/ToggleControlBase.cs — IHtmlAsyncToggle impl
* srcnew/Brinell.Html/Controls/Text/TextInputControl.cs — IHtmlAsyncEditable impl
* srcnew/Brinell.Html/Controls/SelectorControlBase.cs — IHtmlAsyncSelector impl
* srcnew/Brinell.Html/Controls/RangeControlBase.cs — IHtmlAsyncRange impl
* srcnew/Brinell.Html/Controls/ScrollableControlBase.cs — IHtmlAsyncScrollable impl
* srcnew/Brinell.Html/Controls/Buttons/ButtonControl.cs — SubmitAsync
* srcnew/Brinell.Html/Controls/Buttons/LinkControl.cs — GetHrefAsync, AssertHrefAsync
* srcnew/Brinell.Html/Controls/Display/LabelControl.cs — Text-containing async methods
* srcnew/Brinell.Html/Controls/ContainerBase.cs — WaitReadyAsync
* srcnew/Brinell.Html/Controls/Container/ScrollContainerControl.cs — ScrollToTopAsync
* srcnew/Brinell.Html/Controls/Container/TabContainerControl.cs — SelectTabAsync
* srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs — WaitLoadedAsync, AssertLoadedAsync, WaitTitleAsync, AssertTitleAsync

### Files Modified (Tests)

* testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs — NavigateToPageAsync
* testsnew/Brinell.Html.UITests/Tests/Controls/ButtonControlTests.cs — 3 async tests
* testsnew/Brinell.Html.UITests/Tests/Pages/CounterPageTests.cs — 2 async tests
* testsnew/Brinell.Html.UITests/Tests/Controls/CheckBoxControlTests.cs — 3 async tests
* testsnew/Brinell.Html.UITests/Tests/Controls/SelectControlTests.cs — 3 async tests
* testsnew/Brinell.Html.UITests/Tests/Controls/TextInputControlTests.cs — 3 async tests
* testsnew/Brinell.Html.UITests/Tests/Pages/LoginPageTests.cs — 2 async tests
* testsnew/Brinell.Html.UITests/Tests/Scenarios/LoginFlowTests.cs — 2 async tests

### Validation

* `dotnet build srcnew/Brinell.sln` — 0 errors, 0 warnings
* Zero `Thread.Sleep` in Brinell.Html, Brinell.Html.Playwright, and Brinell.Html.UITests
* All existing sync tests unchanged
* No dependency or infrastructure changes
* No breaking changes to existing public API
