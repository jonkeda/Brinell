namespace Brinell.Maui.Configuration;

using System.Text.Json;
using Brinell.Core.Configuration;

/// <summary>
/// MAUI test platform configuration.
/// Loads from brinell.maui.config.json
/// </summary>
public class BrinellMauiConfiguration : BrinellConfigurationBase
{
    /// <summary>
    /// Framework-level options (Windows interaction, automation, etc.).
    /// </summary>
    public FrameworkOptions Framework { get; set; } = new();

    /// <summary>
    /// MAUI platform-specific options (Appium/FlaUI driver configuration).
    /// </summary>
    public MauiOptions Maui { get; set; } = new();

    // Artifacts inherited from BrinellConfigurationBase

    /// <summary>
    /// Loads MAUI configuration from brinell.maui.config.json
    /// </summary>
    /// <remarks>
    /// The <c>APPIUM_PLATFORM</c> environment variable overrides the platform from the file.
    /// The same test assembly is run against Windows, Android and iOS, so which platform a run
    /// targets is a property of the run rather than of the checked-in configuration — and a CI
    /// job or a local script can set it without editing a file.
    /// </remarks>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BrinellMauiConfiguration Load(string? configPath = null)
    {
        var configuration = LoadCore(configPath);

        ApplyPlatformOverride(configuration);

        return configuration;
    }

    private static BrinellMauiConfiguration LoadCore(string? configPath)
    {
        if (configPath == null)
        {
            return LoadFromJson<BrinellMauiConfiguration>("brinell.maui.config.json");
        }

        try
        {
            return JsonSerializer.Deserialize<BrinellMauiConfiguration>(
                File.ReadAllText(configPath))
            ?? new BrinellMauiConfiguration();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
        }
    }

    /// <summary>
    /// Applies the <c>APPIUM_PLATFORM</c> override, when set.
    /// </summary>
    /// <remarks>
    /// An unrecognized value throws rather than silently falling back to Windows: a typo would
    /// otherwise start a FlaUI run on a machine expecting Android and fail much later with an
    /// unrelated message about a missing assembly.
    /// </remarks>
    private static void ApplyPlatformOverride(BrinellMauiConfiguration configuration)
    {
        var requested = Environment.GetEnvironmentVariable("APPIUM_PLATFORM");
        if (string.IsNullOrWhiteSpace(requested)) return;

        configuration.Maui.Platform = requested.Trim().ToLowerInvariant() switch
        {
            "windows" => MauiPlatform.Windows,
            "android" => MauiPlatform.Android,
            "ios" => MauiPlatform.iOS,
            _ => throw new InvalidOperationException(
                $"APPIUM_PLATFORM has unrecognized value '{requested}'. " +
                "Expected 'windows', 'android', or 'ios'.")
        };
    }
}
