using Brinell.Generator;
using Brinell.Generator.Handlers;
using Brinell.Generator.Models;

if (args.Length < 2)
{
    PrintUsage();
    Environment.Exit(1);
}

string? inputFile = null;
string? outputFile = null;
string? className = null;

// Parse arguments
for (int i = 0; i < args.Length; i++)
{
    if ((args[i] == "--input" || args[i] == "-i") && i + 1 < args.Length)
    {
        inputFile = args[++i];
    }
    else if ((args[i] == "--output" || args[i] == "-o") && i + 1 < args.Length)
    {
        outputFile = args[++i];
    }
    else if ((args[i] == "--class" || args[i] == "-c") && i + 1 < args.Length)
    {
        className = args[++i];
    }
}

if (string.IsNullOrWhiteSpace(inputFile) || string.IsNullOrWhiteSpace(outputFile))
{
    Console.Error.WriteLine("Error: --input and --output are required");
    PrintUsage();
    Environment.Exit(1);
}

try
{
    if (!File.Exists(inputFile))
    {
        Console.Error.WriteLine($"Error: Input file not found: {inputFile}");
        Environment.Exit(1);
    }

    Console.WriteLine($"Analyzing {inputFile}...");

    var generator = new MethodWrapperGenerator();
    var options = new GeneratorOptions
    {
        InputFilePath = inputFile,
        OutputFilePath = outputFile,
        TargetClassName = className,
        IncludeGeneratedHeader = true,
        Handlers = new List<IMethodHandler>
        {
            new CoreMethodHandler()
        }
    };

    generator.GenerateToFile(inputFile, outputFile, options);
    Console.WriteLine($"Successfully generated: {outputFile}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    if (!string.IsNullOrEmpty(ex.StackTrace))
        Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}

void PrintUsage()
{
    Console.WriteLine("Brinell Method Wrapper Generator");
    Console.WriteLine();
    Console.WriteLine("Usage: Brinell.Generator.Cli [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --input, -i <path>    Input C# source file to analyze");
    Console.WriteLine("  --output, -o <path>   Output file to write generated code");
    Console.WriteLine("  --class, -c <name>    Target class name (optional)");
}

