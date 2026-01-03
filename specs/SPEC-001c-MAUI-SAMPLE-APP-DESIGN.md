# SPEC-001c-MAUI-SAMPLE-APP-DESIGN

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Sample MAUI Application

A comprehensive test sample application demonstrating all major MAUI control types and interactions.

---

## Application Structure

### AppShell
```
Shell
├── Pages
│   ├── Dashboard (ShellContent)
│   ├── UserForm (ShellContent)
│   ├── DataGrid (ShellContent)
│   ├── MediaGallery (ShellContent)
│   ├── Navigation Demo (ShellContent)
│   └── Advanced (ShellContent)
└── Navigation Route(s)
```

---

## Page 1: Dashboard Page

**Purpose:** Display key information and status

**Layout:** VerticalStackLayout

**Controls:**
- TabbedPage (Navigation)
  - Tab 1: Summary
    - Label (Title)
    - Image (App Logo)
    - Label (Status Text)
    - ProgressBar (Load Progress)
    - ActivityIndicator (Loading)
    - BoxView (Divider)
    - Label (Statistics)
    - Grid (2 columns for KPIs)
      - Label (KPI 1 Label)
      - Label (KPI 1 Value)
      - Label (KPI 2 Label)
      - Label (KPI 2 Value)

  - Tab 2: Quick Actions
    - VerticalStackLayout
      - Button (Refresh Data)
      - Button (Export Report)
      - Button (Settings)
      - Button (Help)

  - Tab 3: Status
    - Label (Last Updated)
    - BoxView (Status Indicator)
    - Label (Status Message)
    - RefreshView (Pull to refresh)
      - CollectionView (Status items)

---

## Page 2: User Form Page

**Purpose:** Demonstrate text input, selection, and toggle controls

**Layout:** ScrollView > VerticalStackLayout

**Controls:**
- Frame (Form Container)
  - VerticalStackLayout
    - Label (Form Title)
    - Label (Name Section)
    - Entry (First Name)
    - Entry (Last Name)
    
    - Label (Contact Section)
    - Entry (Email)
    - SearchBar (Email Search/Validation)
    - Entry (Phone)
    
    - Label (Details Section)
    - Editor (Bio/Description)
    
    - Label (Selection Section)
    - Picker (Country Selection)
    - Picker (Department Selection)
    
    - Label (Date/Time Section)
    - DatePicker (Birth Date)
    - TimePicker (Preferred Contact Time)
    
    - Label (Toggle Section)
    - Switch (Newsletter Subscription)
    - CheckBox (Terms & Conditions)
    - CheckBox (Privacy Policy)
    - RadioButton (Subscription Tier - Basic)
    - RadioButton (Subscription Tier - Premium)
    - RadioButton (Subscription Tier - Enterprise)
    
    - Label (Preference Section)
    - Slider (Font Size)
    - Stepper (Number of Items)
    
    - Label (Button Section)
    - Button (Submit)
    - Button (Clear Form)
    - Button (Save Draft)

---

## Page 3: Data Grid Page

**Purpose:** Demonstrate collection and selection controls

**Layout:** VerticalStackLayout

**Controls:**
- Frame (Header)
  - HorizontalStackLayout
    - SearchBar (Filter Items)
    - Button (Refresh)

- Label (ListView Section)
- ListView (User List)
  - ViewCell
    - Grid (2 columns)
      - Label (Name)
      - Label (Status)

- Label (CollectionView Section)
- CollectionView (Advanced List)
  - Grid (Multi-column display)
    - Label (ID)
    - Label (Name)
    - Label (Email)
    - Button (Edit)
    - Button (Delete)

- Label (TableView Section)
- TableView (Structured Data)
  - TextCell (Row 1)
  - TextCell (Row 2)
  - ImageCell (Row 3)
  - SwitchCell (Row 4)

- Label (CarouselView Section)
- CarouselView (Item Carousel)
  - Frame
    - Label (Title)
    - Image (Thumbnail)
    - Label (Description)
    - Button (View Details)

---

## Page 4: Media Gallery Page

**Purpose:** Demonstrate image, media, and graphical controls

**Layout:** ScrollView > VerticalStackLayout

**Controls:**
- Label (Gallery Title)

- Label (Images Section)
- Grid (3 columns)
  - ImageButton (Thumbnail 1)
  - ImageButton (Thumbnail 2)
  - ImageButton (Thumbnail 3)
  - ImageButton (Thumbnail 4)
  - ImageButton (Thumbnail 5)
  - ImageButton (Thumbnail 6)

- Label (Image Display)
- Image (Full Size Display)

- Label (Graphics Section)
- GraphicsView
  - Shapes (Line, Rectangle, Ellipse, Path)

- Label (Media Section)
- MediaElement (Video Player)

- Label (WebView Section)
- WebView (Embedded Content)

- Label (Status Section)
- BoxView (Color Indicator)
- Label (Color Value)

---

## Page 5: Navigation Demo Page

**Purpose:** Demonstrate navigation and menu controls

**Layout:** VerticalStackLayout

**Controls:**
- Label (Navigation Demo)

- Label (Stack Navigation)
- Button (Push Page)
- Button (Pop Page)
- Button (Modal Page)

- Label (Menu Items)
- MenuItem (Menu Item 1)
- MenuItem (Menu Item 2)
- MenuItem (Menu Item 3)

- Label (ToolBar Items)
- ToolbarItem (Save Action)
- ToolbarItem (Edit Action)
- ToolbarItem (Delete Action)

- Label (FlyoutPage Demo)
- Button (Open Flyout)

- Label (Expander)
- Expander
  - Label (Expandable Header)
  - VerticalStackLayout (Expandable Content)
    - Label (Content 1)
    - Label (Content 2)
    - Label (Content 3)

---

## Page 6: Advanced Controls Page

**Purpose:** Demonstrate advanced interactions and gestures

**Layout:** ScrollView > VerticalStackLayout

**Controls:**
- Label (Gestures & Interactions)

- Label (Tap Gesture)
- Frame
  - Label (Tap Me)
  - GestureRecognizer (TapGestureRecognizer)

- Label (Pan Gesture)
- Frame
  - Label (Drag Me)
  - GestureRecognizer (PanGestureRecognizer)

- Label (Pinch Gesture)
- Frame
  - Image (Pinch to Zoom)
  - GestureRecognizer (PinchGestureRecognizer)

- Label (Pointer Gesture)
- Frame
  - Label (Move Pointer)
  - GestureRecognizer (PointerGestureRecognizer)

- Label (Drop Gesture)
- Frame
  - Label (Drop Target)
  - GestureRecognizer (DropGestureRecognizer)

- Label (SwipeView)
- SwipeView
  - Label (Swipe me left/right)
  - SwipeItems
    - SwipeItem (Delete)
    - SwipeItem (Archive)

- Label (Border Control)
- Border
  - Label (Bordered Content)

- Label (InputView)
- Entry (Special Input)

---

## Navigation Routes

```
dashboard → dashboard
userform → user-form
datagrid → data-grid
media → media-gallery
navigation → navigation-demo
advanced → advanced-controls
push-page → (modal navigation)
pop-page → (pop action)
```

---

## Control Count Summary

| Category | Count | Controls |
|----------|-------|----------|
| Pages | 6 | Dashboard, Form, Grid, Media, Navigation, Advanced |
| Layouts | 8+ | VerticalStackLayout, HorizontalStackLayout, Grid, ScrollView, Frame, Border, Expander, RelativeLayout |
| Text Input | 5 | Entry, Editor, SearchBar, InputView |
| Selection | 6 | Picker, RadioButton, CheckBox, ListView, CollectionView, TableView |
| Display | 8 | Label, Image, ImageButton, WebView, MediaElement, BoxView, GraphicsView |
| Interactive | 5 | Button, Switch, Stepper, Slider, SwipeView |
| Collection | 3 | ListView, CollectionView, CarouselView |
| Navigation | 5 | TabbedPage, Shell, ToolbarItem, MenuItem, Expander |
| Container | 4 | Frame, Border, ScrollView, FlexLayout |
| Gestures | 6 | TapGestureRecognizer, PanGestureRecognizer, PinchGestureRecognizer, PointerGestureRecognizer, DropGestureRecognizer |
| Status | 3 | ProgressBar, ActivityIndicator, RefreshView |
| Date/Time | 2 | DatePicker, TimePicker |
| Other | 4 | Line, Rectangle, Ellipse, Path |
| **TOTAL** | **66+** | All major MAUI controls |

---

## Test Scenarios

### Text Input Testing
- Enter text in Entry controls
- Clear Entry fields
- Enter multi-line text in Editor
- Search functionality with SearchBar
- Input validation

### Selection Testing
- Select from Picker
- RadioButton selection
- CheckBox selection
- ListView item selection
- CollectionView item selection
- TableView cell selection

### Toggle Testing
- Switch On/Off
- CheckBox Check/Uncheck
- RadioButton selection

### Range Testing
- Slider value adjustment
- Stepper increment/decrement
- ProgressBar value display

### Date/Time Testing
- DatePicker date selection
- TimePicker time selection
- Format validation

### Collection Testing
- ListView scroll and select
- CollectionView scroll and select
- CarouselView navigation
- TableView interaction

### Navigation Testing
- Tab navigation
- Page push/pop
- Modal page presentation
- Flyout menu

### Gesture Testing
- Tap gestures
- Pan/drag gestures
- Pinch/zoom gestures
- Pointer tracking
- Drop target

### Display Testing
- Image loading
- Label text display
- WebView content
- MediaElement playback
- Shape rendering

---

**Last Updated:** January 3, 2026
