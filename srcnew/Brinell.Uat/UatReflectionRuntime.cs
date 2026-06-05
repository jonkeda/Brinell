using System.Reflection;
using Brinell.Core.Settings;

namespace Brinell.Uat;

public sealed class UatReflectionRuntime
{
    private const string CurrentPageItemKey = "Uat.Reflection.CurrentPage";
    private readonly Dictionary<string, UatRuntimePage> _pages;
    private readonly object _root;

    private UatReflectionRuntime(object root, IEnumerable<UatRuntimePage> pages)
    {
        _root = root;
        _pages = new Dictionary<string, UatRuntimePage>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            _pages[page.Name] = page;
        }
    }

    public IReadOnlyCollection<UatRuntimePage> Pages => _pages.Values;

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
                page,
                DiscoverControls(page),
                navigation is null ? null : () => Invoke(root, navigation)));
        }

        return new UatReflectionRuntime(root, pages);
    }

    public static void RegisterRootPhrases(UatCommandCatalog catalog, Type rootType)
    {
        RegisterRootPhrases(catalog, rootType, handlerFactory: null);
    }

    public UatCommandCatalog CreateCommandCatalog()
    {
        var catalog = new UatCommandCatalog();
        catalog.Register(
            UatEffectiveStepKeyword.Given,
            "I am on the {page} page",
            "Builtin.Page.Open",
            handler: OpenPageAsync);
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "I should be on the {page} page",
            "Builtin.Page.AssertOpen",
            handler: AssertPageAsync);
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I tap {control}",
            "Builtin.Control.Tap",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "Click"));
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I enter {value} into {control}",
            "Builtin.Control.Enter",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "Enter", "value"));
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I set {control} to {value}",
            "Builtin.Control.SetText",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "SetText", "value"));
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I clear {control}",
            "Builtin.Control.Clear",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "Clear"));
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I check {control}",
            "Builtin.Control.Check",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "Check"));
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I uncheck {control}",
            "Builtin.Control.Uncheck",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "Uncheck"));
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I select {value} from {control}",
            "Builtin.Control.SelectByText",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "SelectByText", "value"));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should contain {value}",
            "Builtin.Control.AssertTextContains",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertTextContains", "value"));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should equal {value}",
            "Builtin.Control.AssertText",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertText", "value"));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should be visible",
            "Builtin.Control.AssertVisible",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertVisible", literalArgument: true));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should not be visible",
            "Builtin.Control.AssertVisible.False",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertVisible", literalArgument: false));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should be enabled",
            "Builtin.Control.AssertEnabled",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertEnabled", literalArgument: true));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should be checked",
            "Builtin.Control.AssertChecked.True",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertChecked", literalArgument: true));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should be unchecked",
            "Builtin.Control.AssertChecked.False",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertChecked", literalArgument: false));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "{control} should have selected {value}",
            "Builtin.Control.AssertSelectedText",
            allowsTable: false,
            handler: (context, invocation, _) => InvokeControlMethodAsync(context, invocation, "control", "AssertSelectedText", "value"));
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "I should see {text}",
            "Builtin.Page.AssertTextVisible",
            allowsTable: false,
            handler: AssertAnyControlTextContainsAsync);

        RegisterRootPhrases(catalog);
        return catalog;
    }

    private void RegisterRootPhrases(UatCommandCatalog catalog)
    {
        RegisterRootPhrases(
            catalog,
            _root.GetType(),
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
            foreach (var phrase in method.GetCustomAttributes<UatPhraseAttribute>())
            {
                foreach (var keyword in ExpandKeywords(phrase.Keyword))
                {
                    catalog.Register(
                        keyword,
                        phrase.Phrase,
                        $"{method.DeclaringType!.FullName}.{method.Name}",
                        handler: handlerFactory?.Invoke(method));
                }
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
            var result = Invoke(_root, method, BuildRootPhraseArguments(method, context, invocation, cancellationToken));
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

    private static object?[] BuildRootPhraseArguments(
        MethodInfo method,
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
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

            if (IsSettingsParameterType(parameter.ParameterType, method.DeclaringType?.Assembly))
            {
                arguments[i] = context.GetSettings(parameter.ParameterType);
                continue;
            }

            if (parameter.Name is not null &&
                invocation.Arguments.TryGetValue(parameter.Name, out var value))
            {
                arguments[i] = ConvertArgument(value, parameter.ParameterType);
                continue;
            }

            arguments[i] = parameter.IsOptional
                ? Type.Missing
                : throw new InvalidOperationException(
                    $"Custom UAT step '{method.Name}' requires parameter '{parameter.Name}' but no matching phrase argument was found.");
        }

        return arguments;
    }

    private static bool IsSettingsParameterType(Type parameterType, Assembly? phraseAssembly)
    {
        if (parameterType == typeof(TestSettings) ||
            parameterType.GetCustomAttribute<TestSettingsRootAttribute>() is not null ||
            parameterType.GetCustomAttribute<TestSettingsSectionAttribute>() is not null)
        {
            return true;
        }

        return phraseAssembly is not null &&
               parameterType.Assembly == phraseAssembly &&
               parameterType.IsClass &&
               parameterType.Name.EndsWith("Settings", StringComparison.Ordinal);
    }

    private static object? ConvertArgument(string value, Type targetType)
    {
        var nullableInnerType = Nullable.GetUnderlyingType(targetType);
        var effectiveType = nullableInnerType ?? targetType;
        if (effectiveType == typeof(string))
        {
            return value;
        }

        if (effectiveType == typeof(int))
        {
            return int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effectiveType == typeof(double))
        {
            return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effectiveType == typeof(bool))
        {
            return bool.Parse(value);
        }

        if (effectiveType.IsEnum)
        {
            return Enum.Parse(effectiveType, value, ignoreCase: true);
        }

        return Convert.ChangeType(value, effectiveType, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IEnumerable<UatEffectiveStepKeyword> ExpandKeywords(UatEffectiveStepKeyword? keyword)
    {
        return keyword.HasValue
            ? [keyword.Value]
            : Enum.GetValues<UatEffectiveStepKeyword>();
    }

    private static IReadOnlyList<UatRuntimeControl> DiscoverControls(object page)
    {
        List<UatRuntimeControl> controls = [];
        foreach (var property in page.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
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

        return UatNameInference.HasKnownSuffix(property.Name) ||
               UatNameInference.HasKnownSuffix(property.PropertyType.Name);
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
        if (!WaitPageReady(page.Instance))
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{page.Name}' did not become ready."));
        }

        context.CurrentPageName = page.Name;
        context.Items[CurrentPageItemKey] = page;
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

        if (!WaitPageReady(page.Instance))
        {
            return Task.FromResult(UatStepResult.Failed(
                invocation,
                $"UAT page '{page.Name}' was not ready."));
        }

        context.CurrentPageName = page.Name;
        context.Items[CurrentPageItemKey] = page;
        return Task.FromResult(UatStepResult.Passed(invocation));
    }

    private Task<UatStepResult> InvokeControlMethodAsync(
        UatExecutionContext context,
        UatStepInvocation invocation,
        string controlArgumentName,
        string methodName,
        string? valueArgumentName = null,
        object? literalArgument = null)
    {
        if (!TryGetCurrentControl(context, invocation, controlArgumentName, out var controlResult))
        {
            return Task.FromResult(controlResult.Result);
        }

        var value = valueArgumentName is null
            ? literalArgument
            : invocation.Arguments[valueArgumentName];
        var arguments = valueArgumentName is null && literalArgument is null
            ? []
            : new[] { value };

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
        foreach (var controlBinding in pageResult.Page!.Controls)
        {
            var control = controlBinding.Get(pageResult.Page.Instance);
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
        var controlBinding = pageResult.Page!.Controls.FirstOrDefault(
            control => control.Name.Equals(controlName, StringComparison.OrdinalIgnoreCase));
        if (controlBinding is null)
        {
            result = new UatControlResolution(
                null,
                controlName,
                UatStepResult.Failed(
                    invocation,
                    $"UAT control '{controlName}' was not found on page '{pageResult.Page.Name}'. Available controls: {FormatControlNames(pageResult.Page)}."));
            return false;
        }

        var control = controlBinding.Get(pageResult.Page.Instance);
        if (control is null)
        {
            result = new UatControlResolution(
                null,
                controlName,
                UatStepResult.Failed(
                    invocation,
                    $"UAT control '{controlName}' on page '{pageResult.Page.Name}' returned null."));
            return false;
        }

        result = new UatControlResolution(control, controlName, UatStepResult.Passed(invocation));
        return true;
    }

    private bool TryGetCurrentPage(
        UatExecutionContext context,
        UatStepInvocation invocation,
        out UatPageResolution result)
    {
        result = default;
        if (context.Items.TryGetValue(CurrentPageItemKey, out var value) &&
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
    object Instance,
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
