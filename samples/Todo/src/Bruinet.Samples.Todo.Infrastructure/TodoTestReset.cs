using Bruinet.Samples.Todo.Core.Sync;
using Bruinet.Samples.Todo.Core.ViewModels;

namespace Bruinet.Samples.Todo.Infrastructure;

/// <summary>
/// The app side of a test's reset without a restart: when the reset file is written, the app
/// forgets what it holds in memory and shows its database as a fresh launch does, then writes the
/// request it handled to the file's <c>.done</c> beside it, which the test waits for.
/// </summary>
/// <remarks>
/// <para>
/// The test has put the database back first: the app opens a connection per use, so a test process
/// can rewrite the same SQLite file under it. What a restart also resets - the last sync, the list's
/// error and filter - is this class's. A warm runner keeps the app open between runs with it (AD-025).
/// </para>
/// <para>
/// Only used when the launch settings name a file (<see cref="LaunchSettings.ResetFile"/>), which only a
/// test run does, as for the network-state file.
/// </para>
/// </remarks>
public sealed class TodoTestReset : IDisposable
{
    private readonly string? _path;
    private readonly ISyncService _sync;
    private readonly TodoListViewModel _list;
    // How often the file is read: a file-system watcher misses some writes of a file replaced in place.
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(100);

    private readonly Action<Action> _dispatch;
    private Timer? _timer;
    private string? _handled;

    /// <summary>Creates the reset for the launch's file; it waits for <see cref="Start"/>.</summary>
    public TodoTestReset(LaunchSettings settings, ISyncService sync, TodoListViewModel list, Action<Action>? dispatch = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _path = settings.ResetFile is { } file ? Path.GetFullPath(file) : null;
        _sync = sync;
        _list = list;
        _dispatch = dispatch ?? (action => action());
    }

    /// <summary>The file a request is acknowledged in.</summary>
    public static string DoneFileOf(string resetFile) => resetFile + ".done";

    /// <summary>Starts reading the reset file; nothing without one. A request written before the app started is not one for it.</summary>
    public void Start()
    {
        if (_path is null || _timer is not null)
        {
            return;
        }

        _handled = TryRead();
        _timer = new Timer(_ => Check(), null, Poll, Poll);
    }

    /// <summary>Starts over now: what a request does, on the caller's thread.</summary>
    public async Task ResetAsync()
    {
        _sync.Forget();
        await _list.StartOverAsync().ConfigureAwait(true);
    }

    /// <inheritdoc />
    public void Dispose() => _timer?.Dispose();

    private string? TryRead()
    {
        try
        {
            return File.Exists(_path) ? File.ReadAllText(_path!).Trim() : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Being replaced: the next look reads it.
            return null;
        }
    }

    // A request is the file's text; one already handled is not done again.
    private void Check()
    {
        if (TryRead() is not { Length: > 0 } request || request == Interlocked.Exchange(ref _handled, request))
        {
            return;
        }

        // Said at once, so a test that times out knows whether the app got the request.
        Acknowledge($"received {request}");
        _dispatch(async () =>
        {
            Acknowledge($"started {request}");
            string answer;
            try
            {
                await ResetAsync().ConfigureAwait(true);
                answer = request;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                answer = $"{request} failed: {exception.Message}";
            }

            Acknowledge(answer);
        });
    }

    private void Acknowledge(string answer)
    {
        var done = DoneFileOf(_path!);
        var temporary = done + ".tmp";

        // The test polls the done file, and Windows refuses to replace a file another handle has open:
        // a read lasts microseconds, so the move is tried again a few times. Dropped, the answer left
        // the test waiting out its timeout on a reset that had finished.
        for (var attempt = 1; attempt <= 20; attempt++)
        {
            try
            {
                File.WriteAllText(temporary, answer);
                File.Move(temporary, done, overwrite: true);
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Still refused after the last try: the test times out waiting, and says the app did not answer.
                Thread.Sleep(10);
            }
        }
    }
}
