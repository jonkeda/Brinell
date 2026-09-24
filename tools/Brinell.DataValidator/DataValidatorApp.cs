using System.Text.Json;
using Brinell.TestData;
using Brinell.Uat;

namespace Brinell.DataValidator;

/// <summary>The <c>brinell-data</c> commands.</summary>
public static class DataValidatorApp
{
    private const string ConfigFileName = "uat.config.md";

    private static readonly JsonSerializerOptions OutputJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Runs one command. Returns 0 when fine, 1 when the data has problems, 2 when the tool could not run.</summary>
    public static int Run(string[] args, TextWriter output, TextWriter errors, string currentDirectory)
    {
        try
        {
            var options = ToolOptions.Parse(args, currentDirectory);
            return options.Command switch
            {
                "validate" => Validate(options, output, errors),
                "describe" => Describe(options, output, errors),
                "list" => List(options, output, errors),
                "schema" => Schema(options, output, errors),
                "help" or "--help" or "-h" => Usage(output, 0),
                _ => Usage(errors, 2),
            };
        }
        catch (ToolException exception)
        {
            errors.WriteLine($"error: {exception.Message}");
            return 2;
        }
    }

    private static int Validate(ToolOptions options, TextWriter output, TextWriter errors)
    {
        var definitions = LoadDefinitions(options, errors);
        List<TestDataDiagnostic> diagnostics = [];

        if (options.Argument is { } file)
        {
            var path = Path.GetFullPath(Path.Combine(options.CurrentDirectory, file));
            if (!File.Exists(path))
            {
                throw new ToolException($"'{file}' does not exist.");
            }

            var owner = definitions.FirstOrDefault(d => path.StartsWith(d.Folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                ?? definitions[0];
            diagnostics.AddRange(TestDataValidator.Validate(owner, path).Diagnostics);
        }
        else
        {
            foreach (var definition in definitions)
            {
                diagnostics.AddRange(TestDataValidator.Validate(definition).Diagnostics);
            }

            if (options.ConfigPath is { } config)
            {
                var known = definitions.SelectMany(d => d.TestSetNames).ToHashSet(StringComparer.Ordinal);
                diagnostics.AddRange(ReferenceCheck.Check(ReferenceCheck.Find(config), known));
            }
        }

        if (options.Json)
        {
            output.WriteLine(JsonSerializer.Serialize(
                diagnostics.Select(d => new { file = Display(options, d.File), d.Line, d.Code, d.Path, d.Message }),
                OutputJson));
        }
        else
        {
            foreach (var diagnostic in diagnostics)
            {
                output.WriteLine((diagnostic with { File = Display(options, diagnostic.File) }).ToString());
            }

            output.WriteLine(diagnostics.Count == 0
                ? $"No problems in {definitions.Sum(d => d.TestSetNames.Count)} test set(s)."
                : $"{diagnostics.Count} error(s).");
        }

        return diagnostics.Count == 0 ? 0 : 1;
    }

    private static int Describe(ToolOptions options, TextWriter output, TextWriter errors)
    {
        var name = options.Argument ?? throw new ToolException("describe needs a type name, e.g. 'brinell-data describe TodoDto'.");
        var definitions = LoadDefinitions(options, errors);

        var candidates = definitions
            .SelectMany(d => TestDataTypes.Reachable(d.JsonOptions, d.SetType, d.TableTypes).Select(type => (Definition: d, Type: type)))
            .Where(pair => !pair.Type.IsEnum && TestDataTypes.FieldsOf(pair.Definition.JsonOptions, pair.Type).Count > 0)
            .ToList();

        var match = candidates.FirstOrDefault(pair => string.Equals(pair.Type.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match.Type is null)
        {
            throw new ToolException($"No test-data type '{name}'. There are: {string.Join(", ", candidates.Select(c => c.Type.Name).Distinct())}.");
        }

        var fields = TestDataTypes.FieldsOf(match.Definition.JsonOptions, match.Type);
        output.WriteLine($"{match.Type.Name} ({match.Type.FullName})");
        var width = fields.Max(f => f.JsonName.Length) + 2;
        var typeWidth = fields.Max(f => f.TypeName.Length) + 2;
        foreach (var field in fields)
        {
            var notes = new List<string> { field.Required ? "required" : "optional" };
            if (field.Nullable)
            {
                notes.Add("may be null");
            }

            if (field.Values.Count > 0)
            {
                notes.Add(string.Join(" | ", field.Values));
            }

            output.WriteLine($"  {field.JsonName.PadRight(width)}{field.TypeName.PadRight(typeWidth)}{string.Join(", ", notes)}");
        }

        return 0;
    }

    private static int List(ToolOptions options, TextWriter output, TextWriter errors)
    {
        var definitions = LoadDefinitions(options, errors);
        var references = options.ConfigPath is { } config ? ReferenceCheck.Find(config) : [];

        foreach (var definition in definitions)
        {
            output.WriteLine($"{definition.Name} ({Display(options, definition.Folder)})");
            var width = definition.TestSetNames.DefaultIfEmpty(string.Empty).Max(n => n.Length) + 2;
            foreach (var name in definition.TestSetNames)
            {
                var usedBy = references.Where(r => r.Name == name).Select(r => Display(options, r.File)).Distinct().ToList();
                var used = usedBy.Count == 0 ? string.Empty : $"   used by: {string.Join(", ", usedBy)}";
                output.WriteLine($"  {name.PadRight(width)}{Counts(definition.FileOf(name))}{used}");
            }
        }

        return 0;
    }

    private static int Schema(ToolOptions options, TextWriter output, TextWriter errors)
    {
        var stale = 0;
        foreach (var definition in LoadDefinitions(options, errors))
        {
            var path = TestDataSchema.PathOf(definition);
            var schema = TestDataSchema.Export(definition);
            var current = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : null;

            if (options.Check)
            {
                if (current != schema)
                {
                    output.WriteLine($"{Display(options, path)} is out of date. Run 'brinell-data schema' to regenerate it.");
                    stale++;
                }
                else
                {
                    output.WriteLine($"{Display(options, path)} is up to date.");
                }
            }
            else if (current != schema)
            {
                File.WriteAllText(path, schema);
                output.WriteLine($"Wrote {Display(options, path)}.");
            }
            else
            {
                output.WriteLine($"{Display(options, path)} is up to date.");
            }
        }

        return stale == 0 ? 0 : 1;
    }

    private static IReadOnlyList<ITestDataDefinition> LoadDefinitions(ToolOptions options, TextWriter errors)
    {
        var assemblies = options.Assemblies.Count > 0 ? options.Assemblies : AssembliesFromConfig(options);
        return assemblies.SelectMany(path => DefinitionLoader.Load(path, errors)).ToList();
    }

    private static IReadOnlyList<string> AssembliesFromConfig(ToolOptions options)
    {
        if (options.ConfigPath is not { } config)
        {
            throw new ToolException($"No {ConfigFileName} here and no --assembly given. Run from a UAT project folder, or pass --assembly <path-to-dll>.");
        }

        var directory = Path.GetDirectoryName(config)!;
        var assemblies = UatConfigParser.ParseFile(config).Assemblies
            .Where(a => string.Equals(a.Kind, UatConfigFields.TestDataAssemblyKind, StringComparison.OrdinalIgnoreCase))
            .Select(a => Path.GetFullPath(Path.Combine(directory, a.Assembly)))
            .ToList();

        return assemblies.Count > 0
            ? assemblies
            : throw new ToolException($"{config} has no '{UatConfigFields.TestDataAssemblyKind}' row in its Assemblies table, and no --assembly was given.");
    }

    private static string Counts(string file)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            var arrays = document.RootElement.EnumerateObject()
                .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                .Select(p => $"{p.Name} {p.Value.GetArrayLength()}");
            return string.Join(", ", arrays);
        }
        catch (JsonException)
        {
            return "(not valid JSON)";
        }
    }

    private static string Display(ToolOptions options, string path)
        => Path.GetRelativePath(options.CurrentDirectory, path).Replace('\\', '/');

    private static int Usage(TextWriter writer, int exitCode)
    {
        writer.WriteLine("""
            brinell-data - check an app's Brinell test data

            Usage (from a UAT project folder with a uat.config.md, or with --assembly):
              brinell-data validate [file] [--format json]   check every test set, or one file
              brinell-data describe <Type>                   the fields of a test-data type
              brinell-data list                              the test sets and what uses them
              brinell-data schema [--check]                  write (or check) testdata.schema.json

            Options:
              --assembly <path>   the assembly with the TestDataDefinition (repeatable)
              --config <path>     the uat.config.md to read (default: ./uat.config.md)
              --format json       validate prints JSON, for tools and models

            Exit code: 0 fine, 1 problems found, 2 the tool could not run.
            """);
        return exitCode;
    }
}

internal sealed record ToolOptions(
    string Command,
    string? Argument,
    IReadOnlyList<string> Assemblies,
    string? ConfigPath,
    bool Json,
    bool Check,
    string CurrentDirectory)
{
    public static ToolOptions Parse(string[] args, string currentDirectory)
    {
        if (args.Length == 0)
        {
            return new ToolOptions("help", null, [], null, false, false, currentDirectory);
        }

        string? argument = null;
        string? config = null;
        List<string> assemblies = [];
        var json = false;
        var check = false;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--assembly" when i + 1 < args.Length:
                    assemblies.Add(Path.GetFullPath(Path.Combine(currentDirectory, args[++i])));
                    break;
                case "--config" when i + 1 < args.Length:
                    config = Path.GetFullPath(Path.Combine(currentDirectory, args[++i]));
                    break;
                case "--format" when i + 1 < args.Length:
                    json = args[++i] == "json";
                    break;
                case "--check":
                    check = true;
                    break;
                case var value when value.StartsWith("--", StringComparison.Ordinal):
                    throw new ToolException($"Unknown option '{value}'.");
                default:
                    argument ??= args[i];
                    break;
            }
        }

        config ??= Path.Combine(currentDirectory, "uat.config.md");
        return new ToolOptions(args[0], argument, assemblies, File.Exists(config) ? config : null, json, check, currentDirectory);
    }
}
