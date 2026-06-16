using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Selection;

/// <summary>
/// UI tests for Picker verifying selection operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Picker")]
public class PickerControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public PickerControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that picker exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Picker_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.CountryPicker.IsExists());
        Assert.True(Page.DepartmentPicker.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that picker is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Picker_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.CountryPicker.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Selection Tests

    /// <summary>
    /// Verifies SelectByIndex() selects item at index.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SelectByIndex")]
    public Task Picker_SelectByIndex_SelectsItem()
    {
        // Act
        Page.CountryPicker.SelectByIndex(0);

        // Assert
        var selectedIndex = Page.CountryPicker.GetSelectedIndex();
        Assert.Equal(0, selectedIndex);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies SelectByText() selects item by text.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "SelectByText")]
    public Task Picker_SelectByText_SelectsItem()
    {
        // Act - select by text
        Page.CountryPicker.SelectByText("United States");

        // Assert
        var selectedText = Page.CountryPicker.GetSelectedText();
        Assert.Equal("United States", selectedText);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies GetSelectedIndex() returns current selection.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetSelectedIndex")]
    public Task Picker_GetSelectedIndex_ReturnsIndex()
    {
        // Arrange
        Page.CountryPicker.SelectByIndex(1);

        // Act
        var selectedIndex = Page.CountryPicker.GetSelectedIndex();

        // Assert
        Assert.Equal(1, selectedIndex);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies GetSelectedText() returns selected item text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetSelectedText")]
    public Task Picker_GetSelectedText_ReturnsText()
    {
        // Arrange
        Page.CountryPicker.SelectByIndex(0);

        // Act
        var selectedText = Page.CountryPicker.GetSelectedText();

        // Assert
        Assert.False(string.IsNullOrEmpty(selectedText));
        return Task.CompletedTask;
    }

    #endregion

    #region Multiple Picker Tests

    /// <summary>
    /// Verifies multiple pickers operate independently.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "MultipleControls")]
    public Task Picker_MultipleControls_OperateIndependently()
    {
        // Act
        Page.CountryPicker.SelectByIndex(0);
        Page.DepartmentPicker.SelectByIndex(1);

        // Assert
        Assert.Equal(0, Page.CountryPicker.GetSelectedIndex());
        Assert.Equal(1, Page.DepartmentPicker.GetSelectedIndex());
        return Task.CompletedTask;
    }

    #endregion
}
