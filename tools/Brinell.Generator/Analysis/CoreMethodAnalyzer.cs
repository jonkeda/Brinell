using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brinell.Generator.Analysis;

/// <summary>
/// Analyzes a C# file using Roslyn to extract methods for code generation.
/// </summary>
public class CoreMethodAnalyzer
{
    /// <summary>
    /// Parses a C# code file and extracts method information using the provided handlers.
    /// </summary>
    /// <param name="sourceCode">The C# source code to analyze.</param>
    /// <param name="handlers">The method handlers to apply.</param>
    /// <param name="targetClassName">Optional target class name; if not specified, uses the first class.</param>
    /// <returns>Tuple of (ClassDeclarationSyntax, MethodInfo list, CompilationUnitSyntax)</returns>
    public (ClassDeclarationSyntax? ClassDecl, List<Models.MethodInfo> Methods, CompilationUnitSyntax Root) 
        AnalyzeCode(string sourceCode, IEnumerable<IMethodHandler> handlers, string? targetClassName = null)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        // Find the target class
        var classDecl = FindClass(root, targetClassName);
        if (classDecl == null)
            return (null, new List<Models.MethodInfo>(), root);

        // Extract methods using handlers
        var methods = ExtractMethods(classDecl, handlers);

        return (classDecl, methods, root);
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
    /// Extracts method information from a class using the provided handlers.
    /// </summary>
    private List<Models.MethodInfo> ExtractMethods(ClassDeclarationSyntax classDecl, IEnumerable<IMethodHandler> handlers)
    {
        var methods = new List<Models.MethodInfo>();
        var handlerList = handlers.ToList();

        var classMethods = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .ToList();

        foreach (var method in classMethods)
        {
            foreach (var handler in handlerList)
            {
                if (handler.Matches(method))
                {
                    var methodInfo = handler.Extract(method);
                    methods.Add(methodInfo);
                    break; // Only process with first matching handler
                }
            }
        }

        return methods;
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
    /// Extracts using statements from a compilation unit as formatted strings.
    /// </summary>
    public List<string> GetUsingStatements(CompilationUnitSyntax root)
    {
        return root.Usings
            .Select(u => u.ToString().TrimEnd())
            .ToList();
    }
}
