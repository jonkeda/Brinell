using System.Text.Json.Nodes;
using Xunit;

namespace Brinell.Bdd.LanguageServer.Tests;

/// <summary>
/// The Brinell MCP server over the Todo sample, one JSON-RPC line at a time as an agent sends them.
/// Needs the Todo sample built (<c>dotnet build samples/Todo/Brinell.Samples.Todo.slnx</c>).
/// </summary>
public sealed class McpServerTests : IDisposable
{
    private readonly BddMcpServer _server = new(TodoPaths.Todo);

    public void Dispose() => _server.Dispose();

    [Fact]
    public async Task ASession_Initializes_ListsItsTools_AndAnswersOneLinePerRequest()
    {
        var input = new StringReader(string.Join('\n',
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}""",
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""));
        var output = new StringWriter();

        await _server.RunAsync(input, output);

        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => JsonNode.Parse(line)!).ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal("brinell-uat", (string?)lines[0]["result"]!["serverInfo"]!["name"]);
        Assert.NotNull(lines[0]["result"]!["capabilities"]!["tools"]);
        Assert.Equal(
            ["list_suites", "list_steps", "list_pages", "list_test_sets", "validate_scenario", "find_step_usages"],
            lines[1]["result"]!["tools"]!.AsArray().Select(tool => (string?)tool!["name"]));
    }

    [Fact]
    public void ListSteps_GivesThePhrases_HoldingEveryWordOfTheQuery()
    {
        var text = CallText("list_steps", new JsonObject { ["suite"] = "Brinell.Samples.Todo.Uat", ["query"] = "tap" });

        Assert.Contains("\"phrase\":\"I tap {control}\"", text);
        Assert.DoesNotContain("\"phrase\":\"I am on the {page} page\"", text);
    }

    [Fact]
    public void ListPages_GivesThePagesAndTheirControls()
    {
        var text = CallText("list_pages", new JsonObject { ["suite"] = "Brinell.Samples.Todo.Uat" });

        Assert.Contains("\"page\":\"Todo List\"", text);
        Assert.Contains("\"name\":\"Sync\"", text);
    }

    [Fact]
    public void ValidateScenario_ChecksADraft_WithTheGatesWords()
    {
        var path = Path.Combine(TodoPaths.Project, "Features", "TOD.99-draft.feature.md");

        var text = CallText("validate_scenario", new JsonObject
        {
            ["path"] = path,
            ["text"] = "# Feature: TOD.99 Draft\n\n## Scenario: TOD.99.1 A draft\n\n```gherkin\nGiven I am on the Todo List page\nWhen I polish the list\n```\n",
        });

        Assert.Contains("\"valid\":false", text);
        Assert.Contains("\"line\":7,\"severity\":\"error\",\"code\":\"UATB001\"", text);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void AnUnknownSuite_IsAToolError_SayingWhichSuitesThereAre()
    {
        var result = Call("list_steps", new JsonObject { ["suite"] = "Nope" });

        Assert.True((bool)result["isError"]!);
        Assert.Contains("The suites are", (string?)result["content"]![0]!["text"]);
    }

    private string CallText(string tool, JsonObject arguments)
    {
        var result = Call(tool, arguments);
        Assert.False((bool)result["isError"]!, (string?)result["content"]![0]!["text"]);
        return (string)result["content"]![0]!["text"]!;
    }

    private JsonNode Call(string tool, JsonObject arguments)
        => _server.Handle(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 7,
            ["method"] = "tools/call",
            ["params"] = new JsonObject { ["name"] = tool, ["arguments"] = arguments },
        }.ToJsonString())!["result"]!;
}
