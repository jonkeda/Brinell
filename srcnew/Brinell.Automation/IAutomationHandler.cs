using Brinell.Automation.Communication;

namespace Brinell.Automation;

/// <summary>
/// Handles automation commands received from the test process.
/// </summary>
public interface IAutomationHandler
{
    Task<AutomationResponse> HandleCommandAsync(AutomationCommand command, CancellationToken cancellationToken = default);
}
