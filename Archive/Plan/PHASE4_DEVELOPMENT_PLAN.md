# Brinell WinForms Framework - Phase 4 Development Plan

**Created**: January 2, 2026  
**Scope**: Enhanced base classes, virtual methods, expanded controls  
**Duration**: Estimated 3-4 phases

---

## Part 1: Quick Wins & Priorities

### Priority 1A: Fix ComboBox Async Selection (Blocker)
**Goal**: Enable 8 currently skipped tests  
**Effort**: Medium (2-3 hours)  
**Steps**:
1. Add explicit wait for selection state change in ComboBoxControl.SelectByText()
2. Implement `Application.DoEvents()` refresh pattern
3. Add timeout/retry logic for async selection
4. Test against all 3 frameworks (net8.0, net9.0, net10.0)
5. Re-enable skipped tests

**Success Criteria**:
- All 17 tests pass on net8.0-windows
- No hanging or process issues
- Same results on net9.0 and net10.0

### Priority 1B: Cross-Framework Validation
**Goal**: Verify consistency across .NET targets  
**Effort**: Low (1 hour)  
**Steps**:
1. Run full test suite on net9.0-windows
2. Run full test suite on net10.0-windows
3. Document any framework-specific issues
4. Create test matrix in documentation

**Success Criteria**:
- 17/17 tests pass on all three frameworks
- No framework-specific workarounds needed

---

## Part 2: Base Class Hierarchy Analysis

### Current WinForms Control Hierarchy

```
Control (Base)
├── ButtonBase
│   ├── Button
│   ├── CheckBox
│   └── RadioButton
├── TextBoxBase
│   └── TextBox
├── ListControl
│   ├── ListBox
│   └── ComboBox
├── Label
├── ProgressBar
├── ScrollableControl
│   ├── Panel
│   └── Form
├── DataGridView
├── TreeView
├── TabControl
├── ToolStripItem
└── PictureBox
```

### Proposed Brinell Base Class Structure

**Tier 0: Core (ControlBase - exists)**
```
ControlBase
  - Click()
  - DoubleClick()
  - RightClick()
  - GetText()
  - SetText()
  - IsVisible / IsEnabled / IsExists
  - WaitVisible / WaitEnabled / WaitExists
  - AssertVisible / AssertEnabled / AssertDisplayed
```

**Tier 1: Input Controls (NEW)**
```
InputControlBase : ControlBase
  - Clear()
  - GetText() [override]
  - SetText() [override]
  - AppendText()
  - IsReadOnly()
  - GetTextLength()
```

**Tier 2: Toggle Controls (NEW)**
```
ToggleControlBase : InputControlBase
  - IsChecked() : bool
  - SetChecked(bool)
  - Check()
  - Uncheck()
  - WaitChecked()
  - AssertChecked()
  - AssertUnchecked()
```

**Tier 3: Selector Controls (NEW)**
```
SelectorControlBase : ControlBase
  - GetSelectedItem() : string
  - GetSelectedIndex() : int
  - GetItems() : IReadOnlyList<string>
  - GetItemCount() : int
  - SelectByText(string)
  - SelectByIndex(int)
  - WaitSelected(string)
  - AssertSelectedItem(string)
```

**Tier 4: Specialized**
```
TextBoxControl : InputControlBase
  - [existing methods]

CheckBoxControl : ToggleControlBase
  - [existing methods]

RadioButtonControl : ToggleControlBase
  - [existing methods]

ComboBoxControl : SelectorControlBase
  - [existing methods + async fix]

ListBoxControl : SelectorControlBase
  - SelectMultiple(string[])
  - GetSelectedItems() : IReadOnlyList<string>
  - WaitMultipleSelected(string[])

ProgressBarControl : ControlBase
  - GetValue() : int
  - GetMaximum() : int
  - WaitForValue(int)
  - AssertValue(int)

DataGridViewControl : SelectorControlBase
  - GetRowCount() : int
  - GetColumnCount() : int
  - GetCell(int, int) : string
  - SelectRow(int)
  - GetSelectedRow() : int

TreeViewControl : SelectorControlBase
  - ExpandNode(string)
  - CollapseNode(string)
  - SelectNode(string)
  - GetNodes() : IReadOnlyList<string>

TabControlControl : SelectorControlBase
  - SelectTab(int)
  - SelectTab(string)
  - GetTabs() : IReadOnlyList<string>
  - GetSelectedTab() : string

SliderControl : ControlBase
  - GetValue() : int
  - SetValue(int)
  - GetMinimum() : int
  - GetMaximum() : int
  - WaitForValue(int)
```

---

## Part 3: Virtual Methods Inventory

### Core Virtual Methods (In ControlBase - exists)
```csharp
public virtual void Click()
public virtual void DoubleClick()
public virtual void RightClick()
public virtual string GetText()
public virtual void SetText(string text)
public virtual void AssertTextEquals(string expected)
public virtual void AssertTextContains(string expected)
```

### InputControlBase Virtual Methods (NEW)
```csharp
public virtual void Clear()
public virtual void AppendText(string text)
public virtual bool IsReadOnly()
public virtual int GetTextLength()
```

### ToggleControlBase Virtual Methods (NEW)
```csharp
public virtual bool IsChecked()
public virtual void SetChecked(bool value)
public virtual void Check()
public virtual void Uncheck()
public virtual bool WaitChecked(bool value, int? timeoutMs = null)
public virtual void AssertChecked()
public virtual void AssertUnchecked()
```

### SelectorControlBase Virtual Methods (NEW)
```csharp
public virtual string GetSelectedItem()
public virtual int GetSelectedIndex()
public virtual IReadOnlyList<string> GetItems()
public virtual int GetItemCount()
public virtual void SelectByText(string text)
public virtual void SelectByIndex(int index)
public virtual bool WaitSelected(string item, int? timeoutMs = null)
public virtual void AssertSelectedItem(string expected)
```

### Design Principles for Virtual Methods
1. **Async-Aware**: All selection/state methods include optional timeout
2. **Consistent Naming**: Is/Wait/Check/Assert pattern everywhere
3. **Type-Safe**: Return appropriate types (bool, int, string, IReadOnlyList)
4. **Override-Friendly**: Virtual with default implementation in base
5. **Cross-Platform Ready**: Methods don't assume WinForms-specific APIs
6. **Logging Integrated**: All methods call LogAction/LogAssertPass

### Future Technology Support (WPF/Stride)
These virtual methods will work with:
- **WPF**: Replace FlaUI with UIAutomation or WPF-specific APIs
- **Stride**: Implement UI navigation with input system
- **Web**: Implement with Selenium/Playwright

All use same virtual method signatures → same test code works everywhere!

---

## Part 4: Implementation Roadmap

### Phase 4.1: Base Class Infrastructure (2 days)
**Deliverables**:
- `InputControlBase.cs` (abstract)
- `ToggleControlBase.cs` (abstract)
- `SelectorControlBase.cs` (abstract)
- Update `ControlBase` with additional virtual methods
- Unit tests for base class method hierarchy

**Files to Create**:
```
src/Brinell.WinForms/Controls/Base/
├── InputControlBase.cs (NEW)
├── ToggleControlBase.cs (NEW)
├── SelectorControlBase.cs (NEW)
└── ControlBase.cs (MODIFY)
```

### Phase 4.2: Input Controls (1 day)
**Refactor Existing**:
- TextBoxControl: Inherit from InputControlBase

**Implement New**:
- PasswordBoxControl: Inherit from InputControlBase
- NumericUpDownControl: Inherit from InputControlBase
- RichTextBoxControl: Inherit from InputControlBase

**Test Coverage**:
- LoginPage updated to use all three input types
- New test methods for each control

### Phase 4.3: Toggle Controls (1 day)
**Refactor Existing**:
- CheckBoxControl: Inherit from ToggleControlBase
- RadioButtonControl: Inherit from ToggleControlBase

**Implement New**:
- ToggleSwitchControl: Inherit from ToggleControlBase (if available)

**Test Coverage**:
- CheckBox tests (already exist, enhance)
- RadioButton tests (new)
- Toggle state validation tests

### Phase 4.4: Selector Controls (2 days)
**Fix Blocker**:
- ComboBoxControl: Async selection fix + inherit from SelectorControlBase

**Refactor Existing**:
- ListBoxControl: Inherit from SelectorControlBase
- (Optional) DataGridViewControl: Inherit from SelectorControlBase

**Implement New**:
- TreeViewControl: Inherit from SelectorControlBase
- TabControlControl: Inherit from SelectorControlBase

**Test Coverage**:
- ComboBox async tests (8 currently skipped)
- ListBox multi-select tests
- TreeView navigation tests
- TabControl switching tests

### Phase 4.5: Specialized Controls (1.5 days)
**Implement**:
- ProgressBarControl: Inherit from ControlBase
- SliderControl: Inherit from ControlBase
- PictureBoxControl: Inherit from ControlBase (if needed)

**Test Coverage**:
- Progress bar value tracking
- Slider value validation
- Image rendering (if applicable)

### Phase 4.6: Cross-Technology Abstraction (Optional, Future)
**For WPF**:
- Create WPF-specific base classes using same virtual method signatures
- Test code remains unchanged!

**For Stride**:
- Create Stride UI base classes
- Adapt virtual methods for game engine context

---

## Part 5: Testing Strategy

### Unit Tests
```
Tests/Controls/
├── Base/
│   ├── InputControlBaseTests.cs
│   ├── ToggleControlBaseTests.cs
│   └── SelectorControlBaseTests.cs
└── Implementations/
    ├── TextBoxControlTests.cs
    ├── ComboBoxControlTests.cs
    ├── CheckBoxControlTests.cs
    ├── ListBoxControlTests.cs
    ├── TreeViewControlTests.cs
    └── TabControlTests.cs
```

### Integration Tests (Sample App)
**Enhanced SampleApp MainForm**:
- TextBox + PasswordBox + NumericUpDown
- CheckBox + RadioButton + ToggleSwitch
- ComboBox + ListBox + TreeView + TabControl
- ProgressBar + Slider
- DataGridView

**Test Collection**:
- LoginPageTests (enhanced with all control types)
- AdvancedLoginTests (workflow with all controls)
- NEW: ToggleControlTests
- NEW: SelectorControlTests
- NEW: ProgressBarTests

### Test Matrix
```
Framework    | Input | Toggle | Selector | Progress | Result
-------------|-------|--------|----------|----------|--------
net8.0       | ✅    | ✅     | 🔧 (fix) | ✅       | WIP
net9.0       | ✅    | ✅     | 🔧 (fix) | ✅       | Pending
net10.0      | ✅    | ✅     | 🔧 (fix) | ✅       | Pending
WPF (future) | 📋    | 📋     | 📋       | 📋       | Planned
```

---

## Part 6: Implementation Sequence

**Week 1**:
1. **Day 1-2**: Create base classes (InputControlBase, ToggleControlBase, SelectorControlBase)
2. **Day 3**: Fix ComboBox async selection issue
3. **Day 4-5**: Refactor existing controls to use new base classes

**Week 2**:
1. **Day 1-2**: Implement new controls (TreeView, TabControl, Slider, etc.)
2. **Day 3-4**: Cross-framework validation (net9.0, net10.0)
3. **Day 5**: Documentation and cleanup

---

## Part 7: Success Criteria

### Code Quality
- ✅ 0 compilation errors across all frameworks
- ✅ DRY principle: No duplicated Click/DoubleClick/RightClick implementations
- ✅ All virtual methods have consistent patterns
- ✅ Base classes cover 80%+ of common control operations

### Test Coverage
- ✅ 17/17 base tests passing (net8.0)
- ✅ 17/17 tests passing on net9.0 and net10.0
- ✅ New control tests: 40+ new test methods
- ✅ 0 flaky tests (no hanging, no state pollution)

### Framework Readiness
- ✅ Same virtual methods work across WinForms/WPF/Stride (by design)
- ✅ Page Object Model fully demonstrated
- ✅ Fixture-based testing established
- ✅ Documentation complete (600+ lines)

### Documentation
- ✅ Updated control hierarchy diagram
- ✅ Virtual method reference guide
- ✅ Control implementation checklist
- ✅ Example tests for each control type

---

## Appendix: File Structure After Phase 4

```
Brinell.WinForms/
├── Controls/
│   ├── Base/
│   │   ├── ControlBase.cs (MODIFIED)
│   │   ├── InputControlBase.cs (NEW)
│   │   ├── ToggleControlBase.cs (NEW)
│   │   └── SelectorControlBase.cs (NEW)
│   ├── ButtonControl.cs
│   ├── TextBoxControl.cs (MODIFIED)
│   ├── PasswordBoxControl.cs (NEW)
│   ├── NumericUpDownControl.cs (NEW)
│   ├── RichTextBoxControl.cs (NEW)
│   ├── CheckBoxControl.cs (MODIFIED)
│   ├── RadioButtonControl.cs (MODIFIED)
│   ├── ToggleSwitchControl.cs (NEW - if available)
│   ├── ComboBoxControl.cs (MODIFIED - async fix)
│   ├── ListBoxControl.cs
│   ├── TreeViewControl.cs (NEW)
│   ├── TabControlControl.cs (NEW)
│   ├── ProgressBarControl.cs (NEW)
│   ├── SliderControl.cs (NEW)
│   ├── DataGridViewControl.cs (MODIFIED - if enhanced)
│   ├── LabelControl.cs
│   └── PictureBoxControl.cs (NEW - optional)
├── Infrastructure/
│   ├── FlaUITestContext.cs
│   ├── FlaUIDriverAdapter.cs
│   ├── FlaUIElementAdapter.cs
│   └── FlaUIScreenshotService.cs
└── Documentation/
    ├── CONTROL_HIERARCHY.md (NEW)
    ├── VIRTUAL_METHODS_GUIDE.md (NEW)
    └── IMPLEMENTATION_CHECKLIST.md (NEW)

Tests/Brinell.Samples.WinForms.UITests/
├── Pages/
│   ├── LoginPage.cs (MODIFIED - more controls)
│   ├── CheckBoxPage.cs (NEW)
│   ├── ProgressPage.cs (NEW)
│   └── TreeViewPage.cs (NEW)
├── Tests/
│   ├── LoginPageTests.cs (MODIFIED)
│   ├── ToggleControlTests.cs (NEW)
│   ├── SelectorControlTests.cs (NEW)
│   ├── InputControlTests.cs (NEW)
│   ├── ProgressBarTests.cs (NEW)
│   └── TreeViewTests.cs (NEW)
├── Fixtures/
│   └── AppFixture.cs
└── SampleApp/
    └── MainForm.cs (MODIFIED - more controls)
```

---

**This plan provides:**
1. ✅ Quick wins (ComboBox fix, cross-framework validation)
2. ✅ Well-organized base class hierarchy (matching WinForms structure)
3. ✅ Extensible virtual methods (usable across technologies)
4. ✅ Clear implementation sequence
5. ✅ Comprehensive test coverage
6. ✅ Foundation for future WPF/Stride integration
