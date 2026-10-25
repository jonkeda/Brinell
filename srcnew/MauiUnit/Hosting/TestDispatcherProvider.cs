using Microsoft.Maui.Dispatching;

namespace MauiUnit.Hosting;

/// <summary>
/// MAUI's dispatcher provider for the test process: the dispatcher of the context whose work runs on
/// this thread, else the context this async flow created, else one that runs work at once.
/// </summary>
/// <remarks>
/// <see cref="DispatcherProvider"/> is process-wide, so it is installed once and asks per call; tests
/// in parallel each get their own context's dispatcher.
/// </remarks>
internal sealed class TestDispatcherProvider : IDispatcherProvider
{
    private static readonly object Gate = new();
    private static readonly AsyncLocal<TestDispatcher?> Ambient = new();
    private static bool s_installed;

    public IDispatcher? GetForCurrentThread()
        => (IDispatcher?)TestDispatcher.Running ?? (IDispatcher?)Ambient.Value ?? ImmediateDispatcher.Instance;

    /// <summary>Installs the provider, once per process; a later call only sets this flow's dispatcher.</summary>
    public static void Install(TestDispatcher ambient)
    {
        lock (Gate)
        {
            if (!s_installed)
            {
                DispatcherProvider.SetCurrent(new TestDispatcherProvider());
                s_installed = true;
            }
        }

        Ambient.Value = ambient;
    }

    /// <summary>For elements made outside any context: work runs where it is dispatched.</summary>
    private sealed class ImmediateDispatcher : IDispatcher
    {
        public static readonly ImmediateDispatcher Instance = new();

        public bool IsDispatchRequired => false;

        public bool Dispatch(Action action)
        {
            action();
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action)
            => throw new InvalidOperationException(
                "Delayed work needs a MauiTestContext: its virtual clock runs it on AdvanceTime.");

        public IDispatcherTimer CreateTimer()
            => throw new InvalidOperationException(
                "Timers need a MauiTestContext: its virtual clock ticks them on AdvanceTime.");
    }
}
