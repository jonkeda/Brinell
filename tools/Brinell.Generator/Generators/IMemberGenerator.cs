using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Models;

namespace Brinell.Generator.Generators;

/// <summary>
/// Recognizes and emits a family of public ControlObject members from a base
/// Core method. Member generators are registered with
/// <see cref="ControlObjectGenerator"/> and run over the analyzed source.
/// </summary>
public interface IMemberGenerator
{
    /// <summary>
    /// Determines whether this generator handles the given Core method.
    /// </summary>
    /// <param name="method">The method syntax to evaluate.</param>
    /// <returns>True if this generator should process the method; otherwise, false.</returns>
    bool Matches(MethodDeclarationSyntax method);

    /// <summary>
    /// Extracts portable metadata from a matched Core method.
    /// </summary>
    /// <param name="method">The method syntax to extract from.</param>
    /// <returns>Portable method metadata for code generation.</returns>
    MethodInfo Extract(MethodDeclarationSyntax method);

    /// <summary>
    /// Emits the public member(s) for one Core method as formatted C# code.
    /// </summary>
    /// <param name="coreMethod">The extracted core method metadata.</param>
    /// <param name="context">The analyzed ControlObject context.</param>
    /// <returns>The generated member(s) as a formatted C# code string.</returns>
    string Generate(MethodInfo coreMethod, ControlObjectContext context);
}
