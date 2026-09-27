using System.Text.Json;
using Brinell.Core.Testing;
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
                "phrases" => Phrases(options, output),
                "references" => References(options, output),
                "snippet" => Snippet(options, output),
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
        List<TestDataDiagnostic> diagnostics = [];
        var scenarioFile = options.Argument is { } argument && argument.EndsWith(UatFileFormat.FeatureExtension, StringComparison.OrdinalIgnoreCase)
            ? Existing(options, argument)
            : null;
        var testSets = 0;
        var scenarios = 0;

        if (options.Argument is null && options.ConfigPath is { } project)
        {
            diagnostics.AddRange(CheckOldForms(options, project, output));
        }

        if (scenarioFile is null && (options.Assemblies.Count > 0 || HasTestData(options) || options.ConfigPath is null || !ScenarioCheck.CanCheck(options.ConfigPath)))
        {
            var definitions = LoadDefinitions(options, errors);
            testSets = definitions.Sum(d => d.TestSetNames.Count);
            if (options.Argument is { } file)
            {
                var path = Existing(options, file);
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
        }

        if (options.Argument is null || scenarioFile is not null)
        {
            if (options.ConfigPath is { } config && ScenarioCheck.CanCheck(config))
            {
                var files = scenarioFile is null ? ScenarioCheck.Files(config) : [scenarioFile];
                var catalogs = ScenarioCheck.Catalogs(config);
                if (scenarioFile is null)
                {
                    diagnostics.AddRange(CheckPhraseFiles(options, catalogs, ScenarioCheck.PhraseFiles(config), errors));
                    diagnostics.AddRange(UatScenarioCheck.CheckConfigUsings(catalogs, ScenarioCheck.PhraseFiles(config), config)
                        .Select(error => new TestDataDiagnostic(config, error.Location.LineNumber, error.Code, null, error.Message)));
                    if (ScenarioCheck.CopyWarning(config) is { } copy)
                    {
                        errors.WriteLine($"warning: {copy}");
                    }
                }

                diagnostics.AddRange(CheckScenarios(options, catalogs, files, output, errors));
                scenarios = files.Count;
            }
            else if (scenarioFile is not null)
            {
                throw new ToolException($"To check '{options.Argument}', run from a UAT project folder whose {ConfigFileName} names its Fixture and a Pages assembly.");
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

            List<string> checkedParts = [];
            if (testSets > 0)
            {
                checkedParts.Add($"{testSets} test set(s)");
            }

            if (scenarios > 0)
            {
                checkedParts.Add($"{scenarios} scenario file(s)");
            }

            output.WriteLine(diagnostics.Count == 0
                ? $"No problems in {string.Join(" and ", checkedParts)}."
                : $"{diagnostics.Count} error(s).");
        }

        return diagnostics.Count == 0 ? 0 : 1;
    }

    // A file named or headed in the form before feature files: --fix renames it and rewrites its
    // headings, otherwise each old name is an error (old headings are the parser's).
    private static IEnumerable<TestDataDiagnostic> CheckOldForms(ToolOptions options, string config, TextWriter output)
    {
        var folder = ScenarioCheck.ScenarioFolder(config);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var old = UatFileFormat.OldFilePatterns
            .SelectMany(pattern => Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (!options.Fix)
        {
            return old.Select(file => new TestDataDiagnostic(
                file, 1, UatFileFormat.OldFormCode, null,
                $"'{Path.GetFileName(file)}' is the old name of '{Path.GetFileName(UatFileFormat.NewFileName(file))}'. Run 'brinell-data validate --fix' to rename it."));
        }

        foreach (var file in old)
        {
            var renamed = UatFileFormat.NewFileName(file)!;
            File.Move(file, renamed);
            output.WriteLine($"Renamed {Display(options, file)} to {Path.GetFileName(renamed)}.");
        }

        foreach (var file in Directory.EnumerateFiles(folder, UatFileFormat.FeatureFilePattern, SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(folder, UatFileFormat.StepFilePattern, SearchOption.AllDirectories)))
        {
            var (text, count) = UatFileFormat.RewriteHeadings(File.ReadAllText(file));
            if (count > 0)
            {
                File.WriteAllText(file, text);
                output.WriteLine($"Fixed {count} heading(s) in {Display(options, file)}.");
            }
        }

        return [];
    }

    // Every problem of the scenario files, the same checks the xUnit gate runs; --fix first rewrites
    // what has one right answer, then checks the file again.
    private static IEnumerable<TestDataDiagnostic> CheckScenarios(ToolOptions options, UatCatalogSet catalogs, IReadOnlyList<string> files, TextWriter output, TextWriter errors)
    {
        foreach (var file in files)
        {
            var found = UatScenarioCheck.CheckFile(file, catalogs);
            if (options.Fix)
            {
                var (text, count) = UatScenarioCheck.ApplyFixes(File.ReadAllText(file), found);
                if (count > 0)
                {
                    File.WriteAllText(file, text);
                    output.WriteLine($"Fixed {count} value(s) in {Display(options, file)}.");
                    found = UatScenarioCheck.CheckFile(file, catalogs);
                }
            }

            foreach (var warning in found.Where(d => d.Severity == UatDiagnosticSeverity.Warning))
            {
                errors.WriteLine($"warning: {Display(options, warning.Location.FilePath ?? file)}:{warning.Location.LineNumber}: {warning.Code} {warning.Message}");
            }

            foreach (var error in found.Where(d => d.Severity == UatDiagnosticSeverity.Error))
            {
                yield return new TestDataDiagnostic(error.Location.FilePath ?? file, error.Location.LineNumber, error.Code, null, error.Message);
            }
        }
    }

    private static IEnumerable<TestDataDiagnostic> CheckPhraseFiles(ToolOptions options, UatCatalogSet catalogs, IReadOnlyList<string> files, TextWriter errors)
    {
        foreach (var file in files)
        {
            var found = UatScenarioCheck.CheckPhraseFile(file, catalogs);
            foreach (var warning in found.Where(d => d.Severity == UatDiagnosticSeverity.Warning))
            {
                errors.WriteLine($"warning: {Display(options, warning.Location.FilePath ?? file)}:{warning.Location.LineNumber}: {warning.Code} {warning.Message}");
            }

            foreach (var error in found.Where(d => d.Severity == UatDiagnosticSeverity.Error))
            {
                yield return new TestDataDiagnostic(error.Location.FilePath ?? file, error.Location.LineNumber, error.Code, null, error.Message);
            }
        }
    }

    private static int Phrases(ToolOptions options, TextWriter output)
    {
        var config = options.ConfigPath ?? throw new ToolException($"No {ConfigFileName} here. Run from a UAT project folder, or pass --config.");
        var catalogs = ScenarioCheck.Catalogs(config);
        var catalog = catalogs.Default;
        var callables = new UatCallableSet(ScenarioCheck.PhraseFiles(config)
                .SelectMany(file => UatScenarioFiles.ParsePhraseFile(file).Document?.Callables ?? []))
            .Prepare(catalogs)
            .All;
        UatEffectiveStepKeyword? keyword = options.Keyword is { } name
            ? Enum.TryParse<UatEffectiveStepKeyword>(name, ignoreCase: true, out var parsed) ? parsed : throw new ToolException($"'{name}' is not Given, When or Then.")
            : null;

        // For one scenario file: only what it sees, grouped by where it comes from.
        if (options.Argument is { } file)
        {
            foreach (var line in UatScenarioCheck.DescribeVisible(Existing(options, file), catalogs, keyword))
            {
                output.WriteLine(line);
            }

            return 0;
        }

        if (options.Json)
        {
            output.WriteLine(JsonSerializer.Serialize(
                catalog.Patterns.Where(p => keyword is null || p.Keyword == keyword).Select(p => new
                {
                    keyword = p.Keyword.ToString(),
                    phrase = p.Phrase,
                    parameters = p.Parameters.Select(parameter => new { parameter.Name, type = UatValues.Describe(parameter.Type), source = parameter.Source.ToString() }),
                    declaration = p.CommandId,
                    problem = p.Problem is { } problem ? $"{problem.Code} {problem.Message}" : null,
                }).Concat(callables.Select(c => new
                {
                    // A callable has no keyword: it answers Given, When and Then alike.
                    keyword = "Any",
                    phrase = c.Callable.Phrase,
                    parameters = c.Types.Select(parameter => new { Name = parameter.Key, type = UatValues.Describe(parameter.Value), source = c.Columns.Contains(parameter.Key) ? "Column" : "Phrase" }),
                    declaration = c.Callable.Source.ToString(),
                    problem = c.Problem is { } problem ? $"{problem.Code} {problem.Message}" : null,
                })),
                OutputJson));
            return 0;
        }

        foreach (var line in UatScenarioCheck.DescribePhrases(catalog, keyword).Concat(UatScenarioCheck.DescribeCallables(callables)))
        {
            output.WriteLine(line);
        }

        return 0;
    }

    // Who depends on a namespace: the config's ## Using, the files that name it in their Using row,
    // and the files in its folder, which see it without saying so.
    private static int References(ToolOptions options, TextWriter output)
    {
        var config = options.ConfigPath ?? throw new ToolException($"No {ConfigFileName} here. Run from a UAT project folder, or pass --config.");
        var name = UatNamespaces.Normalize(options.Argument ?? throw new ToolException("references needs a namespace, e.g. 'brinell-data references Backend'."));
        var found = 0;
        if (UatConfigParser.ParseFile(config).Usings.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            output.WriteLine($"{Display(options, config)}: ## Using");
            found++;
        }

        foreach (var file in ScenarioCheck.Files(config))
        {
            var usings = UatNamespaces.ParseList(UatMarkdownParser.ParseFile(file).Document?.Metadata.GetValueOrDefault(UatMetadataFields.Using));
            var reason = usings.Contains(name, StringComparer.OrdinalIgnoreCase) ? "Using"
                : UatNamespaces.IsVisible(name, UatNamespaces.Of(file)) && UatNamespaces.Normalize(UatNamespaces.Of(file)).Length > 0 ? "in the folder"
                : null;
            if (reason is not null)
            {
                output.WriteLine($"{Display(options, file)}: {reason}");
                found++;
            }
        }

        output.WriteLine(found == 0 ? $"Nothing uses {name}." : $"{found} use(s) of {name}.");
        return 0;
    }

    private static int Snippet(ToolOptions options, TextWriter output)
    {
        var target = options.Argument ?? throw new ToolException("snippet needs a step, e.g. 'brinell-data snippet Scenarios/TOD.01.feature.md:14'.");
        var colon = target.LastIndexOf(':');
        if (colon <= 1 || !int.TryParse(target[(colon + 1)..], out var line))
        {
            throw new ToolException($"'{target}' is not <file>:<line>.");
        }

        var parse = UatMarkdownParser.ParseFile(Existing(options, target[..colon]));
        var step = parse.Document is { } document
            ? document.Background.Concat(document.Scenarios.SelectMany(scenario => scenario.Steps)).FirstOrDefault(candidate => candidate.Source.LineNumber == line)
            : null;
        output.WriteLine(UatScenarioCheck.Snippet(step ?? throw new ToolException($"There is no step on line {line} of {target[..colon]}.")));
        return 0;
    }

    private static bool HasTestData(ToolOptions options)
        => options.ConfigPath is { } config && UatConfigParser.Parse(File.ReadAllText(config)).Assemblies
            .Any(a => string.Equals(a.Kind, UatConfigFields.TestDataAssemblyKind, StringComparison.OrdinalIgnoreCase));

    private static string Existing(ToolOptions options, string file)
    {
        var path = Path.GetFullPath(Path.Combine(options.CurrentDirectory, file));
        return File.Exists(path) ? path : throw new ToolException($"'{file}' does not exist.");
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
        var assemblies = UatConfigParser.Parse(File.ReadAllText(config)).Assemblies
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
            brinell-data - check an app's Brinell test data and scenario files

            Usage (from a UAT project folder with a uat.config.md, or with --assembly):
              brinell-data validate [file] [--format json] [--fix]
                                                             check every test set and scenario, or one file;
                                                             --fix rewrites values with one right answer
              brinell-data phrases [file] [--keyword When] [--format json]
                                                             the phrases steps can use, with argument types;
                                                             with a scenario file, only those it sees, by namespace
              brinell-data references <namespace>            the config and files that use a namespace
              brinell-data snippet <file>:<line>             a method for a step that binds to nothing
              brinell-data describe <Type>                   the fields of a test-data type
              brinell-data list                              the test sets and what uses them
              brinell-data schema [--check]                  write (or check) testdata.schema.json

            Options:
              --assembly <path>   the assembly with the TestDataDefinition (repeatable)
              --config <path>     the config to read (default: ./uat.config.md or ./bdd.config.md)
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
    public bool Fix { get; init; }

    public string? Keyword { get; init; }

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
        var fix = false;
        string? keyword = null;

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
                case "--fix":
                    fix = true;
                    break;
                case "--keyword" when i + 1 < args.Length:
                    keyword = args[++i];
                    break;
                case var value when value.StartsWith("--", StringComparison.Ordinal):
                    throw new ToolException($"Unknown option '{value}'.");
                default:
                    argument ??= args[i];
                    break;
            }
        }

        config ??= UatFileFormat.FindConfig(currentDirectory);
        return new ToolOptions(args[0], argument, assemblies, File.Exists(config) ? config : null, json, check, currentDirectory) { Fix = fix, Keyword = keyword };
    }
}
