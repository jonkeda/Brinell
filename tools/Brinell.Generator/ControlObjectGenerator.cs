using Brinell.Generator.Analysis;
using Brinell.Generator.Generation;
using Brinell.Generator.Generators;
using Brinell.Generator.Models;

namespace Brinell.Generator;

/// <summary>
/// Top-level coordinator for generating Brinell ControlObjects. Registered
/// member generators (e.g., <see cref="ActionGenerator"/> and
/// <see cref="IsWaitAssertGenerator"/>) are run over the analyzed source to emit
/// public members from base Core method definitions.
/// </summary>
public sealed class ControlObjectGenerator
{
    private readonly List<IMemberGenerator> _generators = new();
    private readonly ControlObjectAnalyzer _analyzer = new();
    private readonly ControlObjectBuilder _builder = new();
    private readonly CodeFormatter _formatter = new();

    /// <summary>
    /// Registers a member generator. Registration order matters: the first
    /// generator whose <c>Matches</c> returns true handles the Core method.
    /// </summary>
    public ControlObjectGenerator Register(IMemberGenerator generator)
    {
        _generators.Add(generator);
        return this;
    }

    /// <summary>
    /// Generates ControlObject members from source and returns the formatted code.
    /// </summary>
    /// <param name="sourceCode">The C# source code to analyze.</param>
    /// <param name="options">Generation options.</param>
    /// <returns>The formatted generated code as a string.</returns>
    public string Generate(string sourceCode, GeneratorOptions options)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new ArgumentException("Source code cannot be empty.", nameof(sourceCode));

        if (_generators.Count == 0)
            throw new InvalidOperationException("At least one member generator must be registered.");

        var (classDecl, root) = _analyzer.FindTarget(sourceCode, options.TargetClassName);

        if (classDecl == null)
            throw new InvalidOperationException($"Could not find class {options.TargetClassName ?? "(any)"} in source code.");

        var context = _analyzer.BuildContext(classDecl, root);

        var members = new List<string>();
        foreach (var method in _analyzer.CoreMethods(classDecl))
        {
            foreach (var generator in _generators)
            {
                if (!generator.Matches(method))
                    continue;

                var info = generator.Extract(method);
                members.Add(generator.Generate(info, context));
                break; // first matching generator wins
            }
        }

        if (members.Count == 0)
            return sourceCode; // No members to generate

        var code = _builder.BuildCompilationUnit(context, members, options.IncludeGeneratedHeader);
        return _formatter.Format(code);
    }

    /// <summary>
    /// Generates ControlObject members from a file and writes to the output file.
    /// </summary>
    /// <param name="inputFilePath">Path to the source file.</param>
    /// <param name="outputFilePath">Path to write the generated file.</param>
    /// <param name="options">Generation options.</param>
    public void GenerateToFile(string inputFilePath, string outputFilePath, GeneratorOptions options)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        var sourceCode = File.ReadAllText(inputFilePath);
        var generatedCode = Generate(sourceCode, options);

        File.WriteAllText(outputFilePath, generatedCode);
    }

    /// <summary>
    /// Creates a generator wired with the default member generators: the
    /// Is/Wait/Assert family (registered first so <c>Is*Core</c> is not captured
    /// by the broader action family) and the action family.
    /// </summary>
    public static ControlObjectGenerator CreateDefault() =>
        new ControlObjectGenerator()
            .Register(new IsWaitAssertGenerator())
            .Register(new ActionGenerator());
}
