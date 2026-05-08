using System.Text.Json;
using System.Text.RegularExpressions;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public static class AnalysisResultParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static AnalysisResult Parse(string llmResponse)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
            return new AnalysisResult();

        var json = ExtractJson(llmResponse);

        if (json is null)
            return new AnalysisResult();

        try
        {
            return JsonSerializer.Deserialize<AnalysisResult>(json, JsonOptions)
                ?? new AnalysisResult();
        }
        catch (JsonException)
        {
            return new AnalysisResult();
        }
    }

    public static (List<ControlProposal> Proposals, LocatorReport? LocatorReport)
        ParseControlObjectAnalysis(string llmResponse)
    {
        var result = Parse(llmResponse);
        return (result.ProposedControls, result.LocatorReport);
    }

    private static string? ExtractJson(string response)
    {
        var match = Regex.Match(response, @"```json\s*\n(.*?)```",
            RegexOptions.Singleline);
        if (match.Success)
            return match.Groups[1].Value.Trim();

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
            return response[start..(end + 1)];

        return null;
    }
}
