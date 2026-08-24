using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Models;

namespace Brinell.Generator.Analysis;

/// <summary>
/// Analyzes a C# file using Roslyn to locate the target ControlObject class and
/// build the <see cref="ControlObjectContext"/> used for generation.
/// </summary>
public class ControlObjectAnalyzer
{
    /// <summary>
    /// Parses source and finds the target class.
    /// </summary>
    /// <param name="sourceCode">The C# source code to analyze.</param>
    /// <param name="targetClassName">Optional target class name; if not specified, uses the first class.</param>
    /// <returns>Tuple of (ClassDeclarationSyntax?, CompilationUnitSyntax).</returns>
    public (ClassDeclarationSyntax? ClassDecl, CompilationUnitSyntax Root)
        FindTarget(string sourceCode, string? targetClassName = null)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = FindClass(root, targetClassName);
        return (classDecl, root);
    }

    /// <summary>
    /// Returns the Core methods (protected members ending in "Core") of a class.
    /// </summary>
    public IEnumerable<MethodDeclarationSyntax> CoreMethods(ClassDeclarationSyntax classDecl)
    {
        return classDecl.Members.OfType<MethodDeclarationSyntax>();
    }

    /// <summary>
    /// Builds the <see cref="ControlObjectContext"/> from the target class and root.
    /// </summary>
    public ControlObjectContext BuildContext(ClassDeclarationSyntax classDecl, CompilationUnitSyntax root)
    {
        return new ControlObjectContext
        {
            ContainingTypeName = classDecl.Identifier.Text,
            TypeParameters = GetTypeParameters(classDecl),
            FluentReturnType = ResolveFluentReturnType(classDecl),
            ElementType = DetectElementType(classDecl),
            Namespace = GetNamespace(root),
            Usings = GetUsingStatements(root),
            ClassDeclaration = classDecl
        };
    }

    /// <summary>
    /// Finds a class in the compilation unit.
    /// </summary>
    private ClassDeclarationSyntax? FindClass(CompilationUnitSyntax root, string? className = null)
    {
        var classDecls = new List<ClassDeclarationSyntax>();

        // Search at root level
        classDecls.AddRange(root.Members.OfType<ClassDeclarationSyntax>());

        // Search inside FileScopedNamespaceDeclaration
        var fileScoped = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScoped != null)
        {
            classDecls.AddRange(fileScoped.Members.OfType<ClassDeclarationSyntax>());
        }

        // Search inside regular NamespaceDeclaration
        var regularNs = root.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if (regularNs != null)
        {
            classDecls.AddRange(regularNs.Members.OfType<ClassDeclarationSyntax>());
        }

        if (!classDecls.Any())
            return null;

        if (string.IsNullOrEmpty(className))
            return classDecls.First();

        return classDecls.FirstOrDefault(c => c.Identifier.Text == className);
    }

    /// <summary>
    /// Detects the platform element type from the first Core method's first parameter.
    /// Defaults to "IMauiElement" when none is found.
    /// </summary>
    private string DetectElementType(ClassDeclarationSyntax classDecl)
    {
        foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            if (!method.Identifier.Text.EndsWith("Core"))
                continue;
            if (method.ParameterList.Parameters.Count == 0)
                continue;

            var paramType = method.ParameterList.Parameters[0].Type?.ToString();
            if (string.IsNullOrEmpty(paramType))
                continue;

            return paramType.TrimEnd('?');
        }

        return "IMauiElement";
    }

    /// <summary>
    /// Gets the generic type parameters from a class declaration (e.g., "&lt;TScope&gt;").
    /// </summary>
    public string GetTypeParameters(ClassDeclarationSyntax classDecl)
    {
        if (classDecl.TypeParameterList == null || classDecl.TypeParameterList.Parameters.Count == 0)
            return "";

        var parameters = string.Join(", ", classDecl.TypeParameterList.Parameters.Select(p => p.Identifier.Text));
        return $"<{parameters}>";
    }

    /// <summary>
    /// Resolves the type parameter that public members return for fluent chaining.
    /// </summary>
    /// <remarks>
    /// Rules, in order:
    /// <list type="number">
    /// <item><c>[FluentReturn("T")]</c> on the class, when present.</item>
    /// <item><c>TSelf</c>, when the class declares it — containers and collections
    /// return themselves so a chain stays inside the scope.</item>
    /// <item>The single type parameter — the control case, which returns its
    /// containing scope.</item>
    /// <item>Empty, when the class has no type parameters; actions then return void.</item>
    /// </list>
    /// </remarks>
    /// <param name="classDecl">The class to resolve against.</param>
    /// <returns>The type parameter name, or an empty string when there is none.</returns>
    public string ResolveFluentReturnType(ClassDeclarationSyntax classDecl)
    {
        var declared = classDecl.TypeParameterList?.Parameters
            .Select(p => p.Identifier.Text)
            .ToList() ?? [];

        var explicitReturn = GetFluentReturnAttributeValue(classDecl);
        if (!string.IsNullOrEmpty(explicitReturn))
        {
            if (declared.Count > 0 && !declared.Contains(explicitReturn))
            {
                throw new InvalidOperationException(
                    $"[FluentReturn(\"{explicitReturn}\")] on '{classDecl.Identifier.Text}' names a type " +
                    $"parameter the class does not declare. Declared: {string.Join(", ", declared)}.");
            }

            return explicitReturn;
        }

        if (declared.Contains("TSelf"))
            return "TSelf";

        if (declared.Count == 1)
            return declared[0];

        if (declared.Count == 0)
            return "";

        throw new InvalidOperationException(
            $"Cannot infer the fluent return type for '{classDecl.Identifier.Text}': it declares " +
            $"{declared.Count} type parameters ({string.Join(", ", declared)}) and none is named 'TSelf'. " +
            $"Add [FluentReturn(\"<name>\")] to the class to say which one public members return.");
    }

    /// <summary>
    /// Reads the single string argument of a <c>[FluentReturn(...)]</c> attribute, if present.
    /// </summary>
    private static string? GetFluentReturnAttributeValue(ClassDeclarationSyntax classDecl)
    {
        var attribute = classDecl.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(a =>
            {
                var name = a.Name.ToString();
                return name is "FluentReturn" or "FluentReturnAttribute"
                    || name.EndsWith(".FluentReturn", StringComparison.Ordinal)
                    || name.EndsWith(".FluentReturnAttribute", StringComparison.Ordinal);
            });

        var argument = attribute?.ArgumentList?.Arguments.FirstOrDefault()?.Expression;

        return argument switch
        {
            LiteralExpressionSyntax literal => literal.Token.ValueText,
            // nameof(TSelf) - take the operand's text
            InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } invocation
                => invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString(),
            _ => null
        };
    }

    /// <summary>
    /// Extracts the namespace from a compilation unit.
    /// </summary>
    public string? GetNamespace(CompilationUnitSyntax root)
    {
        var namespaceDecl = root.Members
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (namespaceDecl != null)
            return namespaceDecl.Name.ToString();

        var fileScoped = root.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        return fileScoped?.Name.ToString();
    }

    /// <summary>
    /// Extracts using namespace names from a compilation unit (without the
    /// "using" keyword or trailing semicolon).
    /// </summary>
    public List<string> GetUsingStatements(CompilationUnitSyntax root)
    {
        return root.Usings
            .Select(u => u.Name?.ToString() ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();
    }
}
