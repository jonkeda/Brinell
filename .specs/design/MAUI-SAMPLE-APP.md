# MAUI Sample App Design

**Source of truth:** `samples/Brinell.Samples.Maui.App/`

## Purpose

Sample MAUI app exercising all control interfaces for UI testing. Organized into 8 tabbed categories.

## Tab Structure (SPEC-005)

The app uses TabbedPage navigation with 8 tabs, each containing a dedicated view:

| Tab | View | Controls Demonstrated |
|-----|------|----------------------|
| **Basics** | BasicsView | Button, Label, Entry, ProgressBar |
| **Containers** | ContainerDemoView | Frame, Border, Grid, nested containers, lists |
| **Forms** | UserFormView | Entry, Editor, SearchBar, CheckBox, Switch, RadioButton, Picker, DatePicker, TimePicker, Slider, Stepper |
| **Lists** | ListDemoView | ListView, TableView, CollectionView |
| **Gestures** | GesturesDemoView | SwipeView, RefreshView, drag & drop |
| **Navigation** | NavigationDemoView | Menu, Toolbar, navigation patterns |
| **Toolkit** | ToolkitDemoView | CommunityToolkit Expander, TreeView-like, Popup |
| **Media** | MediaDemoView | Image, ActivityIndicator, WebView, MediaElement |

**Implementation status:** 19/21 tasks complete (Phases 1-5 done, Phase 6 build verification 1/3 remaining).

## Interface-to-Control Mapping

| Interface | MAUI Control | Sample Page |
|-----------|-------------|-------------|
| `IClickableControlObject` | Button, ImageButton | Basics |
| `IEditableTextControlObject` | Entry, Editor, SearchBar | Forms |
| `ITextControlObject` | Label, Span | Basics |
| `IToggleControlObject` | CheckBox, Switch, RadioButton | Forms |
| `ISelectorControlObject` | Picker | Forms |
| `IRangeControlObject` | Slider, Stepper | Forms |
| `IProgressControlObject` | ProgressBar, ActivityIndicator | Basics, Media |
| `IDateControlObject` | DatePicker | Forms |
| `ITimeControlObject` | TimePicker | Forms |
| `IScrollableControlObject` | ScrollView, CollectionView | Lists |
| `IContainerControl` | Frame, Border, Grid | Containers |
| `IExpandableControlObject` | Expander (CommunityToolkit) | Toolkit |
| `ISwipeableControlObject` | SwipeView | Gestures |
| `IRefreshableControlObject` | RefreshView | Gestures |
| `ITabControlObject` | TabbedPage tabs | Navigation (shell) |

## Navigation

Uses TabbedPage for page navigation. Each tab hosts a ContentPage with controls organized by capability category. Tab automation uses XPath-by-Name pattern (see SPEC-023).
