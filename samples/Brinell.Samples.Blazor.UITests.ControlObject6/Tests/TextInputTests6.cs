using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Samples.Blazor.UITests.ControlObject6.TestBase;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.Tests;

/// <summary>
/// Text input control tests using ControlObject6 async API.
/// </summary>
public class TextInputTests6 : BlazorTestBase6
{
    public TextInputTests6(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public async Task Input_EnterAsync_SetsText()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        const string testText = "testuser";

        // Act
        await usernameInput.EnterAsync(testText);

        // Assert
        await usernameInput.AssertTextAsync(testText);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public async Task Input_ClearAsync_RemovesText()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        await usernameInput.EnterAsync("some text");

        // Act
        await usernameInput.ClearAsync();

        // Assert
        await usernameInput.AssertTextEmptyAsync(true);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public async Task Input_ClearAndEnterAsync_ReplacesText()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        await usernameInput.EnterAsync("old text");
        const string newText = "new text";

        // Act
        await usernameInput.ClearAndEnterAsync(newText);

        // Assert
        await usernameInput.AssertTextAsync(newText);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public async Task Input_AppendAsync_AddsToExistingText()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        await usernameInput.ClearAsync();
        await usernameInput.EnterAsync("Hello");

        // Act
        await usernameInput.AppendAsync(" World");

        // Assert
        await usernameInput.AssertTextAsync("Hello World");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public async Task Input_IsVisibleAsync_ReturnsTrueForVisibleInput()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);

        // Act
        var isVisible = await usernameInput.IsVisibleAsync();

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public async Task Input_IsEnabledAsync_ReturnsTrueForEnabledInput()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);

        // Act
        var isEnabled = await usernameInput.IsEnabledAsync();

        // Assert
        isEnabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public async Task Input_GetTextAsync_ReturnsCurrentValue()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        const string testText = "TestValue";
        await usernameInput.EnterAsync(testText);

        // Act
        var text = await usernameInput.GetTextAsync();

        // Assert
        text.Should().Be(testText);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public async Task Input_AssertTextContainsAsync_MatchesPartial()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        await usernameInput.EnterAsync("Hello World");

        // Assert - should not throw
        await usernameInput.AssertTextContainsAsync("World");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public async Task Input_AssertTextStartsWithAsync_MatchesPrefix()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        await usernameInput.EnterAsync("Hello World");

        // Assert - should not throw
        await usernameInput.AssertTextStartsWithAsync("Hello");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public async Task Input_AssertTextEndsWithAsync_MatchesSuffix()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        await usernameInput.EnterAsync("Hello World");

        // Assert - should not throw
        await usernameInput.AssertTextEndsWithAsync("World");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P2")]
    public async Task Input_AssertTextMatchesAsync_MatchesRegex()
    {
        // Arrange
        await NavigateToAsync("login");
        var usernameInput = new InputControl(Context, "username-input", null);
        await usernameInput.EnterAsync("test123");

        // Assert - matches letters followed by numbers
        await usernameInput.AssertTextMatchesAsync(@"^[a-z]+\d+$");
    }
}
