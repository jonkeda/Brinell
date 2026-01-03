# SPEC-000c: Comprehensive Control Reference & Design

**Version:** 1.0  
**Status:** Design Reference  
**Date:** January 2026

---

## Overview

SPEC-000c provides a comprehensive reference of all standard UI controls available in .NET MAUI and Blazor, along with a new unified interface design to support test automation for all platforms.

---

## Documents in This Series

### SPEC-000c-001: MAUI Controls Catalog
- Complete list of all 66 standard MAUI controls
- Organized by category (Pages, Layouts, Controls)
- Mapped to functional groups

### SPEC-000c-002: Blazor Components Catalog
- Complete list of all 36 standard Blazor components
- Organized by type (Layout, Utility, Form)
- Mapped to functional groups

### SPEC-000c-003: Comprehensive Control Interface Design
- Unified interface design for all controls
- 30+ specialized interfaces covering all control behaviors
- Generic implementations for common control patterns
- No explanations, signatures only

---

## Interface Categories

### Core Interfaces
- **IVisualElement** - Visibility and bounds
- **ILocatable** - Position and size
- **IStylable** - Colors, fonts, styling
- **IInteractive** - Enabled state
- **IClickable** - Click interactions
- **IGesturable** - Gesture recognition

### Text Input (4 interfaces)
- **ITextInputControl** - Basic text operations
- **ITextSearchControl** - Text search
- **IEditableTextControl** - Edit operations
- **IValidatableTextControl** - Text validation

### Selection (3 interfaces)
- **ISingleSelectControl** - Single selection
- **IMultiSelectControl** - Multiple selection
- **ISelectableControl** - Item enumeration

### Toggle (3 interfaces)
- **IToggleControl** - Checkbox/toggle
- **IRadioControl** - Radio buttons
- **ISwitchControl** - On/off switches

### Range (3 interfaces)
- **IRangeInputControl** - Numeric ranges
- **ISliderControl** - Slider interactions
- **IProgressControl** - Progress display

### Date/Time (3 interfaces)
- **IDateInputControl** - Date selection
- **ITimeInputControl** - Time selection
- **IDateRangeInputControl** - Date range selection

### Collection (6 interfaces)
- **ICollectionControl** - Item enumeration
- **IClickableCollectionControl** - Item selection
- **IScrollableCollectionControl** - Scrolling
- **ILoadableCollectionControl** - Lazy loading
- **IGroupedCollectionControl** - Grouping
- **IFilterableCollectionControl** - Filtering

### Container (5 interfaces)
- **IContainerControl** - Child element access
- **IScrollableControl** - Scrolling behavior
- **IExpandableControl** - Expand/collapse
- **IRefreshableControl** - Pull to refresh
- **ISwipeableControl** - Swipe gestures

### Display (5 interfaces)
- **ILabelControl** - Text display
- **IImageControl** - Image display
- **IMediaControl** - Media playback
- **IWebViewControl** - Web content
- **IShapeControl** - Shape styling

### File (1 interface)
- **IFileInputControl** - File selection

### State (5 interfaces)
- **ICheckableControl** - Checked state
- **IIndeterminateControl** - Indeterminate state
- **IEnabledControl** - Enabled state
- **IFocusableControl** - Focus state
- **ILoadingControl** - Loading state

### Validation (3 interfaces)
- **IValidatableControl** - Validation state
- **IRequiredFieldControl** - Required field
- **IErrorControl** - Error display

### Navigation (2 interfaces)
- **INavigableControl** - Navigation
- **IMenuControl** - Menu operations

### Behavior (4 interfaces)
- **ITooltipControl** - Tooltips
- **IContextMenuControl** - Context menus
- **IDraggableControl** - Drag operations
- **IDropTargetControl** - Drop target

### Generic Implementations (4 classes)
- **GenericControl** - Base functionality
- **GenericTextInputControl** - Text input
- **GenericSelectableControl** - Selection
- **GenericToggleControl** - Toggle behavior

---

## Control Coverage

### MAUI Controls Mapped to Interfaces
- **Entry** → ITextInputControl, IEditableTextControl, IValidatableTextControl
- **Picker** → ISingleSelectControl, ISelectableControl
- **CollectionView** → ICollectionControl, IClickableCollectionControl, IScrollableCollectionControl
- **Button** → IClickable, IInteractive
- **Slider** → IRangeInputControl
- **DatePicker** → IDateInputControl
- **Switch** → ISwitchControl
- **CheckBox** → IToggleControl
- **ScrollView** → IScrollableControl, IContainerControl
- **Label** → ILabelControl
- **Image** → IImageControl
- **WebView** → IWebViewControl

### Blazor Components Mapped to Interfaces
- **InputText** → ITextInputControl, IEditableTextControl
- **InputSelect** → ISingleSelectControl, ISelectableControl
- **InputCheckbox** → IToggleControl
- **InputDate** → IDateInputControl
- **InputFile** → IFileInputControl
- **ValidationMessage** → IErrorControl
- **EditForm** → IValidatableControl, IContainerControl

---

## Total Interface Count

- Core Interfaces: 6
- Text Input: 4
- Selection: 3
- Toggle: 3
- Range: 3
- Date/Time: 3
- Collection: 6
- Container: 5
- Display: 5
- File: 1
- State: 5
- Validation: 3
- Navigation: 2
- Behavior: 4
- **Total Specialized Interfaces: 53**
- Generic Implementations: 4
- **Total: 57 interfaces and classes**

---

## Usage

1. Start with core interfaces (IVisualElement, IInteractive, etc.) for all controls
2. Add specialized interfaces based on control type
3. Implement generic classes for common patterns
4. Combine multiple interfaces for complex controls

### Example: Entry Control
```
IVisualElement
  + ITextInputControl
  + IEditableTextControl
  + IValidatableTextControl
  + IFocusableControl
  + IErrorControl
```

### Example: CollectionView
```
IVisualElement
  + ICollectionControl
  + IClickableCollectionControl
  + IScrollableCollectionControl
  + ILoadableCollectionControl
  + IFilterableCollectionControl
```

---

## Reference

- MAUI Controls: 66 total (10 Pages + 10 Layouts + 46 Controls)
- Blazor Components: 36 total (8 Layout + 10 Utility + 18 Form)
- Designed Interfaces: 53 specialized + 4 generic = 57 total

---

**Last Updated:** January 3, 2026
