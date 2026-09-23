using Brinell.Core.Interfaces;

namespace Brinell.Wpf.Interfaces;

/// <summary>
/// WPF-specific driver interface extending <see cref="IDriver{TElement}"/> and <see cref="IDiagnosticDriver"/>.
/// Provides window management, navigation, and screenshot capabilities for WPF desktop apps.
/// </summary>
public interface IWpfDriver : IDriver<IWpfElement>, IDiagnosticDriver
{
    #region Window Management

    /// <summary>
    /// Gets the current window handle.
    /// </summary>
    string CurrentWindowHandle { get; }

    /// <summary>
    /// Gets all window handles.
    /// </summary>
    IReadOnlyCollection<string> WindowHandles { get; }

    #endregion

    #region Navigation

    /// <summary>
    /// Navigates to the specified destination (not typically used in WPF).
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

    /// <summary>
    /// Takes a screenshot of the current state.
    /// </summary>
    byte[] TakeScreenshot();

    /// <summary>
    /// Resets the application state (closes and relaunches).
    /// </summary>
    void ResetAppState();

    #endregion
}
