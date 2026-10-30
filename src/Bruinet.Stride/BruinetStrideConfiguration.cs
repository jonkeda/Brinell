namespace Bruinet.Stride;

using System.Text.Json;
using Bruinet.Core.Configuration;

/// <summary>
/// Stride game engine test platform configuration.
/// Loads from bruinet.stride.config.json
/// </summary>
public sealed class BruinetStrideConfiguration : BruinetConfigurationBase
{
    /// <summary>
    /// Stride game-specific options.
    /// </summary>
    public StrideOptions Stride { get; set; } = new();

    /// <summary>
    /// Loads Stride configuration from bruinet.stride.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinetStrideConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinetStrideConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinetStrideConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinetStrideConfiguration>("bruinet.stride.config.json");
    }
}
