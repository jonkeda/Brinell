using Brinell.Presenter.Models;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

internal static class UatWorkspaceConfigInspector
{
    public static UatWorkspaceConfigLoadResult Inspect(string workspacePath)
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
                [$"Error: uat.config.md was not found at {configPath}."]);
        }

        var config = UatConfigParser.ParseFile(configPath);
        List<string> diagnostics = [];

        var target = ReadRuntime(config, "Target");
        if (string.IsNullOrWhiteSpace(target))
        {
            diagnostics.Add("Error: Runtime Target is required.");
        }
        else if (!UatTargetRegistry.TryGet(target, out _))
        {
            diagnostics.Add(
                $"Error: Runtime Target '{target}' is not supported by Presenter yet. Supported targets: {UatTargetRegistry.SupportedTargetList}.");
        }

        var fixture = ReadRuntime(config, "Fixture");
        if (string.IsNullOrWhiteSpace(fixture))
        {
            diagnostics.Add("Error: Runtime Fixture is required.");
        }

        var appPath = ReadRuntime(config, "AppPath");
        var resolvedAppPath = ResolveOptionalPath(workspacePath, appPath);
        var appPathExists = !string.IsNullOrWhiteSpace(resolvedAppPath) && File.Exists(resolvedAppPath);
        if (string.IsNullOrWhiteSpace(appPath))
        {
            diagnostics.Add("Error: Runtime AppPath is required for local execution.");
        }
        else if (!appPathExists)
        {
            diagnostics.Add($"Error: Runtime AppPath was not found: {resolvedAppPath}.");
        }

        var workingDirectory = ReadRuntime(config, "WorkingDirectory");
        var resolvedWorkingDirectory = ResolveOptionalPath(workspacePath, workingDirectory);
        var workingDirectoryExists = string.IsNullOrWhiteSpace(workingDirectory) ||
                                     Directory.Exists(resolvedWorkingDirectory);
        if (!string.IsNullOrWhiteSpace(workingDirectory) && !workingDirectoryExists)
        {
            diagnostics.Add($"Error: Runtime WorkingDirectory was not found: {resolvedWorkingDirectory}.");
        }

        var assemblies = config.Assemblies
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

        if (assemblies.Length == 0)
        {
            diagnostics.Add("Error: At least one assembly registration is required.");
        }

        foreach (var assembly in assemblies.Where(assembly => !assembly.Exists))
        {
            diagnostics.Add($"Error: {assembly.Kind} assembly was not found: {assembly.Assembly}.");
        }

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
            diagnostics);
    }

    public static string ResolveRequiredAppPath(string workspacePath, UatConfig config)
    {
        var configuredPath = ReadRuntime(config, "AppPath");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("Runtime AppPath is required for local execution.");
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
