namespace Brinell.Wpf;

using System.Text.Json;
using Brinell.Core.Configuration;

/// <summary>
/// WPF test platform configuration.
/// Loads from brinell.wpf.config.json
/// </summary>
public sealed class BrinellWpfConfiguration : BrinellConfigurationBase
{
    /// <summary>
    /// WPF platform-specific options (FlaUI driver configuration).
    /// </summary>
    public WpfOptions Wpf { get; set; } = new();

    // Artifacts inherited from BrinellConfigurationBase

    /// <summary>
    /// Loads WPF configuration from brinell.wpf.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BrinellWpfConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BrinellWpfConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BrinellWpfConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BrinellWpfConfiguration>("brinell.wpf.config.json");
    }
}
