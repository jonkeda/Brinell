using System.Drawing;

namespace Bruinell.Presenter.Services;

/// <summary>
/// The Presenter window as a bar beside the app under test: docks it to a side, and shows the
/// app's window as the App window setting says, whatever the target (a MAUI, WPF or WinForms
/// window, or a browser).
/// </summary>
public interface IPresenterWindowService
{
    /// <summary>Docks the Presenter window to a side of its monitor, full height.</summary>
    /// <param name="dock">The side.</param>
    void Dock(PresenterDock dock);

    /// <summary>Where the app will be put, for a launcher that can open it there (a browser); null when it is not moved.</summary>
    /// <param name="mode">How the app is shown.</param>
    /// <param name="dock">The side the bar is on.</param>
    Rectangle? TargetBounds(PresenterAppWindow mode, PresenterDock dock);

    /// <summary>The top-level windows now, to tell the app's apart once it opens.</summary>
    object Snapshot();

    /// <summary>
    /// Shows the largest top-level window that opened since <paramref name="before"/> (not
    /// Presenter's own) as <paramref name="mode"/> says, then gives the keys back to Presenter.
    /// </summary>
    /// <param name="before">A <see cref="Snapshot"/> taken before the app started.</param>
    /// <param name="dock">The side the bar is on.</param>
    /// <param name="mode">How the app is shown.</param>
    /// <returns>What happened, as a placement report; null when no new window has opened yet.</returns>
    string? PlaceApp(object before, PresenterDock dock, PresenterAppWindow mode);

    /// <summary>Takes the foreground back from the app it showed, so Presenter's keys work; leaves any other window alone.</summary>
    void ReclaimFocus();

    /// <summary>Keeps the bar above a maximized app while a run is on.</summary>
    /// <param name="topmost">Whether the bar is on top.</param>
    void SetBarTopmost(bool topmost);
}
