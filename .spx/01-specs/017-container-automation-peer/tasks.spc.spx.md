# Tasks Document: Container and Navigation AutomationPeer

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Each task includes File path, Purpose, _Leverage, _Requirements, and _Prompt fields
- _Prompt provides AI guidance for implementing the task

---

## Phase 1: AutomationContainer Control

### [x] 1. Create AutomationContainer control (cross-platform)

- **File**: `samples/Brinell.Samples.Maui.App/Controls/AutomationContainer.cs`
- **Purpose**: Create a ContentView-based container that can expose AutomationId on Windows
- **Description**: Simple cross-platform control that handlers will customize per-platform

_Leverage: `Microsoft.Maui.Controls.ContentView`_

_Requirements: R1, R3_

_Prompt: Role: MAUI control developer | Task: Create a simple AutomationContainer class that extends ContentView, add XML documentation explaining its purpose for UI test automation, place in Controls folder | Restrictions: Do not add platform-specific code here, keep it simple, no custom properties needed | Success: Class compiles, can be used in XAML with AutomationId property, behaves identically to ContentView visually_

---

### [x] 2. Create AutomationContentPanel for Windows

- **File**: `samples/Brinell.Samples.Maui.App/Platforms/Windows/Controls/AutomationContentPanel.cs`
- **Purpose**: WinUI ContentPanel that overrides OnCreateAutomationPeer to expose AutomationId
- **Description**: The native panel that provides the AutomationPeer for container discovery

_Leverage: `Microsoft.Maui.Platform.ContentPanel`, `Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer`_

_Requirements: R1_

_Prompt: Role: WinUI/Windows developer | Task: Create AutomationContentPanel class extending ContentPanel, override OnCreateAutomationPeer() to return AutomationContainerPeer instance | Restrictions: Must be in Platforms/Windows folder, use proper WinUI namespaces, ensure panel inherits all ContentPanel layout behavior | Success: Panel compiles, overrides OnCreateAutomationPeer, can be instantiated by handler_

---

### [x] 3. Create AutomationContainerPeer for Windows

- **File**: `samples/Brinell.Samples.Maui.App/Platforms/Windows/Controls/AutomationContainerPeer.cs`
- **Purpose**: AutomationPeer that exposes the container to UI Automation with proper control type
- **Description**: Inherits from FrameworkElementAutomationPeer, returns Group control type

_Leverage: `Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer`_

_Requirements: R1_

_Prompt: Role: Windows Accessibility developer | Task: Create AutomationContainerPeer extending FrameworkElementAutomationPeer, override GetClassNameCore() to return "AutomationContainer", override GetAutomationControlTypeCore() to return AutomationControlType.Group | Restrictions: Do not override GetAutomationIdCore - inherited behavior handles this, use WinUI namespaces not UWP | Success: Peer compiles, returns correct class name and control type, inherits AutomationId exposure from base class_

---

### [x] 4. Create AutomationContainerHandler for Windows

- **File**: `samples/Brinell.Samples.Maui.App/Platforms/Windows/Controls/AutomationContainerHandler.cs`
- **Purpose**: MAUI handler that uses AutomationContentPanel as the platform view
- **Description**: Connects the MAUI AutomationContainer to the WinUI AutomationContentPanel

_Leverage: `Microsoft.Maui.Handlers.ContentViewHandler`_

_Requirements: R1_

_Prompt: Role: MAUI handler developer | Task: Create AutomationContainerHandler extending ContentViewHandler, override CreatePlatformView() to return new AutomationContentPanel() instead of default ContentPanel | Restrictions: Must extend ContentViewHandler not create from scratch, only override CreatePlatformView, keep all other behavior from base | Success: Handler compiles, creates AutomationContentPanel, all ContentView functionality preserved_

---

### [x] 5. Register AutomationContainer handler in MauiProgram.cs

- **File**: `samples/Brinell.Samples.Maui.App/MauiProgram.cs`
- **Purpose**: Wire up the custom handler so AutomationContainer uses AutomationContainerHandler on Windows
- **Description**: Add ConfigureMauiHandlers with conditional compilation for Windows

_Leverage: Existing `MauiProgram.cs`, `ConfigureMauiHandlers` API_

_Requirements: R1, R4_

_Prompt: Role: MAUI app developer | Task: Modify MauiProgram.cs to call ConfigureMauiHandlers, add handler mapping for AutomationContainer to AutomationContainerHandler wrapped in #if WINDOWS conditional compilation | Restrictions: Do not break existing builder chain, maintain UseMauiCommunityToolkit call, use proper handler registration syntax | Success: App compiles, AutomationContainer uses custom handler on Windows, other platforms use default ContentView handler_

---

## Phase 2: TabbedPage Handler Mapper

### [x] 6. Create TabbedPageAutomationMapper for Windows

- **File**: `samples/Brinell.Samples.Maui.App/Platforms/Windows/Handlers/TabbedPageAutomationMapper.cs`
- **Purpose**: Handler mapper that sets AutomationId on NavigationViewItem tab elements
- **Description**: Uses AppendToMapping to intercept TabbedPage and map child page AutomationIds to tabs

_Leverage: `Microsoft.Maui.Handlers.TabbedPageHandler`, `Microsoft.UI.Xaml.Automation.AutomationProperties`_

_Requirements: R6, R7_

_Prompt: Role: MAUI handler customization developer | Task: Create static TabbedPageAutomationMapper class with Configure() method that calls TabbedPageHandler.Mapper.AppendToMapping, implement mapper that iterates TabbedPage children and sets AutomationProperties.AutomationId on corresponding NavigationViewItem elements | Restrictions: Use AppendToMapping not ModifyMapping, handle null checks gracefully, do not throw if structure is unexpected, log warnings for debugging | Success: Configure() can be called from MauiProgram, mapper sets AutomationId on tab elements when TabbedPage has children with AutomationId_

---

### [x] 7. Register TabbedPage mapper in MauiProgram.cs

- **File**: `samples/Brinell.Samples.Maui.App/MauiProgram.cs`
- **Purpose**: Call TabbedPageAutomationMapper.Configure() during app startup
- **Description**: Ensure mapper is registered before any TabbedPage is created

_Leverage: Task 5 changes to `MauiProgram.cs`_

_Requirements: R6, R7_

_Prompt: Role: MAUI app developer | Task: Add call to TabbedPageAutomationMapper.Configure() in MauiProgram.cs, place inside #if WINDOWS block, call before builder.Build() | Restrictions: Must be called early in startup, do not call multiple times, maintain existing code structure | Success: App compiles, TabbedPage mapper is active on Windows, no runtime errors_

---

## Phase 3: Sample App Updates

### [x] 8. Update MainPage.xaml to use AutomationContainer (updated ContainerDemoView.xaml)

- **File**: `samples/Brinell.Samples.Maui.App/MainPage.xaml`
- **Purpose**: Replace Grid/Border containers with AutomationContainer for container scoping tests
- **Description**: Update the container scoping test section to use discoverable containers

_Leverage: Existing `MainPage.xaml` structure_

_Requirements: R1, R2, R3_

_Prompt: Role: MAUI XAML developer | Task: In MainPage.xaml, locate container scoping section and replace Grid or Border containers with AutomationContainer controls, ensure each container has unique AutomationId, maintain child control structure | Restrictions: Keep existing child controls unchanged, only replace container elements, preserve visual layout | Success: XAML compiles, containers are now AutomationContainer with AutomationId, visual appearance unchanged_

---

### [x] 9. Create TabbedPage demo page

- **File**: `samples/Brinell.Samples.Maui.App/Pages/TabbedPageDemoPage.xaml` and `.xaml.cs`
- **Purpose**: Demonstrate TabbedPage with testable tabs using AutomationId
- **Description**: Create a TabbedPage with 3+ child pages, each with AutomationId

_Leverage: MAUI TabbedPage documentation_

_Requirements: R6_

_Prompt: Role: MAUI page developer | Task: Create TabbedPageDemoPage as a TabbedPage with 3 child ContentPages (Tab1, Tab2, Tab3), set AutomationId on each child page (e.g., "Tab1Page", "Tab2Page"), add simple content to each tab for verification | Restrictions: Keep content simple (Label with tab name), ensure AutomationId is on ContentPage not just content | Success: TabbedPage displays 3 tabs, each child has AutomationId property set, navigation between tabs works_

---

### [x] 10. Add navigation to TabbedPage demo

- **File**: `samples/Brinell.Samples.Maui.App/MainPage.xaml` (or appropriate navigation location)
- **Purpose**: Allow navigating to the TabbedPage demo from main app
- **Description**: Add button or menu item to navigate to TabbedPageDemoPage

_Leverage: Existing navigation patterns in sample app_

_Requirements: R6_

_Prompt: Role: MAUI app developer | Task: Add navigation method to access TabbedPageDemoPage, either as button on main page or menu item, use Shell navigation or direct page push as appropriate for app structure | Restrictions: Use existing navigation patterns, do not restructure app navigation, keep it simple | Success: User can navigate to TabbedPage demo, demo displays correctly, can navigate back_

---

## Phase 4: Test Validation

### [ ] 11. Update ContainerScopingTests

- **File**: `samples/Brinell.Samples.Maui.UITests.ControlObject6/Tests/ContainerScopingTests.cs`
- **Purpose**: Verify container scoping now works with AutomationContainer
- **Description**: Update tests if needed to work with new container, run all 9 tests

_Leverage: Existing `ContainerScopingTests.cs`_

_Requirements: R1, R2, R5_

_Prompt: Role: UI Test developer | Task: Review ContainerScopingTests, update container AutomationIds if they changed in MainPage.xaml, run all 9 container tests, verify scoped searching works correctly | Restrictions: Do not remove tests, maintain test intent, update only locators if container IDs changed | Success: All 9 ContainerScopingTests pass, no fallback to global search occurs, scoped elements found correctly_

---

### [ ] 12. Create TabbedPageTests

- **File**: `samples/Brinell.Samples.Maui.UITests.ControlObject6/Tests/TabbedPageTests.cs`
- **Purpose**: Verify tab navigation works using AutomationId
- **Description**: Create tests for clicking tabs by AutomationId, verifying correct content displayed

_Leverage: Existing test patterns in sample UI tests, MauiTestBase6_

_Requirements: R6, R7_

_Prompt: Role: UI Test developer | Task: Create TabbedPageTests class extending MauiTestBase6, add tests: NavigateToTabByAutomationId, VerifyTabContentAfterClick, AllTabsDiscoverable, use Appium to find tabs by AccessibilityId and click | Restrictions: Use existing test base class, follow existing test patterns, add navigation to TabbedPageDemoPage first | Success: Tests pass, tabs clickable by AutomationId, correct content verified after tab switch_

---

### [ ] 13. Run full test suite and verify

- **Purpose**: Validate all tests pass including new container and tab tests
- **Description**: Execute complete UI test suite, document results

_Leverage: Existing test runner configuration_

_Requirements: All_

_Prompt: Role: QA Engineer | Task: Run full MAUI UI test suite, verify ContainerScopingTests (target 9/9), TabbedPageTests (all pass), document any failures with details | Restrictions: Do not skip failing tests, investigate and fix issues, ensure stable test execution | Success: ContainerScopingTests 9/9 passing, TabbedPageTests all passing, no regressions in other tests_

---

## Phase 5: Documentation

### [ ] 14. Add usage documentation

- **File**: `docs/platform-guides/maui-automation-containers.md` (or appropriate location)
- **Purpose**: Document how to use AutomationContainer and TabbedPage automation
- **Description**: Explain the limitation, solution, and usage examples

_Leverage: Existing documentation patterns in docs/ folder_

_Requirements: R4, R5_

_Prompt: Role: Technical writer | Task: Create documentation explaining Windows container and TabbedPage automation limitations, how AutomationContainer solves container scoping, how to set up TabbedPage automation, include XAML examples | Restrictions: Keep documentation concise, focus on usage not implementation, include code examples | Success: Documentation clearly explains the problem and solution, examples are copy-paste ready, covers both containers and tabs_

---

## Summary

| Phase | Tasks | Requirements Covered |
|-------|-------|---------------------|
| Phase 1: AutomationContainer | 1-5 | R1, R3, R4 |
| Phase 2: TabbedPage Mapper | 6-7 | R6, R7 |
| Phase 3: Sample App Updates | 8-10 | R1, R2, R6 |
| Phase 4: Test Validation | 11-13 | R1, R2, R5, R6, R7 |
| Phase 5: Documentation | 14 | R4, R5 |

**Estimated Total Time:** 4-6 hours

---

**Document Version:** 1.0  
**Created:** January 19, 2026  
**Workflow:** spec_workflow/tasks  
**Spec ID:** 017-container-automation-peer
