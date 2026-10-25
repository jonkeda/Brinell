namespace Bruinell.WinForms;

using System.Text.Json;
using Bruinell.Core.Configuration;

/// <summary>
/// WinForms test platform configuration.
/// Loads from bruinell.winforms.config.json
/// </summary>
public sealed class BruinellWinFormsConfiguration : BruinellConfigurationBase
{
    /// <summary>
    /// WinForms platform-specific options (FlaUI driver configuration).
    /// </summary>
    public WinFormsOptions WinForms { get; set; } = new();

    /// <summary>
    /// Loads WinForms configuration from bruinell.winforms.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinellWinFormsConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinellWinFormsConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinellWinFormsConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinellWinFormsConfiguration>("bruinell.winforms.config.json");
    }
}
