# MAUI Control Objects Coverage Analysis (Verified)

**Date:** 2025  
**Framework:** .NET 10 MAUI  
**Purpose:** Document which standard MAUI controls have corresponding Brinell control objects and identify gaps  
**Source:** [Microsoft Learn - .NET MAUI Controls](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/?view=net-maui-10.0)

---

## Summary

**Total Standard MAUI Controls (Views, Layouts, Pages):** 70+  
**Controls with Brinell Objects:** 39  
**Coverage:** ~55% (based on primary UI controls)

---

## Controls Implemented (39 documented in Brinell)

| Category | Control | Brinell Object | Status |
|----------|---------|-----------------|--------|
| **Buttons** | Button | ✅ Button<TScope> | Standard |
| | ImageButton | ✅ ImageButton<TScope> | Standard |
| | IconCommandButton | ✅ IconCommandButton<TScope> | Brinell extension |
| | Link | ✅ Link<TScope> | Brinell extension |
| | RoundButton | ✅ RoundButton<TScope> | Brinell extension |
| **Collection** | CarouselView | ✅ CarouselView<TScope> | Standard |
| | CollectionView | ✅ CollectionView<TScope> | Standard |
| | ListView | ✅ ListView<TScope> | Standard |
| | IndicatorView | ❌ Missing | Standard |
| | PaginatedList | ✅ PaginatedList<TScope> | Brinell extension |
| | TableView | ✅ TableView<TScope> | Standard |
| **Container** | Border | ✅ Border<TScope> | Standard |
| | BoxView | ❌ Missing | Standard |
| | ContentView | ❌ Missing | Standard |
| | Expander | ✅ Expander<TScope> | Brinell extension |
| | Frame | ❌ Missing | Standard |
| | Grid | ✅ Grid<TScope> | Standard |
| | RefreshView | ✅ RefreshView<TScope> | Standard |
| | ScrollView | ✅ ScrollView<TScope> | Standard |
| | SwipeView | ✅ SwipeView<TScope> | Standard |
| | TwoPaneView | ❌ Missing | Standard (Foldable) |
| **DateTime** | DatePicker | ✅ DatePicker<TScope> | Standard |
| | TimePicker | ✅ TimePicker<TScope> | Standard |
| **Dialogs** | ContentDialog | ✅ ContentDialog<TScope> | Standard |
| **Display** | ActivityIndicator | ✅ ActivityIndicator<TScope> | Standard |
| | Image | ✅ Image<TScope> | Standard |
| | Label | ✅ Label<TScope> | Standard |
| | ProgressBar | ✅ ProgressBar<TScope> | Standard |
| **Graphics** | GraphicsView | ❌ Missing | Standard (New in MAUI 8+) |
| | HybridWebView | ❌ Missing | Standard (New in MAUI 9+) |
| **Media** | MediaElement | ✅ MediaElement<TScope> | Standard |
| | WebView | ✅ WebView<TScope> | Standard |
| **Navigation** | FlyoutItem | ✅ FlyoutItem<TScope> | Brinell extension |
| | Menu | ✅ Menu<TScope> | Brinell extension |
| | Tab | ✅ Tab<TScope> | Brinell extension |
| | TabMenu | ✅ TabMenu<TScope> | Brinell extension |
| | Toolbar | ✅ Toolbar<TScope> | Brinell extension |
| **Range** | Slider | ✅ Slider<TScope> | Standard |
| | Stepper | ✅ Stepper<TScope> | Standard |
| **Selection** | GenericBrowser | ✅ GenericBrowser<TScope> | Brinell extension |
| | Picker | ✅ Picker<TScope> | Standard |
| | SelectionList | ✅ SelectionList<TScope> | Brinell extension |
| **Shapes** | Ellipse | ❌ Missing | Standard |
| | Line | ❌ Missing | Standard |
| | Polygon | ❌ Missing | Standard |
| | Polyline | ❌ Missing | Standard |
| | Rectangle | ❌ Missing | Standard |
| | RoundRectangle | ❌ Missing | Standard |
| | Path | ❌ Missing | Standard |
| **Text** | Editor | ✅ Editor<TScope> | Standard |
| | Entry | ✅ Entry<TScope> | Standard |
| | SearchBar | ✅ SearchBar<TScope> | Standard |
| **Toggle** | CheckBox | ✅ CheckBox<TScope> | Standard |
| | RadioButton | ✅ RadioButton<TScope> | Standard |
| | Switch | ✅ Switch<TScope> | Standard |
| **Web/Hybrid** | BlazorWebView | ❌ Missing | Standard |
| **Generated** | EditableField | ✅ EditableField<TScope> | Brinell custom |
| **Special** | Map | ❌ Missing | Standard (requires NuGet) |
| | TitleBar | ❌ Missing | Standard (Window decoration) |

---

## Missing Standard MAUI Controls (30+ controls)

### Interactive Controls (Priority 1)
1. **IndicatorView** - Carousel indicators
   - Impact: Low (Used with CarouselView, low test priority)

2. **IsoPaneView** - Dual-pane container
   - Impact: Low (Modern foldable support)

### Container/Layout Controls (Priority 2)
3. **BoxView** - Colored rectangle
   - Impact: Low (UI primitive, rarely in functional tests)

4. **ContentView** - Custom control container
   - Impact: Medium (Base for custom components)

5. **Frame** - Bordered container
   - Impact: Low (Superseded by Border)

### Gesture Recognition (Priority 3)
6-12. **GestureRecognizer** and variants
   - Impact: Medium (Complex gesture testing)
   - Note: Requires Appium integration

### Shapes (Priority 4)
13. **Ellipse** - Circle/ellipse shape
14. **Line** - Line shape
15. **Polygon** - Polygon shape
16. **Polyline** - Connected lines
17. **Rectangle** - Rectangle shape
18. **RoundRectangle** - Rounded rectangle
19. **Path** - Complex vector paths
   - Impact: Low (Visual/drawing elements, not typically tested for interaction)

### Modern Controls (Priority 5)
20. **GraphicsView** - 2D graphics canvas (New in MAUI 8+)
   - Impact: Low (Drawing/graphics, custom rendering)

21. **HybridWebView** - Hybrid web content with C#/JS interop (New in MAUI 9+)
   - Impact: Medium (Advanced web integration)

22. **BlazorWebView** - Blazor in MAUI
   - Impact: Medium (Requires Blazor app integration)

### Navigation/Window (Priority 6)
23. **TitleBar** - Custom window title bar
   - Impact: Low (Window decoration, platform-specific)

24. **Map** - Geographic map view
   - Impact: Low (Requires separate NuGet, location testing complex)

---

## Brinell Extensions (Non-Standard, 7 total)

Controls created by Brinell that extend or wrap MAUI functionality:

1. **ImageButton** - Enhanced button with image support
2. **IconCommandButton** - Command button with icon/label template
3. **Link** - Hyperlink-style button
4. **RoundButton** - Circular button variant
5. **PaginatedList** - Paginated collection wrapper
6. **Expander** - Expandable container (MAUI 9+)
7. **GenericBrowser**, **SelectionList**, **FlyoutItem**, **Menu**, **Tab**, **TabMenu** - Custom navigation/selection UI components
8. **EditableField** - Generated control for edit scenarios

---

## Recommendations

### Priority 1: High-Impact Interactive Controls
- [ ] **ContentView** - Base container for custom controls (Medium effort)
- [ ] **IndicatorView** - Carousel indicator dots (Low effort)
- [ ] **BoxView** - Simple colored shapes (Low effort, low test priority)

### Priority 2: Web & Modern Controls
- [ ] **HybridWebView** - JavaScript/C# interop (Higher effort)
- [ ] **BlazorWebView** - Blazor app hosting (Higher effort, depends on Blazor setup)

### Priority 3: Gesture Support (Medium Effort)
- [ ] **GestureRecognizer Framework** - Base gesture infrastructure
- [ ] Consider gesture test patterns for mobile-specific interactions

### Priority 4: Optional/Lower Priority
- [ ] **GraphicsView** - 2D drawing canvas (Specialized use case)
- [ ] **Frame** - Container (Border provides similar functionality)
- [ ] **Map** - Geographic mapping (Requires separate NuGet, specialized testing)
- [ ] **Shape controls** - Visual primitives (Low test priority)
- [ ] **Gesture variants** - Multi-touch (Complex, Appium handles better)

---

## Coverage by Category

## Coverage by Category

```
Controls/ (Brinell Implementation)
├── Buttons/           [5/5] ✅ Complete (+ 3 extensions)
├── Collection/        [4/5] ⚠️  Missing: IndicatorView (+ 1 extension: PaginatedList)
├── Container/         [6/10] ⚠️  Missing: BoxView, ContentView, Frame, TwoPaneView
├── DateTime/          [2/2] ✅ Complete
├── Dialogs/           [1/1] ✅ Complete
├── Display/           [4/4] ✅ Complete
├── Generated/         [1/1] ✅ Custom control (EditableField)
├── Graphics/          [0/2] ❌ Missing: GraphicsView, HybridWebView
├── Media/             [2/2] ✅ Complete
├── Navigation/        [5/7] ⚠️  Partial (+ 5 Brinell extensions, Missing: TitleBar)
├── Range/             [2/2] ✅ Complete
├── Selection/         [3/5] ⚠️  Partial (+ 2 Brinell extensions)
├── Shapes/            [0/7] ❌ Missing: All shape controls
├── Text/              [3/3] ✅ Complete
├── Toggle/            [3/3] ✅ Complete
└── Web/Special/       [0/3] ❌ Missing: BlazorWebView, Map, TitleBar
```

---

## Official MAUI Control Inventory

### Pages (4 types - Not in Brinell scope)
- ContentPage, FlyoutPage, NavigationPage, TabbedPage

### Layouts (7 types - Not in Brinell scope)
- AbsoluteLayout, BindableLayout, FlexLayout, Grid, HorizontalStackLayout, StackLayout, VerticalStackLayout

### Views - IMPLEMENTED (39 with Brinell objects)
**Coverage: 39/70+ standard views**

## Notes

- **Brinell Extensions** provide enhanced versions of standard controls or domain-specific convenience wrappers
- **Gesture Recognition** support is limited; complex gestures may require Appium API
- **Shapes** are not included in this analysis (SVG/Drawing primitives)
- **Layout Controls** (StackLayout, FlexLayout, AbsoluteLayout) are deprecated in MAUI 10+; use Grid instead
- Control objects follow generic `<TScope>` pattern for fluent test API chaining
