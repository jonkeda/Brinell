# Issue 002: TabView Causes App Crash in Microsoft.UI.Xaml.dll

**Status:** Open  
**Priority:** Critical  
**Created:** 2026-01-19  
**Component:** Sample App - MAUI  
**Affects:** All UI Tests  

---

## Summary

After converting MainPage from `TabbedPage` to `ContentPage` with `CommunityToolkit.Maui.TabView`, the sample application crashes immediately on startup when launched by Appium, causing all 54 UI tests to fail.

---

## Problem Details

### Background
- **Original Issue:** ContainerScopingTests (9 tests) failed with "Element not found: AutomationId:ContainersTab"
- **Root Cause Analysis:** TabViewControl expects `<toolkit:TabView><toolkit:TabViewItem>` structure, but MainPage.xaml used `<TabbedPage>` with direct page children
- **User Decision:** "Update the sample app. We found that the other control didn't work with automation in windows." (MauiTabControl with Title-based XPath locators deemed unsuitable for Windows automation)
- **Attempted Solution:** Convert MainPage.xaml from TabbedPage to ContentPage containing TabView with 9 TabViewItem elements

### Impact
**Before conversion:** 44/54 tests passing (ContainerScopingTests 0/9 failed)  
**After conversion:** 0/54 tests passing (ALL tests fail at fixture initialization)

---

## Technical Evidence

### 1. Windows Event Log - Application Crashes

Multiple crash events recorded during test execution:

```
Faulting application name: Brinell.Samples.Maui.App.exe
Faulting module name: Microsoft.UI.Xaml.dll, version: 3.1.7.0
Exception code: 0xc000027b (Application-internal exception)
Fault offset: 0x000000000039cde5
```

**Exception Code 0xc000027b** indicates a fatal XAML parsing or initialization error in WinUI 3.

### 2. Test Failure Pattern

All tests fail identically in MauiTestContext constructor:

```
OpenQA.Selenium.UnknownErrorException : Failed to locate opened application window 
with appId: E:\...\Brinell.Samples.Maui.App.exe, and processId: 12684

Stack Trace:
   at OpenQA.Selenium.WebDriver.StartSession(ICapabilities capabilities)
   at OpenQA.Selenium.Appium.Windows.WindowsDriver..ctor(Uri remoteAddress, AppiumOptions)
   at Brinell.Maui.Context.MauiTestContext..ctor(MauiTestContextOptions options) 
      in MauiTestContext.cs:line 38
```

**Key Observations:**
- App process starts (processId visible in error messages)
- App window never becomes accessible to Appium
- Error occurs during WindowsDriver session initialization
- Appium timeout increased to 30s (`appWaitDuration: 30000`) - no effect
- Failure occurs before any test code executes (fixture constructor phase)

### 3. Manual Launch Verification

- ✅ App builds successfully (7.6s, 8 warnings about Frame obsolescence)
- ✅ App launches manually via Start-Process - window visible and interactive
- ❌ App crashes when launched by Appium - window never renders

**Conclusion:** TabView initialization crashes when app started under Appium automation context.

---

## Code Changes Made (Now Problematic)

### MainPage.xaml Conversion

**BEFORE (Working for 44/54 tests):**
```xaml
<TabbedPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
            x:Class="Brinell.Samples.Maui.App.MainPage"
            Title="Brinell Sample App">
    <local:MainContentPage Title="Main" AutomationId="MainTab" />
    <pages:DashboardPage Title="Dashboard" AutomationId="DashboardTab" />
    <!-- ...7 more pages... -->
    <pages:ContainerDemoPage Title="Container Demo" AutomationId="ContainersTab" />
</TabbedPage>
```

**AFTER (Crashes - 0/54 tests pass):**
```xaml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             x:Class="Brinell.Samples.Maui.App.MainPage"
             Title="Brinell Sample App">
    <toolkit:TabView AutomationId="MainTabView">
        <toolkit:TabViewItem AutomationId="MainTab" Text="Main">
            <local:MainContentPage />
        </toolkit:TabViewItem>
        <toolkit:TabViewItem AutomationId="DashboardTab" Text="Dashboard">
            <pages:DashboardPage />
        </toolkit:TabViewItem>
        <!-- ...7 more TabViewItems... -->
        <toolkit:TabViewItem AutomationId="ContainersTab" Text="Container Demo">
            <pages:ContainerDemoPage />
        </toolkit:TabViewItem>
    </toolkit:TabView>
</ContentPage>
```

### MainPage.xaml.cs
```csharp
// BEFORE
public partial class MainPage : TabbedPage

// AFTER (Problematic)
public partial class MainPage : ContentPage
```

### AppiumFixture.cs - Timeout Configuration
```csharp
protected override void ConfigureWindowsOptions(AppiumOptions options, string appPath)
{
    base.ConfigureWindowsOptions(options, appPath);
    options.AddAdditionalAppiumOption("appWaitDuration", 30000); // 30 seconds
    options.AddAdditionalAppiumOption("newCommandTimeout", 300);  // 5 minutes
}
```

---

## Environment Details

- **Framework:** .NET 10.0 (net10.0-windows10.0.19041.0)
- **MAUI Version:** Latest (via Microsoft.Maui.Controls package)
- **CommunityToolkit.Maui:** Referenced in csproj (version from Directory.Packages.props)
- **WinUI 3:** Microsoft.UI.Xaml.dll version 3.1.7.0
- **Appium:** 3.1.2 on http://127.0.0.1:4723
- **Windows UIA:** UI Automation for element location
- **Test Framework:** xUnit 3.1.5
- **OS:** Windows (10.0.17763.0 minimum, targeting 10.0.19041.0)

---

## Root Cause Hypothesis

The `CommunityToolkit.Maui.TabView` control has a **critical incompatibility** with the Appium automation environment on Windows:

1. **TabView Initialization Complexity:** TabView is a complex control that dynamically manages child content, tab headers, and visual states
2. **Timing Issue:** TabView may require additional initialization time or specific window properties that aren't available when launched via Appium
3. **WinUI 3 Automation Tree:** TabView might not expose proper UIA (UI Automation) tree structure during initialization
4. **ContentPage Container Issue:** Using ContentPage as direct Window content (vs TabbedPage which IS a Page) may affect window creation
5. **MAUI + WinUI 3 Interop:** Known issues with CommunityToolkit controls and Windows automation frameworks

**Exception 0xc000027b** specifically suggests:
- XAML resource not found
- Invalid XAML structure
- Missing dependency for TabView rendering
- Incompatible property values in TabView/TabViewItem

---

## Affected Tests

### All Test Classes Now Fail
- ❌ MainPageTests (16 tests) - Previously 15/16 passing
- ❌ ButtonControlTests (12 tests) - Previously 12/12 passing  
- ❌ EntryControlTests (17 tests) - Previously 17/17 passing
- ❌ ContainerScopingTests (9 tests) - Previously 0/9 passing (original issue)

**Total Impact:** 54/54 tests failing (100% failure rate)

---

## Investigation Steps Performed

1. ✅ Verified XAML syntax is correct (app builds without errors)
2. ✅ Confirmed CommunityToolkit.Maui package referenced and initialized (UseMauiCommunityToolkit() in MauiProgram.cs)
3. ✅ Tested manual app launch - works correctly
4. ✅ Increased Appium timeout to 30 seconds - no effect
5. ✅ Checked Windows Event Log - found crash evidence
6. ✅ Reviewed Appium logs - no helpful diagnostic information
7. ✅ Tested other test classes - all fail identically

---

## Possible Solutions

### Option 1: Revert to TabbedPage (RECOMMENDED SHORT-TERM)
**Pros:**
- Restores 44/54 passing tests immediately
- Known working configuration
- Minimal risk

**Cons:**
- ContainerScopingTests still fail (original issue)
- Doesn't address user requirement that "the other control didn't work with automation in windows"

**Action:** Revert MainPage.xaml and MainPage.xaml.cs to TabbedPage structure

---

### Option 2: Fix TabView Implementation
**Investigation needed:**
- Research CommunityToolkit.Maui.TabView + Appium compatibility
- Check if TabView requires specific Window properties
- Test TabView in isolation (minimal app)
- Review CommunityToolkit.Maui GitHub issues for similar problems

**Possible fixes:**
- Add explicit Window.Title property
- Set Window AutomationProperties
- Use different TabView initialization pattern
- Add delay/ready check in app startup

---

### Option 3: Alternative Tab Navigation Control
**Options to explore:**
- Custom tab control using CollectionView + ContentView
- MAUI Shell with TabBar (requires full app restructure)
- Standard Grid with manual tab switching
- Third-party tab control with proven Appium compatibility

---

### Option 4: Fix Original ContainersTab Issue with TabbedPage
**Address why ContainersTab wasn't found originally:**
- Review Page hierarchy in TabbedPage
- Check if ContainerDemoPage has initialization issues
- Verify AutomationId propagation in TabbedPage
- Test if adding explicit delay before ContainersTab click helps
- Investigate if ContainersTab needs explicit WaitReady() call

---

## Recommended Action Plan

### Phase 1: Stabilize (Immediate)
1. **Revert to TabbedPage structure**
   - Restore MainPage.xaml to TabbedPage with 9 child pages
   - Restore MainPage.xaml.cs base class to TabbedPage
   - Remove TabView-specific timeout configuration from AppiumFixture.cs
2. **Verify test suite recovery**
   - Run all tests to confirm 44/54 passing status restored
   - Document exact state of ContainerScopingTests failures

### Phase 2: Root Cause Analysis (Original Issue)
1. **Investigate ContainersTab specifically**
   - Why does MainTab, DashboardTab work but ContainersTab doesn't?
   - Review ContainerDemoPage.xaml for problematic controls
   - Check page load timing for ContainerDemoPage
   - Test navigation to ContainersTab after explicit delays
2. **Review TabViewControl implementation**
   - Understand AutomationId expectations
   - Compare with working tab navigation (MainTab)
   - Check if issue is in control location or page activation

### Phase 3: Long-term Solution
Depends on Phase 2 findings:
- Fix ContainerDemoPage initialization if that's the issue
- Implement alternative tab control if TabView fundamentally incompatible
- Work with CommunityToolkit.Maui team if TabView bug confirmed

---

## References

- **Original Issue:** 001-containerscopingtests-fail-containerstab-not-found.isu.spx.md
- **Test Results:** See terminal output from Jan 19, 2026 10:34 AM
- **Windows Event Log:** Application Error events with Report IDs:
  - e91b3132-4155-4d8a-b2a0-1b2fa70c5ef0
  - 021e8301-e486-4723-ae57-72de742abf00
  - c8d0ef4f-08aa-4520-bd7b-93ffccb61a0f
- **Code Files:**
  - `samples/Brinell.Samples.Maui.App/MainPage.xaml`
  - `samples/Brinell.Samples.Maui.App/MainPage.xaml.cs`
  - `testsnew/Brinell.Maui.UITests/AppiumFixture.cs`

---

## Notes

- User explicitly stated MauiTabControl (Title-based XPath approach) doesn't work with Windows automation
- TabView was chosen as alternative because TabViewControl uses AutomationId-based locators
- Manual testing shows TabView XAML is valid and app runs when launched directly
- Crash only occurs under Appium automation context
- Exception location in Microsoft.UI.Xaml.dll suggests WinUI 3 rendering issue, not MAUI framework issue

---

**Next Steps:**
1. Revert TabView changes to restore test suite functionality
2. Re-investigate original ContainerScopingTests failure with TabbedPage
3. Document findings and determine if issue is with ContainerDemoPage content or navigation mechanism
