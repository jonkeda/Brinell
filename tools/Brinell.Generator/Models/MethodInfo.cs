namespace Brinell.Generator.Models;

/// <summary>
/// Portable metadata for a method extracted from Roslyn syntax tree.
/// Does not retain syntax tree references; only stores essential information for code generation.
/// </summary>
public class MethodInfo
{
    /// <summary>
    /// Gets the original method name (e.g., "RightClickCore").
    /// </summary>
    public string MethodName { get; set; } = "";

    /// <summary>
    /// Gets the public wrapper method name (e.g., "RightClick").
    /// </summary>
    public string PublicMethodName { get; set; } = "";

    /// <summary>
    /// Gets the parameter list (excluding the first IMauiElement parameter).
    /// Each item is a tuple of (TypeName, ParameterName, DefaultValue?).
    /// </summary>
    public List<(string TypeName, string ParameterName, string? DefaultValue)> Parameters { get; } = [];

    /// <summary>
    /// Gets the XML documentation comment for the method (if present).
    /// </summary>
    public string? XmlDocumentation { get; set; }

    /// <summary>
    /// Gets the return type (e.g., "void", "bool", "string").
    /// </summary>
    public string ReturnType { get; set; } = "void";

    /// <summary>
    /// Gets the comparison variants to emit for a Get*Core query, read from
    /// [GenerateComparisons] on the Core method. Defaults to equality only.
    /// </summary>
    public List<string> Comparisons { get; } = [];
}
