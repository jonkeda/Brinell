using System.IO.Pipes;
using Bruinell.Bdd.Runner;

namespace Bruinell.Uat.Runner;

/// <summary>
/// The runner process's entry point. A host starts it in a suite's output folder with the suite's
/// own <c>runtimeconfig.json</c> and <c>deps.json</c> (<see cref="BddRunnerStart"/>), so a
/// scenario runs there as it does under <c>dotnet test</c>. See AD-025.
/// </summary>
public static class BddRunnerProgram
{
    /// <summary>Connects to the host's pipe and answers it until it says exit.</summary>
    /// <param name="args"><c>--pipe &lt;name&gt;</c>.</param>
    /// <returns>0 when the host ended the run; 2 when started without a pipe.</returns>
    public static async Task<int> Main(string[] args)
    {
        var at = Array.IndexOf(args, BddRunnerProtocol.PipeArgument);
        if (at < 0 || at + 1 >= args.Length)
        {
            Console.Error.WriteLine($"Bruinell's scenario runner is started by a host (Presenter): {BddRunnerProtocol.PipeArgument} <name>.");
            return 2;
        }

        var pipe = new NamedPipeClientStream(".", args[at + 1], PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync((int)BddRunnerClient.ConnectTimeout.TotalMilliseconds).ConfigureAwait(false);
        using var channel = new BddRunnerChannel(pipe);
        await new BddRunnerHost(channel).RunAsync().ConfigureAwait(false);
        return 0;
    }
}
