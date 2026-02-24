using Brinell.Core.Interfaces;

namespace Brinell.WinForms.Interfaces;

/// <summary>
/// WinForms-specific driver interface.
/// Provides FlaUI-based automation for WinForms desktop applications.
/// </summary>
public interface IWinFormsDriver : IDriver<IWinFormsElement>, IDiagnosticDriver
{
    #region Window Management

    /// <summary>
    /// Gets or sets the main window title.
    /// </summary>
    string? WindowTitle { get; set; }

    /// <summary>
    /// Maximizes the main window.
    /// </summary>
    void MaximizeWindow();

    /// <summary>
    /// Minimizes the main window.
    /// </summary>
    void MinimizeWindow();

    /// <summary>
    /// Restores the main window to normal size.
    /// </summary>
    void RestoreWindow();

    /// <summary>
    /// Closes the main window.
    /// </summary>
    void CloseWindow();

    /// <summary>
    /// Gets the current window size.
    /// </summary>
    System.Drawing.Size GetWindowSize();

    /// <summary>
    /// Sets the window size.
    /// </summary>
    void SetWindowSize(int width, int height);

    /// <summary>
    /// Gets the current window position.
    /// </summary>
    System.Drawing.Point GetWindowPosition();

    /// <summary>
    /// Sets the window position.
    /// </summary>
    void SetWindowPosition(int x, int y);

    #endregion

    #region Navigation

    /// <summary>
    /// Navigates to the specified destination.
    /// </summary>
    void NavigateTo(string destination);

    /// <summary>
    /// Navigates back (sends Alt+Left).
    /// </summary>
    void NavigateBack();

    /// <summary>
    /// Refreshes the current view (sends F5).
    /// </summary>
    void Refresh();

    #endregion

    #region Screenshots

    /// <summary>
    /// Takes a screenshot of the main window.
    /// </summary>
    byte[] TakeScreenshot();

    /// <summary>
    /// Saves a screenshot to the specified path.
    /// </summary>
    void SaveScreenshot(string filePath);

    #endregion

    #region Application State

    /// <summary>
    /// Resets the application state.
    /// </summary>
    void ResetAppState();

    #endregion
}
