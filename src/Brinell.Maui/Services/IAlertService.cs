namespace Brinell.Maui.Services;

/// <summary>
/// Interface for alert/dialog handling services.
/// Provides methods for interacting with native alerts and dialogs.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Check if an alert is currently displayed.
    /// </summary>
    bool IsAlertPresent();

    /// <summary>
    /// Get the alert title.
    /// </summary>
    string? GetAlertTitle();

    /// <summary>
    /// Get the alert message.
    /// </summary>
    string? GetAlertMessage();

    /// <summary>
    /// Accept the alert (tap OK/Accept/Yes).
    /// </summary>
    void AcceptAlert();

    /// <summary>
    /// Dismiss the alert (tap Cancel/No/Dismiss).
    /// </summary>
    void DismissAlert();

    /// <summary>
    /// Enter text in a prompt alert.
    /// </summary>
    /// <param name="text">The text to enter.</param>
    void EnterPromptText(string text);

    /// <summary>
    /// Tap a button in the alert by its text.
    /// </summary>
    /// <param name="buttonText">The button text.</param>
    void TapAlertButton(string buttonText);

    /// <summary>
    /// Wait for an alert to appear.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    bool WaitForAlert(int? timeoutMs = null);

    /// <summary>
    /// Wait for an alert to disappear.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    bool WaitForAlertDismissed(int? timeoutMs = null);

    /// <summary>
    /// Handle a permission dialog (allow/deny).
    /// </summary>
    /// <param name="allow">True to allow, false to deny.</param>
    void HandlePermissionDialog(bool allow);
}
