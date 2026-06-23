namespace Brinell.Stride;

using System.Text.Json;
using Brinell.Core.Configuration;

/// <summary>
/// Stride game engine test platform configuration.
/// Loads from brinell.stride.config.json
/// </summary>
public sealed class BrinellStrideConfiguration : BrinellConfigurationBase
{
    /// <summary>
    /// Stride game-specific options.
    /// </summary>
    public StrideOptions Stride { get; set; } = new();

    // Artifacts inherited from BrinellConfigurationBase

    /// <summary>
    /// Loads Stride configuration from brinell.stride.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BrinellStrideConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BrinellStrideConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BrinellStrideConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BrinellStrideConfiguration>("brinell.stride.config.json");
    }
}
