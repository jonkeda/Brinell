using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Toggle control tests for Switch and CheckBox controls.
/// Uses verified ControlObject6 APIs: IsChecked, IsOn, Toggle, Check, Uncheck, TurnOn, TurnOff, SetChecked.
/// </summary>
public class ToggleControlTests6 : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;

    public ToggleControlTests6(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
    }

    #region Switch Tests

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "Switch")]
    [Trait("Priority", "P0")]
    public void Switch_InitiallyOn_IsCheckedTrue()
    {
        // Arrange - Switch starts as on per MainPage.xaml (IsToggled="true")
        _mainPage.WaitLoaded(true);

        // Assert
        Assert.True(_mainPage.NotificationSwitch.IsOn());
        Assert.True(_mainPage.NotificationSwitch.IsChecked());
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "Switch")]
    [Trait("Priority", "P0")]
    public void Switch_Toggle_ChangesState()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var initialState = _mainPage.NotificationSwitch.IsOn();

        // Act
        _mainPage.NotificationSwitch.Toggle();

        // Assert
        _mainPage.NotificationSwitch.WaitChecked(!initialState, timeoutMs: 2000);
        Assert.Equal(!initialState, _mainPage.NotificationSwitch.IsOn());
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "Switch")]
    [Trait("Priority", "P0")]
    public void Switch_TurnOff_BecomesUnchecked()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NotificationSwitch.TurnOn(); // Ensure on first

        // Act
        _mainPage.NotificationSwitch.TurnOff();

        // Assert
        _mainPage.NotificationSwitch.AssertChecked(false);
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "Switch")]
    [Trait("Priority", "P0")]
    public void Switch_TurnOn_BecomesChecked()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NotificationSwitch.TurnOff(); // Ensure off first

        // Act
        _mainPage.NotificationSwitch.TurnOn();

        // Assert
        _mainPage.NotificationSwitch.AssertChecked(true);
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "Switch")]
    [Trait("Priority", "P1")]
    public void Switch_SetChecked_True_TurnsOn()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NotificationSwitch.TurnOff();

        // Act
        _mainPage.NotificationSwitch.SetChecked(true);

        // Assert
        Assert.True(_mainPage.NotificationSwitch.IsOn());
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "Switch")]
    [Trait("Priority", "P1")]
    public void Switch_SetChecked_False_TurnsOff()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NotificationSwitch.TurnOn();

        // Act
        _mainPage.NotificationSwitch.SetChecked(false);

        // Assert
        Assert.False(_mainPage.NotificationSwitch.IsOn());
    }

    #endregion

    #region CheckBox Tests

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "CheckBox")]
    [Trait("Priority", "P0")]
    public void CheckBox_InitiallyUnchecked_IsCheckedFalse()
    {
        // Arrange - CheckBox starts as unchecked per MainPage.xaml (IsChecked="false")
        _mainPage.WaitLoaded(true);

        // Assert
        Assert.False(_mainPage.AgreeCheckBox.IsChecked());
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "CheckBox")]
    [Trait("Priority", "P0")]
    public void CheckBox_Toggle_ChangesCheckedState()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var initialState = _mainPage.AgreeCheckBox.IsChecked();

        // Act
        _mainPage.AgreeCheckBox.Toggle();

        // Assert
        _mainPage.AgreeCheckBox.WaitChecked(!initialState, timeoutMs: 2000);
        Assert.Equal(!initialState, _mainPage.AgreeCheckBox.IsChecked());
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "CheckBox")]
    [Trait("Priority", "P0")]
    public void CheckBox_Check_BecomesChecked()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.AgreeCheckBox.Uncheck(); // Ensure unchecked first

        // Act
        _mainPage.AgreeCheckBox.Check();

        // Assert
        _mainPage.AgreeCheckBox.AssertChecked(true);
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "CheckBox")]
    [Trait("Priority", "P0")]
    public void CheckBox_Uncheck_BecomesUnchecked()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.AgreeCheckBox.Check(); // Ensure checked first

        // Act
        _mainPage.AgreeCheckBox.Uncheck();

        // Assert
        _mainPage.AgreeCheckBox.AssertChecked(false);
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "CheckBox")]
    [Trait("Priority", "P1")]
    public void CheckBox_SetChecked_True_ChecksTheBox()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.AgreeCheckBox.Uncheck();

        // Act
        _mainPage.AgreeCheckBox.SetChecked(true);

        // Assert
        Assert.True(_mainPage.AgreeCheckBox.IsChecked());
    }

    [Fact]
    [Trait("Category", "Toggle")]
    [Trait("Control", "CheckBox")]
    [Trait("Priority", "P1")]
    public void CheckBox_SetChecked_False_UnchecksTheBox()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.AgreeCheckBox.Check();

        // Act
        _mainPage.AgreeCheckBox.SetChecked(false);

        // Assert
        Assert.False(_mainPage.AgreeCheckBox.IsChecked());
    }

    #endregion
}
