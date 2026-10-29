using Microsoft.Maui.Storage;

namespace MauiHeadless.Samples.App.Services;

/// <summary>Where the survivor's settings are kept.</summary>
public interface ISettingsService
{
    string? LoadName();

    bool LoadNotifications();

    Task SaveAsync(string name, bool notifications);
}

/// <summary>The settings in the device's preferences.</summary>
public sealed class PreferencesSettingsService(IPreferences preferences) : ISettingsService
{
    public string? LoadName() => preferences.Get<string?>("name", null);

    public bool LoadNotifications() => preferences.Get("notifications", true);

    public Task SaveAsync(string name, bool notifications)
    {
        preferences.Set("name", name);
        preferences.Set("notifications", notifications);
        return Task.CompletedTask;
    }
}
