namespace Bruinell.Blazor;

using System.Text.Json;
using Bruinell.Core.Configuration;

/// <summary>
/// Blazor test platform configuration.
/// Loads from bruinell.blazor.config.json
/// </summary>
public sealed class BruinellBlazorConfiguration : BruinellConfigurationBase
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
    /// Loads Blazor configuration from bruinell.blazor.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinellBlazorConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinellBlazorConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinellBlazorConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinellBlazorConfiguration>("bruinell.blazor.config.json");
    }
}
