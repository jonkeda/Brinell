using System.Reflection;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

/// <summary>
/// Lists the fixtures in a Pages assembly, and what each one implies, without loading it.
/// </summary>
/// <remarks>
/// Reflection-only, through <see cref="MetadataLoadContext" />: it releases the file when
/// disposed, where <c>LoadFromAssemblyPath</c> into the default context holds it for the life
/// of the process and would block the next build. Attribute arguments are readable this way;
/// property values are not, which is why a fixture declares its platforms with an attribute.
/// See <c>.my/present/startup/00-presenter-workspace-design.md</c>.
/// </remarks>
public sealed class FixtureInspector : IFixtureInspector
{
    /// <inheritdoc />
    public FixtureInspection Inspect(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        if (!File.Exists(assemblyPath))
        {
            return new FixtureInspection(
                false,
                [],
                $"{Path.GetFileName(assemblyPath)} is not built.");
        }

        try
        {
            List<string> searchPaths =
            [
                .. Directory.GetFiles(Path.GetDirectoryName(assemblyPath)!, "*.dll"),
                .. Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll")
            ];

            using MetadataLoadContext context = new(new PathAssemblyResolver(searchPaths));
            var assembly = context.LoadFromAssemblyPath(assemblyPath);

            List<FixtureCandidate> candidates = [];
            foreach (var type in GetTypes(assembly))
            {
                if (!type.IsClass || type.IsNested)
                {
                    continue;
                }

                var baseChain = BaseChain(type);
                var fixtureBase = baseChain.FirstOrDefault(UatTargetConventions.IsFixtureBase);
                var attributes = type.GetCustomAttributesData();
                var scansPages = attributes.Any(attribute =>
                    attribute.AttributeType.Name.Contains("TestModuleScan", StringComparison.Ordinal));

                // Anything that could plausibly be meant as a fixture is listed, with the
                // reason it cannot be used: "there are none" and "they are all abstract"
                // are different problems.
                if (fixtureBase is null && !scansPages && !type.IsAbstract)
                {
                    continue;
                }

                candidates.Add(new FixtureCandidate(
                    type.Name,
                    type.FullName ?? type.Name,
                    !type.IsAbstract,
                    type.GetConstructor(Type.EmptyTypes) is not null,
                    scansPages,
                    fixtureBase,
                    UatTargetConventions.FromBaseTypeNames(baseChain),
                    ReadPlatforms(attributes),
                    baseChain));
            }

            return new FixtureInspection(true, Rank(candidates), string.Empty);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
        {
            return new FixtureInspection(false, [], $"Could not read {Path.GetFileName(assemblyPath)}: {ex.Message}");
        }
    }

    // Usable first, then the ones that scan pages, then by name: the answer should be at the top.
    private static IReadOnlyList<FixtureCandidate> Rank(List<FixtureCandidate> candidates)
    {
        return
        [
            .. candidates
                .OrderByDescending(candidate => candidate.IsUsable)
                .ThenByDescending(candidate => candidate.ScansPages)
                .ThenByDescending(candidate => candidate.FixtureBase is not null)
                .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
        ];
    }

    private static IEnumerable<Type> GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static IReadOnlyList<string> BaseChain(Type type)
    {
        List<string> chain = [];
        for (var current = type.BaseType; current is not null && current.Name != "Object"; current = current.BaseType)
        {
            chain.Add(current.Name);
        }

        return chain;
    }

    private static IReadOnlyList<string> ReadPlatforms(IList<CustomAttributeData> attributes)
    {
        var platforms = attributes.FirstOrDefault(attribute =>
            attribute.AttributeType.Name is "UatPlatformsAttribute" or "UatPlatforms");
        if (platforms is null)
        {
            return [];
        }

        // Declared as a params array or as separate arguments; both arrive as constructor arguments.
        List<string> names = [];
        foreach (var argument in platforms.ConstructorArguments)
        {
            if (argument.Value is IReadOnlyCollection<CustomAttributeTypedArgument> array)
            {
                names.AddRange(array.Select(item => Describe(item.ArgumentType, item.Value)));
            }
            else
            {
                names.Add(Describe(argument.ArgumentType, argument.Value));
            }
        }

        return [.. names.Where(name => name.Length > 0)];
    }

    /// <summary>Names an attribute argument, resolving an enum value to its member name.</summary>
    private static string Describe(Type argumentType, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text;
        }

        // Reflection-only gives the underlying value, so the name comes from the enum's own
        // constants rather than Enum.GetName, which would need the type loaded.
        if (argumentType.IsEnum)
        {
            var member = argumentType
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(field => Equals(field.GetRawConstantValue(), value));
            if (member is not null)
            {
                return member.Name;
            }
        }

        return value.ToString() ?? string.Empty;
    }
}
