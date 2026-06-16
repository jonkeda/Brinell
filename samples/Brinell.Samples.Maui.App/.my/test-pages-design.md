# Test Views Design Document

**Date:** 2025  
**Purpose:** Define UI test views for Brinell.Maui controls validation  
**Location:** `samples/Brinell.Samples.Maui.App/Views/TestViews/`

---

## Overview

This document outlines one test view per control category, each backed by a dedicated ViewModel. Each view provides a simple, focused environment for testing control behavior, accessibility, and state management. Tests are minimal and validate core interaction patterns.

---

## Control Categories & Test Pages

### 1. Buttons Module
**Files:** 
- View: `ButtonsTestView.xaml`
- ViewModel: `ButtonsTestViewModel.cs`

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
- View: `CollectionTestView.xaml`
- ViewModel: `CollectionTestViewModel.cs`

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
- View: `ContainerTestView.xaml`
- ViewModel: `ContainerTestViewModel.cs`

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

### 4. DateTime Module
**Files:**
- View: `DateTimeTestView.xaml`
- ViewModel: `DateTimeTestViewModel.cs`

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
- View: `DialogsTestView.xaml`
- ViewModel: `DialogsTestViewModel.cs`

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
- View: `DisplayTestView.xaml`
- ViewModel: `DisplayTestViewModel.cs`

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
- View: `GraphicsTestView.xaml`
- ViewModel: `GraphicsTestViewModel.cs`

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
- View: `MediaTestView.xaml`
- ViewModel: `MediaTestViewModel.cs`

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
- View: `NavigationTestView.xaml`
- ViewModel: `NavigationTestViewModel.cs`

**Controls:**
- (Navigation controls are shell-level, not standalone test controls)

**Test Scenarios:**
- Navigation via shell routing
- AppShell navigation menu access

**Validation Points:**
- Navigation works correctly
- Page transitions smooth

---

### 10. Range Module
**Files:**
- View: `RangeTestView.xaml`
- ViewModel: `RangeTestViewModel.cs`

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

---

### 11. Selection Module
**Files:**
- View: `SelectionTestView.xaml`
- ViewModel: `SelectionTestViewModel.cs`

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
- View: `ShapesTestView.xaml`
- ViewModel: `ShapesTestViewModel.cs`

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

### 13. Text Module
**Files:**
- View: `TextTestView.xaml`
- ViewModel: `TextTestViewModel.cs`

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

### 14. Toggle Module
**Files:**
- View: `ToggleTestView.xaml`
- ViewModel: `ToggleTestViewModel.cs`

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
TestView.xaml
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
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Brinell.Samples.Maui.App.Views.ButtonsTestView"
             Title="Buttons Test View">

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

public partial class ButtonsTestView : ContentPage
{
    public ButtonsTestView()
    {
        InitializeComponent();
        BindingContext = new ButtonsTestViewModel();
    }
}
```

### ViewModel Pattern

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Brinell.Samples.Maui.App.ViewModels;

public partial class ButtonsTestViewModel : ObservableObject
{
    [ObservableProperty]
    private string statusMessage = "Ready";

    [RelayCommand]
    private void Test()
    {
        // Execute test logic
        StatusMessage = "Button tapped successfully";
    }

    [RelayCommand]
    private void Reset()
    {
        // Clear state and reload
        StatusMessage = "Ready";
    }
}
```

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

Main app shell:
- AppShell.xaml
- Route all test view pages
- Include quick navigation menu to access each test view

---

## Directory Structure

```
samples/Brinell.Samples.Maui.App/
├── Views/
│   └── TestViews/
│       ├── ButtonsTestView.xaml
│       ├── CollectionTestView.xaml
│       ├── ContainerTestView.xaml
│       ├── DateTimeTestView.xaml
│       ├── DialogsTestView.xaml
│       ├── DisplayTestView.xaml
│       ├── GraphicsTestView.xaml
│       ├── MediaTestView.xaml
│       ├── RangeTestView.xaml
│       ├── SelectionTestView.xaml
│       ├── ShapesTestView.xaml
│       ├── TextTestView.xaml
│       └── ToggleTestView.xaml
├── ViewModels/
│   └── TestViewModels/
│       ├── ButtonsTestViewModel.cs
│       ├── CollectionTestViewModel.cs
│       ├── ContainerTestViewModel.cs
│       ├── DateTimeTestViewModel.cs
│       ├── DialogsTestViewModel.cs
│       ├── DisplayTestViewModel.cs
│       ├── GraphicsTestViewModel.cs
│       ├── MediaTestViewModel.cs
│       ├── RangeTestViewModel.cs
│       ├── SelectionTestViewModel.cs
│       ├── ShapesTestViewModel.cs
│       ├── TextTestViewModel.cs
│       └── ToggleTestViewModel.cs
└── AppShell.xaml
```

## UI Testing & Page Objects

Tests and PageObjects for these test views are created in **`Brinell.Maui.UITests`** project.

### Structure

```
Brinell.Maui.UITests/
├── PageObjects/
│   └── TestViewPages/
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
namespace Brinell.Maui.UITests.PageObjects.TestViewPages;

public class ButtonsTestPage : PageObject
{
    // Locators
    private By TestButton => By.Id("TestButton");
    private By StatusLabel => By.Id("StatusLabel");
    private By ResetButton => By.Id("ResetButton");

    // Actions
    public void TapTestButton()
    {
        var button = WaitFor(TestButton);
        button.Click();
    }

    public string GetStatusMessage()
    {
        return Find(StatusLabel).Text;
    }

    public void Reset()
    {
        Find(ResetButton).Click();
    }
}
```

### Test Class Pattern

Each individual control has its own dedicated test class:

```csharp
namespace Brinell.Maui.UITests.Tests.ControlTests.Buttons;

[TestFixture]
public class ButtonTests : UITestBase
{
    private ButtonsTestPage _page;

    [SetUp]
    public void SetUp()
    {
        _page = new ButtonsTestPage();
        NavigateTo("buttons-test");
    }

    [Test]
    public void ButtonTap_Executes_Command()
    {
        // Arrange
        var initialStatus = _page.GetStatusMessage();

        // Act
        _page.TapTestButton();

        // Assert
        Assert.That(_page.GetStatusMessage(), Is.Not.EqualTo(initialStatus));
    }

    [TearDown]
    public void Cleanup()
    {
        _page.Reset();
    }
}
```

```csharp
namespace Brinell.Maui.UITests.Tests.ControlTests.Buttons;

[TestFixture]
public class IconCommandButtonTests : UITestBase
{
    private ButtonsTestPage _page;

    [SetUp]
    public void SetUp()
    {
        _page = new ButtonsTestPage();
        NavigateTo("buttons-test");
    }

    [Test]
    public void IconCommandButtonTap_Executes_Command()
    {
        // Arrange & Act
        _page.TapIconCommandButton();

        // Assert
        Assert.That(_page.GetStatusMessage(), Contains.Substring("IconCommandButton"));
    }

    [TearDown]
    public void Cleanup()
    {
        _page.Reset();
    }
}
```

### Naming Conventions

- **PageObject classes**: `[ViewName]Page.cs` (e.g., `ButtonsTestPage.cs`) — one per view category
- **Test classes**: One per control in a category folder (e.g., `Buttons/ButtonTests.cs`, `Buttons/IconCommandButtonTests.cs`)
- **Test namespace**: `Brinell.Maui.UITests.Tests.ControlTests.[Category]`
- **Locators**: Use `AutomationId` in XAML views for reliable identification
- **Test methods**: Follow `[Control][Action]_[Condition]_[Expected]` pattern

### Test Execution

Run control view tests from solution root:

```powershell
# Run all UITests
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false

# Run specific control category
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -k ButtonTests -v:minimal /nr:false

# Run specific control with namespace
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -k "Brinell.Maui.UITests.Tests.ControlTests.Buttons" -v:minimal /nr:false
```

---

## Future Enhancements

- Accessibility audit for each view
- Keyboard navigation testing
- Dark/Light theme validation
- Orientation change handling
- Multi-language label support
