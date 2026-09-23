# Step 3.1 — Integrate Microsoft.Extensions.Logging with ILogger<T>

## Objective

Wire up `ILogger<T>` via DI so every service and ViewModel gets structured logging.

## Dependencies

- Phase 1 (project + DI container)
- NuGet: `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Logging.Debug`

## Implementation

Register `ILoggerFactory` in the DI container (`App.xaml.cs`):

```csharp
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddDebug();                         // Output window
    builder.AddProvider(new InAppLogProvider()); // In-app viewer (step 3.3)
    // File sink added in step 3.2
});
```

Inject `ILogger<T>` into every service and ViewModel via constructor:

```csharp
public class BrowserViewModel : ViewModelBase
{
    private readonly ILogger<BrowserViewModel> _logger;

    public BrowserViewModel(ILogger<BrowserViewModel> logger)
    {
        _logger = logger;
    }
}
```

Use structured logging with message templates (not string interpolation):

```csharp
_logger.LogInformation("Navigating to {Url}", url);
_logger.LogError(ex, "Navigation failed for {Url}", url);
```

## Checklist

- [ ] `AddLogging()` registered in DI container
- [ ] All ViewModels accept `ILogger<T>` via constructor
- [ ] Structured message templates used (no string interpolation)
- [ ] Debug output sink active
