using Stride.Engine;
using Stride.UI;
using Brinell.Core.Configuration;
using Brinell.Stride;

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

    /// <summary>
    /// Determines if automation should be enabled based on configuration or fallbacks.
    /// Checks in order: configuration, command line args, environment variable.
    /// </summary>
    public static bool IsAutomationEnabled(BrinellStrideConfiguration? config = null)
    {
        // Check configuration first
        if (config?.Stride?.AutomationEnabled == true)
        {
            return true;
        }

        // Fall back to command line args or environment variable
        return Environment.GetCommandLineArgs().Contains("--automation") ||
               Environment.GetEnvironmentVariable("BRINELL_AUTOMATION") == "1";
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
    public static Game UseAutomationIfEnabled(this Game game, Func<UIElement?> uiRootProvider, BrinellStrideConfiguration? config, AutomationServerOptions? options = null)
    {
        if (IsAutomationEnabled(config))
            return game.UseAutomation(uiRootProvider, options);
        return game;
    }
}
