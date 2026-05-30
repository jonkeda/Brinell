using System.Reflection;
using System.Runtime.Loader;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

public sealed class UatExecutionService : IUatExecutionService
{
    public Task<PresenterUatExecutionSession> CreateSessionAsync(
        string workspacePath,
        string scenarioFilePath,
        string scenarioName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        return Task.Run(
            () => CreateSession(workspacePath, scenarioFilePath, scenarioName, cancellationToken),
            cancellationToken);
    }

    private static PresenterUatExecutionSession CreateSession(
        string workspacePath,
        string scenarioFilePath,
        string scenarioName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var configPath = Path.Combine(workspacePath, "uat.config.md");
        if (!File.Exists(configPath))
        {
            throw new InvalidOperationException($"UAT config was not found: {configPath}");
        }

        var config = UatConfigParser.ParseFile(configPath);
        ValidateConfig(config, configPath);

        var workingDirectory = UatWorkspaceConfigInspector.ResolveWorkingDirectory(workspacePath, config);
        var resolver = new UatRuntimeAssemblyResolver(workspacePath, config, workingDirectory);
        DisposableGroup? environment = null;
        object? fixture = null;

        try
        {
            var appPath = UatWorkspaceConfigInspector.ResolveRequiredAppPath(workspacePath, config);
            environment = new DisposableGroup(
                new EnvironmentVariableScope("APPIUM_APP_PATH", appPath),
                workingDirectory is null ? null : new CurrentDirectoryScope(workingDirectory));

            var pagesAssembly = resolver.LoadRequired(GetRegisteredAssembly(config, "Pages"));
            fixture = CreateFixture(config, pagesAssembly);

            var runtime = UatReflectionRuntime.FromRoot(fixture);
            var catalog = runtime.CreateCommandCatalog();
            var parse = UatMarkdownParser.ParseFile(scenarioFilePath);
            if (!parse.Success || parse.Document is null)
            {
                throw new InvalidOperationException(FormatDiagnostics(parse.Diagnostics));
            }

            var bind = UatBinder.Bind(parse.Document, catalog);
            if (!bind.Success || bind.Document is null)
            {
                throw new InvalidOperationException(string.Join(
                    Environment.NewLine,
                    FormatDiagnostics(bind.Diagnostics),
                    string.Join(Environment.NewLine, runtime.DescribeDiscovery()),
                    FormatCatalog(catalog)));
            }

            var scenario = bind.Document.Scenarios.FirstOrDefault(candidate =>
                candidate.Source.Name.Equals(scenarioName, StringComparison.Ordinal));
            if (scenario is null)
            {
                throw new InvalidOperationException(
                    $"Scenario '{scenarioName}' was not found in {scenarioFilePath}.");
            }

            var runner = new UatScenarioRunner();
            return new PresenterUatExecutionSession(
                runner.CreateSession(scenario),
                runner,
                scenario,
                catalog,
                string.Join(Environment.NewLine, runtime.DescribeDiscovery()),
                FormatCatalog(catalog),
                fixture as IDisposable,
                resolver,
                environment);
        }
        catch
        {
            (fixture as IDisposable)?.Dispose();
            environment?.Dispose();
            resolver.Dispose();
            throw;
        }
    }

    private static void ValidateConfig(UatConfig config, string configPath)
    {
        if (!config.Runtime.TryGetValue("Target", out var target) ||
            !target.Equals("MAUI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Target to MAUI.");
        }

        if (!config.Runtime.TryGetValue("Fixture", out var fixture) ||
            string.IsNullOrWhiteSpace(fixture))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Fixture.");
        }

        if (config.Assemblies.Count == 0)
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must register at least one assembly.");
        }
    }

    private static string GetRegisteredAssembly(UatConfig config, string kind)
    {
        var registration = config.Assemblies.FirstOrDefault(assembly =>
            assembly.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));
        return registration?.Assembly
               ?? throw new InvalidOperationException($"UAT config must register a {kind} assembly.");
    }

    private static object CreateFixture(UatConfig config, Assembly pagesAssembly)
    {
        var fixtureName = config.Runtime["Fixture"].Trim();
        var fixtureType = pagesAssembly.GetTypes().FirstOrDefault(type =>
            type is { IsAbstract: false, IsClass: true } &&
            type.GetConstructor(Type.EmptyTypes) is not null &&
            (type.FullName?.Equals(fixtureName, StringComparison.Ordinal) == true ||
             type.Name.Equals(fixtureName, StringComparison.Ordinal) ||
             type.Name.Equals(fixtureName + "Fixture", StringComparison.Ordinal)));

        if (fixtureType is null)
        {
            throw new InvalidOperationException(
                $"Fixture '{fixtureName}' was not found in {pagesAssembly.GetName().Name}.");
        }

        return Activator.CreateInstance(fixtureType)
               ?? throw new InvalidOperationException($"Fixture '{fixtureType.FullName}' could not be created.");
    }

    private static string FormatDiagnostics(IEnumerable<UatDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(diagnostic => $"{diagnostic.Location}: {diagnostic.Code} {diagnostic.Message}"));
    }

    private static string FormatCatalog(UatCommandCatalog catalog)
    {
        return "Command catalog:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            catalog.Patterns
                .OrderBy(pattern => pattern.Keyword)
                .ThenBy(pattern => pattern.Phrase, StringComparer.Ordinal)
                .Select(pattern => $"- {pattern.Keyword}: {pattern.Phrase} -> {pattern.CommandId}"));
    }
}

internal sealed class UatRuntimeAssemblyResolver : IDisposable
{
    private readonly Func<AssemblyLoadContext, AssemblyName, Assembly?> _handler;
    private readonly IReadOnlyList<string> _probeDirectories;
    private bool _disposed;

    public UatRuntimeAssemblyResolver(string workspacePath, UatConfig config, string? workingDirectory)
    {
        WorkspacePath = workspacePath;
        ProbeRoot = workingDirectory ??
                    UatWorkspaceConfigInspector.FindSolutionRoot(workspacePath) ??
                    workspacePath;
        _probeDirectories = BuildProbeDirectories(workspacePath, config, ProbeRoot, workingDirectory);
        _handler = ResolveAssembly;
        AssemblyLoadContext.Default.Resolving += _handler;
    }

    private string WorkspacePath { get; }

    private string ProbeRoot { get; }

    public Assembly LoadRequired(string assemblyName)
    {
        var fileName = NormalizeAssemblyFileName(Path.GetFileName(assemblyName));
        var loaded = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly =>
            (assembly.GetName().Name + ".dll").Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (loaded is not null)
        {
            return loaded;
        }

        var explicitPath = UatWorkspaceConfigInspector.ResolveAssemblyPath(WorkspacePath, assemblyName);
        if (explicitPath is not null && File.Exists(explicitPath))
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(explicitPath);
        }

        var path = ResolveAssemblyPath(fileName)
                   ?? throw new InvalidOperationException(
                       $"Assembly '{fileName}' was not found. Build the UAT page-object project first.");
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        AssemblyLoadContext.Default.Resolving -= _handler;
        _disposed = true;
    }

    private Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        var fileName = assemblyName.Name + ".dll";
        var path = ResolveAssemblyPath(fileName);
        return path is null ? null : context.LoadFromAssemblyPath(path);
    }

    private string? ResolveAssemblyPath(string fileName)
    {
        foreach (var directory in _probeDirectories)
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return Directory.EnumerateFiles(ProbeRoot, fileName, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(path => path.Contains("net10.0", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static IReadOnlyList<string> BuildProbeDirectories(
        string workspacePath,
        UatConfig config,
        string probeRoot,
        string? workingDirectory)
    {
        HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase)
        {
            AppContext.BaseDirectory,
            workspacePath,
            probeRoot
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            directories.Add(workingDirectory);
        }

        foreach (var sharedFrameworkDirectory in GetSharedFrameworkDirectories())
        {
            directories.Add(sharedFrameworkDirectory);
        }

        foreach (var registration in config.Assemblies)
        {
            var resolvedPath = UatWorkspaceConfigInspector.ResolveAssemblyPath(workspacePath, registration.Assembly);
            if (resolvedPath is not null)
            {
                directories.Add(Path.GetDirectoryName(resolvedPath) ?? resolvedPath);
                continue;
            }

            var path = registration.Assembly;
            if (Path.IsPathRooted(path))
            {
                directories.Add(Path.GetDirectoryName(path) ?? path);
            }
            else
            {
                directories.Add(Path.Combine(workspacePath, Path.GetDirectoryName(path) ?? string.Empty));
            }
        }

        return directories.Where(Directory.Exists).ToArray();
    }

    private static IEnumerable<string> GetSharedFrameworkDirectories()
    {
        var coreLibraryDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var sharedDirectory = coreLibraryDirectory is null
            ? null
            : Directory.GetParent(coreLibraryDirectory)?.FullName;
        if (sharedDirectory is null || !Directory.Exists(sharedDirectory))
        {
            yield break;
        }

        foreach (var frameworkDirectory in Directory.EnumerateDirectories(sharedDirectory))
        {
            foreach (var versionDirectory in Directory.EnumerateDirectories(frameworkDirectory))
            {
                yield return versionDirectory;
            }
        }
    }

    private static string NormalizeAssemblyFileName(string assemblyName)
    {
        return assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? assemblyName
            : assemblyName + ".dll";
    }

}

internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly string _name;
    private readonly string? _previousValue;

    public EnvironmentVariableScope(string name, string value)
    {
        _name = name;
        _previousValue = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_name, _previousValue);
    }
}

internal sealed class CurrentDirectoryScope : IDisposable
{
    private readonly string _previousDirectory;

    public CurrentDirectoryScope(string directory)
    {
        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(directory);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_previousDirectory);
    }
}

internal sealed class DisposableGroup : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _disposables;

    public DisposableGroup(params IDisposable?[] disposables)
    {
        _disposables = disposables.OfType<IDisposable>().ToArray();
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables.Reverse())
        {
            disposable.Dispose();
        }
    }
}
