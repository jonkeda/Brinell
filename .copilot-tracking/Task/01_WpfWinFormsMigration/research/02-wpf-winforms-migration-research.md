<!-- markdownlint-disable-file -->
# Research: Migrating Brinell.Wpf and Brinell.WinForms from src to srcnew

**Brief:** [01-wpf-winforms-migration-research-brief.md](01-wpf-winforms-migration-research-brief.md)
**Date:** February 22, 2026

## 1. Architecture Pattern (from MAUI reference)

The new architecture uses a **scope-based, fluent-chaining UI test framework** with CRTP:

```
Brinell.Core                          ← Platform-agnostic interfaces + ControlObjectBase<TScope>
  ├── IElement<TSelf>                 ← Rich, self-referencing element interface
  ├── IDriver<TElement>               ← Element-typed driver
  ├── ITestContext<TElement>           ← Extends IElementScope<TElement>
  ├── IPageObject<TElement>            ← Extends IElementScope<TElement>
  ├── IControlObject<TScope>           ← Is/Wait/Assert pattern
  ├── IClickableControlObject<TScope>
  ├── IToggleControlObject<TScope>
  ├── IRangeControlObject<TScope>
  ├── ISelectorControlObject<TScope>
  ├── IEditableTextControlObject<TScope>
  └── ControlObjectBase<TScope>        ← Locator + Scope storage

Brinell.{Platform}/                    ← Platform-specific implementation
  ├── Interfaces/                      ← I{Platform}Element, I{Platform}Driver, I{Platform}Scope<T>
  ├── Context/                         ← {Platform}TestContext : ITestContext<I{Platform}Element>
  ├── Pages/                           ← PageObjectBase<TSelf> : I{Platform}Page<TSelf>
  ├── Controls/                        ← ControlBase<TScope> + all control types
  └── Testing/                         ← {Platform}TestFixtureBase
```

## 2. Key Design Decision: FlaUI Inline (No Separate Driver Projects)

The existing `srcnew/Brinell.Wpf.csproj` and `srcnew/Brinell.WinForms.csproj` already reference `FlaUI.Core` and `FlaUI.UIA3` directly. Since WPF/WinForms only support Windows (no Appium alternative), the FlaUI driver code can live **directly inside** each platform project — no separate `Brinell.Wpf.FlaUI` project needed.

This differs from MAUI, which has a separate `Brinell.Maui.FlaUI` project because MAUI also supports Android/iOS via Appium, requiring dynamic driver loading.

## 3. Interface Mapping: MAUI → WPF/WinForms

| MAUI | WPF | WinForms |
|------|-----|----------|
| `IMauiElement : IElement<IMauiElement>` | `IWpfElement : IElement<IWpfElement>` | `IWinFormsElement : IElement<IWinFormsElement>` |
| `IMauiDriver : IDriver<IMauiElement>` | `IWpfDriver : IDriver<IWpfElement>` | `IWinFormsDriver : IDriver<IWinFormsElement>` |
| `IMauiElementScope : IElementScope<IMauiElement>` | `IWpfElementScope : IElementScope<IWpfElement>` | `IWinFormsElementScope : IElementScope<IWinFormsElement>` |
| `IMauiScope<TScope>` | `IWpfScope<TScope>` | `IWinFormsScope<TScope>` |
| `IMauiPage<TSelf>` | `IWpfPage<TSelf>` | `IWinFormsPage<TSelf>` |
| `IMauiTestContext` | `IWpfTestContext` | `IWinFormsTestContext` |
| `FlaUIMauiElement` | `FlaUIWpfElement` | `FlaUIWinFormsElement` |
| `FlaUIMauiDriver` | `FlaUIWpfDriver` | `FlaUIWinFormsDriver` |

## 4. Control Mapping: Old → New

### WPF Controls (13)

| Old (src/) | Old Base | New Base in srcnew/ | Core Interface |
|---|---|---|---|
| ButtonControl | ContentControlBase | ClickableControlBase<TScope> | IClickableControlObject |
| CheckBoxControl | ToggleControlBase | ToggleControlBase<TScope> | IToggleControlObject |
| ComboBoxControl | SelectorControlBase | SelectorControlBase<TScope> | ISelectorControlObject |
| LabelControl | ControlBase | ControlBase<TScope> | IControlObject |
| ListBoxControl | ItemsControlBase | SelectorControlBase<TScope> | ISelectorControlObject |
| MessageBoxDialog | PageBase | Special — extends PageBase | IPageObject |
| PasswordBoxControl | TextControlBase | EditableTextControlBase<TScope> | IEditableTextControlObject |
| ProgressBarControl | RangeControlBase | RangeControlBase<TScope> | IRangeControlObject |
| ScrollViewControl | ControlBase | ControlBase<TScope> + IScrollable | Custom |
| SliderControl | RangeControlBase | RangeControlBase<TScope> | IRangeControlObject |
| TabItemControl | ContentControlBase | ClickableControlBase<TScope> | IClickableControlObject + SelectionItem |
| TextBoxControl | TextControlBase | EditableTextControlBase<TScope> | IEditableTextControlObject |
| TreeViewControl | ItemsControlBase | ControlBase<TScope> | Custom tree interface |

### WinForms Controls (16)

| Old (src/) | Old Base | New Base in srcnew/ | Core Interface |
|---|---|---|---|
| ButtonControl | ContentControlBase | ClickableControlBase<TScope> | IClickableControlObject |
| CheckBoxControl | ToggleControlBase | ToggleControlBase<TScope> | IToggleControlObject |
| ComboBoxControl | SelectorControlBase | SelectorControlBase<TScope> | ISelectorControlObject |
| DataGridViewControl | ItemsControlBase | ControlBase<TScope> | Custom grid interface |
| DateTimePickerControl | ControlBase | ControlBase<TScope> | Custom time interface |
| GroupBoxControl | ControlBase | ContainerBase<TParent,TSelf> | IContainerControl |
| LabelControl | ControlBase | ControlBase<TScope> | IControlObject |
| ListBoxControl | ItemsControlBase | SelectorControlBase<TScope> | ISelectorControlObject |
| NumericUpDownControl | RangeControlBase | RangeControlBase<TScope> | IRangeControlObject |
| PasswordBoxControl | TextControlBase | EditableTextControlBase<TScope> | IEditableTextControlObject |
| ProgressBarControl | RangeControlBase | RangeControlBase<TScope> | IRangeControlObject |
| RadioButtonControl | ToggleControlBase | ToggleControlBase<TScope> | IToggleControlObject |
| RichTextBoxControl | TextControlBase | EditableTextControlBase<TScope> | IEditableTextControlObject |
| TabControlControl | ItemsControlBase | ControlBase<TScope> | Custom tab interface |
| TextBoxControl | TextControlBase | EditableTextControlBase<TScope> | IEditableTextControlObject |
| TrackBarControl | RangeControlBase | RangeControlBase<TScope> | IRangeControlObject |

## 5. FlaUI Element Implementation Pattern

The `FlaUIMauiElement` (which we replicate per-platform) wraps an `AutomationElement` and provides:

1. **State**: Visible, Enabled, Selected, Text (via UIA Value → RangeValue → Name)
2. **Actions**: Click (Invoke → fallback Mouse.Click), SendKeys (Focus+Type / SetValue), Clear
3. **UIA Patterns**: RangeValue, Toggle, ExpandCollapse, SelectionItem, ScrollItem, Scroll, Window
4. **Child finding**: FindFirstDescendant/FindAllDescendants → wrap in new element instances
5. **Attributes**: Map `name`/`automationid`/`className`/`controltype`/`enabled`/`visible` to UIA properties

WPF/WinForms elements can simplify by removing MAUI-specific workarounds (Switch visibility hack, Android UIAutomator, context switching).

## 6. Context/Driver Lifecycle

For WPF/WinForms, the driver lifecycle is simpler than MAUI:

1. **Launch** — `FlaUI.Core.Application.Launch(path, args)` or `Application.Attach(process)`
2. **Get main window** — `app.GetMainWindow(automation)`
3. **Root element** — the main `Window` (AutomationElement)
4. **Element finding** — `rootElement.FindFirstDescendant(condition)` with `LocatorExtensions.ToCondition()`
5. **Cleanup** — `app.Close()` / `automation.Dispose()`

No Appium, no device sessions, no context switching, no platform enum.

## 7. Files to Create per Platform

### Brinell.Wpf (srcnew/)

| Directory | Files to Create |
|-----------|----------------|
| Interfaces/ | `IWpfElement.cs`, `IWpfDriver.cs`, `IWpfElementScope.cs`, `IWpfScope.cs`, `IWpfPage.cs`, `IWpfTestContext.cs`, `IRangePatternElement.cs`, `IExpandCollapsePatternElement.cs` |
| Context/ | `WpfTestContext.cs`, `WpfTestContextOptions.cs` (replace Placeholder.cs) |
| Pages/ | `PageObjectBase.cs` (replace Placeholder.cs) |
| Controls/ | `ControlBase.cs`, `ClickableControlBase.cs`, `ToggleControlBase.cs`, `EditableTextControlBase.cs`, `RangeControlBase.cs`, `SelectorControlBase.cs` (bases) + 13 concrete controls (replace Placeholder.cs) |
| Testing/ | `WpfTestFixtureBase.cs` (replace Placeholder.cs) |
| FlaUI/ | `FlaUIWpfDriver.cs`, `FlaUIWpfElement.cs`, `LocatorExtensions.cs` |
| Root | `GlobalUsings.cs`, `ObjectBase.cs` |

### Brinell.WinForms (srcnew/) — same structure with WinForms naming + 16 controls
