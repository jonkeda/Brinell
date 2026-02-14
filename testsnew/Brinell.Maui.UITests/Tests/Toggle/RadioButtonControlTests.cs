using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Toggle;

/// <summary>
/// UI tests for RadioButton verifying selection operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "RadioButton")]
public class RadioButtonControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public RadioButtonControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that radio buttons exist on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task RadioButton_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.BasicRadio.IsExists());
        Assert.True(Page.ProfessionalRadio.IsExists());
        Assert.True(Page.EnterpriseRadio.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that radio buttons are visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task RadioButton_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.BasicRadio.IsVisible() == true);
        Assert.True(Page.ProfessionalRadio.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Selection Tests

    /// <summary>
    /// Verifies IsSelected returns correct state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsSelected")]
    public Task RadioButton_IsSelected_ReturnsCorrectState()
    {
        // Act & Assert - verify we can query state
        var basicSelected = Page.BasicRadio.IsSelected();
        var professionalSelected = Page.ProfessionalRadio.IsSelected();
        var enterpriseSelected = Page.EnterpriseRadio.IsSelected();
        
        // At most one should be selected in a radio group
        var selectedCount = (basicSelected == true ? 1 : 0) + (professionalSelected == true ? 1 : 0) + (enterpriseSelected == true ? 1 : 0);
        Assert.True(selectedCount <= 1);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Select() selects the radio button.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Select")]
    public Task RadioButton_Select_SelectsTheButton()
    {
        // Act
        Page.BasicRadio.Select();

        // Assert
        Assert.True(Page.BasicRadio.IsSelected() == true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies selecting one radio button deselects others in the group.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "MutualExclusion")]
    public Task RadioButton_Select_DeselectsOthersInGroup()
    {
        // Arrange - select Basic first
        Page.BasicRadio.Select();
        Assert.True(Page.BasicRadio.IsSelected() == true);

        // Act - select Professional
        Page.ProfessionalRadio.Select();

        // Assert - Professional selected, Basic deselected
        Assert.True(Page.ProfessionalRadio.IsSelected() == true);
        Assert.True(Page.BasicRadio.IsSelected() == false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies can select Enterprise tier.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Select")]
    public Task RadioButton_SelectEnterprise_Works()
    {
        // Act
        Page.EnterpriseRadio.Select();

        // Assert
        Assert.True(Page.EnterpriseRadio.IsSelected() == true);
        Assert.True(Page.BasicRadio.IsSelected() == false);
        Assert.True(Page.ProfessionalRadio.IsSelected() == false);
        return Task.CompletedTask;
    }

    #endregion

    #region Assertion Tests

    /// <summary>
    /// Verifies AssertSelected passes when selected.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertSelected")]
    public Task RadioButton_AssertSelected_PassesWhenSelected()
    {
        // Arrange
        Page.BasicRadio.Select();

        // Assert - no exception means success
        Page.BasicRadio.AssertSelected();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies AssertNotSelected passes when not selected.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertNotSelected")]
    public Task RadioButton_AssertNotSelected_PassesWhenNotSelected()
    {
        // Arrange - select a different one
        Page.ProfessionalRadio.Select();

        // Assert - no exception means success
        Page.BasicRadio.AssertNotSelected();
        return Task.CompletedTask;
    }

    #endregion
    
    #region Diagnostic Tests
    
    /// <summary>
    /// Diagnostic test to understand RadioButton attributes.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Category", "Diagnostic")]
    public Task RadioButton_Diagnostic_DumpAttributes()
    {
        var driver = _fixture.Context.Driver;
        
        // Dump page source around BasicRadio
        var pageSource = driver.GetPageSource();
        var basicIndex = pageSource.IndexOf("BasicRadio");
        if (basicIndex >= 0)
        {
            var start = Math.Max(0, basicIndex - 50);
            var end = Math.Min(pageSource.Length, basicIndex + 500);
            Console.WriteLine($"=== RadioButton XML Context ===");
            Console.WriteLine(pageSource.Substring(start, end - start));
        }
        
        // Try to find the element by AutomationId
        try
        {
            var element = driver.FindElement(Brinell.Core.Locators.Locator.ByAutomationId("BasicRadio"));
            Console.WriteLine($"=== Found by AutomationId ===");
            Console.WriteLine($"TagName: {element.TagName}");
            Console.WriteLine($"Text: {element.Text}");
            Console.WriteLine($"Enabled: {element.Enabled}");
            Console.WriteLine($"Selected: {element.Selected}");
            Console.WriteLine($"Visible: {element.Visible}");
            Console.WriteLine($"Location: {element.Location}");
            Console.WriteLine($"Size: {element.Size}");
            
            // Try each attribute we care about
            var attrs = new[] { "IsSelected", "SelectionItem.IsSelected", "ToggleState", 
                              "Toggle.ToggleState", "IsOffscreen", "IsAvailable", "Value.Value",
                              "BoundingRectangle", "x", "y", "width", "height" };
            foreach (var attr in attrs)
            {
                try
                {
                    var value = element.GetAttribute(attr);
                    Console.WriteLine($"Attr[{attr}]: {value ?? "(null)"}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attr[{attr}]: ERROR - {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Find by AutomationId failed: {ex.Message}");
        }
        
        // Test our wrapper's IsSelected
        Console.WriteLine($"\n=== Brinell Wrapper Results ===");
        Console.WriteLine($"Page.BasicRadio.IsSelected(): {Page.BasicRadio.IsSelected()}");
        Console.WriteLine($"Page.BasicRadio.IsChecked(): {Page.BasicRadio.IsChecked()}");
        Console.WriteLine($"Page.BasicRadio.IsExists(): {Page.BasicRadio.IsExists()}");
        Console.WriteLine($"Page.BasicRadio.IsVisible(): {Page.BasicRadio.IsVisible()}");
        
        return Task.CompletedTask;
    }
    
    #endregion
}
