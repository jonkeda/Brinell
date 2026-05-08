# User Acceptance Tests — Phase 13 (Analyzer Pipeline: ControlObjects & PageObjects)

Manual test scenarios to verify the end-to-end pipeline that turns scraped DOM snapshots into reusable ControlObjects and PageObjects: ControlObject Analyzer → Generator → Skill → ControlObject Matcher → PageObject Generator → Code Output, with persistence, error handling, and Copilot SDK integration.

**Prerequisites:**
- Windows 10/11 with .NET 10 runtime, WebView2 runtime
- A site with **at least 5 recorded pages** including repeated UI patterns (cards, table rows, form blocks). Use Phase 1–4 to record.
- A valid GitHub Copilot token. The implementation reads `GITHUB_COPILOT_TOKEN` from environment (or via `ICopilotAuthService`). Tests labelled **(stub-mode)** can be performed without a token to verify graceful degradation.
- For tests that exercise persistence: the SQLite corpus DB at `%LOCALAPPDATA%\Brinell.Scraper\sites\{slug}\corpus.db`.
- Phase 12 UI must be functional (workspace tabs available).

---

## 13.0 — Copilot SDK Integration

### UAT-13.0.1 — Initialize with Site Context

- [ ] Set `GITHUB_COPILOT_TOKEN` to a valid token. Open a site in the Workspace.
- [ ] In the Log tab (filter Information), verify a log entry: `Copilot SDK initialized — analyzer: {id}, generator: {id}, skill: {slug}-controls`.
- [ ] Reopen the same site. The skill name in the log entry remains `{slug}-controls`.
- [ ] Open a different site. The skill name reflects the new site's slug (per-site).

### UAT-13.0.2 — Stub Mode Without Token

- [ ] Unset `GITHUB_COPILOT_TOKEN` (and ensure the credential store is empty). Launch and open a site.
- [ ] A **Warning** log entry appears: `SDK not configured — running in stub mode`.
- [ ] No exception dialog is shown. Pipeline buttons are visible but indicate that LLM features are unavailable (or fail gracefully — see UAT-13.9).

### UAT-13.0.3 — Auth Required Event

- [ ] Set `GITHUB_COPILOT_TOKEN` to an invalid value (e.g. `ghp_invalid`). Trigger any LLM operation (Analyze).
- [ ] An auth-required notification surfaces (toast / dialog / status text), and the Log shows an error entry classifying the failure as 401/403.
- [ ] Setting a valid token and retrying succeeds.

---

## 13.1 — ControlObject Analyzer

### UAT-13.1.1 — Run Analysis (Phase A + Phase B)

- [ ] In the Workspace, switch to the **Control Objects** tab. Click **Analyze Corpus**.
- [ ] Status updates to "Analyzing corpus..." Log entries show:
  - Phase A (local): a `ControlGroupDetector` log line with the number of groups found.
  - Phase B (LLM): an `LLM request — analyzer` and `LLM response — analyzer` pair.
- [ ] On completion, the proposals list is populated with N items (each row: name, signature, frequency, confidence).
- [ ] The status text reads `Found N control patterns`.

### UAT-13.1.2 — Persistence of Analysis Result

- [ ] After a successful analysis, close and relaunch the app. Open the same site.
- [ ] On the Control Objects tab, the proposals from the previous analysis are still listed (loaded from `AnalysisResults` table where `IsCurrent=1`).

### UAT-13.1.3 — Re-Analysis Replaces Current

- [ ] Run **Analyze Corpus** a second time. The previous analysis row should be marked `IsCurrent=0` in `AnalysisResults` and a new row inserted with `IsCurrent=1`.
- [ ] Querying with `SELECT COUNT(*) FROM AnalysisResults WHERE IsCurrent=1 AND SiteId={id}` returns exactly **1**.

### UAT-13.1.4 — Empty Corpus

- [ ] Create a fresh site with zero recorded pages. Click **Analyze Corpus**.
- [ ] No exception. The proposals list ends up empty; status reads `Found 0 control patterns`.

---

## 13.2 — ControlObject Generator

### UAT-13.2.1 — Approve & Generate

- [ ] After analysis, approve 2–3 proposals. Click **Generate All Pending** (or equivalent).
- [ ] Each approved proposal transitions through `Pending → Generated` (or `Failed`) in the UI.
- [ ] Log entries show `LLM request — generator` per proposal and `Validating with Roslyn — Pass` on success.
- [ ] Each successfully generated control is registered (visible in the in-tab properties view, and present in `ControlRegistry` if exposed).

### UAT-13.2.2 — Validation Retry

- [ ] Generate against a proposal whose initial LLM response triggers a Roslyn validation error (this is hard to force on demand; observe naturally if it happens, otherwise N/A).
- [ ] When triggered: a Warning log entry shows the validation error and a single retry attempt with the feedback prompt.
- [ ] If the retry passes, status is `Generated`. If it fails, status is `Failed` with the error message captured.

### UAT-13.2.3 — Skip Rejected & Pending

- [ ] Reject one proposal. Leave another as Pending. Click **Generate All Pending**.
- [ ] Only the **approved** proposals are sent to the generator. Rejected and Pending proposals are not LLM-called.

### UAT-13.2.4 — Regenerate Selected

- [ ] Pick a previously generated control. Click **Regenerate**. The LLM is re-invoked, and the existing control is replaced (idempotently) on success.

---

## 13.3 — Skill Auto-Generation

### UAT-13.3.1 — SKILL.md Created Per Site

- [ ] After at least one control is generated for the site, verify the file exists:
  - `{SkillsRoot}/{slug}-controls/SKILL.md` (the skills root is configurable in Settings, e.g. `%LOCALAPPDATA%\Brinell.Scraper\skills\`).
- [ ] The file content lists each generated control by name with its CSS signature and a one-line description.

### UAT-13.3.2 — Regeneration Updates File

- [ ] Generate or regenerate one more control. Re-open the SKILL.md file.
- [ ] The file now includes the new/updated control entry. The file is overwritten atomically (no `.tmp` file left behind).

### UAT-13.3.3 — Two Sites Get Two Skill Folders

- [ ] Generate controls in Site A and Site B. Verify both `{slugA}-controls/SKILL.md` and `{slugB}-controls/SKILL.md` exist with site-specific content.

---

## 13.4 — PageObject Generator

### UAT-13.4.1 — Generate One Page

- [ ] Switch to the **Page Objects** tab. Pick a recorded page that has at least one snapshot.
- [ ] Click **Generate** on that page.
- [ ] The Log shows `ControlObjectMatcher matched M controls` then `LLM request — generator (page)`.
- [ ] On success, the page row's status becomes `Generated` and the property list populates.

### UAT-13.4.2 — Control-Aware Output

- [ ] On a page known to contain a generated ControlObject (e.g. a header nav matching `NavMenuControl`), generate the page object.
- [ ] Inspect the generated code preview. Properties should reference the matched ControlObject type (e.g. `NavMenuControl Header { get; }`) **rather than** plain primitive controls.

### UAT-13.4.3 — Validate With Registry

- [ ] When the generator emits a property whose type doesn't exist in built-ins, the registry, or inline containers, the validator must flag it.
- [ ] On the Page Objects tab, such a row shows a **validation warning/error** in the detail pane.

### UAT-13.4.4 — Generate All

- [ ] Click **Generate All**. The pipeline iterates all recorded pages.
- [ ] A progress indicator (count or percentage) updates. The Log emits one request/response pair per page.
- [ ] After completion, every successful page has `Status=Generated` and `GeneratedAt` set; failures show `Status=Error`.

### UAT-13.4.5 — Persistence

- [ ] After generation, close and relaunch. Open the site → Page Objects tab. The previously generated pages still show their results (loaded from `PageObjects` table, unique on `SnapshotId`).

---

## 13.5 — ControlObject Matcher

### UAT-13.5.1 — Threshold Behavior

- [ ] On a page that contains a clear ControlObject match (same tag, similar attributes, similar child chain), generate the page.
- [ ] In the Log (Debug filter), verify `ControlObjectMatcher` logs the score for each candidate. The matched ControlObject's score must be ≥ **0.75**.
- [ ] On a page where the same ControlObject does **not** apply (e.g. a different page section), the matcher logs scores below 0.75 and the generator does not reference that ControlObject.

### UAT-13.5.2 — Multiple Candidates

- [ ] On a page that has multiple matching DOM regions for the same ControlObject (e.g. several rows that match `RowControl`), the matcher reports M ≥ 2 matches and the generator emits a collection-style property where appropriate.

---

## 13.6 — Pipeline Orchestrator

### UAT-13.6.1 — Run Full Pipeline

- [ ] If exposed through the UI as **Run Full Pipeline** (or equivalent): trigger it on a fresh site that already has a corpus.
- [ ] The status text moves through stages: `Analyzing → Generating Controls → Generating Pages → Writing Output`.
- [ ] An `IProgress<PipelineProgress>` updates a progress bar / counter for each stage.
- [ ] On completion, every stage reports success and an output folder is produced (see 13.10).

### UAT-13.6.2 — Per-Stage Cancellation

- [ ] Start Analyze, then click **Cancel** before it completes.
- [ ] A Warning log entry indicates cancellation. The UI returns to idle. No partial garbage is committed to the database (no orphan `IsCurrent=1` row).
- [ ] Repeat for Generate Controls and Generate Pages.

### UAT-13.6.3 — RunId Correlation in Logs

- [ ] Start a full pipeline run. Filter the Log tab by the run's RunId (visible as a scope on every log entry).
- [ ] All entries from start to finish carry the same RunId. Stage-only invocations (called directly by tab buttons) carry their own per-call RunId.

---

## 13.7 — Data Persistence

### UAT-13.7.1 — Schema Created Idempotently

- [ ] Delete the corpus DB for a site. Reopen the site → schema is created.
- [ ] Reopen again → no `CREATE TABLE` errors; tables `AnalysisResults` and `PageObjects` exist with `IsCurrent` (analysis) and `UNIQUE(SnapshotId)` (pageobjects).

### UAT-13.7.2 — IsCurrent Single-Row Invariant

- [ ] Per UAT-13.1.3 above: after multiple analyses, only one row per `SiteId` has `IsCurrent=1`.

### UAT-13.7.3 — Snapshot-Unique PageObject

- [ ] Run **Generate** twice on the same page snapshot. The DB has a single row in `PageObjects` for that `SnapshotId` (the second run replaced/updated, not duplicated).
- [ ] Recording a new snapshot of the same URL and generating produces a separate `PageObjects` row (different `SnapshotId`).

### UAT-13.7.4 — Cascading Delete

- [ ] Delete a recorded page from the Corpus tab (UAT-12.6.3). All linked rows in `PageObjects` and snapshot tables are removed via `ON DELETE CASCADE`.

### UAT-13.7.5 — Approval Persistence

- [ ] Approve a proposal. Close and reopen. The proposal's approval state survives.

---

## 13.8 — Skill / Tools Wiring

### UAT-13.8.1 — Corpus Tools Available

- [ ] In stub-mode this is N/A. With a valid token, run **Analyze**.
- [ ] Inspect the LLM logs (or set the analyzer logger to Trace) and verify that the analyzer agent has access to the 5 corpus tools: `list_recorded_pages`, `get_page_snapshot`, `find_similar_elements`, `get_generated_controls`, `search_corpus`.

### UAT-13.8.2 — SessionContext Carries Site Identity

- [ ] Open Site A, run Analyze. Then open Site B and run Analyze.
- [ ] Each agent invocation reads the correct site from `ISessionContext`. Verify by inspecting tool-call logs (each shows the correct `SiteId`).

---

## 13.9 — Error Handling & Retry

### UAT-13.9.1 — Validation Retry (Generator)

- [ ] When a generator response fails Roslyn validation, the retry helper appends the error to the prompt and retries **once**.
- [ ] On final failure, the row shows `Status=Failed` and the error message is captured (no exception bubbles to the user as an unhandled crash).

### UAT-13.9.2 — Auth Required Surface (401/403)

- [ ] Force a 401 by setting an invalid token. Trigger Analyze.
- [ ] An `LlmAuthRequiredException` is classified internally; the UI surfaces an "Authentication required" notification.
- [ ] No retry is performed on auth errors.

### UAT-13.9.3 — Rate-Limited (429) with Retry-After

- [ ] (Best effort, hard to force) Simulate / observe a 429 response. The wrapper honours `Retry-After` and retries up to **3** times with exponential backoff before raising `LlmRateLimitedException`.
- [ ] Log entries show each retry attempt with the wait duration.

### UAT-13.9.4 — Token-Limit Truncation (Page Generator)

- [ ] Run page generation against a very large snapshot (paste a complex page). Observe that on `LlmTokenLimitException`, `PromptTruncator.TruncatePageObjectPrompt` is invoked and a Warning log entry indicates truncation occurred. The retry succeeds with the truncated prompt.
- [ ] If the prompt is still too large after truncation, the row reports a clear error rather than crashing.

### UAT-13.9.5 — Cancellation Tokens

- [ ] Cancel mid-LLM-call. The `CancellationToken` propagates and the call aborts within a few seconds. No background HTTP request continues to consume the token.

---

## 13.10 — Code Output

### UAT-13.10.1 — Project Structure

- [ ] After a successful pipeline run, check the configured output folder. It contains:
  - A `.csproj` file (with the deterministic name).
  - A `Controls/` folder with one `.cs` file per generated control.
  - A `Pages/` folder with one `.cs` file per generated page object.
  - Optionally a `Containers/` folder for inline containers.
- [ ] Files are written in deterministic order (stable diff-friendly).

### UAT-13.10.2 — Atomic Writes

- [ ] During a run, kill the process while writing output. On relaunch and rerun, no `.tmp` files remain in the output folder.

### UAT-13.10.3 — Orphan Cleanup

- [ ] Generate, then **delete** one approved proposal in the next analysis cycle and rerun the pipeline.
- [ ] The previously emitted `.cs` for the deleted control is removed from the output folder (orphan cleanup).

### UAT-13.10.4 — Path Safety

- [ ] Configure an output path inside the workspace (relative is fine). Triggering output never writes outside the configured root, even if a control name includes `..` or an absolute-looking string (this should be sanitized upstream — verify no traversal).

### UAT-13.10.5 — Compiles Out-of-the-Box

- [ ] Open the generated project in `dotnet build`. The build succeeds against the project's referenced Brinell.Html package version.

---

## End-to-End

### UAT-13.E2E.1 — New Site → Recorded → Pipeline → Compile

- [ ] Create a new site. Record 5+ pages from a real site. Run **Analyze Corpus**, approve all proposals, run **Generate All Pending**, run **Generate All** on the Page Objects tab, then trigger Code Output.
- [ ] The generated project builds with `dotnet build`. The resulting page objects expose meaningful, named properties.

### UAT-13.E2E.2 — Re-Run Stability

- [ ] Re-run the full pipeline on the same site without changing the corpus. Output files are byte-stable (identical hashes) on the second run for unchanged inputs.

---

## Sign-off

| Section                          | Tester | Date | Result |
|----------------------------------|--------|------|--------|
| 13.0 Copilot SDK Integration     |        |      |        |
| 13.1 ControlObject Analyzer      |        |      |        |
| 13.2 ControlObject Generator     |        |      |        |
| 13.3 Skill Auto-Generation       |        |      |        |
| 13.4 PageObject Generator        |        |      |        |
| 13.5 ControlObject Matcher       |        |      |        |
| 13.6 Pipeline Orchestrator       |        |      |        |
| 13.7 Data Persistence            |        |      |        |
| 13.8 Skill / Tools Wiring        |        |      |        |
| 13.9 Error Handling & Retry      |        |      |        |
| 13.10 Code Output                |        |      |        |
| End-to-End                       |        |      |        |
