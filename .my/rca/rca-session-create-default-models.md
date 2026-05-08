# RCA: session.create Fails Even With Default Models

**Date:** 2026-05-08  
**Symptom:** `Authenticated — session error: Model 'gpt-4.1-mini' / 'gpt-4.1': Communication error with Copilot CLI: Request session.create failed...`  
**Severity:** Critical — no LLM functionality, auth login never triggered

## Root Cause

`_client.StartAsync()` starts the local Copilot CLI process and opens an IPC channel. `ConnectionState.Connected` only means the local pipe is up — it does **not** verify that the user has a valid GitHub OAuth token.

The actual GitHub auth check happens server-side when `CreateSessionAsync` calls the Copilot API. When the token is missing/expired, the API rejects `session.create`. This happens for **all** models, including the fallback defaults.

Our previous fix separated auth from model errors by checking `_client.State == ConnectionState.Connected` as the auth indicator. This was wrong — IPC connection ≠ GitHub authentication.

## Why Auth Login Never Triggers

```
User clicks "Connect"
  → _client.StartAsync() succeeds (IPC up)
  → IsAuthenticated = (_client.State == Connected) = TRUE  ← WRONG
  → CreateSessionAsync fails (no GitHub token)
  → Fallback models also fail (same reason)
  → Code enters: else if (LastInitError is not null) → shows error message
  → TryCliAuthLoginAsync is NEVER called because IsAuthenticated is true
```

The user expects: try → fail → launch login → user logs in → retry. Instead they get a dead-end error message.

## Fix

`SignInAsync` should check `LastInitError` after init. If sessions failed — even when the CLI process is running — launch the auth login flow:

```
Click Connect
  → InitializeAsync (start CLI + try sessions)
  → If LastInitError is set (sessions failed) → launch auth login
  → Status tells user to retry after completing browser login
```

`IsAuthenticated` should reflect actual usability: client connected AND sessions created (not in stub mode). This restores the original `!_stubMode && _client?.State == Connected` logic, which was correct for the UI's needs — it just wasn't being used to distinguish auth-vs-model errors internally.

## Learnings

- Local IPC `ConnectionState.Connected` is not a proxy for GitHub auth
- The Copilot SDK does not expose a lightweight "am I logged in?" check
- The only way to know auth status is to attempt an API operation
- When fallback models also fail `session.create`, it's almost certainly an auth problem, not a model problem
