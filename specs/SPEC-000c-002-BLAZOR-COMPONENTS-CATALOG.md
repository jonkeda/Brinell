# SPEC-000c-002: Blazor Components Catalog

**Version:** 1.0  
**Status:** Reference  
**Date:** January 2026

---

## Standard Blazor Components

Total: 8 Layout + 10 Utility + 18 Form = 36 built-in components

---

## 1. Layout Components (8)

- MainLayout
- LayoutComponentBase
- CascadingValue<T>
- CascadingParameter
- RouteView
- Router
- NavLink
- NavMenu

---

## 2. Utility Components (10)

- ErrorBoundary
- DynamicComponent
- HeadContent
- HeadOutlet
- PageTitle
- FocusOnNavigate
- Virtualize<T>
- VirtualizeScrollableRegion
- ComponentBase
- LayoutComponentBase

---

## 3. Form Components (18)

### Form Container
- EditForm

### Input Components
- InputText
- InputTextArea
- InputNumber<T>
- InputCheckbox
- InputDate<T>
- InputDateRange<T>
- InputFile
- InputRadio<T>
- InputSelect<T>
- InputRadioGroup<T>

### Validation Components
- ValidationMessage<T>
- ValidationSummary
- DataAnnotationsValidator
- ObjectGraphDataAnnotationsValidator

### Custom Validation
- CustomValidation<T>
- InputBase<T>

---

## Component Hierarchy

### Base Classes
- ComponentBase
- LayoutComponentBase
- EditFormBase
- InputBase<T>

### Cascading
- CascadingValue<T>
- CascadingParameter

### Routing
- RouteView
- Router
- NavLink

### Virtualization
- Virtualize<T>
- VirtualizeScrollableRegion

### Error Handling
- ErrorBoundary

### Dynamic
- DynamicComponent

### Document
- HeadContent
- HeadOutlet
- PageTitle

### Form Validation
- DataAnnotationsValidator
- ObjectGraphDataAnnotationsValidator
- CustomValidation<T>
- ValidationMessage<T>
- ValidationSummary

---

## Category Mapping

### Text Input
InputText, InputTextArea

### Numeric Input
InputNumber<T>

### Date/Time Input
InputDate<T>, InputDateRange<T>

### Selection Input
InputCheckbox, InputRadio<T>, InputSelect<T>, InputRadioGroup<T>

### File Input
InputFile

### Validation
ValidationMessage<T>, ValidationSummary, DataAnnotationsValidator, ObjectGraphDataAnnotationsValidator, CustomValidation<T>

### Layout
MainLayout, LayoutComponentBase, CascadingValue<T>, CascadingParameter

### Navigation
RouteView, Router, NavLink, NavMenu

### Performance
Virtualize<T>, VirtualizeScrollableRegion

### Error Handling
ErrorBoundary

### Dynamic
DynamicComponent

### Document
HeadContent, HeadOutlet, PageTitle

---

**Last Updated:** January 3, 2026
