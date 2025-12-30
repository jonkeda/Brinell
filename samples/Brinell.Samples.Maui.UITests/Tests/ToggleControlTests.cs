using Brinell.Samples.Maui.UITests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.Tests;

/// <summary>
/// Tests for toggle controls (Switch, CheckBox) on MainPage.
/// </summary>
public class ToggleControlTests : MauiTestBase
{
    private readonly MainPageObject _mainPage;

    public ToggleControlTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject(Context);
    }

    [Fact]
    public void NotificationSwitch_InitiallyOn_IsOn()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("NotificationSwitch");

        // Assert
        _mainPage.NotificationSwitch.AssertIsOn();
    }

    [Fact]
    public void NotificationSwitch_Toggle_TurnsOff()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("NotificationSwitch");

        // Act
        _mainPage.NotificationSwitch.Toggle();

        // Assert
        _mainPage.NotificationSwitch.AssertIsOff();
    }

    [Fact]
    public void AgreeCheckBox_InitiallyUnchecked_IsUnchecked()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("AgreeCheckBox");

        // Assert
        _mainPage.AgreeCheckBox.AssertUnchecked();
    }

    [Fact]
    public void AgreeCheckBox_Check_BecomesChecked()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("AgreeCheckBox");

        // Act
        _mainPage.AgreeCheckBox.Check();

        // Assert
        _mainPage.AgreeCheckBox.AssertChecked();
    }

    [Fact]
    public void AgreeCheckBox_Toggle_ChangesState()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("AgreeCheckBox");

        // Act - Toggle twice
        _mainPage.AgreeCheckBox.Toggle();
        _mainPage.AgreeCheckBox.Toggle();

        // Assert - Should be back to unchecked
        _mainPage.AgreeCheckBox.AssertUnchecked();
    }
}
