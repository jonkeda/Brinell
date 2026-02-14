# SPX-024: MAUI Control Objects

**Status:** Active — expanding control coverage

## Goal

Implement all 29 MAUI control objects covering every SPEC-006 interface category.

## Control Catalog

| Category | Controls | Interface | Status |
|----------|----------|-----------|--------|
| **Buttons** | Button, ImageButton | `IClickableControlObject` | Implemented |
| **Text** | Entry, Editor, SearchBar | `IEditableTextControlObject` | Implemented |
| **Display** | Label, Span, Image | `ITextControlObject` | Implemented |
| **Toggle** | CheckBox, Switch, RadioButton | `IToggleControlObject` | Implemented |
| **Selection** | Picker | `ISelectorControlObject` | Implemented |
| **Range** | Slider, Stepper | `IRangeControlObject` | Implemented |
| **Progress** | ProgressBar, ActivityIndicator | `IProgressControlObject` | Implemented |
| **DateTime** | DatePicker, TimePicker | `IDate/TimeControlObject` | Implemented |
| **Collection** | CollectionView, ListView | `IScrollableControlObject` | Partial |
| **Container** | Frame, Border, Grid | `IContainerControl` | Implemented |
| **Navigation** | TabBar, FlyoutItem, NavigationPage | `IClickableControlObject` | Partial |
| **Media** | MediaElement | `IControlObject` | Scaffolded |

## Requirements

- All controls must implement the Is/Wait/Assert pattern from their interface
- All controls must support `ScrollIntoView` before interaction
- All controls must support the `Locator` system for element finding
- Controls must be testable with mocked drivers

## Tasks

Organized by category — implement each control group with unit tests and sample app coverage.
