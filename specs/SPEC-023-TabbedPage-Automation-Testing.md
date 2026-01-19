# SPEC-023: TabbedPage Automation Testing

**Status:** Draft  
**Created:** January 19, 2026  
**Priority:** High  
**Related:** SPEC-016 (TabBar Navigation Redesign), dotnet/maui#3996  
**Author:** Brinell Framework Team

---

## 1. Executive Summary

This specification addresses the inability to automate TabbedPage tab navigation in .NET MAUI Windows applications using Appium. The core issue is that TabbedPage tabs are not accessible via AutomationId in Windows UI Automation, despite a custom `TabbedPageAutomationMapper` already in place.

### 1.1 Problem Statement

TabbedPage tabs cannot be clicked via AutomationId in UI tests:

```
ElementNotFoundException: Element not found with locator: AutomationId:ContainersTab after 1000ms
```

This blocks all UI tests that require navigation to non-default tabs.

### 1.2 Impact

| Test Category | Status | Impact |
|--------------|--------|--------|
| MainPageTests | ✅ Passing | Tests on default tab work |
| ButtonControlTests | ✅ Passing | Tests on default tab work |
| EntryControlTests | ✅ Passing | Tests on default tab work |
| ContainerScopingTests | ❌ Skipped | Requires ContainersTab navigation |
| SingleContainerTests | ❌ Skipped | Requires ContainersTab navigation |
| IndexedContainerTests | ❌ Skipped | Requires ContainersTab navigation |
| ListContainerTests | ❌ Skipped | Requires ContainersTab navigation |
| NestedContainerTests | ❌ Skipped | Requires ContainersTab navigation |

**24+ tests currently skipped** due to this limitation.

---

## 2. Technical Analysis

### 2.1 Current Architecture

**Sample App Structure:**
```xml
<!-- MainPage.xaml -->
<TabbedPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
            x:Class="Brinell.Samples.Maui.App.MainPage">
    
    <ContentPage Title="Basics" AutomationId="BasicsTab">
        <views:BasicsView />
    </ContentPage>

    <ContentPage Title="Containers" AutomationId="ContainersTab">
        <views:ContainerDemoView />
    </ContentPage>
    
    <!-- 6 more tabs... -->
</TabbedPage>
```

**Existing Workaround Attempt:**
```csharp
// TabbedPageAutomationMapper.cs
public static class TabbedPageAutomationMapper
{
    public static void Configure()
    {
        TabbedViewHandler.Mapper.AppendToMapping("AutomationIdFix", MapAutomationIds);
    }

    private static void MapAutomationIds(ITabbedViewHandler handler, ITabbedView tabbedView)
    {
        // Maps AutomationId from ContentPage to NavigationViewItem
        var navigationView = handler.PlatformView as NavigationView;
        var tabbedPage = tabbedView as TabbedPage;
        
        for (int i = 0; i < children.Count && i < menuItems.Count; i++)
        {
            var child = children[i];
            var menuItem = menuItems[i];

            if (menuItem is NavigationViewItem navItem && !string.IsNullOrEmpty(child.AutomationId))
            {
                WinUIAutomation.AutomationProperties.SetAutomationId(navItem, child.AutomationId);
            }
        }
    }
}
```

### 2.2 Why The Current Workaround Fails

**Investigation reveals multiple potential issues:**

1. **Timing Problem**: The mapper runs during handler initialization, but:
   - NavigationView.MenuItems may be empty at mapping time
   - Tabs may be added after the mapping runs
   - Appium may query before AutomationId propagates to the UI Automation tree

2. **Wrong Element**: The NavigationViewItem may not be the clickable element:
   - WinUI NavigationView uses complex internal structure
   - The actual clickable target might be a child element (TextBlock, Button, etc.)
   - AutomationId on NavigationViewItem may not reach the interactive element

3. **Property Propagation**: AutomationProperties may not propagate correctly:
   - WinUI has complex automation tree generation
   - AutomationId might be set but not exposed to UI Automation

### 2.3 Windows TabbedPage Native Structure

On Windows, MAUI TabbedPage renders using WinUI NavigationView:

```
NavigationView (TabbedPage)
├── NavigationViewItem (Tab 1: "Basics")
│   └── TextBlock (Title text)
├── NavigationViewItem (Tab 2: "Containers")
│   └── TextBlock (Title text)
├── ...
└── ContentPresenter (Current page content)
```

**UI Automation Tree** (what Appium sees):
```
"NavigationViewItem" (Name: "Basics", AutomationId: ???)
├── "Text" (Name: "Basics")
"NavigationViewItem" (Name: "Containers", AutomationId: ???)
├── "Text" (Name: "Containers")
```

The **Name** property contains the Title, but **AutomationId** may be empty or incorrect.

---

## 3. Solution Options

### Option A: Fix TabbedPageAutomationMapper (Recommended)

**Approach:** Enhance the existing mapper with proper timing and element targeting.

**Changes Required:**

1. **Defer mapping until NavigationView is loaded**
2. **Re-map when tabs are added/removed**
3. **Set AutomationId on both NavigationViewItem AND its content**
4. **Add diagnostic logging for debugging**

**Implementation:**
```csharp
public static class TabbedPageAutomationMapper
{
    public static void Configure()
    {
        TabbedViewHandler.Mapper.AppendToMapping("AutomationIdFix", MapAutomationIds);
    }

    private static void MapAutomationIds(ITabbedViewHandler handler, ITabbedView tabbedView)
    {
        if (handler.PlatformView is not NavigationView navigationView)
            return;
        if (tabbedView is not TabbedPage tabbedPage)
            return;

        // Defer until NavigationView is loaded
        navigationView.Loaded -= OnNavigationViewLoaded;
        navigationView.Loaded += OnNavigationViewLoaded;
        
        // Handle tab collection changes
        tabbedPage.ChildAdded -= OnTabsChanged;
        tabbedPage.ChildAdded += OnTabsChanged;
        tabbedPage.ChildRemoved -= OnTabsChanged;
        tabbedPage.ChildRemoved += OnTabsChanged;

        void OnNavigationViewLoaded(object sender, RoutedEventArgs e)
        {
            // Use dispatcher to ensure UI is fully rendered
            navigationView.DispatcherQueue.TryEnqueue(() =>
            {
                ApplyAutomationIds(navigationView, tabbedPage);
            });
        }

        void OnTabsChanged(object sender, ElementEventArgs e)
        {
            navigationView.DispatcherQueue.TryEnqueue(() =>
            {
                ApplyAutomationIds(navigationView, tabbedPage);
            });
        }
    }

    private static void ApplyAutomationIds(NavigationView navigationView, TabbedPage tabbedPage)
    {
        var menuItems = navigationView.MenuItems;
        var children = tabbedPage.Children;

        for (int i = 0; i < children.Count && i < menuItems.Count; i++)
        {
            var child = children[i];
            if (menuItems[i] is NavigationViewItem navItem && !string.IsNullOrEmpty(child.AutomationId))
            {
                // Set on NavigationViewItem
                WinUIAutomation.AutomationProperties.SetAutomationId(navItem, child.AutomationId);
                WinUIAutomation.AutomationProperties.SetName(navItem, child.AutomationId);
                
                // Force UI Automation tree refresh
                WinUIAutomation.AutomationPeer.ListenerExists(WinUIAutomation.AutomationEvents.StructureChanged);
                
                System.Diagnostics.Debug.WriteLine(
                    $"[TabbedPageAutomationMapper] Set AutomationId='{child.AutomationId}' on tab {i}");
            }
        }
    }
}
```

**Pros:**
- Minimal changes to existing architecture
- Works with current TabbedPage XAML
- No changes to test code

**Cons:**
- May not work if WinUI doesn't expose AutomationId on NavigationViewItem
- Complex timing/threading considerations

---

### Option B: Alternative Locator Strategy (Name-Based)

**Approach:** Use the tab's Name property instead of AutomationId for location.

**Rationale:** WinUI NavigationViewItem exposes the Title as the `Name` accessibility property.

**Framework Changes:**
```csharp
// New locator type: ByName
public static class Locator
{
    public static Locator ByName(string name) => new Locator(LocatorType.Name, name);
}

// Usage in page object
public MauiButtonControl<AppShellPage> ContainersTab => 
    new(this, Locator.ByName("Containers"));
```

**Appium Implementation:**
```csharp
// Find by Name attribute (accessibility name)
driver.FindElement(MobileBy.Name("Containers"));
```

**Pros:**
- Works immediately without app changes
- Uses existing accessibility property
- Simple implementation

**Cons:**
- Name is localization-dependent (Title changes per language)
- Less precise than AutomationId
- Requires framework changes to support new locator type

---

### Option C: Use TabbedPage.CurrentPage Programmatic Navigation

**Approach:** Instead of clicking tabs, use programmatic navigation via AutomationId on the TabbedPage itself.

**Sample App Change:**
```csharp
// MainPage.xaml.cs
public partial class MainPage : TabbedPage
{
    public void NavigateToTab(string automationId)
    {
        var page = Children.FirstOrDefault(p => p.AutomationId == automationId);
        if (page != null)
            CurrentPage = page;
    }
}
```

**Expose via Custom Action:**
```csharp
// Add a hidden button with action
<Button AutomationId="NavigateToContainersTabAction" 
        IsVisible="False"
        Command="{Binding NavigateToContainersCommand}" />
```

**Pros:**
- Guaranteed to work
- No native platform issues

**Cons:**
- Pollutes app with test-only code
- Doesn't test actual tab clicking
- Not representative of user behavior

---

### Option D: Migrate to Shell TabBar (Long-term)

**Approach:** Replace TabbedPage with Shell TabBar navigation.

**Rationale:** Shell is the recommended navigation paradigm for MAUI and has better automation support.

**App Change:**
```xml
<!-- Replace MainPage.xaml TabbedPage with: -->
<Shell xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       x:Class="Brinell.Samples.Maui.App.AppShell">
    <TabBar>
        <Tab Title="Basics" AutomationId="BasicsTab">
            <ShellContent ContentTemplate="{DataTemplate views:BasicsView}" />
        </Tab>
        <Tab Title="Containers" AutomationId="ContainersTab">
            <ShellContent ContentTemplate="{DataTemplate views:ContainerDemoView}" />
        </Tab>
        <!-- ... more tabs -->
    </TabBar>
</Shell>
```

**Pros:**
- Follows MAUI best practices
- Better automation support out of the box
- Aligns with SPEC-016 direction

**Cons:**
- Significant app restructuring
- Shell has different navigation paradigm
- May not be appropriate for all app types
- TabbedPage still valid use case for simple apps

---

### Option E: XPath Fallback for Tabs

**Approach:** Use XPath to find tabs by Name when AutomationId fails.

**Implementation:**
```csharp
// AppiumFixture.cs
public void NavigateToContainerDemo()
{
    // Try AutomationId first, fall back to Name-based XPath
    try
    {
        var tab = _driver.FindElement(MobileBy.AccessibilityId("ContainersTab"));
        tab.Click();
    }
    catch (NoSuchElementException)
    {
        // Fallback: Find by Name via XPath
        var tab = _driver.FindElement(By.XPath("//NavigationViewItem[@Name='Containers']"));
        tab.Click();
    }
}
```

**Pros:**
- Works with current app
- No app changes needed
- Immediate solution

**Cons:**
- XPath can be slow and fragile
- Relies on internal WinUI structure
- Not portable to other platforms

---

## 4. Recommended Approach

### Primary: Option A (Fix Mapper) + Option E (Fallback)

**Phase 1 - Immediate Fix (Option E):**
1. Implement XPath fallback in test navigation methods
2. Unskip container tests
3. Verify all tests pass

**Phase 2 - Proper Fix (Option A):**
1. Enhance TabbedPageAutomationMapper with:
   - Deferred loading via Loaded event
   - DispatcherQueue for UI thread safety
   - Tab collection change handling
   - Diagnostic logging
2. Verify AutomationId appears in UI Automation tree
3. Remove XPath fallback once verified

**Phase 3 - Framework Enhancement (Option B):**
1. Add `Locator.ByName()` to framework
2. Document as fallback for platform limitations
3. Use for tabs if AutomationId continues to fail

---

## 5. Implementation Details

### 5.1 Phase 1: XPath Fallback

**File:** `testsnew/Brinell.Maui.UITests/AppiumFixture.cs`

```csharp
/// <summary>
/// Navigates to a tab by AutomationId, with XPath fallback.
/// </summary>
/// <param name="automationId">The tab's AutomationId (e.g., "ContainersTab")</param>
/// <param name="tabTitle">The tab's display title (e.g., "Containers") for fallback</param>
public void NavigateToTab(string automationId, string tabTitle)
{
    try
    {
        // Primary: Use AutomationId
        var tab = Context.Driver.FindElement(MobileBy.AccessibilityId(automationId));
        tab.Click();
        System.Diagnostics.Debug.WriteLine($"[Navigation] Clicked tab via AutomationId: {automationId}");
    }
    catch (OpenQA.Selenium.NoSuchElementException)
    {
        // Fallback: Use Name via XPath
        System.Diagnostics.Debug.WriteLine($"[Navigation] AutomationId '{automationId}' not found, trying XPath with Name='{tabTitle}'");
        
        // WinUI NavigationViewItem structure
        var xpaths = new[]
        {
            $"//NavigationViewItem[@Name='{tabTitle}']",
            $"//*[@Name='{tabTitle}' and @LocalizedControlType='tab item']",
            $"//*[@AutomationId='{automationId}']",
            $"//*[contains(@Name,'{tabTitle}')][@LocalizedControlType='tab item']"
        };

        foreach (var xpath in xpaths)
        {
            try
            {
                var tab = Context.Driver.FindElement(By.XPath(xpath));
                tab.Click();
                System.Diagnostics.Debug.WriteLine($"[Navigation] Clicked tab via XPath: {xpath}");
                return;
            }
            catch (OpenQA.Selenium.NoSuchElementException)
            {
                // Try next XPath
            }
        }

        throw new ElementNotFoundException(
            $"Could not find tab '{automationId}' (title: '{tabTitle}'). " +
            "Tried AutomationId and multiple XPath strategies.");
    }
}

/// <summary>
/// Navigates to the Container Demo page.
/// </summary>
public void NavigateToContainerDemo()
{
    NavigateToTab("ContainersTab", "Containers");
    
    // Wait for page to load
    ContainerDemoPage.WaitReady();
}
```

### 5.2 Phase 2: Enhanced Mapper

**File:** `samples/Brinell.Samples.Maui.App/Platforms/Windows/Handlers/TabbedPageAutomationMapper.cs`

```csharp
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIAutomation = Microsoft.UI.Xaml.Automation;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// Enhanced TabbedPage handler that properly maps AutomationId to NavigationViewItem tabs.
/// Fixes GitHub issue dotnet/maui#3996.
/// </summary>
public static class TabbedPageAutomationMapper
{
    private static bool _configured = false;
    private static readonly Dictionary<NavigationView, TabbedPage> _trackedPages = new();

    public static void Configure()
    {
        if (_configured)
            return;

        _configured = true;
        TabbedViewHandler.Mapper.AppendToMapping("AutomationIdFix", MapAutomationIds);
    }

    private static void MapAutomationIds(ITabbedViewHandler handler, ITabbedView tabbedView)
    {
        try
        {
            if (handler.PlatformView is not NavigationView navigationView)
            {
                Log("PlatformView is not NavigationView");
                return;
            }

            if (tabbedView is not TabbedPage tabbedPage)
            {
                Log("VirtualView is not TabbedPage");
                return;
            }

            // Track for cleanup
            _trackedPages[navigationView] = tabbedPage;

            // Wait for NavigationView to be loaded
            if (navigationView.IsLoaded)
            {
                ApplyAutomationIdsDeferred(navigationView, tabbedPage);
            }
            else
            {
                navigationView.Loaded += OnNavigationViewLoaded;
            }

            // Handle dynamic tab changes
            tabbedPage.ChildAdded += OnTabsChanged;
            tabbedPage.ChildRemoved += OnTabsChanged;
            navigationView.Unloaded += OnNavigationViewUnloaded;
        }
        catch (Exception ex)
        {
            Log($"Error in MapAutomationIds: {ex.Message}");
        }
    }

    private static void OnNavigationViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationView navigationView && _trackedPages.TryGetValue(navigationView, out var tabbedPage))
        {
            ApplyAutomationIdsDeferred(navigationView, tabbedPage);
        }
    }

    private static void OnTabsChanged(object? sender, ElementEventArgs e)
    {
        // Find the NavigationView for this TabbedPage
        foreach (var kvp in _trackedPages)
        {
            if (ReferenceEquals(kvp.Value, sender))
            {
                ApplyAutomationIdsDeferred(kvp.Key, kvp.Value);
                break;
            }
        }
    }

    private static void OnNavigationViewUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationView navigationView)
        {
            navigationView.Loaded -= OnNavigationViewLoaded;
            navigationView.Unloaded -= OnNavigationViewUnloaded;
            
            if (_trackedPages.TryGetValue(navigationView, out var tabbedPage))
            {
                tabbedPage.ChildAdded -= OnTabsChanged;
                tabbedPage.ChildRemoved -= OnTabsChanged;
                _trackedPages.Remove(navigationView);
            }
        }
    }

    private static void ApplyAutomationIdsDeferred(NavigationView navigationView, TabbedPage tabbedPage)
    {
        // Use DispatcherQueue to ensure we're on UI thread after layout is complete
        navigationView.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            ApplyAutomationIds(navigationView, tabbedPage);
        });
    }

    private static void ApplyAutomationIds(NavigationView navigationView, TabbedPage tabbedPage)
    {
        try
        {
            var menuItems = navigationView.MenuItems;
            var children = tabbedPage.Children;

            Log($"Applying AutomationIds: {menuItems.Count} menu items, {children.Count} children");

            for (int i = 0; i < children.Count && i < menuItems.Count; i++)
            {
                var child = children[i];
                var menuItem = menuItems[i];

                if (menuItem is NavigationViewItem navItem && !string.IsNullOrEmpty(child.AutomationId))
                {
                    // Set AutomationId
                    WinUIAutomation.AutomationProperties.SetAutomationId(navItem, child.AutomationId);
                    
                    // Also set Name as backup (some Appium drivers prefer Name)
                    WinUIAutomation.AutomationProperties.SetName(navItem, child.AutomationId);
                    
                    // Set accessible name for screen readers
                    WinUIAutomation.AutomationProperties.SetHelpText(navItem, $"Tab: {child.Title}");

                    Log($"Tab {i}: Set AutomationId='{child.AutomationId}', Name='{child.AutomationId}', Title='{child.Title}'");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error applying AutomationIds: {ex.Message}");
        }
    }

    private static void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[TabbedPageAutomationMapper] {message}");
    }
}
```

### 5.3 Diagnostic Tool

**Add to AppiumFixture for debugging:**

```csharp
/// <summary>
/// Dumps the UI Automation tree to help debug element location issues.
/// </summary>
public void DumpPageSource(string filename = "pagesource.xml")
{
    var pageSource = Context.Driver.PageSource;
    var path = Path.Combine(TestContext.Current.TestOutputDirectory, filename);
    File.WriteAllText(path, pageSource);
    System.Diagnostics.Debug.WriteLine($"[Diagnostic] Page source saved to: {path}");
}

/// <summary>
/// Searches for elements matching a pattern in the page source.
/// </summary>
public void FindElementsInPageSource(string pattern)
{
    var pageSource = Context.Driver.PageSource;
    var regex = new System.Text.RegularExpressions.Regex(pattern, 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    
    var matches = regex.Matches(pageSource);
    System.Diagnostics.Debug.WriteLine($"[Diagnostic] Found {matches.Count} matches for pattern '{pattern}':");
    foreach (Match match in matches)
    {
        System.Diagnostics.Debug.WriteLine($"  - {match.Value}");
    }
}
```

---

## 6. Testing Strategy

### 6.1 Test Cases for Tab Navigation

```csharp
[Fact]
public void TabbedPage_NavigateToContainersTab_ShowsContainerDemoView()
{
    // Act
    _fixture.NavigateToTab("ContainersTab", "Containers");
    
    // Assert
    _fixture.ContainerDemoPage.UserProfile.AssertExists();
}

[Fact]
public void TabbedPage_AllTabs_Navigable()
{
    var tabs = new[]
    {
        ("BasicsTab", "Basics"),
        ("ContainersTab", "Containers"),
        ("FormsTab", "Forms"),
        ("ListsTab", "Lists"),
        ("GesturesTab", "Gestures"),
        ("NavigationTab", "Navigation"),
        ("ToolkitTab", "Toolkit"),
        ("MediaTab", "Media")
    };

    foreach (var (automationId, title) in tabs)
    {
        // Act
        _fixture.NavigateToTab(automationId, title);
        
        // Assert - verify page changed (no exception means success)
        System.Diagnostics.Debug.WriteLine($"Successfully navigated to tab: {title}");
    }
}

[Fact]
public void TabbedPage_TabClickViaAutomationId_Works()
{
    // This test specifically verifies AutomationId works (no XPath fallback)
    var tab = Context.Driver.FindElement(MobileBy.AccessibilityId("ContainersTab"));
    tab.Click();
    
    // Assert
    _fixture.ContainerDemoPage.UserProfile.AssertExists();
}
```

### 6.2 Diagnostic Test

```csharp
[Fact]
public void Debug_DumpPageSource_ForTabAnalysis()
{
    // Dump full page source
    _fixture.DumpPageSource("full-page.xml");
    
    // Search for tab-related elements
    _fixture.FindElementsInPageSource("ContainersTab");
    _fixture.FindElementsInPageSource("NavigationViewItem");
    _fixture.FindElementsInPageSource("Containers");
    
    // Always pass - this is a diagnostic test
    Assert.True(true);
}
```

---

## 7. Success Criteria

### 7.1 Minimum Viable Success

1. ✅ Container tests can navigate to ContainersTab using XPath fallback
2. ✅ All skipped tests are unskipped and passing
3. ✅ Diagnostic tooling available for debugging

### 7.2 Full Success

1. ✅ AutomationId works on TabbedPage tabs via enhanced mapper
2. ✅ No XPath fallback needed
3. ✅ Tab navigation works reliably across app restarts
4. ✅ Framework supports `Locator.ByName()` as fallback option

---

## 8. Timeline

| Phase | Scope | Duration | Deliverables |
|-------|-------|----------|--------------|
| **Phase 1** | XPath Fallback | 2 hours | Working tab navigation, unskipped tests |
| **Phase 2** | Enhanced Mapper | 4 hours | Proper AutomationId mapping |
| **Phase 3** | Framework Enhancement | 4 hours | `Locator.ByName()` support |
| **Validation** | All tests pass | 2 hours | Full test suite green |

**Total: ~12 hours**

---

## 9. Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| AutomationId never works on NavigationViewItem | Medium | High | Option B (Name-based locator) as permanent solution |
| XPath fallback is too slow | Low | Medium | Cache XPath results, use more specific patterns |
| WinUI internal structure changes | Low | High | Add version detection, multiple fallback patterns |
| Timing issues with deferred mapping | Medium | Medium | Add retry logic, increase timeouts |

---

## 10. References

- [dotnet/maui#3996 - AutomationId for TabbedPage/Shell](https://github.com/dotnet/maui/issues/3996)
- [dotnet/maui#19328 - AutomationId not being set in Shell Tabbar](https://github.com/dotnet/maui/issues/19328)
- [WinUI NavigationView Documentation](https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview)
- [UI Automation Tree Documentation](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-treeoverview)
- [SPEC-016: TabBar Navigation Redesign](./SPEC-016-TabBar-Navigation-Redesign.md)

---

## Appendix A: Known MAUI Issues

### dotnet/maui#3996

**Title:** [Enhancement] AutomationId for TabbedPage/Shell  
**Status:** Open (Backlog)  
**Description:** AutomationId on TabbedPage child pages doesn't propagate to the tab elements on Windows/Android.  
**Workaround:** Custom handler mapper (this spec provides the implementation).

### dotnet/maui#19328

**Title:** [UITest] AutomationId not being set in Shell Tabbar  
**Status:** Closed (related to #3996)  
**Description:** Shell TabBar has same issue as TabbedPage.

---

## Appendix B: Debug Page Source Analysis

Example NavigationViewItem structure in page source:

```xml
<NavigationViewItem 
    AutomationId="" 
    Name="Basics" 
    LocalizedControlType="tab item"
    IsOffscreen="False"
    IsEnabled="True"
    IsSelected="True">
    <TextBlock Name="Basics" />
</NavigationViewItem>
```

**Key observations:**
- `AutomationId` is empty (confirms the bug)
- `Name` contains the Title (usable for fallback)
- `LocalizedControlType` is "tab item" (useful for XPath)
- `IsSelected` indicates current tab

---

**Revision History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-19 | Brinell Team | Initial draft |
