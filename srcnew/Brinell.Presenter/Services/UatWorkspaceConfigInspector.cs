using Brinell.Presenter.Models;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

internal static class UatWorkspaceConfigInspector
{
    /// <summary>Chooses which framework a multi-targeted project is evaluated for.</summary>
    internal const string ProjectFrameworkField = "ProjectFramework";

    /// <summary>The build configuration a workspace pins, when it pins one.</summary>
    internal const string ConfigurationField = "Configuration";

    // One instance, so its evaluation cache survives across reloads.
    private static readonly IProjectEvaluator SharedEvaluator = new MsBuildProjectEvaluator();

    private static readonly IFixtureInspector SharedFixtureInspector = new FixtureInspector();

    public static UatWorkspaceConfigLoadResult Inspect(
        string workspacePath,
        IProjectEvaluator? evaluator = null,
        IFixtureInspector? fixtureInspector = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var configPath = Path.Combine(workspacePath, "uat.config.md");
        if (!File.Exists(configPath))
        {
            return new UatWorkspaceConfigLoadResult(
                false,
                configPath,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                string.Empty,
                string.Empty,
                false,
                [],
                [$"Error: uat.config.md was not found at {configPath}."],
                null,
                [new UatConfigProblem(UatConfigSections.File, "uat.config.md", $"uat.config.md was not found at {configPath}.")]);
        }

        var config = UatConfigParser.ParseFile(configPath);
        List<UatConfigProblem> problems = [];

        var fixture = ReadRuntime(config, "Fixture");
        if (string.IsNullOrWhiteSpace(fixture))
        {
            problems.Add(new UatConfigProblem(UatConfigSections.Runtime, UatConfigFields.Fixture, "Runtime Fixture is required."));
        }

        // AppPath is an override, not a requirement: the fixture owns the app, and every
        // fixture base makes GetDefaultAppPath abstract. Only a configured-but-wrong path
        // is an error. See .my/present/startup/00-presenter-workspace-design.md.
        var appPath = ReadRuntime(config, "AppPath");
        var resolvedAppPath = ResolveOptionalPath(workspacePath, appPath);
        var appPathExists = !string.IsNullOrWhiteSpace(resolvedAppPath) && File.Exists(resolvedAppPath);
        if (!string.IsNullOrWhiteSpace(appPath) && !appPathExists)
        {
            problems.Add(new UatConfigProblem(UatConfigSections.Runtime, UatConfigFields.AppPath, $"Runtime AppPath was not found: {resolvedAppPath}."));
        }

        var workingDirectory = ReadRuntime(config, "WorkingDirectory");
        var resolvedWorkingDirectory = ResolveOptionalPath(workspacePath, workingDirectory);
        var workingDirectoryExists = string.IsNullOrWhiteSpace(workingDirectory) ||
                                     Directory.Exists(resolvedWorkingDirectory);
        if (!string.IsNullOrWhiteSpace(workingDirectory) && !workingDirectoryExists)
        {
            problems.Add(new UatConfigProblem(UatConfigSections.Runtime, UatConfigFields.WorkingDirectory, $"Runtime WorkingDirectory was not found: {resolvedWorkingDirectory}."));
        }

        var project = ResolveProject(workspacePath, config, evaluator ?? SharedEvaluator, problems);

        var configured = config.Assemblies
            .Select(assembly =>
            {
                var resolvedPath = ResolveAssemblyPath(workspacePath, assembly.Assembly);
                return new UatAssemblyLoadResult(
                    assembly.Kind,
                    assembly.Assembly,
                    resolvedPath ?? string.Empty,
                    resolvedPath is not null && File.Exists(resolvedPath));
            })
            .ToArray();

        // A Pages row overrides the project; with neither, there is nothing to load from.
        var hasConfiguredPages = configured.Any(assembly =>
            assembly.Kind.Equals("Pages", StringComparison.OrdinalIgnoreCase));
        var assemblies = project is not null && !hasConfiguredPages
            ? [new UatAssemblyLoadResult("Pages", project.Configured, project.PagesAssemblyPath, project.PagesAssemblyExists), .. configured]
            : configured;

        if (assemblies.Length == 0)
        {
            problems.Add(new UatConfigProblem(UatConfigSections.Runtime, UatConfigFields.Project, "Runtime Project, or at least one assembly registration, is required."));
        }

        // The project's own diagnostic says "not built" and names the output, which a
        // "Pages assembly was not found: ./X.csproj" would only obscure.
        foreach (var assembly in configured.Where(assembly => !assembly.Exists))
        {
            problems.Add(new UatConfigProblem(UatConfigSections.Assemblies, assembly.Kind, $"{assembly.Kind} assembly was not found: {assembly.Assembly}."));
        }

        var target = ResolveTarget(config, assemblies, fixture, fixtureInspector ?? SharedFixtureInspector, problems);

        return new UatWorkspaceConfigLoadResult(
            true,
            configPath,
            target,
            fixture,
            appPath,
            resolvedAppPath,
            appPathExists,
            workingDirectory,
            resolvedWorkingDirectory,
            workingDirectoryExists,
            assemblies,
            [.. problems.Select(problem => problem.Line)],
            project,
            problems);
    }

    /// <summary>
    /// Derives the target from the fixture's base type, and checks any configured override
    /// against it.
    /// </summary>
    /// <remarks>
    /// A derived target cannot disagree with itself, so the config no longer carries one. When
    /// it does, it is an override — and a row that contradicts the fixture is now detectable
    /// rather than taken on faith.
    /// </remarks>
    private static string ResolveTarget(
        UatConfig config,
        IReadOnlyList<UatAssemblyLoadResult> assemblies,
        string fixture,
        IFixtureInspector inspector,
        List<UatConfigProblem> problems)
    {
        var configured = ReadRuntime(config, UatConfigFields.Target);
        if (configured.Length > 0 && !UatTargetRegistry.TryGet(configured, out _))
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime,
                UatConfigFields.Target,
                $"Runtime Target '{configured}' is not supported by Presenter yet. " +
                $"Supported targets: {UatTargetRegistry.SupportedTargetList}."));
            return configured;
        }

        var pages = assemblies.FirstOrDefault(assembly =>
            assembly.Kind.Equals("Pages", StringComparison.OrdinalIgnoreCase) && assembly.Exists);
        if (pages is null)
        {
            // Nothing to derive from yet; a configured value still stands.
            return configured;
        }

        var inspection = inspector.Inspect(pages.ResolvedPath);
        var candidate = inspection.Find(fixture);
        if (candidate is null)
        {
            if (inspection.Succeeded && fixture.Length > 0)
            {
                var usable = inspection.Usable.Select(item => item.Name).ToArray();
                problems.Add(new UatConfigProblem(
                    UatConfigSections.Runtime,
                    UatConfigFields.Fixture,
                    $"Fixture '{fixture}' was not found in {Path.GetFileName(pages.ResolvedPath)}. " +
                    (usable.Length > 0
                        ? $"Available: {string.Join(", ", usable)}."
                        : "It registers no usable fixture.")));
            }

            return configured;
        }

        if (!candidate.IsUsable)
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime,
                UatConfigFields.Fixture,
                $"Fixture '{candidate.Name}' cannot be constructed: {candidate.UnusableReason}."));
        }

        if (candidate.Target is null)
        {
            return configured;
        }

        if (configured.Length > 0 && !configured.Equals(candidate.Target, StringComparison.OrdinalIgnoreCase))
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime,
                UatConfigFields.Target,
                $"Runtime Target is '{configured}' but {candidate.Name} derives " +
                $"{candidate.FixtureBase}, which is {candidate.Target}."));
        }

        return candidate.Target;
    }

    /// <summary>
    /// Resolves the <c>Runtime Project</c> row to the assembly its build produces.
    /// </summary>
    /// <remarks>
    /// Evaluation answers before the first build, so "not built" is reported with the path the
    /// build will write to, rather than as a missing file with nothing to act on.
    /// </remarks>
    private static UatProjectResolution? ResolveProject(
        string workspacePath,
        UatConfig config,
        IProjectEvaluator evaluator,
        List<UatConfigProblem> problems)
    {
        var configured = ReadRuntime(config, UatConfigFields.Project);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        var resolvedPath = ResolveOptionalPath(workspacePath, configured);
        if (!File.Exists(resolvedPath))
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime, UatConfigFields.Project, $"Runtime Project was not found: {resolvedPath}."));
            return new UatProjectResolution(configured, resolvedPath, string.Empty, false, []);
        }

        var evaluation = evaluator.Evaluate(
            resolvedPath,
            ReadRuntime(config, ProjectFrameworkField) is { Length: > 0 } framework ? framework : null,
            ReadRuntime(config, ConfigurationField) is { Length: > 0 } configuration ? configuration : null);

        if (!evaluation.Succeeded)
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime, UatConfigFields.Project, $"Runtime Project could not be evaluated: {evaluation.Error}"));
            return new UatProjectResolution(configured, resolvedPath, string.Empty, false, []);
        }

        if (evaluation.IsAmbiguous)
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime,
                ProjectFrameworkField,
                $"Runtime Project targets {string.Join(", ", evaluation.TargetFrameworks)}; " +
                $"set Runtime {ProjectFrameworkField} to one of them."));
            return new UatProjectResolution(
                configured,
                resolvedPath,
                string.Empty,
                false,
                evaluation.TargetFrameworks);
        }

        if (evaluation.TargetPath.Length == 0)
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime, UatConfigFields.Project, "Runtime Project reported no output path."));
            return new UatProjectResolution(
                configured,
                resolvedPath,
                string.Empty,
                false,
                evaluation.TargetFrameworks);
        }

        if (!evaluation.OutputExists)
        {
            problems.Add(new UatConfigProblem(
                UatConfigSections.Runtime,
                UatConfigFields.Project,
                $"{Path.GetFileNameWithoutExtension(resolvedPath)} is not built. " +
                $"Its output belongs at {evaluation.TargetPath}."));
        }

        return new UatProjectResolution(
            configured,
            resolvedPath,
            evaluation.TargetPath,
            evaluation.OutputExists,
            evaluation.TargetFrameworks);
    }

    /// <summary>Resolves the configured app path, when the config overrides the fixture's own.</summary>
    /// <param name="workspacePath">The workspace folder.</param>
    /// <param name="config">The parsed config.</param>
    /// <returns>The resolved path, or <see langword="null" /> when the fixture owns the app.</returns>
    /// <exception cref="InvalidOperationException">The configured path does not exist.</exception>
    public static string? ResolveConfiguredAppPath(string workspacePath, UatConfig config)
    {
        var configuredPath = ReadRuntime(config, "AppPath");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return null;
        }

        var resolvedPath = ResolveOptionalPath(workspacePath, configuredPath);
        if (!File.Exists(resolvedPath))
        {
            throw new InvalidOperationException($"Runtime AppPath was not found: {resolvedPath}");
        }

        return resolvedPath;
    }

    public static string? ResolveWorkingDirectory(string workspacePath, UatConfig config)
    {
        var configuredPath = ReadRuntime(config, "WorkingDirectory");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return null;
        }

        var resolvedPath = ResolveOptionalPath(workspacePath, configuredPath);
        if (!Directory.Exists(resolvedPath))
        {
            throw new InvalidOperationException($"Runtime WorkingDirectory was not found: {resolvedPath}");
        }

        return resolvedPath;
    }

    public static string? ResolveAssemblyPath(string workspacePath, string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            return null;
        }

        var directPath = ResolveOptionalPath(workspacePath, assemblyPath);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var fileName = Path.GetFileName(assemblyPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var root = FindSolutionRoot(workspacePath) ?? workspacePath;
        return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(path => path.Contains("net10.0", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    public static string ResolveOptionalPath(string workspacePath, string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        var path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(workspacePath, configuredPath);
        return Path.GetFullPath(path);
    }

    public static string? FindSolutionRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Brinell.sln")) ||
                directory.GetFiles("*.sln").Length > 0 ||
                directory.GetFiles("*.slnx").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string ReadRuntime(UatConfig config, string field)
    {
        return config.Runtime.TryGetValue(field, out var value) ? value.Trim() : string.Empty;
    }

}
