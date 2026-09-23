namespace Brinell.Presenter.Services;

/// <summary>
/// The theme the user has chosen, as persisted in <see cref="PresenterUserSettings" />.
/// <see cref="System" /> is the first-launch default; the toggle button flips between
/// <see cref="Light" /> and <see cref="Dark" /> and never returns to <see cref="System" />.
/// </summary>
public enum PresenterTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}
