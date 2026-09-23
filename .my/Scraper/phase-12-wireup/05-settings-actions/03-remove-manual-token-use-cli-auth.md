# Step 12.W.5 — Remove Manual Token Flow, Use Copilot CLI Auth

> **Status: DONE** — Implemented 2026-05-08.
> Steps 12.W.5a and 12.W.5b (credential store, token-change event) were **never implemented** — superseded by this step.
> Files `01-github-signin-credential-store.md` and `02-reinit-copilot-on-token-change.md` deleted.

## Objective

Remove the dead-end manual token gate (`GITHUB_COPILOT_TOKEN` env var / credential store) and rely entirely on the Copilot SDK's built-in authentication via the bundled Copilot CLI (`UseLoggedInUser = true`).

### Why

The original implementation had an architectural mismatch:

1. `CopilotAuthService.GetTokenAsync()` read a token from `GITHUB_COPILOT_TOKEN`.
2. `CopilotService.InitializeAsync` used that token **only as a boolean gate** — if empty, enter stub mode.
3. But `CopilotClient` was created with `UseLoggedInUser = true`, which delegates auth entirely to the **Copilot CLI's own OAuth state**.
4. The user-supplied token was **never passed to the SDK**.

Result: the manual token flow was dead code.

### Approach taken

- Removed `ICopilotAuthService` interface and `CopilotAuthService` class entirely.
- `CopilotService.InitializeAsync` now directly attempts to start the SDK; catches failures and enters stub mode.
- Settings "Sign in" button re-attempts `InitializeAsync` and shows `copilot auth login` instructions on failure.

## Changes made

### Deleted

| Path | Reason |
|------|--------|
| `Services/ICopilotAuthService.cs` | Entire file — interface + implementation removed |

### Modified

| Path | Change |
|------|--------|
| `Services/CopilotService.cs` | Removed `ICopilotAuthService` field + constructor param; removed token gate in `InitializeAsync`; catch block updated to reference `copilot auth login` |
| `ViewModels/Tabs/SettingsTabViewModel.cs` | Replaced `ICopilotAuthService` with `ISessionContext`; `SignInAsync` now calls `InitializeAsync` directly and shows CLI instructions on failure |
| `App.xaml.cs` | Removed `ICopilotAuthService` DI registration |

### Unchanged

| Path | Reason |
|------|--------|
| `Services/CopilotServiceExtensions.cs` | `LlmAuthRequiredException` classification stays — SDK can still throw 401/403 at runtime |
| `Exceptions/LlmAuthRequiredException.cs` | Kept — runtime auth expiry is still possible |

## Auth flow (final)

| Scenario | Behavior |
|----------|----------|
| User has run `copilot auth login` | SDK connects automatically; `IsAuthenticated = true` |
| User has NOT authenticated the CLI | `StartAsync()` throws; stub mode; Settings shows "run copilot auth login" |
| Token expires mid-session | SDK throws 401 → `CopilotServiceExtensions` classifies as `LlmAuthRequiredException` → pipeline retries or surfaces error |
| User clicks "Sign in" in Settings | Re-attempts `InitializeAsync`; if CLI is now authenticated, session starts |

## Supported auth methods (SDK built-in)

The Copilot CLI supports these without any app-side code:

1. **`copilot auth login`** — interactive OAuth (opens browser)
2. **`COPILOT_GITHUB_TOKEN`** env var — PAT or OAuth token
3. **`GH_TOKEN` / `GITHUB_TOKEN`** env var — standard GitHub env vars
4. **BYOK** — `ProviderConfig` in `SessionConfig` (future option)

## Checklist

- [x] Delete `Services/ICopilotAuthService.cs`
- [x] Update `CopilotService` — remove `ICopilotAuthService` dependency and token gate
- [x] Update `SettingsTabViewModel` — remove `ICopilotAuthService`; add `ISessionContext`; update sign-in to re-init flow
- [x] Update `App.xaml.cs` — remove `ICopilotAuthService` DI registration
- [x] Verify build compiles without `ICopilotAuthService` references
- [x] Delete superseded step files (01, 02)
