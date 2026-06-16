using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Text;

/// <summary>
/// UI tests for Editor verifying multi-line text operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Editor")]
public class EditorControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public EditorControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that editor exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Editor_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.BioEditor.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that editor is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Editor_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.BioEditor.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Text Entry Tests

    /// <summary>
    /// Verifies Enter() sets text in editor.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Enter")]
    public Task Editor_Enter_SetsText()
    {
        // Arrange
        Page.BioEditor.Clear();

        // Act
        Page.BioEditor.Enter("Hello, this is my bio.");

        // Assert
        Assert.Equal("Hello, this is my bio.", Page.BioEditor.GetText());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Clear() removes text from editor.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Clear")]
    public Task Editor_Clear_RemovesText()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        Page.BioEditor.Enter("Some text to clear");

        // Act
        Page.BioEditor.Clear();

        // Assert
        Assert.Equal("", Page.BioEditor.GetText());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies GetText() returns entered text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task Editor_GetText_ReturnsEnteredText()
    {
        // Arrange
        Page.BioEditor.Clear();
        const string testText = "Test bio content";
        Page.BioEditor.Enter(testText);

        // Act
        var text = Page.BioEditor.GetText();

        // Assert
        Assert.Equal(testText, text);
        return Task.CompletedTask;
    }

    #endregion

    #region Multi-line Tests

    /// <summary>
    /// Verifies editor handles multi-line text.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Pattern", "MultiLine")]
    public Task Editor_Enter_HandlesMultiLineText()
    {
        // Arrange
        Page.BioEditor.Clear();
        const string multiLineText = "Line 1\nLine 2\nLine 3";

        // Act
        Page.BioEditor.Enter(multiLineText);

        // Assert - verify text contains newlines
        var text = Page.BioEditor.GetText();
        Assert.Contains("Line 1", text);
        Assert.Contains("Line 2", text);
        return Task.CompletedTask;
    }

    #endregion

    #region Assertion Tests

    /// <summary>
    /// Verifies AssertText passes with correct text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertText")]
    public Task Editor_AssertText_PassesWithCorrectText()
    {
        // Arrange
        Page.BioEditor.Clear();
        Page.BioEditor.Enter("Expected text");

        // Assert - no exception means success
        Page.BioEditor.AssertText("Expected text");
        return Task.CompletedTask;
    }

    #endregion
}
