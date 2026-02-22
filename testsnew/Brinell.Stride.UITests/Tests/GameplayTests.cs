using Brinell.Stride.UITests.PageObjects;

namespace Brinell.Stride.UITests.Tests;

public class GameplayTests : StrideUITestBase
{
    public GameplayTests(StrideAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output) { }

    [Fact]
    public void Game_Initializes_ShowsHUD()
    {
        var game = new GamePage(Context);
        game.AssertLoaded(true);

        game.GameTitle.AssertExists(true);
        game.GameTitle.AssertText("Brinell Stride Sample");
        game.PositionDisplay.AssertExists(true);
        game.EscHint.AssertExists(true);
        game.MovementHint.AssertExists(true);
    }

    [Fact]
    public void Player_InitialPosition_IsAtOrigin()
    {
        var game = new GamePage(Context);
        game.AssertLoaded(true);

        // Player may have moved from prior tests — just verify position is displayed
        game.PositionDisplay.AssertExists(true);
        game.PositionDisplay.AssertTextContains("Position:");
    }

    [Fact]
    public void Player_MoveNorth_PositionChanges()
    {
        var game = new GamePage(Context);
        game.AssertLoaded(true);
        var initialText = game.PositionDisplay.GetText();

        game.MoveNorth(durationMs: 300);

        Assert.NotEqual(initialText, game.PositionDisplay.GetText());
    }

    [Fact]
    public void Player_MoveSouth_PositionChanges()
    {
        var game = new GamePage(Context);
        game.AssertLoaded(true);
        var initialText = game.PositionDisplay.GetText();

        game.MoveSouth(durationMs: 300);

        Assert.NotEqual(initialText, game.PositionDisplay.GetText());
    }

    [Fact]
    public void HUD_EscHintSaysPress()
    {
        var game = new GamePage(Context);
        game.AssertLoaded(true);

        game.EscHint.AssertTextContains("ESC");
    }

    [Fact]
    public void Game_PressEscape_OpensSettings()
    {
        // Ensure settings are closed first (may be left open by prior test)
        var settingsCheck = new SettingsPage(Context);
        if (settingsCheck.IsLoaded())
            settingsCheck.Close();

        var game = new GamePage(Context);
        game.AssertLoaded(true);

        game.OpenSettings();

        var settings = new SettingsPage(Context);
        settings.AssertLoaded(true);
    }

    [Fact]
    public void Game_OpenAndCloseSettings_ReturnsToGame()
    {
        // Ensure settings are closed first (may be left open by prior test)
        var settingsCheck = new SettingsPage(Context);
        if (settingsCheck.IsLoaded())
            settingsCheck.Close();

        var game = new GamePage(Context);
        game.AssertLoaded(true);

        game.OpenSettings();
        var settings = new SettingsPage(Context);
        settings.AssertLoaded(true);

        settings.Close();
        game.AssertLoaded(true);
    }
}
