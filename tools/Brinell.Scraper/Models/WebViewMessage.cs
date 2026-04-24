using System.Text.Json.Serialization;

namespace Brinell.Scraper.Models;

public sealed class WebViewMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("dataTestId")]
    public string? DataTestId { get; set; }

    [JsonPropertyName("ariaLabel")]
    public string? AriaLabel { get; set; }

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }

    [JsonPropertyName("inIframe")]
    public bool InIframe { get; set; }

    [JsonPropertyName("boundingBox")]
    public WebViewBoundingBox? BoundingBox { get; set; }
}

public sealed class WebViewBoundingBox
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }
}
