namespace Brinell.Html;

using System.Text.Json;
using Brinell.Core.Configuration;

/// <summary>
/// HTML/Playwright test platform configuration.
/// Loads from brinell.html.config.json
/// </summary>
public sealed class BrinellHtmlConfiguration : BrinellConfigurationBase
{
    /// <summary>
    /// HTML application-specific options.
    /// </summary>
    public HtmlOptions Html { get; set; } = new();

    /// <summary>
    /// Browser configuration for Playwright automation.
    /// </summary>
    public BrowserOptions Browser { get; set; } = new();

    // Artifacts inherited from BrinellConfigurationBase

    /// <summary>
    /// Loads HTML configuration from brinell.html.config.json
    /// </summary>
    /// <param name="configPath">Optional custom config file path. If null, uses default location.</param>
    /// <returns>Loaded configuration or defaults if file not found.</returns>
    public static BrinellHtmlConfiguration Load(string? configPath = null)
    {
        if (configPath != null)
        {
            try
            {
                return JsonSerializer.Deserialize<BrinellHtmlConfiguration>(
                    File.ReadAllText(configPath))
                ?? new BrinellHtmlConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse: {configPath}", ex);
            }
        }

        return LoadFromJson<BrinellHtmlConfiguration>("brinell.html.config.json");
    }
}
