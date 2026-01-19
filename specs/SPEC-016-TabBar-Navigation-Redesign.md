# SPEC-016: TabBar Navigation Redesign

**Status:** Draft  
**Created:** January 18, 2026  
**Related:** SPEC-015, SPEC-015b (Element Lookup Optimization)  
**Author:** Brinell Framework Team

---

## 1. Overview

This specification describes the redesign of the Brinell MAUI sample app from Shell FlyoutItem navigation to TabBar navigation, and the corresponding restructuring of UI tests to use page-per-content architecture.

### 1.1 Current Problems

1. **FlyoutItem locator issues** - XPath `@Name` strategy doesn't work reliably with Windows Appium driver
2. **Complex navigation** - Flyout requires opening menu, scrolling, and clicking items
3. **Slow tests** - Flyout navigation adds overhead to every test
4. **Monolithic test structure** - All tests use AppShell directly instead of dedicated page objects

### 1.2 Proposed Solution

1. **Replace Shell Flyout with TabBar** - Simpler, always-visible navigation
2. **Create TabControl** - New control object for tab interaction
3. **Page-per-content architecture** - Each content page gets its own test page object
4. **Dedicated test files** - One test file per content page

---

## 2. Sample App Changes

### 2.1 AppShell.xaml - From Flyout to TabBar

**Current Structure (Flyout):**
```xml
<Shell ...>
    <FlyoutItem Title="Main" Icon="home.png">
        <ShellContent ContentTemplate="{DataTemplate pages:MainPage}" />
    </FlyoutItem>
    <FlyoutItem Title="Dashboard" Icon="dashboard.png">
        <ShellContent ContentTemplate="{DataTemplate pages:DashboardPage}" />
    </FlyoutItem>
    <!-- 7 more FlyoutItems... -->
</Shell>
```

**New Structure (TabBar):**
```xml
<Shell ...>
    <TabBar>
        <Tab Title="Main" Icon="home.png">
            <ShellContent ContentTemplate="{DataTemplate pages:MainPage}" />
        </Tab>
        <Tab Title="Dashboard" Icon="dashboard.png">
            <ShellContent ContentTemplate="{DataTemplate pages:DashboardPage}" />
        </Tab>
        <Tab Title="Forms" Icon="form.png">
            <ShellContent ContentTemplate="{DataTemplate pages:UserFormPage}" />
        </Tab>
        <Tab Title="Data" Icon="grid.png">
            <ShellContent ContentTemplate="{DataTemplate pages:DataGridPage}" />
        </Tab>
        <Tab Title="Media" Icon="media.png">
            <ShellContent ContentTemplate="{DataTemplate pages:MediaGalleryPage}" />
        </Tab>
        <Tab Title="Navigation" Icon="navigation.png">
            <ShellContent ContentTemplate="{DataTemplate pages:NavigationPage}" />
        </Tab>
        <Tab Title="Validation" Icon="validation.png">
            <ShellContent ContentTemplate="{DataTemplate pages:ValidationPage}" />
        </Tab>
        <Tab Title="Advanced" Icon="advanced.png">
            <ShellContent ContentTemplate="{DataTemplate pages:AdvancedPage}" />
        </Tab>
        <Tab Title="Containers" Icon="container.png">
            <ShellContent ContentTemplate="{DataTemplate pages:ContainerDemoPage}" />
        </Tab>
    </TabBar>
</Shell>
```

### 2.2 Benefits of TabBar

| Aspect | Flyout | TabBar |
|--------|--------|--------|
| **Visibility** | Hidden, must open | Always visible |
| **Scrolling** | Required for 9+ items | All tabs shown (scrollable if needed) |
| **Locator** | XPath `@Name` (unreliable) | AccessibilityId (reliable) |
| **Click target** | Text-based | Button-based |
| **Test speed** | Slow (open + scroll + click) | Fast (direct click) |

---

## 3. Framework Control: MauiTabControl

### 3.1 Interface: ITabControlObject

```csharp
/// <summary>
/// Interface for tab controls allowing tab selection.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public interface ITabControlObject<TScope> : IClickableControlObject<TScope>
{
    /// <summary>
    /// Gets the title/text of the tab.
    /// </summary>
    string Title { get; }
    
    /// <summary>
    /// Checks if the tab is currently selected.
    /// </summary>
    /// <returns>True if selected, false if not, null if element not found.</returns>
    bool? IsSelected();
    
    /// <summary>
    /// Waits for the tab to be selected or unselected.
    /// </summary>
    /// <param name="expected">Expected selected state. Null skips the check.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    bool WaitSelected(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the tab is selected or unselected.
    /// </summary>
    /// <param name="expected">Expected selected state. Null skips the check.</param>
    /// <param name="message">Optional custom assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null);
}
```

### 3.2 Implementation: MauiTabControl

```csharp
namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Tab control for Shell TabBar navigation.
/// Tabs use AutomationId = Title of the Tab element.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiTabControl<TScope> : MauiControlBase<TScope>, ITabControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string _title;

    /// <summary>
    /// Creates a new tab control.
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="title">The Title of the Tab (becomes AutomationId).</param>
    public MauiTabControl(IMauiScope<TScope> scope, string title)
        : base(scope, Locator.ByAutomationId(title))
    {
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <inheritdoc />
    public string Title => _title;

    #region IClickableControlObject - Click to select tab

    /// <inheritdoc />
    public TScope Click(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Click), timeoutMs, element =>
        {
            ClickCore(element);
        });
    }

    /// <inheritdoc />
    public TScope DoubleClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(DoubleClick), timeoutMs, element =>
        {
            DoubleClickCore(element);
        });
    }

    /// <inheritdoc />
    public TScope RightClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(RightClick), timeoutMs, element =>
        {
            RightClickCore(element);
        });
    }

    /// <inheritdoc />
    public bool? IsClickable()
    {
        return IsClickableCore(TryFindElement());
    }

    /// <inheritdoc />
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null)
            return expected.Value == false;

        return WaitClickableCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertClickable), expected, () =>
        {
            WaitClickable(expected, timeoutMs);
            return IsClickable();
        }, message ?? $"Expected tab '{_title}' {(expected.Value ? "to be clickable" : "not to be clickable")}.");
    }

    #endregion

    #region Core Methods (Element-Aware)

    protected void ClickCore(IMauiElement element)
    {
        element.Click();
    }

    protected void DoubleClickCore(IMauiElement element)
    {
        element.Click();
        element.Click();
    }

    protected void RightClickCore(IMauiElement element)
    {
        var unwrappedElement = element.UnwrapElement();
        var unwrappedDriver = Context.Driver.UnwrapDriver();

        var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
        actions.ContextClick(unwrappedElement).Perform();
    }

    protected bool? IsClickableCore(IMauiElement? element)
    {
        var isVisible = IsVisibleCore(element);
        var isEnabled = IsEnabledCore(element);

        if (isVisible == null || isEnabled == null)
            return null;

        return isVisible.Value && isEnabled.Value;
    }

    protected bool WaitClickableCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(element, e => IsClickableCore(e) == expected, timeoutMs);
    }

    #endregion

    #region ITabControlObject - Selection State

    /// <inheritdoc />
    public bool? IsSelected()
    {
        return IsSelectedCore(TryFindElement());
    }

    protected bool? IsSelectedCore(IMauiElement? element)
    {
        if (element == null) return null;

        // For MAUI TabBar, check the Selected property or aria-selected attribute
        var selected = element.GetAttribute("Selected") 
                    ?? element.GetAttribute("IsSelected")
                    ?? element.GetAttribute("aria-selected");

        if (selected != null)
            return selected.Equals("true", StringComparison.OrdinalIgnoreCase);

        // Fallback: check if element has "selected" in class/state
        var className = element.GetAttribute("class") ?? "";
        return className.Contains("selected", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool WaitSelected(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null)
            return expected.Value == false;

        return WaitSelectedCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    protected bool WaitSelectedCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(element, e => IsSelectedCore(e) == expected, timeoutMs);
    }

    /// <inheritdoc />
    public TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertSelected), expected, () =>
        {
            WaitSelected(expected, timeoutMs);
            return IsSelected();
        }, message ?? $"Expected tab '{_title}' {(expected.Value ? "to be selected" : "not to be selected")}.");
    }

    #endregion
}
```

---

## 4. Test Architecture Redesign

### 4.1 Current Structure (Monolithic)

```
AppiumFixture
  └─ AppShellPage
       ├─ MainFlyout.Click() → MainPage content
       ├─ DashboardFlyout.Click() → DashboardPage content
       └─ ... (all navigation through shell)

Tests access controls directly on AppShell:
- Shell.NameEntry.Enter("test")
- Shell.GreetButton.Click()
```

**Problems:**
- No separation of concerns
- All controls mixed on one page object
- Hard to maintain as app grows
- No clear page boundaries

### 4.2 New Structure (Page-per-Content)

```
AppiumFixture
  └─ AppShellPage
       ├─ MainTab.Click() → MainPage
       ├─ DashboardTab.Click() → DashboardPage
       ├─ FormsTab.Click() → UserFormPage
       └─ ... (navigation only)

Dedicated Page Objects:
  ├─ MainPage (greeting controls)
  ├─ DashboardPage (dashboard widgets)
  ├─ UserFormPage (form controls)
  ├─ DataGridPage (grid controls)
  ├─ MediaGalleryPage (media controls)
  ├─ NavigationPage (navigation demo)
  ├─ ValidationPage (validation controls)
  ├─ AdvancedPage (advanced controls)
  └─ ContainerDemoPage (container controls)

Tests navigate then use dedicated page:
- Shell.MainTab.Click()
- MainPage.NameEntry.Enter("test")
- MainPage.GreetButton.Click()
```

**Benefits:**
- Clear separation of concerns
- Each page object manages its own controls
- Easy to maintain and extend
- Follows Page Object Model pattern
- Test files organized by feature

---

## 5. Implementation Plan

### Phase 1: Sample App Changes

**Files to modify:**
1. `samples/Brinell.Samples.Maui.App/AppShell.xaml`
   - Replace `<FlyoutItem>` with `<TabBar>` and `<Tab>`
   - Ensure Tab `Title` becomes `AutomationId`

2. `samples/Brinell.Samples.Maui.App/AppShell.xaml.cs`
   - Remove flyout-related code if any
   - No significant changes needed

**Expected result:** App uses TabBar navigation, all 9 pages accessible via tabs

### Phase 2: Framework Control

**Files to create:**
1. `srcnew/Brinell.Core/Abstractions/Controls/ITabControlObject.cs`
   - Interface definition with Click, IsSelected, WaitSelected, AssertSelected

2. `srcnew/Brinell.Maui/Controls/MauiTabControl.cs`
   - Implementation using AutomationId locator
   - Core methods following SPEC-015b pattern

**Expected result:** MauiTabControl available for test page objects

### Phase 3: Test Page Objects

**Files to create:**
1. `testsnew/Brinell.Maui.UITests/Pages/MainPage.cs`
   - Controls: NameEntry, GreetButton, GreetingLabel, ValidationLabel
   - Inherits from `MauiPageObjectBase<MainPage>`

2. `testsnew/Brinell.Maui.UITests/Pages/DashboardPage.cs`
   - Controls: (dashboard-specific controls)
   - Inherits from `MauiPageObjectBase<DashboardPage>`

3. `testsnew/Brinell.Maui.UITests/Pages/UserFormPage.cs`
   - Controls: (form-specific controls)

4. `testsnew/Brinell.Maui.UITests/Pages/DataGridPage.cs`
5. `testsnew/Brinell.Maui.UITests/Pages/MediaGalleryPage.cs`
6. `testsnew/Brinell.Maui.UITests/Pages/NavigationPage.cs`
7. `testsnew/Brinell.Maui.UITests/Pages/ValidationPage.cs`
8. `testsnew/Brinell.Maui.UITests/Pages/AdvancedPage.cs`
9. `testsnew/Brinell.Maui.UITests/Pages/ContainerDemoPage.cs`

**Files to modify:**
1. `testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs`
   - Replace flyout controls with tab controls
   - Remove ScrollFlyout methods
   - Keep minimal - just navigation

2. `testsnew/Brinell.Maui.UITests/AppiumFixture.cs`
   - Add properties for all page objects
   - Initialize pages in constructor

**Expected result:** Clean page object hierarchy with separation of concerns

### Phase 4: Test Restructuring

**Files to create:**
1. `testsnew/Brinell.Maui.UITests/Tests/MainPageTests.cs`
   - Move greeting tests from EntryControlTests
   - Use MainPage object

2. `testsnew/Brinell.Maui.UITests/Tests/DashboardPageTests.cs`
3. `testsnew/Brinell.Maui.UITests/Tests/UserFormPageTests.cs`
4. `testsnew/Brinell.Maui.UITests/Tests/DataGridPageTests.cs`
5. `testsnew/Brinell.Maui.UITests/Tests/MediaGalleryPageTests.cs`
6. `testsnew/Brinell.Maui.UITests/Tests/NavigationPageTests.cs`
7. `testsnew/Brinell.Maui.UITests/Tests/ValidationPageTests.cs`
8. `testsnew/Brinell.Maui.UITests/Tests/AdvancedPageTests.cs`

**Files to modify:**
1. `testsnew/Brinell.Maui.UITests/Tests/ContainerDemoPageTests.cs` (rename from ContainerDemoTests)
   - Update to use ContainerDemoPage object
   - Navigate via tab instead of flyout

**Files to delete:**
1. `testsnew/Brinell.Maui.UITests/Tests/FlyoutItemControlTests.cs`
   - No longer needed with TabBar

2. Consider removing/restructuring:
   - `ButtonControlTests.cs` - Move tests to relevant page tests
   - `EntryControlTests.cs` - Move tests to relevant page tests

**Expected result:** Tests organized by page/feature, easy to navigate and maintain

### Phase 5: TabControl Tests

**Files to create:**
1. `testsnew/Brinell.Maui.UITests/Tests/TabControlTests.cs`
   - Test tab clicking
   - Test tab selection state
   - Test tab visibility/enabled
   - Test navigation between tabs

**Expected result:** Comprehensive tab control test coverage

---

## 6. Test Example Comparison

### 6.1 Before (Current - Flyout)

```csharp
[Fact]
public void Entry_EnterNameAndGreet_ShowsGreetingMessage()
{
    // Arrange
    var shell = _fixture.AppShell;
    
    // Act - all on shell page
    shell.NameEntry.SetText("Alice");
    shell.GreetButton.Click();
    
    // Assert
    shell.GreetingLabel.AssertTextContains("Hello, Alice!");
}
```

### 6.2 After (New - TabBar + Page Objects)

```csharp
[Fact]
public void EnterNameAndGreet_ShowsGreetingMessage()
{
    // Arrange - navigate to Main page
    _fixture.AppShell.MainTab.Click();
    var mainPage = _fixture.MainPage;
    mainPage.WaitReady();
    
    // Act - use dedicated page object
    mainPage.NameEntry.SetText("Alice");
    mainPage.GreetButton.Click();
    
    // Assert
    mainPage.GreetingLabel.AssertTextContains("Hello, Alice!");
}
```

**Benefits:**
- Clear navigation step
- Explicit page object usage
- Better test organization
- Easier to maintain

---

## 7. Migration Strategy

### 7.1 Incremental Approach

1. ✅ **Phase 1**: Sample app (TabBar)
2. ✅ **Phase 2**: Framework control (MauiTabControl)
3. ✅ **Phase 3**: AppShellPage + MainPage only
4. ✅ **Phase 4**: MainPageTests working
5. 🔄 **Phase 5**: Add remaining pages one at a time
6. 🔄 **Phase 6**: Migrate remaining tests
7. 🔄 **Phase 7**: Delete old flyout tests

### 7.2 Validation Steps

After each phase:
1. Build succeeds
2. Existing tests still pass (or are updated)
3. New tests pass
4. No regressions in test execution time

---

## 8. Success Criteria

1. ✅ Sample app uses TabBar navigation
2. ✅ All 9 pages accessible via tabs
3. ✅ MauiTabControl implemented with Core methods (SPEC-015b pattern)
4. ✅ 9 dedicated page objects (one per content page)
5. ✅ Tests organized by page/feature
6. ✅ AppShellPage simplified (navigation only)
7. ✅ FlyoutItemControlTests deleted
8. ✅ All tests pass
9. ✅ Test execution time improved (no flyout overhead)

---

## 9. Expected Performance Improvements

| Metric | Before (Flyout) | After (TabBar) | Improvement |
|--------|-----------------|----------------|-------------|
| **Navigation time** | ~2-3s (open + scroll + click) | ~0.5s (click) | 4-6x faster |
| **Test reliability** | Low (XPath issues) | High (AutomationId) | Stable |
| **Flyout scroll calls** | 3-5 per test | 0 | Eliminated |
| **Element lookups** | XPath (slow) | AutomationId (fast) | 2-3x faster |
| **Overall test suite** | ~15-20 min | ~10-12 min | 30-40% faster |

---

## 10. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Tab AutomationId not set | High | Ensure Tab Title becomes AutomationId |
| Too many tabs (9+) | Medium | Use scrollable TabBar if needed |
| Breaking existing tests | High | Incremental migration, keep tests passing |
| Page object complexity | Medium | Start simple, add complexity as needed |

---

## 11. Future Enhancements

1. **Tab groups** - Group related tabs (Forms, Data, Media)
2. **Dynamic tabs** - Add/remove tabs programmatically
3. **Tab icons** - Verify icon rendering in tests
4. **Tab badges** - Test notification badges on tabs
5. **Keyboard navigation** - Test tab navigation with keyboard

---

## 12. References

- [SPEC-015: Element Lookup Optimization](./SPEC-015-Element-Lookup-Optimization.md)
- [SPEC-015b: Element Lookup Optimization Phase 2](./SPEC-015b-Element-Lookup-Optimization-Phase2.md)
- [MAUI Shell TabBar Documentation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/tabs)
- [Page Object Model Pattern](https://martinfowler.com/bliki/PageObject.html)

---

**Revision History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-18 | Brinell Team | Initial draft |
