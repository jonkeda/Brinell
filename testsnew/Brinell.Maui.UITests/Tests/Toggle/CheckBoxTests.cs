using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Toggle;

/// <summary>
/// UI tests for the CheckBox control in the ToggleTestView.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "CheckBox")]
public class CheckBoxTests
{
    private readonly MauiFixture _fixture;

    public CheckBoxTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.AppShell.ToggleTab.Click();
    }

    private ToggleTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the CheckBox exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task CheckBox_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestCheckBox.AssertExists();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that clicking the CheckBox toggles its state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task CheckBox_Click_TogglesChecked()
    {
        // Arrange
        var page = GetPage();

        // Act & Assert
        page.TestCheckBox.Click()
            .CheckBoxStatusLabel.AssertTextContains("checked");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that unchecking the CheckBox updates the status.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task CheckBox_ClickTwice_TogglesUnchecked()
    {
        // Arrange
        var page = GetPage();

        // Act & Assert
        page.TestCheckBox.Click()
            .CheckBoxStatusLabel.AssertTextContains("checked")
            .TestCheckBox.Click()
            .CheckBoxStatusLabel.AssertTextContains("unchecked");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Reset button clears the CheckBox state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Reset")]
    public Task CheckBox_Reset_ClearsState()
    {
        // Arrange
        var page = GetPage();

        // Act & Assert
        page.TestCheckBox.Click()
            .CheckBoxStatusLabel.AssertTextContains("checked")
            .ResetButton.Click()
            .CheckBoxStatusLabel.AssertTextContains("unchecked");

        return Task.CompletedTask;
    }
}
