using System.Collections.Concurrent;

namespace Brinell.Scraper.Tests.TestHelpers;

/// <summary>
/// Provides a single persistent STA thread for WPF-dependent tests.
/// Use with <see cref="Xunit.IClassFixture{T}"/> to share the same
/// STA thread across all tests in a class (required when static WPF
/// objects like Brush have thread affinity).
/// </summary>
public sealed class StaThreadFixture : IDisposable
{
    private readonly BlockingCollection<(Action Action, TaskCompletionSource Tcs)> _queue = new();
    private readonly Thread _thread;

    public StaThreadFixture()
    {
        _thread = new Thread(ProcessQueue);
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.IsBackground = true;
        _thread.Start();
    }

    public void Run(Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add((action, tcs));
        tcs.Task.GetAwaiter().GetResult();
    }

    private void ProcessQueue()
    {
        foreach (var (action, tcs) in _queue.GetConsumingEnumerable())
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(5));
    }
}
