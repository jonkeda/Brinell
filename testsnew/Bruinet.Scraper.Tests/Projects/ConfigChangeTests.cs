using Bruinet.Scraper.Projects;

namespace Bruinet.Scraper.Tests.Projects;

public sealed class ConfigChangeTests
{
    private const string Config = "# Scraper Config\r\n\r\nOur test tenant.\r\n\r\n## App\r\n\r\n| Field | Value |\r\n| --- | --- |\r\n| StartUrl | https://test.app.example/ |\r\n\r\n## Danger words\r\n\r\n| Category | On |\r\n| --- | --- |\r\n| delete | false |\r\n";

    [Fact]
    public void A_field_is_set_in_its_section_added_when_missing()
    {
        var edit = ScraperConfigWriter.Apply(Config, new ConfigChange("set", "Explore", ["ClickPolicy", "not-unsafe"]));

        Assert.True(edit.Changed);
        Assert.EndsWith("| delete | false |\r\n\r\n## Explore\r\n\r\n| Field | Value |\r\n| --- | --- |\r\n| ClickPolicy | not-unsafe |\r\n", edit.Text);
        Assert.Equal(ClickPolicy.NotUnsafe, ScraperConfig.Parse(edit.Text).Explore.ClickPolicy);
    }

    [Fact]
    public void A_danger_word_category_is_set_by_replacing_its_row()
    {
        var on = ScraperConfigWriter.Apply(Config, new ConfigChange("set", "Danger words", ["delete", "true"]));
        var off = ScraperConfigWriter.Apply(Config, new ConfigChange("set", "Danger words", ["update", "false"]));

        Assert.True(ScraperConfig.Parse(on.Text).Explore.IsOn("delete"));
        Assert.DoesNotContain("| delete | false |", on.Text);
        Assert.False(ScraperConfig.Parse(off.Text).Explore.IsOn("update"));
        Assert.False(ScraperConfig.Parse(off.Text).Explore.IsOn("delete"));
        Assert.StartsWith("# Scraper Config\r\n\r\nOur test tenant.", off.Text);
    }

    [Fact]
    public void A_row_is_added_in_the_section_s_own_columns_and_removed_by_its_first_cell()
    {
        var added = ScraperConfigWriter.Apply(Config, new ConfigChange("add", "Allow requests", ["POST", "/graphql"]));
        var removed = ScraperConfigWriter.Apply(added.Text, new ConfigChange("remove", "Allow requests", ["POST"]));

        Assert.Equal(new AllowedRequest("POST", "/graphql"), Assert.Single(ScraperConfig.Parse(added.Text).Explore.AllowRequests));
        Assert.Empty(ScraperConfig.Parse(removed.Text).Explore.AllowRequests);
    }

    [Theory]
    [InlineData("add", "Never click", 2)]
    [InlineData("add", "App", 1)]
    [InlineData("move", "Explore", 2)]
    [InlineData("remove", "Explore hosts", 2)]
    public void What_is_not_a_change_is_refused(string action, string section, int values)
    {
        var e = Assert.Throws<ArgumentException>(() => ScraperConfigWriter.Apply(Config, new ConfigChange(action, section, [.. Enumerable.Repeat("x", values)])));

        Assert.Contains("is not a change", e.Message);
    }
}
