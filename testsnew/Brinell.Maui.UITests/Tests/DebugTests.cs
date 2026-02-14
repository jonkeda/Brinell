using System.Diagnostics;
using Brinell.Core.Locators;
using Brinell.Maui.Interfaces;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// Debug tests to diagnose FlaUI element finding issues.
/// </summary>
[Collection("Appium")]
[Trait("Category", "Debug")]
public class DebugTests
{
    private readonly AppiumFixture _fixture;

    public DebugTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Dumps the automation tree to diagnose element finding issues.
    /// </summary>
    [Fact(Timeout = 120000)]
    public async Task Debug_DumpAutomationTree()
    {
        // Wait for app to fully load
        await Task.Delay(5000);
        
        // Get page source if driver supports it
        var driver = _fixture.Context.Driver;
        
        // Try to get page source via diagnostic interface
        if (driver is IDiagnosticDriver diagnosticDriver)
        {
            var pageSource = diagnosticDriver.GetPageSource();
            
            // Write to test output
            Console.WriteLine("=== AUTOMATION TREE ===");
            Console.WriteLine(pageSource);
            Console.WriteLine("=== END TREE ===");
            
            // Also save to file
            var outputPath = Path.Combine(Path.GetTempPath(), "automation-tree.xml");
            File.WriteAllText(outputPath, pageSource);
            Console.WriteLine($"Saved to: {outputPath}");
            
            // Assert that we got something
            Assert.False(string.IsNullOrEmpty(pageSource), "Page source should not be empty");
        }
        else
        {
            Console.WriteLine($"Driver type: {driver.GetType().FullName}");
            Console.WriteLine("Driver does not support IDiagnosticDriver");
        }
    }
    
    /// <summary>
    /// Tests finding the MainPage element directly.
    /// </summary>
    [Fact(Timeout = 120000)]
    public async Task Debug_FindMainPage()
    {
        // Wait for app to fully load
        await Task.Delay(5000);
        
        // Try to find MainPage by AutomationId
        var mainPageLocator = Locator.ByAutomationId("MainPage");
        
        var elements = _fixture.Context.FindElements(mainPageLocator);
        Console.WriteLine($"Found {elements.Count} elements with AutomationId=MainPage");
        
        foreach (var element in elements)
        {
            Console.WriteLine($"  - Element: {element.GetAttribute("AutomationId")}, Name: {element.GetAttribute("Name")}");
        }
        
        Assert.True(elements.Count > 0 || OperatingSystem.IsWindows(), "Should find MainPage element");
    }
    
    /// <summary>
    /// Tests finding any tab elements.
    /// </summary>
    [Fact(Timeout = 120000)]
    public async Task Debug_FindTabs()
    {
        // Wait for app to fully load
        await Task.Delay(5000);
        
        // Try to find by Name (which might be the tab title)
        var basicsLocator = Locator.ByName("Basics");
        
        var elements = _fixture.Context.FindElements(basicsLocator);
        Console.WriteLine($"Found {elements.Count} elements with Name=Basics");
        
        foreach (var element in elements)
        {
            Console.WriteLine($"  - AutomationId: {element.GetAttribute("AutomationId")}, Name: {element.GetAttribute("Name")}");
        }
        
        // Also try BasicsTab
        var basicsTabLocator = Locator.ByAutomationId("BasicsTab");
        var tabElements = _fixture.Context.FindElements(basicsTabLocator);
        Console.WriteLine($"Found {tabElements.Count} elements with AutomationId=BasicsTab");
    }
    
    /// <summary>
    /// Debug test to understand Switch visibility issue.
    /// </summary>
    [Fact(Timeout = 120000)]
    public async Task Debug_SwitchVisibility()
    {
        // Navigate to User Form
        _fixture.NavigateToUserForm();
        await Task.Delay(2000);
        
        // Try to find the NewsletterSwitch directly
        var switchLocator = Locator.ByAutomationId("NewsletterSwitch");
        var elements = _fixture.Context.FindElements(switchLocator);
        
        Console.WriteLine($"Found {elements.Count} elements with AutomationId=NewsletterSwitch");
        
        foreach (var element in elements)
        {
            // Use safe property access
            Console.WriteLine($"  - AutomationId: {element.GetAttribute("AutomationId")}");
            Console.WriteLine($"  - Name: {element.GetAttribute("Name")}");
            Console.WriteLine($"  - ControlType: {element.GetAttribute("ControlType")}");
            Console.WriteLine($"  - Visible property: {element.Visible}");
            Console.WriteLine($"  - Enabled property: {element.Enabled}");
            Console.WriteLine($"  - Location: {element.Location}");
            Console.WriteLine($"  - Size: {element.Size}");
            Console.WriteLine($"  - Rect: {element.Rect}");
        }
        
        // Also check what the control says
        var page = _fixture.UserFormPage;
        var isExists = page.NewsletterSwitch.IsExists();
        var isVisible = page.NewsletterSwitch.IsVisible();
        var isEnabled = page.NewsletterSwitch.IsEnabled();
        
        Console.WriteLine($"\nControl properties:");
        Console.WriteLine($"  - IsExists: {isExists}");
        Console.WriteLine($"  - IsVisible: {isVisible}");
        Console.WriteLine($"  - IsEnabled: {isEnabled}");
        
        Assert.True(elements.Count > 0, "Should find NewsletterSwitch element");
    }
}
