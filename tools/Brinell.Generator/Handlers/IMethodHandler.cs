using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brinell.Generator;

/// <summary>
/// Interface for pluggable method extraction and wrapper generation strategies.
/// Handlers define how to identify and transform methods for code generation.
/// </summary>
public interface IMethodHandler
{
    /// <summary>
    /// Determines if this handler should process a method.
    /// </summary>
    /// <param name="method">The method syntax to evaluate.</param>
    /// <returns>True if this handler should process the method; otherwise, false.</returns>
    bool Matches(MethodDeclarationSyntax method);

    /// <summary>
    /// Extracts portable metadata from a matched method.
    /// </summary>
    /// <param name="method">The method syntax to extract from.</param>
    /// <returns>Portable method metadata for code generation.</returns>
    Models.MethodInfo Extract(MethodDeclarationSyntax method);

    /// <summary>
    /// Generates a public wrapper method as formatted C# code.
    /// </summary>
    /// <param name="coreMethod">The extracted core method metadata.</param>
    /// <param name="containingTypeName">The name of the containing type (for return types).</param>
    /// <param name="typeParameters">Generic type parameters (e.g., "&lt;TScope&gt;").</param>
    /// <returns>The generated wrapper method as a formatted C# code string.</returns>
    string GenerateWrapper(Models.MethodInfo coreMethod, string containingTypeName, string typeParameters);
}
