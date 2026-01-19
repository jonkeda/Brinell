# Tasks Document

## Task Format

Each task should follow this structure:
- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields
- _Prompt provides AI guidance for implementing the task

---

## Phase 1: Update Page Objects

### [x] 1. Update AppShellPage for new 8-tab structure
- **File:** `testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs`
- **Purpose:** Align AppShellPage with new TabbedPage navigation structure
- **_Leverage:** `samples/Brinell.Samples.Maui.App/MainPage.xaml` (source of AutomationIds)
- **_Requirements:** Requirement 1 (AppShellPage for TabbedPage Navigation)
- **_Prompt:** Role: .NET MAUI Test Developer | Task: Update AppShellPage to match 8 tabs from MainPage.xaml: BasicsTab, ContainersTab, FormsTab, ListsTab, GesturesTab, NavigationTab, ToolkitTab, MediaTab. Remove obsolete tabs (MainTab, DashboardTab, DataTab, ValidationTab, AdvancedTab). Update IsLoaded() to check BasicsTab. | Restrictions: Do not change base class or TabViewControl usage pattern. Keep using ITabControlObject interface. | Success: AppShellPage has exactly 8 tab properties matching MainPage.xaml AutomationIds. IsLoaded() returns true when BasicsTab exists.

### [x] 2. Update MainPage page object for BasicsView controls
- **File:** `testsnew/Brinell.Maui.UITests/Pages/MainPage.cs`
- **Purpose:** Expose all testable controls from BasicsView.xaml
- **_Leverage:** `samples/Brinell.Samples.Maui.App/Views/BasicsView.xaml` (source of AutomationIds)
- **_Requirements:** Requirement 2 (MainPage for BasicsView Controls)
- **_Prompt:** Role: .NET MAUI Test Developer | Task: Update MainPage to expose all controls from BasicsView.xaml with correct AutomationIds. Add new properties: VolumeLabel, NotificationLabel, SelectedColorLabel, MessageEditor, NotificationSwitch, AgreeCheckBox, VolumeSlider, VolumeProgress, ColorPicker, BirthDatePicker, ReminderTimePicker, LoadingIndicator. Keep existing controls that match. | Restrictions: Use MauiControlBase<MainPage> for non-specialized controls. Use factory methods (Control, Button, Entry) where appropriate. | Success: All BasicsView controls are accessible via MainPage properties. AutomationIds match exactly.

---

## Phase 2: Update Fixture and Navigation

### [x] 3. Update AppiumFixture navigation methods
- **File:** `testsnew/Brinell.Maui.UITests/AppiumFixture.cs`
- **Purpose:** Update navigation to use new tab names
- **_Leverage:** Updated `AppShellPage.cs` from Task 1
- **_Requirements:** Requirement 4 (AppiumFixture Navigation)
- **_Prompt:** Role: .NET MAUI Test Developer | Task: Update NavigateToMain() to click BasicsTab instead of MainTab. Keep NavigateToContainerDemo() using ContainersTab (unchanged). Verify page objects are created correctly in constructor. | Restrictions: Do not change fixture base class or driver configuration. Keep existing timeout values. | Success: NavigateToMain() clicks BasicsTab and waits for MainPage. NavigateToContainerDemo() works unchanged.

---

## Phase 3: Update Tests

### [x] 4. Update MainPageTests for new structure
- **File:** `testsnew/Brinell.Maui.UITests/Tests/MainPageTests.cs`
- **Purpose:** Update tests to work with new tab navigation and controls
- **_Leverage:** Updated `AppShellPage.cs`, `MainPage.cs` from Tasks 1-2
- **_Requirements:** Requirement 3 (MainPageTests for New Structure)
- **_Prompt:** Role: .NET MAUI Test Developer | Task: Update tab navigation to use BasicsTab instead of MainTab. Update MainPage_NavigateToMainTab_ShowsControls test to navigate to BasicsTab. Verify all existing tests still reference valid controls. | Restrictions: Do not add new tests in this spec. Only update existing tests to work with new structure. Keep test patterns unchanged. | Success: All MainPageTests compile. Tab navigation uses BasicsTab. Control references match BasicsView.

---

## Phase 4: Verification

### [x] 5. Build and verify tests compile
- **File:** `testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj`
- **Purpose:** Ensure all changes compile without errors
- **_Leverage:** N/A
- **_Requirements:** All requirements
- **_Prompt:** Role: .NET Developer | Task: Run dotnet build on Brinell.Maui.UITests project. Fix any compilation errors. Ensure no missing references or type mismatches. | Restrictions: Do not change project references or NuGet packages. | Success: Project builds with 0 errors. All tests are discoverable.

### [x] 6. Verify AutomationId alignment
- **File:** N/A (verification task)
- **Purpose:** Ensure page objects match XAML AutomationIds exactly
- **_Leverage:** `MainPage.xaml`, `BasicsView.xaml`
- **_Requirements:** All requirements
- **_Prompt:** Role: QA Engineer | Task: Cross-reference all AutomationIds in page objects against XAML files. Document any mismatches. | Restrictions: Do not modify XAML files. Only verify alignment. | Success: All AutomationIds in page objects match corresponding XAML files exactly.

---

## Summary

| Task | File | Type | Requirement |
|------|------|------|-------------|
| 1 | AppShellPage.cs | Modify | REQ-1 |
| 2 | MainPage.cs | Modify | REQ-2 |
| 3 | AppiumFixture.cs | Modify | REQ-4 |
| 4 | MainPageTests.cs | Modify | REQ-3 |
| 5 | Build verification | Verify | All |
| 6 | AutomationId check | Verify | All |
