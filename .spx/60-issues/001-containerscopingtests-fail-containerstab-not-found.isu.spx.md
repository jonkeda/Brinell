# Issue 001: ContainerScopingTests fail - ContainersTab element not found

## Status: Open
## Date: January 19, 2026
## Version: Brinell MAUI Tests - Current (testsnew/Brinell.Maui.UITests)

## Summary

All 9 tests in the ContainerScopingTests test class fail during fixture construction when attempting to navigate to the Containers demo page. The tests cannot find the "ContainersTab" element (AutomationId:ContainersTab) in the MAUI sample application, causing all tests to fail before they can even run their test logic. This appears to be an issue with either the control object mapping, the Windows UIA automation ID implementation, or a mismatch between expected and actual UI structure in the sample app.

## Symptoms

1. All 9 tests in ContainerScopingTests fail with identical error: "Element not found with locator: AutomationId:ContainersTab after 1000ms"
2. Failure occurs in the test constructor during `AppiumFixture.NavigateToContainerDemo()` call
3. MainPageTests has similar issue with 1 test failing on "MainTab" element not found
4. Other test classes (MainPageTests, ButtonControlTests, EntryControlTests) pass successfully, indicating Appium and basic framework functionality work correctly
5. The error occurs consistently on every test run

## Evidence

### Error Messages

```
Brinell.Core.Exceptions.ElementNotFoundException : Element not found with locator: AutomationId:ContainersTab after 1000ms

Stack Trace:
   at Brinell.Maui.Context.MauiTestContext.FindElement(Locator locator) in E:\repos\Private\Iosk\Oravey\Brinell\srcnew\Brinell.Maui\Context\MauiTestContext.cs:line 125
   at Brinell.Maui.Pages.MauiPageObjectBase`1.FindElement(Locator locator) in E:\repos\Private\Iosk\Oravey\Brinell\srcnew\Brinell.Maui\Pages\MauiPageObjectBase.cs:line 144
   at Brinell.Maui.Controls.MauiControlBase`1.FindElement() in E:\repos\Private\Iosk\Oravey\Brinell\srcnew\Brinell.Maui\Controls\MauiControlBase.cs:line 121
   at Brinell.Maui.Controls.MauiControlBase`1.FindElementWithWait(Nullable`1 timeoutMs) in E:\repos\Private\Iosk\Oravey\Brinell\srcnew\Brinell.Maui\Controls\MauiControlBase.cs:line 148
   at Brinell.Maui.CommunityToolkit.Controls.TabViewControl`1.Click(Nullable`1 timeoutMs) in E:\repos\Private\Iosk\Oravey\Brinell\srcnew\Brinell.Maui.CommunityToolkit\Controls\TabViewControl.cs:line 58
   at Brinell.Maui.UITests.AppiumFixture.NavigateToContainerDemo() in E:\repos\Private\Iosk\Oravey\Brinell\testsnew\Brinell.Maui.UITests\AppiumFixture.cs:line 53
   at Brinell.Maui.UITests.Tests.ContainerScopingTests..ctor(AppiumFixture fixture) in E:\repos\Private\Iosk\Oravey\Brinell\testsnew\Brinell.Maui.UITests\Tests\ContainerScopingTests.cs:line 22
```

### Test Execution Results

**Test Run Date**: January 19, 2026, 10:09 AM

**ContainerScopingTests Results**: 0/9 passed (0%)
- Container_InvalidateCache_DoesNotBreak - FAILED
- OuterContainer_FindsNestedControlsViaInner - FAILED
- InnerContainer_DoesNotFindOuterControls - FAILED
- PageControls_AndContainerControls_Coexist - FAILED
- Container_ScopesSearchToItsRoot - FAILED
- ListItems_AreIndependentlyScoped - FAILED
- Containers_HaveDistinctControls - FAILED
- Containers_TextValues_AreScoped - FAILED
- IndexedContainers_AreIndependentlyScoped - FAILED

**Other Test Class Results (for comparison)**:
- MainPageTests: 15/16 passed (93.75%) - 1 test fails on "MainTab" not found
- ButtonControlTests: 12/12 passed (100%)
- EntryControlTests: 17/17 passed (100%)

### Steps to Reproduce

1. Start Appium server: `Start-Process cmd.exe -ArgumentList "/c","appium --address 127.0.0.1 --port 4723 --relaxed-security"`
2. Wait 3 seconds for server to start
3. Run tests: `cd testsnew\Brinell.Maui.UITests; dotnet test --filter "FullyQualifiedName~ContainerScopingTests" --logger:"console;verbosity=normal"`
4. **Expected**: Tests navigate to Containers tab and run container scoping tests
5. **Actual**: All tests fail immediately with "ContainersTab element not found"

## Environment

- **Version**: .NET 10.0.2, Brinell MAUI Framework (current master)
- **OS**: Windows 10 (10.0.19041.0)
- **Appium**: 3.1.2 with Windows driver
- **Test Framework**: xUnit 3.1.5
- **Sample App**: Brinell.Samples.Maui.App (bin\Debug\net10.0-windows10.0.19041.0\win-x64\)

## Root Cause Analysis

### Investigation Findings

**NOT YET INVESTIGATED** - See hypotheses below for investigation direction.

### Initial Hypotheses

**IMPORTANT**: Increasing timeout is NOT the solution - the issue is not timing-related.

| Hypothesis | Priority | Investigation Steps |
|------------|----------|-------------------|
| **H1: AutomationId mismatch** - The XAML control has a different AutomationId than expected | HIGH | 1. Inspect MainPage.xaml or AppShell.xaml in sample app<br>2. Search for "ContainersTab" automation ID<br>3. Check if tab exists with different name<br>4. Use UI Spy or Accessibility Insights to view actual automation IDs |
| **H2: Control type mismatch** - TabViewControl looking for wrong control type | HIGH | 1. Check TabViewControl implementation in Brinell.Maui.CommunityToolkit<br>2. Verify expected control type vs actual (TabViewItem vs FlyoutItem vs other)<br>3. Check if sample app uses different tab control type |
| **H3: Windows UIA not exposing AutomationId** - MAUI/Windows bug where AutomationId not exposed properly | MEDIUM | 1. Use Accessibility Insights to inspect running app<br>2. Check if ANY automation properties are exposed<br>3. Verify if other locator strategies work (XPath, Name, etc.)<br>4. Test with different MAUI versions |
| **H4: Sample app missing Containers page** - The UI structure doesn't exist in the sample app | MEDIUM | 1. Search sample app codebase for "Containers" references<br>2. Check if ContainerDemoPage exists<br>3. Verify AppShell or navigation structure includes this page<br>4. Run app manually and navigate to see if tab exists |
| **H5: TabView vs Shell navigation mismatch** - Using wrong page object pattern | LOW | 1. Check if sample app uses Shell-based navigation or TabView<br>2. Verify AppiumFixture.NavigateToContainerDemo() uses correct navigation approach<br>3. Compare with MainPageTests navigation (which mostly works) |

### Recommended Investigation Approach

1. **First**: Use Accessibility Insights or Inspect.exe to view the running sample app and see actual automation IDs
2. **Second**: Review sample app XAML to understand actual UI structure
3. **Third**: Compare working MainPageTests vs failing ContainerScopingTests to identify differences
4. **Fourth**: Check TabViewControl implementation for potential issues with locator strategy

### Affected Components

- `testsnew\Brinell.Maui.UITests\Tests\ContainerScopingTests.cs` - Test class (all 9 tests)
- `testsnew\Brinell.Maui.UITests\AppiumFixture.cs` - Line 53: NavigateToContainerDemo()
- `testsnew\Brinell.Maui.UITests\Pages\AppShellPage.cs` - Likely contains ContainersTab definition
- `samples\Brinell.Samples.Maui.App\` - Sample app XAML/structure
- `srcnew\Brinell.Maui.CommunityToolkit\Controls\TabViewControl.cs` - Line 58: Click() method

## Solution

### Approach

[To be determined after investigation]

### Implementation

[To be completed after root cause identified]

### Files Modified

[To be completed after fix implemented]

## Verification

### Test Steps

1. Run ContainerScopingTests: `dotnet test --filter "FullyQualifiedName~ContainerScopingTests"`
2. Verify all 9 tests pass
3. Verify MainPageTests.MainPage_NavigateToMainTab_ShowsControls also passes (related issue)

### Verified In

- [ ] Development environment
- [ ] CI/CD pipeline
- [ ] All test classes pass (full test run)

## Related Issues

- MainPageTests has 1 similar failure: "MainTab" element not found in `MainPage_NavigateToMainTab_ShowsControls` test
- Both issues involve tab navigation and element discovery
- May share root cause related to tab control automation IDs or Windows UIA implementation

## Learnings

[To be completed after resolution - focus on preventing similar issues]

Potential learnings to consider:
- Document expected AutomationId conventions for sample app controls
- Create diagnostic tool to list all available automation IDs in running app
- Add better error messages when controls not found (include available controls in error)
- Consider fallback locator strategies when AutomationId fails

## Notes

**Development Context**:
- This issue was discovered during systematic test execution on January 19, 2026
- 44/54 total tests passing (81.5%) before investigating remaining test classes
- Appium server working correctly (verified by successful test classes)
- Framework functionality verified through ButtonControlTests and EntryControlTests success

**User Guidance**:
1. Do NOT increase timeouts as first solution
2. Focus on control object definition and Windows automation ID implementation
3. Use UI inspection tools to understand actual vs expected structure
