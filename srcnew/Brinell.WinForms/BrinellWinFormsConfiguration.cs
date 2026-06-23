namespace Brinell.WinForms;

using System.Text.Json;
using Brinell.Core.Configuration;

/// <summary>
/// WinForms test platform configuration.
/// Loads from brinell.winforms.config.json
/// </summary>
public sealed class BrinellWinFormsConfiguration : BrinellConfigurationBase
{
    /// <summary>
    /// WinForms platform-specific options (FlaUI driver configuration).
    /// </summary>
    public WinFormsOptions WinForms { get; set; } = new();

    // Artifacts inherited from BrinellConfigurationBase

    /// <summary>
    /// Loads WinForms configuration from brinell.winforms.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BrinellWinFormsConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BrinellWinFormsConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BrinellWinFormsConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BrinellWinFormsConfiguration>("brinell.winforms.config.json");
    }
}
