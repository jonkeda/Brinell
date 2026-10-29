using Microsoft.Maui.Dispatching;

namespace MauiHeadless.Hosting;

/// <summary>A dispatcher timer on the virtual clock: it ticks only when the test advances time.</summary>
internal sealed class TestDispatcherTimer(TestDispatcher dispatcher) : IDispatcherTimer
{
    // A zero interval would tick forever within one advance.
    private static readonly TimeSpan Shortest = TimeSpan.FromMilliseconds(1);

    private object? _next;

    public TimeSpan Interval { get; set; }

    public bool IsRepeating { get; set; } = true;

    public bool IsRunning { get; private set; }

    public event EventHandler? Tick;

    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        ScheduleNext();
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        if (_next is not null)
            dispatcher.Cancel(_next);
        _next = null;
    }

    private void ScheduleNext()
        => _next = dispatcher.Schedule(Interval < Shortest ? Shortest : Interval, OnDue);

    private void OnDue()
    {
        _next = null;
        if (!IsRunning)
            return;

        if (IsRepeating)
            ScheduleNext();
        else
            IsRunning = false;

        Tick?.Invoke(this, EventArgs.Empty);
    }
}
