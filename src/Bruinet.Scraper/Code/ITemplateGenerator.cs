namespace Bruinet.Scraper.Code;

/// <summary>
/// Writes a control template's generated members (each <c>.tpl.cs</c> to its <c>.gen.cs</c>) before a
/// scraped project builds, as <c>bruinet controls generate</c> does for <c>src</c>. The generator
/// lives in the CLI, so the host that makes the <see cref="Service.ScraperService"/> gives it.
/// </summary>
public interface ITemplateGenerator
{
    /// <summary>Generates every template under <paramref name="folder"/>.</summary>
    /// <param name="folder">A folder of the project, searched with its subfolders.</param>
    /// <returns>What went wrong or needs a look, per template; empty when all were written cleanly.</returns>
    IReadOnlyList<CodeProblem> Generate(string folder);
}
