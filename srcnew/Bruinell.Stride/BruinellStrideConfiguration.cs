namespace Bruinell.Stride;

using System.Text.Json;
using Bruinell.Core.Configuration;

/// <summary>
/// Stride game engine test platform configuration.
/// Loads from bruinell.stride.config.json
/// </summary>
public sealed class BruinellStrideConfiguration : BruinellConfigurationBase
{
    /// <summary>
    /// Stride game-specific options.
    /// </summary>
    public StrideOptions Stride { get; set; } = new();

    /// <summary>
    /// Loads Stride configuration from bruinell.stride.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinellStrideConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinellStrideConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinellStrideConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinellStrideConfiguration>("bruinell.stride.config.json");
    }
}
