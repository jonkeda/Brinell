# User Acceptance Tests — Phase 12 Wireup 05 (Settings Tab Actions)

Manual test scenarios for the Settings tab: **Site settings**, **Model selection**, **Logging toggles**, **Save/Reset**, and **Copilot authentication** (SDK-only, no manual token).

**Prerequisites:**

- Windows 10/11 with .NET 10 runtime
- At least 1 existing site in the corpus database
- Phase 12 Workspace shell functional (Settings tab renders inside tabbed workspace)
- Copilot SDK NuGet package installed

---

## W5.1 — Site Settings (Context-Dependent)

### UAT-W5.1.1 — Site Group Visible When Site Active

- [X] Open a site from the Start Page. Navigate to the Settings tab.
- [X] The "Site" group box is visible, showing Name, Start URL, Output Path (with Browse button), and Target Namespace fields.
- [X] All fields are pre-populated with the current site's values.

### UAT-W5.1.2 — Site Group Hidden When No Site

- [ ] If the Settings tab is somehow rendered without an active site (`IsSiteContextActive = false`), the "Site" group box is collapsed.

### UAT-W5.1.3 — Browse Output Path

- [X] Click **Browse…** next to the Output Path field. A folder picker dialog opens.
- [X] Select a folder and confirm. The Output Path field is populated with the selected path.
- [X] Cancel the folder picker. The Output Path field remains unchanged.

### UAT-W5.1.4 — Save Persists Site Changes

- [X] Change the site Name to "Updated Name" and click **Save**.
- [ ] Close and relaunch the application. Open the same site. Navigate to Settings. The Name reads "Updated Name".

### UAT-W5.1.5 — Reset Reverts Site Changes

- [X] Change the site Name and Start URL but do **not** click Save. Click **Reset**.
- [X] Both fields revert to their previously saved values.

---

## W5.2 — Model Selection

### UAT-W5.2.1 — Analyzer and Generator Dropdowns (Fallback List)

- [X] The "Models" group box contains two editable combo boxes: Analyzer model and Generator model.
- [X] When Copilot is **not authenticated**, each dropdown lists the fallback models: `gpt-4.1`, `gpt-4.1-mini`, `gpt-4.1-nano`, `o3-mini`, `claude-sonnet-4`, `claude-opus-4`.
- [X] The default selections are `gpt-4.1-mini` (Analyzer) and `gpt-4.1` (Generator).
- [X] The current selections match `AppSettings.AnalyzerModel` and `AppSettings.GeneratorModel`.

### UAT-W5.2.2 — Dynamic Model List (Authenticated)

- [X] Authenticate with `copilot auth login`. Open a site and navigate to Settings.
- [X] The model dropdowns are populated with the live model list returned by `ListModelsAsync()` — not the static fallback.
- [X] The previously selected model is preserved if it exists in the live list.

### UAT-W5.2.3 — Refresh Models Button

- [X] A "↻" refresh button is visible next to the Models group header.
- [X] Click the refresh button. The dropdown lists update to the latest models from the SDK.
- [X] If the SDK is not authenticated, the refresh is a no-op (fallback list remains).

### UAT-W5.2.4 — Custom Model Entry

- [X] Type a custom model name (e.g. "gpt-5") directly into the Analyzer combo box. The field accepts the freeform text.
- [X] Click **Save**. Close and relaunch. The custom model name persists.

### UAT-W5.2.5 — Model Change Saved

- [X] Change the Generator model from `gpt-4.1` to `gpt-4.1-mini`. Click **Save**.
- [X] Close and relaunch. Navigate to Settings. The Generator model reads `gpt-4.1-mini`.

---

## W5.3 — Logging Toggles

### UAT-W5.3.1 — Checkboxes Reflect Current State

- [X] The "Logging" group box contains two checkboxes: "Log LLM prompts" and "Log LLM responses".
- [X] Their checked states match `AppSettings.LogLlmPrompts` and `AppSettings.LogLlmResponses`.

### UAT-W5.3.2 — Toggle and Save Persists

- [X] Check "Log LLM prompts" (if unchecked), click **Save**.
- [X] Close and relaunch. The checkbox is still checked.
- [X] Uncheck it, click **Save**. Relaunch. The checkbox is unchecked.

---

## W5.4 — Paths (Read-Only)

### UAT-W5.4.1 — Corpus and Skills Root Displayed

- [X] The "Paths" group box shows Corpus root and Skills root as read-only text fields.
- [X] Both display the values from `AppSettings.CorpusRoot` and `AppSettings.SkillsRoot`.
- [X] The fields are not editable (typing has no effect).

---

## W5.5 — Copilot Authentication (SDK CLI Auth)

### UAT-W5.5.1 — Initial Status — Not Authenticated

- [X] Launch the app without having run `copilot auth login`. Open a site and navigate to Settings.
- [X] The "GitHub Copilot" group box shows status text "Not signed in" and a "Sign in to GitHub" button.

### UAT-W5.5.2 — Sign In — Launches CLI Auth Login

- [X] With the Copilot CLI not authenticated, click **Sign in to GitHub**.
- [X] Status shows "Connecting..." then "Waiting for browser login...".
- [X] A console window opens running `copilot auth login`.
- [X] The browser OAuth flow is triggered automatically.

### UAT-W5.5.3 — Sign In — Complete OAuth Flow

- [X] Complete the browser OAuth flow from UAT-W5.5.2.
- [X] The console window closes after authentication succeeds.
- [X] The app automatically retries SDK initialization.
- [X] Status changes to "Authenticated". `IsCopilotAuthenticated` is `true`.
- [X] The Copilot SDK sessions (analyzer + generator) are initialized — verify via log output.
- [X] The model dropdowns are refreshed with the live model list.

### UAT-W5.5.4 — Sign In — OAuth Cancelled

- [X] Click **Sign in to GitHub**. The console window opens.
- [X] Close the console window without completing the OAuth flow.
- [X] Status changes to "Authentication failed — check browser login".
- [X] `IsCopilotAuthenticated` remains `false`. The app does not crash.

### UAT-W5.5.5 — Sign In — No Active Site

- [X] If `CurrentSiteId` is 0 or null (no site selected), clicking **Sign in to GitHub** does not attempt `InitializeAsync`.
- [X] Status reflects current `IsAuthenticated` state without error.

### UAT-W5.5.6 — Sign In Re-Attempts After Failure

- [ ] Click **Sign in to GitHub** — cancel or fail the OAuth flow → failure status shown.
- [ ] Click **Sign in to GitHub** again → console window opens again for a fresh attempt.
- [ ] Complete the OAuth flow. Status changes to "Authenticated". Sessions initialize.

### UAT-W5.5.7 — Sign In — CLI Path Not Found (Fallback)

- [ ] If `GetCliPath()` returns `null` (e.g. `COPILOT_CLI_PATH` not set and bundled binary missing), clicking **Sign in to GitHub** shows "Authentication failed — check browser login".
- [ ] No console window opens. The app does not crash.

### UAT-W5.5.8 — Sign In Error Handling

- [ ] If `InitializeAsync` throws an unexpected exception after CLI auth login, the CLI auth login is attempted as a recovery step.
- [ ] If recovery also fails, status shows "Authentication failed — check browser login".
- [ ] `IsCopilotAuthenticated` is set to `false`. The app does not crash.

### UAT-W5.5.9 — Stub Mode After Init Failure

- [ ] When the SDK fails to start (CLI not authenticated), `CopilotService` enters stub mode.
- [ ] `AnalyzeAsync` and `GenerateAsync` return empty strings with a warning log — no exceptions thrown.
- [ ] The rest of the app continues to function normally (site management, corpus browsing, etc.).

### UAT-W5.5.10 — Env Var Auth (SDK Built-In)

- [ ] Set `COPILOT_GITHUB_TOKEN` (or `GH_TOKEN` / `GITHUB_TOKEN`) to a valid token before launching the app.
- [ ] Open a site. The SDK authenticates via the env var — `IsAuthenticated` is `true` without needing `copilot auth login`.
- [ ] The Settings tab shows "Authenticated" status.

---

## W5.6 — Save / Reset Buttons

### UAT-W5.6.1 — Save Persists All App Settings

- [X] Change Analyzer model, Generator model, and toggle both logging checkboxes. Click **Save**.
- [X] Close and relaunch. All changed values persist.

### UAT-W5.6.2 — Reset Reverts Unsaved Changes

- [X] Modify several fields (site name, model, logging toggles) without saving. Click **Reset**.
- [X] All fields revert to their last-saved values.

### UAT-W5.6.3 — Save Error Logged

- [ ] If `Save` fails (e.g. database locked), an error is logged. The app does not crash.

---

## W5.7 — ICopilotAuthService Removal (Regression)

### UAT-W5.7.1 — No References to ICopilotAuthService

- [X] Verify `Services/ICopilotAuthService.cs` does not exist.
- [X] Grep the `Brinell.Scraper` project for `ICopilotAuthService` — zero source code matches.

### UAT-W5.7.2 — DI Container Clean

- [X] `App.xaml.cs` does not register `ICopilotAuthService`. No runtime DI resolution errors on startup.

### UAT-W5.7.3 — CopilotService Has No Auth Dependency

- [X] `CopilotService` constructor takes `ILogger`, `CorpusTools`, `ISessionContext`, `AppSettings` — no `ICopilotAuthService`.
- [X] `InitializeAsync` does not call `GetTokenAsync` — it directly starts the SDK.

### UAT-W5.7.4 — Build Succeeds

- [X] `dotnet build Brinell.Scraper.csproj` completes with 0 errors.

---

## Sign-off

| Section                             | Tester | Date | Result |
| ----------------------------------- | ------ | ---- | ------ |
| W5.1 — Site Settings               |        |      |        |
| W5.2 — Model Selection             |        |      |        |
| W5.3 — Logging Toggles             |        |      |        |
| W5.4 — Paths                       |        |      |        |
| W5.5 — Copilot Authentication      |        |      |        |
| W5.6 — Save / Reset                |        |      |        |
| W5.7 — ICopilotAuthService Removal |        |      |        |
