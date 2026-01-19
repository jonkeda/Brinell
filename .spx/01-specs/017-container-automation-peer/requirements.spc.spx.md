# Requirements Document: Container and Navigation AutomationPeer for UI Test Automation

## Introduction

This specification addresses fundamental platform limitations discovered during MAUI UI test development:

1. **Layout containers** (Grid, StackLayout, Frame, Border, ContentView) do not expose AutomationId to UI Automation on Windows because they lack AutomationPeers.

2. **TabbedPage/Shell tabs** do not pass AutomationId to the underlying WinUI NavigationViewItem elements, making tab navigation untestable via automation.

Microsoft documentation explicitly states:
> "Examples of classes that do not implement automation peers are Border and classes based on Panel, such as Grid and Canvas. A Panel has no peer because it is providing a layout behavior that is visual only."

These limitations break the Brinell framework's container scoping pattern and tab navigation testing capabilities.

### Problem Statement

1. **Container Scoping Fails**: The `IContainerControl` pattern relies on finding a container root element, then scoping child searches to that container. When container elements don't appear in the automation tree, scoped searching fails.

2. **Frame is Obsolete**: `Frame` was the traditional MAUI container with styling. It's obsolete in .NET 9+ and replaced by `Border`, which also lacks an AutomationPeer.

3. **No Native Solution**: There's no built-in way to make Grid, Border, or StackLayout expose AutomationId to UI Automation.

4. **Workarounds Break Isolation**: Current workarounds (Label markers + fallback to parent scope) partially work but break true scoped isolation—identical AutomationIds in sibling containers find the first global match instead of the scoped match.

5. **TabbedPage/Shell Tab AutomationId Not Exposed**: GitHub issue [dotnet/maui#3996](https://github.com/dotnet/maui/issues/3996) (open since January 2022) documents that TabbedPage and Shell tabs don't expose AutomationId:
   - **iOS**: Works correctly (AutomationId passed to tab elements)
   - **Android**: Never implemented (AutomationId not mapped to ContentDescription)
   - **Windows**: MAUI handler doesn't map AutomationId to WinUI NavigationViewItem's AutomationProperties

6. **No Tab Navigation Testing**: Without AutomationId on tabs, UI tests cannot reliably select tabs. The MAUI team's workaround is to use tab Title text, which is fragile and locale-dependent.

### Value Proposition

Implementing a proper solution will:
- Enable true container-scoped element searching in MAUI UI tests
- Allow test writers to use the same container patterns as WPF/WinForms (which work correctly)
- Ensure tests can verify that elements belong to specific containers
- Support scenarios like list items, form groups, and card-based layouts where multiple containers have identical control structures
- Enable reliable tab navigation in UI tests using AutomationId instead of fragile text matching
- Provide a reusable pattern for exposing AutomationId on any MAUI element that lacks native support

## Alignment with Product Vision

This feature directly supports the product vision:

| Product Principle | How This Feature Supports It |
|-------------------|------------------------------|
| **Platform-Native Performance** | Uses native AutomationPeer APIs instead of workarounds |
| **Consistent Over Identical** | Enables same container patterns across MAUI/WPF/WinForms |
| **Fail Fast with Context** | Container scoping provides meaningful context when elements aren't found |
| **Test Writer First** | Transparent solution—test writers don't need to know about platform limitations |

## Requirements

### Requirement 1: Container Control with AutomationPeer

**User Story:** As a test writer, I want to define containers in my MAUI application that are discoverable by UI Automation, so that I can scope element searches to specific containers.

#### Acceptance Criteria

1. WHEN a custom container control with `AutomationId="MyContainer"` is placed in XAML THEN UI Automation SHALL expose that element with the specified AutomationId
2. WHEN Appium queries for an element by AutomationId matching the container THEN the container element SHALL be returned as a valid element
3. WHEN a container is nested inside another container THEN both containers SHALL be independently discoverable in the automation tree
4. IF a container has no AutomationId THEN the control SHALL still function visually but not be discoverable via automation
5. WHEN a container contains child elements THEN those children SHALL be queryable relative to the container's automation element

### Requirement 2: Container Scoped Searching

**User Story:** As a test writer, I want to search for elements within a specific container's scope, so that I find the correct element when multiple containers have identical child structures.

#### Acceptance Criteria

1. WHEN searching for `AutomationId="UserName"` within `ContainerA` AND `ContainerB` also contains `AutomationId="UserName"` THEN the framework SHALL return only the element within `ContainerA`
2. WHEN a scoped search finds no matching element THEN the framework SHALL NOT fall back to searching the entire visual tree
3. WHEN searching from a nested container scope THEN the search SHALL be limited to descendants of that container
4. IF an element exists outside the container scope THEN `GetControl("ElementId")` SHALL throw `NoSuchElementException`
5. WHEN using indexed container access (e.g., `GetContainers()[0]`) THEN each container SHALL maintain independent scope

### Requirement 3: Visual Compatibility

**User Story:** As an app developer, I want the automation container to behave like a standard layout container, so that I don't have to change my app's visual design for testing purposes.

#### Acceptance Criteria

1. WHEN the automation container is used in XAML THEN it SHALL support standard layout properties (Padding, Margin, Background, etc.)
2. WHEN the container hosts child content THEN it SHALL layout children identically to ContentView or Border
3. IF the container is used without UI tests running THEN the app SHALL have no performance or visual impact
4. WHEN styling the container THEN ControlTemplates and Styles SHALL apply correctly

### Requirement 4: Cross-Platform Consideration

**User Story:** As a framework maintainer, I want the solution to work across MAUI target platforms (Windows, macOS, Android, iOS), so that container scoping works consistently.

#### Acceptance Criteria

1. WHEN running UI tests on Windows THEN the custom AutomationPeer SHALL expose AutomationId correctly
2. IF the solution uses platform-specific APIs THEN conditional compilation or multi-targeting SHALL isolate platform code
3. WHEN running on platforms without custom AutomationPeer support THEN a graceful fallback or clear documentation SHALL exist

### Requirement 5: Developer Experience

**User Story:** As a test writer, I want container controls that work transparently with the existing Brinell API, so that I don't need to learn new patterns.

#### Acceptance Criteria

1. WHEN defining a container page object THEN the existing `IContainerControl` interface SHALL work without modification
2. WHEN using `GetContainer<T>("ContainerId")` THEN the custom container control SHALL be correctly located and wrapped
3. WHEN container searching fails THEN error messages SHALL clearly indicate the AutomationId that wasn't found and the search scope
4. IF test writers use standard MAUI containers (Grid, Border) THEN documentation SHALL clearly state they are not automation-compatible

### Requirement 6: TabbedPage/Shell Tab AutomationId Support

**User Story:** As a test writer, I want to navigate between tabs in a TabbedPage or Shell using AutomationId, so that my tab navigation tests are reliable and locale-independent.

#### Acceptance Criteria

1. WHEN a Tab or ShellContent has `AutomationId="MyTab"` THEN UI Automation on Windows SHALL expose that tab element with the specified AutomationId
2. WHEN Appium queries for an element by AutomationId matching the tab THEN the tab element SHALL be returned and clickable
3. WHEN using TabbedPage with multiple tabs THEN each tab with an AutomationId SHALL be independently discoverable
4. WHEN using Shell with TabBar THEN each Tab or ShellContent with AutomationId SHALL be exposed to automation
5. IF a tab has no AutomationId THEN the tab SHALL still function but require Title-based selection (document limitation)

### Requirement 7: Custom TabbedPage Handler (Alternative Approach)

**User Story:** As a framework maintainer, I want to provide a custom TabbedPage handler that properly maps AutomationId to tab elements, so that test writers can use standard TabbedPage without modifications.

#### Acceptance Criteria

1. WHEN a custom TabbedPage handler is registered THEN the handler SHALL map each child page's AutomationId to the corresponding NavigationViewItem
2. WHEN the MAUI AutomationId property is set on a ContentPage within TabbedPage THEN the WinUI AutomationProperties.AutomationId SHALL be set on the tab
3. IF MAUI fixes this issue natively (issue #3996) THEN the custom handler SHALL be easily removable
4. WHEN using Shell instead of TabbedPage THEN a similar handler approach SHALL be documented or implemented

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility**: The custom control and its AutomationPeer SHALL be separate classes
- **Platform Isolation**: Platform-specific AutomationPeer code SHALL be isolated in `Platforms/Windows/` folder or conditional compilation
- **Clean Interface**: The control SHALL expose a simple XAML API similar to `Border` or `ContentView`
- **No Core Changes**: The `Brinell.Core` interface layer SHALL NOT require changes

### Performance

- **Zero Runtime Overhead**: The custom control SHALL have negligible performance impact compared to Border/ContentView
- **Lazy Peer Creation**: AutomationPeer SHALL be created only when automation queries it
- **No Reflection**: The solution SHALL NOT rely on runtime reflection for automation exposure

### Reliability

- **Test Isolation**: Container scoping SHALL not leak between tests
- **Deterministic Behavior**: Same test SHALL produce same results (no race conditions in scoping)
- **Resource Cleanup**: Container controls SHALL properly dispose when page/test ends

### Maintainability

- **Documentation**: Usage examples and limitations SHALL be documented
- **Sample App Integration**: The sample MAUI app SHALL demonstrate the container control
- **Unit Tests**: The control SHALL have unit tests for AutomationPeer behavior (if testable outside Appium)

## Scope

### In Scope

- Custom MAUI container control with AutomationPeer support for Windows
- TabbedPage/Shell tab AutomationId mapping for Windows
- Custom handler approach for TabbedPage to map AutomationId to NavigationViewItem
- Integration with existing `IContainerControl` and `MauiContainerBase`
- Sample app using the new container for ContainerScopingTests
- Sample app demonstrating TabbedPage with testable tabs
- Documentation for test writers

### Out of Scope

- Fixing the platform (Microsoft WinUI/MAUI) to add AutomationPeers to Grid/Border
- Fixing MAUI's TabbedPage/Shell AutomationId mapping (that's issue #3996)
- Blazor container scoping (different mechanism—DOM-based)
- WPF/WinForms (already works—panels have AutomationPeers via UIA)
- macOS/Android/iOS implementation (future work, document limitations)

## Technical Research Summary

### Microsoft Documentation Findings

From [Custom Automation Peers](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/custom-automation-peers):

1. **Override `OnCreateAutomationPeer()`** on the custom control:
   ```csharp
   protected override AutomationPeer OnCreateAutomationPeer()
   {
       return new TestContainerAutomationPeer(this);
   }
   ```

2. **Derive from `FrameworkElementAutomationPeer`**:
   ```csharp
   public class TestContainerAutomationPeer : FrameworkElementAutomationPeer
   {
       public TestContainerAutomationPeer(TestContainer owner) : base(owner) { }
       
       protected override string GetClassNameCore() => "TestContainer";
       protected override AutomationControlType GetAutomationControlTypeCore() 
           => AutomationControlType.Group;
   }
   ```

3. **AutomationId is exposed via `GetAutomationIdCore()`** - inherited from `FrameworkElementAutomationPeer` if the control's `AutomationProperties.AutomationId` is set.

### TabbedPage/Shell Research Findings

From GitHub Issues and MAUI Documentation:

1. **Issue #3996** (Open since January 2022): "[Enhancement] AutomationId for TabbedPage/Shell"
   - iOS has AutomationId support working out of the box
   - Android was never implemented
   - Windows uses NavigationView/NavigationViewItem but MAUI doesn't map AutomationId

2. **Issue #19328**: "[UITest] AutomationId not being set in Shell Tabbar"
   - Confirmed duplicate of #3996
   - MAUI team workaround: Use tab Title text instead of AutomationId

3. **WinUI NavigationViewItem** inherits from:
   - `NavigationViewItemBase` → `ListViewItem` → `SelectorItem` → `ContentControl` → `Control`
   - Controls DO have `OnCreateAutomationPeer()` support
   - The issue is in MAUI's handler not mapping the AutomationId property

4. **Potential Solution**: Custom TabbedPageHandler that:
   - Intercepts tab creation
   - Sets `AutomationProperties.AutomationId` on the WinUI NavigationViewItem
   - Maps from MAUI's `ContentPage.AutomationId` to the native element

### Proposed Solution Direction

#### For Containers:

Create a custom MAUI control (`TestContainer` or `AutomationContainer`) that:
1. Inherits from `ContentView` or `Border` for layout behavior
2. Overrides `OnCreateAutomationPeer()` on the WinUI handler level
3. Returns a custom `FrameworkElementAutomationPeer` that exposes:
   - `AutomationControlType.Group` as the control type
   - The `AutomationId` from the MAUI control's property
4. Works transparently with existing `MauiContainerBase` control wrapper

#### For TabbedPage/Shell:

Create a custom handler that:
1. Extends `TabbedPageHandler` for Windows
2. Overrides the method that creates NavigationViewItem for each tab
3. Sets `AutomationProperties.SetAutomationId()` on the NavigationViewItem
4. Uses the child ContentPage's `AutomationId` as the source

```csharp
// Conceptual approach
public class AutomationTabbedPageHandler : TabbedPageHandler
{
    protected override void OnTabAdded(ContentPage page, NavigationViewItem item)
    {
        base.OnTabAdded(page, item);
        
        if (!string.IsNullOrEmpty(page.AutomationId))
        {
            AutomationProperties.SetAutomationId(item, page.AutomationId);
        }
    }
}
```

### Alternative Approaches Considered

| Approach | Pros | Cons |
|----------|------|------|
| **Custom Control + AutomationPeer** | Native, clean, proper solution | Requires platform-specific code |
| **Custom Handler** | Works with existing controls | More complex, handler API may change |
| **Label Markers** (current workaround) | Works today, no new code | Breaks true scoping, adds noise to UI |
| **XPath Relative Searching** | Uses existing Appium capabilities | Complex, fragile, poor performance |
| **Attached Behavior** | Could apply to any element | May not create automation peer correctly |
| **Title-based Tab Selection** | Works without code changes | Fragile, locale-dependent, not recommended |

## References

- [Microsoft: Custom Automation Peers](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/custom-automation-peers)
- [Microsoft: MAUI Accessibility](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/accessibility)
- [Microsoft: MAUI TabbedPage](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pages/tabbedpage)
- [Microsoft: WinUI NavigationView](https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview)
- [Microsoft: NavigationViewItem Class](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.navigationviewitem)
- [UI Automation Fundamentals](https://learn.microsoft.com/en-us/windows/win32/winauto/ui-automation-fundamentals)
- [GitHub: dotnet/maui#3996 - AutomationId for TabbedPage/Shell](https://github.com/dotnet/maui/issues/3996)
- [GitHub: dotnet/maui#19328 - AutomationId not set in Shell Tabbar](https://github.com/dotnet/maui/issues/19328)
- SPEC-002b Interface Contracts (IContainerControl requirements)

---

**Document Version:** 1.1  
**Created:** January 18, 2026  
**Updated:** January 19, 2026  
**Workflow:** spec_workflow/requirements  
**Spec ID:** 017-container-automation-peer
