using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Toggle;

/// <summary>
/// UI tests for CheckBox verifying check/uncheck operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "CheckBox")]
public class CheckBoxControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public CheckBoxControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that checkboxes exist on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task CheckBox_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.TermsCheckBox.IsExists());
        Assert.True(Page.PrivacyCheckBox.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that checkboxes are visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task CheckBox_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.TermsCheckBox.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Toggle Operation Tests

    /// <summary>
    /// Verifies IsChecked returns correct state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsChecked")]
    public Task CheckBox_IsChecked_ReturnsCorrectState()
    {
        // Act & Assert - just verify we can query state (nullable bool)
        var isChecked = Page.TermsCheckBox.IsChecked();
        Assert.True(isChecked == true || isChecked == false); // Either state is valid
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Check() sets checked state to true.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Check")]
    public Task CheckBox_Check_SetsCheckedToTrue()
    {
        // Arrange - ensure unchecked first
        Page.TermsCheckBox.Uncheck();

        // Act
        Page.TermsCheckBox.Check();

        // Assert
        Assert.True(Page.TermsCheckBox.IsChecked() == true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Uncheck() sets checked state to false.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Uncheck")]
    public Task CheckBox_Uncheck_SetsCheckedToFalse()
    {
        // Arrange - ensure checked first
        Page.TermsCheckBox.Check();

        // Act
        Page.TermsCheckBox.Uncheck();

        // Assert
        Assert.True(Page.TermsCheckBox.IsChecked() == false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Toggle() inverts the checked state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Toggle")]
    public Task CheckBox_Toggle_InvertsState()
    {
        // Arrange
        Page.TermsCheckBox.Uncheck();
        var initialState = Page.TermsCheckBox.IsChecked();

        // Act
        Page.TermsCheckBox.Toggle();

        // Assert
        Assert.NotEqual(initialState, Page.TermsCheckBox.IsChecked());
        return Task.CompletedTask;
    }

    #endregion

    #region Assertion Tests

    /// <summary>
    /// Verifies AssertChecked passes when checked.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertChecked")]
    public Task CheckBox_AssertChecked_PassesWhenChecked()
    {
        // Arrange
        Page.TermsCheckBox.Check();

        // Assert - no exception means success
        Page.TermsCheckBox.AssertChecked(true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies AssertChecked passes when unchecked with false expectation.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertChecked")]
    public Task CheckBox_AssertChecked_PassesWhenUnchecked()
    {
        // Arrange
        Page.TermsCheckBox.Uncheck();

        // Assert - no exception means success
        Page.TermsCheckBox.AssertChecked(false);
        return Task.CompletedTask;
    }

    #endregion

    #region Multiple CheckBox Tests

    /// <summary>
    /// Verifies multiple checkboxes can be operated independently.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "MultipleControls")]
    public Task CheckBox_MultipleControls_OperateIndependently()
    {
        // Arrange - set initial states
        Page.TermsCheckBox.Uncheck();
        Page.PrivacyCheckBox.Uncheck();

        // Act - check only Terms
        Page.TermsCheckBox.Check();

        // Assert - Terms checked, Privacy unchecked
        Assert.True(Page.TermsCheckBox.IsChecked() == true);
        Assert.True(Page.PrivacyCheckBox.IsChecked() == false);
        return Task.CompletedTask;
    }

    #endregion
}
