using System.Reflection;
using Brinell.Core.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Uat;

/// <summary>
/// The step definitions of a suite that are not about a screen: the fixture's own methods and the
/// step-definition classes of its composition, found by reflection, with their arguments read from
/// the step and their calls made.
/// </summary>
public sealed class UatStepDefinitionRuntime
{
    private const string ServiceProviderItemKey = "Uat.Reflection.ServiceProvider";
    private readonly object _root;
    private readonly Type _rootType;
    private readonly IReadOnlyList<UatPhraseClassBinding> _phraseClasses;
    private readonly UatArgumentTypes _argumentTypes;

    /// <summary>Creates the runtime over a fixture, or over its type only for checking.</summary>
    /// <param name="root">The fixture; null when built for checking, whose handlers never run.</param>
    /// <param name="rootType">The fixture's type.</param>
    /// <param name="phraseClasses">The step-definition classes of its composition.</param>
    /// <param name="argumentTypes">The argument types its steps can read.</param>
    public UatStepDefinitionRuntime(object? root, Type rootType, IEnumerable<UatPhraseClassBinding>? phraseClasses, UatArgumentTypes argumentTypes)
    {
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(argumentTypes);
        _root = root!;
        _rootType = rootType;
        _phraseClasses = phraseClasses?.ToArray() ?? [];
        _argumentTypes = argumentTypes;
    }

    /// <summary>The step-definition classes and their methods.</summary>
    public IReadOnlyList<UatPhraseClassBinding> PhraseClasses => _phraseClasses;

    /// <summary>The argument types its steps can read.</summary>
    public UatArgumentTypes ArgumentTypes => _argumentTypes;

    /// <summary>The scope the step-definition classes are resolved from, for one scenario.</summary>
    /// <param name="context">The scenario's execution context.</param>
    /// <param name="serviceProvider">The scenario's scope.</param>
    public static void SetServiceProvider(UatExecutionContext context, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        context.AppItems[ServiceProviderItemKey] = serviceProvider;
    }

    internal void RegisterPhraseClasses(UatCommandCatalog catalog)
        => RegisterPhraseClassPhrases(
            catalog,
            _phraseClasses,
            (phraseClassType, method) => (context, invocation, cancellationToken) =>
                InvokePhraseClassMethodAsync(
                    phraseClassType,
                    method,
                    context,
                    invocation,
                    cancellationToken));

    internal static void RegisterPhraseClassPhrases(
        UatCommandCatalog catalog,
        IEnumerable<UatPhraseClassBinding> phraseClasses,
        Func<Type, MethodInfo, UatCommandHandler?>? handlerFactory)
    {
        foreach (var phraseClass in phraseClasses)
        {
            foreach (var method in phraseClass.Methods)
            {
                foreach (var phrase in method.Phrases)
                {
                    catalog.Register(
                        phrase.Keyword,
                        phrase.Phrase,
                        $"{method.Method.DeclaringType!.FullName}.{method.Method.Name}",
                        handler: handlerFactory?.Invoke(phraseClass.Type, method.Method),
                        parameters: phrase.Parameters,
                        problem: phrase.Problem,
                        phraseNamespace: phraseClass.Namespace);
                }

                foreach (var unmatchable in method.Unmatchable)
                {
                    catalog.AddUnmatchable(unmatchable);
                }
            }
        }
    }

    internal void RegisterRootPhrases(UatCommandCatalog catalog)
    {
        RegisterRootPhrases(
            catalog,
            _rootType,
            method => (context, invocation, cancellationToken) =>
                InvokeRootPhraseAsync(method, context, invocation, cancellationToken));
    }

    internal static void RegisterRootPhrases(
        UatCommandCatalog catalog,
        Type rootType,
        Func<MethodInfo, UatCommandHandler?>? handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rootType);

        foreach (var method in rootType
                     .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                     .Where(method => method.GetCustomAttribute<UatIgnoreAttribute>() is null))
        {
            // On the fixture only an attribute makes a phrase: a public method that happens to
            // start with When stays a method.
            var (phrases, unmatchable) = UatPhraseDeclarations.Of(method, inferWithoutAttribute: false, catalog.ArgumentTypes);
            var commandId = $"{method.DeclaringType!.FullName}.{method.Name}";
            foreach (var phrase in phrases)
            {
                // A global phrase class's phrase takes precedence over the fixture's. A page phrase
                // or a namespace's phrase of another method does not: the same text there and here
                // is ambiguous where both are seen (D5).
                if (catalog.Patterns.Any(pattern =>
                        ((!pattern.IsPageScoped && pattern.Namespace.Length == 0) || pattern.CommandId == commandId) &&
                        pattern.Keyword == phrase.Keyword &&
                        pattern.Phrase.Equals(phrase.Phrase, StringComparison.Ordinal)))
                {
                    continue;
                }

                catalog.Register(
                    phrase.Keyword,
                    phrase.Phrase,
                    commandId,
                    handler: handlerFactory?.Invoke(method),
                    parameters: phrase.Parameters,
                    problem: phrase.Problem);
            }

            foreach (var declaration in unmatchable)
            {
                catalog.AddUnmatchable(declaration);
            }
        }
    }

    private async Task<UatStepResult> InvokeRootPhraseAsync(
        MethodInfo method,
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = UatInvoker.Invoke(_root, method, BuildPhraseArguments(method, context, invocation, cancellationToken));
            if (result is Task<UatStepResult> resultTask)
            {
                return await resultTask.ConfigureAwait(false);
            }

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                context.Diagnostics.Add($"custom:{method.Name}");
                return UatStepResult.Passed(invocation);
            }

            if (result is UatStepResult stepResult)
            {
                return stepResult;
            }

            if (result is bool boolean && !boolean)
            {
                return UatStepResult.Failed(invocation, $"Custom UAT step '{method.Name}' returned false.");
            }

            context.Diagnostics.Add($"custom:{method.Name}");
            return UatStepResult.Passed(invocation, result as string);
        }
        catch (Exception ex)
        {
            return UatStepResult.Failed(invocation, $"Custom UAT step '{method.Name}' failed: {ex.Message}", ex);
        }
    }

    private async Task<UatStepResult> InvokePhraseClassMethodAsync(
        Type phraseClassType,
        MethodInfo method,
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetServiceProvider(context, out var serviceProvider))
        {
            return UatStepResult.Failed(
                invocation,
                $"UAT phrase class '{phraseClassType.FullName}' requires an active DI scope.");
        }

        try
        {
            var instance = serviceProvider.GetService(phraseClassType) ??
                           ActivatorUtilities.CreateInstance(serviceProvider, phraseClassType);
            var result = UatInvoker.Invoke(instance, method, BuildPhraseArguments(method, context, invocation, cancellationToken));
            if (result is Task<UatStepResult> resultTask)
            {
                return await resultTask.ConfigureAwait(false);
            }

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                context.Diagnostics.Add($"phrase:{method.Name}");
                return UatStepResult.Passed(invocation);
            }

            if (result is UatStepResult stepResult)
            {
                return stepResult;
            }

            if (result is bool boolean && !boolean)
            {
                return UatStepResult.Failed(invocation, $"UAT phrase '{method.Name}' returned false.");
            }

            context.Diagnostics.Add($"phrase:{method.Name}");
            return UatStepResult.Passed(invocation, result as string);
        }
        catch (Exception ex)
        {
            return UatStepResult.Failed(invocation, $"UAT phrase '{method.Name}' failed: {ex.Message}", ex);
        }
    }

    internal object?[] BuildPhraseArguments(
        MethodInfo method,
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        var values = ValueContext(context);
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.ParameterType == typeof(UatExecutionContext))
            {
                arguments[i] = context;
                continue;
            }

            if (parameter.ParameterType == typeof(UatStepInvocation))
            {
                arguments[i] = invocation;
                continue;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                arguments[i] = cancellationToken;
                continue;
            }

            if (parameter.ParameterType == typeof(UatTable))
            {
                arguments[i] = invocation.Step.Table
                    ?? (parameter.IsOptional
                        ? null
                        : throw new InvalidOperationException(
                            $"Custom UAT step '{method.Name}' needs a table under the step ({invocation.Step.Source})."));
                continue;
            }

            if (UatPhraseDeclarations.IsSettingsType(parameter.ParameterType, method.DeclaringType?.Assembly))
            {
                arguments[i] = context.GetSettings(parameter.ParameterType);
                continue;
            }

            if (invocation.Command.Parameters.FirstOrDefault(p => p.Name == parameter.Name) is { } typed)
            {
                arguments[i] = UatArguments.Read(typed, invocation, values);
                continue;
            }

            // A pattern registered without types (by hand, or by an older catalog): text as before.
            if (parameter.Name is not null &&
                invocation.RawArguments.TryGetValue(parameter.Name, out var value))
            {
                arguments[i] = UatValues.Read(value, parameter.ParameterType, values);
                continue;
            }

            arguments[i] = parameter.IsOptional
                ? Type.Missing
                : throw new InvalidOperationException(
                    $"Custom UAT step '{method.Name}' requires parameter '{parameter.Name}' but no matching phrase argument was found.");
        }

        return arguments;
    }

    internal UatValueContext ValueContext(UatExecutionContext context)
        => new(
            context.Now ?? context.Clock?.Now ?? DateTimeOffset.UtcNow,
            context.Apps,
            _argumentTypes,
            TryGetServiceProvider(context, out var services) ? services : null);

    internal static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    internal static IReadOnlyList<UatPhraseClassBinding> DiscoverPhraseClasses(IReadOnlyList<Type> types, UatArgumentTypes argumentTypes)
    {
        List<UatPhraseClassBinding> phraseClasses = [];

        foreach (var type in types.Where(IsPhraseClassType))
        {
            List<UatPhraseClassMethodBinding> methods = [];
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName ||
                    method.GetCustomAttribute<UatIgnoreAttribute>() is not null)
                {
                    continue;
                }

                var (phrases, unmatchable) = UatPhraseDeclarations.Of(method, inferWithoutAttribute: true, argumentTypes);
                if (phrases.Count == 0 && unmatchable.Count == 0)
                {
                    continue;
                }

                methods.Add(new UatPhraseClassMethodBinding(
                    method,
                    phrases.Select(phrase => new UatPhraseMethodPhrase(phrase.Phrase, phrase.Keyword)
                    {
                        Parameters = phrase.Parameters,
                        Problem = phrase.Problem,
                    }).ToArray())
                {
                    Unmatchable = unmatchable,
                });
            }

            if (methods.Count > 0)
            {
                phraseClasses.Add(new UatPhraseClassBinding(type, methods) { Namespace = NamespaceOf(type) });
            }
        }

        return phraseClasses;
    }

    // The phrase class's own namespace, else its [BddNamespace] or its assembly's; the root without any.
    private static string NamespaceOf(Type type)
        => UatNamespaces.Normalize(
            type.GetCustomAttribute<BddStepDefinitionsAttribute>()?.Namespace ??
            BddNamespaceAttribute.Of(type));

    private static bool IsPhraseClassType(Type type)
    {
        return type.GetCustomAttribute<BddStepDefinitionsAttribute>() is not null ||
               typeof(BddStepDefinitionsBase).IsAssignableFrom(type);
    }

    internal static bool TryGetServiceProvider(UatExecutionContext context, out IServiceProvider serviceProvider)
    {
        if (context.AppItems.TryGetValue(ServiceProviderItemKey, out var value) &&
            value is IServiceProvider found)
        {
            serviceProvider = found;
            return true;
        }

        serviceProvider = null!;
        return false;
    }
}

public sealed record UatPhraseClassBinding(
    Type Type,
    IReadOnlyList<UatPhraseClassMethodBinding> Methods)
{
    /// <summary>The namespace its phrases are seen in (<see cref="UatNamespaces"/>); <c>""</c> for every scenario.</summary>
    public string Namespace { get; init; } = string.Empty;
}

public sealed record UatPhraseClassMethodBinding(
    MethodInfo Method,
    IReadOnlyList<UatPhraseMethodPhrase> Phrases)
{
    /// <summary>The method's broken declarations that have no phrase to match.</summary>
    public IReadOnlyList<UatUnmatchableDeclaration> Unmatchable { get; init; } = [];
}

public sealed record UatPhraseMethodPhrase(
    string Phrase,
    UatEffectiveStepKeyword Keyword)
{
    /// <summary>What a step supplies, with types.</summary>
    public IReadOnlyList<UatPhraseParameter> Parameters { get; init; } = [];

    /// <summary>Why the phrase cannot run; null when it is fine.</summary>
    public UatPhraseProblem? Problem { get; init; }
}
