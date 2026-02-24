---
title: "HTML/Playwright Stack Async Migration Inventory"
description: "Complete inventory of files, classes, interfaces, and methods requiring async counterparts"
ms.date: 2026-02-24
ms.topic: reference
---

## Scope

Three projects examined:

- `srcnew/Brinell.Html/` (abstraction layer)
- `srcnew/Brinell.Html.Playwright/` (Playwright implementation)
- `testsnew/Brinell.Html.UITests/` (UI test suite)

## 1. Interfaces in Brinell.Html/Interfaces/

Six interfaces total. None define async methods today; all are sync-only.

### 1.1 IHtmlElement (extends IElement&lt;IHtmlElement&gt;)

File: `srcnew/Brinell.Html/Interfaces/IHtmlElement.cs`

| Member | Signature |
|--------|-----------|
| Property | `string InnerHtml { get; }` |
| Property | `string OuterHtml { get; }` |
| Property | `bool IsChecked { get; }` |
| Property | `string InputValue { get; }` |
| Method | `string? GetDomAttribute(string attributeName)` |
| Method | `string? GetDomProperty(string propertyName)` |
| Method | `string? GetCssValue(string propertyName)` |
| Method | `void Submit()` |
| Method | `void Fill(string value)` |
| Method | `void SelectOption(string value)` |
| Method | `void SelectOption(string[] values)` |
| Method | `void Check()` |
| Method | `void Uncheck()` |
| Method | `void Focus()` |
| Method | `void Blur()` |
| Method | `T? Evaluate<T>(string expression)` |
| Method | `void Evaluate(string expression)` |

Inherits from `IElement<IHtmlElement>` which adds:

| Member | Signature |
|--------|-----------|
| Property | `bool Visible { get; }` |
| Property | `bool Enabled { get; }` |
| Property | `bool Selected { get; }` |
| Property | `string? Text { get; }` |
| Property | `string? TagName { get; }` |
| Property | `Point Location { get; }` |
| Property | `Size Size { get; }` |
| Property | `Rectangle Rect { get; }` |
| Method | `void Click()` |
| Method | `void SendKeys(string text, TextInputMethod method = Keys)` |
| Method | `void Clear()` |
| Method | `void DoubleClick()` |
| Method | `void RightClick()` |
| Method | `void Hover()` |
| Method | `void LongPress(int durationMs = 1000)` |
| Method | `void ScrollIntoView(int timeoutMs = 5000)` |
| Method | `void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)` |
| Method | `string? GetAttribute(string name)` |
| Method | `TSelf FindElement(Locator locator, int timeoutMs = 5000)` |
| Method | `IReadOnlyList<TSelf> FindElements(Locator locator, int timeoutMs = 0)` |
| Method | `bool TryFindElement(Locator locator, out TSelf? element, int timeoutMs = 0)` |

### 1.2 IHtmlElementScope (extends IElementScope&lt;IHtmlElement&gt;)

File: `srcnew/Brinell.Html/Interfaces/IHtmlElementScope.cs`

| Member | Signature |
|--------|-----------|
| Property | `IHtmlTestContext Context { get; }` |

Inherits from `IElementScope<IHtmlElement>`:

| Member | Signature |
|--------|-----------|
| Method | `IHtmlElement? TryFindElement(Locator locator)` |
| Method | `IHtmlElement FindElement(Locator locator)` |
| Method | `IReadOnlyList<IHtmlElement> FindElements(Locator locator)` |

### 1.3 IHtmlScope&lt;TScope&gt; (extends IHtmlElementScope)

File: `srcnew/Brinell.Html/Interfaces/IHtmlScope.cs`

| Member | Signature |
|--------|-----------|
| Property | `TScope Self { get; }` |

### 1.4 IHtmlPage&lt;TSelf&gt; (extends IHtmlScope&lt;TSelf&gt;, IPageObject&lt;IHtmlElement&gt;)

File: `srcnew/Brinell.Html/Interfaces/IHtmlPage.cs`

Empty body. Unifies `IHtmlScope<TSelf>` and `IPageObject<IHtmlElement>`.

### 1.5 IHtmlContainer&lt;TParent, TSelf&gt; (extends IHtmlScope&lt;TSelf&gt;, IContainerControl&lt;IHtmlElement&gt;)

File: `srcnew/Brinell.Html/Interfaces/IHtmlContainer.cs`

| Member | Signature |
|--------|-----------|
| Property | `TParent Parent { get; }` |

### 1.6 IHtmlTestContext (extends ITestContext&lt;IHtmlElement&gt;, IHtmlElementScope)

File: `srcnew/Brinell.Html/Interfaces/IHtmlTestContext.cs`

| Member | Signature |
|--------|-----------|
| Property | `IHtmlTestContext Context { get; }` (new) |
| Property | `string CurrentUrl { get; }` |
| Property | `string PageTitle { get; }` |
| Method | `void GoForward()` |

Inherits from `ITestContext` (via `ITestContext<IHtmlElement>`):

| Member | Signature |
|--------|-----------|
| Property | `TimeoutSettings Timeouts { get; }` |
| Property | `ITestLogger Logger { get; }` |
| Method | `void NavigateTo(string destination)` |
| Method | `void NavigateBack()` |
| Method | `void Refresh()` |
| Method | `byte[] TakeScreenshot()` |
| Method | `void SaveScreenshot(string path)` |
| Method | `void ResetAppState()` |

## 2. ObjectBase

File: `srcnew/Brinell.Html/ObjectBase.cs`

| Member | Kind | Signature | Async impact |
|--------|------|-----------|-------------|
| `Context` | abstract property | `abstract IHtmlTestContext Context { get; }` | No change needed |
| `DefaultTimeoutMs` | property | `int DefaultTimeoutMs => Context.Timeouts.DefaultWait` | No change needed |
| `PollingIntervalMs` | property | `int PollingIntervalMs => Context.Timeouts.PollingInterval` | No change needed |
| `Poll` | method | `bool Poll(Func<bool> condition, int timeoutMs)` | Needs `PollAsync(Func<Task<bool>>, int)` counterpart |

Note: `Poll` calls `WaitHelper.Pause(PollingIntervalMs)` internally. An async version would use `Task.Delay` instead.

## 3. Control classes in Brinell.Html/Controls/

### 3.1 Class hierarchy

```text
ObjectBase
├── ControlBase<TScope>                    (IControlObject<TScope>)
│   ├── Control<TScope>
│   │   ├── ClickableControlBase<TScope>
│   │   │   ├── FocusableControlBase<TScope>
│   │   │   │   ├── SelectorControlBase<TScope>  (abstract)
│   │   │   │   │   ├── SelectControl<TScope>
│   │   │   │   │   └── RadioGroupControl<TScope>
│   │   │   │   ├── RangeControlBase<TScope>
│   │   │   │   │   ├── DateInputControl<TScope>
│   │   │   │   │   ├── TimeInputControl<TScope>
│   │   │   │   │   └── RangeInputControl<TScope>
│   │   │   │   └── TextInputControl<TScope>
│   │   │   │       └── TextAreaControl<TScope>
│   │   │   ├── ToggleControlBase<TScope>
│   │   │   │   ├── CheckBoxControl<TScope>
│   │   │   │   └── RadioButtonControl<TScope>
│   │   │   └── ScrollableControlBase<TScope>
│   │   └── ButtonControl<TScope>          (Buttons/)
│   │       └── LinkControl<TScope>        (Buttons/)
│   ├── LabelControl<TScope>              (Display/)
│   ├── ProgressControl<TScope>           (Display/)
│   ├── ListControl<TScope>              (Collection/)
│   ├── TableControl<TScope>             (Collection/)
│   └── List<TScope>
└── ContainerBase<TParent, TScope>        (IHtmlContainer<TParent, TScope>)
    ├── ScrollContainerControl<TParent, TScope>
    └── TabContainerControl<TParent, TScope>
```

### 3.2 ControlBase&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/ControlBase.cs` (28 public methods/properties)

| Method | Signature | Needs async |
|--------|-----------|-------------|
| `IsExists()` | `bool IsExists()` | Yes |
| `IsVisible()` | `bool? IsVisible()` | Yes |
| `IsEnabled()` | `bool? IsEnabled()` | Yes |
| `WaitExists` | `bool WaitExists(bool? expected, int? timeoutMs = null)` | Yes |
| `WaitVisible` | `bool WaitVisible(bool? expected, int? timeoutMs = null)` | Yes |
| `WaitEnabled` | `bool WaitEnabled(bool? expected, int? timeoutMs = null)` | Yes |
| `AssertExists` | `TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)` | Yes |
| `AssertVisible` | `TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)` | Yes |
| `AssertEnabled` | `TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)` | Yes |
| `GetText` | `string? GetText(int? timeoutMs = null)` | Yes |
| `WaitText` | `bool WaitText(string? expected, int? timeoutMs = null)` | Yes |
| `AssertText` | `TScope AssertText(string? expected, string? message = null, int? timeoutMs = null)` | Yes |
| `AssertTextContains` | `TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)` | Yes |
| `GetAttribute` | `string? GetAttribute(string name)` | Yes |

Protected helpers that need async counterparts:

| Method | Signature |
|--------|-----------|
| `TryFindElement()` | `IHtmlElement? TryFindElement()` |
| `FindElement()` | `IHtmlElement FindElement()` |
| `RunWithElement` | `TScope RunWithElement(Action<IHtmlElement>)` |
| `RunWithElement<T>` | `TResult RunWithElement<TResult>(Func<IHtmlElement, TResult>)` |
| `RunAssert` | `TScope RunAssert(Action<IHtmlElement>)` |

### 3.3 Control&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Control.cs`

| Method | Needs async |
|--------|-------------|
| `TScope Click()` | Yes |
| `TScope SendKeys(string text)` | Yes |
| `TScope Clear()` | Yes |
| `TScope ScrollIntoView(int timeoutMs = 5000)` | Yes |

### 3.4 ClickableControlBase&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/ClickableControlBase.cs`

| Method | Needs async |
|--------|-------------|
| `TScope DoubleClick()` | Yes |
| `TScope RightClick()` | Yes |
| `TScope Hover()` | Yes |

### 3.5 FocusableControlBase&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/FocusableControlBase.cs`

| Method | Needs async |
|--------|-------------|
| `TScope Focus()` | Yes |
| `TScope Blur()` | Yes |
| `bool HasFocus()` | Yes |

### 3.6 SelectorControlBase&lt;TScope&gt; (abstract)

File: `srcnew/Brinell.Html/Controls/SelectorControlBase.cs`

| Method | Needs async |
|--------|-------------|
| `abstract TScope SelectByValue(string value)` | Yes |
| `abstract TScope SelectByText(string text)` | Yes |
| `abstract string? GetSelectedValue()` | Yes |

### 3.7 ToggleControlBase&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/ToggleControlBase.cs`

| Method | Needs async |
|--------|-------------|
| `bool IsChecked()` | Yes |
| `TScope SetChecked(bool value)` | Yes |
| `bool WaitChecked(bool expected, int? timeoutMs = null)` | Yes |
| `TScope AssertChecked(bool expected)` | Yes |

### 3.8 RangeControlBase&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/RangeControlBase.cs`

| Method | Needs async |
|--------|-------------|
| `string? GetMin()` | Yes |
| `string? GetMax()` | Yes |
| `string? GetStep()` | Yes |
| `string GetValue()` | Yes |
| `TScope SetValue(string value)` | Yes |

### 3.9 ScrollableControlBase&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/ScrollableControlBase.cs`

| Method | Needs async |
|--------|-------------|
| `TScope ScrollTo(int x, int y)` | Yes |
| `TScope ScrollToTop()` | Yes |

### 3.10 ButtonControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Buttons/ButtonControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope Submit()` | Yes |

### 3.11 LinkControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Buttons/LinkControl.cs`

| Method | Needs async |
|--------|-------------|
| `string? Href` (property) | Yes |
| `TScope AssertHref(string? expected)` | Yes |

### 3.12 SelectControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Selection/SelectControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope SelectByValue(string value)` | Yes |
| `TScope SelectByText(string text)` | Yes |
| `string? GetSelectedValue()` | Yes |
| `TScope SelectMultiple(params string[] values)` | Yes |

### 3.13 RadioGroupControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Selection/RadioGroupControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope SelectByValue(string value)` | Yes |
| `TScope SelectByText(string text)` | Yes |
| `string? GetSelectedValue()` | Yes |

### 3.14 DateInputControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/DateTime/DateInputControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope SetDate(DateOnly date)` | Yes |
| `DateOnly? GetDate()` | Yes |

### 3.15 TimeInputControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/DateTime/TimeInputControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope SetTime(TimeOnly time)` | Yes |
| `TimeOnly? GetTime()` | Yes |

### 3.16 TextInputControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Text/TextInputControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope SetText(string text)` | Yes |
| `string GetValue()` | Yes |
| `TScope TypeText(string text)` | Yes |
| `TScope AssertValue(string? expected)` | Yes |
| `TScope WaitValue(string? expected, int? timeoutMs = null)` | Yes |

### 3.17 TextAreaControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Text/TextAreaControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope AppendText(string text)` | Yes |

### 3.18 CheckBoxControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Toggle/CheckBoxControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope Check()` | Yes |
| `TScope Uncheck()` | Yes |
| `TScope Toggle()` | Yes |

### 3.19 RadioButtonControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Toggle/RadioButtonControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope Select()` | Yes |

### 3.20 LabelControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Display/LabelControl.cs`

| Method | Needs async |
|--------|-------------|
| `bool IsTextContaining(string substring, int? timeoutMs = null)` | Yes |
| `TScope WaitTextContaining(string substring, int? timeoutMs = null)` | Yes |
| `TScope AssertTextContaining(string substring)` | Yes |

### 3.21 ProgressControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Display/ProgressControl.cs`

| Method | Needs async |
|--------|-------------|
| `double GetValue()` | Yes |
| `double GetMax()` | Yes |
| `double GetPercentage()` | Yes |
| `TScope AssertValue(double expected)` | Yes |

### 3.22 ListControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Collection/ListControl.cs`

| Method | Needs async |
|--------|-------------|
| `int ItemCount` (property) | Yes |
| `string? GetItemText(int index)` | Yes |
| `IReadOnlyList<string?> GetItemTexts()` | Yes |

### 3.23 TableControl&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/Collection/TableControl.cs`

| Method | Needs async |
|--------|-------------|
| `int RowCount` (property) | Yes |
| `int ColumnCount` (property) | Yes |
| `string? GetCellText(int row, int column)` | Yes |
| `string? GetHeaderText(int column)` | Yes |
| `IReadOnlyList<string?> GetRowTexts(int row)` | Yes |

### 3.24 List&lt;TScope&gt;

File: `srcnew/Brinell.Html/Controls/List.cs`

| Method | Needs async |
|--------|-------------|
| `int Count` (property) | Yes |
| `string? GetItemText(int index)` | Yes |
| `IReadOnlyList<string?> GetItemTexts()` | Yes |

### 3.25 ContainerBase&lt;TParent, TScope&gt;

File: `srcnew/Brinell.Html/Controls/ContainerBase.cs`

| Method | Needs async |
|--------|-------------|
| `bool IsReady(int? timeoutMs = null)` | Yes |
| `bool WaitReady(int? timeoutMs = null)` | Yes |
| `IHtmlElement ContainerRoot` (property) | Yes |
| `IHtmlElement? TryFindElement(Locator locator)` | Yes |
| `IHtmlElement FindElement(Locator locator)` | Yes |
| `IReadOnlyList<IHtmlElement> FindElements(Locator locator)` | Yes |

### 3.26 ScrollContainerControl&lt;TParent, TScope&gt;

File: `srcnew/Brinell.Html/Controls/Container/ScrollContainerControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope ScrollToTop()` | Yes |
| `TScope ScrollBy(int deltaX, int deltaY)` | Yes |

### 3.27 TabContainerControl&lt;TParent, TScope&gt;

File: `srcnew/Brinell.Html/Controls/Container/TabContainerControl.cs`

| Method | Needs async |
|--------|-------------|
| `TScope SelectTab(int index)` | Yes |
| `TScope SelectTab(string text)` | Yes |
| `int TabCount` (property) | Yes |

## 4. PlaywrightHtmlElement.cs — blocking calls

File: `srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs`

**40 total `.GetAwaiter().GetResult()` calls:**

| Line | Playwright API blocked | Sync wrapper |
|------|----------------------|--------------|
| 19 | `IsVisibleAsync()` | `Visible` property |
| 21 | `IsEnabledAsync()` | `Enabled` property |
| 25 | `InnerTextAsync()` | `Text` property |
| 27 | `EvaluateAsync<string>(tagName)` | `TagName` property |
| 35 | `ClickAsync()` | `Click()` |
| 42 | `FillAsync(text)` | `SendKeys()` SetValue branch |
| 45 | `FillAsync(text)` | `SendKeys()` Paste branch |
| 48 | `PressSequentiallyAsync(text)` | `SendKeys()` Keys branch |
| 53 | `ClearAsync()` | `Clear()` |
| 55 | `DblClickAsync()` | `DoubleClick()` |
| 57 | `ClickAsync(RightButton)` | `RightClick()` |
| 59 | `HoverAsync()` | `Hover()` |
| 61 | `ClickAsync(Delay)` | `LongPress()` |
| 64 | `ScrollIntoViewIfNeededAsync()` | `ScrollIntoView()` |
| 69 | `Mouse.MoveAsync(start)` | `Swipe()` step 1 |
| 70 | `Mouse.DownAsync()` | `Swipe()` step 2 |
| 71 | `Mouse.MoveAsync(end)` | `Swipe()` step 3 |
| 72 | `Mouse.UpAsync()` | `Swipe()` step 4 |
| 75 | `GetAttributeAsync(name)` | `GetAttribute()` |
| 88 | `WaitForAsync(Attached)` | `FindElement()` wait |
| 100 | `CountAsync()` | `FindElement()` count |
| 123 | `WaitForAsync(Attached)` | `FindElements()` wait |
| 135 | `CountAsync()` | `FindElements()` count |
| 162 | `EvaluateAsync<string?>(property)` | `GetDomProperty()` |
| 165 | `EvaluateAsync<string>(computedStyle)` | `GetCssValue()` |
| 168 | `EvaluateAsync(form.submit)` | `Submit()` |
| 170 | `InnerHTMLAsync()` | `InnerHtml` property |
| 172 | `EvaluateAsync<string>(outerHTML)` | `OuterHtml` property |
| 174 | `IsCheckedAsync()` | `IsChecked` property |
| 176 | `InputValueAsync()` | `InputValue` property |
| 178 | `FillAsync(value)` | `Fill()` |
| 180 | `SelectOptionAsync(single)` | `SelectOption(string)` |
| 185 | `SelectOptionAsync(multi)` | `SelectOption(string[])` |
| 188 | `CheckAsync()` | `Check()` |
| 190 | `UncheckAsync()` | `Uncheck()` |
| 192 | `FocusAsync()` | `Focus()` |
| 194 | `BlurAsync()` | `Blur()` |
| 197 | `EvaluateAsync<T>(expr)` | `Evaluate<T>()` |
| 200 | `EvaluateAsync(expr)` | `Evaluate()` |
| 204 | `BoundingBoxAsync()` | `EvaluateRect()` helper |

## 5. PlaywrightTestContext.cs — blocking calls

File: `srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs`

**13 total `.GetAwaiter().GetResult()` calls:**

| Line | Playwright API blocked | Sync wrapper |
|------|----------------------|--------------|
| 138 | `TitleAsync()` | `PageTitle` property |
| 182 | `CountAsync()` | `TryFindElement()` |
| 219 | `CountAsync()` | `FindElements()` |
| 237 | `GotoAsync(destination)` | `NavigateTo()` |
| 242 | `GoBackAsync()` | `NavigateBack()` |
| 247 | `GoForwardAsync()` | `GoForward()` |
| 252 | `ReloadAsync()` | `Refresh()` |
| 257 | `ScreenshotAsync()` | `TakeScreenshot()` |
| 263 | `ScreenshotAsync(path)` | `SaveScreenshot()` |
| 268 | `ClearCookiesAsync()` | `ResetAppState()` |
| 273 | `DisposeAsyncCore(false)` | `Dispose()` |
| 305 | `CloseAsync()` | `Dispose()` browserContext |
| 319 | `CloseAsync()` | `Dispose()` browser |

Additional note: `WaitReady()` at line 156 uses `Thread.Sleep(100)` in a polling loop. `FindElement()` at line 203 also uses `Thread.Sleep(100)` in a polling loop. Both need async counterparts using `Task.Delay`.

## 6. LocatorExtensions.cs

File: `srcnew/Brinell.Html.Playwright/LocatorExtensions.cs`

**Zero blocking async calls.** This is a pure synchronous mapping utility (Brinell `Locator` to Playwright `ILocator`). No changes needed for async migration.

## 7. Supporting files in Brinell.Html/

### 7.1 HtmlPageObjectBase&lt;TSelf&gt;

File: `srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs`

| Method | Needs async |
|--------|-------------|
| `bool IsLoaded(int? timeoutMs = null)` | Yes |
| `bool WaitLoaded(bool? expected, int? timeoutMs = null)` | Yes |
| `void AssertLoaded(bool? expected, string?, int?)` | Yes |
| `string? GetTitle(int? timeoutMs = null)` | Yes |
| `bool WaitTitle(string? expected, int? timeoutMs = null)` | Yes |
| `void AssertTitle(string? expected, string?, int?)` | Yes |
| `void TakeScreenshot(string?, int?)` | Yes |
| `bool IsReady(int?)` | Yes |
| `bool WaitReady(int?)` | Yes |
| `IHtmlElement? TryFindElement(Locator)` | Yes (delegates to context) |
| `IHtmlElement FindElement(Locator)` | Yes (delegates to context) |
| `IReadOnlyList<IHtmlElement> FindElements(Locator)` | Yes (delegates to context) |

### 7.2 HtmlTestFixtureBase

File: `srcnew/Brinell.Html/Testing/HtmlTestFixtureBase.cs`

Already async for lifecycle (`InitializeAsync`, `DisposeAsync`). `NavigateTo()` method calls sync `Context.NavigateTo()`, needs async version.

### 7.3 HtmlTestContextOptions

File: `srcnew/Brinell.Html/Context/HtmlTestContextOptions.cs`

Pure configuration POCO. No changes needed.

## 8. Test files in Brinell.Html.UITests/

### 8.1 Test infrastructure

| File | Class | Notes |
|------|-------|-------|
| `TestBase/BlazorSampleTestBase.cs` | `BlazorSampleTestBase` | Already uses `IAsyncLifetime` (async setup/teardown). `NavigateToPage()` calls sync `Context.NavigateTo()`. |
| `GlobalUsings.cs` | N/A | Global using directives |

### 8.2 Page objects (all sync, need async counterparts)

| File | Class | Controls exposed |
|------|-------|-----------------|
| `PageObjects/CounterPage.cs` | `CounterPage` | `LabelControl` CountDisplay, `ButtonControl` IncrementButton, `ButtonControl` ResetButton |
| `PageObjects/LoginPage.cs` | `LoginPage` | 3x `TextInputControl`, `ButtonControl`, 2x `LabelControl` |
| `PageObjects/FormControlsPage.cs` | `FormControlsPage` | 2x `CheckBoxControl`, 2x `SelectControl`, `TextAreaControl`, `LinkControl`, `ProgressControl`, `RangeInputControl`, `LabelControl` |
| `PageObjects/DataTablePage.cs` | `DataTablePage` | `TableControl` DataGrid |

### 8.3 Test classes

All test methods are currently **synchronous** (`void` return). Every test method will need an `async Task` counterpart when the framework exposes async APIs.

| File | Class | Test count | Methods |
|------|-------|-----------|---------|
| `Tests/Controls/ButtonControlTests.cs` | `ButtonControlTests` | 3 sync | `Button_Click_IncrementsCounter`, `Button_IsVisible_ReturnsTrueForVisibleButton`, `Button_AssertEnabled_PassesForEnabledButton` |
| `Tests/Controls/CheckBoxControlTests.cs` | `CheckBoxControlTests` | 3 sync | `CheckBox_Check_SetsCheckedTrue`, `CheckBox_Uncheck_SetsCheckedFalse`, `CheckBox_Toggle_FlipsCheckedState` |
| `Tests/Controls/SelectControlTests.cs` | `SelectControlTests` | 3 sync | `Select_SelectByValue_UpdatesSelectedValue`, `Select_SelectByText_UpdatesSelectedValue`, `Select_GetSelectedValue_ReturnsCurrentValue` |
| `Tests/Controls/TextInputControlTests.cs` | `TextInputControlTests` | 3 sync | `TextInput_SetTextAndGetValue_RoundTripsValue`, `TextInput_Clear_RemovesText`, `TextInput_TypeText_AppendsTypedCharacters` |
| `Tests/Pages/CounterPageTests.cs` | `CounterPageTests` | 2 sync | `Counter_MultipleIncrements_DisplaysCorrectCount`, `Counter_ResetAfterIncrements_DisplaysZero` |
| `Tests/Pages/LoginPageTests.cs` | `LoginPageTests` | 2 sync | `Login_ValidCredentials_ShowsSuccessMessage`, `Login_InvalidCredentials_ShowsErrorMessage` |
| `Tests/Scenarios/LoginFlowTests.cs` | `LoginFlowTests` | 2 sync | `LoginFlow_ValidCredentials_ShowsSuccessMessage`, `LoginFlow_EmptyCredentials_ShowsError` |

**Total: 7 test classes, 18 sync test methods, 0 async test methods.**

## 9. Summary counts

| Category | Count |
|----------|-------|
| Interfaces in Brinell.Html | 6 |
| Control/base classes in Brinell.Html | 22 (including ObjectBase, ContainerBase, HtmlPageObjectBase) |
| Concrete control classes | 17 |
| Source files in Brinell.Html | 28 |
| Source files in Brinell.Html.Playwright | 3 |
| `.GetAwaiter().GetResult()` in PlaywrightHtmlElement | 40 |
| `.GetAwaiter().GetResult()` in PlaywrightTestContext | 13 |
| `Thread.Sleep` in PlaywrightTestContext | 2 (lines 156, 203) |
| `.GetAwaiter().GetResult()` in LocatorExtensions | 0 |
| Test files in Brinell.Html.UITests | 12 (4 page objects, 1 test base, 1 global usings, 7 test files minus GlobalUsings) |
| Total sync test methods | 18 |
| Public methods needing async counterparts (controls) | ~95 |
| Properties needing async counterparts | ~15 |
