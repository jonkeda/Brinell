namespace Bruinet.Html;

using System.Text.Json;
using Bruinet.Core.Configuration;

/// <summary>
/// HTML/Playwright test platform configuration.
/// Loads from bruinet.html.config.json
/// </summary>
public sealed class BruinetHtmlConfiguration : BruinetConfigurationBase
{
    /// <summary>
    /// HTML application-specific options.
    /// </summary>
    public HtmlOptions Html { get; set; } = new();

    /// <summary>
    /// Browser configuration for Playwright automation.
    /// </summary>
    public BrowserOptions Browser { get; set; } = new();

    /// <summary>
    /// Loads HTML configuration from bruinet.html.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinetHtmlConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinetHtmlConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinetHtmlConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinetHtmlConfiguration>("bruinet.html.config.json");
    }
}
