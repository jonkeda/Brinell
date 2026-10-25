namespace Bruinell.Html;

using System.Text.Json;
using Bruinell.Core.Configuration;

/// <summary>
/// HTML/Playwright test platform configuration.
/// Loads from bruinell.html.config.json
/// </summary>
public sealed class BruinellHtmlConfiguration : BruinellConfigurationBase
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
    /// Loads HTML configuration from bruinell.html.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BruinellHtmlConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BruinellHtmlConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BruinellHtmlConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BruinellHtmlConfiguration>("bruinell.html.config.json");
    }
}
