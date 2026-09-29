using System.Text.Json;
using System.Text.Json.Nodes;
using Brinell.Bdd.Workspace;

namespace Brinell.Bdd.LanguageServer;

/// <summary>
/// The Brinell MCP server (phase 4, Copilot): the same engine as the language server, as tools an
/// agent calls. It gives the suites, their steps, pages and test sets, and checks a scenario draft
/// with the gate's own words, so an agent writes steps that exist rather than inventing them. It
/// holds data and checks; it calls no model. MCP over stdio: one JSON-RPC message per line.
/// </summary>
public sealed class BddMcpServer : IDisposable
{
    private const string ProtocolVersion = "2025-06-18";

    // Readable text for an agent: quotes and non-ASCII kept as they are, not \u escapes.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly BddLanguageServer _server = new();
    private readonly string _solutionDir;

    /// <summary>A server for the suites under <paramref name="solutionDir"/>.</summary>
    public BddMcpServer(string solutionDir)
    {
        _solutionDir = Path.GetFullPath(solutionDir);
        _server.Initialize(new InitializeParams(null, null, new InitializationOptions(_solutionDir)));
    }

    /// <summary>The tools, as <c>tools/list</c> gives them.</summary>
    public static IReadOnlyList<(string Name, string Description, JsonObject Schema)> Tools { get; } =
    [
        ("list_suites", "The UAT and BDD suites of the solution: name, head, config, and whether it is built.", Schema()),
        ("list_steps", "The steps a suite's scenarios can use: keyword, phrase with {placeholders} and their types, and where it comes from (class, page, namespace, callable step). Use only these phrases in a scenario. 'query' keeps the steps holding every word of it.", Schema(("suite", "string", "A suite name; the first suite when left out."), ("query", "string", "Words the phrase must hold."))),
        ("list_pages", "A UI suite's pages and their controls: the names {page} and {control} take.", Schema(("suite", "string", "A suite name; the first suite when left out."))),
        ("list_test_sets", "A suite's test sets: the values of a feature file's TestData metadata.", Schema(("suite", "string", "A suite name; the first suite when left out."))),
        ("validate_scenario", "Checks a feature or step file as the xUnit gate would, with its words: every step must bind. Give 'path' (where the file is, or will be, in a suite's Features folder) and 'text' for a draft; without 'text' the file on disk is checked.", Schema(("path", "string", "The file's path; required."), ("text", "string", "The draft's text; the file on disk when left out."))),
        ("find_step_usages", "The steps across every suite that use a C# member: a step definition, a page phrase or a page object's control, as Namespace.Type.Member.", Schema(("symbol", "string", "Namespace.Type.Member; required."))),
        ("delete_results", "Deletes old scenario results from every run, for good: of a scenario (the heading at 'line' of 'path'), a feature file ('path') or a suite ('suite'). Only scenario reports and their screenshots; a run left empty goes. Counts only (dryRun) unless 'dryRun' is false.", Schema(("scope", "string", "scenario, file or suite; required."), ("path", "string", "The feature file, for a scenario or a file."), ("line", "integer", "The scenario heading's line, for a scenario."), ("suite", "string", "The suite, for a suite (or the one a linked file is picked in)."), ("dryRun", "boolean", "Only count; true when left out."))),
    ];

    /// <summary>Serves until the input ends.</summary>
    public async Task RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken = default)
    {
        while (await input.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }

            if (Handle(line) is { } response)
            {
                await output.WriteLineAsync(response.ToJsonString()).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>One message in, its response out; null for a notification.</summary>
    public JsonObject? Handle(string message)
    {
        JsonObject request;
        try
        {
            request = JsonNode.Parse(message)?.AsObject() ?? throw new JsonException("Not an object.");
        }
        catch (JsonException exception)
        {
            return Error(null, -32700, $"Not JSON: {exception.Message}");
        }

        var id = request["id"]?.DeepClone();
        var method = (string?)request["method"];
        if (id is null)
        {
            return null;
        }

        try
        {
            JsonNode result = method switch
            {
                "initialize" => new JsonObject
                {
                    ["protocolVersion"] = ProtocolVersion,
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                    ["serverInfo"] = new JsonObject { ["name"] = "brinell-bdd", ["version"] = typeof(BddMcpServer).Assembly.GetName().Version?.ToString() ?? "0" },
                    ["instructions"] = "Brinell BDD: use list_steps before writing a scenario step, and validate_scenario before proposing a feature file. Never invent a phrase: leave a step that has none as a marked gap.",
                },
                "ping" => new JsonObject(),
                "tools/list" => new JsonObject
                {
                    ["tools"] = new JsonArray([.. Tools.Select(tool => (JsonNode)new JsonObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        ["inputSchema"] = tool.Schema.DeepClone(),
                    })]),
                },
                "tools/call" => Call((string?)request["params"]?["name"] ?? string.Empty, request["params"]?["arguments"] as JsonObject ?? []),
                _ => throw new McpException(-32601, $"No method '{method}'."),
            };
            return new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result };
        }
        catch (McpException exception)
        {
            return Error(id, exception.Code, exception.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _server.Dispose();

    // A tool's answer is text: JSON an agent reads, or the reason it could not answer (isError).
    private JsonObject Call(string name, JsonObject arguments)
    {
        try
        {
            object answer = name switch
            {
                "list_suites" => ListSuites(),
                "list_steps" => ListSteps(Suite(arguments), (string?)arguments["query"]),
                "list_pages" => ListPages(Suite(arguments)),
                "list_test_sets" => Suite(arguments).TestDataDefinitions().SelectMany(definition => definition.TestSetNames).Distinct().ToList(),
                "validate_scenario" => Validate(Required(arguments, "path"), (string?)arguments["text"]),
                "find_step_usages" => _server.Usages(new UsagesParams(Required(arguments, "symbol"))),
                "delete_results" => DeleteResults(arguments),
                _ => throw new McpException(-32602, $"No tool '{name}'. The tools are {string.Join(", ", Tools.Select(tool => tool.Name))}."),
            };
            return Text(JsonSerializer.Serialize(answer, Json), error: false);
        }
        catch (ToolProblem problem)
        {
            return Text(problem.Message, error: true);
        }
    }

    // An agent is not asked first, so it only counts unless it says dryRun: false.
    private DeleteResultsResult DeleteResults(JsonObject arguments)
    {
        var path = (string?)arguments["path"];
        var result = _server.DeleteResults(new DeleteResultsParams(
            Required(arguments, "scope"),
            path is null ? null : new Uri(Path.GetFullPath(path, _solutionDir)).AbsoluteUri,
            (int?)arguments["line"],
            (string?)arguments["suite"],
            (bool?)arguments["dryRun"] ?? true));
        return result.Done ? result : throw new ToolProblem(result.Reason ?? "Nothing to delete.");
    }

    private object ListSuites()
        => _server.Explorer(new ExplorerParams(null, null)).Suites
            .Select(name => SuiteNamed(name))
            .Select(suite => new
            {
                suite.Name,
                Head = TryHead(suite),
                Config = suite.ConfigPath,
                Built = suite.TryGetCatalogs(out var reason) is not null,
                Reason = reason,
            })
            .ToList();

    private static object ListSteps(BddSuite suite, string? query)
    {
        var catalogs = Catalogs(suite);
        var words = (query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var steps = catalogs.Default.Patterns
            .Where(pattern => pattern.Callable is null)
            .Select(pattern => new
            {
                Keyword = pattern.Keyword.ToString(),
                pattern.Phrase,
                Parameters = pattern.Parameters.Select(parameter => parameter.ToString()).ToList(),
                From = pattern.ControlVerb is not null ? "control verb, on the current page"
                    : pattern.Page is { } page ? $"page {page}"
                    : pattern.Namespace.Length > 0 ? $"namespace {pattern.Namespace} (the file must use it)"
                    : pattern.CommandId,
            })
            .Concat(suite.StepFiles.SelectMany(path => BddScenarioFiles.ParsePhraseFile(path).Document?.Callables ?? []).Select(callable => new
            {
                Keyword = "Any",
                callable.Phrase,
                Parameters = callable.Declared.Select(parameter => $"{{{parameter.Name}: {parameter.TypeName}}}").ToList(),
                From = $"{callable} in {Path.GetFileName(callable.Source.FilePath)} (a feature file under that folder sees it)",
            }))
            .Where(step => words.All(word => step.Phrase.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(step => step.Keyword)
            .ThenBy(step => step.Phrase, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new { Suite = suite.Name, Count = steps.Count, Steps = steps };
    }

    private static object ListPages(BddSuite suite)
        => UatPageSet.Of(Catalogs(suite).Default).Values
            .OrderBy(page => page.Key, StringComparer.OrdinalIgnoreCase)
            .Select(page => new
            {
                Page = page.Key,
                Class = page.PageType.FullName,
                Controls = page.Controls.Select(control => new { control.Name, Type = control.ControlType.Name, control.NavigatesTo }).ToList(),
            })
            .ToList();

    // The draft checked in the suite that holds its path, as the editor checks unsaved text.
    private object Validate(string path, string? text)
    {
        var full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(_solutionDir, path));
        if (text is null && !File.Exists(full))
        {
            throw new ToolProblem($"'{full}' does not exist; give its text.");
        }

        var uri = new Uri(full).AbsoluteUri;
        _server.DidOpen(new DidOpenTextDocumentParams(new TextDocumentItem(uri, "markdown", 1, text ?? File.ReadAllText(full))));
        try
        {
            var file = _server.Context(full);
            var diagnostics = _server.DiagnosticsOf(file);
            return new
            {
                File = full,
                Suite = file.Suite?.Name,
                Bound = file.Catalogs is not null,
                file.Reason,
                Valid = diagnostics.All(diagnostic => diagnostic.Severity != 1) && file.Catalogs is not null,
                Problems = diagnostics.Select(diagnostic => new { Line = diagnostic.Range.Start.Line + 1, Severity = diagnostic.Severity == 1 ? "error" : "warning", diagnostic.Code, diagnostic.Message }).ToList(),
            };
        }
        finally
        {
            _server.DidClose(new DidCloseTextDocumentParams(new TextDocumentIdentifier(uri)));
        }
    }

    private BddSuite Suite(JsonObject arguments)
    {
        var name = (string?)arguments["suite"];
        var names = _server.Explorer(new ExplorerParams(null, null)).Suites;
        if (names.Count == 0)
        {
            throw new ToolProblem($"No suite under {_solutionDir}: no project has a uat.config.md or bdd.config.md.");
        }

        var found = name is null ? names[0] : names.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
        return found is null
            ? throw new ToolProblem($"No suite '{name}'. The suites are {string.Join(", ", names)}.")
            : SuiteNamed(found);
    }

    private BddSuite SuiteNamed(string name) => _server.SuiteNamed(name) ?? throw new ToolProblem($"No suite '{name}'.");

    private static BddCatalogSet Catalogs(BddSuite suite)
        => suite.TryGetCatalogs(out var reason) ?? throw new ToolProblem($"{suite.Name} is not bound: {reason}");

    private static string? TryHead(BddSuite suite)
    {
        try
        {
            return suite.Head;
        }
        catch (BddWorkspaceException)
        {
            return null;
        }
    }

    private static string Required(JsonObject arguments, string name)
        => (string?)arguments[name] is { Length: > 0 } value ? value : throw new ToolProblem($"'{name}' is required.");

    private static JsonObject Text(string text, bool error)
        => new() { ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text }), ["isError"] = error };

    private static JsonObject Error(JsonNode? id, int code, string message)
        => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JsonObject { ["code"] = code, ["message"] = message } };

    private static JsonObject Schema(params (string Name, string Type, string Description)[] properties)
        => new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject([.. properties.Select(property => new KeyValuePair<string, JsonNode?>(property.Name, new JsonObject { ["type"] = property.Type, ["description"] = property.Description }))]),
        };

    private sealed class McpException(int code, string message) : Exception(message)
    {
        public int Code { get; } = code;
    }

    // A tool that cannot answer says why, as its result: the agent reads it and can act on it.
    private sealed class ToolProblem(string message) : Exception(message);
}
