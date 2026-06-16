using Brinell.Maui.UITests.Pages;
using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.DateTime;

/// <summary>
/// Diagnostic tests to understand Windows MAUI TimePicker automation tree structure.
/// These tests dump element information to help debug value extraction.
/// </summary>
[Collection("Appium")]
[Trait("Category", "Diagnostic")]
public class TimePickerDiagnosticTests
{
    private readonly AppiumFixture _fixture;
    private readonly ITestOutputHelper _output;
    private UserFormPage Page => _fixture.UserFormPage;

    public TimePickerDiagnosticTests(AppiumFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.NavigateToUserForm();
    }

    /// <summary>
    /// Dumps all attributes and child elements of the TimePicker to diagnose value extraction.
    /// </summary>
    [Fact(Timeout = 60000)]
    [Trait("Method", "Diagnostic")]
    public Task TimePicker_DumpElementStructure()
    {
        // Use the driver interface to find the element
        var driver = _fixture.Context.Driver;
        
        // Find the TimePicker element
        if (!driver.TryFindElement(Locator.ByName("PreferredTimePicker"), out var element))
        {
            if (!driver.TryFindElement(Locator.ByAutomationId("PreferredTimePicker"), out element))
            {
                _output.WriteLine("PreferredTimePicker not found by Name or AutomationId.");
                Assert.True(true, "Diagnostic completed without direct TimePicker match");
                return Task.CompletedTask;
            }
        }
        
        _output.WriteLine("=== TimePicker Element Found ===");
        DumpElement(element!, 0);
        
        // Dump children
        _output.WriteLine("\n=== Child Elements ===");
        try
        {
            var children = element!.FindElements(Locator.ByAutomationId("FlyoutButton"), 0);
            _output.WriteLine($"Found {children.Count} child elements via FlyoutButton lookup");
            foreach (var child in children.Take(5))
            {
                DumpElement(child, 1);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error finding children: {ex.Message}");
        }
        
        // Always pass - this is diagnostic
        Assert.True(true, "Diagnostic test completed - check output");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dumps full page source to understand available elements.
    /// </summary>
    [Fact(Timeout = 60000)]
    [Trait("Method", "Diagnostic")]
    public Task TimePicker_DumpPageSource()
    {
        var driver = _fixture.Context.Driver;
        var pageSource = driver.GetPageSource();
        
        // Find TimePicker-related content
        var lines = pageSource.Split('\n');
        var timePickerLines = lines
            .Select((line, idx) => (line, idx))
            .Where(x => x.line.Contains("TimePicker", StringComparison.OrdinalIgnoreCase) 
                     || x.line.Contains("PreferredTime", StringComparison.OrdinalIgnoreCase)
                     || x.line.Contains("9:00", StringComparison.OrdinalIgnoreCase)
                     || x.line.Contains("AM", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        _output.WriteLine($"=== Page Source Lines Containing TimePicker/Time ===");
        _output.WriteLine($"Found {timePickerLines.Count} relevant lines");
        
        foreach (var (line, idx) in timePickerLines.Take(50))
        {
            _output.WriteLine($"[{idx}] {line.Trim()}");
        }
        
        // Also output first 5000 chars of page source
        _output.WriteLine("\n=== First 5000 chars of PageSource ===");
        _output.WriteLine(pageSource.Substring(0, Math.Min(5000, pageSource.Length)));
        
        Assert.True(true, "Diagnostic test completed - check output");
        return Task.CompletedTask;
    }

    private void DumpElement(Brinell.Maui.Interfaces.IMauiElement element, int indent)
    {
        var prefix = new string(' ', indent * 2);
        
        try
        {
            _output.WriteLine($"{prefix}--- Element ---");
            _output.WriteLine($"{prefix}TagName: {SafeGet(() => element.TagName)}");
            _output.WriteLine($"{prefix}Text: '{SafeGet(() => element.Text)}'");
            _output.WriteLine($"{prefix}Visible: {SafeGet(() => element.Visible.ToString())}");
            _output.WriteLine($"{prefix}Location: {SafeGet(() => element.Location.ToString())}");
            _output.WriteLine($"{prefix}Size: {SafeGet(() => element.Size.ToString())}");
            
            // Common Windows UI Automation attributes
            var attributes = new[]
            {
                "Name", "AutomationId", "ClassName", "ControlType",
                "Value", "Time", "SelectedTime", "value.value",
                "LocalizedControlType", "IsEnabled", "IsOffscreen",
                "HelpText", "ItemType", "ItemStatus",
                "RangeValue.Value", "RangeValue.Minimum", "RangeValue.Maximum",
                "Selection.Selection", "Selection.Item.Name"
            };
            
            foreach (var attr in attributes)
            {
                var value = SafeGet(() => element.GetAttribute(attr));
                if (!string.IsNullOrEmpty(value))
                {
                    _output.WriteLine($"{prefix}{attr}: '{value}'");
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"{prefix}Error dumping element: {ex.Message}");
        }
    }

    private string SafeGet(Func<string?> getter)
    {
        try
        {
            return getter() ?? "(null)";
        }
        catch
        {
            return "(error)";
        }
    }
}
