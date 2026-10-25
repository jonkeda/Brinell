namespace Bruinell.Wpf;

using System.Text.Json;
using Bruinell.Core.Configuration;

/// <summary>
/// WPF test platform configuration.
/// Loads from bruinell.wpf.config.json
/// </summary>
public sealed class BruinellWpfConfiguration : BruinellConfigurationBase
{
    /// <summary>
    /// WPF platform-specific options (FlaUI driver configuration).
    /// </summary>
    public WpfOptions Wpf { get; set; } = new();

    /// <summary>
    /// Loads WPF configuration from bruinell.wpf.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinellWpfConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinellWpfConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinellWpfConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinellWpfConfiguration>("bruinell.wpf.config.json");
    }
}
