# MAUI Sample App Design

**Source of truth:** `samples/Brinell.Samples.Maui.App/`

## Purpose

Sample MAUI app exercising all SPEC-006 interfaces for UI testing.

## Interface-to-Control Mapping

| Interface | MAUI Control | Sample Page |
|-----------|-------------|-------------|
| `IClickableControlObject` | Button, ImageButton | Buttons page |
| `IEditableTextControlObject` | Entry, Editor, SearchBar | Text page |
| `ITextControlObject` | Label, Span | Display page |
| `IToggleControlObject` | CheckBox, Switch, RadioButton | Toggle page |
| `ISelectorControlObject` | Picker | Selection page |
| `IRangeControlObject` | Slider, Stepper | Range page |
| `IProgressControlObject` | ProgressBar, ActivityIndicator | Range page |
| `IDateControlObject` | DatePicker | DateTime page |
| `ITimeControlObject` | TimePicker | DateTime page |
| `IScrollableControlObject` | ScrollView, CollectionView | Collection page |
| `IContainerControl` | Frame, Border, Grid | Container page |
| `IExpandableControlObject` | Expander (CommunityToolkit) | Advanced page |
| `ITabControlObject` | TabbedPage tabs | Navigation |

## Navigation

Currently uses TabbedPage for page navigation. The sample app provides a page per capability category.
