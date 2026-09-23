using System.Text.Json;

namespace Brinell.Presenter.Services;

public sealed class PresenterUserSettingsService : IPresenterUserSettingsService
{
    private const int MaxRecentFolders = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public PresenterUserSettingsService()
        : this(GetDefaultSettingsPath())
    {
    }

    public PresenterUserSettingsService(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    public PresenterUserSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new PresenterUserSettings();
        }

        try
        {
            using var stream = File.OpenRead(_settingsPath);
            return JsonSerializer.Deserialize<PresenterUserSettings>(stream, JsonOptions)
                   ?? new PresenterUserSettings();
        }
        catch
        {
            return new PresenterUserSettings();
        }
    }

    public void Save(PresenterUserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(_settingsPath);
        JsonSerializer.Serialize(stream, settings, JsonOptions);
    }

    public PresenterUserSettings RecordOpenedFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var fullPath = Path.GetFullPath(folderPath);
        var settings = Load();
        settings.LastOpenedFolder = fullPath;
        settings.RecentFolders = settings.RecentFolders
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Where(path => !path.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
            .Prepend(fullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxRecentFolders)
            .ToList();

        Save(settings);
        return settings;
    }

    private static string GetDefaultSettingsPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("BRINELL_PRESENTER_SETTINGS_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Brinell.Presenter", "user-settings.json");
    }
}
