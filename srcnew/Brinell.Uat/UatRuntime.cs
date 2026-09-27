using System.Reflection;
using Brinell.Core.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Uat;

public sealed class UatRuntime
{
    private readonly UatReflectionRuntime _reflectionRuntime;
    private readonly object _root;

    public UatRuntime(
        object root,
        string configFilePath,
        UatRuntimeValidationOptions? validationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(configFilePath);

        _root = root;
        ConfigFilePath = Path.GetFullPath(configFilePath);
        ConfigDirectory = Path.GetDirectoryName(ConfigFilePath) ?? Directory.GetCurrentDirectory();
        Config = UatConfigParser.ParseFile(ConfigFilePath, root.GetType().Assembly.GetName().Name);
        ValidateConfig(root, Config, ConfigFilePath, validationOptions ?? UatRuntimeValidationOptions.Default);
        Composition = ResolveComposition(root);
        _reflectionRuntime = Composition is null
            ? UatReflectionRuntime.FromRoot(root)
            : UatReflectionRuntime.FromComposition(root, Composition);
    }

    public string ConfigFilePath { get; }

    public string ConfigDirectory { get; }

    public UatConfig Config { get; }

    public TestComposition? Composition { get; }

    public string DiscoveryReport => string.Join(Environment.NewLine, _reflectionRuntime.DescribeDiscovery());

    public UatCommandCatalog CreateCommandCatalog()
    {
        var catalog = _reflectionRuntime.CreateCommandCatalog();
        if (_root is IUatSessionHost host)
        {
            UatSessionSteps.Register(catalog, host, Config);
        }

        return catalog;
    }

    /// <summary>
    /// The catalogs a document binds against: <paramref name="defaultCatalog"/> for the fixture's
    /// own app and the apps of its head, and one more per head an app of the <c>Apps</c> table
    /// brings (<see cref="IUatSessionHost.CompositionFor"/>).
    /// </summary>
    /// <param name="defaultCatalog">The fixture's own catalog; <see cref="CreateCommandCatalog"/> when null.</param>
    public UatCatalogSet CreateCatalogSet(UatCommandCatalog? defaultCatalog = null)
    {
        defaultCatalog ??= CreateCommandCatalog();
        var suiteHead = Config.Runtime.TryGetValue(UatConfigFields.Head, out var head) && head.Trim() is { Length: > 0 } trimmed
            ? trimmed
            : null;

        Dictionary<string, UatCommandCatalog> byHead = new(StringComparer.OrdinalIgnoreCase);
        List<UatReflectionRuntime> runtimes = [_reflectionRuntime];
        if (_root is IUatSessionHost host)
        {
            foreach (var app in Config.Apps)
            {
                var appHead = app.Head ?? suiteHead;
                if (appHead is null || byHead.ContainsKey(appHead) || host.CompositionFor(app.Name) is not { } composition)
                {
                    continue;
                }

                var runtime = UatReflectionRuntime.FromComposition(_root, composition);
                var catalog = runtime.CreateCommandCatalog();
                UatSessionSteps.Register(catalog, host, Config);
                byHead[appHead] = catalog;
                runtimes.Add(runtime);
            }
        }

        var reserved = runtimes
            .SelectMany(runtime => runtime.Pages)
            .SelectMany(page => page.Controls.Select(control => control.Name).Append(page.Name));
        return new UatCatalogSet(defaultCatalog, Config.Apps, suiteHead, byHead, reserved) { Usings = Config.Usings };
    }

    /// <summary>Gives a scenario's context what it needs to switch apps.</summary>
    /// <param name="context">The context.</param>
    public void ConfigureContext(UatExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_root is IUatSessionHost host)
        {
            context.SessionHost = host;
        }

        context.Apps = Config.Apps;
        if (_root is IUatClock clock)
        {
            context.Clock = clock;
        }
    }

    public IServiceScope? CreateScope()
    {
        return Composition?.CreateScope();
    }

    public static void ConfigureScope(UatExecutionContext context, IServiceProvider serviceProvider)
    {
        UatReflectionRuntime.SetServiceProvider(context, serviceProvider);
    }

    private static TestComposition? ResolveComposition(object root)
    {
        if (root is null)
        {
            return null;
        }

        var properties = root.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var property = FindCompositionProperty(properties, "Composition") ??
                       FindCompositionProperty(properties, "TestComposition");

        return property?.GetValue(root) as TestComposition;
    }

    private static PropertyInfo? FindCompositionProperty(
        IEnumerable<PropertyInfo> properties,
        string propertyName)
    {
        return properties.FirstOrDefault(property =>
            property.Name.Equals(propertyName, StringComparison.Ordinal) &&
            property.GetIndexParameters().Length == 0 &&
            typeof(TestComposition).IsAssignableFrom(property.PropertyType));
    }

    private static void ValidateConfig(
        object root,
        UatConfig config,
        string configFilePath,
        UatRuntimeValidationOptions options)
    {
        // The declared target is checked against the fixture, not against a string in the
        // config: the fixture's base is what a target actually is. A config row still counts,
        // for a fixture that derives from no Brinell base.
        if (!string.IsNullOrWhiteSpace(options.Target))
        {
            var derived = UatTargetConventions.FromFixture(root);
            var declared = config.Runtime.TryGetValue("Target", out var configured) ? configured : null;
            var actual = derived ?? declared;

            if (actual is null || !actual.Equals(options.Target, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    derived is not null
                        ? $"{root.GetType().Name} is a {derived} fixture, but {options.Target} was expected."
                        : $"UAT config '{configFilePath}' must set Runtime Target to {options.Target}.");
            }

            if (derived is not null &&
                declared is not null &&
                !declared.Equals(derived, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"UAT config '{configFilePath}' sets Runtime Target to {declared}, " +
                    $"but {root.GetType().Name} is a {derived} fixture.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.Fixture) &&
            (!config.Runtime.TryGetValue("Fixture", out var fixture) ||
             !fixture.Equals(options.Fixture, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must set Runtime Fixture to {options.Fixture}.");
        }

        if (options.RequireAssemblies && config.Assemblies.Count == 0)
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must register at least one assembly.");
        }
    }
}

public sealed record UatRuntimeValidationOptions(
    string? Target = null,
    string? Fixture = null,
    bool RequireAssemblies = true)
{
    public static UatRuntimeValidationOptions Default { get; } = new();
}
