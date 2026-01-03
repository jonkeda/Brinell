# PLAN-010: Blazor Implementation - Small Testable Steps

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Overview

Phased implementation plan for Blazor sample app, interfaces, and control objects. Each phase produces testable, deployable artifacts. Follows MAUI implementation.

---

## Phase 1: Reuse Core Interfaces from MAUI

### 1.1 Add Blazor-Specific Interfaces
- [ ] Create `Brinell.Blazor.Core.Interfaces` project
- [ ] Reference shared interfaces from MAUI
- [ ] Add Blazor-specific interface variants if needed
- [ ] Create mock Blazor component implementations
- [ ] Add unit tests
- [ ] **Validate:** Blazor interfaces defined and mocked

---

## Phase 2: Blazor Component Base Classes

### 2.1 Create Blazor Base Classes
- [ ] Create `BlazorComponentBase` abstract class
- [ ] Create `BlazorFormComponentBase` class
- [ ] Create `BlazorDataDisplayComponentBase` class
- [ ] Create `BlazorContainerComponentBase` class
- [ ] Add unit tests
- [ ] **Validate:** Base classes implement expected patterns

### 2.2 Create Blazor Control Adapters
- [ ] Create wrapper classes for InputText → ITextInputControl
- [ ] Create wrapper classes for InputSelect → ISelectControl
- [ ] Create wrapper classes for InputCheckbox → IToggleControl
- [ ] Add unit tests
- [ ] **Validate:** Adapters correctly wrap Blazor components

---

## Phase 3: Blazor Form Component Implementations (Batch 1)

### 3.1 Text Input Components
- [ ] Implement InputTextControl wrapper
- [ ] Implement InputTextAreaControl wrapper
- [ ] Implement SearchInputControl wrapper
- [ ] Add validation support
- [ ] Unit tests
- [ ] **Validate:** Text components work with validation

### 3.2 Selection Components
- [ ] Implement InputSelectControl wrapper
- [ ] Implement InputCheckboxControl wrapper
- [ ] Implement InputRadioControl wrapper
- [ ] Add binding support
- [ ] Unit tests
- [ ] **Validate:** Selection controls bind correctly

### 3.3 Number & Date Components
- [ ] Implement InputNumberControl wrapper
- [ ] Implement InputDateControl wrapper
- [ ] Implement InputDateRangeControl wrapper
- [ ] Unit tests
- [ ] **Validate:** Number/date components work

### Phase 3 Checkpoint
- [ ] 10 form components wrapped
- [ ] **Validate:** All form components functional

---

## Phase 4: Blazor Layout & Navigation Components (Batch 2)

### 4.1 Layout Components
- [ ] Implement MainLayoutControl wrapper
- [ ] Implement CascadingValueControl wrapper
- [ ] Implement FlexLayoutControl wrapper
- [ ] Unit tests
- [ ] **Validate:** Layouts render correctly

### 4.2 Navigation Components
- [ ] Implement RouterControl wrapper
- [ ] Implement NavLinkControl wrapper
- [ ] Implement NavMenuControl wrapper
- [ ] Unit tests
- [ ] **Validate:** Navigation works

### 4.3 Utility Components
- [ ] Implement DynamicComponentControl wrapper
- [ ] Implement ErrorBoundaryControl wrapper
- [ ] Implement VirtualizeControl wrapper
- [ ] Implement PageTitleControl wrapper
- [ ] Unit tests
- [ ] **Validate:** Utilities functional

### Phase 4 Checkpoint
- [ ] 20+ components wrapped
- [ ] **Validate:** Navigation and layouts work

---

## Phase 5: Blazor Validation & Advanced Components (Batch 3)

### 5.1 Validation Components
- [ ] Implement DataAnnotationsValidatorControl wrapper
- [ ] Implement ValidationMessageControl wrapper
- [ ] Implement ValidationSummaryControl wrapper
- [ ] Implement CustomValidationControl wrapper
- [ ] Unit tests
- [ ] **Validate:** Validation messages display

### 5.2 Advanced Components
- [ ] Implement InputFileControl wrapper
- [ ] Implement HeadContentControl wrapper
- [ ] Implement FocusOnNavigateControl wrapper
- [ ] Implement CascadingParameterControl wrapper
- [ ] Unit tests
- [ ] **Validate:** Advanced features work

### Phase 5 Checkpoint
- [ ] 36+ components wrapped
- [ ] **Validate:** All Blazor components available

---

## Phase 6: Blazor Sample App Implementation

### 6.1 Create Sample App Project
- [ ] Create `Brinell.Samples.Blazor.App` project
- [ ] Setup Router and MainLayout
- [ ] Create 6-page structure
- [ ] Add navigation menu
- [ ] **Validate:** App compiles and runs

### 6.2 Implement Dashboard Page
- [ ] Create Dashboard.razor with KPI cards
- [ ] Use new component wrappers
- [ ] Connect to sample data
- [ ] Add tests
- [ ] **Validate:** Dashboard displays data

### 6.3 Implement User Form Page
- [ ] Create UserForm.razor with form components
- [ ] Implement EditForm and validation
- [ ] Add model binding
- [ ] Add tests
- [ ] **Validate:** Form validates and submits

### 6.4 Implement Data Table Page
- [ ] Create DataTable.razor with Virtualize
- [ ] Add search and sort
- [ ] Add pagination
- [ ] Add tests
- [ ] **Validate:** Table displays and filters

### 6.5 Implement File Upload Page
- [ ] Create FileUpload.razor
- [ ] Implement InputFile handling
- [ ] Add progress tracking
- [ ] Add tests
- [ ] **Validate:** Files upload and display

### 6.6 Implement Advanced Features Page
- [ ] Create Advanced.razor with dynamic components
- [ ] Implement error boundary
- [ ] Add focus management
- [ ] Add tests
- [ ] **Validate:** Advanced features work

### Phase 6 Checkpoint
- [ ] Sample app complete with 6 pages
- [ ] All 36+ components demonstrated
- [ ] **Validate:** App runs without errors

---

## Phase 7: Blazor Test Implementation

### 7.1 Implement Minimal Test Suite
- [ ] Create TEST-003 test project
- [ ] Implement 25 tests from minimal set
- [ ] Run against sample app
- [ ] **Validate:** All 25 tests pass

### 7.2 Implement Full Test Suite
- [ ] Create TEST-004 test project
- [ ] Implement remaining 87 tests
- [ ] Run against sample app
- [ ] **Validate:** All 112 tests pass

### 7.3 Implement Integration Tests
- [ ] Create form workflow tests
- [ ] Create data grid workflow tests
- [ ] Create file upload workflow tests
- [ ] Create navigation workflow tests
- [ ] **Validate:** Integration tests pass

### Phase 7 Checkpoint
- [ ] 112+ tests implemented and passing
- [ ] **Validate:** Sample app fully tested

---

## Phase 8: Sample Data & Documentation

### 8.1 Create Sample Data
- [ ] Create shared fixture classes
- [ ] Create data builders
- [ ] Use in sample app and tests
- [ ] **Validate:** Consistent test data

### 8.2 Update Documentation
- [ ] Create Blazor component usage guides
- [ ] Add Blazor-specific API docs
- [ ] Create migration guide from standard Blazor
- [ ] Add troubleshooting section
- [ ] **Validate:** Documentation complete

### 8.3 Create Integration Guide
- [ ] Document how to use wrapped components in existing Blazor apps
- [ ] Create example implementations
- [ ] Add setup guide
- [ ] **Validate:** Clear usage pattern

---

## Phase 9: Cleanup & Obsolescence

### 9.1 Mark Components as Wrappers
- [ ] Document which components are wrappers vs. new implementations
- [ ] Add comments indicating wrapped component
- [ ] Add migration notes if needed
- [ ] **Validate:** Clear what's wrapped

### 9.2 Performance Comparison
- [ ] Benchmark wrapped components vs. direct use
- [ ] Document performance characteristics
- [ ] Optimize if needed
- [ ] **Validate:** Performance acceptable

### 9.3 Final Validation
- [ ] All tests pass
- [ ] Sample app works
- [ ] Documentation complete
- [ ] Performance acceptable
- [ ] **Validate:** Ready for release

### 9.4 Plan Future Enhancements
- [ ] Document opportunities for native Blazor implementations
- [ ] Plan performance optimizations
- [ ] Plan additional components
- [ ] **Validate:** Roadmap documented

---

## Cross-Platform Integration (After Both Complete)

### 10.1 Shared Test Framework
- [ ] Create shared test base classes
- [ ] Align test naming conventions
- [ ] Create cross-platform test runner
- [ ] **Validate:** Tests run for both platforms

### 10.2 Documentation Consolidation
- [ ] Create unified control documentation
- [ ] Maintain platform-specific guides
- [ ] Create migration matrix
- [ ] **Validate:** Single source of truth

### 10.3 Release & Versioning
- [ ] Version both sample apps together
- [ ] Update NuGet packages
- [ ] Create release notes
- [ ] **Validate:** Versions aligned

---

## Summary

- **Total Phases:** 10
- **Implementation Components:** 36+ Blazor components
- **Tests:** 112+ test cases
- **Timeline:** 8-10 weeks (after MAUI)
- **Deliverables:**
  - Sample Blazor app
  - Test suite
  - Documentation
  - Migration guide

**Key Success Criteria:**
- ✓ Sample app compiles and runs
- ✓ All 112+ tests pass
- ✓ Documentation complete
- ✓ Performance acceptable
- ✓ Cross-platform integration successful

**Dependencies:**
- Requires completion of PLAN-009 (MAUI) first
- Reuses shared interfaces from MAUI
- Follows same patterns as MAUI implementation

