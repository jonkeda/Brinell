using System.Text;
using System.Text.RegularExpressions;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public sealed class PromptBuilder
{
    public string BuildPageObjectPrompt(
        DomSnapshot snapshot,
        IReadOnlyList<DomElement> actionable,
        IReadOnlyList<ControlGroupSuggestion>? containerGroups,
        IReadOnlyList<ControlObjectMatch> matches,
        IReadOnlyList<GeneratedControl> registeredControls,
        LocatorReport? locatorReport,
        string targetNamespace)
    {
        var className = DeriveClassName(snapshot.PageName);
        var sb = new StringBuilder();

        sb.AppendLine("Generate a Brinell page object class with the following details:");
        sb.AppendLine();
        sb.AppendLine($"Class Name: {className}");
        sb.AppendLine($"Namespace: {targetNamespace}");
        sb.AppendLine($"Page URL: {snapshot.PageUrl}");
        sb.AppendLine($"Page Title: {snapshot.PageTitle}");
        sb.AppendLine();

        sb.AppendLine("## Available Custom Controls");
        sb.AppendLine();
        if (registeredControls.Count > 0)
        {
            sb.AppendLine("These site-specific ControlObjects are registered. Prefer them over inline " +
                "containers whenever a DOM pattern matches:");
            sb.AppendLine();
            foreach (var ctrl in registeredControls)
            {
                sb.AppendLine($"- **{ctrl.Name}** — signature: `{ctrl.DomSignature}`");
                var props = ExtractControlPropertyNames(ctrl.Code);
                if (props.Count > 0)
                    sb.AppendLine($"  - Properties: {string.Join(", ", props)}");
            }
        }
        else
        {
            sb.AppendLine("(none — generate inline containers for repeated patterns)");
        }
        sb.AppendLine();

        sb.AppendLine("## Pre-Computed Matches");
        sb.AppendLine();
        if (matches.Count > 0)
        {
            sb.AppendLine("The matcher already identified these elements as instances of registered " +
                "ControlObjects. Use the named control directly:");
            sb.AppendLine();
            foreach (var m in matches)
                sb.AppendLine($"- Use `{m.Control.Name}` for element `{m.XPath}` ({m.Reason})");
        }
        else
        {
            sb.AppendLine("(no pre-computed matches)");
        }
        sb.AppendLine();

        if (locatorReport is not null)
        {
            sb.AppendLine("## Site-Specific Patterns");
            sb.AppendLine();
            sb.AppendLine($"Stable attributes: {string.Join(", ", locatorReport.StableAttributes)}");
            sb.AppendLine($"Unstable attributes: {string.Join(", ", locatorReport.UnstableAttributes)}");
            sb.AppendLine($"Recommendations: {locatorReport.Recommendations}");
            sb.AppendLine();
        }

        sb.AppendLine("## Locator Preference Order");
        sb.AppendLine();
        sb.AppendLine("Use locators in this order: ByText > ByDataTestId > ByAriaLabel > ById > ByCss. " +
            "ByCss is a last resort.");
        sb.AppendLine();

        sb.AppendLine("## Allowed Control Types");
        sb.AppendLine();
        sb.AppendLine("Built-in: TextInputControl, ButtonControl, SelectControl, LabelControl, " +
            "CheckBoxControl, RadioButtonControl, LinkControl, FileInputControl, TextAreaControl, " +
            "ImageControl, ElementControl.");
        if (registeredControls.Count > 0)
        {
            sb.AppendLine("Custom (from registry): " +
                string.Join(", ", registeredControls.Select(c => c.Name)) + ".");
        }
        sb.AppendLine("Do NOT invent control types outside this whitelist.");
        sb.AppendLine();

        sb.AppendLine("## Page Elements");
        sb.AppendLine();
        sb.AppendLine("```html");
        foreach (var el in actionable)
            CorpusTools.FormatElement(sb, el, indent: 0);
        sb.AppendLine("```");
        sb.AppendLine();

        if (containerGroups is { Count: > 0 })
        {
            sb.AppendLine("## Container Group Suggestions");
            sb.AppendLine();
            sb.AppendLine("Generate these as inline ContainerBase<" + className + ", TContainer> " +
                "classes only when no registered ControlObject matches:");
            sb.AppendLine();
            foreach (var group in containerGroups)
            {
                sb.AppendLine($"### Group \"{group.DisplayName}\" (root: `{group.ContainerType}`)");
                sb.AppendLine();
                sb.AppendLine("```html");
                CorpusTools.FormatElement(sb, group.Element, indent: 0);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Output Requirements");
        sb.AppendLine();
        sb.AppendLine($"- Emit a sealed class `{className}` deriving from `HtmlPageObjectBase<{className}>`.");
        sb.AppendLine("- Use expression-bodied properties for each control.");
        sb.AppendLine("- For each pre-computed match above, use the named ControlObject directly.");
        sb.AppendLine("- Emit each inline container as a separate ```csharp code block deriving from " +
            $"`ContainerBase<{className}, TContainer>`.");
        sb.AppendLine("- The first code block must contain the page class; subsequent code blocks " +
            "contain inline containers.");

        return sb.ToString();
    }

    private static List<string> ExtractControlPropertyNames(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return [];
        var names = new List<string>();
        foreach (Match m in System.Text.RegularExpressions.Regex.Matches(
            code, @"public\s+\w[\w<>,\s]*\s+(\w+)\s*=>"))
        {
            names.Add(m.Groups[1].Value);
        }
        return names;
    }

    private static string DeriveClassName(string pageName)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(pageName, @"[^a-zA-Z0-9]", "");
        if (string.IsNullOrEmpty(cleaned))
            cleaned = "Page";
        if (!cleaned.EndsWith("Page", StringComparison.Ordinal))
            cleaned += "Page";
        return cleaned;
    }

    public string BuildPagePrompt(
        string className,
        string namespaceName,
        string pageUrl,
        string pageTitle,
        IReadOnlyList<DomElement> selectedElements,
        IReadOnlyList<GeneratedControl> customControls,
        LocatorReport? locatorReport = null,
        IReadOnlyList<ControlGroupSuggestion>? containerGroups = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Generate a Brinell page object class with the following details:");
        sb.AppendLine();
        sb.AppendLine($"Class Name: {className}");
        sb.AppendLine($"Namespace: {namespaceName}");
        sb.AppendLine($"Page URL: {pageUrl}");
        sb.AppendLine($"Page Title: {pageTitle}");
        sb.AppendLine();

        if (customControls.Count > 0)
        {
            sb.AppendLine("## Available Custom Controls");
            sb.AppendLine();
            sb.AppendLine("Use these site-specific controls when their DOM patterns are detected:");
            sb.AppendLine();
            foreach (var ctrl in customControls)
            {
                sb.AppendLine($"- **{ctrl.Name}** — matches: `{ctrl.DomSignature}`");
            }
            sb.AppendLine();
        }

        if (locatorReport is not null)
        {
            sb.AppendLine("## Site-Specific Patterns");
            sb.AppendLine();
            sb.AppendLine($"Stable attributes: {string.Join(", ", locatorReport.StableAttributes)}");
            sb.AppendLine($"Unstable attributes: {string.Join(", ", locatorReport.UnstableAttributes)}");
            sb.AppendLine($"Recommendations: {locatorReport.Recommendations}");
            sb.AppendLine();
        }

        sb.AppendLine("## Page Elements");
        sb.AppendLine();
        sb.AppendLine("The page contains these elements (selected for automation):");
        sb.AppendLine();
        sb.AppendLine("```html");
        foreach (var el in selectedElements)
            CorpusTools.FormatElement(sb, el, indent: 0);
        sb.AppendLine("```");
        sb.AppendLine();

        if (containerGroups is { Count: > 0 })
        {
            sb.AppendLine("## Container Groups");
            sb.AppendLine();
            sb.AppendLine($"The following element groups should be generated as " +
                $"ContainerBase<{className}, TContainer> classes:");
            sb.AppendLine();
            foreach (var group in containerGroups)
            {
                sb.AppendLine($"### Group \"{group.DisplayName}\" (root: `{group.ContainerType}`)");
                sb.AppendLine();
                sb.AppendLine("```html");
                CorpusTools.FormatElement(sb, group.Element, indent: 0);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"Generate a sealed class inheriting from HtmlPageObjectBase<{className}> " +
            "with expression-bodied control properties for each element. " +
            "Use custom controls when their DOM signature matches. " +
            "Choose the most appropriate control type and locator strategy for each element.");

        return sb.ToString();
    }

    public static string BuildControlPrompt(ControlProposal proposal, string siteNamespace)
    {
        return $"""
            Generate a Brinell custom control class with the following details:

            Control Name: {proposal.Name}
            Namespace: {siteNamespace}.Controls
            DOM Signature: {proposal.DomSignature}

            ## Example DOM

            {proposal.ExampleSnippet}

            ## Suggested Properties

            {string.Join(", ", proposal.SuggestedProperties)}

            Generate a sealed class inheriting from ContainerBase<TParent, {proposal.Name}Container<TParent>>.
            Use expression-bodied properties for each child control.
            Choose the most appropriate control type and locator strategy for each property.
            Follow the locator preference order: ByText > ByDataTestId > ByAriaLabel > ById > ByCss.
            """;
    }

    public string BuildControlObjectAnalysisPrompt(
        string siteName,
        IReadOnlyList<ControlObjectAnalyzer.AggregatedPattern> aggregated,
        IReadOnlyList<DomSnapshot> snapshots)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Analyze the corpus for site \"{siteName}\" and propose reusable ControlObjects.");
        sb.AppendLine();
        sb.AppendLine($"## Site Context");
        sb.AppendLine();
        sb.AppendLine($"- Site name: {siteName}");
        sb.AppendLine($"- Pages recorded: {snapshots.Count}");
        sb.AppendLine($"- Pre-aggregated local patterns: {aggregated.Count}");
        sb.AppendLine();

        sb.AppendLine("## Available Tools");
        sb.AppendLine();
        sb.AppendLine("- `list_recorded_pages()` — enumerate captured pages");
        sb.AppendLine("- `get_page_snapshot(pageId)` — fetch a page's DOM snapshot");
        sb.AppendLine("- `find_similar_elements(selector)` — locate repeated patterns");
        sb.AppendLine();

        sb.AppendLine("## Pre-Aggregated Patterns");
        sb.AppendLine();
        sb.AppendLine("The local detector identified these candidate groups (compact JSON):");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine(SerializeAggregated(aggregated));
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Required Response Schema");
        sb.AppendLine();
        sb.AppendLine("Return a JSON object with this exact shape:");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine("{");
        sb.AppendLine("  \"proposedControls\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"name\": \"PascalCaseControlName\",");
        sb.AppendLine("      \"domSignature\": \"css-like pattern\",");
        sb.AppendLine("      \"frequency\": 0,");
        sb.AppendLine("      \"confidence\": 0,");
        sb.AppendLine("      \"exampleSnippet\": \"<div>...</div>\",");
        sb.AppendLine("      \"suggestedProperties\": [");
        sb.AppendLine("        { \"name\": \"PropertyName\", \"controlType\": \"TextBox\", \"selector\": \"...\" }");
        sb.AppendLine("      ]");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"locatorReport\": {");
        sb.AppendLine("    \"stableAttributes\": [\"data-testid\", \"aria-label\"],");
        sb.AppendLine("    \"unstableAttributes\": [\"id\"],");
        sb.AppendLine("    \"recommendations\": \"summary text\"");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine("```");

        return sb.ToString();
    }

    private static string SerializeAggregated(
        IReadOnlyList<ControlObjectAnalyzer.AggregatedPattern> aggregated)
    {
        var projection = aggregated.Select(p => new
        {
            signature = p.Signature,
            frequency = p.Frequency,
            pageIds = p.PageIds,
            isFuzzy = p.IsFuzzy,
            exampleHtml = p.ExampleHtml,
            localSuggestions = p.LocalSuggestions.Select(s => new
            {
                containerType = s.ContainerType,
                displayName = s.DisplayName
            })
        });
        return System.Text.Json.JsonSerializer.Serialize(projection,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
    }
}
