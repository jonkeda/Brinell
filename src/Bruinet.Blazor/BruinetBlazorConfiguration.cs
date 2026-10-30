namespace Bruinet.Blazor;

using System.Text.Json;
using Bruinet.Core.Configuration;

/// <summary>
/// Blazor test platform configuration.
/// Loads from bruinet.blazor.config.json
/// </summary>
public sealed class BruinetBlazorConfiguration : BruinetConfigurationBase
{
    /// <summary>
    /// Blazor application-specific options.
    /// </summary>
    public BlazorOptions Blazor { get; set; } = new();

    /// <summary>
    /// Browser configuration for Playwright automation.
    /// </summary>
    public BrowserOptions Browser { get; set; } = new();

    /// <summary>
    /// Loads Blazor configuration from bruinet.blazor.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinetBlazorConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinetBlazorConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinetBlazorConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinetBlazorConfiguration>("bruinet.blazor.config.json");
    }
}
