using Brinell.Core.Interfaces;

namespace Brinell.Stride.Interfaces;

/// <summary>
/// Stride test context extending the core ITestContext with Stride-specific operations.
/// Provides element state querying, input simulation, and game lifecycle management.
/// </summary>
public interface IStrideTestContext : ITestContext
{
    /// <summary>
    /// Whether the game is connected and ready for automation.
    /// </summary>
    bool IsGameReady { get; }

    /// <summary>
    /// Get element state from the game via automation pipe.
    /// </summary>
    ElementState GetElementState(string automationId);

    /// <summary>
    /// Click an element by automation ID using coordinate-based input.
    /// </summary>
    void ClickElement(string automationId);

    /// <summary>
    /// Set element text directly via server-side automation command.
    /// </summary>
    bool SetElementText(string automationId, string text);

    /// <summary>
    /// Set slider value directly via server-side automation command.
    /// </summary>
    bool SetSliderValue(string automationId, double value);

    /// <summary>
    /// Set toggle value directly via server-side automation command.
    /// </summary>
    bool SetToggleValue(string automationId, bool value);

    /// <summary>
    /// Press a virtual key.
    /// </summary>
    void PressKey(VirtualKey key);

    /// <summary>
    /// Hold a key for a duration.
    /// </summary>
    void HoldKey(VirtualKey key, int durationMs);

    /// <summary>
    /// Check if an element exists.
    /// </summary>
    bool ElementExists(string automationId);

    /// <summary>
    /// Check if an element is visible.
    /// </summary>
    bool ElementIsVisible(string automationId);

    /// <summary>
    /// Check if the game is currently busy (loading, etc.).
    /// </summary>
    bool IsGameBusy();

    /// <summary>
    /// Wait for a condition to become true.
    /// </summary>
    bool WaitFor(Func<bool> condition, int? timeoutMs = null, string description = "condition");

    /// <summary>
    /// Wait for game to be ready.
    /// </summary>
    bool WaitForGameReady(int? timeoutMs = null);

    /// <summary>
    /// Send a raw automation command to the game.
    /// </summary>
    AutomationResponse SendCommand(AutomationCommand command);
}
