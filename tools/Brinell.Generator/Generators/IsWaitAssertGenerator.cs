using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Models;
using Brinell.Generator.Writers;

namespace Brinell.Generator.Generators;

/// <summary>
/// Emits a query/Wait/Assert trio for a protected virtual <c>Is*Core</c> or
/// <c>Get*Core</c> query. <c>Is*Core</c> (returning <c>bool?</c>) yields
/// <c>Is*</c>/<c>Wait*</c>/<c>Assert*</c> (e.g., <c>IsVisibleCore</c> →
/// <c>IsVisible</c>, <c>WaitVisible</c>, <c>AssertVisible</c>). <c>Get*Core</c>
/// (returning any value) yields <c>Get*</c>/<c>Wait*</c>/<c>Assert*</c> (e.g.,
/// <c>GetTextCore</c> → <c>GetText</c>, <c>WaitText</c>, <c>AssertText</c>).
/// </summary>
public class IsWaitAssertGenerator : IMemberGenerator
{
    private readonly MemberGeneratorOptions _options;
    private readonly string _methodPrefix = "Is";   // Prefix for Is*Core state queries
    private readonly string _getterPrefix = "Get";  // Prefix for Get*Core value queries

    public IsWaitAssertGenerator(MemberGeneratorOptions? options = null)
    {
        _options = options ?? new MemberGeneratorOptions { MethodSuffix = "Core" };
    }

    /// <summary>
    /// Matches protected virtual query methods ending with "Core":
    /// <c>Is*Core</c> returning <c>bool?</c>, or <c>Get*Core</c> returning any value.
    /// The first parameter must be the platform element.
    /// </summary>
    public bool Matches(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;

        // Must end with the Core suffix.
        if (!methodName.EndsWith(_options.MethodSuffix))
            return false;

        var isState = methodName.StartsWith(_methodPrefix);   // Is*Core
        var isGetter = methodName.StartsWith(_getterPrefix);  // Get*Core
        if (!isState && !isGetter)
            return false;

        var returnType = method.ReturnType.ToString();

        // Is*Core must return bool or bool?; Get*Core must return a value (non-void).
        if (isState && returnType != "bool?" && returnType != "bool")
            return false;
        if (isGetter && returnType == "void")
            return false;

        var modifiers = method.Modifiers;
        if (_options.RequireProtected && !modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return false;
        if (_options.RequireVirtual && !modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            return false;

        // Ensure first parameter is the platform element (nullable or not)
        if (method.ParameterList.Parameters.Count == 0)
            return false;

        var firstParam = method.ParameterList.Parameters[0];
        var paramType = firstParam.Type?.ToString() ?? "";
        if (!paramType.Contains("Element"))
            return false;

        return true;
    }

    /// <summary>
    /// Extracts method metadata. Property name is derived by stripping the prefix
    /// ("Is" or "Get") and "Core" suffix (e.g., IsExistsCore → Exists,
    /// GetTextCore → Text). For getters, the extra parameters after the element
    /// are captured for the generated signatures.
    /// </summary>
    public MethodInfo Extract(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;
        var isGetter = methodName.StartsWith(_getterPrefix);
        var prefix = isGetter ? _getterPrefix : _methodPrefix;

        // Extract property name: "IsExistsCore" → "Exists", "GetTextCore" → "Text"
        var prefixLength = prefix.Length;
        var suffixLength = _options.MethodSuffix.Length;
        var propertyName = methodName.Substring(prefixLength, methodName.Length - prefixLength - suffixLength);

        var info = new MethodInfo
        {
            MethodName = methodName,
            PublicMethodName = propertyName,  // Used as base for Is*/Get*, Wait*, Assert*
            ReturnType = method.ReturnType.ToString(),
            XmlDocumentation = ExtractXmlDocumentation(method)
        };

        // Capture extra parameters (after the element) for getter signatures.
        if (isGetter)
        {
            var parameters = method.ParameterList.Parameters;
            for (int i = 1; i < parameters.Count; i++)
            {
                var param = parameters[i];
                var typeName = param.Type?.ToString() ?? "object";
                var paramName = param.Identifier.Text;
                info.Parameters.Add((typeName, paramName, null));
            }
        }

        return info;
    }

    /// <summary>
    /// Generates the query/Wait/Assert trio from a single Is*Core or Get*Core method.
    /// </summary>
    public string Generate(MethodInfo coreMethod, ControlObjectContext context)
    {
        return coreMethod.MethodName.StartsWith(_getterPrefix)
            ? GenerateGetter(coreMethod)
            : GenerateState(coreMethod);
    }

    private string GenerateState(MethodInfo coreMethod)
    {
        var propertyName = coreMethod.PublicMethodName;
        var writer = new CsWriter(0);

        // Write region start
        writer.WriteLine($"#region {propertyName} (Is{propertyName} / Wait{propertyName} / Assert{propertyName})");
        writer.WriteLine();

        // Generate Is*() method
        GenerateIsMethod(writer, coreMethod, propertyName);
        writer.WriteLine();

        // Generate Wait*(bool? expected, int? timeoutMs = null) method
        GenerateWaitMethod(writer, coreMethod, propertyName);
        writer.WriteLine();

        // Generate Assert*(bool? expected, string? message = null, int? timeoutMs = null) method
        GenerateAssertMethod(writer, coreMethod, propertyName);
        writer.WriteLine();

        // Write region end
        writer.WriteLine("#endregion");

        return writer.ToString();
    }

    /// <summary>
    /// Generates the Get*/Wait*/Assert* trio from a single Get*Core method.
    /// </summary>
    private string GenerateGetter(MethodInfo coreMethod)
    {
        var propertyName = coreMethod.PublicMethodName;
        var returnType = coreMethod.ReturnType;
        var nullableReturnType = returnType.EndsWith("?") ? returnType : returnType + "?";
        var paramPrefix = BuildParameterListPrefix(coreMethod);
        var lambdaArgs = BuildLambdaArguments(coreMethod);
        var writer = new CsWriter(0);

        // Write region start
        writer.WriteLine($"#region {propertyName} (Get{propertyName} / Wait{propertyName} / Assert{propertyName})");
        writer.WriteLine();

        // Get{PropertyName}(...) getter
        writer.WriteLine($"public {nullableReturnType} Get{propertyName}({paramPrefix}int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine($"return RunGetWithElement(element => {coreMethod.MethodName}(element{lambdaArgs}), timeoutMs);");
        writer.Close();
        writer.WriteLine();

        // Wait{PropertyName}(...) waiter
        writer.WriteLine($"public bool? Wait{propertyName}({paramPrefix}{nullableReturnType} expected, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("return RunWaitWithElement(expected,");
        writer.IncreaseSpace(1);
        writer.WriteLine($"element => {coreMethod.MethodName}(element{lambdaArgs}) == expected,");
        writer.WriteLine("timeoutMs);");
        writer.DecreaseSpace(1);
        writer.Close();
        writer.WriteLine();

        // Assert{PropertyName}(...) assertion
        writer.WriteLine($"public TScope Assert{propertyName}({paramPrefix}{nullableReturnType} expected, string? message = null, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("return RunAssertWithElement(expected,");
        writer.IncreaseSpace(1);
        writer.WriteLine($"element => {coreMethod.MethodName}(element{lambdaArgs}), (actual, expected1) => (actual == expected1),");
        writer.WriteLine("null, timeoutMs);");
        writer.DecreaseSpace(1);
        writer.Close();
        writer.WriteLine();

        // Write region end
        writer.WriteLine("#endregion");

        return writer.ToString();
    }

    private static string BuildParameterListPrefix(MethodInfo coreMethod)
    {
        if (coreMethod.Parameters.Count == 0)
            return "";

        var parts = coreMethod.Parameters.Select(p => $"{p.TypeName} {p.ParameterName}");
        return string.Join(", ", parts) + ", ";
    }

    private static string BuildLambdaArguments(MethodInfo coreMethod)
    {
        if (coreMethod.Parameters.Count == 0)
            return "";

        return ", " + string.Join(", ", coreMethod.Parameters.Select(p => p.ParameterName));
    }

    #region IsExists / WaitExists / AssertExists

    /// <summary>
    /// Generates public bool Is{PropertyName}() method.
    /// </summary>
    private void GenerateIsMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        writer.WriteLine($"public {coreMethod.ReturnType} Is{propertyName}()");
        writer.Open();
        writer.WriteLine($"return {coreMethod.MethodName}(TryFindElement()) == true;");
        writer.Close();
    }

    /// <summary>
    /// Generates public Wait{PropertyName}(bool? expected, int? timeoutMs = null) method.
    /// The return type mirrors the Is*Core method (bool or bool?).
    /// </summary>
    private void GenerateWaitMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        writer.WriteLine($"public bool Wait{propertyName}(bool? expected = true, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("return RunWaitWithElement(expected,");
        writer.IncreaseSpace(1);
        writer.WriteLine($"element => {coreMethod.MethodName}(element) == expected!.Value,");
        writer.WriteLine("timeoutMs);");
        writer.DecreaseSpace(1);
        writer.Close();
    }

    /// <summary>
    /// Generates public TScope Assert{PropertyName}(bool? expected, string? message = null, int? timeoutMs = null) method.
    /// </summary>
    private void GenerateAssertMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        writer.WriteLine($"public TScope Assert{propertyName}(bool? expected = true, string? message = null, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("return RunAssertWithElement(expected,");
        writer.IncreaseSpace(1);
        writer.WriteLine($"{coreMethod.MethodName}, (actual, expected1) => (actual == expected1),");
        writer.WriteLine("null, timeoutMs);");
        writer.DecreaseSpace(1);
        writer.Close();
    }

    #endregion

    private string? ExtractXmlDocumentation(MethodDeclarationSyntax method)
    {
        var trivia = method.GetLeadingTrivia();
        var xmlTrivia = trivia
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                 t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

        return xmlTrivia.IsKind(SyntaxKind.None) ? null : xmlTrivia.ToString();
    }
}
