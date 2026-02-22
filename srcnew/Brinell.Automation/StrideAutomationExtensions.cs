using Stride.Engine;
using Stride.UI;

namespace Brinell.Automation;

/// <summary>
/// Extension methods for integrating Brinell automation into Stride games.
/// </summary>
public static class StrideAutomationExtensions
{
    public static Game UseAutomation(this Game game, Func<UIElement?> uiRootProvider, AutomationServerOptions? options = null)
    {
        var system = new AutomationGameSystem(game.Services, uiRootProvider, options, game);
        game.GameSystems.Add(system);
        return game;
    }

    public static Game UseAutomation(this Game game, IAutomationHandler handler, AutomationServerOptions? options = null)
    {
        var system = new AutomationGameSystem(game.Services, handler, options);
        game.GameSystems.Add(system);
        return game;
    }

    public static bool IsAutomationEnabled()
    {
        return Environment.GetCommandLineArgs().Contains("--automation") ||
               Environment.GetEnvironmentVariable("BRINELL_AUTOMATION") == "1";
    }

    public static Game UseAutomationIfEnabled(this Game game, Func<UIElement?> uiRootProvider, AutomationServerOptions? options = null)
    {
        if (IsAutomationEnabled())
            return game.UseAutomation(uiRootProvider, options);
        return game;
    }
}
