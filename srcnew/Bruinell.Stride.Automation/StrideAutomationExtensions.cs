using Stride.Engine;
using Stride.UI;
using Bruinell.Core.Configuration;
using Bruinell.Stride;

namespace Bruinell.Stride.Automation;

/// <summary>
/// Extension methods for integrating Bruinell automation into Stride games.
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

    /// <summary>
    /// Determines if automation should be enabled based on configuration or fallbacks.
    /// Checks in order: configuration, command line args, environment variable.
    /// </summary>
    public static bool IsAutomationEnabled(BruinellStrideConfiguration? config = null)
    {
        if (config?.Stride?.AutomationEnabled == true)
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Contains("--automation") ||
               Environment.GetEnvironmentVariable("BRUINELL_AUTOMATION") == "1";
    }

    public static Game UseAutomationIfEnabled(this Game game, Func<UIElement?> uiRootProvider, AutomationServerOptions? options = null)
    {
        if (IsAutomationEnabled())
            return game.UseAutomation(uiRootProvider, options);
        return game;
    }

    /// <summary>
    /// Conditionally applies automation based on configuration or fallbacks.
    /// </summary>
    public static Game UseAutomationIfEnabled(this Game game, Func<UIElement?> uiRootProvider, BruinellStrideConfiguration? config, AutomationServerOptions? options = null)
    {
        if (IsAutomationEnabled(config))
            return game.UseAutomation(uiRootProvider, options);
        return game;
    }
}
