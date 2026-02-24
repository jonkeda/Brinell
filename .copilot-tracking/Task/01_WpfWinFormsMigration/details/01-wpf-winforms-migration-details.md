<!-- markdownlint-disable-file -->
# Implementation Details: Migrate Brinell.Wpf and Brinell.WinForms to srcnew

## Context Reference

Sources:
* .copilot-tracking/Task/01_WpfWinFormsMigration/research/02-wpf-winforms-migration-research.md — Architecture analysis, interface mapping, control mapping
* .copilot-tracking/Task/01_WpfWinFormsMigration/research/01-wpf-winforms-migration-research-brief.md — Locked decisions and scope
* srcnew/Brinell.Maui/ — Reference architecture (71+ files)
* srcnew/Brinell.Maui.FlaUI/ — FlaUI driver reference (5 files to adapt)

## Implementation Phase 1: WPF Platform Interfaces

<!-- parallelizable: false -->

### Step 1.1: Create WPF Interfaces

Create `srcnew/Brinell.Wpf/Interfaces/` directory with 8 interface files. Each mirrors its MAUI counterpart with `Wpf` naming.

Files:
* `srcnew/Brinell.Wpf/Interfaces/IWpfElement.cs` — `interface IWpfElement : IElement<IWpfElement>` — Add `GetDomAttribute`, `GetDomProperty` stubs (return null)
* `srcnew/Brinell.Wpf/Interfaces/IWpfDriver.cs` — `interface IWpfDriver : IDriver<IWpfElement>, IDiagnosticDriver` — Remove MauiPlatform, context switching, AndroidUIAutomator. Add WindowHandles, CurrentWindowHandle, NavigateTo, NavigateBack, TakeScreenshot
* `srcnew/Brinell.Wpf/Interfaces/IWpfElementScope.cs` — `interface IWpfElementScope : IElementScope<IWpfElement>` — Property: `IWpfTestContext Context { get; }`
* `srcnew/Brinell.Wpf/Interfaces/IWpfScope.cs` — `interface IWpfScope<TScope> : IWpfElementScope where TScope : IWpfScope<TScope>` — Property: `TScope Self { get; }`
* `srcnew/Brinell.Wpf/Interfaces/IWpfPage.cs` — `interface IWpfPage<TSelf> : IWpfScope<TSelf>, IPageObject<IWpfElement> where TSelf : IWpfPage<TSelf>`
* `srcnew/Brinell.Wpf/Interfaces/IWpfTestContext.cs` — `interface IWpfTestContext : ITestContext<IWpfElement>, IWpfElementScope`
* `srcnew/Brinell.Wpf/Interfaces/IRangePatternElement.cs` — `interface IRangePatternElement` — SupportsRangeValue, SetRangeValue, GetRangeValue, GetRangeMinimum, GetRangeMaximum, GetRangeSmallChange (source: Brinell.Maui.Interfaces)
* `srcnew/Brinell.Wpf/Interfaces/IExpandCollapsePatternElement.cs` — `interface IExpandCollapsePatternElement` — SupportsExpandCollapse, IsExpanded, Expand, Collapse, GetExpandedItems, SelectItemByText/Index, GetSelectedItemText (source: Brinell.Maui.Interfaces)

Success criteria:
* All 8 interface files created with correct namespaces (`Brinell.Wpf.Interfaces`)
* All interfaces reference `IWpfElement` instead of `IMauiElement`
* Namespace uses `Brinell.Wpf.Interfaces`

Context references:
* .copilot-tracking/Task/01_WpfWinFormsMigration/research/02-wpf-winforms-migration-research.md (Lines 37-49) — Interface mapping table

Dependencies:
* `srcnew/Brinell.Core/` must exist (already does)

### Step 1.2: Create GlobalUsings and ObjectBase

Files:
* `srcnew/Brinell.Wpf/GlobalUsings.cs` — Global usings for FlaUI.Core, FlaUI.Core.AutomationElements, FlaUI.Core.Conditions, FlaUI.UIA3, Brinell.Core, Brinell.Core.Locators, Brinell.Wpf.Interfaces
* `srcnew/Brinell.Wpf/ObjectBase.cs` — Abstract class with `abstract IWpfTestContext Context { get; }`, `DefaultTimeoutMs`, `PollingIntervalMs`, `Poll(Func<bool>, int)` method (mirrors `srcnew/Brinell.Maui/ObjectBase.cs`)

Success criteria:
* Both files compile
* ObjectBase provides the polling foundation for pages and controls

Context references:
* srcnew/Brinell.Maui/ObjectBase.cs — Reference implementation
* srcnew/Brinell.Maui.FlaUI/GlobalUsings.cs — Reference global usings

## Implementation Phase 2: WPF FlaUI Driver

<!-- parallelizable: false -->

### Step 2.1: Create WPF FlaUI Driver Files

Create `srcnew/Brinell.Wpf/FlaUI/` directory with 3 files adapted from `srcnew/Brinell.Maui.FlaUI/`.

Files:
* `srcnew/Brinell.Wpf/FlaUI/FlaUIWpfDriver.cs` — `sealed class FlaUIWpfDriver : IWpfDriver, IDisposable`
  * 3 constructors: by HWND, by executable path, by Process (same as FlaUIMauiDriver)
  * Internal ConditionFactory, Automation properties
  * FindElement/FindElements/TryFindElement with polling via WaitHelper
  * GetScreenshot via Capture.Element
  * GetPageSource/GetAutomationTree diagnostics
  * Quit/Close/Dispose
  * Remove: Platform property, ContextSwitching, FindByAndroidUIAutomator, ExecuteScript, ResetAppState
  * Keep: CurrentWindowHandle, WindowHandles, NavigateBack (Alt+Left), EnsureRootWindowFocused

* `srcnew/Brinell.Wpf/FlaUI/FlaUIWpfElement.cs` — `sealed class FlaUIWpfElement : IWpfElement, IRangePatternElement, IExpandCollapsePatternElement`
  * Wraps `AutomationElement` + back-reference to `FlaUIWpfDriver`
  * State: Visible (remove MAUI Switch workaround), Enabled, Selected, Text
  * Actions: Click (Invoke → fallback Mouse.Click), SendKeys, Clear
  * UIA Patterns: RangeValue, Toggle, ExpandCollapse, SelectionItem, ScrollItem, Scroll
  * GetAttribute mapping (name, automationid, className, controltype, enabled, visible)
  * Child finding: FindFirstDescendant/FindAllDescendants → wrap in FlaUIWpfElement
  * Remove: MAUI-specific Submit, DomAttribute/DomProperty (return null)

* `srcnew/Brinell.Wpf/FlaUI/LocatorExtensions.cs` — `static class LocatorExtensions`
  * Extension method `Locator.ToCondition(ConditionFactory)` — identical to Maui.FlaUI version
  * Maps LocatorStrategy → FlaUI ConditionBase
  * ParseControlType for friendly string → ControlType enum

Success criteria:
* All 3 FlaUI files compile
* Driver can launch/attach to WPF apps
* Element wraps all needed UIA patterns
* No MAUI-specific code remains

Context references:
* srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs — Reference driver
* srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs — Reference element
* srcnew/Brinell.Maui.FlaUI/LocatorExtensions.cs — Reference locator

Dependencies:
* Phase 1 completion (interfaces must exist)
* FlaUI.Core and FlaUI.UIA3 packages (already in csproj)

## Implementation Phase 3: WPF Context and Pages

<!-- parallelizable: false -->

### Step 3.1: Create WPF Test Context

Delete `srcnew/Brinell.Wpf/Context/Placeholder.cs`. Create:

Files:
* `srcnew/Brinell.Wpf/Context/WpfTestContext.cs` — `class WpfTestContext : IWpfTestContext, IDisposable`
  * Constructor takes `WpfTestContextOptions`
  * Creates `FlaUIWpfDriver` from options (executable path, process, or window handle)
  * Implements `IElementScope<IWpfElement>` by delegating to driver
  * TakeScreenshot, SaveScreenshot, NavigateTo (throws), NavigateBack, Refresh
  * Timeouts, Logger
  * IDisposable: disposes driver

* `srcnew/Brinell.Wpf/Context/WpfTestContextOptions.cs` — `class WpfTestContextOptions`
  * Properties: ExecutablePath, Arguments, ProcessName, ProcessId, WindowHandle
  * Properties: Timeouts, Logger, Driver (for injection/mocking)

Success criteria:
* WpfTestContext creates FlaUIWpfDriver from options
* Implements full IWpfTestContext interface
* Element finding delegates to driver

Context references:
* srcnew/Brinell.Maui/Context/MauiTestContext.cs — Reference implementation
* src/Brinell.FlaUI/FlaUITestContext.cs — Old implementation patterns

Dependencies:
* Phase 2 completion (FlaUI driver must exist)

### Step 3.2: Create WPF Page Object Base

Delete `srcnew/Brinell.Wpf/Pages/Placeholder.cs`. Create:

Files:
* `srcnew/Brinell.Wpf/Pages/PageObjectBase.cs` — `abstract class PageObjectBase<TSelf> : ObjectBase, IWpfPage<TSelf> where TSelf : PageObjectBase<TSelf>`
  * CRTP: `TSelf Self => (TSelf)this;`
  * Constructor takes `IWpfTestContext`
  * Implements IPageObject: Name, IsLoaded, WaitLoaded, AssertLoaded, GetTitle, WaitTitle, AssertTitle, TakeScreenshot
  * Implements IElementScope: DefaultLocatorStrategy, Page, IsReady, WaitReady
  * Implements IWpfElementScope: Context property, TryFindElement, FindElement, FindElements
  * Factory methods for all WPF control types: Button(), CheckBox(), ComboBox(), Label(), ListBox(), PasswordBox(), ProgressBar(), ScrollView(), Slider(), TabItem(), TextBox(), TreeView()
  * Each factory returns `ControlType<TSelf>` for fluent chaining

Success criteria:
* PageObjectBase compiles
* Factory methods return appropriate control types
* CRTP pattern enables `page.TextBox("Name").Enter("text").Button("Save").Click()` fluent chain

Context references:
* srcnew/Brinell.Maui/Pages/PageObjectBase.cs — Reference implementation
* src/Brinell.FlaUI/Controls/Base/PageBase.cs — Old page patterns

Dependencies:
* Phase 2 completion (driver) and Phase 1 (interfaces)

## Implementation Phase 4: WPF Control Base Classes

<!-- parallelizable: false -->

### Step 4.1: Create WPF Control Base Classes

Delete `srcnew/Brinell.Wpf/Controls/Placeholder.cs`. Create 6 base class files:

Files:
* `srcnew/Brinell.Wpf/Controls/ControlBase.cs` — `class ControlBase<TScope> : ControlObjectBase<TScope>, IControlObject<TScope>`
  * Constructor: `(IWpfScope<TScope> scope, Locator locator)` and `(IWpfScope<TScope> scope, string locatorValue)`
  * Properties: ContainingScope → scope.Self, WpfScope, Context
  * Is/Wait/Assert for Exists, Visible, Enabled, Text, TextContains
  * GetAttribute
  * RunWithElement pattern (find → scroll into view → execute → log)
  * Poll/PollWithElement helpers
  * Nullable skip pattern: `if (expected == null) return true/scope`
  * ~600 lines, adapted from srcnew/Brinell.Maui/Controls/ControlBase.cs

* `srcnew/Brinell.Wpf/Controls/ClickableControlBase.cs` — `class ClickableControlBase<TScope> : ControlBase<TScope>, IClickableControlObject<TScope>`
  * Click(), DoubleClick(), RightClick(), Hover(), LongPress()
  * Each returns TScope for fluent chaining
  * Click uses element.Click(), others use element.DoubleClick(), etc.

* `srcnew/Brinell.Wpf/Controls/ToggleControlBase.cs` — `class ToggleControlBase<TScope> : ClickableControlBase<TScope>, IToggleControlObject<TScope>`
  * IsChecked(), Toggle(), Check(), Uncheck(), SetChecked()
  * WaitChecked(), AssertChecked()
  * Uses UIA Toggle pattern for state detection

* `srcnew/Brinell.Wpf/Controls/EditableTextControlBase.cs` — `class EditableTextControlBase<TScope> : ControlBase<TScope>, IEditableTextControlObject<TScope>`
  * Enter(), Clear(), SetText(), Append()
  * IsReadOnly(), GetPlaceholder()
  * Uses element.SendKeys() and element.Clear()

* `srcnew/Brinell.Wpf/Controls/RangeControlBase.cs` — `class RangeControlBase<TScope> : ControlBase<TScope>, IRangeControlObject<TScope>`
  * GetValue(), SetValue(), GetMinimum(), GetMaximum(), GetStep()
  * Increment(), Decrement()
  * WaitValue(), AssertValue()
  * Casts element to IRangePatternElement for UIA RangeValue pattern

* `srcnew/Brinell.Wpf/Controls/SelectorControlBase.cs` — `class SelectorControlBase<TScope> : ClickableControlBase<TScope>, ISelectorControlObject<TScope>`
  * SelectByText(), SelectByIndex(), SelectByValue()
  * GetSelectedText(), GetSelectedIndex()
  * GetItemTexts(), GetItemCount()
  * Casts element to IExpandCollapsePatternElement for ComboBox expand/collapse

Success criteria:
* All 6 base classes compile
* Each implements the correct Core interface with TScope
* RunWithElement pattern provides consistent element finding + scrolling
* Nullable skip pattern on all Wait/Assert methods

Context references:
* srcnew/Brinell.Maui/Controls/ControlBase.cs — Reference ControlBase (~600 lines)
* srcnew/Brinell.Maui/Controls/ClickableControlBase.cs — Reference
* srcnew/Brinell.Maui/Controls/ToggleControlBase.cs — Reference
* src/Brinell.FlaUI/Controls/Base/ — Old base classes for WPF-specific patterns

Dependencies:
* Phase 3 completion (context and pages)

## Implementation Phase 5: WPF Concrete Controls

<!-- parallelizable: false -->

### Step 5.1: Create 13 WPF Controls

Create individual files in `srcnew/Brinell.Wpf/Controls/`:

Files:
* `Button.cs` — `class Button<TScope> : ClickableControlBase<TScope>` — Click via Invoke pattern. Port from `src/Brinell.Wpf/Controls/ButtonControl.cs`
* `CheckBox.cs` — `class CheckBox<TScope> : ToggleControlBase<TScope>` — IsChecked via AsCheckBox, Toggle. Port from `src/Brinell.Wpf/Controls/CheckBoxControl.cs`
* `ComboBox.cs` — `class ComboBox<TScope> : SelectorControlBase<TScope>` — Open/Close dropdown, ExpandCollapse. Port from `src/Brinell.Wpf/Controls/ComboBoxControl.cs`
* `Label.cs` — `class Label<TScope> : ControlBase<TScope>` — GetText, read-only. Port from `src/Brinell.Wpf/Controls/LabelControl.cs`
* `ListBox.cs` — `class ListBox<TScope> : SelectorControlBase<TScope>` — SelectByText/Index, GetItems. Port from `src/Brinell.Wpf/Controls/ListBoxControl.cs`
* `MessageBoxDialog.cs` — `class MessageBoxDialog<TScope> : ControlBase<TScope>` — ClickYes/No/Ok/Cancel, GetMessage. Port from `src/Brinell.Wpf/Controls/MessageBoxDialog.cs` (simplify — was based on PageBase)
* `PasswordBox.cs` — `class PasswordBox<TScope> : EditableTextControlBase<TScope>` — Keyboard input for secure fields. Port from `src/Brinell.Wpf/Controls/PasswordBoxControl.cs`
* `ProgressBar.cs` — `class ProgressBar<TScope> : RangeControlBase<TScope>` — GetPercentage, IsIndeterminate. Port from `src/Brinell.Wpf/Controls/ProgressBarControl.cs`
* `ScrollView.cs` — `class ScrollView<TScope> : ControlBase<TScope>` — ScrollTo/Up/Down/Left/Right, scroll state. Port from `src/Brinell.Wpf/Controls/ScrollViewControl.cs`
* `Slider.cs` — `class Slider<TScope> : RangeControlBase<TScope>` — Thin wrapper, inherits range behavior. Port from `src/Brinell.Wpf/Controls/SliderControl.cs`
* `TabItem.cs` — `class TabItem<TScope> : ClickableControlBase<TScope>` — Select via SelectionItemPattern, IsSelected. Port from `src/Brinell.Wpf/Controls/TabItemControl.cs`
* `TextBox.cs` — `class TextBox<TScope> : EditableTextControlBase<TScope>` — Standard text input. Port from `src/Brinell.Wpf/Controls/TextBoxControl.cs`
* `TreeView.cs` — `class TreeView<TScope> : ControlBase<TScope>` — SelectNode/ExpandNode/CollapseNode by path. Port from `src/Brinell.Wpf/Controls/TreeViewControl.cs`

Success criteria:
* All 13 control files created and compile
* Each extends the correct base class
* Methods return TScope for fluent chaining
* WPF-specific UIA patterns preserved from old controls

Context references:
* src/Brinell.Wpf/Controls/ — All 13 old control files
* .copilot-tracking/Task/01_WpfWinFormsMigration/research/02-wpf-winforms-migration-research.md (Lines 51-71) — WPF control mapping table

Dependencies:
* Phase 4 completion (base classes)

## Implementation Phase 6: WPF Testing Base

<!-- parallelizable: false -->

### Step 6.1: Create WPF Test Fixture Base

Delete `srcnew/Brinell.Wpf/Testing/Placeholder.cs`. Create:

Files:
* `srcnew/Brinell.Wpf/Testing/WpfTestFixtureBase.cs` — `abstract class WpfTestFixtureBase : IAsyncLifetime`
  * Creates WpfTestContext from env vars or constructor options
  * Abstract: GetDefaultAppPath()
  * InitializeAsync: launch app, create context
  * DisposeAsync: close app, dispose context
  * Provides Context property for test classes

Success criteria:
* WpfTestFixtureBase compiles
* xUnit IAsyncLifetime pattern for setup/teardown
* Matches MauiTestFixtureBase pattern

Context references:
* srcnew/Brinell.Maui/Testing/MauiTestFixtureBase.cs — Reference
* src/Brinell.Wpf/Testing/WpfUITestBase.cs — Old implementation

Dependencies:
* Phase 3 completion (context)

## Implementation Phase 7: WinForms Platform Interfaces

<!-- parallelizable: false -->

### Step 7.1: Create WinForms Interfaces

Create `srcnew/Brinell.WinForms/Interfaces/` directory with 8 interface files. Mirror WPF interfaces with `WinForms` naming.

Files:
* `srcnew/Brinell.WinForms/Interfaces/IWinFormsElement.cs` — `interface IWinFormsElement : IElement<IWinFormsElement>`
* `srcnew/Brinell.WinForms/Interfaces/IWinFormsDriver.cs` — `interface IWinFormsDriver : IDriver<IWinFormsElement>, IDiagnosticDriver`
* `srcnew/Brinell.WinForms/Interfaces/IWinFormsElementScope.cs` — `interface IWinFormsElementScope : IElementScope<IWinFormsElement>`
* `srcnew/Brinell.WinForms/Interfaces/IWinFormsScope.cs` — `interface IWinFormsScope<TScope> : IWinFormsElementScope`
* `srcnew/Brinell.WinForms/Interfaces/IWinFormsPage.cs` — `interface IWinFormsPage<TSelf> : IWinFormsScope<TSelf>, IPageObject<IWinFormsElement>`
* `srcnew/Brinell.WinForms/Interfaces/IWinFormsTestContext.cs` — `interface IWinFormsTestContext : ITestContext<IWinFormsElement>, IWinFormsElementScope`
* `srcnew/Brinell.WinForms/Interfaces/IRangePatternElement.cs` — Same as WPF version
* `srcnew/Brinell.WinForms/Interfaces/IExpandCollapsePatternElement.cs` — Same as WPF version

Success criteria:
* All 8 interface files created with `Brinell.WinForms.Interfaces` namespace
* Structurally identical to WPF interfaces with naming substitution

Context references:
* srcnew/Brinell.Wpf/Interfaces/ — WPF reference (created in Phase 1)

Dependencies:
* srcnew/Brinell.Core/ must exist (already does)

### Step 7.2: Create WinForms GlobalUsings and ObjectBase

Files:
* `srcnew/Brinell.WinForms/GlobalUsings.cs` — Same pattern as WPF with `Brinell.WinForms.Interfaces`
* `srcnew/Brinell.WinForms/ObjectBase.cs` — Same pattern as WPF with `IWinFormsTestContext`

Success criteria:
* Both files compile

Dependencies:
* Step 7.1 completion

## Implementation Phase 8: WinForms FlaUI Driver

<!-- parallelizable: false -->

### Step 8.1: Create WinForms FlaUI Driver Files

Create `srcnew/Brinell.WinForms/FlaUI/` with 3 files. Mirror WPF FlaUI driver with WinForms naming.

Files:
* `srcnew/Brinell.WinForms/FlaUI/FlaUIWinFormsDriver.cs` — Same as FlaUIWpfDriver with WinForms interface/element types
* `srcnew/Brinell.WinForms/FlaUI/FlaUIWinFormsElement.cs` — Same as FlaUIWpfElement with WinForms interface types
* `srcnew/Brinell.WinForms/FlaUI/LocatorExtensions.cs` — Identical to WPF version

Success criteria:
* All 3 files compile with WinForms naming
* Functionally identical to WPF FlaUI driver

Dependencies:
* Phase 7 completion (interfaces)

## Implementation Phase 9: WinForms Context, Pages, Controls, Testing

<!-- parallelizable: false -->

### Step 9.1: Create WinForms Test Context

Delete `srcnew/Brinell.WinForms/Context/Placeholder.cs`. Create:

Files:
* `srcnew/Brinell.WinForms/Context/WinFormsTestContext.cs` — Same pattern as WpfTestContext with WinForms naming
* `srcnew/Brinell.WinForms/Context/WinFormsTestContextOptions.cs` — Same pattern as WpfTestContextOptions

Success criteria:
* WinFormsTestContext creates FlaUIWinFormsDriver from options
* Implements full IWinFormsTestContext interface

Dependencies:
* Phase 8 completion (driver)

### Step 9.2: Create WinForms Page Object Base

Delete `srcnew/Brinell.WinForms/Pages/Placeholder.cs`. Create:

Files:
* `srcnew/Brinell.WinForms/Pages/PageObjectBase.cs` — Same pattern as WPF with WinForms naming + factory methods for 16 WinForms control types

Success criteria:
* PageObjectBase compiles with all 16 control factory methods

Dependencies:
* Step 9.1 completion

### Step 9.3: Create WinForms Control Bases + 16 Concrete Controls

Delete `srcnew/Brinell.WinForms/Controls/Placeholder.cs`. Create:

Base classes (6 files — mirror WPF):
* `ControlBase.cs`, `ClickableControlBase.cs`, `ToggleControlBase.cs`, `EditableTextControlBase.cs`, `RangeControlBase.cs`, `SelectorControlBase.cs`

Concrete controls (16 files):
* `Button.cs` — `ClickableControlBase<TScope>` — Port from `src/Brinell.WinForms/Controls/ButtonControl.cs`
* `CheckBox.cs` — `ToggleControlBase<TScope>` — Port from CheckBoxControl.cs
* `ComboBox.cs` — `SelectorControlBase<TScope>` — Port from ComboBoxControl.cs (WinForms uses Click to open, not Expand)
* `DataGridView.cs` — `ControlBase<TScope>` — Port from DataGridViewControl.cs (GetRowCount, GetCellValue, SelectRow, etc.)
* `DateTimePicker.cs` — `ControlBase<TScope>` — Port from DateTimePickerControl.cs (arrow-key segment navigation)
* `GroupBox.cs` — `ControlBase<TScope>` — Port from GroupBoxControl.cs (container with CreateChild)
* `Label.cs` — `ControlBase<TScope>` — Port from LabelControl.cs
* `ListBox.cs` — `SelectorControlBase<TScope>` — Port from ListBoxControl.cs
* `NumericUpDown.cs` — `RangeControlBase<TScope>` — Port from NumericUpDownControl.cs (Spinner pattern)
* `PasswordBox.cs` — `EditableTextControlBase<TScope>` — Port from PasswordBoxControl.cs
* `ProgressBar.cs` — `RangeControlBase<TScope>` — Port from ProgressBarControl.cs
* `RadioButton.cs` — `ToggleControlBase<TScope>` — Port from RadioButtonControl.cs (Check only, no Uncheck)
* `RichTextBox.cs` — `EditableTextControlBase<TScope>` — Port from RichTextBoxControl.cs
* `TabControl.cs` — `ControlBase<TScope>` — Port from TabControlControl.cs (SelectTab, GetSelectedTab)
* `TextBox.cs` — `EditableTextControlBase<TScope>` — Port from TextBoxControl.cs
* `TrackBar.cs` — `RangeControlBase<TScope>` — Port from TrackBarControl.cs

Success criteria:
* All 22 files (6 bases + 16 controls) compile
* WinForms-specific patterns preserved (ComboBox Click-to-open, DataGridView, DateTimePicker segments)

Context references:
* src/Brinell.WinForms/Controls/ — All 16 old control files
* .copilot-tracking/Task/01_WpfWinFormsMigration/research/02-wpf-winforms-migration-research.md (Lines 73-95) — WinForms control mapping table

Dependencies:
* Step 9.2 completion (page base must exist for scope types)

### Step 9.4: Create WinForms Test Fixture Base

Delete `srcnew/Brinell.WinForms/Testing/Placeholder.cs`. Create:

Files:
* `srcnew/Brinell.WinForms/Testing/WinFormsTestFixtureBase.cs` — Same pattern as WpfTestFixtureBase

Success criteria:
* WinFormsTestFixtureBase compiles with IAsyncLifetime pattern

Dependencies:
* Step 9.1 completion (context)

## Implementation Phase 10: Tests and Samples

<!-- parallelizable: false -->

### Step 10.1: Port WPF Sample UI Tests

Port 3 test classes + 3 page objects + 1 test base from `samples/Brinell.Samples.Wpf.UITests/` to `testsnew/Brinell.Wpf.UITests/`.

Files to create:
* `testsnew/Brinell.Wpf.UITests/PageObjects/HomePage.cs` — Port, use new `PageObjectBase<HomePage>` with new fluent API
* `testsnew/Brinell.Wpf.UITests/PageObjects/LoginPage.cs` — Port
* `testsnew/Brinell.Wpf.UITests/PageObjects/ShellPage.cs` — Port
* `testsnew/Brinell.Wpf.UITests/TestBase/WpfSampleTestBase.cs` — Port, use `WpfTestFixtureBase`
* `testsnew/Brinell.Wpf.UITests/Tests/IsBusyTests.cs` — Port, adapt to fluent TScope API
* `testsnew/Brinell.Wpf.UITests/Tests/LoginTests.cs` — Port
* `testsnew/Brinell.Wpf.UITests/Tests/NavigationTests.cs` — Port

Success criteria:
* All test files compile against srcnew/ framework
* Tests use new fluent API: `page.TextBox("Username").Enter("admin").Button("Login").Click()`

Context references:
* samples/Brinell.Samples.Wpf.UITests/ — Source files to port
* testsnew/Brinell.Wpf.UITests/GlobalUsings.cs — Already imports Brinell.Wpf.Testing

Dependencies:
* Phase 6 completion (WPF framework complete)

### Step 10.2: Port WinForms Sample UI Tests

Port 5 test classes + 1 page object + 1 fixture from `samples/Brinell.Samples.WinForms.UITests/` to `testsnew/Brinell.WinForms.UITests/`.

Files to create:
* `testsnew/Brinell.WinForms.UITests/Fixtures/AppFixture.cs` — Port, use WinFormsTestFixtureBase
* `testsnew/Brinell.WinForms.UITests/Pages/LoginPage.cs` — Port, use PageObjectBase<LoginPage>
* `testsnew/Brinell.WinForms.UITests/Tests/LoginPageTests.cs` — Port, adapt to fluent API
* `testsnew/Brinell.WinForms.UITests/Tests/AdvancedLoginTests.cs` — Port
* `testsnew/Brinell.WinForms.UITests/Tests/ContainerControlTests.cs` — Port
* `testsnew/Brinell.WinForms.UITests/Tests/DateTimePickerTests.cs` — Port
* `testsnew/Brinell.WinForms.UITests/Tests/InputControlTests.cs` — Port

Success criteria:
* All test files compile against srcnew/ framework
* Tests use new fluent API

Context references:
* samples/Brinell.Samples.WinForms.UITests/ — Source files to port

Dependencies:
* Phase 9 completion (WinForms framework complete)

### Step 10.3: Update Sample Project References

Update sample test project csproj files to reference `srcnew/` instead of `src/`.

Files to modify:
* `samples/Brinell.Samples.Wpf.UITests/Brinell.Samples.Wpf.UITests.csproj` — Change ProjectReference from src/ to srcnew/
* `samples/Brinell.Samples.WinForms.UITests/Brinell.Samples.WinForms.UITests.csproj` — Change ProjectReference from src/ to srcnew/

Success criteria:
* Sample projects reference srcnew/ path
* May require namespace/API updates in sample code to match new fluent API

Dependencies:
* Steps 10.1 and 10.2 completion

## Implementation Phase 11: Validation

<!-- parallelizable: false -->

### Step 11.1: Run full solution build

Execute all validation commands:
* `dotnet build srcnew/Brinell.sln` — Zero errors, zero warnings

### Step 11.2: Run test project builds

* `dotnet build testsnew/Brinell.Wpf.Tests/Brinell.Wpf.Tests.csproj`
* `dotnet build testsnew/Brinell.Wpf.UITests/Brinell.Wpf.UITests.csproj`
* `dotnet build testsnew/Brinell.WinForms.Tests/Brinell.WinForms.Tests.csproj`
* `dotnet build testsnew/Brinell.WinForms.UITests/Brinell.WinForms.UITests.csproj`

### Step 11.3: Fix minor validation issues

Iterate on build errors and warnings. Apply fixes when corrections are straightforward.

### Step 11.4: Report blocking issues

When validation failures require changes beyond minor fixes:
* Document the issues and affected files
* Provide the user with next steps
* Recommend additional research and planning

## Dependencies

* .NET SDK 10.0 preview (`global.json`)
* FlaUI.Core + FlaUI.UIA3 NuGet packages (already in csproj)
* Brinell.Core project (srcnew/) — generic interfaces and ControlObjectBase<TScope>
* xUnit v3 — test project framework

## Success Criteria

* `dotnet build srcnew/Brinell.sln` — zero errors, zero warnings
* `srcnew/Brinell.Wpf/` — 30+ files: 8 interfaces, 3 FlaUI, 2 context, 1 page, 6 control bases, 13 controls, 1 testing, 2 root
* `srcnew/Brinell.WinForms/` — 33+ files: 8 interfaces, 3 FlaUI, 2 context, 1 page, 6 control bases, 16 controls, 1 testing, 2 root
* All test projects in testsnew/ build successfully
* No references to old src/Brinell.FlaUI from srcnew/ projects
