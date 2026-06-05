using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Core.Composition;

public sealed class TestComposition
{
    private readonly IServiceProvider _serviceProvider;

    private TestComposition(
        TestCompositionCatalog catalog,
        IServiceProvider serviceProvider)
    {
        Catalog = catalog;
        _serviceProvider = serviceProvider;
    }

    public TestCompositionCatalog Catalog { get; }

    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    public static TestComposition ForFixture(
        object fixture,
        Action<IServiceCollection>? configureServices = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var options = TestCompositionDiscoveryOptions.FromFixture(fixture.GetType());
        var catalog = TestCompositionCatalog.Discover(options);
        var services = new ServiceCollection();

        AddSingletonIfMissing(services, fixture.GetType(), fixture);
        foreach (var interfaceType in fixture.GetType().GetInterfaces())
        {
            AddSingletonIfMissing(services, interfaceType, fixture);
        }

        configureServices?.Invoke(services);
        catalog.AddDiscoveredServices(services);

        return new TestComposition(
            catalog,
            services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = false,
                ValidateScopes = true
            }));
    }

    private static void AddSingletonIfMissing(IServiceCollection services, Type serviceType, object instance)
    {
        if (services.Any(descriptor => descriptor.ServiceType == serviceType))
        {
            return;
        }

        services.AddSingleton(serviceType, instance);
    }
}

public sealed class TestCompositionCatalog
{
    private TestCompositionCatalog(
        IReadOnlyList<Type> scannedTypes,
        IReadOnlyList<TestPageDescriptor> pages,
        IReadOnlyList<TestServiceDescriptor> services,
        IReadOnlyList<string> diagnostics)
    {
        ScannedTypes = scannedTypes;
        Pages = pages;
        Services = services;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<Type> ScannedTypes { get; }

    public IReadOnlyList<TestPageDescriptor> Pages { get; }

    public IReadOnlyList<TestServiceDescriptor> Services { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    public static TestCompositionCatalog Discover(TestCompositionDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<Type> scannedTypes = [];
        List<TestPageDescriptor> pages = [];
        List<TestServiceDescriptor> services = [];
        List<string> diagnostics = [];

        foreach (var type in options.Assemblies.SelectMany(GetLoadableTypes))
        {
            if (!IsCandidateType(type) || !IsInSelectedNamespace(type, options.NamespacePrefixes))
            {
                continue;
            }

            scannedTypes.Add(type);

            var pageAttribute = type.GetCustomAttribute<TestPageAttribute>();
            if (pageAttribute is not null || typeof(IPageObject).IsAssignableFrom(type))
            {
                pages.Add(new TestPageDescriptor(
                    ResolvePageName(type, pageAttribute),
                    type,
                    pageAttribute?.Lifetime ?? ServiceLifetime.Scoped,
                    type.Assembly.Location));
                continue;
            }

            var serviceAttribute = type.GetCustomAttribute<TestScenarioServiceAttribute>();
            if (serviceAttribute is not null || typeof(TestScenarioServiceBase).IsAssignableFrom(type))
            {
                services.Add(new TestServiceDescriptor(
                    type,
                    serviceAttribute?.Lifetime ?? ServiceLifetime.Scoped,
                    type.Assembly.Location));
            }
        }

        AddDuplicatePageDiagnostics(pages, diagnostics);

        return new TestCompositionCatalog(scannedTypes, pages, services, diagnostics);
    }

    public void AddDiscoveredServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var page in Pages)
        {
            AddIfMissing(services, page.PageType, page.Lifetime);
        }

        foreach (var service in Services)
        {
            AddIfMissing(services, service.Type, service.Lifetime);
        }
    }

    private static void AddIfMissing(IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        if (services.Any(descriptor => descriptor.ServiceType == type))
        {
            return;
        }

        services.Add(new ServiceDescriptor(type, type, lifetime));
    }

    private static bool IsCandidateType(Type type)
    {
        return type is { IsAbstract: false, IsClass: true } &&
               (type.IsVisible || type.IsNestedPublic) &&
               type.GetCustomAttribute<TestCompositionIgnoreAttribute>() is null;
    }

    private static bool IsInSelectedNamespace(Type type, IReadOnlyList<string> namespacePrefixes)
    {
        if (namespacePrefixes.Count == 0)
        {
            return true;
        }

        return namespacePrefixes.Any(prefix =>
            type.Namespace?.StartsWith(prefix, StringComparison.Ordinal) == true);
    }

    private static string ResolvePageName(Type pageType, TestPageAttribute? attribute)
    {
        if (!string.IsNullOrWhiteSpace(attribute?.Name))
        {
            return attribute.Name;
        }

        var name = pageType.Name.EndsWith("Page", StringComparison.Ordinal)
            ? pageType.Name[..^"Page".Length]
            : pageType.Name;

        return SplitPascalCase(name);
    }

    private static string SplitPascalCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        List<string> words = [];
        var start = 0;
        for (var i = 1; i < value.Length; i++)
        {
            if (char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
            {
                words.Add(value[start..i]);
                start = i;
            }
        }

        words.Add(value[start..]);
        return string.Join(' ', words);
    }

    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>().ToArray();
        }
    }

    private static void AddDuplicatePageDiagnostics(
        IReadOnlyList<TestPageDescriptor> pages,
        ICollection<string> diagnostics)
    {
        foreach (var duplicate in pages
                     .GroupBy(page => page.Name, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add($"Duplicate test page name '{duplicate.Key}'.");
        }
    }
}

public sealed class TestCompositionDiscoveryOptions
{
    public TestCompositionDiscoveryOptions(
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyList<string> namespacePrefixes)
    {
        Assemblies = assemblies;
        NamespacePrefixes = namespacePrefixes;
    }

    public IReadOnlyList<Assembly> Assemblies { get; }

    public IReadOnlyList<string> NamespacePrefixes { get; }

    public static TestCompositionDiscoveryOptions FromFixture(Type fixtureType)
    {
        ArgumentNullException.ThrowIfNull(fixtureType);

        var scans = fixtureType
            .GetCustomAttributes<TestModuleScanAttribute>()
            .ToArray();

        if (scans.Length == 0)
        {
            return new TestCompositionDiscoveryOptions([fixtureType.Assembly], []);
        }

        var assemblies = scans
            .Select(scan => scan.AssemblyMarkerType.Assembly)
            .Distinct()
            .ToArray();
        var namespacePrefixes = scans
            .Select(scan => scan.NamespacePrefix)
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .Select(prefix => prefix!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new TestCompositionDiscoveryOptions(assemblies, namespacePrefixes);
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TestCompositionIgnoreAttribute : Attribute
{
}

public sealed record TestPageDescriptor(
    string Name,
    Type PageType,
    ServiceLifetime Lifetime,
    string SourcePath);

public sealed record TestServiceDescriptor(
    Type Type,
    ServiceLifetime Lifetime,
    string SourcePath);
