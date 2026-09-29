namespace Brinell.Presenter.Services;

/// <summary>
/// The Presenter window as a bar beside the app under test: docks it to a side, and places the
/// app's window in the rest of the screen, whatever the target (a MAUI, WPF or WinForms window,
/// or a browser).
/// </summary>
public interface IPresenterWindowService
{
    /// <summary>Docks the Presenter window to a side of its monitor, full height.</summary>
    /// <param name="dock">The side.</param>
    void Dock(PresenterDock dock);

    /// <summary>The top-level windows now, to tell the app's apart once it opens.</summary>
    object Snapshot();

    /// <summary>
    /// Places the largest top-level window that opened since <paramref name="before"/> (not
    /// Presenter's own) beside the bar.
    /// </summary>
    /// <param name="before">A <see cref="Snapshot"/> taken before the app started.</param>
    /// <param name="dock">The side the bar is on.</param>
    /// <returns>What happened, as a placement report; null when no new window has opened yet.</returns>
    string? PlaceApp(object before, PresenterDock dock);
}
