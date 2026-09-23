using System.Reflection;
using System.Text.RegularExpressions;
using Brinell.Core.Interfaces;

namespace Brinell.Uat;

/// <summary>
/// A control step discovered from a <see cref="UatStepAttribute"/>. The engine invokes
/// <see cref="MethodName"/> by name, so no reference to the control type is needed.
/// </summary>
internal sealed record UatStepBinding(
    UatEffectiveStepKeyword Keyword,
    string Phrase,
    string CommandSuffix,
    string MethodName,
    string? ValueArgumentName,
    bool? Literal);

/// <summary>
/// Discovers control-step vocabulary from <see cref="UatStepAttribute"/> declarations and
/// projects it into a <see cref="UatCommandCatalog"/>. This replaces the hand-written phrase
/// tables: the runtime and spec catalogs are the same discovery pass with and without handlers.
/// </summary>
internal static class UatCatalogBuilder
{
    // The Brinell.Core assembly declares the built-in control vocabulary via [UatStep].
    private static readonly Type[] BuiltinStepTypes = typeof(IElementObject<>).Assembly.GetTypes();

    public static IReadOnlyList<Type> CoreStepTypes => BuiltinStepTypes;

    public static void RegisterControlVerbs(
        UatCommandCatalog catalog,
        IEnumerable<Type> stepTypes,
        string commandIdPrefix,
        Func<UatStepBinding, UatCommandHandler?>? handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(stepTypes);
        ArgumentNullException.ThrowIfNull(commandIdPrefix);

        foreach (var binding in DiscoverBindings(stepTypes))
        {
            catalog.Register(
                binding.Keyword,
                binding.Phrase,
                commandIdPrefix + binding.CommandSuffix,
                allowsTable: false,
                handler: handlerFactory?.Invoke(binding));
        }
    }

    public static IReadOnlyList<UatStepBinding> DiscoverBindings(IEnumerable<Type> stepTypes)
    {
        List<UatStepBinding> bindings = [];
        HashSet<(UatEffectiveStepKeyword Keyword, string Phrase)> seen = [];

        foreach (var type in stepTypes)
        {
            foreach (var method in type.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var attribute in method.GetCustomAttributes<UatStepAttribute>(inherit: false))
                {
                    if (!seen.Add((attribute.Keyword, attribute.Phrase)))
                    {
                        continue;
                    }

                    bindings.Add(new UatStepBinding(
                        attribute.Keyword,
                        attribute.Phrase,
                        attribute.CommandId ?? method.Name,
                        method.Name,
                        ResolveValueArgument(attribute.Phrase),
                        attribute.HasLiteral ? attribute.Literal : null));
                }
            }
        }

        return bindings;
    }

    // The one non-{control} placeholder is the value argument; literal-only phrases have none.
    private static string? ResolveValueArgument(string phrase)
    {
        var names = Regex.Matches(phrase, @"\{([A-Za-z][A-Za-z0-9_-]*)\}")
            .Select(match => match.Groups[1].Value)
            .Where(name => !name.Equals("control", StringComparison.Ordinal))
            .ToArray();
        return names.Length == 1 ? names[0] : null;
    }
}
