using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Models;
using Brinell.Generator.Writers;

namespace Brinell.Generator.Handlers;

/// <summary>
/// Handler for Is/Wait/Assert property patterns (e.g., IsExistsCore).
/// Generates three public methods: Is*, Wait*, and Assert*.
/// </summary>
public class IsPropertyHandler : IMethodHandler
{
    private readonly HandlerOptions _options;
    private readonly string _methodPrefix = "Is";  // Fixed prefix for Is*Core pattern

    public IsPropertyHandler(HandlerOptions? options = null)
    {
        _options = options ?? new HandlerOptions { MethodSuffix = "Core" };
    }

    /// <summary>
    /// Matches methods like: protected virtual bool? IsExistsCore(IMauiElement? element)
    /// Must start with "Is", end with "Core", return bool?, and have IMauiElement parameter.
    /// </summary>
    public bool Matches(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;

        // Check name pattern: Is*Core
        if (!methodName.StartsWith(_methodPrefix) || !methodName.EndsWith(_options.MethodSuffix))
            return false;

        // Check return type is bool? (nullable bool)
        var returnType = method.ReturnType.ToString();
        if (returnType != "bool?")
            return false;

        var modifiers = method.Modifiers;
        if (_options.RequireProtected && !modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return false;
        if (_options.RequireVirtual && !modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            return false;

        // Ensure first parameter is IMauiElement (nullable or not)
        if (method.ParameterList.Parameters.Count == 0)
            return false;

        var firstParam = method.ParameterList.Parameters[0];
        var paramType = firstParam.Type?.ToString() ?? "";
        if (!paramType.Contains("IMauiElement"))
            return false;

        return true;
    }

    /// <summary>
    /// Extracts method metadata from IsExistsCore pattern.
    /// Property name is derived by stripping "Is" prefix and "Core" suffix (e.g., IsExistsCore → Exists).
    /// </summary>
    public MethodInfo Extract(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;

        // Extract property name: "IsExistsCore" → "Exists"
        var prefixLength = _methodPrefix.Length;
        var suffixLength = _options.MethodSuffix.Length;
        var propertyName = methodName.Substring(prefixLength, methodName.Length - prefixLength - suffixLength);

        return new MethodInfo
        {
            MethodName = methodName,
            PublicMethodName = propertyName,  // Used as base for Is*, Wait*, Assert*
            ReturnType = "bool?",
            XmlDocumentation = ExtractXmlDocumentation(method)
        };
    }

    /// <summary>
    /// Generates Is*, Wait*, and Assert* methods using CsWriter.
    /// Each property generates three public methods from a single Is*Core protected method.
    /// </summary>
    public string GenerateWrapper(MethodInfo coreMethod, string containingTypeName, string typeParameters)
    {
        var propertyName = coreMethod.PublicMethodName;
        var writer = new CsWriter(0);

        // Generate IsExists() method
        GenerateIsMethod(writer, coreMethod, propertyName);
        writer.WriteLine();

        // Generate WaitExists(bool? expected, int? timeoutMs = null) method
        GenerateWaitMethod(writer, coreMethod, propertyName);
        writer.WriteLine();

        // Generate AssertExists(bool? expected, string? message = null, int? timeoutMs = null) method
        GenerateAssertMethod(writer, coreMethod, propertyName);

        return writer.ToString();
    }

    /// <summary>
    /// Generates public bool Is{PropertyName}() method.
    /// </summary>
    private void GenerateIsMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        writer.WriteLine($"public bool Is{propertyName}()");
        writer.Open();
        writer.WriteLine($"return {coreMethod.MethodName}(TryFindElement()) == true;");
        writer.Close();
    }

    /// <summary>
    /// Generates public bool Wait{PropertyName}(bool? expected, int? timeoutMs = null) method.
    /// Includes optimization to return true immediately if expected is null.
    /// </summary>
    private void GenerateWaitMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        writer.WriteLine($"public bool Wait{propertyName}(bool? expected, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("if (expected == null) return true;");
        writer.WriteLine("return RunWaitWithElement(");
        writer.IncreaseSpace(1);
        writer.WriteLine($"element => {coreMethod.MethodName}(element) == expected.Value,");
        writer.WriteLine("timeoutMs);");
        writer.DecreaseSpace(1);
        writer.Close();
    }

    /// <summary>
    /// Generates public TScope Assert{PropertyName}(bool? expected, string? message = null, int? timeoutMs = null) method.
    /// </summary>
    private void GenerateAssertMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        writer.WriteLine($"public TScope Assert{propertyName}(bool? expected, string? message = null, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("return RunAssertWithElement(expected,");
        writer.IncreaseSpace(1);
        writer.WriteLine($"{coreMethod.MethodName}, (actual, expected1) => (actual == expected1),");
        writer.WriteLine("null, timeoutMs);");
        writer.DecreaseSpace(1);
        writer.Close();
    }

    private string? ExtractXmlDocumentation(MethodDeclarationSyntax method)
    {
        var trivia = method.GetLeadingTrivia();
        var xmlTrivia = trivia
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                 t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

        return xmlTrivia.IsKind(SyntaxKind.None) ? null : xmlTrivia.ToString();
    }
}
