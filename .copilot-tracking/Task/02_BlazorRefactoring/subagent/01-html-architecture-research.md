# Brinell.Html Architecture Research

**Date:** February 23, 2026  
**Scope:** `srcnew/Brinell.Html/` — complete file-by-file analysis  
**Purpose:** Understand how Blazor controls should layer on top of the Html base layer

---

## 1. Project Structure

**Project file:** [Brinell.Html.csproj](srcnew/Brinell.Html/Brinell.Html.csproj)
- Depends on `Brinell.Core`
- Namespace: `Brinell.Html`
- PackageId: `Brinell.Html`

### Directory Layout (37 .cs files)

```
srcnew/Brinell.Html/
├── ObjectBase.cs                          # Root base class
├── Brinell.Html.csproj
├── Interfaces/
│   ├── IHtmlElement.cs                    # Platform element type
│   ├── IHtmlElementScope.cs              # Element-finding scope
│   ├── IHtmlScope.cs                     # Generic scope with Self pattern
│   ├── IHtmlPage.cs                      # Page interface
│   ├── IHtmlContainer.cs                 # Container interface
│   └── IHtmlTestContext.cs               # Test context interface
├── Context/
│   └── HtmlTestContextOptions.cs         # Context configuration
├── Pages/
│   └── HtmlPageObjectBase.cs             # Page object base class
├── Testing/
│   └── HtmlTestFixtureBase.cs            # Test fixture base class
└── Controls/
    ├── ControlBase.cs                     # ★ Root control base (168 lines)
    ├── Control.cs                         # Adds Click/SendKeys/Clear
    ├── ClickableControlBase.cs            # Adds DoubleClick/RightClick/Hover
    ├── FocusableControlBase.cs            # Adds Focus/Blur/HasFocus
    ├── ToggleControlBase.cs               # Adds IsChecked/SetChecked/AssertChecked
    ├── SelectorControlBase.cs             # Abstract SelectByValue/Text/GetSelectedValue
    ├── ScrollableControlBase.cs           # Adds ScrollTo/ScrollToTop
    ├── RangeControlBase.cs                # Adds GetMin/Max/Step/Value/SetValue
    ├── ContainerBase.cs                   # Container scoping base
    ├── List.cs                            # Generic list helper
    ├── Buttons/
    │   ├── ButtonControl.cs               # Submit()
    │   └── LinkControl.cs                 # Href, AssertHref()
    ├── Toggle/
    │   ├── CheckBoxControl.cs             # Check/Uncheck/Toggle
    │   └── RadioButtonControl.cs          # Select()
    ├── Text/
    │   ├── TextInputControl.cs            # SetText/GetValue/TypeText/AssertValue/WaitValue
    │   └── TextAreaControl.cs             # AppendText()
    ├── Selection/
    │   ├── SelectControl.cs               # SelectByValue/Text, SelectMultiple
    │   └── RadioGroupControl.cs           # SelectByValue/Text (finds radio inputs)
    ├── Collection/
    │   ├── TableControl.cs                # RowCount/ColumnCount/GetCellText/GetHeaderText
    │   └── ListControl.cs                 # ItemCount/GetItemText/GetItemTexts
    ├── Range/
    │   └── RangeInputControl.cs           # GetNumericValue/SetNumericValue/AssertNumericValue
    ├── DateTime/
    │   ├── DateInputControl.cs            # SetDate/GetDate (DateOnly)
    │   └── TimeInputControl.cs            # SetTime/GetTime (TimeOnly)
    ├── Display/
    │   ├── LabelControl.cs                # IsTextContaining/WaitTextContaining/AssertTextContaining
    │   └── ProgressControl.cs             # GetValue/GetMax/GetPercentage/AssertValue
    └── Container/
        ├── TabContainerControl.cs         # SelectTab(index)/SelectTab(text)/TabCount
        └── ScrollContainerControl.cs      # ScrollToTop/ScrollBy
```

---

## 2. Core Interfaces (from Brinell.Core)

### 2.1 `IElement<TSelf>` — [IElement.cs](srcnew/Brinell.Core/Interfaces/IElement.cs)

The platform element abstraction. TSelf enables typed child finding.

| Category | Members |
|----------|---------|
| **State** | `bool Visible`, `bool Enabled`, `bool Selected`, `string? Text`, `string? TagName` |
| **Location** | `Point Location`, `Size Size`, `Rectangle Rect` |
| **Actions** | `Click()`, `SendKeys(string, TextInputMethod)`, `Clear()` |
| **Gestures** | `DoubleClick()`, `RightClick()`, `Hover()`, `LongPress(int)`, `ScrollIntoView(int)`, `Swipe(int,int,int,int,int)` |
| **Attributes** | `string? GetAttribute(string)` |
| **Child Finding** | `TSelf FindElement(Locator, int)`, `IReadOnlyList<TSelf> FindElements(Locator, int)`, `bool TryFindElement(Locator, out TSelf?, int)` |

### 2.2 `IElementScope<TElement>` — [IElementScope.cs](srcnew/Brinell.Core/Interfaces/IElementScope.cs)

Non-generic base: `LocatorStrategy DefaultLocatorStrategy`, `IPageObject? Page`, `IsReady(int?)`, `WaitReady(int?)`
Generic: `TElement? TryFindElement(Locator)`, `TElement FindElement(Locator)`, `IReadOnlyList<TElement> FindElements(Locator)`

### 2.3 `IControlObject<TScope>` — [IControlObject.cs](srcnew/Brinell.Core/Interfaces/IControlObject.cs)

The universal control interface. **TScope** is the containing scope (page or container) — all action/assert methods return TScope for fluent chaining.

| Category | Members |
|----------|---------|
| **State** | `bool IsExists()`, `bool? IsVisible()`, `bool? IsEnabled()` |
| **Wait** | `bool WaitExists(bool?, int?)`, `bool WaitVisible(bool?, int?)`, `bool WaitEnabled(bool?, int?)` |
| **Assert** | `TScope AssertExists(bool?, string?, int?)`, `TScope AssertVisible(bool?, string?, int?)`, `TScope AssertEnabled(bool?, string?, int?)` |
| **Text** | `string? GetText(int?)`, `bool WaitText(string?, int?)`, `TScope AssertText(string?, string?, int?)`, `TScope AssertTextContains(string?, string?, int?)` |
| **Attributes** | `string? GetAttribute(string)` |

### 2.4 `IPageObject` / `IPageObject<TElement>` — [IPageObject.cs](srcnew/Brinell.Core/Interfaces/IPageObject.cs)

Non-generic: `string Name`, `IsLoaded(int?)`, `WaitLoaded(bool?, int?)`, `AssertLoaded(bool?, string?, int?)`, `GetTitle(int?)`, `WaitTitle(string?, int?)`, `AssertTitle(string?, string?, int?)`, `TakeScreenshot(string?, int?)`

Generic adds `IElementScope<TElement>` for typed element finding.

### 2.5 `IContainerControl<TElement>` — [IContainerControl.cs](srcnew/Brinell.Core/Interfaces/IContainerControl.cs)

`TElement ContainerRoot` + inherits `IElementScope<TElement>` (TryFindElement, FindElement, FindElements).

### 2.6 `ITestContext` / `ITestContext<TElement>` — [ITestContext.cs](srcnew/Brinell.Core/Interfaces/ITestContext.cs)

Non-generic: `TimeoutSettings Timeouts`, `ITestLogger Logger`, `NavigateTo(string)`, `NavigateBack()`, `Refresh()`, `byte[] TakeScreenshot()`, `SaveScreenshot(string)`, `ResetAppState()`, `IDisposable`

Generic adds `IElementScope<TElement>`.

---

## 3. Html-Specific Interfaces

### 3.1 `IHtmlElement` — [IHtmlElement.cs](srcnew/Brinell.Html/Interfaces/IHtmlElement.cs)

Extends `IElement<IHtmlElement>` with HTML-specific operations:

```csharp
public interface IHtmlElement : IElement<IHtmlElement>
{
    string? GetDomAttribute(string attributeName);
    string? GetDomProperty(string propertyName);
    string? GetCssValue(string propertyName);
    void Submit();
    string InnerHtml { get; }
    string OuterHtml { get; }
    bool IsChecked { get; }
    string InputValue { get; }
    void Fill(string value);
    void SelectOption(string value);
    void SelectOption(string[] values);
    void Check();
    void Uncheck();
    void Focus();
    void Blur();
}
```

### 3.2 `IHtmlElementScope` — [IHtmlElementScope.cs](srcnew/Brinell.Html/Interfaces/IHtmlElementScope.cs#L5)

```csharp
public interface IHtmlElementScope : IElementScope<IHtmlElement>
{
    IHtmlTestContext Context { get; }
}
```

**Key insight:** Binds the element type to `IHtmlElement` and adds `Context` access.

### 3.3 `IHtmlScope<TScope>` — [IHtmlScope.cs](srcnew/Brinell.Html/Interfaces/IHtmlScope.cs#L3)

```csharp
public interface IHtmlScope<TScope> : IHtmlElementScope
    where TScope : IHtmlScope<TScope>
{
    TScope Self { get; }
}
```

**Key insight:** This is the **CRTP (Curiously Recurring Template Pattern)** that enables fluent chaining. `Self` returns the concrete type so controls can return their containing scope with the correct type.

### 3.4 `IHtmlPage<TSelf>` — [IHtmlPage.cs](srcnew/Brinell.Html/Interfaces/IHtmlPage.cs#L5)

```csharp
public interface IHtmlPage<TSelf> : IHtmlScope<TSelf>, IPageObject<IHtmlElement>
    where TSelf : IHtmlPage<TSelf>
{
}
```

Combines scope (Self pattern) + page object + typed element finding.

### 3.5 `IHtmlContainer<TParent, TSelf>` — [IHtmlContainer.cs](srcnew/Brinell.Html/Interfaces/IHtmlContainer.cs#L5)

```csharp
public interface IHtmlContainer<TParent, TSelf> : IHtmlScope<TSelf>, IContainerControl<IHtmlElement>
    where TParent : IHtmlScope<TParent>
    where TSelf : IHtmlContainer<TParent, TSelf>
{
    TParent Parent { get; }
}
```

Two type parameters: TParent (the parent scope) and TSelf (this container's type). Adds `Parent` for upward navigation.

### 3.6 `IHtmlTestContext` — [IHtmlTestContext.cs](srcnew/Brinell.Html/Interfaces/IHtmlTestContext.cs#L5)

```csharp
public interface IHtmlTestContext : ITestContext<IHtmlElement>, IHtmlElementScope
{
    new IHtmlTestContext Context { get; }    // self-referencing
    string CurrentUrl { get; }
    string PageTitle { get; }
    void GoForward();
}
```

---

## 4. Complete Inheritance Hierarchy

### 4.1 Control Hierarchy (all classes are generic on `TScope`)

```
ObjectBase                                          (abstract)
├── ControlBase<TScope>                             (abstract, implements IControlObject<TScope>)
│   ├── Control<TScope>                             (abstract, adds Click/SendKeys/Clear/ScrollIntoView)
│   │   ├── ClickableControlBase<TScope>            (abstract, adds DoubleClick/RightClick/Hover)
│   │   │   ├── ButtonControl<TScope>               (concrete, adds Submit)
│   │   │   ├── LinkControl<TScope>                 (concrete, adds Href/AssertHref)
│   │   │   ├── FocusableControlBase<TScope>        (abstract, adds Focus/Blur/HasFocus)
│   │   │   │   ├── TextInputControl<TScope>        (concrete, adds SetText/GetValue/TypeText/AssertValue/WaitValue)
│   │   │   │   │   └── TextAreaControl<TScope>     (concrete, adds AppendText)
│   │   │   │   ├── SelectorControlBase<TScope>     (abstract, adds SelectByValue/Text/GetSelectedValue)
│   │   │   │   │   ├── SelectControl<TScope>       (concrete, overrides Select*, adds SelectMultiple)
│   │   │   │   │   └── RadioGroupControl<TScope>   (concrete, overrides Select*, CSS form interaction)
│   │   │   │   └── RangeControlBase<TScope>        (abstract, adds GetMin/Max/Step/Value/SetValue)
│   │   │   │       ├── RangeInputControl<TScope>   (concrete, adds GetNumericValue/SetNumericValue/AssertNumericValue)
│   │   │   │       ├── DateInputControl<TScope>    (concrete, adds SetDate/GetDate)
│   │   │   │       └── TimeInputControl<TScope>    (concrete, adds SetTime/GetTime)
│   │   │   ├── ToggleControlBase<TScope>           (abstract, adds IsChecked/SetChecked/WaitChecked/AssertChecked)
│   │   │   │   ├── CheckBoxControl<TScope>         (concrete, adds Check/Uncheck/Toggle)
│   │   │   │   └── RadioButtonControl<TScope>      (concrete, adds Select)
│   │   │   └── ScrollableControlBase<TScope>       (abstract, adds ScrollTo/ScrollToTop)
│   │   └── (no direct concrete children of Control)
│   ├── LabelControl<TScope>                        (concrete, display-only control)
│   ├── ProgressControl<TScope>                     (concrete, display-only control)
│   ├── TableControl<TScope>                        (concrete, collection control)
│   ├── ListControl<TScope>                         (concrete, collection control)
│   └── List<TScope>                                (concrete, generic list helper)
└── ContainerBase<TParent, TScope>                  (abstract, implements IHtmlContainer<TParent, TScope>)
    ├── TabContainerControl<TParent, TScope>        (concrete, tab container)
    └── ScrollContainerControl<TParent, TScope>     (concrete, scroll container)

HtmlPageObjectBase<TSelf> : ObjectBase, IHtmlPage<TSelf>
HtmlTestFixtureBase                                 (abstract, standalone - not ObjectBase)
```

### 4.2 Interface Hierarchy

```
IElement<TSelf>
└── IHtmlElement : IElement<IHtmlElement>

IElementScope
├── IElementScope<TElement>
│   ├── IContainerControl<TElement>
│   │   └── IHtmlContainer<TParent, TSelf>
│   ├── IPageObject<TElement>
│   │   └── IHtmlPage<TSelf>
│   └── ITestContext<TElement>
│       └── IHtmlTestContext
└── IHtmlElementScope : IElementScope<IHtmlElement>
    └── IHtmlScope<TScope>                       ← CRTP with Self property
        ├── IHtmlPage<TSelf>
        └── IHtmlContainer<TParent, TSelf>

IControlObject<TScope>
└── (implemented by ControlBase<TScope>)
```

---

## 5. The `IHtmlScope<TScope>` / ContainingScope Pattern

### How Fluent Chaining Works

This is the **central design pattern** of the entire framework.

1. **`IHtmlScope<TScope>`** defines `TScope Self { get; }` — the CRTP self-type.
2. **Pages** implement `IHtmlScope<TSelf>` where TSelf is the concrete page type.
3. **Containers** implement `IHtmlScope<TSelf>` where TSelf is the concrete container type.
4. **Controls** receive `IHtmlScope<TScope>` as their scope, storing it as `_scope`.
5. **`ContainingScope`** property returns `_scope.Self` — the strongly-typed parent.
6. **All action/assert methods** return `ContainingScope` (= TScope), enabling:

```csharp
// In a test — MyPage implements IHtmlPage<MyPage>, so TScope = MyPage
page.Username.SetText("admin")     // returns MyPage
    .Password.SetText("secret")    // returns MyPage
    .LoginButton.Click()           // returns MyPage
    .ResultLabel.AssertText("OK"); // returns MyPage
```

### Scope Resolution Chain

| Entity | Scope Type | Self | Controls' TScope |
|--------|-----------|------|------------------|
| `MyPage : HtmlPageObjectBase<MyPage>` | `IHtmlScope<MyPage>` | `(MyPage)this` | `MyPage` |
| `MyContainer : ContainerBase<MyPage, MyContainer>` | `IHtmlScope<MyContainer>` | `(TScope)(object)this` | `MyContainer` |
| `ButtonControl<MyPage>` | receives `IHtmlScope<MyPage>` | N/A | `MyPage` |
| `ButtonControl<MyContainer>` | receives `IHtmlScope<MyContainer>` | N/A | `MyContainer` |

---

## 6. Constructor Patterns

### 6.1 All Regular Controls (ControlBase-derived)

**Pattern:** Two constructor overloads — one with `Locator`, one with `string selectorOrId`:

```csharp
public ConcreteControl(IHtmlScope<TScope> scope, Locator locator)
    : base(scope, locator) { }

public ConcreteControl(IHtmlScope<TScope> scope, string selectorOrId)
    : base(scope, selectorOrId) { }
```

- `scope` — The containing scope (page or container) that implements `IHtmlScope<TScope>`
- `locator` — A `Locator` instance (e.g., `Locator.ByCss(".my-class")`, `Locator.ByAutomationId("myId")`)
- `selectorOrId` — A string auto-resolved by `ResolveLocator()`:
  - If contains `#`, `.`, `[`, `:`, `>`, or space → treated as CSS selector (`Locator.ByCss`)
  - Otherwise → treated as automation ID (`Locator.ByAutomationId`)

### 6.2 Container Controls (ContainerBase-derived)

**Pattern:** Two type parameters, locator-based constructor:

```csharp
public TabContainerControl(IHtmlScope<TParent> parentScope, Locator locator, string tabSelector = "[role='tab']")
    : base(parentScope, locator) { }

public ScrollContainerControl(IHtmlScope<TParent> parentScope, Locator locator)
    : base(parentScope, locator) { }
```

- `parentScope` — The parent scope (e.g., the page)
- `locator` — Locates the container root element
- Extra params as needed (e.g., `tabSelector` for TabContainer)
- Must override `TScope Self` property (typically: `(TScope)(object)this`)

### 6.3 Page Objects

```csharp
protected HtmlPageObjectBase(IHtmlTestContext context)
```

- Receives the test context
- `Self` returns `(TSelf)this`

### 6.4 Test Fixtures

```csharp
// Abstract methods to implement:
protected abstract Task<IHtmlTestContext> CreateContextAsync(HtmlTestContextOptions options);
protected virtual HtmlTestContextOptions CreateOptions() => new();
```

---

## 7. Method Signatures by Class

### 7.1 `ObjectBase` — [ObjectBase.cs](srcnew/Brinell.Html/ObjectBase.cs#L1-L48)

```csharp
public abstract class ObjectBase
{
    public abstract IHtmlTestContext Context { get; }
    protected int DefaultTimeoutMs { get; }          // → Context.Timeouts.DefaultWait
    protected int PollingIntervalMs { get; }         // → Context.Timeouts.PollingInterval
    protected bool Poll(Func<bool> condition, int timeoutMs);
}
```

### 7.2 `ControlBase<TScope>` — [ControlBase.cs](srcnew/Brinell.Html/Controls/ControlBase.cs#L1-L247)

```csharp
public abstract class ControlBase<TScope> : ObjectBase, IControlObject<TScope>
{
    // Constructor
    protected ControlBase(IHtmlScope<TScope> scope, Locator locator);
    protected ControlBase(IHtmlScope<TScope> scope, string selectorOrId);

    // Properties
    protected Locator Locator { get; }
    protected TScope ContainingScope { get; }       // → _scope.Self
    public override IHtmlTestContext Context { get; }

    // Element access
    protected IHtmlElement? TryFindElement();
    protected IHtmlElement FindElement();

    // Fluent helpers
    protected TScope RunWithElement(Action<IHtmlElement> action);
    protected TResult RunWithElement<TResult>(Func<IHtmlElement, TResult> action);
    protected TScope RunAssert(Action<IHtmlElement> assertion);

    // IControlObject<TScope> implementation
    bool IsExists();
    bool? IsVisible();
    bool? IsEnabled();
    bool WaitExists(bool? expected, int? timeoutMs = null);
    bool WaitVisible(bool? expected, int? timeoutMs = null);
    bool WaitEnabled(bool? expected, int? timeoutMs = null);
    TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    string? GetText(int? timeoutMs = null);
    bool WaitText(string? expected, int? timeoutMs = null);
    TScope AssertText(string? expected, string? message = null, int? timeoutMs = null);
    TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    string? GetAttribute(string name);

    // Static helpers
    protected static Locator ResolveLocator(string selectorOrId);
}
```

### 7.3 `Control<TScope>` — [Control.cs](srcnew/Brinell.Html/Controls/Control.cs#L1-L34)

```csharp
public abstract class Control<TScope> : ControlBase<TScope>
{
    TScope Click();
    TScope SendKeys(string text);
    TScope Clear();
    TScope ScrollIntoView(int timeoutMs = 5000);
}
```

### 7.4 `ClickableControlBase<TScope>` — [ClickableControlBase.cs](srcnew/Brinell.Html/Controls/ClickableControlBase.cs#L1-L34)

```csharp
public abstract class ClickableControlBase<TScope> : Control<TScope>
{
    TScope DoubleClick();
    TScope RightClick();
    TScope Hover();
}
```

### 7.5 `FocusableControlBase<TScope>` — [FocusableControlBase.cs](srcnew/Brinell.Html/Controls/FocusableControlBase.cs#L1-L35)

```csharp
public abstract class FocusableControlBase<TScope> : ClickableControlBase<TScope>
{
    TScope Focus();
    TScope Blur();
    bool HasFocus();
}
```

### 7.6 `ToggleControlBase<TScope>` — [ToggleControlBase.cs](srcnew/Brinell.Html/Controls/ToggleControlBase.cs#L1-L54)

```csharp
public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>
{
    bool IsChecked();
    TScope SetChecked(bool value);
    bool WaitChecked(bool expected, int? timeoutMs = null);
    TScope AssertChecked(bool expected);
}
```

### 7.7 `SelectorControlBase<TScope>` — [SelectorControlBase.cs](srcnew/Brinell.Html/Controls/SelectorControlBase.cs#L1-L24)

```csharp
public abstract class SelectorControlBase<TScope> : FocusableControlBase<TScope>
{
    abstract TScope SelectByValue(string value);
    abstract TScope SelectByText(string text);
    abstract string? GetSelectedValue();
}
```

### 7.8 `ScrollableControlBase<TScope>` — [ScrollableControlBase.cs](srcnew/Brinell.Html/Controls/ScrollableControlBase.cs#L1-L42)

```csharp
public abstract class ScrollableControlBase<TScope> : ClickableControlBase<TScope>
{
    TScope ScrollTo(int x, int y);
    TScope ScrollToTop();
}
```

### 7.9 `RangeControlBase<TScope>` — [RangeControlBase.cs](srcnew/Brinell.Html/Controls/RangeControlBase.cs#L1-L34)

```csharp
public abstract class RangeControlBase<TScope> : FocusableControlBase<TScope>
{
    string? GetMin();       // from "min" attribute
    string? GetMax();       // from "max" attribute
    string? GetStep();      // from "step" attribute
    string GetValue();      // from element.InputValue
    TScope SetValue(string value);  // via element.Fill()
}
```

### 7.10 `ContainerBase<TParent, TScope>` — [ContainerBase.cs](srcnew/Brinell.Html/Controls/ContainerBase.cs#L1-L62)

```csharp
public abstract class ContainerBase<TParent, TScope> : ObjectBase, IHtmlContainer<TParent, TScope>
{
    // Constructor
    protected ContainerBase(IHtmlScope<TParent> parentScope, Locator locator);

    // Properties
    IHtmlTestContext Context { get; }
    TParent Parent { get; }
    abstract TScope Self { get; }
    LocatorStrategy DefaultLocatorStrategy { get; }
    IPageObject? Page { get; }
    IHtmlElement ContainerRoot { get; }

    // Ready state
    bool IsReady(int? timeoutMs = null);
    bool WaitReady(int? timeoutMs = null);

    // Element finding (scoped to container root)
    IHtmlElement? TryFindElement(Locator locator);
    IHtmlElement FindElement(Locator locator);
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);
}
```

---

## 8. Concrete Controls Summary

### 8.1 Buttons

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `ButtonControl<TScope>` | `ClickableControlBase<TScope>` | [ButtonControl.cs](srcnew/Brinell.Html/Controls/Buttons/ButtonControl.cs#L8) | `TScope Submit()` |
| `LinkControl<TScope>` | `ClickableControlBase<TScope>` | [LinkControl.cs](srcnew/Brinell.Html/Controls/Buttons/LinkControl.cs#L8) | `string? Href`, `TScope AssertHref(string?)` |

### 8.2 Toggle

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `CheckBoxControl<TScope>` | `ToggleControlBase<TScope>` | [CheckBoxControl.cs](srcnew/Brinell.Html/Controls/Toggle/CheckBoxControl.cs#L6) | `Check()`, `Uncheck()`, `Toggle()` |
| `RadioButtonControl<TScope>` | `ToggleControlBase<TScope>` | [RadioButtonControl.cs](srcnew/Brinell.Html/Controls/Toggle/RadioButtonControl.cs#L6) | `Select()` |

### 8.3 Text

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `TextInputControl<TScope>` | `FocusableControlBase<TScope>` | [TextInputControl.cs](srcnew/Brinell.Html/Controls/Text/TextInputControl.cs#L8) | `SetText(string)`, `GetValue()`, `TypeText(string)`, `AssertValue(string?)`, `WaitValue(string?, int?)` |
| `TextAreaControl<TScope>` | `TextInputControl<TScope>` | [TextAreaControl.cs](srcnew/Brinell.Html/Controls/Text/TextAreaControl.cs#L6) | `AppendText(string)` |

### 8.4 Selection

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `SelectControl<TScope>` | `SelectorControlBase<TScope>` | [SelectControl.cs](srcnew/Brinell.Html/Controls/Selection/SelectControl.cs#L6) | Overrides `SelectByValue`, `SelectByText`, `GetSelectedValue`. Adds `SelectMultiple(params string[])` |
| `RadioGroupControl<TScope>` | `SelectorControlBase<TScope>` | [RadioGroupControl.cs](srcnew/Brinell.Html/Controls/Selection/RadioGroupControl.cs#L6) | Overrides `SelectByValue`, `SelectByText`, `GetSelectedValue`. CSS-based radio finding. |

### 8.5 Range / DateTime

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `RangeInputControl<TScope>` | `RangeControlBase<TScope>` | [RangeInputControl.cs](srcnew/Brinell.Html/Controls/Range/RangeInputControl.cs#L8) | `GetNumericValue()`, `SetNumericValue(double)`, `AssertNumericValue(double)` |
| `DateInputControl<TScope>` | `RangeControlBase<TScope>` | [DateInputControl.cs](srcnew/Brinell.Html/Controls/DateTime/DateInputControl.cs#L6) | `SetDate(DateOnly)`, `GetDate()` |
| `TimeInputControl<TScope>` | `RangeControlBase<TScope>` | [TimeInputControl.cs](srcnew/Brinell.Html/Controls/DateTime/TimeInputControl.cs#L6) | `SetTime(TimeOnly)`, `GetTime()` |

### 8.6 Display

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `LabelControl<TScope>` | `ControlBase<TScope>` | [LabelControl.cs](srcnew/Brinell.Html/Controls/Display/LabelControl.cs#L8) | `IsTextContaining(string, int?)`, `WaitTextContaining(string, int?)`, `AssertTextContaining(string)` |
| `ProgressControl<TScope>` | `ControlBase<TScope>` | [ProgressControl.cs](srcnew/Brinell.Html/Controls/Display/ProgressControl.cs#L8) | `GetValue()`, `GetMax()`, `GetPercentage()`, `AssertValue(double)` |

### 8.7 Collection

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `TableControl<TScope>` | `ControlBase<TScope>` | [TableControl.cs](srcnew/Brinell.Html/Controls/Collection/TableControl.cs#L6) | `RowCount`, `ColumnCount`, `GetCellText(int, int)`, `GetHeaderText(int)`, `GetRowTexts(int)` |
| `ListControl<TScope>` | `ControlBase<TScope>` | [ListControl.cs](srcnew/Brinell.Html/Controls/Collection/ListControl.cs#L6) | `ItemCount`, `GetItemText(int)`, `GetItemTexts()` |
| `List<TScope>` | `ControlBase<TScope>` | [List.cs](srcnew/Brinell.Html/Controls/List.cs#L6) | `Count`, `GetItemText(int)`, `GetItemTexts()` |

### 8.8 Container

| Class | Base | File | Added Methods |
|-------|------|------|---------------|
| `TabContainerControl<TParent, TScope>` | `ContainerBase<TParent, TScope>` | [TabContainerControl.cs](srcnew/Brinell.Html/Controls/Container/TabContainerControl.cs#L6) | `SelectTab(int)`, `SelectTab(string)`, `TabCount` |
| `ScrollContainerControl<TParent, TScope>` | `ContainerBase<TParent, TScope>` | [ScrollContainerControl.cs](srcnew/Brinell.Html/Controls/Container/ScrollContainerControl.cs#L6) | `ScrollToTop()`, `ScrollBy(int, int)` |

---

## 9. HtmlPageObjectBase — [HtmlPageObjectBase.cs](srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs#L1-L113)

```csharp
public abstract class HtmlPageObjectBase<TSelf> : ObjectBase, IHtmlPage<TSelf>
    where TSelf : HtmlPageObjectBase<TSelf>
{
    // Constructor
    protected HtmlPageObjectBase(IHtmlTestContext context);

    // Properties
    IHtmlTestContext Context { get; }
    TSelf Self { get; }                          // (TSelf)this
    string Name { get; }                         // GetType().Name
    LocatorStrategy DefaultLocatorStrategy { get; }
    IPageObject? Page { get; }                   // this

    // Page state
    virtual bool IsLoaded(int? timeoutMs = null);  // default: true
    bool WaitLoaded(bool?, int?);
    void AssertLoaded(bool?, string?, int?);

    // Title
    virtual string? GetTitle(int?);          // Context.PageTitle
    bool WaitTitle(string?, int?);
    void AssertTitle(string?, string?, int?);

    // Screenshots
    void TakeScreenshot(string?, int?);

    // Ready state
    bool IsReady(int?);
    bool WaitReady(int?);

    // Element finding (delegates to context)
    IHtmlElement? TryFindElement(Locator);
    IHtmlElement FindElement(Locator);
    IReadOnlyList<IHtmlElement> FindElements(Locator);
}
```

---

## 10. HtmlTestFixtureBase — [HtmlTestFixtureBase.cs](srcnew/Brinell.Html/Testing/HtmlTestFixtureBase.cs#L1-L40)

```csharp
public abstract class HtmlTestFixtureBase
{
    protected IHtmlTestContext Context { get; }

    protected virtual HtmlTestContextOptions CreateOptions();
    protected abstract Task<IHtmlTestContext> CreateContextAsync(HtmlTestContextOptions options);

    public virtual Task InitializeAsync();
    public virtual Task DisposeAsync();
    protected void NavigateTo(string path);
}
```

---

## 11. HtmlTestContextOptions — [HtmlTestContextOptions.cs](srcnew/Brinell.Html/Context/HtmlTestContextOptions.cs#L1-L18)

```csharp
public class HtmlTestContextOptions
{
    string? BaseUrl { get; set; }
    bool Headless { get; set; } = true;
    string BrowserType { get; set; } = "chromium";
    TimeoutSettings Timeouts { get; set; } = TimeoutSettings.Default;
    ITestLogger? Logger { get; set; }
    bool EnableTracing { get; set; }
    string? CdpEndpoint { get; set; }
}
```

---

## 12. Key Design Insights for Blazor Layering

### 12.1 The TScope Generic Parameter is Everything

- **Every control** is `SomeControl<TScope>` where `TScope : IHtmlScope<TScope>`.
- TScope is the containing scope — the page or container that owns the control.
- Action methods return `TScope` (via `ContainingScope`) for fluent chaining.
- **Blazor controls can inherit from the same base classes** with no modification, since TScope is generic.

### 12.2 Element Access Always Goes Through Scope

- Controls never hold elements directly — they call `_scope.TryFindElement(Locator)`.
- The scope (page/container/context) provides the element-finding implementation.
- **For Blazor:** The scope chain remains the same — only the `IHtmlTestContext` implementation changes (Playwright vs. other drivers).

### 12.3 ContainerBase Is a Separate Branch

- `ContainerBase<TParent, TScope>` has TWO type parameters (parent + self).
- It does NOT inherit from `ControlBase` — it's a separate `ObjectBase` branch.
- It provides its own `TryFindElement`/`FindElement`/`FindElements` scoped to `ContainerRoot`.
- Containers find their root via `_parentScope.FindElement(_locator)`, then child elements within that root.

### 12.4 How Blazor Should Layer

Since all controls are in `Brinell.Html`, a `Brinell.Blazor` package should:

1. **NOT duplicate controls** — Blazor-specific controls can inherit from `Brinell.Html.Controls.*` directly.
2. **Provide `IHtmlTestContext` implementation** — backed by Playwright's `IPage`.
3. **Provide `IHtmlElement` implementation** — wrapping Playwright's `ILocator` or `IElementHandle`.
4. **Provide Blazor-specific page objects** — extend `HtmlPageObjectBase<TSelf>`.
5. **Provide Blazor-specific test fixture** — extend `HtmlTestFixtureBase`, implementing `CreateContextAsync` with Playwright setup.
6. **Add Blazor-specific controls only when needed** — e.g., component-specific wrappers.

### 12.5 Constructor Wire-Up Pattern

In a page object, controls are instantiated like:

```csharp
public class LoginPage : HtmlPageObjectBase<LoginPage>
{
    public LoginPage(IHtmlTestContext context) : base(context) { }

    // 'this' is IHtmlScope<LoginPage>, so TScope = LoginPage for all controls
    public TextInputControl<LoginPage> Username => new(this, "username");
    public TextInputControl<LoginPage> Password => new(this, "password");
    public ButtonControl<LoginPage> LoginButton => new(this, "button.login");
    public LabelControl<LoginPage> ErrorMessage => new(this, "#error-msg");
}
```

Usage:
```csharp
page.Username.SetText("admin")   // returns LoginPage
    .Password.SetText("secret")  // returns LoginPage
    .LoginButton.Click()         // returns LoginPage
    .ErrorMessage.AssertText("Welcome!");  // returns LoginPage
```

---

## 13. Summary Statistics

| Category | Count |
|----------|-------|
| Total .cs files | 37 |
| Interfaces (Brinell.Html) | 6 |
| Abstract base classes | 9 (ObjectBase, ControlBase, Control, ClickableControlBase, FocusableControlBase, ToggleControlBase, SelectorControlBase, ScrollableControlBase, RangeControlBase) + ContainerBase |
| Concrete controls | 15 |
| Page/Fixture/Context | 3 |
| Brinell.Core interfaces used | IControlObject, IElement, IElementScope, IPageObject, IContainerControl, ITestContext |
