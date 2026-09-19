using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace Brinell.Samples.Todo.JourneyCoverage;

/// <summary>One test method and the traits it carries, its class's included.</summary>
/// <param name="Name">The class and method, <c>ListJourneyTests.Row_ShowsTitleDueDateAndStatus</c>.</param>
/// <param name="Pyramid">The <c>Pyramid</c> trait, or null when it has none.</param>
/// <param name="Gates">The <c>Gate</c> traits: <c>Smoke</c>, <c>Review</c>.</param>
/// <param name="Journeys">The <c>Journey</c> traits: subtask ids.</param>
/// <param name="Cases">How many cases it runs: 1 for a fact, one per data row for a theory.</param>
public sealed record TestMethod(
    string Name,
    string? Pyramid,
    IReadOnlyList<string> Gates,
    IReadOnlyList<string> Journeys,
    int Cases)
{
    /// <summary>Whether this test counts for <paramref name="tier"/>.</summary>
    public bool Covers(Tier tier) => tier switch
    {
        Tier.Unit => Pyramid == "Unit",
        Tier.Integration => Pyramid == "Integration",
        Tier.Contract => Pyramid == "Contract",
        Tier.UiHermetic => Pyramid == "UiHermetic",
        Tier.UiLive => Pyramid == "UiLive",
        Tier.Smoke => Gates.Contains("Smoke"),
        Tier.Review => Gates.Contains("Review"),
        _ => false,
    };
}

/// <summary>
/// The tests of the sample's test assemblies, read by reflection: nothing is run and no app is launched.
/// </summary>
public static class TestCatalog
{
    /// <summary>The assemblies whose tests the report compares with <c>journeys.md</c>.</summary>
    public static readonly string[] AssemblyNames =
    [
        "Brinell.Samples.Todo.UnitTests",
        "Brinell.Samples.Todo.IntegrationTests",
        // The UI tests' sources, as the mobile head compiles them.
        "Brinell.Samples.Todo.UITests.Mobile",
    ];

    /// <summary>Every test method in <see cref="AssemblyNames"/>.</summary>
    public static IReadOnlyList<TestMethod> Load()
        => [.. AssemblyNames.Select(Assembly.Load).SelectMany(Read)];

    private static IEnumerable<TestMethod> Read(Assembly assembly)
        => from type in assembly.GetTypes()
           where type is { IsClass: true, IsAbstract: false }
           from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
           where method.IsDefined(typeof(FactAttribute), inherit: true)
           select Describe(type, method);

    private static TestMethod Describe(Type type, MethodInfo method)
    {
        // A class's traits apply to each of its tests, as xUnit applies them.
        var traits = Traits(method).Concat(ClassHierarchy(type).SelectMany(Traits)).ToList();

        return new TestMethod(
            $"{type.Name}.{method.Name}",
            traits.FirstOrDefault(t => t.Name == "Pyramid").Value,
            [.. traits.Where(t => t.Name == "Gate").Select(t => t.Value).Distinct()],
            [.. traits.Where(t => t.Name == "Journey").Select(t => t.Value).Distinct()],
            CountCases(method));
    }

    private static IEnumerable<Type> ClassHierarchy(Type type)
    {
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            yield return current;
        }
    }

    // TraitAttribute keeps its name and value only as constructor arguments.
    private static IEnumerable<(string Name, string Value)> Traits(MemberInfo member)
        => member.GetCustomAttributesData()
            .Where(a => a.AttributeType == typeof(TraitAttribute))
            .Select(a => ((string)a.ConstructorArguments[0].Value!, (string)a.ConstructorArguments[1].Value!));

    private static int CountCases(MethodInfo method)
    {
        var data = method.GetCustomAttributes<DataAttribute>(inherit: true).ToList();
        if (data.Count == 0) return 1;

        try
        {
            // MemberData runs a static member of the test class: rule tables, no side effects.
            return Math.Max(1, data.Sum(attribute => attribute.GetData(method).Count()));
        }
        catch (Exception)
        {
            return data.Count;
        }
    }
}
