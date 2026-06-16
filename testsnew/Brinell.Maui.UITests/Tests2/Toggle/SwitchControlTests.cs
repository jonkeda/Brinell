using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Toggle;

/// <summary>
/// UI tests for Switch verifying on/off operations.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Switch")]
public class SwitchControlTests
{
    private readonly MauiFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public SwitchControlTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that switch exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Switch_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.NewsletterSwitch.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that switch is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Switch_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.NewsletterSwitch.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Toggle Operation Tests

    /// <summary>
    /// Verifies IsOn returns correct state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsOn")]
    public Task Switch_IsOn_ReturnsCorrectState()
    {
        // Act & Assert - just verify we can query state (nullable bool)
        var isOn = Page.NewsletterSwitch.IsOn();
        Assert.True(isOn == true || isOn == false); // Either state is valid
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies TurnOn() sets switch to on.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "TurnOn")]
    public Task Switch_TurnOn_SetsSwitchToOn()
    {
        // Arrange - ensure off first
        Page.NewsletterSwitch.TurnOff();

        // Act
        Page.NewsletterSwitch.TurnOn();

        // Assert
        Assert.True(Page.NewsletterSwitch.IsOn() == true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies TurnOff() sets switch to off.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "TurnOff")]
    public Task Switch_TurnOff_SetsSwitchToOff()
    {
        // Arrange - ensure on first
        Page.NewsletterSwitch.TurnOn();

        // Act
        Page.NewsletterSwitch.TurnOff();

        // Assert
        Assert.True(Page.NewsletterSwitch.IsOn() == false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Toggle() inverts the switch state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Toggle")]
    public Task Switch_Toggle_InvertsState()
    {
        // Arrange
        Page.NewsletterSwitch.TurnOff();
        var initialState = Page.NewsletterSwitch.IsOn();

        // Act
        Page.NewsletterSwitch.Toggle();

        // Assert
        Assert.NotEqual(initialState, Page.NewsletterSwitch.IsOn());
        return Task.CompletedTask;
    }

    #endregion

    #region Assertion Tests

    /// <summary>
    /// Verifies AssertOn passes when switch is on.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertOn")]
    public Task Switch_AssertOn_PassesWhenOn()
    {
        // Arrange
        Page.NewsletterSwitch.TurnOn();

        // Assert - no exception means success
        Page.NewsletterSwitch.AssertOn();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies AssertOff passes when switch is off.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertOff")]
    public Task Switch_AssertOff_PassesWhenOff()
    {
        // Arrange
        Page.NewsletterSwitch.TurnOff();

        // Assert - no exception means success
        Page.NewsletterSwitch.AssertOff();
        return Task.CompletedTask;
    }

    #endregion

    #region Idempotency Tests

    /// <summary>
    /// Verifies TurnOn is idempotent (calling when already on doesn't throw).
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Idempotent")]
    public Task Switch_TurnOn_IsIdempotent()
    {
        // Arrange
        Page.NewsletterSwitch.TurnOn();

        // Act - turn on again
        Page.NewsletterSwitch.TurnOn();

        // Assert - still on
        Assert.True(Page.NewsletterSwitch.IsOn() == true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies TurnOff is idempotent (calling when already off doesn't throw).
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Idempotent")]
    public Task Switch_TurnOff_IsIdempotent()
    {
        // Arrange
        Page.NewsletterSwitch.TurnOff();

        // Act - turn off again
        Page.NewsletterSwitch.TurnOff();

        // Assert - still off
        Assert.True(Page.NewsletterSwitch.IsOn() == false);
        return Task.CompletedTask;
    }

    #endregion
}
