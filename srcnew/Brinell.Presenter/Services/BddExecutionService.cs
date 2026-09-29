using System.Reflection;
using System.Runtime.Loader;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

public sealed class BddExecutionService : IBddExecutionService
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

        var configPath = BddFileFormat.FindConfig(workspacePath);
        if (!File.Exists(configPath))
        {
            throw new InvalidOperationException($"UAT config was not found: {configPath}");
        }

        var config = BddConfigParser.ParseFile(configPath);
        ValidateConfig(config, configPath);

        var workingDirectory = BddWorkspaceConfigInspector.ResolveWorkingDirectory(workspacePath, config);
        var resolver = new BddRuntimeAssemblyResolver(workspacePath, config, workingDirectory);
        var placementReportPath = Path.Combine(
            Path.GetTempPath(),
            $"BrinellPresenterAutPlacement_{Guid.NewGuid():N}.txt");
        DisposableGroup? environment = null;
        object? fixture = null;
        IDisposable? executionScope = null;

        try
        {
            var appPath = BddWorkspaceConfigInspector.ResolveConfiguredAppPath(workspacePath, config);
            environment = new DisposableGroup(
                [.. CreateTargetEnvironment(config, appPath, placementReportPath),
                 workingDirectory is null ? null : new CurrentDirectoryScope(workingDirectory)]);

            var pagesAssembly = resolver.LoadRequired(ResolvePagesAssembly(workspacePath, config));
            fixture = CreateFixture(config, pagesAssembly);
            var autPlacementReport = TryReadPlacementReport(placementReportPath);

            var runtime = new UatRuntime(fixture, configPath);
            var catalog = runtime.CreateCommandCatalog();
            var file = BddScenarioFiles.Load(scenarioFilePath);
            if (!file.Success)
            {
                throw new InvalidOperationException(FormatDiagnostics(file.Diagnostics));
            }

            var bind = file.Bind(runtime.CreateCatalogSet(catalog));
            if (!bind.Success || bind.Document is null)
            {
                throw new InvalidOperationException(string.Join(
                    Environment.NewLine,
                    FormatDiagnostics(bind.Diagnostics),
                    runtime.DiscoveryReport,
                    FormatCatalog(catalog)));
            }

            var scenario = bind.Document.Scenarios.FirstOrDefault(candidate =>
                candidate.Source.Name.Equals(scenarioName, StringComparison.Ordinal));
            if (scenario is null)
            {
                throw new InvalidOperationException(
                    $"Scenario '{scenarioName}' was not found in {scenarioFilePath}.");
            }

            var runner = new BddScenarioRunner();
            runtime.ConfigureContext(runner.Context);

            // Presenter pauses between steps itself; the engine's own breaks are for a debugger.
            runner.Context.Breaks = null;
            executionScope = runtime.CreateScope();
            if (executionScope is Microsoft.Extensions.DependencyInjection.IServiceScope serviceScope)
            {
                UatRuntime.ConfigureScope(runner.Context, serviceScope.ServiceProvider);
            }

            return new PresenterUatExecutionSession(
                runner.CreateSession(scenario),
                runner,
                scenario,
                catalog,
                runtime.DiscoveryReport,
                FormatCatalog(catalog),
                autPlacementReport,
                executionScope,
                fixture as IDisposable,
                resolver,
                environment);
        }
        catch
        {
            executionScope?.Dispose();
            (fixture as IDisposable)?.Dispose();
            environment?.Dispose();
            resolver.Dispose();
            throw;
        }
    }

    private static string TryReadPlacementReport(string path)
    {
        try
        {
            return File.Exists(path)
                ? File.ReadAllText(path)
                : "AUT placement:" + Environment.NewLine + "Result: not supported";
        }
        catch (Exception ex)
        {
            return "AUT placement:" + Environment.NewLine + $"Result: failed: {ex.Message}";
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Placement diagnostics are best effort only.
            }
        }
    }

    private static void ValidateConfig(BddConfig config, string configPath)
    {
        if (!config.Runtime.TryGetValue("Target", out var target) ||
            !BddTargetRegistry.TryGet(target, out _))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Target to one of: {BddTargetRegistry.SupportedTargetList}.");
        }

        if (!config.Runtime.TryGetValue("Fixture", out var fixture) ||
            string.IsNullOrWhiteSpace(fixture))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Fixture.");
        }

        // A Project row replaces the Pages registration, so either is enough.
        if (config.Assemblies.Count == 0 &&
            !config.Runtime.ContainsKey(BddConfigFields.Project))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Project or register at least one assembly.");
        }
    }

    private static IEnumerable<IDisposable?> CreateTargetEnvironment(
        BddConfig config,
        string? appPath,
        string placementReportPath)
    {
        var target = BddTargetRegistry.GetRequired(config);

        // Only an explicit override travels to the environment. With no AppPath the fixture
        // resolves the app itself, which is what every fixture base already requires of it.
        yield return appPath is null
            ? null
            : new EnvironmentVariableScope(target.AppPathEnvironmentVariable, appPath);

        if (target.SupportsPresenterAutPlacement)
        {
            yield return new EnvironmentVariableScope("BRINELL_AUT_PLACE_RIGHT", "1");
            yield return new EnvironmentVariableScope("BRINELL_AUT_PLACEMENT_RESULT_FILE", placementReportPath);
        }
    }

    private static string GetRegisteredAssembly(BddConfig config, string kind)
    {
        var registration = config.Assemblies.FirstOrDefault(assembly =>
            assembly.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));
        return registration?.Assembly
               ?? throw new InvalidOperationException($"UAT config must register a {kind} assembly.");
    }

    /// <summary>
    /// The Pages assembly: an explicit registration when there is one, otherwise the output
    /// of the project named by <c>Runtime Project</c>.
    /// </summary>
    private static string ResolvePagesAssembly(string workspacePath, BddConfig config)
    {
        var registration = config.Assemblies.FirstOrDefault(assembly =>
            assembly.Kind.Equals("Pages", StringComparison.OrdinalIgnoreCase));
        if (registration is not null)
        {
            return registration.Assembly;
        }

        var inspection = BddWorkspaceConfigInspector.Inspect(workspacePath);
        if (inspection.Project is { PagesAssemblyPath.Length: > 0 } project)
        {
            return project.PagesAssemblyExists
                ? project.PagesAssemblyPath
                : throw new InvalidOperationException(
                    $"{Path.GetFileNameWithoutExtension(project.ResolvedPath)} is not built. " +
                    $"Its output belongs at {project.PagesAssemblyPath}.");
        }

        throw new InvalidOperationException(
            "UAT config must set Runtime Project or register a Pages assembly.");
    }

    private static object CreateFixture(BddConfig config, Assembly pagesAssembly)
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

    private static string FormatDiagnostics(IEnumerable<BddDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(diagnostic => $"{diagnostic.Location}: {diagnostic.Code} {diagnostic.Message}"));
    }

    private static string FormatCatalog(BddCommandCatalog catalog)
    {
        return "Command catalog:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            catalog.Patterns
                .OrderBy(pattern => pattern.Keyword)
                .ThenBy(pattern => pattern.Phrase, StringComparer.Ordinal)
                .Select(pattern => $"- {pattern.Keyword}: {pattern.Phrase} -> {pattern.CommandId}"));
    }
}

internal sealed class BddRuntimeAssemblyResolver : IDisposable
{
    private readonly Func<AssemblyLoadContext, AssemblyName, Assembly?> _handler;
    private readonly IReadOnlyList<string> _probeDirectories;
    private readonly BddSessionLoadContext _sessionContext;
    private bool _disposed;

    public BddRuntimeAssemblyResolver(string workspacePath, BddConfig config, string? workingDirectory)
    {
        WorkspacePath = workspacePath;
        ProbeRoot = workingDirectory ??
                    BddWorkspaceConfigInspector.FindSolutionRoot(workspacePath) ??
                    workspacePath;
        _probeDirectories = BuildProbeDirectories(workspacePath, config, ProbeRoot, workingDirectory);
        _sessionContext = new BddSessionLoadContext(ResolveAssemblyPath);
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

        var explicitPath = BddWorkspaceConfigInspector.ResolveAssemblyPath(WorkspacePath, assemblyName);
        var path = explicitPath is not null && File.Exists(explicitPath)
            ? explicitPath
            : ResolveAssemblyPath(fileName)
              ?? throw new InvalidOperationException(
                  $"Assembly '{fileName}' was not found. Build the UAT page-object project first.");

        // Into the session's own collectible context, so the file is released when the session
        // ends and the next build can overwrite it. The default context can never unload, and
        // a workspace that has been run once would otherwise be unbuildable until restart.
        return _sessionContext.LoadFromAssemblyPath(path);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        AssemblyLoadContext.Default.Resolving -= _handler;
        _sessionContext.Unload();
        _disposed = true;

        // Unload is asynchronous: the file stays mapped until the context is collected, and
        // "build now" has to work immediately after a run.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
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
        BddConfig config,
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
            var resolvedPath = BddWorkspaceConfigInspector.ResolveAssemblyPath(workspacePath, registration.Assembly);
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
