using System.Text.Json.Serialization;

namespace Brinell.Presenter.Services;

public sealed class PresenterUserSettings
{
    public string? LastOpenedFolder { get; set; }

    public List<string> RecentFolders { get; set; } = [];

    /// <summary>
    /// Serialized by name so the settings file stays readable. A file written before
    /// this member existed has no "Theme" property and loads as <see cref="PresenterTheme.System" />.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<PresenterTheme>))]
    public PresenterTheme Theme { get; set; } = PresenterTheme.System;
}
