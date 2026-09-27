using Brinell.Uat.LanguageServer;

// Served over stdio by default. With --preview, renders one file as a page, for a look without an IDE:
//   Brinell.Uat.LanguageServer --preview <file> [--solution <folder>] [--suite <name>] [--out <page.html>]
if (args.Length > 0 && args[0] == "--preview")
{
    return PreviewCommand.Run(args[1..], Console.Out, Console.Error);
}

using var server = new UatLanguageServer();
using var rpc = server.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput());
var exit = new TaskCompletionSource();
server.ExitRequested += (_, _) => exit.TrySetResult();
await Task.WhenAny(rpc.Completion, exit.Task);
return 0;
