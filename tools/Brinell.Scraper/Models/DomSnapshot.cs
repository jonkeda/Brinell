namespace Brinell.Scraper.Models;

public sealed class DomSnapshot
{
    public string SiteName { get; set; } = "";
    public string PageName { get; set; } = "";
    public string PageUrl { get; init; } = "";
    public string PageTitle { get; init; } = "";
    public DateTimeOffset CapturedAt { get; init; }
    public DomElement RootElement { get; init; } = new();
    public List<DomElement> SelectedElements { get; init; } = [];
}
