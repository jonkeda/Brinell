using Xunit;

namespace Brinell.Scraper.Tests.Services;

using Brinell.Scraper.Services;

public sealed class AnalysisResultParserTests
{
    [Fact]
    public void Parse_JsonInCodeFence_ReturnsProposals()
    {
        var input = """
            Based on my analysis of the corpus, here are the detected patterns:

            ```json
            {
              "proposedControls": [
                {
                  "name": "DatePickerControl",
                  "domSignature": "div.date-picker > input + button.calendar",
                  "frequency": 8,
                  "confidence": 94,
                  "exampleSnippet": "<div class=\"date-picker\"><input type=\"date\" /></div>",
                  "suggestedProperties": ["DateInput", "CalendarButton"]
                },
                {
                  "name": "SearchBarControl",
                  "domSignature": "div.search-bar > input[type=text] + button",
                  "frequency": 5,
                  "confidence": 87,
                  "exampleSnippet": "<div class=\"search-bar\"><input type=\"text\" /></div>",
                  "suggestedProperties": ["SearchInput", "SearchButton"]
                }
              ]
            }
            ```

            These patterns appear consistently across multiple pages.
            """;

        var result = AnalysisResultParser.Parse(input);

        Assert.Equal(2, result.ProposedControls.Count);
        Assert.Equal("DatePickerControl", result.ProposedControls[0].Name);
        Assert.Equal(8, result.ProposedControls[0].Frequency);
        Assert.Equal(94, result.ProposedControls[0].Confidence);
        Assert.Equal("SearchBarControl", result.ProposedControls[1].Name);
        Assert.False(result.ProposedControls[0].IsApproved);
    }

    [Fact]
    public void Parse_RawJson_ReturnsProposals()
    {
        var input = """
            {
              "proposedControls": [
                {
                  "name": "DatePickerControl",
                  "domSignature": "div.date-picker",
                  "frequency": 3,
                  "confidence": 80,
                  "exampleSnippet": "<div class=\"date-picker\" />",
                  "suggestedProperties": ["DateInput"]
                }
              ]
            }
            """;

        var result = AnalysisResultParser.Parse(input);

        Assert.Single(result.ProposedControls);
        Assert.Equal("DatePickerControl", result.ProposedControls[0].Name);
    }

    [Fact]
    public void Parse_WithLocatorReport_ReturnsReport()
    {
        var input = """
            ```json
            {
              "proposedControls": [],
              "locatorReport": {
                "stableAttributes": ["data-testid", "aria-label"],
                "unstableAttributes": ["id (dynamic on 8/15 pages)"],
                "recommendations": "Prefer ByText() and ByDataTestId(). Avoid ById() on Dashboard."
              }
            }
            ```
            """;

        var result = AnalysisResultParser.Parse(input);

        Assert.NotNull(result.LocatorReport);
        Assert.Equal(2, result.LocatorReport!.StableAttributes.Count);
        Assert.Contains("data-testid", result.LocatorReport.StableAttributes);
        Assert.Single(result.LocatorReport.UnstableAttributes);
        Assert.Contains("Prefer ByText()", result.LocatorReport.Recommendations);
    }

    [Fact]
    public void Parse_MalformedResponse_ReturnsEmptyResult()
    {
        var input = """
            I analyzed the corpus and found several interesting patterns.
            The site uses a mix of semantic HTML and custom components.
            I recommend starting with the DatePicker and SearchBar controls.
            """;

        var result = AnalysisResultParser.Parse(input);

        Assert.Empty(result.ProposedControls);
        Assert.Null(result.LocatorReport);
    }
}
