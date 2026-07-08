namespace Brinell.Generator.Models;

/// <summary>
/// Configuration options for a method handler.
/// </summary>
public class HandlerOptions
{
    /// <summary>
    /// Gets or sets the method name suffix to match (e.g., "Core").
    /// </summary>
    public string MethodSuffix { get; set; } = "Core";

    /// <summary>
    /// Gets or sets whether to require the 'virtual' modifier.
    /// </summary>
    public bool RequireVirtual { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require the 'protected' modifier.
    /// </summary>
    public bool RequireProtected { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix to prepend to public method names (typically empty).
    /// </summary>
    public string PublicMethodPrefix { get; set; } = "";

    /// <summary>
    /// Gets or sets the name of the first parameter to skip when generating public signature.
    /// Typically "element" for IMauiElement parameter.
    /// </summary>
    public string SkipFirstParameterName { get; set; } = "element";
}
