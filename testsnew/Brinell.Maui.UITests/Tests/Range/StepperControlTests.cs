using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Range;

/// <summary>
/// UI tests for MauiStepperControl verifying increment/decrement operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Stepper")]
public class StepperControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public StepperControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that stepper exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Stepper_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.QuantityStepper.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that stepper is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Stepper_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.QuantityStepper.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Value Tests

    /// <summary>
    /// Verifies GetValue() returns current stepper value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetValue")]
    public Task Stepper_GetValue_ReturnsCurrentValue()
    {
        // Act
        var value = Page.QuantityStepper.GetValue();

        // Assert - value should be within range 1-99
        Assert.True(value.HasValue && value.Value >= 1 && value.Value <= 99);
        return Task.CompletedTask;
    }

    #endregion

    #region Increment Tests

    /// <summary>
    /// Verifies Increment() increases value by step.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Increment")]
    public Task Stepper_Increment_IncreasesValue()
    {
        // Arrange
        Page.QuantityStepper.SetValue(5);
        var initialValue = Page.QuantityStepper.GetValue()!.Value;

        // Act
        Page.QuantityStepper.Increment();

        // Assert
        var newValue = Page.QuantityStepper.GetValue()!.Value;
        Assert.Equal(initialValue + 1, newValue);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies IncrementBy() increases value by specified amount.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IncrementBy")]
    public Task Stepper_IncrementBy_IncreasesValueByAmount()
    {
        // Arrange
        Page.QuantityStepper.SetValue(5);
        var initialValue = Page.QuantityStepper.GetValue()!.Value;

        // Act
        Page.QuantityStepper.IncrementBy(3);

        // Assert
        var newValue = Page.QuantityStepper.GetValue()!.Value;
        Assert.Equal(initialValue + 3, newValue);
        return Task.CompletedTask;
    }

    #endregion

    #region Decrement Tests

    /// <summary>
    /// Verifies Decrement() decreases value by step.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Decrement")]
    public Task Stepper_Decrement_DecreasesValue()
    {
        // Arrange
        Page.QuantityStepper.SetValue(10);
        var initialValue = Page.QuantityStepper.GetValue()!.Value;

        // Act
        Page.QuantityStepper.Decrement();

        // Assert
        var newValue = Page.QuantityStepper.GetValue()!.Value;
        Assert.Equal(initialValue - 1, newValue);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies DecrementBy() decreases value by specified amount.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "DecrementBy")]
    public Task Stepper_DecrementBy_DecreasesValueByAmount()
    {
        // Arrange
        Page.QuantityStepper.SetValue(10);
        var initialValue = Page.QuantityStepper.GetValue()!.Value;

        // Act
        Page.QuantityStepper.DecrementBy(3);

        // Assert
        var newValue = Page.QuantityStepper.GetValue()!.Value;
        Assert.Equal(initialValue - 3, newValue);
        return Task.CompletedTask;
    }

    #endregion

    #region Boundary Tests

    /// <summary>
    /// Verifies CanIncrement() returns false at maximum.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "CanIncrement")]
    public Task Stepper_CanIncrement_ReturnsFalseAtMax()
    {
        // Arrange - set to maximum (99)
        Page.QuantityStepper.SetValue(99);

        // Assert
        Assert.True(Page.QuantityStepper.CanIncrement() == false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies CanDecrement() returns false at minimum.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "CanDecrement")]
    public Task Stepper_CanDecrement_ReturnsFalseAtMin()
    {
        // Arrange - set to minimum (1)
        Page.QuantityStepper.SetValue(1);

        // Assert
        Assert.True(Page.QuantityStepper.CanDecrement() == false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies CanIncrement() and CanDecrement() return true in middle.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "CanIncrement")]
    public Task Stepper_CanIncrementDecrement_TrueInMiddle()
    {
        // Arrange - set to middle value
        Page.QuantityStepper.SetValue(50);

        // Assert
        Assert.True(Page.QuantityStepper.CanIncrement() == true);
        Assert.True(Page.QuantityStepper.CanDecrement() == true);
        return Task.CompletedTask;
    }

    #endregion
}
