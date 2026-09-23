# Phase 3 — Logging

## Goal

Integrate structured logging throughout the application using Microsoft.Extensions.Logging. Provide file output, in-app viewer, and specific logging for LLM and DOM operations.

## Tasks

### 3.1 — Integrate `Microsoft.Extensions.Logging` with `ILogger<T>` Throughout All Services

**Implementation Details:**

- NuGet package: `Microsoft.Extensions.Logging` + `Microsoft.Extensions.Logging.Abstractions`
- Register `ILoggerFactory` in the DI container (`App.xaml.cs`):
  ```csharp
  services.AddLogging(builder =>
  {
      builder.SetMinimumLevel(LogLevel.Debug);
      builder.AddDebug();                         // Output window
      builder.AddProvider(new InAppLogProvider()); // In-app viewer (task 3.3)
      // File sink added in task 3.2
  });
  ```
- Inject `ILogger<T>` into every service and ViewModel via constructor:
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
- Use structured logging with message templates (not string interpolation):
  ```csharp
  _logger.LogInformation("Navigating to {Url}", url);
  _logger.LogError(ex, "Navigation failed for {Url}", url);
  ```

---

### 3.2 — File Sink — Structured Log Output to `logs/` Folder

**Implementation Details:**

- **Option A — Serilog** (recommended):
  - NuGet packages: `Serilog.Extensions.Logging`, `Serilog.Sinks.File`
  - Configuration:
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
  - Rolling daily files with pattern `scraper-20260420.json`
  - Structured JSON format — each line is a JSON object with timestamp, level, message, properties

- **Option B — Custom file logger provider** (if avoiding Serilog dependency):
  - Implement `ILoggerProvider` + `ILogger` that writes to a `StreamWriter`
  - Buffer writes and flush periodically or on `Warning`+ level
  - Roll files daily based on date check

- Log directory: `{app-base}/logs/`
- Retention: keep last 30 days of log files (configurable)

---

### 3.3 — In-App Log Viewer Panel

**Implementation Details:**

- WPF UserControl `LogViewerPanel.xaml`:
  ```xml
  <DockPanel>
    <!-- Filter bar -->
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="4">
      <TextBlock Text="Level:" VerticalAlignment="Center" Margin="0,0,4,0"/>
      <ComboBox ItemsSource="{Binding LogLevels}"
                SelectedItem="{Binding SelectedLogLevel}"
                Width="120"/>
      <Button Content="Clear" Command="{Binding ClearLogsCommand}" Margin="8,0,0,0"/>
    </StackPanel>

    <!-- Log entries -->
    <ListBox ItemsSource="{Binding FilteredLogEntries}"
             ScrollViewer.CanContentScroll="True"
             VirtualizingPanel.IsVirtualizing="True">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <StackPanel Orientation="Horizontal">
            <TextBlock Text="{Binding Timestamp, StringFormat=HH:mm:ss.fff}"
                       Width="90" Foreground="Gray"/>
            <TextBlock Text="{Binding Level}" Width="60"
                       Foreground="{Binding Level, Converter={StaticResource LogLevelToBrush}}"/>
            <TextBlock Text="{Binding Source}" Width="120" Foreground="DarkCyan"
                       ToolTip="Log source: Browser, DomCapture, Corpus, Analyzer, Generator, etc."/>
            <TextBlock Text="{Binding Category}" Width="200" Foreground="DarkCyan"/>
            <TextBlock Text="{Binding Message}" TextTrimming="CharacterEllipsis"/>
          </StackPanel>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </DockPanel>
  ```
- `LogEntry` model:
  ```csharp
  public record LogEntry(DateTime Timestamp, LogLevel Level, string Source, string Category, string Message);
  ```
- `Source` identifies the subsystem: `Browser`, `DomCapture`, `Corpus`, `Analyzer`, `Generator`, `Llm`, `Navigation`, etc.
- `InAppLogProvider` implements `ILoggerProvider` — creates loggers that push `LogEntry` to a shared `ObservableCollection<LogEntry>`
- `FilteredLogEntries` — `ICollectionView` with filter predicate based on `SelectedLogLevel`
- ComboBox options: `Debug`, `Information`, `Warning`, `Error` (show entries at or above the selected level)
- Virtualization enabled for performance with large log volumes
- Auto-scroll to bottom on new entries (optional toggle)

---

### 3.4 — LLM Request/Response Logging

**Implementation Details:**

- **Two-model strategy** — the Scraper uses two LLM agents with different roles:
  - **Analyzer model** (cheaper/faster) — pattern detection across the corpus, identifying shared layouts and proposing custom controls
  - **Generator model** (smarter/more capable) — code generation for individual pages and custom controls
- Dedicated logger categories per agent:
  - `ILogger` with named category `"Brinell.Scraper.Analyzer"` for analysis calls
  - `ILogger` with named category `"Brinell.Scraper.Generator"` for generation calls
  - Shared base category `"Brinell.Scraper.Llm"` for common LLM plumbing
- Log on every LLM call (both agents):
  ```csharp
  _logger.LogInformation(
      "LLM request — Agent: {Agent}, Model: {Model}, Prompt length: {PromptLength} chars",
      agent, model, prompt.Length);

  // ... await LLM call ...

  _logger.LogInformation(
      "LLM response — Agent: {Agent}, Model: {Model}, Response length: {ResponseLength} chars, " +
      "Tokens: {PromptTokens}+{CompletionTokens}={TotalTokens}, Elapsed: {ElapsedMs} ms",
      agent, model, response.Length, usage.PromptTokens, usage.CompletionTokens,
      usage.TotalTokens, stopwatch.ElapsedMilliseconds);
  ```
- **Analysis-specific logging** (Analyzer agent):
  ```csharp
  _logger.LogInformation(
      "Analysis — Pages analyzed: {PageCount}, Patterns detected: {PatternCount}, " +
      "Custom controls proposed: {ControlCount}",
      pageCount, patternCount, controlCount);
  ```
- **Generation-specific logging** (Generator agent):
  ```csharp
  _logger.LogInformation(
      "Generation — Page: {PageName}, Custom controls used: {ControlNames}",
      pageName, string.Join(", ", controlNames));
  ```
- Log prompt text truncated to first 500 characters at `Debug` level (avoid flooding logs with full prompts)
- Log full prompt/response at `Trace` level for diagnostics
- On error, log exception with `LogError` including agent name, model name, and prompt length
- Track cumulative token usage per session per agent (surfaced in status bar or log viewer)

---

### 3.5 — DOM Capture Logging

**Implementation Details:**

- Dedicated logger category: `ILogger<DomCaptureService>` (or named category `"Brinell.Scraper.DomCapture"`)
- Log on every DOM snapshot:
  ```csharp
  _logger.LogInformation(
      "DOM capture — URL: {Url}, Elements: {ElementCount}, " +
      "Size: {SnapshotSizeBytes} bytes, Elapsed: {ElapsedMs} ms",
      url, elementCount, snapshotSizeBytes, stopwatch.ElapsedMilliseconds);
  ```
- **Corpus storage** — after capture, the snapshot is stored in a per-site SQLite corpus. Log the storage operation:
  ```csharp
  _logger.LogInformation(
      "Corpus store — Site: {SiteName}, Page: {PageName}, IsNew: {IsNewPage}, " +
      "Corpus total pages: {TotalPages}, Corpus total elements: {TotalElements}",
      siteName, pageName, isNewPage, totalPages, totalElements);
  ```
- Log fields:
  - `Url` — the page URL at capture time
  - `ElementCount` — total number of DOM elements in the snapshot
  - `SnapshotSizeBytes` — size of the serialized DOM snapshot in bytes
  - `ElapsedMs` — time taken to execute the JavaScript DOM capture and transfer the result
  - `SiteName` — which site corpus the snapshot was stored in
  - `IsNewPage` — whether this is a new page or a re-recording of an existing page
  - `TotalPages` — corpus page count after storage
  - `TotalElements` — corpus total element count after storage
- Log at `Debug` level: individual element details (tag, id, classes) for selected/inspected elements
- On capture failure, log exception with `LogError` including URL and partial element count if available

---

### 3.6 — Corpus & Analysis Logging

**Implementation Details:**

- Dedicated logger category: named category `"Brinell.Scraper.Corpus"`
- **Corpus lifecycle:**
  ```csharp
  _logger.LogInformation(
      "Corpus opened — Site: {SiteName}, Pages: {PageCount}, Last recorded: {LastRecordingDate}",
      siteName, pageCount, lastRecordingDate);
  ```
- **Analysis started/completed:**
  ```csharp
  _logger.LogInformation(
      "Analysis started — Pages: {PageCount}, Model: {Model}",
      pageCount, model);

  _logger.LogInformation(
      "Analysis completed — Patterns found: {PatternCount}, " +
      "Custom controls proposed: {ControlCount}, Elapsed: {ElapsedMs} ms",
      patternCount, controlCount, stopwatch.ElapsedMilliseconds);
  ```
- **Control approval flow:**
  ```csharp
  _logger.LogInformation(
      "Control {Action} — Name: {ControlName}, Reason: {Reason}",
      action, controlName, reason); // action = "approved" | "rejected"
  ```
- **Generation batch tracking:**
  ```csharp
  _logger.LogInformation(
      "Generation batch — Queued: {QueuedCount}, Completed: {CompletedCount}, " +
      "Failed: {FailedCount}, Total tokens: {TotalTokens}",
      queuedCount, completedCount, failedCount, totalTokens);
  ```

---

## UI Design — Log Viewer (Bottom Panel)

Toggle via `View → Logs` or status bar click. Collapsible bottom panel.

```
┌──────────────────────────────────────────────────────────────────────┐
│  ◀ ▶ ↻ │ https://start.exactonline.nl/app/#/time  │ 🔍  ▶  ⏺  🔬│
├──────────────────┬───────────────────────────────────────────────────┤
│ 📁 Exact Online  │                                                   │
│ ...               │    (any content panel, shorter height)            │
│                   │                                                   │
├──────────────────┴───────────────────────────────────────────────────┤
│ 📋 Logs                                          [▼ Level: All ▼] ▲ │
│ ───────────────────────────────────────────────────────────────────── │
│ 14:32:01  INFO   Browser        Navigated to /app/#/time             │
│ 14:32:02  INFO   Corpus         Snapshot captured: 342 elements      │
│ 14:32:02  DEBUG  Corpus         Capture took 120ms, 48KB JSON        │
│ 14:35:10  INFO   Analyzer       Analysis started: 50 pages           │
│ 14:35:18  INFO   Analyzer       3 custom control patterns detected   │
│ 14:36:01  INFO   Generator      Generating ProjectListPage.cs        │
│ 14:36:03  INFO   Generator      Response: 890 tokens, 1.8s           │
│ 14:36:03  INFO   Roslyn         Parse OK — 0 errors, 0 warnings      │
│ 14:36:03  INFO   Generator      Generated ProjectListPage.cs ✅      │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

- DataGrid with columns: Time, Level, Source, Message
- Dropdown filter by level (All, Debug, Info, Warning, Error)
- Resizable via GridSplitter (drag top edge)
- Auto-scroll to latest, pause button
- Two-model distinction: `Analyzer` and `Generator` as separate Source categories

---

## Acceptance Criteria

- [ ] All services and ViewModels receive `ILogger<T>` via constructor injection
- [ ] Log entries are written to rolling daily JSON files in `logs/`
- [ ] In-app log viewer displays log entries in real time
- [ ] Log viewer can be filtered by log level (Debug and above, Info and above, etc.)
- [ ] LLM requests log: agent (analyzer/generator), model name, prompt length, response length, token counts, elapsed time
- [ ] Analyzer logs include: pages analyzed, patterns detected, custom controls proposed
- [ ] Generator logs include: page being generated, custom controls in use
- [ ] LLM prompts are logged at Debug level (truncated) and Trace level (full)
- [ ] DOM captures log: URL, element count, snapshot size, elapsed time
- [ ] DOM captures log corpus storage: site name, new vs re-recorded page, corpus totals
- [ ] Corpus operations log: open/create with site name and page count, last recording date
- [ ] Analysis lifecycle logged: start (page count, model), completion (patterns, controls, elapsed)
- [ ] Control approval/rejection logged with control name and reason
- [ ] Generation batch logged: queued/completed/failed counts, total tokens
- [ ] Log viewer Source column distinguishes subsystems (Browser, DomCapture, Corpus, Analyzer, Generator)
- [ ] Log files older than 30 days are cleaned up automatically
- [ ] Structured log properties are preserved in JSON output (not baked into message strings)

## Dependencies

| Dependency | Purpose |
|---|---|
| `Microsoft.Extensions.Logging` NuGet | Logging abstractions and `ILoggerFactory` |
| `Microsoft.Extensions.Logging.Abstractions` NuGet | `ILogger<T>` interface |
| `Microsoft.Extensions.Logging.Debug` NuGet | Debug output window sink |
| `Serilog.Extensions.Logging` NuGet | Serilog integration with Microsoft.Extensions.Logging |
| `Serilog.Sinks.File` NuGet | Rolling file sink with structured JSON output |
| Phase 1, step 1.2 (MVVM Foundation) | DI container and ViewModelBase for log viewer ViewModel |

---

## Unit Test Plan

> Full test details in [unittest-roadmap.md](unittest-roadmap.md)

### Testable Components (~20 tests)

| Component | Tests | Strategy |
|-----------|-------|----------|
| `InAppLogService` | 5 | Add/clear entries, ordering, thread safety, observable notifications |
| `InAppLogProvider` | 4 | Logger creation, short name extraction, IsEnabled, entry forwarding |
| `LogViewerViewModel` | 6 | Level filtering, filter refresh, clear command, auto-scroll default |
| `LogLevelToBrushConverter` | 5 | Color mapping per log level, fallback for unknown values |

### Not Unit-Tested

- `LogViewerPanel.xaml.cs` — WPF UserControl with auto-scroll behavior
- Serilog file sink configuration — verified by integration test (log file appears on disk)
- `AddLogging()` DI registration — verified by app startup

### Test Infrastructure

- **Mocking:** `InAppLogService` is concrete (no interface needed — simple collection)
- **Threading:** `InAppLogService.Add()` tests use `SynchronizationContext` or direct calls (no dispatcher in tests)
