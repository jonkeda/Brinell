namespace Brinell.Presenter.Services;

/// <summary>Answers where a project's build output goes, without building it.</summary>
public interface IProjectEvaluator
{
    /// <summary>Evaluates a project file.</summary>
    /// <param name="projectPath">The full path to a <c>.csproj</c>.</param>
    /// <param name="targetFramework">The target framework to evaluate for, or null to let the project decide.</param>
    /// <param name="configuration">The configuration, or null for the project's default.</param>
    /// <returns>What the project says about its output.</returns>
    ProjectEvaluation Evaluate(string projectPath, string? targetFramework = null, string? configuration = null);
}

/// <summary>What one project evaluation produced.</summary>
/// <param name="Succeeded">Whether MSBuild answered at all.</param>
/// <param name="ProjectPath">The project that was evaluated.</param>
/// <param name="TargetPath">
/// The assembly the build will produce, or empty when the project multi-targets and no
/// target framework was given — MSBuild returns an empty string rather than an error.
/// </param>
/// <param name="RunCommand">The command <c>dotnet run</c> would use, where the project has one.</param>
/// <param name="TargetFrameworks">Every target framework the project declares.</param>
/// <param name="Error">Why the evaluation failed, when it did.</param>
public sealed record ProjectEvaluation(
    bool Succeeded,
    string ProjectPath,
    string TargetPath,
    string RunCommand,
    IReadOnlyList<string> TargetFrameworks,
    string Error)
{
    /// <summary>Whether a target framework has to be chosen before the output path is knowable.</summary>
    public bool IsAmbiguous => Succeeded && TargetPath.Length == 0 && TargetFrameworks.Count > 1;

    /// <summary>Whether the build has already produced the output.</summary>
    public bool OutputExists => TargetPath.Length > 0 && File.Exists(TargetPath);

    /// <summary>A failed evaluation, carrying its reason.</summary>
    /// <param name="projectPath">The project that was evaluated.</param>
    /// <param name="error">Why it failed.</param>
    /// <returns>The failed result.</returns>
    public static ProjectEvaluation Failed(string projectPath, string error) =>
        new(false, projectPath, string.Empty, string.Empty, [], error);
}
