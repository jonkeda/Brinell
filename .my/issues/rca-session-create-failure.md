# RCA: session.create Failure After Auth Separation

**Date:** 2026-05-08  
**Symptom:** `Authenticated — session error: Communication error with Copilot CLI: Request session.create failed with message: M…`  
**Severity:** High — blocks all LLM functionality despite valid authentication

## Timeline

1. Phase 12-05 identified that `IsAuthenticated` conflated auth failures with model/session errors.
2. Fix separated `InitializeAsync` into two phases: client start (auth) → session creation (model).
3. After the fix, the status correctly shows "Authenticated" but session creation still fails.

## Root Cause

The user's persisted `settings.json` (`%LOCALAPPDATA%\Brinell.Scraper\settings.json`) contains an invalid model ID from a previous session (e.g. `"Claude Opus 4.6"` instead of `"claude-opus-4"`).

`AppSettings.Load()` reads the stale value and overwrites the code defaults (`gpt-4.1-mini` / `gpt-4.1`). When `CreateSessionAsync` sends this invalid model ID to the Copilot CLI, the CLI rejects it with a `session.create` failure.

**Chain:**
```
settings.json has invalid model ID
  → AppSettings.Load() overwrites defaults
    → CopilotService.InitializeAsync passes bad model to CreateSessionAsync
      → CLI rejects: "Request session.create failed with message: Model not found"
        → _stubMode = true, LastInitError = ex.Message
          → UI shows "Authenticated — session error: …"
```

## Why It Wasn't Caught Earlier

Before the auth/model separation, this same failure set `IsAuthenticated = false`, which triggered the CLI auth login flow — masking the real problem (bad model name) as an auth issue.

## Fix Options

1. **Validate model on save** — When saving settings, check the model ID against `ListModelsAsync()` results. Reject or warn if not in the list.
2. **Validate model on init** — In `InitializeAsync`, catch `session.create` failures and include the model name in the error message so the user knows which model to change.
3. **Delete stale settings** — User manually deletes `%LOCALAPPDATA%\Brinell.Scraper\settings.json` to reset to code defaults. (One-time workaround, not a fix.)
4. **Fallback on session failure** — If `CreateSessionAsync` fails for a model, retry with the code-default model before entering stub mode.

## Recommended Fix

Option 2 (improve error message) + Option 4 (fallback retry):

- Catch `session.create` exceptions and set `LastInitError` to include the model name:  
  `"Model '{model}' rejected by Copilot CLI: {message}"`
- On failure, retry with `gpt-4.1-mini` / `gpt-4.1` before giving up.
- Update the persisted settings to the working model so the stale value doesn't persist.

## Immediate Workaround

Delete or edit `%LOCALAPPDATA%\Brinell.Scraper\settings.json` — remove or correct the `analyzerModel` / `generatorModel` values.
