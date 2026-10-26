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
    /// <param name="whileWaiting">
    /// Run between looks at the answer. In memory (MauiUnit) the app's main thread is the test's own
    /// and runs only when the test lets it, so the wait lets it; on a device the app runs by itself.
    /// </param>
    public void Request(Action? whileWaiting = null)
    {
        var request = $"reset {Environment.ProcessId}-{Interlocked.Increment(ref _requests)}";
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, request);
        Replace(temporary);

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

            whileWaiting?.Invoke();
            Thread.Sleep(Poll);
        }

        throw new TimeoutException($"The app did not answer the reset '{request}' within {AnswerTimeout.TotalSeconds} s; it last said '{TryRead(done) ?? "nothing"}' ({done}).");
    }

    // The app polls the reset file, and Windows refuses to replace a file another handle has open: a
    // read lasts microseconds, so the move is tried again a few times before the refusal stands.
    private void Replace(string temporary)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temporary, Path, overwrite: true);
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException && attempt < 20)
            {
                Thread.Sleep(10);
            }
        }
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
