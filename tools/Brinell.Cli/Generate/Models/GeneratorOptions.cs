namespace Brinell.Generator.Models;

/// <summary>
/// Configuration options for the overall code generator.
/// </summary>
public class GeneratorOptions
{
    /// <summary>
    /// Gets or sets the input file path to read from.
    /// </summary>
    public string InputFilePath { get; set; } = "";

    /// <summary>
    /// Gets or sets the output file path to write to.
    /// </summary>
    public string OutputFilePath { get; set; } = "";

    /// <summary>
    /// Gets or sets whether to include a generated header comment.
    /// </summary>
    public bool IncludeGeneratedHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets the class name to generate methods for (if not specified, uses first class found).
    /// </summary>
    public string? TargetClassName { get; set; }
}
