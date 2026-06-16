# Test Pages Design Document

**Date:** 2025  
**Purpose:** Define UI test pages for Brinell.Maui controls validation  
**Location:** `samples/Brinell.Samples.Maui.App/Pages/TestPages/`

---

## Overview

This document outlines one test page per control category. Each page provides a simple, focused environment for testing control behavior, accessibility, and state management. Tests are minimal and validate core interaction patterns.

---

## Control Categories & Test Pages

### 1. Buttons Module
**File:** `ButtonsTestPage.xaml` / `ButtonsTestPage.xaml.cs`

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
**File:** `CollectionTestPage.xaml` / `CollectionTestPage.xaml.cs`

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
**File:** `ContainerTestPage.xaml` / `ContainerTestPage.xaml.cs`

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
**File:** `DateTimeTestPage.xaml` / `DateTimeTestPage.xaml.cs`

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
**File:** `DialogsTestPage.xaml` / `DialogsTestPage.xaml.cs`

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
**File:** `DisplayTestPage.xaml` / `DisplayTestPage.xaml.cs`

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
**File:** `MediaTestPage.xaml` / `MediaTestTestPage.xaml.cs`

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
**File:** `NavigationTestPage.xaml` / `NavigationTestPage.xaml.cs`

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
**File:** `RangeTestPage.xaml` / `RangeTestPage.xaml.cs`

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
**File:** `SelectionTestPage.xaml` / `SelectionTestPage.xaml.cs`

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
**File:** `TextTestPage.xaml` / `TextTestPage.xaml.cs`

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
**File:** `ToggleTestPage.xaml` / `ToggleTestPage.xaml.cs`

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

## Test Page Structure

Each test page follows this pattern:

```
TestPage.xaml
├── VerticalStackLayout (main container)
│   ├── Label (page title)
│   ├── Label (description)
│   ├── [Control under test]
│   ├── Label (status/result)
│   └── Button (reset / clear)
```

### Code-Behind Pattern

```csharp
public partial class [Control]TestPage : ContentPage
{
    public [Control]TestPage()
    {
        InitializeComponent();
        SetupControls();
    }

    private void SetupControls()
    {
        // Initialize test controls with sample data
    }

    private void OnControlInteraction(object sender, EventArgs e)
    {
        // Update status label
        // Verify expected behavior
    }

    private void OnReset(object sender, EventArgs e)
    {
        // Clear selections, reload data, reset state
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

Main test page shell:
- App.xaml.cs or AppShell.xaml
- Route all test category pages
- Include quick navigation menu

---

## Future Enhancements

- Accessibility audit for each page
- Keyboard navigation testing
- Dark/Light theme validation
- Orientation change handling
- Multi-language label support
