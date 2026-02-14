# PLAN-002: Specification Levels for Control Objects

**Version:** 1.0
**Created:** January 6, 2026
**Status:** Draft

---

## 1. Overview

This plan defines how to create specifications incrementally using SPX V7 specification blocks. While architecture is complete from day one (see [PLAN-001](PLAN-001-Architecture-Creation.md)), specifications are created in levels to:

1. Validate understanding of the spec format early
2. Get feedback before writing all specifications
3. Ensure base hierarchies work before adding more controls
4. Minimize rework by proving patterns first

### Key Principle

> **Each level adds code, never refactors.**

If Level 1 is correct, Level 2 only adds more controls. No changes to existing specifications, interfaces, or base classes.

---

## 2. SPX V7 Specification Blocks to Use

Based on [SPX V7 Block Index](../../SPX/Docs/V7/_Index2.md):

| SPX Block         | Code | Purpose for Brinell                |
| ----------------- | ---- | ---------------------------------- |
| 250 specification | SPC  | Main specification container       |
| 251 behavior      | BHV  | What the control does              |
| 252 boundary      | BND  | Edge cases, limits, error handling |
| 253 acceptance    | ACC  | Testable acceptance criteria       |
| 254 assumption    | ASM  | Preconditions and dependencies     |
| 255 exclusion     | EXC  | Explicit out-of-scope items        |

**Note:** SPX V7 uses 150 for specifications. Brinell uses **250** to place specifications after architecture (200).

---

## 3. Level Structure

### Level 0: Foundation (Prerequisites)

**Not control specifications** — Foundation layer that must exist first.

| Specification         | Purpose                                     |
| --------------------- | ------------------------------------------- |
| IControlObject Base   | State methods, wait methods, assert methods |
| IPageObject           | Page container definition                   |
| Context/Configuration | Test context, timeouts, logging             |

#### Complete Interface Hierarchy Definition

The interface hierarchy must support all controls across MAUI, Blazor, and WPF platforms:

```
IControlObject                      # Base for all controls
│
├── IClickableControlObject         # Click capability (Button, Link, Image)
│   └── ILongPressControlObject     # Long press (mobile-specific)
│
├── ITextControlObject              # Text display (Label, Span)
│   └── IEditableTextControlObject  # Text input (Entry, TextArea)
│
├── IToggleControlObject            # On/off state (CheckBox, Switch, RadioButton)
│
├── ISelectorControlObject          # Single selection (Picker, Select, ComboBox)
│   └── IMultiSelectorControlObject # Multi-selection (ListBox)
│
├── IRangeControlObject             # Numeric range (Slider, ProgressBar, Stepper)
│
├── IContainerControlObject         # Child scoping (Frame, Grid, Panel)
│   └── IWindowControlObject        # Window/Dialog (Modal, Popup)
│
├── IItemsControlObject             # Item enumeration (ListView, CollectionView, Table)
│   └── IDataGridControlObject      # Row/cell access (DataGrid)
│
├── IScrollableControlObject        # Scrolling (ScrollView, ScrollViewer)
│
├── IDateTimeControlObject          # Date/time (DatePicker, TimePicker)
│
└── IWebViewControlObject           # Web content (WebView, IFrame)
```

**Interface Coverage by Platform:**

| Interface | MAUI Controls | Blazor Controls | WPF Controls |
|-----------|---------------|-----------------|---------------|
| IClickableControlObject | Button, ImageButton, Image | button, a, img | Button, Image |
| ITextControlObject | Label | span, label, p | Label, TextBlock |
| IEditableTextControlObject | Entry, Editor, SearchBar | input[text], textarea | TextBox, RichTextBox |
| IToggleControlObject | CheckBox, Switch, RadioButton | input[checkbox], input[radio] | CheckBox, RadioButton, ToggleButton |
| ISelectorControlObject | Picker | select | ComboBox |
| IMultiSelectorControlObject | — | select[multiple] | ListBox |
| IRangeControlObject | Slider, Stepper, ProgressBar | input[range] | Slider, ProgressBar |
| IContainerControlObject | Frame, Grid, StackLayout, ScrollView | div, section, form | Panel, Grid, StackPanel |
| IItemsControlObject | ListView, CollectionView | ul, ol, table | ListView, ListBox, ItemsControl |
| IScrollableControlObject | ScrollView | div[overflow] | ScrollViewer |
| IDateTimeControlObject | DatePicker, TimePicker | input[date], input[time] | DatePicker |
| IWebViewControlObject | WebView | iframe | WebBrowser |

**Interface Member Categories (each interface must define):**

- **State methods:** `IsExists()`, `IsVisible()`, `IsEnabled()` → `bool`
- **Wait methods:** `WaitExists(bool?, int?)`, `WaitVisible(bool?, int?)`, `WaitEnabled(bool?, int?)` → `bool`
- **Assert methods:** `AssertExists(bool?, string?, int?)`, `AssertVisible(bool?, string?, int?)`, `AssertEnabled(bool?, string?, int?)` → `void`
- **Capability-specific methods:** Defined per interface (e.g., `Click()` for IClickableControlObject)

#### Complete Base Class Hierarchy Definition

Each platform implements its own base class hierarchy. Base classes use the Template Method pattern to define algorithms while delegating platform-specific operations to abstract methods.

##### MAUI Base Class Hierarchy (Brinell.Maui)

```
MauiControlBase                    # Implements IControlObject, uses AppiumElement
├── MauiClickableControlBase       # Implements IClickableControlObject
│   └── MauiLongPressControlBase   # Implements ILongPressControlObject
├── MauiTextControlBase            # Implements ITextControlObject
│   └── MauiEditableTextControlBase # Implements IEditableTextControlObject
├── MauiToggleControlBase          # Implements IToggleControlObject
├── MauiSelectorControlBase        # Implements ISelectorControlObject
├── MauiRangeControlBase           # Implements IRangeControlObject
├── MauiContainerControlBase       # Implements IContainerControlObject
├── MauiItemsControlBase           # Implements IItemsControlObject
├── MauiScrollableControlBase      # Implements IScrollableControlObject
└── MauiDateTimeControlBase        # Implements IDateTimeControlObject
```

##### Blazor Base Class Hierarchy (Brinell.Blazor)

```
BlazorControlBase                  # Implements IControlObject, uses IWebElement
├── BlazorClickableControlBase     # Implements IClickableControlObject
├── BlazorTextControlBase          # Implements ITextControlObject
│   └── BlazorEditableTextControlBase # Implements IEditableTextControlObject
├── BlazorToggleControlBase        # Implements IToggleControlObject
├── BlazorSelectorControlBase      # Implements ISelectorControlObject
│   └── BlazorMultiSelectorControlBase # Implements IMultiSelectorControlObject
├── BlazorRangeControlBase         # Implements IRangeControlObject
├── BlazorContainerControlBase     # Implements IContainerControlObject
│   └── BlazorWindowControlBase    # Implements IWindowControlObject (Modals)
├── BlazorItemsControlBase         # Implements IItemsControlObject
│   └── BlazorTableControlBase     # Implements IDataGridControlObject
├── BlazorScrollableControlBase    # Implements IScrollableControlObject
└── BlazorDateTimeControlBase      # Implements IDateTimeControlObject
```

##### WPF Base Class Hierarchy (Brinell.Wpf)

```
WpfControlBase                     # Implements IControlObject, uses AutomationElement
├── WpfClickableControlBase        # Implements IClickableControlObject
├── WpfTextControlBase             # Implements ITextControlObject
│   └── WpfEditableTextControlBase # Implements IEditableTextControlObject
├── WpfToggleControlBase           # Implements IToggleControlObject
├── WpfSelectorControlBase         # Implements ISelectorControlObject
│   └── WpfMultiSelectorControlBase # Implements IMultiSelectorControlObject
├── WpfRangeControlBase            # Implements IRangeControlObject
├── WpfContainerControlBase        # Implements IContainerControlObject
│   └── WpfWindowControlBase       # Implements IWindowControlObject
├── WpfItemsControlBase            # Implements IItemsControlObject
│   └── WpfDataGridControlBase     # Implements IDataGridControlObject
├── WpfScrollableControlBase       # Implements IScrollableControlObject
└── WpfDateTimeControlBase         # Implements IDateTimeControlObject
```

**Base Class Implementation Patterns:**

- **Template Method:** Base defines algorithm, abstract methods for platform-specific parts
- **Nullable Skip Pattern:** Nullable parameters skip action when null (`if (expected is null) return;`)
- **Logging Integration:** Actions are logged via `_context.Logger.LogAction()`
- **Timeout Inheritance:** Methods use `_context.Timeouts.DefaultWait` when timeout not specified
- **Platform Element Wrapper:** Each base class wraps platform-specific element type (AppiumElement, IWebElement, AutomationElement)

#### Platform Test Context Interfaces

Platform-specific context interfaces enable type-safe control creation:

```
ITestContext (Core)
├── IMauiTestContext    → AppiumDriver, AppiumElement
├── IBlazorTestContext  → IWebDriver, IWebElement
└── IWpfTestContext     → FlaUI AutomationElement
```

**ITestContext Required Members:**

- `TimeoutSettings Timeouts { get; }`
- `ITestLogger Logger { get; }`
- `void NavigateTo(string destination)`
- `void NavigateBack()`
- `byte[] TakeScreenshot()`
- `void ResetAppState()`

**Platform Context Required Members:**

- `TElement FindElement(Locator locator)` — Find single element
- `TElement? TryFindElement(Locator locator)` — Find or return null
- `IReadOnlyList<TElement> FindElements(Locator locator)` — Find multiple elements
- `TDriver Driver { get; }` — Platform-specific driver access

### Level 1: Core Controls (5 Controls)

**Goal:** Validate entire pattern with minimal controls.

| Control       | Interface(s)         | Why Selected                      |
| ------------- | -------------------- | --------------------------------- |
| Button        | IClickableControl    | Simplest interaction - click only |
| Label         | ITextControl         | Read-only text - no input         |
| Entry/TextBox | IEditableTextControl | Text input - extends ITextControl |
| CheckBox      | IToggleControl       | Binary state toggle               |
| Container     | IContainerControl    | Scoping mechanism                 |

**Why these 5:**

- Button validates click flow
- Label validates read flow
- Entry validates inheritance (ITextControl → IEditableTextControl)
- CheckBox validates toggle pattern
- Container validates scoping pattern

**If Level 1 works:** Base classes are correct, interface hierarchy is correct.

### Level 2: Selection Controls (3 Controls)

| Control           | Interface(s)          | Why Selected                             |
| ----------------- | --------------------- | ---------------------------------------- |
| Dropdown/ComboBox | ISelectorControl      | Single selection from list               |
| ListBox           | IMultiSelectorControl | Multi-selection extends ISelectorControl |
| RadioGroup        | ISelectorControl      | Alternative selection pattern            |

**What Level 2 validates:**

- Selection abstraction works
- Multi-selection extends properly
- Different selection patterns supported

### Level 3: Advanced Controls (4 Controls)

| Control    | Interface(s)                         | Why Selected               |
| ---------- | ------------------------------------ | -------------------------- |
| Slider     | IRangeControl                        | Numeric range input        |
| DatePicker | ISelectorControl + ITextControl      | Composite control          |
| DataGrid   | ICollectionControl                   | Collection with rows/cells |
| Tab        | IContainerControl + ISelectorControl | Navigation container       |

**What Level 3 validates:**

- Range control pattern
- Multiple interface implementation
- Collection patterns
- Complex composite controls

### Level 4: Platform-Specific Controls

| Control      | Platform    | Why                    |
| ------------ | ----------- | ---------------------- |
| Switch       | MAUI        | Mobile-specific toggle |
| CarouselView | MAUI        | Mobile collection      |
| Modal        | Blazor      | Web dialog pattern     |
| Toast        | MAUI/Blazor | Notification pattern   |

### Level 5: Remaining Controls

All remaining controls as documented in existing requirements.

---

## 4. Specification Folder Structure

```
specs2/
├── 250_specifications/
│   ├── 250_INDEX.md                    # Specification index
│   │
│   ├── 250_000_Foundation/             # Level 0: Foundation specifications
│   │   ├── 250_000_INDEX.md            # Foundation index
│   │   ├── 250_001_IControlObject.spx.md      # Base interface specification
│   │   ├── 250_002_IPageObject.spx.md         # Page object interface
│   │   ├── 250_003_IContainerScope.spx.md     # Container scoping interface
│   │   ├── 250_004_TestContext.spx.md         # Test context specification
│   │   ├── 250_005_InterfaceHierarchy.spx.md  # Complete interface hierarchy (all platforms)
│   │   ├── 250_006_MauiBaseClasses.spx.md     # MAUI base class hierarchy
│   │   ├── 250_007_BlazorBaseClasses.spx.md   # Blazor base class hierarchy
│   │   ├── 250_008_WpfBaseClasses.spx.md      # WPF base class hierarchy
│   │   └── 250_009_PlatformContexts.spx.md    # Platform-specific contexts
│   │
│   ├── 250_100_CoreControls/           # Level 1: Core control specifications
│   │   ├── 250_100_INDEX.md            # Core controls index
│   │   ├── 250_101_Button.spx.md
│   │   ├── 250_102_Label.spx.md
│   │   ├── 250_103_Entry.spx.md
│   │   ├── 250_104_CheckBox.spx.md
│   │   └── 250_105_Container.spx.md
│   │
│   ├── 250_200_SelectionControls/      # Level 2: Selection control specifications
│   │   ├── 250_200_INDEX.md            # Selection controls index
│   │   ├── 250_201_Dropdown.spx.md
│   │   ├── 250_202_ListBox.spx.md
│   │   └── 250_203_RadioGroup.spx.md
│   │
│   ├── 250_300_AdvancedControls/       # Level 3: Advanced control specifications
│   │   ├── 250_300_INDEX.md            # Advanced controls index
│   │   ├── 250_301_Slider.spx.md
│   │   ├── 250_302_DatePicker.spx.md
│   │   ├── 250_303_DataGrid.spx.md
│   │   └── 250_304_Tab.spx.md
│   │
│   ├── 250_400_PlatformSpecific/       # Level 4: Platform-specific specifications
│   │   ├── 250_400_INDEX.md            # Platform-specific index
│   │   ├── 250_401_Switch.spx.md       # MAUI
│   │   ├── 250_402_CarouselView.spx.md # MAUI
│   │   ├── 250_403_Modal.spx.md        # Blazor
│   │   └── 250_404_Toast.spx.md        # All
│   │
│   └── 250_500_Remaining/              # Level 5: Remaining specifications
│       ├── 250_500_INDEX.md            # Remaining controls index
│       └── ...                         # Additional controls as needed
```

---

## 5. Specification Template

Each control specification follows SPX V7 format:

```markdown
# specification ButtonControl
- **id**: SPC-100
- **level**: 1
- **requirement**: FR-100
- **interfaces**: IControlObject, IClickableControl

## behavior
1. Button displays text label
2. Button can be clicked to trigger action
3. Button has enabled/disabled state
4. Button supports automation ID for location

## boundary
- Click on disabled button does nothing (no error)
- Click on hidden button waits for visibility (with timeout)
- Double-click is distinct from single-click
- Focus does not trigger click

## acceptance
- Given a visible enabled button, clicking triggers action
- Given a disabled button, clicking returns without error
- Given a hidden button, click waits then succeeds when visible
- Given automation ID, button is located correctly

## assumption
- Underlying automation library supports click action
- Element has accessible automation ID or locator

## exclusion
- Long-press gestures (Level 4 mobile-specific)
- Drag operations from button
```

---

## 6. Level Progression Rules

### Gate Criteria for Level Advancement

Before advancing to next level:

| Gate                             | Requirement                                |
| -------------------------------- | ------------------------------------------ |
| Specifications Complete          | All specs for current level written        |
| Review Passed                    | Specifications reviewed and approved       |
| Implementation Verified          | Controls implemented match spec            |
| Sample App Updated               | Controls added to sample apps (FR-950)     |
| ControlObject Unit Tests Passing | Unit tests with mocks pass (FR-960)        |
| Framework Unit Tests Passing     | Infrastructure tests pass (FR-961)         |
| UI Tests Passing                 | UI tests against sample apps pass (FR-970) |
| No Base Changes                  | Base classes unchanged (only extended)     |

### If Base Changes Required

If Level N requires base class changes:

1. **STOP** — Do not proceed to Level N+1
2. **Analyze** — Why did base need changing?
3. **Fix Architecture** — Update architecture docs if needed
4. **Re-validate** — Run all previous level tests
5. **Document** — Update ADR with learning

---

## 7. Execution Timeline

### Week 1: Level 0 Foundation

| Day | Task                                                                            |
| --- | ------------------------------------------------------------------------------- |
| 1   | Write IControlObject specification (250_001)                                    |
| 2   | Write IPageObject + IContainerScope specifications (250_002, 250_003)           |
| 3   | Write TestContext specification (250_004)                                       |
| 4   | Write InterfaceHierarchy specification (250_005)                                |
| 5   | Write MAUI/Blazor/WPF BaseClass specifications (250_006, 250_007, 250_008)      |
| 6   | Write PlatformContexts specification (250_009), review and finalize Level 0     |

### Week 2: Level 1 Core Controls

| Day | Task                                      |
| --- | ----------------------------------------- |
| 1   | Write Button specification                |
| 2   | Write Label + Entry specifications        |
| 3   | Write CheckBox + Container specifications |
| 4   | Review all Level 1 specifications         |
| 5   | Implementation validation                 |

### Week 3: Level 2 Selection Controls

| Day | Task                                               |
| --- | -------------------------------------------------- |
| 1-2 | Write Dropdown, ListBox, RadioGroup specifications |
| 3   | Review Level 2 specifications                      |
| 4-5 | Implementation validation                          |

### Week 4+: Levels 3-5

Continue pattern: Write → Review → Validate → Gate Check → Next Level

---

## 8. Validation Strategy

### Per-Specification Validation

Each specification must:

- [ ] Link to requirement (FR-xxx)
- [ ] Define all behaviors
- [ ] Document boundaries (edge cases)
- [ ] Have testable acceptance criteria
- [ ] List assumptions
- [ ] Explicitly exclude out-of-scope items

### Level 0 Specific Validation

Level 0 Foundation specifications must also:

- [ ] Complete interface hierarchy defined (all interfaces covering MAUI, Blazor, WPF)
- [ ] MAUI base class hierarchy defined (all MauiXxxBase classes)
- [ ] Blazor base class hierarchy defined (all BlazorXxxBase classes)
- [ ] WPF base class hierarchy defined (all WpfXxxBase classes)
- [ ] Platform context interfaces defined (IMauiTestContext, IBlazorTestContext, IWpfTestContext)
- [ ] Nullable skip pattern documented for all methods accepting nullable parameters
- [ ] Template method pattern documented for base class implementations
- [ ] Logging integration points documented
- [ ] Timeout inheritance pattern documented
- [ ] Platform element types documented (AppiumElement, IWebElement, AutomationElement)

### Per-Level Validation

Each level must:

- [ ] All specifications pass individual validation
- [ ] Base classes support all controls without modification
- [ ] Interface contracts unchanged from previous level
- [ ] Sample apps contain all controls for current level (FR-950)
- [ ] ControlObject unit tests created with mocks (FR-960)
- [ ] Framework infrastructure tests passing (FR-961)
- [ ] UI tests created against sample apps (FR-970)
- [ ] Existing tests still pass
- [ ] New tests pass for new controls

### Cross-Level Validation

After each level:

- [ ] Run all UI tests (all levels)
- [ ] Review for specification conflicts
- [ ] Update index documents
- [ ] Archive validation results

---

## 9. Risk Mitigation

| Risk                                 | Mitigation                                         |
| ------------------------------------ | -------------------------------------------------- |
| Base class needs changes at Level 2+ | Include one of each interface type in Level 1      |
| Interface changes required           | Define complete interfaces in architecture first   |
| Platform differences break pattern   | Include Container in Level 1 to test scoping early |
| Selection pattern issues             | Level 2 is only selection - isolated validation    |
| Complex controls don't fit           | Level 3 tests composition before quantity          |

---

## 10. Success Criteria

### Per Level

- Specifications written in SPX V7 format
- Specifications reviewed and approved
- Implementation matches specification
- No changes to previous level specifications
- No changes to base classes (only additions)

### Overall

- All controls specified through levels
- Architecture unchanged from initial definition
- Clear traceability: Requirement → Specification → Implementation → Test
- Pattern proven to work incrementally

---

## 11. Testing Requirements per Level

Each level requires corresponding testing artifacts per FR-950, FR-960, FR-961, and FR-970:

### Sample App Requirements (FR-950)

| Level   | Sample App Changes                                                   |
| ------- | -------------------------------------------------------------------- |
| Level 0 | No UI changes — foundation only                                     |
| Level 1 | Add Button, Label, Entry, CheckBox, Container to Basic Controls page |
| Level 2 | Add Dropdown, ListBox, RadioGroup to Selection Controls page         |
| Level 3 | Add Slider, DatePicker, DataGrid, Tab to Advanced Controls page      |
| Level 4 | Add platform-specific controls (Switch, Modal, etc.)                 |
| Level 5 | Add remaining controls as needed                                     |

### ControlObject Unit Test Requirements (FR-960)

| Level    | Unit Test Files                                                                                       |
| -------- | ----------------------------------------------------------------------------------------------------- |
| Level 0  | ControlObjectBaseTests, PageObjectBaseTests, TestContextTests                                         |
| Level 1  | ButtonControlTests, LabelControlTests, EntryControlTests, CheckBoxControlTests, ContainerControlTests |
| Level 2  | DropdownControlTests, ListBoxControlTests, RadioGroupControlTests                                     |
| Level 3  | SliderControlTests, DatePickerControlTests, DataGridControlTests, TabControlTests                     |
| Level 4+ | Platform-specific control tests                                                                       |

### Framework Infrastructure Unit Test Requirements (FR-961)

| Level   | Unit Test Files                                                                               |
| ------- | --------------------------------------------------------------------------------------------- |
| Level 0 | ControlLocatorTests, ByFactoryTests, LocatorChainingTests, ExceptionTests, ConfigurationTests |
| Level 0 | IControlObjectContractTests, IClickableControlContractTests, ITextControlContractTests        |
| Level 1 | IToggleControlContractTests, IContainerControlContractTests                                   |
| Level 2 | ISelectorControlContractTests, IMultiSelectorControlContractTests                             |
| Level 3 | IRangeControlContractTests, ICollectionControlContractTests                                   |
| All     | LocatorConversionTests (per technology)                                                       |

### UI Test Requirements (FR-970)

| Level    | UI Test Files                                                 |
| -------- | ------------------------------------------------------------- |
| Level 0  | ContextTests, NavigationTests                                 |
| Level 1  | BasicControlTests (Button, Label, Entry, CheckBox, Container) |
| Level 2  | SelectionControlTests (Dropdown, ListBox, RadioGroup)         |
| Level 3  | AdvancedControlTests (Slider, DatePicker, DataGrid, Tab)      |
| Level 4+ | Platform-specific UI tests                                    |

---

## 12. Control Inventory by Level

### Level 1 (5 Controls)

| # | Control   | Primary Interface    | Platform |
| - | --------- | -------------------- | -------- |
| 1 | Button    | IClickableControl    | All      |
| 2 | Label     | ITextControl         | All      |
| 3 | Entry     | IEditableTextControl | All      |
| 4 | CheckBox  | IToggleControl       | All      |
| 5 | Container | IContainerControl    | All      |

### Level 2 (3 Controls)

| # | Control    | Primary Interface     | Platform |
| - | ---------- | --------------------- | -------- |
| 6 | Dropdown   | ISelectorControl      | All      |
| 7 | ListBox    | IMultiSelectorControl | All      |
| 8 | RadioGroup | ISelectorControl      | All      |

### Level 3 (4 Controls)

| #  | Control    | Primary Interface  | Platform |
| -- | ---------- | ------------------ | -------- |
| 9  | Slider     | IRangeControl      | All      |
| 10 | DatePicker | ISelectorControl   | All      |
| 11 | DataGrid   | ICollectionControl | All      |
| 12 | Tab        | IContainerControl  | All      |

### Level 4 (Platform-Specific)

| #  | Control      | Primary Interface  | Platform |
| -- | ------------ | ------------------ | -------- |
| 13 | Switch       | IToggleControl     | MAUI     |
| 14 | CarouselView | ICollectionControl | MAUI     |
| 15 | Modal        | IContainerControl  | Blazor   |
| 16 | Toast        | ITextControl       | All      |

### Level 5 (Remaining)

All other controls from existing requirements.

---

## Related Documents

- [PLAN-001-Architecture-Creation](PLAN-001-Architecture-Creation.md) — Complete architecture plan
- [120_100_ControlObject](../100_requirements/120_functional/120_100_ControlObject.spx.md) — Control requirements
- [SPX V7 Specification Overview](../../SPX/Docs/V7/blocks2/150_specifications/15X_overview.md) — SPX uses 150, Brinell uses 250

### Testing Infrastructure Requirements

- [FR-950 Sample Applications](../100_requirements/120_functional/120_950_SampleApplications.spx.md) — Sample apps per technology
- [FR-960 Unit Tests](../100_requirements/120_functional/120_960_UnitTests.spx.md) — Unit tests for ControlObjects with mocks
- [FR-961 Unit Tests Framework](../100_requirements/120_functional/120_961_UnitTestsFramework.spx.md) — Unit tests for framework infrastructure
- [FR-970 UI Tests](../100_requirements/120_functional/120_970_UITests.spx.md) — UI integration tests
