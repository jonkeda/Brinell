using Bruinell.Samples.Todo.Infrastructure;

namespace Bruinell.Samples.Todo.UITests;

/// <summary>
/// The test side of <see cref="TodoTestReset"/>: writes a request into the app's reset file and
/// waits until the app says it handled it, having started over on its database as it is.
/// </summary>
/// <param name="path">The reset file the app was launched with.</param>
public sealed class TodoResetFile(string path)
{
    // How long the app gets to reload its list; it takes a moment, not seconds.
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(50);
    private int _requests;

    /// <summary>The reset file.</summary>
    public string Path { get; } = path;

    /// <summary>Asks the app to start over and waits for its answer.</summary>
    /// <exception cref="TimeoutException">The app did not answer, or failed: the message says which.</exception>
    public void Request()
    {
        var request = $"reset {Environment.ProcessId}-{Interlocked.Increment(ref _requests)}";
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, request);
        File.Move(temporary, Path, overwrite: true);

        var done = TodoTestReset.DoneFileOf(Path);
        var deadline = DateTime.UtcNow + AnswerTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var answer = TryRead(done);
            if (answer == request)
            {
                return;
            }

            if (answer?.StartsWith(request + " failed", StringComparison.Ordinal) == true)
            {
                throw new TimeoutException($"The app could not start over: {answer}");
            }

            Thread.Sleep(Poll);
        }

        throw new TimeoutException($"The app did not answer the reset '{request}' within {AnswerTimeout.TotalSeconds} s; it last said '{TryRead(done) ?? "nothing"}' ({done}).");
    }

    private static string? TryRead(string file)
    {
        try
        {
            return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
        }
        catch (IOException)
        {
            // Being replaced just now: read again.
            return null;
        }
    }
}
