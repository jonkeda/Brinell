using System.Reflection;
using Brinell.Core.Composition;
using Brinell.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Uat;

public sealed class UatReflectionRuntime
{
    private const string CurrentPageItemKey = "Uat.Reflection.CurrentPage";
    private const string ServiceProviderItemKey = "Uat.Reflection.ServiceProvider";
    private readonly Dictionary<string, UatRuntimePage> _pages;
    private readonly IReadOnlyList<UatPhraseClassBinding> _phraseClasses;
    private readonly object _root;
    private readonly Type _rootType;
    private readonly UatArgumentTypes _argumentTypes;

    private UatReflectionRuntime(
        object? root,
        IEnumerable<UatRuntimePage> pages,
        IEnumerable<UatPhraseClassBinding>? phraseClasses = null,
        UatArgumentTypes? argumentTypes = null,
        Type? rootType = null)
    {
        // A runtime built for checking has a fixture type and no fixture; its handlers never run.
        _root = root!;
        _rootType = rootType ?? root!.GetType();
        _argumentTypes = argumentTypes ?? UatArgumentTypes.Empty;
        _phraseClasses = phraseClasses?.ToArray() ?? [];
        _pages = new Dictionary<string, UatRuntimePage>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            _pages[page.Name] = page;
        }
    }

    public IReadOnlyCollection<UatRuntimePage> Pages => _pages.Values;

    public IReadOnlyList<UatPhraseClassBinding> PhraseClasses => _phraseClasses;

    public IReadOnlyList<string> DescribeDiscovery()
    {
        List<string> lines = ["Discovered UAT pages:"];
        foreach (var page in Pages.OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase))
        {
            var controls = page.Controls.Count == 0
                ? "(no controls)"
                : string.Join(", ", page.Controls.Select(control => control.Name).Order(StringComparer.OrdinalIgnoreCase));
            lines.Add($"- {page.Name}: {controls}");
        }

        return lines;
    }

    public static UatReflectionRuntime FromRoot(object root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var rootType = root.GetType();
        List<UatRuntimePage> pages = [];

        foreach (var property in rootType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0 ||
                property.GetCustomAttribute<UatIgnoreAttribute>() is not null ||
                !IsRuntimePageProperty(property))
            {
                continue;
            }

            var page = property.GetValue(root);
            if (page is null)
            {
                continue;
            }

            var name = ResolveRuntimeName(property);
            var navigation = FindNavigationMethod(rootType, name);
            pages.Add(new UatRuntimePage(
                name,
                page.GetType(),
                page,
                DiscoverControls(page),
                navigation is null ? null : () => Invoke(root, navigation)));
        }

        return new UatReflectionRuntime(root, pages, argumentTypes: UatArgumentTypes.Discover(LoadableTypes(rootType.Assembly)));
    }

    public static UatReflectionRuntime FromComposition(object root, TestComposition composition)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(composition);

        var rootType = root.GetType();
        var pages = composition.Catalog.Pages
            .Select(page =>
            {
                var navigation = FindNavigationMethod(rootType, page.Name);
                return new UatRuntimePage(
                    page.Name,
                    page.PageType,
                    Instance: null,
                    DiscoverControls(page.PageType),
                    navigation is null ? null : () => Invoke(root, navigation));
            })
            .ToArray();
        var argumentTypes = UatArgumentTypes.Discover(composition.Catalog.ScannedTypes.Concat(LoadableTypes(rootType.Assembly)));
        var phraseClasses = DiscoverPhraseClasses(composition.Catalog.ScannedTypes, argumentTypes);

        return new UatReflectionRuntime(root, pages, phraseClasses, argumentTypes);
    }

    /// <summary>
    /// The phrases of <paramref name="fixtureType"/> without creating the fixture: for checking
    /// scenario files (the <c>brinell-data</c> tool), not for running them.
    /// </summary>
    /// <param name="fixtureType">The suite's fixture, as the config's <c>Fixture</c> names it.</param>
    /// <param name="modules">A head's modules, for a fixture with several heads; the fixture's own scan when null.</param>
    public static UatReflectionRuntime ForChecking(Type fixtureType, TestCompositionCatalog? modules = null)
    {
        ArgumentNullException.ThrowIfNull(fixtureType);

        var usesComposition = fixtureType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Any(property => property.Name is "Composition" or "TestComposition" && typeof(TestComposition).IsAssignableFrom(property.PropertyType));
        if (!usesComposition && modules is null)
        {
            var rootPages = fixtureType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.GetIndexParameters().Length == 0 &&
                                   property.GetCustomAttribute<UatIgnoreAttribute>() is null &&
                                   IsRuntimePageProperty(property))
                .Select(property => new UatRuntimePage(ResolveRuntimeName(property), property.PropertyType, Instance: null, DiscoverControls(property.PropertyType), Navigate: null));
            return new UatReflectionRuntime(null, rootPages, argumentTypes: UatArgumentTypes.Discover(LoadableTypes(fixtureType.Assembly)), rootType: fixtureType);
        }

        var catalog = modules ?? TestCompositionCatalog.Discover(TestCompositionDiscoveryOptions.FromFixture(fixtureType));
        var pages = catalog.Pages.Select(page => new UatRuntimePage(page.Name, page.PageType, Instance: null, DiscoverControls(page.PageType), Navigate: null));
        var argumentTypes = UatArgumentTypes.Discover(catalog.ScannedTypes.Concat(LoadableTypes(fixtureType.Assembly)));
        return new UatReflectionRuntime(null, pages, DiscoverPhraseClasses(catalog.ScannedTypes, argumentTypes), argumentTypes, fixtureType);
    }

    public static void SetServiceProvider(UatExecutionContext context, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        context.AppItems[ServiceProviderItemKey] = serviceProvider;
    }

    public static void RegisterRootPhrases(UatCommandCatalog catalog, Type rootType)
    {
        RegisterRootPhrases(catalog, rootType, handlerFactory: null);
    }

    public static void RegisterCompositionPhrases(UatCommandCatalog catalog, Type rootType)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rootType);

        var options = TestCompositionDiscoveryOptions.FromFixture(rootType);
        var compositionCatalog = TestCompositionCatalog.Discover(options);
        RegisterPhraseClassPhrases(
            catalog,
            DiscoverPhraseClasses(compositionCatalog.ScannedTypes, UatArgumentTypes.Discover(compositionCatalog.ScannedTypes)),
            handlerFactory: null);
    }

    public UatCommandCatalog CreateCommandCatalog()
    {
        var catalog = new UatCommandCatalog { ArgumentTypes = _argumentTypes };

        // The engine's three intrinsic page-graph verbs — defined once, here, each with its named
        // form for a scenario with several users (AD-015).
        foreach (var phrase in new[] { "I am on the {page} page", "{user} is on the {page} page" })
        {
            catalog.Register(UatEffectiveStepKeyword.Given, phrase, "Builtin.Page.Open", handler: OpenPageAsync, namesPage: true);
        }

        foreach (var phrase in new[] { "I should be on the {page} page", "{user} should be on the {page} page" })
        {
            catalog.Register(UatEffectiveStepKeyword.Then, phrase, "Builtin.Page.AssertOpen", handler: AssertPageAsync, namesPage: true);
        }

        foreach (var phrase in new[] { "I should see {text}", "{user} should see {text}" })
        {
            catalog.Register(
                UatEffectiveStepKeyword.Then,
                phrase,
                "Builtin.Page.AssertTextVisible",
                allowsTable: false,
                handler: AssertAnyControlTextContainsAsync);
        }

        // Everything control-shaped is discovered from [UatWhen]/[UatThen] on Brinell.Core interfaces
        // and any decorated app control types found during page discovery.
        UatCatalogBuilder.RegisterControlVerbs(
            catalog,
            ControlStepTypes(),
            "Builtin.",
            binding => (context, invocation, _) => InvokeControlMethodAsync(context, invocation, binding));

        RegisterPhraseClassPhrases(
            catalog,
            _phraseClasses,
            (phraseClassType, method) => (context, invocation, cancellationToken) =>
                InvokePhraseClassMethodAsync(
                    phraseClassType,
                    method,
                    context,
                    invocation,
                    cancellationToken));
        RegisterRootPhrases(catalog);
        return catalog;
    }

    private IEnumerable<Type> ControlStepTypes()
    {
        // A verb declared on a control's base class (StateContainer under an app's own state
        // container) is found through the class that the page declares.
        var discovered = _pages.Values
            .SelectMany(page => page.Controls)
            .SelectMany(control => SelfAndBaseTypes(control.Property.PropertyType));
        return UatCatalogBuilder.CoreStepTypes.Concat(discovered).Distinct();
    }

    private static IEnumerable<Type> SelfAndBaseTypes(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            yield return current;
        }
    }

    private static void RegisterPhraseClassPhrases(
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
                        problem: phrase.Problem);
                }

                foreach (var unmatchable in method.Unmatchable)
                {
                    catalog.AddUnmatchable(unmatchable);
                }
            }
        }
    }

    private void RegisterRootPhrases(UatCommandCatalog catalog)
    {
        RegisterRootPhrases(
            catalog,
            _rootType,
            method => (context, invocation, cancellationToken) =>
                InvokeRootPhraseAsync(method, context, invocation, cancellationToken));
    }

    private static void RegisterRootPhrases(
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
            foreach (var phrase in phrases)
            {
                if (catalog.Patterns.Any(pattern =>
                        pattern.Keyword == phrase.Keyword &&
                        pattern.Phrase.Equals(phrase.Phrase, StringComparison.Ordinal)))
                {
                    continue;
                }

                catalog.Register(
                    phrase.Keyword,
                    phrase.Phrase,
                    $"{method.DeclaringType!.FullName}.{method.Name}",
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
            var result = Invoke(_root, method, BuildPhraseArguments(method, context, invocation, cancellationToken));
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
            var result = Invoke(instance, method, BuildPhraseArguments(method, context, invocation, cancellationToken));
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

    private object?[] BuildPhraseArguments(
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

    private UatValueContext ValueContext(UatExecutionContext context)
        => new(
            context.Now ?? context.Clock?.Now ?? DateTimeOffset.UtcNow,
            context.Apps,
            _argumentTypes,
            TryGetServiceProvider(context, out var services) ? services : null);

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
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

    private static IReadOnlyList<UatRuntimeControl> DiscoverControls(object page)
    {
        return DiscoverControls(page.GetType());
    }

    private static IReadOnlyList<UatRuntimeControl> DiscoverControls(Type pageType)
    {
        List<UatRuntimeControl> controls = [];
        foreach (var property in pageType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0 ||
                property.GetCustomAttribute<UatIgnoreAttribute>() is not null ||
                !IsRuntimeControlProperty(property))
            {
                continue;
            }

            controls.Add(new UatRuntimeControl(
                ResolveRuntimeName(property),
                property.Name,
                property));
        }

        return controls;
    }

    private static IReadOnlyList<UatPhraseClassBinding> DiscoverPhraseClasses(IReadOnlyList<Type> types, UatArgumentTypes argumentTypes)
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
                phraseClasses.Add(new UatPhraseClassBinding(type, methods));
            }
        }

        return phraseClasses;
    }

    private static bool IsPhraseClassType(Type type)
    {
        return type.GetCustomAttribute<UatPhraseClassAttribute>() is not null ||
               typeof(UatPhraseClassBase).IsAssignableFrom(type);
    }

    private static bool TryGetServiceProvider(UatExecutionContext context, out IServiceProvider serviceProvider)
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

    private static bool IsRuntimePageProperty(PropertyInfo property)
    {
        return property.GetCustomAttribute<UatNameAttribute>() is not null ||
               UatNameInference.HasKnownSuffix(property.Name) && property.Name.EndsWith("Page", StringComparison.Ordinal);
    }

    private static bool IsRuntimeControlProperty(PropertyInfo property)
    {
        if (property.GetCustomAttribute<UatNameAttribute>() is not null)
        {
            return true;
        }

        // A type that declares its own verbs is a control whatever it is called (ContentDialog).
        return UatNameInference.HasKnownSuffix(property.Name) ||
               UatNameInference.HasKnownSuffix(property.PropertyType.Name) ||
               property.PropertyType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                   .Any(method => method.IsDefined(typeof(UatPhraseAttribute), inherit: false));
    }

    private static string ResolveRuntimeName(MemberInfo member)
    {
        return member.GetCustomAttribute<UatNameAttribute>()?.Name ??
               UatNameInference.FromMember(member);
    }

    private static MethodInfo? FindNavigationMethod(Type rootType, string pageName)
    {
        var expected = "NavigateTo" + pageName.Replace(" ", string.Empty, StringComparison.Ordinal);
        return rootType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method =>
                method.Name.Equals(expected, StringComparison.Ordinal) &&
                method.GetParameters().All(parameter => parameter.IsOptional));
    }

    private Task<UatStepResult> OpenPageAsync(
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!invocation.Arguments.TryGetValue("page", out var pageName) ||
            !_pages.TryGetValue(pageName, out var page))
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{pageName}' was not found. Available pages: {FormatPageNames()}."));
        }

        page.Navigate?.Invoke();
        var instance = GetPageInstance(context, page);
        if (instance is null)
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{page.Name}' requires an active DI scope."));
        }

        if (!WaitPageReady(instance))
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{page.Name}' did not become ready."));
        }

        context.CurrentPageName = page.Name;
        context.AppItems[CurrentPageItemKey] = page;
        context.Diagnostics.Add($"page:{page.Name}");
        return Task.FromResult(UatStepResult.Passed(invocation));
    }

    private Task<UatStepResult> AssertPageAsync(
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!invocation.Arguments.TryGetValue("page", out var pageName) ||
            !_pages.TryGetValue(pageName, out var page))
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{pageName}' was not found. Available pages: {FormatPageNames()}."));
        }

        var instance = GetPageInstance(context, page);
        if (instance is null)
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{page.Name}' requires an active DI scope."));
        }

        if (!WaitPageReady(instance))
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{page.Name}' was not ready."));
        }

        context.CurrentPageName = page.Name;
        context.AppItems[CurrentPageItemKey] = page;
        return Task.FromResult(UatStepResult.Passed(invocation));
    }

    private Task<UatStepResult> InvokeControlMethodAsync(
        UatExecutionContext context,
        UatStepInvocation invocation,
        UatStepBinding binding)
    {
        var (methodName, valueArgumentName, valueType) = (binding.MethodName, binding.ValueArgumentName, binding.ValueType);
        object? literalArgument = binding.Literal;
        var found = binding.NamesControl
            ? TryGetCurrentControl(context, invocation, UatCatalogBuilder.ControlParameter, out var controlResult)
            : TryGetDeclaringControl(context, invocation, binding, out controlResult);
        if (!found)
        {
            return Task.FromResult(controlResult.Result);
        }

        object? value;
        try
        {
            value = valueArgumentName is null ? literalArgument
                : valueType is null || valueType == typeof(string) ? invocation.Arguments[valueArgumentName]
                : UatValues.Read(invocation.RawArguments[valueArgumentName], valueType, ValueContext(context));
        }
        catch (UatValueException ex)
        {
            return Task.FromResult(UatStepResult.Failed(invocation, $"'{valueArgumentName}' is a {UatValues.Describe(valueType!)}; {ex.Problem.Message}", ex));
        }
        object?[] arguments = [
            .. binding.ItemArgumentName is { } item ? [invocation.Arguments[item]] : Array.Empty<object?>(),
            .. valueArgumentName is null && literalArgument is null ? Array.Empty<object?>() : [value],
        ];

        try
        {
            Invoke(controlResult.Control!, methodName, arguments);
            context.Diagnostics.Add($"{methodName}:{controlResult.ControlName}");
            return Task.FromResult(UatStepResult.Passed(invocation));
        }
        catch (Exception ex)
        {
            var methodDetail = ex is MissingMethodException
                ? $" Available methods: {FormatMethodNames(controlResult.Control!)}."
                : string.Empty;
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"Failed to run '{methodName}' on control '{controlResult.ControlName}': {ex.Message}.{methodDetail}",
                ex));
        }
    }

    private Task<UatStepResult> AssertAnyControlTextContainsAsync(
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetCurrentPage(context, invocation, out var pageResult))
        {
            return Task.FromResult(pageResult.Result);
        }

        var expected = invocation.Arguments["text"];
        var pageInstance = GetPageInstance(context, pageResult.Page!);
        if (pageInstance is null)
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{pageResult.Page!.Name}' requires an active DI scope."));
        }

        foreach (var controlBinding in pageResult.Page!.Controls)
        {
            var control = controlBinding.Get(pageInstance);
            if (control is null)
            {
                continue;
            }

            var text = Invoke(control, "GetText");
            if (text is string value &&
                value.Contains(expected, StringComparison.Ordinal))
            {
                context.Diagnostics.Add($"see:{expected}");
                return Task.FromResult(UatStepResult.Passed(invocation));
            }
        }

        return Task.FromResult(UatStepResult.Failed(
            invocation,
            $"Text '{expected}' was not found on page '{pageResult.Page.Name}'."));
    }

    private bool TryGetCurrentControl(
        UatExecutionContext context,
        UatStepInvocation invocation,
        string controlArgumentName,
        out UatControlResolution result)
    {
        result = default;
        if (!TryGetCurrentPage(context, invocation, out var pageResult))
        {
            result = new UatControlResolution(null, string.Empty, pageResult.Result);
            return false;
        }

        var controlName = invocation.Arguments[controlArgumentName];
        var page = pageResult.Page!;
        var controlBinding = FindControl(page, controlName);
        if (controlBinding is null)
        {
            result = new UatControlResolution(
                null,
                controlName,
                UatStepResult.Failed(
                    invocation,
                    $"UAT control '{controlName}' was not found on page '{page.Name}'. Available controls: {FormatControlNames(page)}."));
            return false;
        }

        var pageInstance = GetPageInstance(context, page);
        if (pageInstance is null)
        {
            result = new UatControlResolution(
                null,
                controlName,
                UatStepResult.Failed(
                    invocation,
                    $"UAT page '{page.Name}' requires an active DI scope."));
            return false;
        }

        var control = controlBinding.Get(pageInstance);
        if (control is null)
        {
            result = new UatControlResolution(
                null,
                controlName,
                UatStepResult.Failed(
                    invocation,
                    $"UAT control '{controlName}' on page '{page.Name}' returned null."));
            return false;
        }

        result = new UatControlResolution(control, controlName, UatStepResult.Passed(invocation));
        return true;
    }

    // A verb without {control} runs on the one control of the current page whose type declares it:
    // "the todo {item} should be synced" on the list's collection.
    private bool TryGetDeclaringControl(
        UatExecutionContext context,
        UatStepInvocation invocation,
        UatStepBinding binding,
        out UatControlResolution result)
    {
        result = default;
        if (!TryGetCurrentPage(context, invocation, out var pageResult))
        {
            result = new UatControlResolution(null, string.Empty, pageResult.Result);
            return false;
        }

        var page = pageResult.Page!;
        var declaring = page.Controls
            .Where(control => SelfAndBaseTypes(control.Property.PropertyType).Any(type => Declares(type, binding)))
            .ToList();
        if (declaring.Count != 1)
        {
            result = new UatControlResolution(
                null,
                string.Empty,
                UatStepResult.Failed(
                    invocation,
                    declaring.Count == 0
                        ? $"No control on page '{page.Name}' has the step '{binding.Phrase}'. Go to the page whose control has it first."
                        : $"The step '{binding.Phrase}' is on several controls of page '{page.Name}' ({string.Join(", ", declaring.Select(control => control.Name))}), so it cannot tell which one is meant. Put {{control}} in the phrase."));
            return false;
        }

        var pageInstance = GetPageInstance(context, page);
        var control = pageInstance is null ? null : declaring[0].Get(pageInstance);
        if (control is null)
        {
            result = new UatControlResolution(
                null,
                declaring[0].Name,
                UatStepResult.Failed(invocation, $"UAT control '{declaring[0].Name}' on page '{page.Name}' could not be created."));
            return false;
        }

        result = new UatControlResolution(control, declaring[0].Name, UatStepResult.Passed(invocation));
        return true;
    }

    private static bool Declares(Type type, UatStepBinding binding)
        => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Any(method => method.Name == binding.MethodName &&
                           method.GetCustomAttributes<UatPhraseAttribute>(inherit: false)
                               .Any(attribute => attribute.Keyword == binding.Keyword && attribute.Phrase == binding.Phrase));

    // A step may put "the" before a control's name: "the dialog should ask ..." finds Dialog.
    private static UatRuntimeControl? FindControl(UatRuntimePage page, string name)
        => page.Controls.FirstOrDefault(control => control.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
           ?? (name.StartsWith("the ", StringComparison.OrdinalIgnoreCase)
               ? page.Controls.FirstOrDefault(control => control.Name.Equals(name[4..].TrimStart(), StringComparison.OrdinalIgnoreCase))
               : null);

    private bool TryGetCurrentPage(
        UatExecutionContext context,
        UatStepInvocation invocation,
        out UatPageResolution result)
    {
        result = default;
        if (context.AppItems.TryGetValue(CurrentPageItemKey, out var value) &&
            value is UatRuntimePage page)
        {
            result = new UatPageResolution(page, UatStepResult.Passed(invocation));
            return true;
        }

        if (context.CurrentPageName is not null &&
            _pages.TryGetValue(context.CurrentPageName, out var namedPage))
        {
            result = new UatPageResolution(namedPage, UatStepResult.Passed(invocation));
            return true;
        }

        result = new UatPageResolution(
            null,
            UatStepResult.Failed(
                invocation,
                $"No current UAT page is active. Add a 'Given I am on the ... page' step first. Available pages: {FormatPageNames()}."));
        return false;
    }

    private string FormatPageNames()
    {
        return _pages.Count == 0
            ? "(none)"
            : string.Join(", ", _pages.Keys.Order(StringComparer.OrdinalIgnoreCase));
    }

    private static string FormatControlNames(UatRuntimePage page)
    {
        return page.Controls.Count == 0
            ? "(none)"
            : string.Join(", ", page.Controls.Select(control => control.Name).Order(StringComparer.OrdinalIgnoreCase));
    }

    private static string FormatMethodNames(object control)
    {
        var methods = control.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.DeclaringType != typeof(object))
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return methods.Length == 0 ? "(none)" : string.Join(", ", methods);
    }

    private static object? GetPageInstance(UatExecutionContext context, UatRuntimePage page)
    {
        if (page.Instance is not null)
        {
            return page.Instance;
        }

        return TryGetServiceProvider(context, out var serviceProvider)
            ? serviceProvider.GetService(page.PageType)
            : null;
    }

    private static bool WaitPageReady(object page)
    {
        if (TryInvokeBool(page, "WaitReady", 10000, out var ready))
        {
            return ready;
        }

        if (TryInvokeBool(page, "WaitLoaded", true, 10000, out ready))
        {
            return ready;
        }

        if (TryInvokeBool(page, "IsLoaded", 10000, out ready))
        {
            return ready;
        }

        return true;
    }

    private static bool TryInvokeBool(object instance, string methodName, object? argument, out bool result)
    {
        result = false;
        var method = FindInvokableMethod(instance.GetType(), methodName, argument is null ? [] : [argument]);
        if (method is null)
        {
            return false;
        }

        var value = Invoke(instance, method, argument);
        if (value is bool boolean)
        {
            result = boolean;
            return true;
        }

        return false;
    }

    private static bool TryInvokeBool(object instance, string methodName, object? first, object? second, out bool result)
    {
        result = false;
        var method = FindInvokableMethod(instance.GetType(), methodName, [first, second]);
        if (method is null)
        {
            return false;
        }

        var value = Invoke(instance, method, first, second);
        if (value is bool boolean)
        {
            result = boolean;
            return true;
        }

        return false;
    }

    private static object? Invoke(object instance, string methodName, params object?[] providedArguments)
    {
        var method = FindInvokableMethod(instance.GetType(), methodName, providedArguments);
        if (method is null)
        {
            throw new MissingMethodException(instance.GetType().FullName, methodName);
        }

        return Invoke(instance, method, providedArguments);
    }

    private static object? Invoke(object instance, MethodInfo method, params object?[] providedArguments)
    {
        var parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = i < providedArguments.Length
                ? providedArguments[i]
                : Type.Missing;
        }

        try
        {
            var result = method.Invoke(instance, arguments);
            return result;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static MethodInfo? FindInvokableMethod(Type type, string methodName, IReadOnlyList<object?> providedArguments)
    {
        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name.Equals(methodName, StringComparison.Ordinal))
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                if (parameters.Count(parameter => !parameter.IsOptional) > providedArguments.Count ||
                    parameters.Length < providedArguments.Count)
                {
                    return false;
                }

                for (var i = 0; i < providedArguments.Count; i++)
                {
                    if (!CanPassArgument(providedArguments[i], parameters[i].ParameterType))
                    {
                        return false;
                    }
                }

                return true;
            });
    }

    private static bool CanPassArgument(object? argument, Type parameterType)
    {
        var nullableInnerType = Nullable.GetUnderlyingType(parameterType);
        if (argument is null)
        {
            return nullableInnerType is not null || !parameterType.IsValueType;
        }

        var effectiveParameterType = nullableInnerType ?? parameterType;
        return effectiveParameterType.IsInstanceOfType(argument);
    }

    private readonly record struct UatPageResolution(UatRuntimePage? Page, UatStepResult Result);

    private readonly record struct UatControlResolution(object? Control, string ControlName, UatStepResult Result);
}

public sealed record UatRuntimePage(
    string Name,
    Type PageType,
    object? Instance,
    IReadOnlyList<UatRuntimeControl> Controls,
    Action? Navigate);

public sealed record UatRuntimeControl(
    string Name,
    string MemberName,
    PropertyInfo Property)
{
    public object? Get(object page)
    {
        return Property.GetValue(page);
    }
}

public sealed record UatPhraseClassBinding(
    Type Type,
    IReadOnlyList<UatPhraseClassMethodBinding> Methods);

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
