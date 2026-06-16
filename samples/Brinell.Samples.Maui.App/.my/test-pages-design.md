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
- IconCommandButton
- ImageButton
- Link
- RoundButton

**Test Scenarios:**
- Tap button and verify command execution
- Verify button text/icon display
- Verify enabled/disabled state
- Verify visual feedback on press

**Validation Points:**
- Command fires once per tap
- Button state persists
- Accessibility: label readable

---

### 2. Collection Module
**Files:**
- View: `CollectionTestView.xaml`
- ViewModel: `CollectionTestViewModel.cs`

**Controls:**
- CarouselView
- CollectionView
- ListView
- PaginatedList
- TableView

**Test Scenarios:**
- Load data and verify item count
- Scroll and verify items visible
- Tap item and verify selection
- Refresh and reload data
- Paginate to next set

**Validation Points:**
- Items render correctly
- Selection fires event
- Scroll position maintained
- Refresh clears and reloads
- Pagination updates content

---

### 3. Container Module
**Files:**
- View: `ContainerTestView.xaml`
- ViewModel: `ContainerTestViewModel.cs`

**Controls:**
- Border
- Expander
- Grid
- RefreshView
- ScrollView
- SwipeView

**Test Scenarios:**
- Border displays with correct styling
- Expander toggles open/closed
- Grid arranges children
- RefreshView pulls and refreshes
- ScrollView scrolls to bottom
- SwipeView reveals action on swipe

**Validation Points:**
- Visual bounds correct
- Toggle state changes
- Child layout respected
- Refresh triggers action
- Swipe gesture recognized
- View can return to default

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

**Test Scenarios:**
- Label displays text
- Image loads and displays
- ActivityIndicator animates when running
- ProgressBar shows progress value
- Verify text wrapping/truncation

**Validation Points:**
- Text renders correctly
- Image visible (if file exists)
- Spinner animates smoothly
- Progress bar fills proportionally
- Visual hierarchy maintained

---

### 7. Media Module
**Files:**
- View: `MediaTestView.xaml`
- ViewModel: `MediaTestViewModel.cs`

**Controls:**
- MediaElement
- WebView

**Test Scenarios:**
- Load media and play
- Load web content
- Stop playback
- Navigate webview

**Validation Points:**
- Media loads
- Content renders
- Playback responds to controls
- WebView navigation works

---

### 8. Navigation Module
**Files:**
- View: `NavigationTestView.xaml`
- ViewModel: `NavigationTestViewModel.cs`

**Controls:**
- FlyoutItem
- Menu
- Tab
- TabMenu
- Toolbar

**Test Scenarios:**
- Tap menu item and navigate
- Select tab and verify content changes
- Tap toolbar button and verify action
- Verify menu open/close
- Flyout item highlighted when selected

**Validation Points:**
- Navigation fires correctly
- Selection syncs with content
- Toolbar buttons fire commands
- Flyout opens/closes smoothly
- Current item highlighted

---

### 9. Range Module
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

### 10. Selection Module
**Files:**
- View: `SelectionTestView.xaml`
- ViewModel: `SelectionTestViewModel.cs`

**Controls:**
- GenericBrowser
- Picker
- SelectionList

**Test Scenarios:**
- Open picker and select item
- Open browser and search/filter
- Verify selection list shows items
- Tap item and verify selection

**Validation Points:**
- Picker opens and closes
- Selection fires event
- Browser filters results
- SelectionList displays all items
- Selected state visually distinct

---

### 11. Text Module
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

### 12. Toggle Module
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
│       ├── MediaTestView.xaml
│       ├── NavigationTestView.xaml
│       ├── RangeTestView.xaml
│       ├── SelectionTestView.xaml
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
│       ├── MediaTestViewModel.cs
│       ├── NavigationTestViewModel.cs
│       ├── RangeTestViewModel.cs
│       ├── SelectionTestViewModel.cs
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
│       ├── MediaTestPage.cs
│       ├── NavigationTestPage.cs
│       ├── RangeTestPage.cs
│       ├── SelectionTestPage.cs
│       ├── TextTestPage.cs
│       └── ToggleTestPage.cs
└── Tests/
    └── ControlTests/
        ├── Buttons/
        │   ├── ButtonTests.cs
        │   ├── IconCommandButtonTests.cs
        │   ├── ImageButtonTests.cs
        │   ├── LinkTests.cs
        │   └── RoundButtonTests.cs
        ├── Collection/
        │   ├── CarouselViewTests.cs
        │   ├── CollectionViewTests.cs
        │   ├── ListViewTests.cs
        │   ├── PaginatedListTests.cs
        │   └── TableViewTests.cs
        ├── Container/
        │   ├── BorderTests.cs
        │   ├── ExpanderTests.cs
        │   ├── GridTests.cs
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
        │   └── ProgressBarTests.cs
        ├── Media/
        │   ├── MediaElementTests.cs
        │   └── WebViewTests.cs
        ├── Navigation/
        │   ├── FlyoutItemTests.cs
        │   ├── MenuTests.cs
        │   ├── TabTests.cs
        │   ├── TabMenuTests.cs
        │   └── ToolbarTests.cs
        ├── Range/
        │   ├── SliderTests.cs
        │   └── StepperTests.cs
        ├── Selection/
        │   ├── GenericBrowserTests.cs
        │   ├── PickerTests.cs
        │   └── SelectionListTests.cs
        ├── Text/
        │   ├── EditorTests.cs
        │   ├── EntryTests.cs
        │   └── SearchBarTests.cs
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
