namespace ClaudeSwitcher.Mvvm;

[Flags]
public enum AsyncRelayCommandOptions
{
    None = 0,
    AllowConcurrentExecutions = 1 << 0,
    FlowExceptionsToTaskScheduler = 1 << 1,
    OnceOnly = 1 << 2,
    SkipBusyTracking = 1 << 3,
}
