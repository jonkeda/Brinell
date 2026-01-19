# Tasks Document: MAUI Sample App Tab Navigation

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Each task includes File path, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Core Structure

### [x] 1. Create BasicsViewModel

- **File:** `samples/Brinell.Samples.Maui.App/ViewModels/BasicsViewModel.cs`
- **Purpose:** Extract counter, greet, volume, and toggle logic from MainPage.xaml.cs into a proper ViewModel
- _Leverage: ViewModels/ContainerDemoViewModel.cs, CommunityToolkit.Mvvm patterns_
- _Requirements: REQ-001, REQ-008_
- _Prompt: Role: MAUI MVVM developer | Task: Create BasicsViewModel with ObservableObject, Counter property, IncrementCommand, DecrementCommand, ResetCommand, GreetCommand, Volume property with binding | Restrictions: Use CommunityToolkit.Mvvm, no code-behind logic, follow existing ViewModel patterns | Success: All commands work, properties notify changes, no direct UI references_

### [x] 2. Create BasicsView

- **File:** `samples/Brinell.Samples.Maui.App/Views/BasicsView.xaml` and `.xaml.cs`
- **Purpose:** Extract counter, entry, toggle, slider, picker sections from MainPage.xaml into a reusable ContentView
- _Leverage: MainPage.xaml (current content), ViewModels/BasicsViewModel.cs_
- _Requirements: REQ-001, REQ-008, REQ-009_
- _Prompt: Role: MAUI XAML developer | Task: Create BasicsView ContentView extracting all content from MainPage ScrollView, bind to BasicsViewModel, ensure all AutomationIds follow {Type}{Purpose} pattern | Restrictions: Keep all existing AutomationIds, use data binding not code-behind, ContentView not ContentPage | Success: All controls render correctly, bindings work, AutomationIds preserved_

### [x] 3. Redesign MainPage with TabView

- **File:** `samples/Brinell.Samples.Maui.App/MainPage.xaml` and `.xaml.cs`
- **Purpose:** Replace button-based navigation with CommunityToolkit.Maui TabView as the main navigation container
- _Leverage: CommunityToolkit.Maui TabView, Views/BasicsView.xaml, Views/ContainerDemoView.xaml_
- _Requirements: REQ-001, REQ-002, REQ-009_
- _Prompt: Role: MAUI navigation developer | Task: Replace MainPage content with toolkit:TabView, add 8 TabViewItems (Basics, Containers, Forms, Lists, Gestures, Navigation, Toolkit, Media), embed BasicsView and ContainerDemoView, use placeholder Labels for others | Restrictions: TabStripPlacement="Top", each TabViewItem needs AutomationId like "BasicsTab", remove old button-based navigation code | Success: TabView renders, tabs are clickable, Basics and Containers tabs show content_

---

## Phase 2: Extract Existing Pages

### [x] 4. Create GesturesView from AdvancedPage

- **File:** `samples/Brinell.Samples.Maui.App/Views/GesturesView.xaml` and `.xaml.cs`
- **Purpose:** Convert AdvancedPage content to a ContentView for embedding in Gestures tab
- _Leverage: Pages/AdvancedPage.xaml, ViewModels/AdvancedViewModel.cs_
- _Requirements: REQ-008_
- _Prompt: Role: MAUI refactoring developer | Task: Copy AdvancedPage content (TapGesture, PanGesture, PinchGesture, SwipeView sections) into GesturesView ContentView, keep AdvancedViewModel binding, convert gesture event handlers to commands where possible | Restrictions: Keep all AutomationIds, ContentView not ContentPage, reuse existing AdvancedViewModel | Success: All gesture demos work, AutomationIds preserved, embedded in Gestures tab_

### [x] 5. Wire GesturesView to Gestures Tab

- **File:** `samples/Brinell.Samples.Maui.App/MainPage.xaml`
- **Purpose:** Replace Gestures tab placeholder with GesturesView
- _Leverage: Views/GesturesView.xaml_
- _Requirements: REQ-008_
- _Prompt: Role: MAUI developer | Task: Replace Gestures tab placeholder Label with views:GesturesView | Restrictions: Keep AutomationId="GesturesTab" on TabViewItem | Success: Gestures tab shows gesture demos_

### [x] 6. Create NavigationDemoView

- **File:** `samples/Brinell.Samples.Maui.App/Views/NavigationDemoView.xaml` and `.xaml.cs`
- **Purpose:** Create ContentView for navigation patterns demo or extract from NavigationDemoPage
- _Leverage: Pages/NavigationDemoPage.xaml (if exists)_
- _Requirements: REQ-008_
- _Prompt: Role: MAUI developer | Task: Create NavigationDemoView showing navigation patterns (buttons that would navigate, breadcrumb-like display), if NavigationDemoPage exists extract content, otherwise create simple navigation demo | Restrictions: ContentView not ContentPage, add appropriate AutomationIds | Success: Navigation tab has demo content_

### [x] 7. Create MediaGalleryView

- **File:** `samples/Brinell.Samples.Maui.App/Views/MediaGalleryView.xaml` and `.xaml.cs`
- **Purpose:** Create ContentView for media/image display demo or extract from MediaGalleryPage
- _Leverage: Pages/MediaGalleryPage.xaml (if exists)_
- _Requirements: REQ-008_
- _Prompt: Role: MAUI developer | Task: Create MediaGalleryView with Image controls, CarouselView of images, or extract from MediaGalleryPage if exists | Restrictions: ContentView, use placeholder images from Resources, add AutomationIds | Success: Media tab shows image gallery demo_

### [x] 8. Wire Navigation and Media Views to Tabs

- **File:** `samples/Brinell.Samples.Maui.App/MainPage.xaml`
- **Purpose:** Replace Navigation and Media tab placeholders with actual views
- _Leverage: Views/NavigationDemoView.xaml, Views/MediaGalleryView.xaml_
- _Requirements: REQ-008_
- _Prompt: Role: MAUI developer | Task: Replace placeholder Labels with views:NavigationDemoView and views:MediaGalleryView | Restrictions: Keep TabViewItem AutomationIds | Success: All 8 tabs have real content_

---

## Phase 3: New Control Demos - Forms

### [x] 9. Create FormsViewModel

- **File:** `samples/Brinell.Samples.Maui.App/ViewModels/FormsViewModel.cs`
- **Purpose:** ViewModel for forms tab with validation state, TableView settings data
- _Leverage: CommunityToolkit.Mvvm, existing ViewModel patterns_
- _Requirements: REQ-004_
- _Prompt: Role: MAUI MVVM developer | Task: Create FormsViewModel with Username/Email/Phone properties, ValidationErrors dictionary, IsDarkMode/NotificationsEnabled toggle properties for TableView demo, SaveCommand with validation | Restrictions: Use ObservableObject, RelayCommand, data validation in setters | Success: Properties notify, validation works, commands execute_

### [x] 10. Create FormsView with TableView

- **File:** `samples/Brinell.Samples.Maui.App/Views/FormsView.xaml` and `.xaml.cs`
- **Purpose:** Forms tab content with user form, validation demo, and TableView settings demo
- _Leverage: ViewModels/FormsViewModel.cs, Pages/ValidationPage.xaml (patterns)_
- _Requirements: REQ-004, REQ-008, REQ-009_
- _Prompt: Role: MAUI XAML developer | Task: Create FormsView with 3 sections: 1) User Form (Name/Email/Phone Entries with validation labels), 2) TableView with Account section (EntryCell, SwitchCell, TextCell) and Preferences section (SwitchCell, ViewCell), 3) Save button | Restrictions: All cells need AutomationId like "UsernameCell", "NotificationsSwitchCell", use SettingsTableView as root AutomationId | Success: Form validates input, TableView shows settings-style UI, all controls have AutomationIds_

### [x] 11. Wire FormsView to Forms Tab

- **File:** `samples/Brinell.Samples.Maui.App/MainPage.xaml`
- **Purpose:** Replace Forms tab placeholder with FormsView
- _Leverage: Views/FormsView.xaml_
- _Requirements: REQ-004, REQ-008_
- _Prompt: Role: MAUI developer | Task: Replace Forms tab placeholder with views:FormsView | Restrictions: Keep AutomationId="FormsTab" | Success: Forms tab shows form and TableView demos_

---

## Phase 4: New Control Demos - Lists

### [x] 12. Create ListsViewModel

- **File:** `samples/Brinell.Samples.Maui.App/ViewModels/ListsViewModel.cs`
- **Purpose:** ViewModel for lists tab with ListView items, TreeView nodes
- _Leverage: CommunityToolkit.Mvvm, ViewModels/DataGridViewModel.cs patterns_
- _Requirements: REQ-003, REQ-007_
- _Prompt: Role: MAUI MVVM developer | Task: Create ListsViewModel with ObservableCollection<ListItem> for ListView (5-10 items with Id, Name, Description), ObservableCollection<TreeNode> for hierarchy (3 parent nodes, each with 2-3 children, some with grandchildren), RefreshCommand, SelectedItem property | Restrictions: Use record or class for ListItem/TreeNode, TreeNode needs Id like "1", "1_1", "1_1_2" | Success: Collections populated, refresh works, selection bindable_

### [x] 13. Create ListsView with ListView and TreeView-like

- **File:** `samples/Brinell.Samples.Maui.App/Views/ListsView.xaml` and `.xaml.cs`
- **Purpose:** Lists tab with classic ListView demo and TreeView-like hierarchy using nested Expanders
- _Leverage: ViewModels/ListsViewModel.cs, CommunityToolkit.Maui Expander_
- _Requirements: REQ-003, REQ-007, REQ-009_
- _Prompt: Role: MAUI XAML developer | Task: Create ListsView with 2 sections: 1) ListView with IsPullToRefreshEnabled, TextCell template with AutomationId="ListItem_{Binding Id}", 2) TreeView-like section using nested toolkit:Expander controls for 3-level hierarchy, node AutomationIds like "Node_1", "Node_1_1" | Restrictions: ListView not CollectionView for classic demo, Expanders for tree not custom control, proper AutomationIds | Success: ListView shows items with pull-to-refresh, tree nodes expand/collapse with proper AutomationIds_

### [x] 14. Wire ListsView to Lists Tab

- **File:** `samples/Brinell.Samples.Maui.App/MainPage.xaml`
- **Purpose:** Replace Lists tab placeholder with ListsView
- _Leverage: Views/ListsView.xaml_
- _Requirements: REQ-003, REQ-007_
- _Prompt: Role: MAUI developer | Task: Replace Lists tab placeholder with views:ListsView | Restrictions: Keep AutomationId="ListsTab" | Success: Lists tab shows ListView and TreeView demos_

---

## Phase 5: New Control Demos - Toolkit

### [x] 15. Create ToolkitViewModel

- **File:** `samples/Brinell.Samples.Maui.App/ViewModels/ToolkitViewModel.cs`
- **Purpose:** ViewModel for toolkit tab with popup commands, expander state
- _Leverage: CommunityToolkit.Mvvm, CommunityToolkit.Maui.Views.Popup_
- _Requirements: REQ-005, REQ-006, REQ-010_
- _Prompt: Role: MAUI MVVM developer | Task: Create ToolkitViewModel with ShowPopupCommand, ShowSnackbarCommand, PopupMessage property, Expander IsExpanded properties for accordion demo | Restrictions: Use WeakReferenceMessenger or Page reference for popup display, async commands | Success: Commands trigger popup/snackbar display_

### [x] 16. Create ToolkitView with Expander and Nested TabView

- **File:** `samples/Brinell.Samples.Maui.App/Views/ToolkitView.xaml` and `.xaml.cs`
- **Purpose:** Toolkit tab with CommunityToolkit demos: Expander, nested TabView, Popup triggers
- _Leverage: ViewModels/ToolkitViewModel.cs, CommunityToolkit.Maui_
- _Requirements: REQ-005, REQ-006, REQ-010, REQ-009_
- _Prompt: Role: MAUI XAML developer | Task: Create ToolkitView with 3 sections: 1) Expander demos (SimpleExpander with AutomationId, nested Expanders for accordion with 3 items), 2) Nested TabView (NestedTabView AutomationId, 3 inner tabs with InnerTab1/2/3 AutomationIds), 3) Popup buttons (ShowPopupButton, ShowSnackbarButton) | Restrictions: All controls need unique AutomationIds, Expanders need both Header and Content identifiable | Success: Expanders expand/collapse, nested tabs work independently, popup buttons are clickable_

### [x] 17. Create SamplePopup

- **File:** `samples/Brinell.Samples.Maui.App/Views/SamplePopup.xaml` and `.xaml.cs`
- **Purpose:** CommunityToolkit Popup for testing popup automation
- _Leverage: CommunityToolkit.Maui.Views.Popup_
- _Requirements: REQ-010_
- _Prompt: Role: MAUI developer | Task: Create SamplePopup inheriting from Popup, with title, message label, OK/Cancel buttons, all with AutomationIds (PopupTitle, PopupMessage, PopupOkButton, PopupCancelButton) | Restrictions: Must be Popup not ContentPage, return result on button click | Success: Popup displays when triggered, buttons dismiss popup_

### [x] 18. Wire ToolkitView to Toolkit Tab

- **File:** `samples/Brinell.Samples.Maui.App/MainPage.xaml`
- **Purpose:** Replace Toolkit tab placeholder with ToolkitView
- _Leverage: Views/ToolkitView.xaml_
- _Requirements: REQ-005, REQ-006, REQ-010_
- _Prompt: Role: MAUI developer | Task: Replace Toolkit tab placeholder with views:ToolkitView | Restrictions: Keep AutomationId="ToolkitTab" | Success: Toolkit tab shows Expander, nested TabView, popup demos_

---

## Phase 6: Build and Verify

### [x] 19. Build Solution and Fix Errors

- **Command:** `dotnet build samples/Brinell.Samples.Maui.App/Brinell.Samples.Maui.App.csproj`
- **Purpose:** Verify all new views, ViewModels, and MainPage changes compile
- _Leverage: N/A_
- _Requirements: All_
- _Prompt: Role: Build engineer | Task: Build sample app, fix any compilation errors in new files | Restrictions: Do not change AutomationIds to fix errors, fix actual code issues | Success: Build succeeds with no errors_

### [ ] 20. Run Sample App and Manual Test

- **Command:** Run app, click through all 8 tabs
- **Purpose:** Verify app launches, all tabs render, basic interactions work
- _Leverage: N/A_
- _Requirements: All_
- _Prompt: Role: QA tester | Task: Launch app, verify each tab displays content, test key interactions (counter, form validation, expanders, popups) | Restrictions: Document any issues found | Success: All tabs accessible, major features work_

### [ ] 21. Update UI Tests for New Structure

- **File:** `testsnew/Brinell.Maui.UITests/Pages/MainPage.cs`
- **Purpose:** Update test page objects to use new TabView navigation
- _Leverage: Existing MainPage.cs, TabViewControl from Brinell.Maui.CommunityToolkit_
- _Requirements: REQ-001, REQ-009_
- _Prompt: Role: Test automation developer | Task: Update MainPage page object to use TabViewControl for navigation, add properties for each tab (BasicsTab, ContainersTab, etc.), update any tests that use old button navigation | Restrictions: Use Brinell framework patterns, TabViewControl for tab automation | Success: Tests can navigate between tabs using new TabView structure_

---

## Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| 1 | 1-3 | Core structure: ViewModel, BasicsView, MainPage TabView |
| 2 | 4-8 | Extract existing pages to ContentViews |
| 3 | 9-11 | Forms tab with TableView |
| 4 | 12-14 | Lists tab with ListView and TreeView-like |
| 5 | 15-18 | Toolkit tab with Expander, nested TabView, Popup |
| 6 | 19-21 | Build, verify, update tests |

**Total: 21 tasks**
