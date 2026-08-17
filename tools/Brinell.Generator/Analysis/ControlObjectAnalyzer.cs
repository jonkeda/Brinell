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
