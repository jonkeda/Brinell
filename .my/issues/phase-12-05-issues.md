# Phase 12-05 — Issues

## Issue 1: Available models list is outdated

**Location:** `SettingsTabViewModel.cs` constructor

The `AvailableModels` dropdown is hardcoded to old, retired models:

```csharp
AvailableModels = ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "o1-mini"];
```

Confirmed from logs — `gpt-4o-mini`, `gpt-4-turbo`, and `Claude Opus 4.6` (user-typed display name) all fail with `"Model X is not available"`. The defaults (`_analyzerModel = "gpt-4o-mini"` and `_generatorModel = "gpt-4o"`) are also stale.

### Fix Design

The Copilot SDK exposes `CopilotClient.ListModelsAsync()` to query available models at runtime. Use that instead of a hardcoded list.

**Changes:**

1. **`ICopilotService`** — add:

   ```csharp
   Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default);
   ```
2. **`CopilotService`** — implement by calling `_client.ListModelsAsync()` after `StartAsync()`. Return an empty list when in stub mode or before initialization.
3. **`SettingsTabViewModel`** — replace the hardcoded collection initializer:

   - Keep a static fallback list for when the SDK is unavailable:
     ```csharp
     private static readonly string[] FallbackModels = ["gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "o3-mini", "claude-sonnet-4", "claude-opus-4"];
     ```
   - In the constructor, seed `AvailableModels` with the fallback list.
   - Add a `RefreshModelsCommand` (async) that calls `_copilot.ListModelsAsync()`, clears and repopulates `AvailableModels`, preserving the current selection.
   - Call `RefreshModelsCommand` from `RefreshCopilotStatus()` when `IsAuthenticated` becomes `true`.
4. **Update defaults:**

   ```csharp
   private string _analyzerModel = "gpt-4.1-mini";
   private string _generatorModel = "gpt-4.1";
   ```
5. **`AppSettings`** — update the default values for `AnalyzerModel` and `GeneratorModel` to match.
6. **`SettingsTabView.xaml`** — add a small "↻" refresh button next to the Models group header, bound to `RefreshModelsCommand`.

---

## Issue 2: Sign-in button should run `copilot auth login` instead of showing a message

**Location:** `SettingsTabViewModel.SignInAsync()`

When Copilot authentication fails, the UI only displays a status message telling the user to run `copilot auth login` in a terminal:

```csharp
CopilotStatus = "Not authenticated — run 'copilot auth login' in a terminal";
```

### Fix Design

The Copilot CLI is bundled inside the `GitHub.Copilot.SDK` NuGet package — no separate install needed. When auth fails, launch the CLI's interactive `auth login` flow in a visible console window, then retry SDK initialization when it exits.

**Changes:**

1. **`ICopilotService`** — add:

   ```csharp
   string? GetCliPath();
   ```
   Returns the path to the bundled `copilot` binary. The `CopilotClient` constructor resolves this from the `COPILOT_CLI_PATH` env var or the NuGet package's `tools/` directory. Expose it so the ViewModel can launch the process.
2. **`CopilotService`** — implement `GetCliPath()`:

   - Check `COPILOT_CLI_PATH` env var first.
   - Fall back to locating `copilot.exe` relative to the SDK assembly (`GitHub.Copilot.SDK.dll`) in the NuGet tools folder.
3. **`SettingsTabViewModel.SignInAsync()`** — replace the "run in a terminal" status messages:

   ```csharp
   if (!_copilot.IsAuthenticated)
   {
       var cliPath = _copilot.GetCliPath();
       if (cliPath is not null)
       {
           CopilotStatus = "Waiting for browser login...";
           var proc = Process.Start(new ProcessStartInfo
           {
               FileName = cliPath,
               Arguments = "auth login",
               UseShellExecute = true, // opens a visible console window
           });
           if (proc is not null)
               await proc.WaitForExitAsync(ct);

           // Retry SDK initialization after login completes
           var siteId = _sessionContext.CurrentSiteId ?? 0;
           var slug = _sessionContext.CurrentSiteSlug ?? "default";
           if (siteId > 0)
               await _copilot.InitializeAsync(siteId, slug, ct);
           RefreshCopilotStatus();
       }
       if (!_copilot.IsAuthenticated)
           CopilotStatus = "Authentication failed — check browser login";
   }
   ```
4. **Catch block** — same pattern: try launching `copilot auth login` before falling back to the status message.
5. **Edge case:** if `GetCliPath()` returns `null` (env misconfigured, package corruption), fall back to the current message behavior as a last resort.

---

## Issue 3: `GetCliPath()` returns null — no login window opens

**Location:** `CopilotService.GetCliPath()`

**Symptom:** Clicking "Sign in to GitHub" shows "Authentication failed — check browser login" immediately, but no console window opens for `copilot auth login`.

**Root cause:** `GetCliPath()` looks for `copilot.exe` next to `GitHub.Copilot.SDK.dll` in the output directory. But the SDK's MSBuild targets download the CLI to `runtimes/{rid}/native/copilot.exe` — not next to the DLL:

```
bin/Debug/net10.0-windows/
├── GitHub.Copilot.SDK.dll          ← SDK assembly
├── Brinell.Scraper.exe
└── runtimes/win-x64/native/
    └── copilot.exe                  ← actual CLI binary
```

The current implementation only checks:
1. `COPILOT_CLI_PATH` env var
2. `copilot.exe` next to the SDK DLL

Neither matches.

**Secondary issue:** The logs also show the SDK *is* authenticated (ping/pong succeeds, `ConnectionState.Connected`) — but `CreateSessionAsync` fails with `"Model \"Claude Opus 4.6\" is not available"` because the user previously typed a display name instead of an API model ID. This puts the service in stub mode, making `IsAuthenticated` return `false`, which incorrectly triggers the auth login flow instead of surfacing the model error.

### Fix

Update `GetCliPath()` to search the `runtimes/{rid}/native/` directory relative to the application base directory:

```csharp
public string? GetCliPath()
{
    var envPath = Environment.GetEnvironmentVariable("COPILOT_CLI_PATH");
    if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        return envPath;

    var appDir = AppContext.BaseDirectory;
    var rid = RuntimeInformation.RuntimeIdentifier;

    // Primary: runtimes/{rid}/native/copilot.exe (SDK MSBuild target output)
    var runtimePath = Path.Combine(appDir, "runtimes", rid, "native", "copilot.exe");
    if (File.Exists(runtimePath))
        return runtimePath;

    // Fallback: portable RID (e.g. win-x64 when full RID is win-x64 or win10-x64)
    var parts = rid.Split('-');
    if (parts.Length >= 2)
    {
        var portableRid = $"{parts[0]}-{parts[^1]}";
        var portablePath = Path.Combine(appDir, "runtimes", portableRid, "native", "copilot.exe");
        if (File.Exists(portablePath))
            return portablePath;
    }

    return null;
}
```
