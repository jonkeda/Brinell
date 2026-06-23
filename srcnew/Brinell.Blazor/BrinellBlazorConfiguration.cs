namespace Brinell.Blazor;

using System.Text.Json;
using Brinell.Core.Configuration;

/// <summary>
/// Blazor test platform configuration.
/// Loads from brinell.blazor.config.json
/// </summary>
public sealed class BrinellBlazorConfiguration : BrinellConfigurationBase
{
    /// <summary>
    /// Blazor application-specific options.
    /// </summary>
    public BlazorOptions Blazor { get; set; } = new();

    /// <summary>
    /// Browser configuration for Playwright automation.
    /// </summary>
    public BrowserOptions Browser { get; set; } = new();

    // Artifacts inherited from BrinellConfigurationBase

    /// <summary>
    /// Loads Blazor configuration from brinell.blazor.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BrinellBlazorConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BrinellBlazorConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BrinellBlazorConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BrinellBlazorConfiguration>("brinell.blazor.config.json");
    }
}
