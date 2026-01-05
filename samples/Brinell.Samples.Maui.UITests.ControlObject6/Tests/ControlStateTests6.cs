using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Control state tests using ControlObject6 API.
/// Tests IsExists, IsVisible, IsEnabled, Wait, and Assert methods.
/// </summary>
public class ControlStateTests6 : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;

    public ControlStateTests6(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
    }

    #region Existence Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_IsExists_ReturnsTrueForExistingControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert
        Assert.True(_mainPage.IncrementButton.IsExists());
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_WaitExists_WaitsForControlToExist()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        var result = _mainPage.IncrementButton.WaitExists(true, 5000);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_AssertExists_PassesForExistingControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert - should not throw
        _mainPage.IncrementButton.AssertExists(true);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_CheckExists_PassesForExistingControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert - should not throw
        _mainPage.IncrementButton.CheckExists(true);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_IsVisible_ReturnsTrueForVisibleControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert
        Assert.True(_mainPage.IncrementButton.IsVisible());
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_WaitVisible_WaitsForControlToBeVisible()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        var result = _mainPage.CounterLabel.WaitVisible(true, 5000);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_AssertVisible_PassesForVisibleControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert - should not throw
        _mainPage.TitleLabel.AssertVisible(true);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_CheckVisible_PassesForVisibleControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert - should not throw
        _mainPage.CounterLabel.CheckVisible(true);
    }

    #endregion

    #region Enabled Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_IsEnabled_ReturnsTrueForEnabledControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert
        Assert.True(_mainPage.IncrementButton.IsEnabled());
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_WaitEnabled_WaitsForControlToBeEnabled()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        var result = _mainPage.GreetButton.WaitEnabled(true, 5000);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_AssertEnabled_PassesForEnabledControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert - should not throw
        _mainPage.ResetButton.AssertEnabled(true);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public void Control_CheckEnabled_PassesForEnabledControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert - should not throw
        _mainPage.DecrementButton.CheckEnabled(true);
    }

    #endregion

    #region Nullable Expected Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P1")]
    public void WaitExists_NullExpected_ReturnsImmediately()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act - null expected should return immediately (true)
        var result = _mainPage.IncrementButton.WaitExists(null, 100);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P1")]
    public void WaitVisible_NullExpected_ReturnsImmediately()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act - null expected should return immediately (true)
        var result = _mainPage.IncrementButton.WaitVisible(null, 100);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P1")]
    public void WaitEnabled_NullExpected_ReturnsImmediately()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act - null expected should return immediately (true)
        var result = _mainPage.IncrementButton.WaitEnabled(null, 100);

        // Assert
        Assert.True(result);
    }

    #endregion
}
