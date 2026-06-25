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
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BrinellMauiConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
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

        return LoadFromJson<BrinellMauiConfiguration>("brinell.maui.config.json");
    }
}
