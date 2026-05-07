using System.Text;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public sealed class PromptBuilder
{
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
}
