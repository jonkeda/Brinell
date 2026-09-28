using Brinell.Bdd.LanguageServer;

// Served over stdio by default. With --preview, renders one file as a page, for a look without an IDE:
//   Brinell.Bdd.LanguageServer --preview <file> [--solution <folder>] [--suite <name>] [--out <page.html>]
if (args.Length > 0 && args[0] == "--preview")
{
    return PreviewCommand.Run(args[1..], Console.Out, Console.Error);
}

// With --mcp, the Brinell MCP server for Copilot and other agents, over stdio:
//   Brinell.Bdd.LanguageServer --mcp [--solution <folder>]   (the current folder by default)
if (args.Length > 0 && args[0] == "--mcp")
{
    var solution = args.Length > 2 && args[1] == "--solution" ? args[2] : Environment.CurrentDirectory;
    // The protocol owns stdout: anything else written to the console goes to stderr.
    var protocol = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
    Console.SetOut(Console.Error);
    using var mcp = new BddMcpServer(solution);
    await mcp.RunAsync(Console.In, protocol);
    return 0;
}

using var server = new BddLanguageServer();
using var rpc = server.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput());
var exit = new TaskCompletionSource();
server.ExitRequested += (_, _) => exit.TrySetResult();
await Task.WhenAny(rpc.Completion, exit.Task);
return 0;
