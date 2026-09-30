using Brinell.Scraper.Projects;

namespace Brinell.Scraper.Tests.Projects;

public sealed class ExploreSettingsTests
{
    private const string Loosened = """
        # Scraper Config

        Recorded on the test tenant: deleting and saving are fine there.

        ## App

        | Field | Value |
        | --- | --- |
        | StartUrl | https://test.app.example/ |

        ## Explore

        | Field | Value |
        | --- | --- |
        | MaxPages | 20 |
        | AgentMinutes | 5 |
        | LoginUrl | /login |
        | ClickPolicy | not-unsafe |
        | BlockMethods | |
        | WebsocketApps | warn |

        ## Danger words

        | Category | On |
        | --- | --- |
        | delete | false |
        | update | false |

        ## Explore hosts

        | Host |
        | --- |
        | test.app.example |

        ## Never click

        | Match |
        | --- |
        | text: Empty cache |
        | selector: [data-testid=danger-zone] button |

        ## Always click

        | Match |
        | --- |
        | Filters |

        ## Never visit

        | Url |
        | --- |
        | /admin |

        ## Allow requests

        | Method | Url |
        | --- | --- |
        | post | /graphql |
        """;

    [Fact]
    public void Without_sections_the_careful_defaults_hold()
    {
        var explore = ScraperConfig.Parse("## App\n\n| Field | Value |\n| --- | --- |\n| StartUrl | http://localhost/ |\n").Explore;

        Assert.Equal((50, 4, 10, 500, 10), (explore.MaxPages, explore.MaxDepth, explore.MaxMinutes, explore.DelayMs, explore.AgentMinutes));
        Assert.Equal(ClickPolicy.Safe, explore.ClickPolicy);
        Assert.Equal(["POST", "PUT", "PATCH", "DELETE"], explore.BlockMethods);
        Assert.Equal(WebsocketApps.Links, explore.WebsocketApps);
        Assert.Equal(["localhost", "127.0.0.1"], explore.Hosts);
        Assert.All(ExploreSettings.Categories, c => Assert.True(explore.IsOn(c)));
    }

    [Fact]
    public void A_project_loosens_what_is_harmless_for_it()
    {
        var explore = ScraperConfig.Parse(Loosened).Explore;

        Assert.Equal((20, 5, "/login"), (explore.MaxPages, explore.AgentMinutes, explore.LoginUrl));
        Assert.Equal(ClickPolicy.NotUnsafe, explore.ClickPolicy);
        Assert.Empty(explore.BlockMethods);
        Assert.Equal(WebsocketApps.Warn, explore.WebsocketApps);
        Assert.False(explore.IsOn("delete"));
        Assert.False(explore.IsOn("update"));
        Assert.True(explore.IsOn("sign-out"));
        Assert.Equal(["test.app.example"], explore.Hosts);
        Assert.Equal([new ElementMatch("text", "Empty cache"), new ElementMatch("selector", "[data-testid=danger-zone] button")], explore.NeverClick);
        Assert.Equal([new ElementMatch("text", "Filters")], explore.AlwaysClick);
        Assert.Equal(["/admin"], explore.NeverVisit);
        Assert.Equal([new AllowedRequest("POST", "/graphql")], explore.AllowRequests);
    }

    [Theory]
    [InlineData("| ClickPolicy | careless |", "ClickPolicy is links, safe, not-unsafe or all")]
    [InlineData("| BlockMethods | POST, GET |", "not 'GET'")]
    [InlineData("| MaxPages | lots |", "not a whole number")]
    [InlineData("| Speed | fast |", "not a field of ## Explore")]
    public void A_wrong_setting_is_refused_with_its_line(string row, string message)
    {
        var e = Assert.Throws<FormatException>(() => ScraperConfig.Parse($"## Explore\n\n| Field | Value |\n| --- | --- |\n{row}\n"));

        Assert.Contains("Line 5", e.Message);
        Assert.Contains(message, e.Message);
    }

    [Fact]
    public void An_unknown_danger_category_is_refused()
    {
        var e = Assert.Throws<FormatException>(() => ScraperConfig.Parse("## Danger words\n\n| Category | On |\n| --- | --- |\n| logout | false |\n"));

        Assert.Contains("sign-out", e.Message);
    }
}

public sealed class ScraperConfigWriterTests
{
    private const string Config = "# Scraper Config\r\n\r\nOur notes stay.\r\n\r\n## App\r\n\r\n| Field | Value |\r\n| --- | --- |\r\n| StartUrl | http://localhost:5180/ |\r\n\r\n## Aliases\r\n\r\n| Url |\r\n| --- |\r\n| https://a.example/ |\r\n\r\nMore notes.\r\n";

    [Fact]
    public void A_row_is_added_to_its_table_and_nothing_else_changes()
    {
        var edit = ScraperConfigWriter.AddRow(Config, "Aliases", ["Url"], ["https://b.example/"]);

        Assert.True(edit.Changed);
        Assert.Equal(Config.Replace("| https://a.example/ |\r\n", "| https://a.example/ |\r\n| https://b.example/ |\r\n"), edit.Text);
        Assert.Equal(["https://a.example/", "https://b.example/"], ScraperConfig.Parse(edit.Text).Aliases);
    }

    [Fact]
    public void A_row_that_is_there_is_not_added_twice()
    {
        var edit = ScraperConfigWriter.AddRow(Config, "Aliases", ["Url"], ["https://a.example/"]);

        Assert.False(edit.Changed);
        Assert.Equal(Config, edit.Text);
    }

    [Fact]
    public void A_missing_section_is_added_at_the_end()
    {
        var edit = ScraperConfigWriter.AddRow(Config, "Explore hosts", ["Host"], ["test.app.example"]);

        Assert.EndsWith("More notes.\r\n\r\n## Explore hosts\r\n\r\n| Host |\r\n| --- |\r\n| test.app.example |\r\n", edit.Text);
        Assert.Equal(["test.app.example"], ScraperConfig.Parse(edit.Text).Explore.Hosts);
    }

    [Fact]
    public void A_field_is_set_in_place_or_added()
    {
        var set = ScraperConfigWriter.SetField(Config, "App", "StartUrl", "http://localhost:9000/");
        var added = ScraperConfigWriter.SetField(set.Text, "App", "Platform", "blazor");
        var explore = ScraperConfigWriter.SetField(added.Text, "Explore", "ClickPolicy", "links");

        var config = ScraperConfig.Parse(explore.Text);
        Assert.Equal(("http://localhost:9000/", "blazor", ClickPolicy.Links), (config.StartUrl, config.Platform, config.Explore.ClickPolicy));
        Assert.Contains("| StartUrl | http://localhost:9000/ |\r\n| Platform | blazor |\r\n\r\n## Aliases", explore.Text);
        Assert.Contains("Our notes stay.", explore.Text);
    }

    [Fact]
    public void A_row_is_removed_by_its_first_cell()
    {
        var edit = ScraperConfigWriter.RemoveRow(Config, "Aliases", "https://a.example/");

        Assert.True(edit.Changed);
        Assert.Empty(ScraperConfig.Parse(edit.Text).Aliases);
        Assert.False(ScraperConfigWriter.RemoveRow(Config, "Aliases", "https://nope/").Changed);
    }

    [Fact]
    public void A_value_that_would_break_the_table_is_refused()
    {
        Assert.Throws<ArgumentException>(() => ScraperConfigWriter.AddRow(Config, "Never click", ["Match"], ["text: a | b"]));
    }

    [Fact]
    public void A_new_config_reads_back()
    {
        var config = ScraperConfig.Parse(ScraperConfigWriter.New("http://localhost:5180/", "blazor"));

        Assert.Equal(("http://localhost:5180/", "blazor"), (config.StartUrl, config.Platform));
    }
}
