namespace Brinell.Stride.Communication;

/// <summary>
/// Abstraction for communication between test process and game process.
/// </summary>
public interface IAutomationChannel : IDisposable
{
    /// <summary>
    /// Check if connected to game.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect to the game's automation service.
    /// </summary>
    /// <param name="timeout">Connection timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a command and receive response.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the game.</returns>
    Task<AutomationResponse> SendCommandAsync(AutomationCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from game.
    /// </summary>
    Task DisconnectAsync();
}
