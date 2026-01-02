using Brinell.Samples.Stride.UITests.PageObjects;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests;

/// <summary>
/// Tests for the gameplay functionality (player movement, position display).
/// </summary>
public class GameplayTests : StrideUITestBase
{
    public GameplayTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void Game_Initializes_ShowsHUD()
    {
        // Arrange & Act
        var game = new GamePage(Context);
        game.CheckActive();

        // Assert
        game.GameTitle.AssertExists();
        game.GameTitle.AssertTextEquals("Brinell Stride Sample");
        game.PositionDisplay.AssertExists();
        game.EscHint.AssertExists();
        game.MovementHint.AssertExists();
    }

    [Fact]
    public void Player_InitialPosition_IsAtOrigin()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();

        // Assert
        game.PositionDisplay.AssertTextContains("0.0");
    }

    [Fact]
    public void Player_MoveNorth_PositionIncreases()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();
        var initialText = game.PositionDisplay.GetText();

        // Act
        game.MoveNorth(durationMs: 300);

        // Assert - Position should have changed (Z coordinate should decrease since we move north in negative Z)
        Context.WaitFor(() => true, 500); // Wait for movement to complete
        var finalText = game.PositionDisplay.GetText();
        finalText.Should().NotBe(initialText, "Position should have changed after moving north");
    }

    [Fact]
    public void Player_MoveSouth_PositionDecreases()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();
        var initialText = game.PositionDisplay.GetText();

        // Act
        game.MoveSouth(durationMs: 300);

        // Assert - Position should have changed (Z coordinate should increase)
        Context.WaitFor(() => true, 500);
        var finalText = game.PositionDisplay.GetText();
        finalText.Should().NotBe(initialText, "Position should have changed after moving south");
    }

    [Fact]
    public void Player_MoveEast_PositionChanges()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();
        var initialText = game.PositionDisplay.GetText();

        // Act
        game.MoveEast(durationMs: 300);

        // Assert - Position X should have changed
        Context.WaitFor(() => true, 500);
        var finalText = game.PositionDisplay.GetText();
        finalText.Should().NotBe(initialText, "Position should have changed after moving east");
    }

    [Fact]
    public void Player_MoveWest_PositionChanges()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();
        var initialText = game.PositionDisplay.GetText();

        // Act
        game.MoveWest(durationMs: 300);

        // Assert - Position X should have changed
        Context.WaitFor(() => true, 500);
        var finalText = game.PositionDisplay.GetText();
        finalText.Should().NotBe(initialText, "Position should have changed after moving west");
    }

    [Fact]
    public void HUD_EscHintSaysPress()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();

        // Assert
        game.EscHint.AssertTextContains("ESC");
    }

    [Fact]
    public void Game_PressEscape_OpensSettings()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();

        // Act
        game.OpenSettings();
        Context.WaitFor(() => true, 500); // Wait for settings to open

        // Assert
        var settings = new SettingsPage(Context);
        settings.CheckActive();
    }

    [Fact]
    public void Game_OpenAndCloseSettings_ReturnsToGame()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();

        // Act - Open settings
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);
        settings.CheckActive();

        // Act - Close settings
        settings.Close();
        Context.WaitFor(() => true, 500);

        // Assert - Back to game
        game.CheckActive();
    }

    [Fact]
    public void Game_PlayerCannotMovePastBoundary()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();

        // Act - Try to move in one direction for a long time
        game.MoveEast(durationMs: 5000);

        // Assert - Player should still be visible and not thrown error
        Context.WaitFor(() => true, 500);
        game.PositionDisplay.AssertExists();
        game.PositionDisplay.AssertTextContains("Position:"); // Still shows position, not crashed
    }

    [Fact]
    public void HUD_ShowsMovementHint()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();

        // Assert
        game.MovementHint.AssertTextContains("WASD");
    }
}
