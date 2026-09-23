using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brinell.Generator.Models;

/// <summary>
/// Carries the analyzed ControlObject metadata that member generators and the
/// builder need in order to emit public members. Keeping this in one place lets
/// the same generators be reused across platforms by varying the element type.
/// </summary>
public sealed class ControlObjectContext
{
    /// <summary>
    /// Gets the containing type name (e.g., "ControlBase").
    /// </summary>
    public string ContainingTypeName { get; init; } = "";

    /// <summary>
    /// Gets the generic type parameters (e.g., "&lt;TScope&gt;"), or an empty string.
    /// </summary>
    public string TypeParameters { get; init; } = "";

    /// <summary>
    /// Gets the platform element type used by Core methods (e.g., "IMauiElement").
    /// </summary>
    public string ElementType { get; init; } = "IMauiElement";

    /// <summary>
    /// Gets the type parameter that public members return for fluent chaining.
    /// Controls return the containing scope (<c>TScope</c>); containers and collections
    /// return themselves (<c>TSelf</c>). Resolved by
    /// <see cref="Analysis.ControlObjectAnalyzer.ResolveFluentReturnType"/>.
    /// Empty when the class has no type parameters, in which case actions return void.
    /// </summary>
    public string FluentReturnType { get; init; } = "TScope";

    /// <summary>
    /// Gets the namespace of the source class (null when none).
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Gets the using directives from the source file.
    /// </summary>
    public IReadOnlyList<string> Usings { get; init; } = [];

    /// <summary>
    /// Gets the original class declaration, used to preserve signature and documentation.
    /// </summary>
    public ClassDeclarationSyntax ClassDeclaration { get; init; } = null!;
}
