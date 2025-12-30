using Stride.Engine;
using Stride.UI;

namespace Brinell.Stride.Automation;

/// <summary>
/// Extension methods for setting up automation in Stride games.
/// </summary>
public static class StrideAutomationExtensions
{
    /// <summary>
    /// Add automation support to a game.
    /// </summary>
    public static Game UseAutomation(
        this Game game, 
        Func<UIElement?> uiRootProvider,
        AutomationServerOptions? options = null)
    {
        var system = new AutomationGameSystem(game.Services, uiRootProvider, options);
        game.GameSystems.Add(system);
        return game;
    }

    /// <summary>
    /// Add automation support with a custom handler.
    /// </summary>
    public static Game UseAutomation(
        this Game game,
        IAutomationHandler handler,
        AutomationServerOptions? options = null)
    {
        var system = new AutomationGameSystem(game.Services, handler, options);
        game.GameSystems.Add(system);
        return game;
    }

    /// <summary>
    /// Check if automation is enabled (e.g., via command line or environment variable).
    /// </summary>
    public static bool IsAutomationEnabled()
    {
        return Environment.GetCommandLineArgs().Contains("--automation") ||
               Environment.GetEnvironmentVariable("BRINELL_AUTOMATION") == "1";
    }

    /// <summary>
    /// Conditionally add automation support if enabled.
    /// </summary>
    public static Game UseAutomationIfEnabled(
        this Game game,
        Func<UIElement?> uiRootProvider,
        AutomationServerOptions? options = null)
    {
        if (IsAutomationEnabled())
        {
            return game.UseAutomation(uiRootProvider, options);
        }
        return game;
    }
}
