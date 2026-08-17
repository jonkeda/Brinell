using Brinell.Maui.UITests.Pages2;
using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.Navigation;

/// <summary>
/// UI tests for TabbedPage tab navigation in the MAUI sample app.
/// Tests verify that tabs can be located and clicked using the fallback locator strategy.
/// </summary>
/// <remarks>
/// These tests validate the fix for dotnet/maui#3996 where TabbedPage tabs don't expose
/// AutomationId on Windows. The TabViewControl uses Name-based XPath fallback when
/// AutomationId is unavailable.
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Feature", "TabbedPage")]
public class TabbedPageTests
{
    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;
    private AppShellPage AppShell => _fixture.AppShell;
    private MainPage MainPage => _fixture.MainPage;
    private ContainerDemoPage ContainerDemoPage => _fixture.ContainerDemoPage;

    public TabbedPageTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Tab Navigation Tests

    /// <summary>
    /// Verifies that the Containers tab can be clicked and ContainerDemoPage loads.
    /// This is the primary test for the SPEC-023 fix.
    /// </summary>
    [Fact]
    [Trait("Tab", "Containers")]
    public void TabbedPage_NavigateToContainersTab_Success()
    {
        // Act - click Containers tab
        AppShell.ContainersTab.Click();

        // Assert - verify ContainerDemoPage loaded
        Assert.True(ContainerDemoPage.WaitReady(5000), 
            "ContainerDemoPage should be ready after clicking ContainersTab");
    }

    /// <summary>
    /// Verifies that the Basics tab (first tab) is accessible.
    /// </summary>
    [Fact]
    [Trait("Tab", "Basics")]
    public void TabbedPage_NavigateToBasicsTab_Success()
    {
        // Act - click Basics tab
        AppShell.BasicsTab.Click();

        // Assert - verify MainPage content accessible
        Assert.True(MainPage.WaitReady(5000),
            "MainPage should be ready after clicking BasicsTab");
    }

    /// <summary>
    /// Verifies that all 8 tabs exist and are clickable.
    /// </summary>
    [Fact]
    [Trait("Feature", "AllTabs")]
    public void TabbedPage_AllTabs_Accessible()
    {
        // Test each tab exists
        Assert.True(AppShell.BasicsTab.WaitExists(true, 5000), "BasicsTab should exist");
        Assert.True(AppShell.ContainersTab.WaitExists(true, 5000), "ContainersTab should exist");
        Assert.True(AppShell.FormsTab.WaitExists(true, 5000), "FormsTab should exist");
        Assert.True(AppShell.ListsTab.WaitExists(true, 5000), "ListsTab should exist");
        Assert.True(AppShell.GesturesTab.WaitExists(true, 5000), "GesturesTab should exist");
        Assert.True(AppShell.NavigationTab.WaitExists(true, 5000), "NavigationTab should exist");
        Assert.True(AppShell.ToolkitTab.WaitExists(true, 5000), "ToolkitTab should exist");
        Assert.True(AppShell.MediaTab.WaitExists(true, 5000), "MediaTab should exist");
    }

    /// <summary>
    /// Verifies navigation between multiple tabs.
    /// </summary>
    [Fact]
    [Trait("Feature", "TabSwitching")]
    public void TabbedPage_SwitchBetweenTabs_Success()
    {
        // Navigate to Containers
        AppShell.ContainersTab.Click();
        Assert.True(ContainerDemoPage.WaitReady(5000), "ContainerDemoPage should load");

        // Navigate back to Basics
        AppShell.BasicsTab.Click();
        Assert.True(MainPage.WaitReady(5000), "MainPage should load");

        // Navigate to Forms (no page object, just verify no exception)
        AppShell.FormsTab.Click();
        
        // Small wait to ensure tab switch completes
        AppShell.FormsTab.WaitExists(true, 2000);
    }

    #endregion

    #region Diagnostic Tests

    /// <summary>
    /// Diagnostic test that dumps tab element information for debugging.
    /// </summary>
    [Fact]
    [Trait("Category", "Diagnostic")]
    public void TabbedPage_DumpTabElements_ForDebugging()
    {
        // Check each tab and log its state
        var tabs = new (string name, Func<bool?> exists)[]
        {
            ("BasicsTab", () => AppShell.BasicsTab.IsExists()),
            ("ContainersTab", () => AppShell.ContainersTab.IsExists()),
            ("FormsTab", () => AppShell.FormsTab.IsExists()),
            ("ListsTab", () => AppShell.ListsTab.IsExists()),
            ("GesturesTab", () => AppShell.GesturesTab.IsExists()),
            ("NavigationTab", () => AppShell.NavigationTab.IsExists()),
            ("ToolkitTab", () => AppShell.ToolkitTab.IsExists()),
            ("MediaTab", () => AppShell.MediaTab.IsExists()),
        };

        _output.WriteLine("=== Tab Element Status ===");
        foreach (var (name, exists) in tabs)
        {
            var status = exists() == true ? "FOUND" : "NOT FOUND";
            _output.WriteLine($"  {name}: {status}");
        }
        _output.WriteLine("===========================");

        // At least one tab should exist
        Assert.True(tabs.Any(t => t.exists() == true), 
            "At least one tab should be found - check Appium connection and app state");
    }

    /// <summary>
    /// Diagnostic test that dumps page source to understand element structure.
    /// </summary>
    [Fact]
    [Trait("Category", "Diagnostic")]
    public void TabbedPage_DumpPageSource_ForDebugging()
    {
        // Get page source
        var pageSource = _fixture.Context.Driver.GetPageSource();
        
        // Write to output (will be in test results)
        _output.WriteLine("=== Page Source (first 10000 chars) ===");
        _output.WriteLine(pageSource.Length > 10000 ? pageSource[..10000] : pageSource);
        _output.WriteLine("=== End Page Source ===");
        
        // Look for NavigationViewItem elements
        _output.WriteLine("\n=== NavigationViewItem Search ===");
        if (pageSource.Contains("NavigationViewItem"))
        {
            _output.WriteLine("Found NavigationViewItem elements in page source");
        }
        else
        {
            _output.WriteLine("No NavigationViewItem elements found");
        }
        
        // Look for common tab-related patterns
        var searchTerms = new[] { "Basics", "Containers", "Forms", "TabViewItem", "Tab", "NavigationView", "Name=" };
        foreach (var term in searchTerms)
        {
            var count = pageSource.Split(term).Length - 1;
            _output.WriteLine($"  '{term}' occurrences: {count}");
        }
        
        // This test always passes - it's for diagnostics
        Assert.True(true, "Diagnostic test completed");
    }

    #endregion
}
