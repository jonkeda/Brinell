namespace Brinell.Presenter.Services;

public sealed class PresenterUserSettings
{
    public string? LastOpenedFolder { get; set; }

    public List<string> RecentFolders { get; set; } = [];
}
