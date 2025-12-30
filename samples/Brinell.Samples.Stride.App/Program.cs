using Brinell.Samples.Stride.App;
using Brinell.Stride.Automation;

// Create and run the game
using var game = new SampleStrideGame();

// Enable automation if running in test mode
if (StrideAutomationExtensions.IsAutomationEnabled())
{
    game.UseAutomation(() => game.MainUI);
    Console.WriteLine("Automation server enabled");
}

game.Run();
