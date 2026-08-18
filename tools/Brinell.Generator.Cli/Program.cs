using Brinell.Generator;
using Brinell.Generator.Generators;
using Brinell.Generator.Models;

const string TemplateExtension = ".tpl.cs";
const string GeneratedExtension = ".gen.cs";

if (args.Length < 2)
{
    PrintUsage();
    Environment.Exit(1);
}

string? input = null;
string members = "all";

// Parse arguments
for (int i = 0; i < args.Length; i++)
{
    if ((args[i] == "--input" || args[i] == "-i") && i + 1 < args.Length)
    {
        input = args[++i];
    }
    else if (args[i] == "--members" && i + 1 < args.Length)
    {
        members = args[++i].ToLowerInvariant();
    }
}

if (string.IsNullOrWhiteSpace(input))
{
    Console.Error.WriteLine("Error: --input is required");
    PrintUsage();
    Environment.Exit(1);
}

if (members is not ("all" or "actions" or "state"))
{
    Console.Error.WriteLine($"Error: --members must be one of: all, actions, state (got '{members}')");
    Environment.Exit(1);
}

var inputFiles = new List<string>();

foreach (var entry in input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
{
    if (Directory.Exists(entry))
    {
        var found = Directory
            .EnumerateFiles(entry, "*" + TemplateExtension, SearchOption.AllDirectories)
            .Where(IsTemplateFile);

        inputFiles.AddRange(found);
    }
    else if (File.Exists(entry))
    {
        if (!IsTemplateFile(entry))
        {
            Console.Error.WriteLine($"Skipping (not a {TemplateExtension} file): {entry}");
            continue;
        }

        inputFiles.Add(entry);
    }
    else
    {
        Console.Error.WriteLine($"Error: Input not found: {entry}");
        Environment.Exit(1);
    }
}

inputFiles = inputFiles
    .Select(Path.GetFullPath)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .ToList();

if (inputFiles.Count == 0)
{
    Console.Error.WriteLine($"Error: No {TemplateExtension} files found for --input '{input}'");
    Environment.Exit(1);
}

var generator = BuildGenerator(members);
int failed = 0;

foreach (var inputFile in inputFiles)
{
    var outputFile = GetOutputPath(inputFile);

    try
    {
        Console.WriteLine($"Analyzing {inputFile}...");

        var options = new GeneratorOptions
        {
            InputFilePath = inputFile,
            OutputFilePath = outputFile,
            IncludeGeneratedHeader = true
        };

        generator.GenerateToFile(inputFile, outputFile, options);
        Console.WriteLine($"Successfully generated: {outputFile}");
    }
    catch (InvalidOperationException ex)
    {
        // Validation failures (name collisions, no target class) are expected input
        // errors — report the message without a stack trace.
        failed++;
        Console.Error.WriteLine($"Error processing {inputFile}: {ex.Message}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"Error processing {inputFile}: {ex.Message}");
        if (!string.IsNullOrEmpty(ex.StackTrace))
            Console.Error.WriteLine(ex.StackTrace);
    }
}

if (failed > 0)
{
    Console.Error.WriteLine($"{failed} of {inputFiles.Count} file(s) failed.");
    Environment.Exit(1);
}

static bool IsTemplateFile(string path)
    => path.EndsWith(TemplateExtension, StringComparison.OrdinalIgnoreCase);

static string GetOutputPath(string inputFile)
    => string.Concat(inputFile.AsSpan(0, inputFile.Length - TemplateExtension.Length), GeneratedExtension);

static ControlObjectGenerator BuildGenerator(string members)
{
    if (members == "all")
        return ControlObjectGenerator.CreateDefault();

    var generator = new ControlObjectGenerator();
    if (members == "state")
        generator.Register(new IsWaitAssertGenerator());
    if (members == "actions")
        generator.Register(new ActionGenerator());
    return generator;
}

void PrintUsage()
{
    Console.WriteLine("Brinell ControlObject Generator");
    Console.WriteLine();
    Console.WriteLine("Usage: Brinell.Generator.Cli [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --input, -i <paths>   Semicolon-separated list of files and/or folders.");
    Console.WriteLine("                        Folders are searched recursively.");
    Console.WriteLine($"                        Only files ending in {TemplateExtension} are processed.");
    Console.WriteLine($"                        Output is written next to each input as <name>{GeneratedExtension}.");
    Console.WriteLine("  --members <value>     Which members to generate: all (default), actions, state");
}
