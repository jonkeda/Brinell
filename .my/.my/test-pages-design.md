# Test Views Design Document

**Date:** 2025  
**Purpose:** Define UI test views for Brinell.Maui controls validation  
**Location:** `samples/Brinell.Samples.Maui.App/Views/`

---

## Overview

This document outlines one test view per control category, each backed by a dedicated ViewModel. Each view provides a simple, focused environment for testing control behavior, accessibility, and state management. Tests are minimal and validate core interaction patterns.

**Implementation Steps for Each Module:**

1. **Create the View (XAML)** - Follow the XAML View Pattern below
2. **Create the Code-Behind** - Simple InitializeComponent call
3. **Create the ViewModel** - Inherit from ParentViewModel, implement property bindings and command handling
4. **Register in Navigation** - Add tab to MainPage.xaml and AppShellPage.cs (see Navigation section)
5. **Create UI Tests** - 
   - **PageObject** in `testsnew/Brinell.Maui.UITests/Pages/[Module]TestPage.cs`
   - **Test Classes** in `testsnew/Brinell.Maui.UITests/Tests/[Category]/` (one test class per control)
   - Use xUnit with `[Collection("Appium")]`, `[Fact]`, and `[Trait]` attributes
   - Follow fluent assertion patterns and test naming conventions

See **DateTime Module** (sections 4, Navigation, Test Automation, and UI Testing & Page Objects) for a complete example implementation.

---

## Control Categories & Test Pages

### 1. Buttons Module X
**Files:** 
- View: `ButtonsView.xaml`
- ViewModel: `ButtonsViewModel.cs`

**Controls:**
- Button
- ImageButton

**Test Scenarios:**
- Tap button and verify command execution
- Verify button text/image display
- Verify enabled/disabled state
- Verify visual feedback on press
- Tap image button and verify command

**Validation Points:**
- Command fires once per tap
- Button state persists
- Accessibility: label readable
- ImageButton loads and displays image

---

### 2. Collection Module
**Files:**
- View: `CollectionView.xaml`
- ViewModel: `CollectionViewModel.cs`

**Controls:**
- CarouselView
- CollectionView
- IndicatorView
- ListView
- TableView

**Test Scenarios:**
- Load data and verify item count
- Scroll and verify items visible
- Tap item and verify selection
- Refresh and reload data
- Verify carousel navigation

**Validation Points:**
- Items render correctly
- Selection fires event
- Scroll position maintained
- Refresh clears and reloads
- IndicatorView displays position

---

### 3. Container Module
**Files:**
- View: `ContainerView.xaml`
- ViewModel: `ContainerViewModel.cs`

**Controls:**
- Border
- BoxView
- ContentView
- Frame
- Grid
- IsoPaneView
- RefreshView
- ScrollView
- SwipeView

**Test Scenarios:**
- Border displays with correct styling
- Grid arranges children
- RefreshView pulls and refreshes
- ScrollView scrolls to bottom
- SwipeView reveals action on swipe
- Frame displays with border
- ContentView contains child

**Validation Points:**
- Visual bounds correct
- Child layout respected
- Refresh triggers action
- Swipe gesture recognized
- View can return to default
- BoxView displays color/dimensions

---

### 4. DateTime Module X
**Files:**
- View: `DateTimeView.xaml`
- ViewModel: `DateTimeViewModel.cs`

**Controls:**
- DatePicker
- TimePicker

**Test Scenarios:**
- Open date picker and select date
- Verify date displayed
- Open time picker and select time
- Verify time displayed
- Verify minimum/maximum constraints

**Validation Points:**
- Picker opens
- Selection fires event
- Value updates immediately
- Format is consistent
- Out-of-range values rejected

---

### 5. Dialogs Module
**Files:**
- View: `DialogsView.xaml`
- ViewModel: `DialogsViewModel.cs`

**Controls:**
- ContentDialog

**Test Scenarios:**
- Show dialog
- Tap primary button
- Tap secondary button
- Tap cancel/dismiss
- Verify dialog closes

**Validation Points:**
- Dialog modal (blocks background)
- Button tap fires correct action
- Dialog dismisses on button tap
- Content renders inside dialog

---

### 6. Display Module
**Files:**
- View: `DisplayView.xaml`
- ViewModel: `DisplayViewModel.cs`

**Controls:**
- ActivityIndicator
- Image
- Label
- ProgressBar
- TitleBar

**Test Scenarios:**
- Label displays text
- Image loads and displays
- ActivityIndicator animates when running
- ProgressBar shows progress value
- TitleBar displays with title
- Verify text wrapping/truncation

**Validation Points:**
- Text renders correctly
- Image visible (if file exists)
- Spinner animates smoothly
- Progress bar fills proportionally
- Visual hierarchy maintained
- TitleBar positioned correctly

---

### 7. Graphics Module
**Files:**
- View: `GraphicsView.xaml`
- ViewModel: `GraphicsViewModel.cs`

**Controls:**
- GraphicsView

**Test Scenarios:**
- Load graphics and verify rendering
- Verify graphics display correctly
- Test custom drawing operations

**Validation Points:**
- Graphics render without errors
- Content displays
- Performance acceptable

---

### 8. Media Module
**Files:**
- View: `MediaView.xaml`
- ViewModel: `MediaViewModel.cs`

**Controls:**
- BlazorWebView
- HybridWebView
- MediaElement
- WebView

**Test Scenarios:**
- Load media and play
- Load web content
- Stop playback
- Navigate webview
- Load Blazor component

**Validation Points:**
- Media loads
- Content renders
- Playback responds to controls
- WebView navigation works
- Blazor component initializes

---

### 9. Navigation Module
**Files:**
- View: `NavigationView.xaml`
- ViewModel: `NavigationViewModel.cs`

**Controls:**
- (Navigation controls are shell-level, not standalone test controls)

**Test Scenarios:**
- Navigation via shell routing
- AppShell navigation menu access

**Validation Points:**
- Navigation works correctly
- Page transitions smooth

---

### 10. Range Module X
**Files:**
- View: `RangeView.xaml`
- ViewModel: `RangeViewModel.cs`

**Controls:**
- Slider
- Stepper

**Test Scenarios:**
- Drag slider and verify value changes
- Tap stepper increment/decrement
- Verify min/max bounds
- Verify step size

**Validation Points:**
- Value updates continuously while dragging
- Stepper changes value by step amount
- Value bounded by min/max
- Event fires on value change

**Shell Navigation:**
A ShellContent needs to be added to `AppShell.xaml` and a `RangePage.xaml` wrapper page must be created. See the **ShellContent Pattern** and **RangePage.xaml Pattern** sections below for implementation details.

---

### 11. Selection Module X
**Files:**
- View: `SelectionView.xaml`
- ViewModel: `SelectionViewModel.cs`

**Controls:**
- Picker

**Test Scenarios:**
- Open picker and select item
- Verify selection displays
- Verify picker closes after selection

**Validation Points:**
- Picker opens and closes
- Selection fires event
- Selected value displays
- Selected state visually distinct

---

### 12. Shapes Module
**Files:**
- View: `ShapesView.xaml`
- ViewModel: `ShapesViewModel.cs`

**Controls:**
- Ellipse
- Line
- Path
- Polygon
- Polyline
- Rectangle
- RoundRectangle

**Test Scenarios:**
- Shapes render correctly
- Verify dimensions and positioning
- Verify fill and stroke colors

**Validation Points:**
- All shapes display without error
- Geometric calculations correct
- Colors apply correctly
- Layout respects bounds

---

### 13. Text Module X
**Files:**
- View: `TextView.xaml`
- ViewModel: `TextViewModel.cs`

**Controls:**
- Editor
- Entry
- SearchBar

**Test Scenarios:**
- Type text in entry
- Type multi-line text in editor
- Type search query in search bar
- Clear text
- Verify placeholder text
- Verify keyboard type

**Validation Points:**
- Text input captured
- Text displayed correctly
- Placeholder shows when empty
- Clear button removes text
- Text committed on return/done

---

### 14. Toggle Module X
**Files:**
- View: `ToggleView.xaml`
- ViewModel: `ToggleViewModel.cs`

**Controls:**
- CheckBox
- RadioButton
- Switch

**Test Scenarios:**
- Tap checkbox and verify toggle
- Tap radio button and verify selection
- Toggle switch and verify state
- Verify radio buttons are mutually exclusive
- Verify label associated with control

**Validation Points:**
- Toggle fires event
- State persists
- Radio buttons exclusive (only one can be selected)
- Visual state reflects checked/unchecked
- Label clickable (selects/deselects control)

---

## Test View Structure

Each test view follows this pattern:

```
View.xaml
├── VerticalStackLayout (main container)
│   ├── Label (page title)
│   ├── Label (description)
│   ├── [Control under test]
│   ├── Label (status/result)
│   └── Button (reset / clear)
```

### XAML View Pattern

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:viewModels="using:Brinell.Samples.Maui.App.ViewModels"
             x:Class="Brinell.Samples.Maui.App.Views.ButtonsView"
             Title="Buttons Test View">

    <ContentView.BindingContext>
        <viewModels:ButtonsViewModel />
    </ContentView.BindingContext>

    <VerticalStackLayout Padding="20" Spacing="10">
        <Label Text="Buttons Module Test" FontSize="18" FontAttributes="Bold" />
        <Label Text="Test all button variants and interactions" />

        <!-- Test Controls -->
        <Button Text="Test Button" 
                Command="{Binding TestCommand}"
                AutomationId="TestButton" />

        <!-- Status Display -->
        <Label Text="{Binding StatusMessage}" 
               AutomationId="StatusLabel" />

        <!-- Reset -->
        <Button Text="Reset" 
                Command="{Binding ResetCommand}"
                AutomationId="ResetButton" />
    </VerticalStackLayout>
</ContentPage>
```

### Code-Behind View Pattern

```csharp
namespace Brinell.Samples.Maui.App.Views;

public partial class ButtonsView
{
    public ButtonsView()
    {
        InitializeComponent();
    }
}
```

### ViewModel Pattern

```csharp
using System.Windows.Input;

namespace Brinell.Samples.Maui.App.ViewModels;

public class ButtonsViewModel : ParentViewModel
{
    private string statusMessage = "Ready. Click any button to test.";
    private int tapCount = 0;

    public string StatusMessage
    {
        get => statusMessage;
        set
        {
            if (statusMessage != value)
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand TestButtonCommand => new RelayCommand(TestButton);
    public ICommand ResetCommand => new RelayCommand(Reset);

    private void TestButton()
    {
        tapCount++;
        StatusMessage = $"✓ Button tapped {tapCount} time{(tapCount != 1 ? "s" : "")}. Command executed successfully.";
    }

    private void Reset()
    {
        StatusMessage = "Ready. Click any button to test.";
        tapCount = 0;
    }
}
```

### ShellContent Pattern

ShellContent elements are used within an `AppShell.xaml` to register a tab in the shell navigation. Each test view module should have a corresponding ShellContent entry:

```xaml
<!-- Module Name Tab -->
<ShellContent
    Title="ModuleName"
    Icon="tab_module.png"
    ContentTemplate="{DataTemplate local:ModuleNamePage}"
    Route="ModuleNamePage" />
```

**Key attributes:**
- **Title** - The tab label displayed to the user
- **Icon** - The tab icon image file (located in resources)
- **ContentTemplate** - DataTemplate binding to the page (e.g., `local:RangePage`)
- **Route** - Unique route identifier for navigation (matches the page name convention)

**Example for Range Module:**

```xaml
<!-- Range Tab -->
<ShellContent
    Title="Range"
    Icon="tab_range.png"
    ContentTemplate="{DataTemplate local:RangePage}"
    Route="RangePage" />
```

---

### RangePage.xaml Pattern

A page like `RangePage.xaml` provides navigation entry point to the Range test controls. It wraps the test view in a ContentPage for shell navigation:

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:testViews="using:Brinell.Samples.Maui.App.Views"
             x:Class="Brinell.Samples.Maui.App.Pages.RangePage"
             Title="Range">

    <testViews:RangeView />

</ContentPage>
```

**Code-Behind:**

```csharp
namespace Brinell.Samples.Maui.App.Pages;

public partial class RangePage : ContentPage
{
    public RangePage()
    {
        InitializeComponent();
    }
}
```

**Directory Structure Note:**

- Pages are located in: `Brinell.Samples.Maui.App/Pages/`
- Views are located in: `Brinell.Samples.Maui.App/Views/`
- The page acts as a shell-navigable container; the view contains the actual test controls

---

## Validation Checklist

For each test page:

- [ ] All controls from category render without error
- [ ] Basic interaction works (tap, type, select, etc.)
- [ ] State changes are reflected visually
- [ ] Events fire and are handled correctly
- [ ] Data persists across interactions
- [ ] Reset button clears all state
- [ ] Labels and descriptions are clear
- [ ] No layout issues or overflow

---

## Test Automation Notes

Each test page should be easily testable with UITest automation:

1. **Name all interactive elements** — Use AutomationId for programmatic access
2. **Status labels** — Display test results (success/failure) for validation
3. **Clear actions** — Use single taps/clicks; avoid complex gestures initially
4. **Deterministic state** — Reset to known state before each test
5. **Wait conditions** — Ensure UI is stable before asserting

---

## Navigation

Main app navigation uses TabbedPage (`MainPage.xaml`). Each test view module must be registered in the UI navigation:

**Steps to add a new test view to navigation:**

1. **Add Tab to MainPage.xaml:**
   ```xaml
   <!-- Module Name Tab -->
   <ContentPage Title="ModuleName" AutomationId="ModuleNameTab">
       <testViews:ModuleNameView />
   </ContentPage>
   ```

2. **Add Tab to AppShellPage.cs** (test fixture navigation):
   ```csharp
   // In constructor:
   ModuleNameTab = new TabViewControl<AppShellPage>(this, "ModuleNameTab", "ModuleName");

   // In Tab Controls region:
   public ITabControlObject<AppShellPage> ModuleNameTab { get; }
   ```

3. **Update test fixture navigation** - Tests can now navigate to the view:
   ```csharp
   _fixture.AppShell.ModuleNameTab.Click();
   ```

See **DateTime Module** (MainPage.xaml and AppShellPage.cs) for implementation examples.

---

## Directory Structure

```
samples/Brinell.Samples.Maui.App/
├── Views/
│   └── Views/
│       ├── ButtonsView.xaml
│       ├── CollectionView.xaml
│       ├── ContainerView.xaml
│       ├── DateTimeView.xaml
│       ├── DialogsView.xaml
│       ├── DisplayView.xaml
│       ├── GraphicsView.xaml
│       ├── MediaView.xaml
│       ├── RangeView.xaml
│       ├── SelectionView.xaml
│       ├── ShapesView.xaml
│       ├── TextView.xaml
│       └── ToggleView.xaml
├── ViewModels/
│   └── ViewModels/
│       ├── ButtonsViewModel.cs
│       ├── CollectionViewModel.cs
│       ├── ContainerViewModel.cs
│       ├── DateTimeViewModel.cs
│       ├── DialogsViewModel.cs
│       ├── DisplayViewModel.cs
│       ├── GraphicsViewModel.cs
│       ├── MediaViewModel.cs
│       ├── RangeViewModel.cs
│       ├── SelectionViewModel.cs
│       ├── ShapesViewModel.cs
│       ├── TextViewModel.cs
│       └── ToggleViewModel.cs
└── AppShell.xaml
```

## UI Testing & Page Objects

Tests and PageObjects for these test views are created in **`Brinell.Maui.UITests`** project.

### Structure

```
Brinell.Maui.UITests/
├── PageObjects/
│   └── ViewPages/
│       ├── ButtonsTestPage.cs
│       ├── CollectionTestPage.cs
│       ├── ContainerTestPage.cs
│       ├── DateTimeTestPage.cs
│       ├── DialogsTestPage.cs
│       ├── DisplayTestPage.cs
│       ├── GraphicsTestPage.cs
│       ├── MediaTestPage.cs
│       ├── RangeTestPage.cs
│       ├── SelectionTestPage.cs
│       ├── ShapesTestPage.cs
│       ├── TextTestPage.cs
│       └── ToggleTestPage.cs
└── Tests/
    └── ControlTests/
        ├── Buttons/
        │   ├── ButtonTests.cs
        │   └── ImageButtonTests.cs
        ├── Collection/
        │   ├── CarouselViewTests.cs
        │   ├── CollectionViewTests.cs
        │   ├── IndicatorViewTests.cs
        │   ├── ListViewTests.cs
        │   └── TableViewTests.cs
        ├── Container/
        │   ├── BorderTests.cs
        │   ├── BoxViewTests.cs
        │   ├── ContentViewTests.cs
        │   ├── FrameTests.cs
        │   ├── GridTests.cs
        │   ├── IsoPaneViewTests.cs
        │   ├── RefreshViewTests.cs
        │   ├── ScrollViewTests.cs
        │   └── SwipeViewTests.cs
        ├── DateTime/
        │   ├── DatePickerTests.cs
        │   └── TimePickerTests.cs
        ├── Dialogs/
        │   └── ContentDialogTests.cs
        ├── Display/
        │   ├── ActivityIndicatorTests.cs
        │   ├── ImageTests.cs
        │   ├── LabelTests.cs
        │   ├── ProgressBarTests.cs
        │   └── TitleBarTests.cs
        ├── Graphics/
        │   └── GraphicsViewTests.cs
        ├── Media/
        │   ├── BlazorWebViewTests.cs
        │   ├── HybridWebViewTests.cs
        │   ├── MediaElementTests.cs
        │   └── WebViewTests.cs
        ├── Range/
        │   ├── SliderTests.cs
        │   └── StepperTests.cs
        ├── Selection/
        │   └── PickerTests.cs
        ├── Shapes/
        │   ├── EllipseTests.cs
        │   ├── LineTests.cs
        │   ├── PathTests.cs
        │   ├── PolygonTests.cs
        │   ├── PolylineTests.cs
        │   ├── RectangleTests.cs
        │   └── RoundRectangleTests.cs
        └── Text/
            ├── EditorTests.cs
            ├── EntryTests.cs
            └── SearchBarTests.cs
        └── Toggle/
            ├── CheckBoxTests.cs
            ├── RadioButtonTests.cs
            └── SwitchTests.cs
```

### PageObject Pattern

Each test view has a corresponding PageObject that encapsulates element locators and interactions:

```csharp
namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the ButtonsTestView. Exposes all button controls and their interactions.
/// Demonstrates the page object pattern with control locators and action methods.
/// </summary>
public class ButtonsTestPage : PageObjectBase<ButtonsTestPage>
{
    public ButtonsTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "ButtonsTestPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the status label exists
        return StatusLabel.IsExists();
    }

    #region Buttons

    /// <summary>
    /// The basic Button test control.
    /// </summary>
    public Button<ButtonsTestPage> TestButton => new (this, "TestButton");

    /// <summary>
    /// The ImageButton test control.
    /// </summary>
    public Button<ButtonsTestPage> TestImageButton => new (this, "TestImageButton");

    /// <summary>
    /// The Reset button.
    /// </summary>
    public Button<ButtonsTestPage> ResetButton => new (this, "ResetButton");

    #endregion

    #region Labels

    /// <summary>
    /// The status message label showing test results.
    /// </summary>
    public Label<ButtonsTestPage> StatusLabel => new (this, "StatusLabel");

    #endregion
}
```

### Test Class Pattern

Each individual control has its own dedicated test class using xUnit with fluent assertions:

```csharp
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Buttons;

/// <summary>
/// UI tests for the Button control in the ButtonsTestView.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Button")]
public class ButtonTests
{
    private readonly AppiumFixture _fixture;

    public ButtonTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.AppShell.ButtonsTab.Click();
    }

    private ButtonsTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the Button exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Button_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestButton.AssertExists();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the Button executes its command.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task Button_Tap_ExecutesCommand()
    {
        // Arrange
        var page = GetPage();

        // Act & Assert
        page.TestButton.Click()
            .StatusLabel.AssertTextContains("Button tapped");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the Button multiple times increments the tap count.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task Button_MultipleTaps_IncrementsCount()
    {
        // Arrange
        var page = GetPage();

        // Act & Assert
        page.TestButton.Click()
            .StatusLabel.AssertTextContains("1 time")
            .TestButton.Click()
            .StatusLabel.AssertTextContains("2 times");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the Reset button clears the status.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Reset")]
    public Task Button_Reset_ClearsStatus()
    {
        // Arrange
        var page = GetPage();

        // Act & Assert
        page.TestButton.Click()
            .StatusLabel.AssertTextContains("Button tapped")
            .ResetButton.Click()
            .StatusLabel.AssertTextContains("Ready");

        return Task.CompletedTask;
    }
}
```

### Naming Conventions

- **PageObject classes**: `[ViewName]TestPage.cs` (e.g., `ButtonsTestPage.cs`) — one per view category
- **Test classes**: One per control in a category folder (e.g., `Buttons/ButtonTests.cs`, `Buttons/ImageButtonTests.cs`)
- **Test namespace**: `Brinell.Maui.UITests.Tests.[Category]`
- **PageObject namespace**: `Brinell.Maui.UITests.Pages`
- **Locators**: Use `AutomationId` in XAML views for reliable identification
- **Test methods**: Follow `[Control]_[Method]_[Expected]` pattern (e.g., `Button_Tap_ExecutesCommand`)
- **Traits**: Use xUnit Trait attributes for categorization (Category, Control, Method)
- **Collection**: Use `[Collection("Appium")]` for Appium fixture sharing
- **PageObject inheritance**: Inherit from `PageObjectBase<T>` where T is the PageObject itself

### Test Execution

Run control view tests from solution root using xUnit-based test runner:

```powershell
# Run all UI tests
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false

# Run specific control category
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -k ButtonTests -v:minimal /nr:false

# Run specific test with full namespace
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -k "Brinell.Maui.UITests.Tests.Buttons" -v:minimal /nr:false
```

---


---

## Implementation status (revised)

Modules marked **X** in the headings below were already implemented. This section records
what was added since, and one hard platform limit discovered in the process.

### The 10-tab ceiling — read this before adding a module

**Exactly 10 Shell tabs are reachable on Windows.** An eleventh pushes the tenth into a
WinUI overflow "More" menu, where `ShellContent` cannot click it — it locates a tab by
control type `TabItem` plus Title, and an overflowed tab is not present under that.

This was found the hard way: adding four module tabs took the count to 14 and silently
broke navigation for **13 previously passing tests**, including the automation probe.
`TabBarCapacityProbeTests` now measures and reports this; run it first if navigation starts
failing for no apparent reason.

The design's step 4 ("Register in Navigation — add tab to MainPage.xaml") therefore does not
scale past 10 modules. Modules beyond that must be reached by **Shell route**, registered in
`AppShell.xaml.cs` and linked from an existing page. The `AutomationProbeView` hosts those
links because it is already the diagnostics surface.

### Added: sample pages for four modules

| Module | View | ViewModel | Reached by |
|---|---|---|---|
| Container (3) | `ContainerView.xaml` | `ContainerViewModel` | route from probe page |
| Collection (2) | `CollectionModuleView.xaml` | `CollectionModuleViewModel` | route from probe page |
| Shapes (12) + Graphics (7) | `ShapesView.xaml` | none needed | route from probe page |
| Dialogs (5) | `DialogsView.xaml` | `DialogsViewModel` | route from probe page |

Shapes and Graphics share a view: both are non-interactive, and the design's validation
points for each are "renders without error" and "dimensions correct". Two pages for eight
controls needing only existence checks would be ceremony.

`CollectionModuleView` deliberately omits `CollectionView` — it already has a dedicated
page (`GridCollectionDemoView`) with 15 tests covering item scoping, mutation, and empty
state.

### Platform notes affecting these modules

Measured on Windows/FlaUI. The planned Android/iOS phase should re-measure rather than
inherit these:

| Control | Status | Consequence for the design's scenarios |
|---|---|---|
| `Frame` | not addressable | deprecated in MAUI; use `Border` |
| `SwipeView` | not addressable | the swipe scenario is a mobile gesture |
| `RefreshView` | not addressable | pull-to-refresh is a mobile gesture; the command is driven by a button instead |
| `BoxView` | not addressable | a drawing primitive with no AutomationPeer; no children and no behaviour, so nothing is lost |
| `CollectionView` | recycles rows | ~30 of 63 rows exist at once even at 100% scroll, so lists here are kept short |

### Incomplete: Container module tests

`ContainerModuleTests` (10 tests) is written but **not passing**. The tests themselves are
sound — route navigation was verified working, and the container objects resolve — but the
fixture cannot reliably return to the probe page between tests once a route has pushed a
page onto the Shell stack. That is a navigation-plumbing problem introduced by the 10-tab
workaround, not a container defect.

**No pre-existing test regressed**: Navigation, Collection, Grid container, and probe suites
are 47 passed / 2 skipped, unchanged.

The remaining work is a fixture helper that pops the Shell stack back to a tab root before
each module navigation. Page objects and tests for Collection, Shapes, and Dialogs are not
written yet, and should wait until that helper exists — they will need it too.
