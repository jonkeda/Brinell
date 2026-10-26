using Bruinet.Generator;
using Bruinet.Generator.Models;
using Bruinet.Scraper.Code;

namespace Bruinet.Cli.Generate;

/// <summary>
/// The generator for a scraped project's control templates: each <c>.tpl.cs</c> to its <c>.gen.cs</c>,
/// as <c>bruinet controls generate</c> does, run by the Scraper before it builds the project.
/// </summary>
internal sealed class ControlTemplates : ITemplateGenerator
{
    /// <inheritdoc/>
    public IReadOnlyList<CodeProblem> Generate(string folder)
    {
        var problems = new List<CodeProblem>();
        var generator = ControlObjectGenerator.CreateDefault();
        foreach (var template in Directory.EnumerateFiles(folder, "*" + CodeGate.TemplateExtension, SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            var output = string.Concat(template.AsSpan(0, template.Length - CodeGate.TemplateExtension.Length), ".gen.cs");
            try
            {
                generator.GenerateToFile(template, output, new GeneratorOptions { InputFilePath = template, OutputFilePath = output, IncludeGeneratedHeader = true });
                problems.AddRange(generator.Warnings.Select(w => new CodeProblem(template, 1, 1, "warning", "BRGEN", w)));
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException)
            {
                problems.Add(new CodeProblem(template, 1, 1, "error", "BRGEN", $"The generator could not write {Path.GetFileName(output)}: {exception.Message}"));
            }
        }
        return problems;
    }
}
