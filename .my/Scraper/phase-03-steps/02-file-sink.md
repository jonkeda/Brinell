# Step 3.2 — File Sink (Rolling Daily JSON)

## Objective

Write structured logs to rolling daily JSON files in `logs/` for diagnostics and post-mortem analysis.

## Dependencies

- Step 3.1 (logging framework registered)
- NuGet: `Serilog.Extensions.Logging`, `Serilog.Sinks.File`

## Implementation

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "scraper-.json"),
        rollingInterval: RollingInterval.Day,
        formatter: new Serilog.Formatting.Json.JsonFormatter())
    .CreateLogger();

builder.AddSerilog();
```

- Rolling daily files: `scraper-20260420.json`
- Structured JSON format — each line is a JSON object with timestamp, level, message, properties
- Log directory: `{app-base}/logs/`
- Retention: keep last 30 days (configurable), auto-cleanup older files

### Alternative: Custom file logger provider

If avoiding Serilog dependency:
- Implement `ILoggerProvider` + `ILogger` that writes to a `StreamWriter`
- Buffer writes and flush periodically or on `Warning`+ level
- Roll files daily based on date check

## Checklist

- [ ] Serilog file sink registered in logging pipeline
- [ ] JSON log files appear in `logs/` folder
- [ ] Files roll daily with date suffix
- [ ] Structured properties preserved in JSON (not baked into message)
- [ ] Files older than 30 days cleaned up automatically
