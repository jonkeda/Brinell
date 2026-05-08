namespace Brinell.Scraper.Models;

public sealed class PageObjectPropertyItem
{
    public string Name { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string Locator { get; set; } = "";
    public bool IsCustomControlObject { get; set; }
}
