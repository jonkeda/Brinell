using Brinell.Stride.Communication;

namespace Brinell.Stride.Automation;

/// <summary>
/// Interface for handling automation commands in the game.
/// </summary>
public interface IAutomationHandler
{
    /// <summary>
    /// Handle an automation command.
    /// </summary>
    Task<AutomationResponse> HandleCommandAsync(AutomationCommand command, CancellationToken cancellationToken = default);
}
