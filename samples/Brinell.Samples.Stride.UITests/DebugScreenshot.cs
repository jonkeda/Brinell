using Brinell.Samples.Stride.UITests.PageObjects;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests;

/// <summary>
/// Debug test to verify what elements actually exist in the game.
/// </summary>
public class DebugScreenshot : StrideUITestBase
{
    public DebugScreenshot(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void Debug_TakeGameplayScreenshot()
    {
        var game = new GamePage(Context);
        game.CheckActive();

        // Wait for game to render
        Context.WaitFor(() => true, 1000);
        
        // Take screenshot of gameplay
        var screenshotPath = Context.TakeScreenshot("gameplay");
        if (screenshotPath != null)
        {
            Log($"Gameplay screenshot saved to: {screenshotPath}");
        }
        else
        {
            Log("ERROR: TakeScreenshot returned null");
        }
    }

    [Fact]
    public void Debug_CheckGameplayElements()
    {
        var game = new GamePage(Context);
        game.CheckActive();

        // Check what HUD elements exist
        Log("=== GAMEPLAY HUD ===");
        Log($"GameTitle exists: {Context.ElementExists("GameTitle")}");
        Log($"GameTitle visible: {Context.ElementIsVisible("GameTitle")}");
        Log($"GameTitle text: '{Context.GetElementText("GameTitle")}'");
        
        Log($"PositionDisplay exists: {Context.ElementExists("PositionDisplay")}");
        Log($"PositionDisplay visible: {Context.ElementIsVisible("PositionDisplay")}");
        Log($"PositionDisplay text: '{Context.GetElementText("PositionDisplay")}'");
        
        Log($"EscHint exists: {Context.ElementExists("EscHint")}");
        Log($"EscHint visible: {Context.ElementIsVisible("EscHint")}");
        Log($"EscHint text: '{Context.GetElementText("EscHint")}'");
        
        Log($"MovementHint exists: {Context.ElementExists("MovementHint")}");
        Log($"MovementHint text: '{Context.GetElementText("MovementHint")}'");
    }

    [Fact]
    public void Debug_TakeSettingsScreenshot()
    {
        var game = new GamePage(Context);
        game.CheckActive();

        // Open settings
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        
        // Take screenshot of settings
        var screenshotPath = Context.TakeScreenshot("settings");
        Log($"Settings screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Debug_CheckSettingsElements()
    {
        var game = new GamePage(Context);
        game.CheckActive();

        Log("=== OPENING SETTINGS ===");
        game.OpenSettings();
        Context.WaitFor(() => true, 500);

        Log($"SettingsPanel exists: {Context.ElementExists("SettingsPanel")}");
        Log($"SettingsPanel visible: {Context.ElementIsVisible("SettingsPanel")}");
        Log($"SettingsOverlay exists: {Context.ElementExists("SettingsOverlay")}");
        
        Log("=== AUDIO SECTION ===");
        Log($"MasterVolumeSlider exists: {Context.ElementExists("MasterVolumeSlider")}");
        Log($"MasterVolumeSlider visible: {Context.ElementIsVisible("MasterVolumeSlider")}");
        
        Log("=== GRAPHICS SECTION ===");
        Log($"FullscreenToggle exists: {Context.ElementExists("FullscreenToggle")}");
        Log($"FullscreenToggle visible: {Context.ElementIsVisible("FullscreenToggle")}");
        
        Log("=== GAMEPLAY SECTION ===");
        Log($"PlayerNameInput exists: {Context.ElementExists("PlayerNameInput")}");
        Log($"PlayerNameInput visible: {Context.ElementIsVisible("PlayerNameInput")}");
        
        Log("=== BUTTONS ===");
        Log($"ApplyButton exists: {Context.ElementExists("ApplyButton")}");
        Log($"ApplyButton visible: {Context.ElementIsVisible("ApplyButton")}");
        Log($"CloseButton exists: {Context.ElementExists("CloseButton")}");
        Log($"CloseButton visible: {Context.ElementIsVisible("CloseButton")}");
    }
}

