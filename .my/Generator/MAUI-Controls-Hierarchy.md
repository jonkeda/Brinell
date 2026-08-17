# MAUI Controls Hierarchy

## Core Inheritance Tree

```
Element (Base Class)
├── BindableObject
└── VisualElement
    ├── View
    │   ├── Layout (Abstract)
    │   │   ├── StackLayout
    │   │   ├── Grid
    │   │   ├── AbsoluteLayout
    │   │   ├── FlexLayout
    │   │   ├── VerticalStackLayout
    │   │   └── HorizontalStackLayout
    │   │
    │   ├── Page (Abstract)
    │   │   ├── ContentPage
    │   │   ├── NavigationPage
    │   │   ├── TabbedPage
    │   │   ├── FlyoutPage
    │   │   ├── TemplatedPage
    │   │   ├── AppShell
    │   │   └── CarouselPage
    │   │
    │   ├── ActivityIndicator
    │   ├── BoxView
    │   ├── Button
    │   ├── CheckBox
    │   ├── CollectionView
    │   ├── DatePicker
    │   ├── Editor
    │   ├── Entry
    │   ├── Frame
    │   ├── GraphicsView
    │   ├── Image
    │   ├── ImageButton
    │   ├── Label
    │   ├── Line
    │   ├── ListView
    │   ├── Picker
    │   ├── PolyLine
    │   ├── Polygon
    │   ├── ProgressBar
    │   ├── RadioButton
    │   ├── Rectangle
    │   ├── RefreshView
    │   ├── RoundRectangle
    │   ├── ScrollView
    │   ├── SearchBar
    │   ├── SemanticOrderView
    │   ├── Slider
    │   ├── Stepper
    │   ├── SwipeView
    │   ├── Switch
    │   ├── TableView
    │   ├── TimePicker
    │   ├── WebView
    │   ├── Path
    │   ├── Ellipse
    │   └── Shape (Abstract Base for Shapes)
    │       ├── Line
    │       ├── Polyline
    │       ├── Polygon
    │       ├── Path
    │       ├── Rectangle
    │       ├── RoundRectangle
    │       └── Ellipse
    │
    └── Gesture Recognizers
        ├── GestureRecognizer (Abstract)
        │   ├── ClickGestureRecognizer
        │   ├── DragGestureRecognizer
        │   ├── DropGestureRecognizer
        │   ├── PinchGestureRecognizer
        │   ├── PointerGestureRecognizer
        │   ├── SwipeGestureRecognizer
        │   └── TapGestureRecognizer
```

## Layout Controls

### StackLayout / VerticalStackLayout / HorizontalStackLayout
- **Spacing**: Gap between children
- **Padding**: Internal padding
- **Children**: IList<View>
- **Orientation**: Vertical/Horizontal (StackLayout allows both)

### Grid
- **RowDefinitions**: RowDefinitionCollection
- **ColumnDefinitions**: ColumnDefinitionCollection
- **RowSpacing**: Gap between rows
- **ColumnSpacing**: Gap between columns
- **Children**: Positioned with Row/Column attached properties

### AbsoluteLayout
- **Padding**: Internal padding
- **Children**: Positioned with absolute or proportional coordinates via AbsoluteLayoutFlags

### FlexLayout
- **Direction**: Row, Column, RowReverse, ColumnReverse
- **Wrap**: NoWrap, Wrap, WrapReverse
- **JustifyContent**: Content alignment along primary axis
- **AlignItems**: Item alignment along secondary axis
- **AlignContent**: Multi-line content alignment

## Page Controls

### ContentPage
- Simple single-content container
- Content property holds a View

### NavigationPage
- Manages a stack of pages
- Provides navigation push/pop

### TabbedPage
- Multiple tabs with page content
- SelectedTab and Children

### FlyoutPage
- Master-detail layout
- Flyout and Detail properties

### AppShell
- Route-based navigation
- Flyout items and Shell routes

### CarouselPage
- Horizontal page carousel

## Common View Properties

All controls inherit these properties:

```
Common Properties:
├── BackgroundColor
├── Opacity
├── Rotation
├── RotationX
├── RotationY
├── Scale
├── ScaleX
├── ScaleY
├── TranslationX
├── TranslationY
├── IsEnabled
├── IsVisible
├── Padding
├── Margin
├── HorizontalOptions (LayoutOptions)
├── VerticalOptions (LayoutOptions)
├── MinimumHeightRequest
├── MinimumWidthRequest
├── HeightRequest
├── WidthRequest
├── Clip (Geometry)
├── Shadow (Shadow)
├── Border (Border)
├── GestureRecognizers (IGestureRecognizer collection)
└── AutomationProperties
```

## Interactive Controls

### Button / ImageButton
- **Text**: Button label
- **ImageSource**: Image for ImageButton
- **Command**: ICommand binding
- **CommandParameter**: Parameter for command
- **CornerRadius**: Border radius
- **BorderWidth**: Border thickness
- **BorderColor**: Border color
- **Padding**: Internal padding

### Entry / Editor
- **Text**: Input text
- **TextColor**: Text color
- **FontSize**: Font size
- **FontFamily**: Font family
- **FontAttributes**: Bold, Italic
- **Placeholder**: Hint text
- **PlaceholderColor**: Hint color
- **IsPassword**: Mask input (Entry only)
- **KeyboardType**: Virtual keyboard type
- **ReturnType**: Return key behavior
- **CursorPosition**: Cursor position
- **SelectionLength**: Selected text length

### Label
- **Text**: Display text
- **TextColor**: Text color
- **FontSize**: Font size
- **FontFamily**: Font family
- **FontAttributes**: Bold, Italic
- **TextDecorations**: Underline, Strikethrough
- **LineHeight**: Line height multiplier
- **CharacterSpacing**: Spacing between characters
- **TextAlignment**: Horizontal/Vertical alignment
- **LineBreakMode**: Text wrapping mode
- **TextTransform**: Uppercase, Lowercase

### Image
- **Source**: ImageSource (file, URI, resource, stream)
- **Aspect**: Fill, AspectFill, AspectFit
- **IsAnimationPlaying**: For animated GIF

### Slider / Stepper
- **Value**: Current numeric value
- **Minimum**: Minimum value
- **Maximum**: Maximum value
- **Increment**: Step size (Stepper)

### Switch / CheckBox / RadioButton
- **IsToggled**: Is on/off state
- **IsChecked**: Is checked state (CheckBox/RadioButton)
- **ThumbColor**: Thumb/toggle color
- **TrackColor**: Track/background color
- **OnColor**: Color when enabled/checked
- **OffColor**: Color when disabled/unchecked

### Picker / DatePicker / TimePicker
- **SelectedItem**: Selected value
- **SelectedDate**: Selected date (DatePicker)
- **SelectedTime**: Selected time (TimePicker)
- **ItemsSource**: Data source (Picker)
- **Items**: Item collection (Picker)
- **SelectedIndex**: Index of selected item
- **Format**: Display format string

### ListView / CollectionView
- **ItemsSource**: Data source
- **ItemTemplate**: DataTemplate for items
- **SelectedItem**: Selected item
- **SelectedItems**: Multiple selected items (CollectionView)
- **SelectionMode**: Single, Multiple, or None
- **GroupHeaderTemplate**: Template for group headers
- **GroupFooterTemplate**: Template for group footers
- **Header**: Header view
- **Footer**: Footer view
- **RefreshCommand**: Refresh/pull-to-refresh command
- **ItemsUpdatingScrollMode**: Scroll behavior when items update

### Other Controls
- **ActivityIndicator**: Loading spinner
- **ProgressBar**: Progress display
- **BoxView**: Colored rectangle
- **Frame**: Border container
- **RefreshView**: Pull-to-refresh container
- **ScrollView**: Scrollable container
- **SearchBar**: Search input
- **SwipeView**: Swipe gesture container
- **WebView**: Web content display
- **GraphicsView**: Custom drawing surface
- **TableView**: Cell-based table view

## Data Binding

All controls support:
- **BindingContext**: Data context
- **Bindings**: BindingBase collection
- **Attached Binding Properties**: Custom attachable bindings
- **PropertyChanged Events**: Property change notifications

## Gesture Recognizers

Attached to any View to handle interactions:

- **TapGestureRecognizer**: Single tap
- **ClickGestureRecognizer**: Click with context menu
- **DoubleTapGestureRecognizer**: Double tap
- **LongPressGestureRecognizer**: Long press/hold
- **PinchGestureRecognizer**: Pinch zoom gesture
- **SwipeGestureRecognizer**: Swipe in direction
- **DragGestureRecognizer**: Drag operation
- **DropGestureRecognizer**: Drop target
- **PointerGestureRecognizer**: Pointer events

## Styling & Theming

All controls support:
- **Style**: Inline or resource style
- **ResourceDictionary**: Theme resources
- **Visual States**: Normal, Disabled, Focused, Selected, etc.
- **Light/Dark Mode**: Automatic theme switching based on OS settings
- **Brushes & Fills**: Solid colors, gradients, patterns

## Key Concepts

1. **Inheritance Chain**: Element → BindableObject → VisualElement → View/Page
2. **Layouts**: Containers that manage child positioning and sizing
3. **Pages**: Specialized Views that typically fill the screen
4. **Gesture Recognizers**: Attached to Views for user interaction handling
5. **Data Binding**: Two-way binding between properties and UI
6. **Visual States**: State management for accessibility and interactivity
7. **Styling**: Inline, resource-based, or theme-based styling
8. **Accessibility**: AutomationProperties for testing and screen readers
