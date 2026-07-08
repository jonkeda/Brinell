namespace Brinell.Generator.Models;

/// <summary>
/// Represents a property pattern match with its handler and extracted metadata.
/// Used by PropertyMethodAnalyzer to group Is/Wait/Assert method generation.
/// </summary>
public class PropertyMethodGroup
{
    /// <summary>
    /// Gets or sets the extracted core method metadata.
    /// </summary>
    public MethodInfo CoreMethod { get; set; } = new();

    /// <summary>
    /// Gets or sets the handler that matched and extracted this property.
    /// </summary>
    public IMethodHandler Handler { get; set; } = null!;
}
