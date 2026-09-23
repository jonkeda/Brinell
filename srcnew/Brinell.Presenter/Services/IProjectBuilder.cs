namespace Brinell.Presenter.Services;

/// <summary>Builds a project, and reports what MSBuild said.</summary>
public interface IProjectBuilder
{
    /// <summary>Builds a project.</summary>
    /// <param name="projectPath">The project file.</param>
    /// <param name="targetFramework">The framework to build, or null for the project's default.</param>
    /// <param name="configuration">The configuration, or null for the project's default.</param>
    /// <param name="onOutput">Called with each line as it arrives.</param>
    /// <param name="cancellationToken">Cancels the build.</param>
    /// <returns>Whether it succeeded, and the output.</returns>
    Task<ProjectBuildResult> BuildAsync(
        string projectPath,
        string? targetFramework,
        string? configuration,
        Action<string>? onOutput,
        CancellationToken cancellationToken);
}

/// <summary>What a build produced.</summary>
/// <param name="Succeeded">Whether MSBuild returned success.</param>
/// <param name="Output">
/// The build's own output. Its errors are better than anything a wrapper would paraphrase:
/// a missing workload arrives as NETSDK1147, which names the workload and the command to fix it.
/// </param>
public sealed record ProjectBuildResult(bool Succeeded, string Output);
