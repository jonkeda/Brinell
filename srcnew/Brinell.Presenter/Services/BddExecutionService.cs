using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

public sealed class BddExecutionService : IBddExecutionService
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> WindowEnvironment { get; set; } = new Dictionary<string, string>();

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
            () => CreateSession(workspacePath, scenarioFilePath, scenarioName, WindowEnvironment, cancellationToken),
            cancellationToken);
    }

    private static PresenterUatExecutionSession CreateSession(
        string workspacePath,
        string scenarioFilePath,
        string scenarioName,
        IReadOnlyDictionary<string, string> windowEnvironment,
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
        if (TryCreateSession(workspacePath, scenarioFilePath, scenarioName, windowEnvironment, configPath, config, workingDirectory, resolver, out var failure) is { } session)
        {
            return session;
        }

        // Only here, with the failure's own frames gone: the exception holds the suite's types,
        // and unloading while it lives leaves the suite's files locked until Presenter closes.
        resolver.Dispose();
        throw new InvalidOperationException(failure);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PresenterUatExecutionSession? TryCreateSession(
        string workspacePath,
        string scenarioFilePath,
        string scenarioName,
        IReadOnlyDictionary<string, string> windowEnvironment,
        string configPath,
        BddConfig config,
        string? workingDirectory,
        BddRuntimeAssemblyResolver resolver,
        out string failure)
    {
        failure = string.Empty;
        DisposableGroup? environment = null;
        object? fixture = null;
        IDisposable? executionScope = null;

        try
        {
            var appPath = BddWorkspaceConfigInspector.ResolveConfiguredAppPath(workspacePath, config);
            environment = new DisposableGroup(
                [.. CreateTargetEnvironment(config, appPath, windowEnvironment),
                 workingDirectory is null ? null : new CurrentDirectoryScope(workingDirectory)]);

            var pagesAssembly = resolver.LoadRequired(ResolvePagesAssembly(workspacePath, config));
            fixture = CreateFixture(config, pagesAssembly);

            // As the suite's test class does, and before binding: it may restart the app on the
            // file's test set, and binding reads the fixture's composition.
            var lifecycle = fixture as IUatScenarioLifecycle;
            if (lifecycle is not null && BddMarkdownParser.ParseFile(scenarioFilePath).Document is { } document)
            {
                lifecycle.BeforeFile(document);
            }

            // The session does not place the app; Presenter does, once its window is open.
            var autPlacementReport = "AUT placement:" + Environment.NewLine + "Result: not supported";

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

            lifecycle?.BeforeScenario(scenario);
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
                environment)
            {
                RecordingTarget = () => UatRecording.TargetOf(fixture),
            };
        }
        catch (Exception ex)
        {
            executionScope?.Dispose();
            (fixture as IDisposable)?.Dispose();
            environment?.Dispose();
            if (ex is OperationCanceledException)
            {
                resolver.Dispose();
                throw;
            }

            failure = Describe(ex);
            return null;
        }
    }

    // Every message of the chain once, without reflection's own wrapper.
    private static string Describe(Exception exception)
    {
        List<string> messages = [];
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is not TargetInvocationException && !messages.Contains(current.Message, StringComparer.Ordinal))
            {
                messages.Add(current.Message);
            }
        }

        return messages.Count == 0 ? exception.Message : string.Join(Environment.NewLine, messages);
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
        IReadOnlyDictionary<string, string> windowEnvironment)
    {
        var target = BddTargetRegistry.GetRequired(config);

        // Only an explicit override travels to the environment. With no AppPath the fixture
        // resolves the app itself, which is what every fixture base already requires of it.
        yield return appPath is null
            ? null
            : new EnvironmentVariableScope(target.AppPathEnvironmentVariable, appPath);

        // How the app's window is shown; Presenter places it itself once it is open
        // (PresenterWindowService), for every target.
        foreach (var (name, value) in windowEnvironment)
        {
            yield return new EnvironmentVariableScope(name, value);
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
    private readonly Func<Assembly, string, IntPtr> _nativeHandler;
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

        // Native libraries too (e_sqlite3): the host's folder has none of the suite's runtimes\<rid>\native.
        _nativeHandler = ResolveNativeLibrary;
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += _nativeHandler;
        _sessionContext.ResolvingUnmanagedDll += _nativeHandler;
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
        AssemblyLoadContext.Default.ResolvingUnmanagedDll -= _nativeHandler;
        _sessionContext.ResolvingUnmanagedDll -= _nativeHandler;
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

    private IntPtr ResolveNativeLibrary(Assembly assembly, string name)
    {
        var fileName = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name : name + ".dll";
        var native = Path.Combine("runtimes", "win-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(), "native");
        var directories = string.IsNullOrEmpty(assembly.Location)
            ? _probeDirectories
            : _probeDirectories.Prepend(Path.GetDirectoryName(assembly.Location)!);
        foreach (var directory in directories)
        {
            foreach (var candidate in new[] { Path.Combine(directory, native, fileName), Path.Combine(directory, fileName) })
            {
                if (File.Exists(candidate))
                {
                    return NativeLibrary.Load(candidate);
                }
            }
        }

        return IntPtr.Zero;
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

        // Last: what the suite ships wins over the shared framework's copy.
        foreach (var sharedFrameworkDirectory in GetSharedFrameworkDirectories())
        {
            directories.Add(sharedFrameworkDirectory);
        }

        return directories.Where(Directory.Exists).ToArray();
    }

    // The shared frameworks beside the runtime's own (ASP.NET Core for a suite with a mock server),
    // each at the running runtime's version, or its newest of the same major. They load into the
    // session's context: in the host, ASP.NET Core loads a second copy of the suite's assemblies by name.
    private static IEnumerable<string> GetSharedFrameworkDirectories()
    {
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var sharedDirectory = runtimeDirectory is null
            ? null
            : Directory.GetParent(runtimeDirectory)?.Parent?.FullName;
        if (sharedDirectory is null || !Directory.Exists(sharedDirectory))
        {
            yield break;
        }

        var running = Path.GetFileName(runtimeDirectory)!;
        foreach (var frameworkDirectory in Directory.EnumerateDirectories(sharedDirectory))
        {
            var exact = Path.Combine(frameworkDirectory, running);
            if (Directory.Exists(exact))
            {
                yield return exact;
                continue;
            }

            var newest = Directory.EnumerateDirectories(frameworkDirectory)
                .Select(directory => (Directory: directory, Version: Version.TryParse(Path.GetFileName(directory), out var version) ? version : null))
                .Where(candidate => candidate.Version?.Major == Environment.Version.Major)
                .OrderByDescending(candidate => candidate.Version)
                .FirstOrDefault();
            if (newest.Directory is not null)
            {
                yield return newest.Directory;
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
