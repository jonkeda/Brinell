namespace Bruinet.Wpf;

using System.Text.Json;
using Bruinet.Core.Configuration;

/// <summary>
/// WPF test platform configuration.
/// Loads from bruinet.wpf.config.json
/// </summary>
public sealed class BruinetWpfConfiguration : BruinetConfigurationBase
{
    /// <summary>
    /// WPF platform-specific options (FlaUI driver configuration).
    /// </summary>
    public WpfOptions Wpf { get; set; } = new();

    /// <summary>
    /// Loads WPF configuration from bruinet.wpf.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinetWpfConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinetWpfConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinetWpfConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinetWpfConfiguration>("bruinet.wpf.config.json");
    }
}
