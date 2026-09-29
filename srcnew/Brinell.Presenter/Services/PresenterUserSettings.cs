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

    /// <summary>The side the Presenter bar docks to; the app under test takes the rest of the screen.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<PresenterDock>))]
    public PresenterDock Dock { get; set; } = PresenterDock.Left;

    /// <summary>The pace a run goes at: the Speed button's choice.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<PresenterSpeed>))]
    public PresenterSpeed Speed { get; set; } = PresenterSpeed.Normal;

    /// <summary>The pause between steps of each speed, set on the Settings page.</summary>
    public PresenterSpeeds Speeds { get; set; } = new();

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

/// <summary>A run's pace: how long it waits between steps.</summary>
public enum PresenterSpeed
{
    Fast = 0,
    Normal = 1,
    Slow = 2,
}

/// <summary>The pause between steps of each speed, in milliseconds.</summary>
public sealed class PresenterSpeeds
{
    public int Fast { get; set; } = 250;

    public int Normal { get; set; } = 800;

    public int Slow { get; set; } = 1500;

    /// <summary>The pause of a speed.</summary>
    public int Of(PresenterSpeed speed) => speed switch
    {
        PresenterSpeed.Fast => Fast,
        PresenterSpeed.Slow => Slow,
        _ => Normal,
    };
}