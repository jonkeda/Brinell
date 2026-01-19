# Requirements Document: MAUI Sample App Tab Navigation

## Introduction

This specification defines the updates needed to reorganize the Brinell MAUI sample application to use a proper tab-based navigation structure using CommunityToolkit.Maui TabView. The current app has multiple pages (9 existing pages) but limited navigation—only 3 tabs are visible with manual button-based switching. This update will:

1. Replace the current button-based tab navigation with CommunityToolkit TabView
2. Make all existing pages accessible via tabs
3. Add new tabs demonstrating MAUI Community Toolkit controls
4. Add new tabs demonstrating standard MAUI collection/list controls

## Alignment with Product Vision

The Brinell framework exists to enable comprehensive UI testing of .NET applications. The sample app serves as both:
- A testbed for validating Brinell control implementations
- A reference for test writers on how to structure testable MAUI apps

By expanding the sample app to include all major control types, we ensure:
- Complete test coverage for all Brinell control base classes
- Real-world examples of AutomationId patterns for complex controls
- A comprehensive demo app for CommunityToolkit controls testing

## Current State Analysis

### Existing Pages (9 pages, only 3 accessible via tabs)
| Page | Description | Currently Accessible |
|------|-------------|---------------------|
| MainPage.xaml | Counter, Entry, Toggle, Slider, Picker, ActivityIndicator | ✅ (Tab: Main) |
| ContainerDemoView.xaml | Container scoping, nested containers, CollectionView | ✅ (Tab: Containers) |
| TabbedPageDemoPage.xaml | Demonstrates TabbedPage control | ✅ (Tab: TabbedPage Demo) |
| AdvancedPage.xaml | Gestures (Tap, Pan, Pinch, Swipe), Grid, FlexLayout | ❌ Not accessible |
| DataGridPage.xaml | CarouselView, CollectionView, RefreshView, Grouped lists | ❌ Not accessible |
| ValidationPage.xaml | Form validation patterns | ❌ Not accessible |
| UserFormPage.xaml | Complex form with multiple inputs | ❌ Not accessible |
| NavigationDemoPage.xaml | Navigation patterns | ❌ Not accessible |
| DashboardPage.xaml | Dashboard layout | ❌ Not accessible |
| MediaGalleryPage.xaml | Image/media display | ❌ Not accessible |

### Missing Control Demonstrations
- **Standard MAUI**: ListView (classic), TreeView-like patterns, TableView
- **CommunityToolkit**: Expander, AvatarView, DrawingView, Popup, Snackbar

---

## Requirements

### REQ-001: Replace Tab Navigation with CommunityToolkit TabView

**User Story:** As a test writer, I want the sample app to use CommunityToolkit.Maui TabView for navigation, so that I can test the TabViewControl implementation in Brinell.

#### Acceptance Criteria

1. WHEN the app launches THEN the MainPage SHALL display a TabView with multiple TabViewItems
2. WHEN a TabViewItem is tapped THEN the corresponding content view SHALL be displayed
3. WHEN TabView is used THEN each TabViewItem SHALL have a unique AutomationId for testing
4. IF the TabView has more tabs than fit on screen THEN the TabView SHALL be scrollable

### REQ-002: Organize Tabs into Logical Groups

**User Story:** As a developer exploring the sample app, I want tabs organized by category, so that I can find relevant control demos quickly.

#### Acceptance Criteria

1. WHEN viewing the TabView THEN tabs SHALL be organized into these categories:
   - **Basics** - Counter, inputs, toggles, sliders (current MainPage content)
   - **Containers** - Container scoping demo (current ContainerDemoView)
   - **Forms** - UserForm, Validation pages
   - **Lists** - ListView, CollectionView, CarouselView, grouped data
   - **Gestures** - Tap, Pan, Pinch, Swipe (current AdvancedPage)
   - **Navigation** - Navigation patterns demo
   - **Toolkit** - CommunityToolkit controls
   - **Media** - Images, gallery

2. WHEN there are many tabs THEN the TabView SHALL support horizontal scrolling

### REQ-003: Add ListView Demo Tab

**User Story:** As a test writer, I want a ListView demo, so that I can test the classic ListView control which behaves differently from CollectionView.

#### Acceptance Criteria

1. WHEN viewing the Lists tab THEN there SHALL be a ListView demonstrating:
   - Simple item list with TextCell
   - List with ImageCell
   - List with ViewCell (custom template)
   - Pull-to-refresh capability
   - Item selection (single and multiple modes)
   
2. WHEN ListView items are rendered THEN each item SHALL have an indexed AutomationId (e.g., `ListItem_0`, `ListItem_1`)

### REQ-004: Add TableView Demo Tab

**User Story:** As a test writer, I want a TableView demo, so that I can test settings-style UI patterns common in mobile apps.

#### Acceptance Criteria

1. WHEN viewing the Forms tab THEN there SHALL be a TableView demonstrating:
   - TableSection with header
   - EntryCell for text input
   - SwitchCell for toggles
   - TextCell for display
   - Custom ViewCell

2. WHEN TableView cells are rendered THEN each cell SHALL have a unique AutomationId

### REQ-005: Add CommunityToolkit Expander Demo

**User Story:** As a test writer, I want an Expander demo, so that I can test expand/collapse patterns using MauiExpandableControlBase.

#### Acceptance Criteria

1. WHEN viewing the Toolkit tab THEN there SHALL be Expander controls demonstrating:
   - Simple expander with header and content
   - Nested expanders (accordion pattern)
   - Expander with complex content (lists, forms)
   
2. WHEN an Expander is rendered THEN it SHALL have AutomationId for both header and content areas
3. WHEN an Expander is expanded/collapsed THEN the state change SHALL be testable via IsExpanded

### REQ-006: Add CommunityToolkit TabView Within Page Demo

**User Story:** As a test writer, I want a TabView-within-TabView demo, so that I can test nested tab scenarios.

#### Acceptance Criteria

1. WHEN viewing the Toolkit tab THEN there SHALL be a nested TabView demonstrating:
   - TabView as content within an outer TabViewItem
   - Multiple inner tabs with different content
   
2. WHEN inner TabViewItems are rendered THEN they SHALL have unique AutomationIds distinct from outer tabs

### REQ-007: Add TreeView-like Hierarchical Demo

**User Story:** As a test writer, I want a hierarchical/tree structure demo, so that I can test expandable hierarchical data patterns.

#### Acceptance Criteria

1. WHEN viewing the Lists tab THEN there SHALL be a tree-like structure demonstrating:
   - Parent/child relationships (using nested Expanders or custom control)
   - Multiple levels of nesting (at least 3 levels)
   - Expand/collapse at each level
   
2. WHEN tree nodes are rendered THEN each node SHALL have AutomationId with path-like structure (e.g., `Node_1`, `Node_1_2`, `Node_1_2_1`)

### REQ-008: Make All Existing Pages Accessible

**User Story:** As a test writer, I want all existing sample pages accessible via tabs, so that I can test all control types.

#### Acceptance Criteria

1. WHEN the app launches THEN ALL existing pages SHALL be accessible:
   - MainPage content → Basics tab
   - ContainerDemoView → Containers tab
   - ValidationPage → Forms tab
   - UserFormPage → Forms tab (sub-tab or scroll section)
   - DataGridPage → Lists tab
   - AdvancedPage → Gestures tab
   - NavigationDemoPage → Navigation tab
   - MediaGalleryPage → Media tab
   - DashboardPage → Overview/Dashboard tab

### REQ-009: Consistent AutomationId Patterns

**User Story:** As a test writer, I want consistent AutomationId naming across all tabs, so that I can write predictable locators.

#### Acceptance Criteria

1. WHEN any control is added THEN it SHALL follow AutomationId patterns:
   - Tabs: `{TabName}Tab` (e.g., `BasicsTab`, `ListsTab`, `ToolkitTab`)
   - Content areas: `{TabName}Content` (e.g., `BasicsContent`)
   - List items: `{ListName}_{Index}` (e.g., `TaskList_0`, `TreeNode_1_2`)
   - Buttons: `{Action}Button` (e.g., `SaveButton`, `DeleteButton`)
   - Entries: `{Field}Entry` (e.g., `NameEntry`, `EmailEntry`)

### REQ-010: Add Popup/Dialog Demo

**User Story:** As a test writer, I want popup/dialog demos, so that I can test modal and non-modal overlay patterns.

#### Acceptance Criteria

1. WHEN viewing the Toolkit tab THEN there SHALL be controls to trigger:
   - CommunityToolkit Popup (modal)
   - Snackbar/Toast notifications
   - ActionSheet demonstration
   
2. WHEN a popup is shown THEN it SHALL have AutomationId for the popup container and its contents

---

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: Each tab's content should be in a separate ContentView or Page
- **Modular Design**: New control demos should be in dedicated view files
- **Shared ViewModels**: Use shared ViewModels where appropriate for data binding demos
- **Clear Interfaces**: All controls must implement proper AutomationId for testability

### Performance
- TabView SHALL lazy-load content to minimize startup time
- Large lists SHALL use virtualization (CollectionView, not manual StackLayout)
- Tab switching SHALL complete within 200ms

### Testability
- Every interactive control SHALL have an AutomationId
- AutomationIds SHALL be unique within their scope
- Container controls SHALL properly scope their children for Brinell container testing

### Maintainability
- New demo pages SHALL follow existing patterns for consistency
- XAML files SHALL be under 500 lines; split into multiple views if larger
- ViewModels SHALL use CommunityToolkit.Mvvm for consistency

---

## Scope

### In Scope
- Replace MainPage navigation with CommunityToolkit TabView
- Create new content views for missing control demos
- Wire up all existing pages to be accessible via tabs
- Add ListView, TableView, TreeView-like, Expander, nested TabView demos
- Add popup/snackbar demos
- Ensure all controls have AutomationIds

### Out of Scope
- Android/iOS platform support (Windows-only for now)
- Advanced theming beyond current styles
- Data persistence (all data is in-memory)
- Unit tests for the sample app itself (that's what Brinell.Maui.UITests is for)
- Complex navigation (Shell, NavigationPage) - we're focusing on TabView

---

## Dependencies

- CommunityToolkit.Maui (already referenced)
- Existing sample app pages and ViewModels
- Brinell.Maui.CommunityToolkit.Controls.TabViewControl for testing
