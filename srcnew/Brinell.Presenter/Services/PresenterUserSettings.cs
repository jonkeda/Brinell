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

    /// <summary>
    /// The build configuration and platform chosen per workspace folder.
    /// </summary>
    /// <remarks>
    /// Session state, not config: a configuration and a platform are things you switch while
    /// demoing, so writing them into <c>uat.config.md</c> would mean editing the file to show
    /// the other one.
    /// </remarks>
    public Dictionary<string, PresenterWorkspacePreferences> WorkspacePreferences { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>What one workspace was last run as.</summary>
public sealed class PresenterWorkspacePreferences
{
    /// <summary>The build configuration, such as <c>Debug</c>.</summary>
    public string Configuration { get; set; } = "Debug";

    /// <summary>The platform, as <c>APPIUM_PLATFORM</c> takes it.</summary>
    public string Platform { get; set; } = "windows";
}
