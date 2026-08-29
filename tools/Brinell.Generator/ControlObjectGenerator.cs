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

        // A near-miss Core method used to vanish silently; with [SkipGeneration] available to
        // declare a deliberate exclusion, an undeclared one is an error rather than a maybe.
        var skipped = _analyzer.FindSilentlySkippedCoreMethods(classDecl);
        if (skipped.Count > 0)
        {
            throw new InvalidOperationException(
                $"In {classDecl.Identifier.Text}: " + string.Join(" ", skipped));
        }

        var context = _analyzer.BuildContext(classDecl, root);

        var members = new List<string>();

        // Tracks which Core method claimed each generated member name, so colliding
        // names fail with a clear message instead of emitting uncompilable code.
        var claimedNames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var method in _analyzer.CoreMethods(classDecl))
        {
            // Filtered once here rather than in each generator's Matches: opting out is a
            // property of the method, not of which family would have claimed it.
            if (ControlObjectAnalyzer.IsGenerationSkipped(method))
                continue;

            foreach (var generator in _generators)
            {
                if (!generator.Matches(method))
                    continue;

                var info = generator.Extract(method);

                if (claimedNames.TryGetValue(info.PublicMethodName, out var previousCoreMethod))
                {
                    throw new InvalidOperationException(
                        $"Generated member name '{info.PublicMethodName}' is claimed by both " +
                        $"'{previousCoreMethod}' and '{info.MethodName}' in {context.ContainingTypeName}. " +
                        "Rename one of the Core methods — overloads that differ only by parameters " +
                        "collide on the generated name.");
                }

                claimedNames.Add(info.PublicMethodName, info.MethodName);

                members.Add(generator.Generate(info, context));
                break; // first matching generator wins
            }
        }

        // A template with no Core methods still gets a (member-less) partial: echoing the
        // source back would duplicate the class declaration in the .gen.cs file.
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
    /// Creates a generator wired with the default member generators. Order matters:
    /// the Is/Wait/Assert family and the Set family are registered before the broader
    /// action family so <c>Is*Core</c>, <c>Get*Core</c>, and <c>Set*Core</c> are not
    /// captured as plain actions.
    /// </summary>
    public static ControlObjectGenerator CreateDefault() =>
        new ControlObjectGenerator()
            .Register(new IsWaitAssertGenerator())
            .Register(new SetGenerator())
            .Register(new ActionGenerator());
}
