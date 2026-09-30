using Brinell.Core.Scraping;
using Brinell.Scraper.Code;
using Brinell.Scraper.Projects;

namespace Brinell.Scraper.Tests.Code;

public sealed class PageDrafterTests : IDisposable
{
    private readonly string _solution = Path.Combine(Path.GetTempPath(), "brinell-scraper-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_solution)) Directory.Delete(_solution, recursive: true);
    }

    [Theory]
    [InlineData("/invoices/{id}/edit", "InvoiceEditPage")]
    [InlineData("/", "HomePage")]
    [InlineData("/index.html", "HomePage")]
    [InlineData("/pages/account.html", "AccountPage")]
    [InlineData("/user-settings", "UserSettingsPage")]
    [InlineData("/categories/{id}", "CategoryPage")]
    [InlineData("#/orders/{id}/lines", "OrderLinesPage")]
    [InlineData("/2024/report", "Page2024ReportPage")]
    [InlineData("/checkout-page", "CheckoutPage")]
    public void A_class_is_named_from_the_page_key(string key, string name)
    {
        Assert.Equal(name, PageDrafter.ClassName(key));
    }

    private static ScrapeNode N(int n, int p, string type, string? text = null, params (string Key, string Value)[] attributes) =>
        new() { Number = n, Parent = p, Type = type, Text = text, Bounds = new(0, 0, 10, 10), Attributes = attributes.ToDictionary(a => a.Key, a => a.Value) };

    [Fact]
    public void A_draft_has_a_member_per_stable_suggestion_from_every_state_and_lists_the_unstable_ones()
    {
        var folder = Path.Combine(_solution, "Shop.UITests");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "Shop.UITests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Combine(folder, ScraperConfig.FileName), "## App\n\n| Field | Value |\n| --- | --- |\n| StartUrl | https://shop.example/ |\n| Namespace | Shop.UITests.Pages |\n");
        var project = ScrapeProject.Open(folder);
        var session = project.StartSession("explore", new DateTimeOffset(2026, 9, 30, 9, 0, 0, TimeSpan.Zero));
        session.AddPage(new ScrapeSnapshot
        {
            Platform = "html", Location = "https://shop.example/orders/12", Title = "Order", CapturedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            Nodes = [N(1, 0, "body"), N(2, 1, "button", "Details", ("data-testid", "order-details"), ("type", "button")), N(3, 1, "div", null, ("class", "x")), N(4, 3, "button", null, ("type", "button"))],
        });
        // A second state of the page: a dialog with a close button.
        session.AddPage(new ScrapeSnapshot
        {
            Platform = "html", Location = "https://shop.example/orders/12", Title = "Order", CapturedAt = DateTimeOffset.UnixEpoch.AddMinutes(2),
            Nodes = [N(1, 0, "body"), N(2, 1, "button", "Details", ("data-testid", "order-details"), ("type", "button")), N(3, 1, "dialog"), N(4, 3, "button", "×", ("data-testid", "order-close"), ("type", "button"))],
        });
        var index = project.Index();

        var draft = PageDrafter.Draft(project, index, index.Page("/orders/{id}")!, "OrderPage");

        Assert.Equal(Path.Combine("Pages", "OrderPage.scraped.cs"), draft.Path);
        Assert.StartsWith("// <scraped page=\"/orders/{id}\" from=\"2026-09-30-explore/002-order.nodes.jsonl\" hash=\"", draft.Code);
        Assert.Contains("using Brinell.Html.Controls.Buttons;\nusing Brinell.Html.Interfaces;\nusing Brinell.Html.Pages;", draft.Code.ReplaceLineEndings("\n"));
        Assert.Contains("namespace Shop.UITests.Pages;", draft.Code);
        Assert.Contains("public sealed partial class OrderPage(IHtmlTestContext context) : HtmlPageObjectBase<OrderPage>(context)", draft.Code);
        Assert.Contains("    public ButtonControl<OrderPage> OrderDetails => new(this, Locator.ByDataTestId(\"order-details\"));", draft.Code);
        Assert.Contains("    public ButtonControl<OrderPage> OrderClose => new(this, Locator.ByDataTestId(\"order-close\"));", draft.Code);
        Assert.Equal(2, draft.Members.Count);
        Assert.Contains("    // Not drafted: no stable locator.", draft.Code);
        Assert.Contains("    // ButtonControl ", draft.Code);
        Assert.Empty(CodeGate.Syntax(draft.Code));
    }

    [Fact]
    public void A_member_is_never_named_as_the_base_class_s_members_or_the_class()
    {
        var folder = Path.Combine(_solution, "Shop.UITests");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, ScraperConfig.FileName), "## App\n\n| Field | Value |\n| --- | --- |\n| StartUrl | https://shop.example/ |\n");
        var project = ScrapeProject.Open(folder);
        project.StartSession("s", DateTimeOffset.UnixEpoch).AddPage(new ScrapeSnapshot
        {
            Platform = "html", Location = "https://shop.example/url", Title = "Url",
            Nodes = [N(1, 0, "body"), N(2, 1, "button", "x", ("data-testid", "url")), N(3, 1, "button", "y", ("data-testid", "url-page")), N(4, 1, "input", null, ("data-testid", "name"))],
        });
        var index = project.Index();

        var draft = PageDrafter.Draft(project, index, index.Pages[0], "UrlPage");

        Assert.Equal(["Url2", "UrlPage2", "Name2"], draft.Members.Select(m => m.Name));
    }
}
