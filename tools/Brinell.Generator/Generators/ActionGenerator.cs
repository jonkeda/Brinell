using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Models;
using Brinell.Generator.Writers;

namespace Brinell.Generator.Generators;

/// <summary>
/// Emits the public action wrapper for a protected virtual <c>*Core</c> method
/// (e.g., <c>ClickCore</c> → <c>Click</c>). The wrapper delegates to the Core
/// method via <c>RunDoWithElement</c> and the clickable guard.
/// </summary>
public class ActionGenerator : IMemberGenerator
{
    private readonly MemberGeneratorOptions _options;

    public ActionGenerator(MemberGeneratorOptions? options = null)
    {
        _options = options ?? new MemberGeneratorOptions();
    }

    /// <summary>
    /// Matches methods ending with "Core", declared as protected and virtual,
    /// excluding <c>Is*Core</c> state queries (handled by the Is/Wait/Assert family).
    /// </summary>
    public bool Matches(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;

        // Check if name ends with suffix
        if (!methodName.EndsWith(_options.MethodSuffix))
            return false;

        // Exclude Is*Core state queries — those belong to IsWaitAssertGenerator.
        if (methodName.StartsWith("Is") && method.ReturnType.ToString() == "bool?")
            return false;

        // Exclude Get*Core value queries — those belong to IsWaitAssertGenerator.
        if (methodName.StartsWith("Get"))
            return false;

        var modifiers = method.Modifiers;

        // Check for required modifiers
        if (_options.RequireProtected && !modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return false;

        if (_options.RequireVirtual && !modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            return false;

        // Ensure it has at least the element parameter
        if (method.ParameterList.Parameters.Count == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Extracts method metadata, stripping the Core suffix and preparing parameters.
    /// </summary>
    public MethodInfo Extract(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;
        var suffixLength = _options.MethodSuffix.Length;
        var publicName = methodName.Substring(0, methodName.Length - suffixLength);

        var info = new MethodInfo
        {
            MethodName = methodName,
            PublicMethodName = _options.PublicMethodPrefix + publicName,
            ReturnType = method.ReturnType.ToString(),
            XmlDocumentation = ExtractXmlDocumentation(method)
        };

        // Extract parameters, skipping the first one (element)
        var parameters = method.ParameterList.Parameters;
        for (int i = 1; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var typeName = param.Type?.ToString() ?? "object";
            var paramName = param.Identifier.Text;
            var defaultValue = param.Default?.Value?.ToString();

            info.Parameters.Add((typeName, paramName, defaultValue));
        }

        return info;
    }

    /// <summary>
    /// Generates the public action wrapper using <c>RunDoWithElement</c> and the
    /// clickable guard.
    /// </summary>
    public string Generate(MethodInfo coreMethod, ControlObjectContext context)
    {
        var writer = new CsWriter(0); // Start with no indentation - let the file-level writer handle it
        var publicMethodName = coreMethod.PublicMethodName;

        // Determine return type
        var returnTypeStr = context.TypeParameters.TrimStart('<').TrimEnd('>');
        if (string.IsNullOrEmpty(returnTypeStr))
            returnTypeStr = "void";

        // Write region start
        writer.WriteLine($"#region {publicMethodName}");
        writer.WriteLine();

        // Build parameter list
        var paramList = BuildParameterList(coreMethod);

        // Write method signature
        writer.WriteLine($"public {returnTypeStr} {publicMethodName}({paramList})");
        writer.Open();

        // Build lambda arguments (additional parameters after element)
        var lambdaArgs = BuildLambdaArguments(coreMethod);
        var hasTimeoutMs = coreMethod.Parameters.Any(p => p.ParameterName == "timeoutMs");

        // Build method body with RunDoWithElement call
        writer.WriteStart("return RunDoWithElement(element => { ");
        writer.Write($"{coreMethod.MethodName}(element{lambdaArgs}); ");
        writer.Write("}");

        if (hasTimeoutMs)
        {
            writer.Write(", timeoutMs");
        }

        writer.WriteEnd(");");
        writer.Close();
        writer.WriteLine();

        // Write region end
        writer.WriteLine("#endregion");

        return writer.ToString();
    }

    private string BuildParameterList(MethodInfo coreMethod)
    {
        if (coreMethod.Parameters.Count == 0)
            return "";

        var paramParts = new List<string>();
        foreach (var (typeName, paramName, defaultValue) in coreMethod.Parameters)
        {
            if (string.IsNullOrEmpty(defaultValue))
                paramParts.Add($"{typeName} {paramName}");
            else
                paramParts.Add($"{typeName} {paramName} = {defaultValue}");
        }

        return string.Join(", ", paramParts);
    }

    private string BuildLambdaArguments(MethodInfo coreMethod)
    {
        if (coreMethod.Parameters.Count == 0)
            return "";

        var paramNames = coreMethod.Parameters.Select(p => p.ParameterName);
        return ", " + string.Join(", ", paramNames);
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
