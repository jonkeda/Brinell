using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Models;
using Brinell.Generator.Writers;

namespace Brinell.Generator.Handlers;

/// <summary>
/// Handler for wrapping protected virtual *Core methods with public wrapper methods.
/// Generates public wrapper methods that delegate to the Core methods via RunDoWithElement.
/// </summary>
public class CoreMethodHandler : IMethodHandler
{
    private readonly HandlerOptions _options;

    public CoreMethodHandler(HandlerOptions? options = null)
    {
        _options = options ?? new HandlerOptions();
    }

    /// <summary>
    /// Matches methods ending with "Core", declared as protected and virtual.
    /// </summary>
    public bool Matches(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;
        
        // Check if name ends with suffix
        if (!methodName.EndsWith(_options.MethodSuffix))
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
    /// Generates the public wrapper method code using CsWriter (without pre-computed indentation).
    /// The method calls RunDoWithElement with a lambda that ensures clickability and invokes the Core method.
    /// </summary>
    public string GenerateWrapper(MethodInfo coreMethod, string containingTypeName, string typeParameters)
    {
        var writer = new CsWriter(0); // Start with no indentation - let the file-level writer handle it
        var publicMethodName = coreMethod.PublicMethodName;

        // Determine return type
        var returnTypeStr = typeParameters.TrimStart('<').TrimEnd('>');
        if (string.IsNullOrEmpty(returnTypeStr))
            returnTypeStr = "void";

        // Build parameter list
        var paramList = BuildParameterList(coreMethod);

        // Write method signature
        writer.WriteLine($"public {returnTypeStr} {publicMethodName}({paramList})");
        writer.Open();

        // Build lambda arguments (additional parameters after element)
        var lambdaArgs = BuildLambdaArguments(coreMethod);
        var hasTimeoutMs = coreMethod.Parameters.Any(p => p.ParameterName == "timeoutMs");

        // Build method body with RunDoWithElement call
        writer.WriteStart("return RunDoWithElement(element => { EnsureClickableCore(element); ");
        writer.Write($"{coreMethod.MethodName}(element{lambdaArgs}); ");
        writer.Write("}");

        if (hasTimeoutMs)
        {
            writer.Write(", timeoutMs");
        }

        writer.WriteEnd(");");
        writer.Close();

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
