namespace Brinell.Generator.Generation;

/// <summary>
/// Generates property methods (Is/Wait/Assert) for property pattern groups.
/// </summary>
public class PropertyWrapperGenerator
{
    /// <summary>
    /// Generates Is*, Wait*, and Assert* methods for a property group.
    /// </summary>
    /// <param name="group">The property method group.</param>
    /// <param name="containingTypeName">The name of the containing type (for return types).</param>
    /// <param name="typeParameters">Generic type parameters (e.g., "&lt;TScope&gt;").</param>
    /// <returns>The generated property methods as a formatted C# code string.</returns>
    public string GenerateMethods(
        Models.PropertyMethodGroup group,
        string containingTypeName = "",
        string typeParameters = "")
    {
        return group.Handler.GenerateWrapper(group.CoreMethod, containingTypeName, typeParameters);
    }

    /// <summary>
    /// Generates Is*, Wait*, and Assert* methods for all property groups.
    /// </summary>
    /// <param name="groups">The property method groups.</param>
    /// <param name="containingTypeName">The name of the containing type (for return types).</param>
    /// <param name="typeParameters">Generic type parameters (e.g., "&lt;TScope&gt;").</param>
    /// <returns>List of generated property method code strings.</returns>
    public List<string> GenerateAllMethods(
        List<Models.PropertyMethodGroup> groups,
        string containingTypeName = "",
        string typeParameters = "")
    {
        var methods = new List<string>();
        foreach (var group in groups)
        {
            var generated = GenerateMethods(group, containingTypeName, typeParameters);
            methods.Add(generated);
        }
        return methods;
    }
}
