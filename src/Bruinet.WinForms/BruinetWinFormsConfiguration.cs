namespace Bruinet.WinForms;

using System.Text.Json;
using Bruinet.Core.Configuration;

/// <summary>
/// WinForms test platform configuration.
/// Loads from bruinet.winforms.config.json
/// </summary>
public sealed class BruinetWinFormsConfiguration : BruinetConfigurationBase
{
    /// <summary>
    /// WinForms platform-specific options (FlaUI driver configuration).
    /// </summary>
    public WinFormsOptions WinForms { get; set; } = new();

    /// <summary>
    /// Loads WinForms configuration from bruinet.winforms.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinetWinFormsConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinetWinFormsConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinetWinFormsConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinetWinFormsConfiguration>("bruinet.winforms.config.json");
    }
}
