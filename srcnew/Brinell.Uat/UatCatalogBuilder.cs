using System.Reflection;
using System.Text.RegularExpressions;
using Brinell.Core.Interfaces;

namespace Brinell.Uat;

/// <summary>
/// A control step discovered from a <see cref="UatPhraseAttribute"/> on a control method. The engine invokes
/// <see cref="MethodName"/> by name, so no reference to the control type is needed.
/// </summary>
internal sealed record UatStepBinding(
    UatEffectiveStepKeyword Keyword,
    string Phrase,
    string CommandSuffix,
    string MethodName,
    string? ValueArgumentName,
    bool? Literal,
    Type? ValueType = null);

/// <summary>
/// Discovers control-step vocabulary from <see cref="UatPhraseAttribute"/> declarations on control methods and
/// projects it into a <see cref="UatCommandCatalog"/>. This replaces the hand-written phrase
/// tables: the runtime and spec catalogs are the same discovery pass with and without handlers.
/// </summary>
internal static class UatCatalogBuilder
{
    // The Brinell.Core assembly declares the built-in control vocabulary via [UatWhen]/[UatThen].
    // A type whose dependency is missing (xunit, when the brinell-data tool loads Core) is skipped.
    private static readonly Type[] BuiltinStepTypes = LoadableTypes(typeof(IElementObject<>).Assembly);

    public static IReadOnlyList<Type> CoreStepTypes => BuiltinStepTypes;

    private static Type[] LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>().ToArray();
        }
    }

    public static void RegisterControlVerbs(
        UatCommandCatalog catalog,
        IEnumerable<Type> stepTypes,
        string commandIdPrefix,
        Func<UatStepBinding, UatCommandHandler?>? handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(stepTypes);
        ArgumentNullException.ThrowIfNull(commandIdPrefix);

        foreach (var binding in DiscoverBindings(stepTypes, catalog.AddUnmatchable))
        {
            catalog.Register(
                binding.Keyword,
                binding.Phrase,
                commandIdPrefix + binding.CommandSuffix,
                allowsTable: false,
                handler: handlerFactory?.Invoke(binding),
                parameters: binding is { ValueArgumentName: { } name, ValueType: { } type }
                    ? [new UatPhraseParameter(name, type, UatParameterSource.Phrase, Optional: false, AllowsNull: false)]
                    : []);
        }
    }

    public static IReadOnlyList<UatStepBinding> DiscoverBindings(IEnumerable<Type> stepTypes, Action<UatUnmatchableDeclaration>? unmatchable = null)
    {
        List<UatStepBinding> bindings = [];
        HashSet<(UatEffectiveStepKeyword Keyword, string Phrase)> seen = [];

        foreach (var type in stepTypes)
        {
            foreach (var method in type.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var attribute in method.GetCustomAttributes<UatPhraseAttribute>(inherit: false))
                {
                    // A control verb declares {control} and its command in text; it is never inferred.
                    if (attribute.IsInferred)
                    {
                        unmatchable?.Invoke(new UatUnmatchableDeclaration(
                            attribute.Keyword,
                            new UatPhraseProblem(
                                "UATA013",
                                $"is a control verb with an empty [Uat{attribute.Keyword}]; a control verb writes its phrase, with {{control}}.",
                                $"{type.FullName}.{method.Name}")));
                        continue;
                    }

                    if (!seen.Add((attribute.Keyword, attribute.Phrase)))
                    {
                        continue;
                    }

                    var valueArgument = ResolveValueArgument(attribute.Phrase);
                    bindings.Add(new UatStepBinding(
                        attribute.Keyword,
                        attribute.Phrase,
                        attribute.CommandId ?? method.Name,
                        method.Name,
                        valueArgument,
                        attribute.HasLiteral ? attribute.Literal : null,
                        valueArgument is null ? null : method.GetParameters().FirstOrDefault()?.ParameterType is { IsGenericParameter: false } valueType ? valueType : null));
                }
            }
        }

        return bindings;
    }

    // The one placeholder that is neither {control} nor {user} (the actor, AD-015) is the value
    // argument; literal-only phrases have none.
    private static string? ResolveValueArgument(string phrase)
    {
        var names = Regex.Matches(phrase, @"\{([A-Za-z][A-Za-z0-9_-]*)\}")
            .Select(match => match.Groups[1].Value)
            .Where(name => name is not ("control" or "user"))
            .ToArray();
        return names.Length == 1 ? names[0] : null;
    }
}
