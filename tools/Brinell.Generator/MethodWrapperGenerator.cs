using Brinell.Generator.Analysis;
using Brinell.Generator.Generation;
using Brinell.Generator.Models;

namespace Brinell.Generator;

/// <summary>
/// Main orchestrator for code generation workflow.
/// Coordinates analysis, generation, and formatting to produce generated code files.
/// </summary>
public class MethodWrapperGenerator
{
    private readonly CoreMethodAnalyzer _analyzer;
    private readonly WrapperGenerator _wrapperGenerator;
    private readonly CodeFormatter _formatter;

    public MethodWrapperGenerator()
    {
        _analyzer = new CoreMethodAnalyzer();
        _wrapperGenerator = new WrapperGenerator();
        _formatter = new CodeFormatter();
    }

    /// <summary>
    /// Generates wrapper methods from a source file and returns the formatted code.
    /// </summary>
    /// <param name="sourceCode">The C# source code to analyze.</param>
    /// <param name="options">Generation options including handlers.</param>
    /// <returns>The formatted generated code as a string.</returns>
    public string Generate(string sourceCode, GeneratorOptions options)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new ArgumentException("Source code cannot be empty.", nameof(sourceCode));

        if (!options.Handlers.Any())
            throw new ArgumentException("At least one handler must be provided.", nameof(options));

        // Analyze the source code
        var (classDecl, methods, root) = _analyzer.AnalyzeCode(sourceCode, options.Handlers, options.TargetClassName);

        if (classDecl == null)
            throw new InvalidOperationException($"Could not find class {options.TargetClassName ?? "(any)"} in source code.");

        if (!methods.Any())
            return sourceCode; // No methods to generate

        // Generate wrapper methods
        var typeParams = _analyzer.GetTypeParameters(classDecl);
        var wrapperMethods = _wrapperGenerator.GenerateWrapperMethods(
            methods,
            options.Handlers,
            classDecl.Identifier.Text,
            typeParams);

        // Build complete compilation unit with generated methods
        var @namespace = _analyzer.GetNamespace(root);
        var usings = _analyzer.GetUsingStatements(root);
        var code = _wrapperGenerator.BuildCompilationUnit(
            classDecl.Identifier.Text,
            wrapperMethods,
            @namespace,
            usings,
            options.IncludeGeneratedHeader,
            classDecl);

        // Format and return
        return _formatter.Format(code);
    }

    /// <summary>
    /// Generates wrapper methods from a file and writes to output file.
    /// </summary>
    /// <param name="inputFilePath">Path to source file.</param>
    /// <param name="outputFilePath">Path to write generated file.</param>
    /// <param name="options">Generation options.</param>
    public void GenerateToFile(string inputFilePath, string outputFilePath, GeneratorOptions options)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        var sourceCode = File.ReadAllText(inputFilePath);
        var generatedCode = Generate(sourceCode, options);

        File.WriteAllText(outputFilePath, generatedCode);
    }
}
