using System.Text.Json.Nodes;
using Brinell.Core.Scraping;
using Brinell.Scraper.Projects;
using Xunit;

namespace Brinell.Bdd.LanguageServer.Tests;

/// <summary>The Scraper's MCP tools over a recording made beforehand: no browser.</summary>
public sealed class ScraperMcpTests : IDisposable
{
    private readonly string _solution = Path.Combine(Path.GetTempPath(), "brinell-scraper-mcp", Guid.NewGuid().ToString("N"));
    private readonly BddMcpServer _server;

    public ScraperMcpTests()
    {
        var folder = Path.Combine(_solution, "Shop.UITests");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "Shop.UITests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Combine(folder, ScraperConfig.FileName), "## App\n\n| Field | Value |\n| --- | --- |\n| StartUrl | https://shop.example/ |\n");
        var session = ScrapeProject.Open(folder).StartSession("Orders", new DateTimeOffset(2026, 9, 30, 9, 0, 0, TimeSpan.Zero));
        session.AddPage(Orders("https://shop.example/orders", "Ada", "Grace"));
        session.AddPage(Orders("https://shop.example/archive", "Linus"));
        _server = new BddMcpServer(_solution);
    }

    public void Dispose()
    {
        _server.Dispose();
        Directory.Delete(_solution, recursive: true);
    }

    private static ScrapeSnapshot Orders(string location, params string[] customers)
    {
        var nodes = new List<ScrapeNode>
        {
            new() { Number = 1, Parent = 0, Type = "table", Attributes = new Dictionary<string, string> { ["data-testid"] = "orders" } },
        };
        foreach (var customer in customers)
        {
            var row = nodes.Count + 1;
            nodes.Add(new() { Number = row, Parent = 1, Type = "tr", Attributes = new Dictionary<string, string> { ["class"] = "order-row" } });
            nodes.Add(new() { Number = row + 1, Parent = row, Type = "td", Text = customer });
        }
        return new ScrapeSnapshot { Platform = "html", Location = location, Title = "Orders", Nodes = nodes };
    }

    [Fact]
    public void The_projects_and_their_recorded_pages_are_listed()
    {
        Assert.Contains("\"name\":\"Shop.UITests\"", CallText("list_scraped_projects", []));
        var pages = CallText("list_recorded_pages", []);

        Assert.Contains("\"key\":\"/orders\"", pages);
        Assert.Contains("\"key\":\"/archive\"", pages);
    }

    [Fact]
    public void A_page_comes_as_an_outline_and_a_node_s_kind_is_found_on_every_page()
    {
        var outline = CallText("get_page_snapshot", new JsonObject { ["page"] = "/orders" });
        var similar = CallText("find_similar_elements", new JsonObject { ["page"] = "/orders", ["node"] = 2 });
        var found = CallText("search_recordings", new JsonObject { ["text"] = "grace" });

        Assert.StartsWith("#1 table [data-testid=orders]\n  #2 tr .order-row\n    #3 td \"Ada\"", outline);
        Assert.Equal(3, JsonNode.Parse(similar)!.AsArray().Count); // two rows on /orders, one on /archive
        Assert.Contains("\\\"Grace\\\"", found);
    }

    [Fact]
    public async Task The_view_reads_the_same_over_json_rpc()
    {
        await using var harness = await ServerHarness.StartAsync(_solution);

        var projects = await harness.Client.InvokeWithParameterObjectAsync<JsonNode>("brinell/scraper/projects", new { });
        var pages = await harness.Client.InvokeWithParameterObjectAsync<JsonNode>("brinell/scraper/pages", new { project = "Shop.UITests" });
        var outline = await harness.Client.InvokeWithParameterObjectAsync<JsonNode>("brinell/scraper/outline", new { page = "/archive" });
        var capture = await harness.Client.InvokeWithParameterObjectAsync<JsonNode>("brinell/scraper/capture", new { });
        var host = await harness.Client.InvokeWithParameterObjectAsync<JsonNode>("brinell/scraper/host", new { });
        var similar = await harness.Client.InvokeWithParameterObjectAsync<JsonNode>("brinell/scraper/similar", new { page = "/orders", node = 2 });
        var header = await harness.Client.InvokeWithParameterObjectAsync<JsonNode>("brinell/scraper/header", new { page = "/orders" });

        Assert.Contains("<title>Scraper</title>", (string?)host["html"]);
        Assert.Equal(3, similar["value"]!.AsArray().Count);
        Assert.True(similar.AsObject().ContainsKey("problem")); // sent even as null: the view tells a result by it
        Assert.StartsWith("// <scraped page=\"/orders\"", (string?)header["value"]);

        Assert.Equal("Shop.UITests", (string?)projects[0]!["name"]);
        Assert.Equal(["/archive", "/orders"], pages["value"]!.AsArray().Select(page => (string?)page!["key"]));
        Assert.Contains("#3 td \"Linus\"", (string?)outline["value"]);
        Assert.Contains("No recording is in progress", (string?)capture["problem"]);
    }

    [Fact]
    public void What_cannot_be_done_is_a_tool_error_saying_why()
    {
        Assert.Contains("No recording is in progress", ErrorText("capture_page", []));
        Assert.Contains("No recorded page '/nope'", ErrorText("get_page_snapshot", new JsonObject { ["page"] = "/nope" }));
        Assert.Contains("'session' is required", ErrorText("start_recording", []));
    }

    private string CallText(string tool, JsonObject arguments)
    {
        var result = Call(tool, arguments);
        Assert.False((bool)result["isError"]!, (string?)result["content"]![0]!["text"]);
        return (string)result["content"]![0]!["text"]!;
    }

    private string ErrorText(string tool, JsonObject arguments)
    {
        var result = Call(tool, arguments);
        Assert.True((bool)result["isError"]!);
        return (string)result["content"]![0]!["text"]!;
    }

    private JsonNode Call(string tool, JsonObject arguments)
        => _server.Handle(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 7,
            ["method"] = "tools/call",
            ["params"] = new JsonObject { ["name"] = tool, ["arguments"] = arguments },
        }.ToJsonString())!["result"]!;
}
