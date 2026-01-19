# Requirements Document

## Introduction

Update the existing `Brinell.Maui.UITests` test project to work with the new TabbedPage-based sample app structure introduced in SPEC-005. The sample app was refactored from a Shell-based navigation to a native MAUI `TabbedPage` with 8 tabs (Basics, Containers, Forms, Lists, Gestures, Navigation, Toolkit, Media).

This spec focuses on updating existing tests and page objects to align with the new structure. Additional control tests will be added in a future spec.

## Alignment with Product Vision

This update ensures the test framework continues to validate the sample app after its structural redesign. Maintaining a working test suite is essential for:
- Verifying Brinell framework capabilities
- Demonstrating proper test patterns to users
- Enabling CI/CD validation of framework changes

## Scope

### In Scope
- Update `AppShellPage` to match new tab structure (8 tabs with correct AutomationIds)
- Update `MainPage` page object to work with `BasicsView` controls
- Update `MainPageTests` to work with new control structure
- Update `AppiumFixture` navigation methods
- Ensure tests compile and are structurally correct

### Out of Scope (Future Spec)
- New tests for FormsView controls
- New tests for ListsView controls
- New tests for ToolkitView controls
- New tests for MediaGalleryView controls
- New tests for GesturesView controls
- New tests for NavigationDemoView controls

## Requirements

### Requirement 1: Update AppShellPage for TabbedPage Navigation

**User Story:** As a test author, I want the AppShellPage to reflect the new 8-tab structure, so that I can navigate between tabs in my tests.

#### Acceptance Criteria

1. WHEN test uses `AppShellPage` THEN it SHALL have properties for all 8 tabs: `BasicsTab`, `ContainersTab`, `FormsTab`, `ListsTab`, `GesturesTab`, `NavigationTab`, `ToolkitTab`, `MediaTab`
2. WHEN `IsLoaded()` is called THEN it SHALL check for `BasicsTab` existence (first/default tab)
3. WHEN tab properties are accessed THEN they SHALL use correct AutomationIds matching MainPage.xaml

### Requirement 2: Update MainPage for BasicsView Controls

**User Story:** As a test author, I want the MainPage page object to expose controls from BasicsView, so that I can write tests against the new Basics tab.

#### Acceptance Criteria

1. WHEN MainPage is used THEN it SHALL expose all testable controls from BasicsView with correct AutomationIds
2. WHEN `IsLoaded()` is called THEN it SHALL check for `TitleLabel` existence
3. WHEN controls are accessed THEN they SHALL match the AutomationIds in BasicsView.xaml:
   - Labels: `TitleLabel`, `SubtitleLabel`, `CounterLabel`, `GreetingLabel`, `VolumeLabel`, `NotificationLabel`, `SelectedColorLabel`
   - Buttons: `IncrementButton`, `DecrementButton`, `ResetButton`, `GreetButton`, `ToggleLoadingButton`
   - Entries: `NameEntry`, `EmailEntry`
   - Editor: `MessageEditor`
   - Toggle controls: `NotificationSwitch`, `AgreeCheckBox`
   - Slider: `VolumeSlider`
   - Picker: `ColorPicker`, `BirthDatePicker`, `ReminderTimePicker`
   - Indicator: `LoadingIndicator`, `VolumeProgress`

### Requirement 3: Update MainPageTests for New Structure

**User Story:** As a test author, I want existing MainPageTests to work with the new structure, so that I can validate the Basics tab functionality.

#### Acceptance Criteria

1. WHEN tests reference tab navigation THEN they SHALL use `BasicsTab` instead of `MainTab`
2. WHEN tests check control existence THEN they SHALL use controls available in BasicsView
3. WHEN tests check greeting functionality THEN they SHALL work with BasicsViewModel behavior

### Requirement 4: Update AppiumFixture Navigation

**User Story:** As a test author, I want AppiumFixture navigation methods to work with TabbedPage, so that I can navigate to any tab.

#### Acceptance Criteria

1. WHEN `NavigateToMain()` is called THEN it SHALL click `BasicsTab` and wait for MainPage ready
2. WHEN `NavigateToContainerDemo()` is called THEN it SHALL click `ContainersTab` and wait for ContainerDemoPage ready
3. WHEN fixture initializes pages THEN it SHALL create AppShellPage, MainPage, and ContainerDemoPage with correct context

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Each page object represents one page/tab content
- **Modular Design**: Tab navigation (AppShellPage) separate from content page objects (MainPage, etc.)
- **Dependency Management**: Page objects depend only on Brinell.Maui framework
- **Clear Interfaces**: Use standard Brinell control interfaces (ITabControlObject, etc.)

### Performance
- Tab navigation should complete within 5 seconds
- Page ready checks should timeout at 5 seconds with clear error message

### Reliability
- Tests should handle TabbedPage initialization timing (may be slower than Shell)
- Wait methods should poll for conditions rather than arbitrary sleeps

### Maintainability
- AutomationIds should match between page objects and XAML exactly
- Comments should document which XAML file each control comes from
