# PLAN-009: MAUI Implementation - Small Testable Steps

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Overview

Phased implementation plan for MAUI sample app, interfaces, and control objects. Each phase produces testable, deployable artifacts.

---

## Phase 1: Foundation & Interface Setup

### 1.1 Create Interface Assembly
- [ ] Create `Brinell.Core.Interfaces` project
- [ ] Implement 6 core interfaces (IVisualElement, ILocatable, IStylable, IInteractive, IClickable, IGesturable)
- [ ] Add unit tests for interface contracts
- [ ] **Validate:** Core interfaces compile and tests pass

### 1.2 Implement Text Input Interfaces
- [ ] Create 4 text input interfaces (ITextInputControl, ITextSearchControl, IEditableTextControl, IValidatableTextControl)
- [ ] Add method signatures and XML documentation
- [ ] Create mock implementations for testing
- [ ] Add unit tests
- [ ] **Validate:** All interfaces defined, mocks functional

### 1.3 Implement Selection Interfaces
- [ ] Create 3 selection interfaces (ISingleSelectControl, IMultiSelectControl, ISelectableControl)
- [ ] Add mock implementations
- [ ] Add unit tests
- [ ] **Validate:** Selection interface contracts verified

### 1.4 Implement Remaining Specialized Interfaces
- [ ] Toggle interfaces (3)
- [ ] Range interfaces (3)
- [ ] Date/Time interfaces (3)
- [ ] Collection interfaces (6)
- [ ] Container interfaces (5)
- [ ] Display interfaces (5)
- [ ] Other specialized interfaces (9)
- [ ] **Validate:** All 57 interfaces implemented and mocked

---

## Phase 2: Base Classes & Generic Implementations

### 2.1 Create Generic Base Classes
- [ ] Create `GenericControl` base class
- [ ] Create `GenericTextInputControl` class
- [ ] Create `GenericSelectableControl` class
- [ ] Create `GenericToggleControl` class
- [ ] Add unit tests for each
- [ ] **Validate:** Base classes implement expected interfaces

### 2.2 Create Abstract Base Classes
- [ ] Create `MauiControlBase` abstract class
- [ ] Create control-specific base classes (TextControlBase, SelectableControlBase, etc.)
- [ ] Add unit tests
- [ ] **Validate:** Base classes ready for implementation

---

## Phase 3: MAUI Control Implementations (Batch 1 - High Priority)

### 3.1 Text Input Controls
- [ ] Implement EntryControl
- [ ] Implement EditorControl
- [ ] Implement SearchBarControl
- [ ] Unit tests for each control
- [ ] **Validate:** All three controls work, tests pass

### 3.2 Selection Controls
- [ ] Implement PickerControl
- [ ] Implement CollectionViewControl
- [ ] Unit tests
- [ ] **Validate:** Selection works correctly

### 3.3 Toggle Controls
- [ ] Implement SwitchControl
- [ ] Implement CheckBoxControl
- [ ] Implement RadioButtonControl
- [ ] Unit tests
- [ ] **Validate:** Toggle behavior verified

### 3.4 Button Control
- [ ] Implement ButtonControl
- [ ] Unit tests
- [ ] **Validate:** Click/tap functionality works

### Phase 3 Checkpoint
- [ ] Mark old Entry/Picker/Switch/CheckBox as `[Obsolete]`
- [ ] Create migration guide
- [ ] Update existing tests to use new controls
- [ ] **Validate:** All core controls functional

---

## Phase 4: MAUI Control Implementations (Batch 2 - Collections & Display)

### 4.1 Collection Controls
- [ ] Implement ListViewControl
- [ ] Implement CarouselViewControl
- [ ] Unit tests
- [ ] **Validate:** Collection rendering works

### 4.2 Display Controls
- [ ] Implement LabelControl
- [ ] Implement ImageControl
- [ ] Implement ImageButtonControl
- [ ] Unit tests
- [ ] **Validate:** Display controls render correctly

### 4.3 Layout Controls
- [ ] Implement GridControl
- [ ] Implement StackLayoutControl
- [ ] Implement FlexLayoutControl
- [ ] Unit tests
- [ ] **Validate:** Layouts position controls correctly

### Phase 4 Checkpoint
- [ ] Mark old ListView/Label/Image as `[Obsolete]`
- [ ] Create migration guide additions
- [ ] **Validate:** 20+ controls implemented

---

## Phase 5: MAUI Control Implementations (Batch 3 - Remaining)

### 5.1 Navigation & Container Controls
- [ ] Implement TabbedPageControl
- [ ] Implement ShellControl
- [ ] Implement FrameControl
- [ ] Implement BorderControl
- [ ] Implement ScrollViewControl
- [ ] Implement ExpanderControl
- [ ] Unit tests
- [ ] **Validate:** Navigation works between pages

### 5.2 Gesture & Advanced Controls
- [ ] Implement GestureRecognizerControl (base)
- [ ] Implement TapGestureControl
- [ ] Implement PanGestureControl
- [ ] Implement PinchGestureControl
- [ ] Implement SwipeViewControl
- [ ] Unit tests
- [ ] **Validate:** Gestures recognize correctly

### 5.3 Media & Web Controls
- [ ] Implement WebViewControl
- [ ] Implement MediaElementControl
- [ ] Implement GraphicsViewControl
- [ ] Unit tests
- [ ] **Validate:** Media controls functional

### Phase 5 Checkpoint
- [ ] Mark old TabbedPage/Shell/Frame as `[Obsolete]`
- [ ] **Validate:** 40+ controls implemented

---

## Phase 6: MAUI Sample App Implementation

### 6.1 Create Sample App Project
- [ ] Create `Brinell.Samples.Maui.App` project structure
- [ ] Create Shell with 6 pages structure
- [ ] Add navigation setup
- [ ] **Validate:** App compiles and runs

### 6.2 Implement Dashboard Page
- [ ] Create Dashboard.xaml with KPI cards
- [ ] Use new control implementations
- [ ] Connect to sample data
- [ ] Add tests
- [ ] **Validate:** Dashboard page loads and displays data

### 6.3 Implement User Form Page
- [ ] Create UserForm.xaml with all input controls
- [ ] Implement validation
- [ ] Connect to model
- [ ] Add tests
- [ ] **Validate:** Form submits and validates

### 6.4 Implement Data Grid Page
- [ ] Create DataGrid.xaml with CollectionView
- [ ] Add search, sort, pagination
- [ ] Add tests
- [ ] **Validate:** Grid displays and filters correctly

### 6.5 Implement File Upload Page
- [ ] Create FileUpload.xaml
- [ ] Implement file operations
- [ ] Add tests
- [ ] **Validate:** Files upload and display

### 6.6 Implement Navigation & Advanced Pages
- [ ] Create Navigation.xaml with buttons/expander
- [ ] Create Advanced.xaml with gestures
- [ ] Add tests
- [ ] **Validate:** All pages functional

### Phase 6 Checkpoint
- [ ] Sample app complete with all 6 pages
- [ ] All 66+ controls demonstrated
- [ ] **Validate:** Sample app runs without errors

---

## Phase 7: MAUI Test Implementation

### 7.1 Implement Minimal Test Suite
- [ ] Create TEST-001 test project
- [ ] Implement 23 tests from minimal set
- [ ] Run against sample app
- [ ] **Validate:** All 23 tests pass

### 7.2 Implement Full Test Suite
- [ ] Create TEST-002 test project
- [ ] Implement remaining 81 tests
- [ ] Run against sample app
- [ ] **Validate:** All 104 tests pass

### 7.3 Implement Integration Tests
- [ ] Create cross-page navigation tests
- [ ] Create form-to-grid workflow tests
- [ ] Create file upload workflow tests
- [ ] **Validate:** Integration tests pass

### Phase 7 Checkpoint
- [ ] 100+ tests implemented and passing
- [ ] **Validate:** Sample app fully tested

---

## Phase 8: Sample Data & Documentation

### 8.1 Create Sample Data
- [ ] Create fixture classes with mock data
- [ ] Create data builder patterns
- [ ] Use in sample app and tests
- [ ] **Validate:** Consistent test data

### 8.2 Update Documentation
- [ ] Create control usage guides
- [ ] Add API documentation
- [ ] Create migration guide from old controls
- [ ] Add troubleshooting section
- [ ] **Validate:** Documentation complete

### 8.3 Create Integration Guide
- [ ] Document how to use new controls in existing projects
- [ ] Create example implementations
- [ ] Add configuration guide
- [ ] **Validate:** Clear upgrade path

---

## Phase 9: Cleanup & Obsolescence

### 9.1 Mark Old Controls as Obsolete
- [ ] Add `[Obsolete("Use new EntryControl instead", false)]` to old classes
- [ ] Verify code still compiles
- [ ] Update compiler warnings in codebase
- [ ] **Validate:** Old code still works but warns

### 9.2 Create Migration Scripts
- [ ] Create find/replace patterns for old → new
- [ ] Create automated migration tool (if complex)
- [ ] Document manual migration steps
- [ ] **Validate:** Migration scripts work on sample projects

### 9.3 Final Validation
- [ ] All tests pass
- [ ] Sample app works
- [ ] Documentation complete
- [ ] Old code marked obsolete
- [ ] **Validate:** Ready for release

### 9.4 Plan Removal Phase (Future Release)
- [ ] Document removal schedule (next major version)
- [ ] Plan removal of old classes
- [ ] Create removal checklist
- [ ] **Validate:** Plan documented

---

## Summary

- **Total Phases:** 9
- **Implementation Controls:** 50+ MAUI controls
- **Tests:** 104+ test cases
- **Timeline:** 8-12 weeks (phased)
- **Deliverables:** 
  - Sample MAUI app
  - Test suite
  - Documentation
  - Migration guide

**Key Success Criteria:**
- ✓ Sample app compiles and runs
- ✓ All 104+ tests pass
- ✓ Old code marked obsolete (not removed)
- ✓ Documentation complete
- ✓ Migration path clear

