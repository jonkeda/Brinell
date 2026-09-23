# User Acceptance Tests — Phase 5 (LLM Code Generation)

Manual test scenarios to verify end-to-end functionality of the Copilot SDK integration, corpus analysis, custom control generation, page object generation, and code validation.

**Prerequisites:**
- Windows 10/11 with .NET 10 runtime
- GitHub Copilot subscription (free tier or above) — the Copilot CLI must be able to authenticate
- A site corpus with **at least 3 recorded pages** containing forms, tables, navigation, and repeated UI patterns (e.g. date pickers, search bars). Use Phases 1–4 to record pages first.
- Internet access for Copilot CLI ↔ GitHub API communication

**Test site suggestion:** Record 5–10 pages from https://the-internet.herokuapp.com or an internal app with forms and repeated widgets.

---

## 5.0 — Copilot SDK Initialization

### UAT-5.0.1 — First Launch with Authentication

Verify that the Copilot SDK initializes and authenticates on startup.

- [ ] Launch the Scraper with a site that has recorded pages. Open View → Logs and set the filter to "Information".
- [ ] Within a few seconds of startup, a log entry appears: `Copilot SDK initialized — analyzer: {id}, generator: {id}` with two distinct session IDs.
- [ ] No error-level log entries related to Copilot appear. The Analyze 🔬 button in the toolbar is enabled.

### UAT-5.0.2 — Authentication Failure

Verify graceful handling when the Copilot CLI cannot authenticate.

- [ ] Set the environment variable `COPILOT_GITHUB_TOKEN` to an invalid value (e.g. `ghp_invalid`). Launch the Scraper.
- [ ] An error log entry appears indicating authentication failure. The Analyze 🔬 button is disabled or shows an error when clicked.
- [ ] The rest of the application (browsing, recording, inspecting) continues to work normally.
- [ ] Remove the invalid env var and relaunch. Authentication succeeds.

---

## 5.1 — Corpus Analysis

### UAT-5.1.1 — Run Analysis on a Recorded Corpus

Trigger the LLM analysis pass and verify that control proposals are returned.

- [ ] Open a site with at least 3 recorded pages. Click the 🔬 Analyze button in the toolbar.
- [ ] The status text updates to "Analyzing corpus..." and the Analyze button is disabled while analysis is in progress.
- [ ] The log viewer shows an `LLM request — Agent: analyzer` entry with the prompt length, followed by an `LLM response — Agent: analyzer` entry with the response length and elapsed time.
- [ ] After a few seconds (typically 10–30s), the analysis completes. The status text updates to "Found N control patterns" where N ≥ 0.

### UAT-5.1.2 — Proposed Controls List

Verify the analysis results populate the UI with actionable proposals.

- [ ] After analysis, a list of proposed custom controls appears. Each proposal shows: a name (PascalCase, e.g. "DatePickerControl"), a DOM signature (CSS-like pattern), frequency count, and confidence percentage.
- [ ] Each proposal has Approve ✓ and Reject ✗ actions.
- [ ] An "Approve All" action is available and enabled when proposals exist.

### UAT-5.1.3 — Locator Report

Verify that the locator stability report is populated.

- [ ] After analysis, a locator report section appears (if the LLM returned one). It shows:
  - Stable attributes (e.g. `data-testid`, `aria-label`)
  - Unstable attributes (e.g. `id (dynamic on N pages)`)
  - Recommendations text

### UAT-5.1.4 — Approve and Reject Controls

Exercise the approval workflow.

- [ ] Click Approve ✓ on one proposed control. Its `IsApproved` state changes (visual indicator, e.g. green check or highlight). A log entry confirms: `Control approved — Name: {name}`.
- [ ] Click Reject ✗ on another proposed control. Its `IsApproved` state is false. A log entry confirms: `Control rejected — Name: {name}`.
- [ ] Click "Approve All". All remaining proposals are marked approved. A log entry confirms: `All controls approved — Count: N`.

### UAT-5.1.5 — Analysis with Empty Corpus

Verify graceful behavior when no pages are recorded.

- [ ] Create a new site with zero recorded pages. Click Analyze 🔬.
- [ ] The analysis runs but returns zero proposals (the LLM has nothing to analyze). The status shows "Found 0 control patterns". No error occurs.

### UAT-5.1.6 — Analysis Logging

Verify structured logging for the full analysis pipeline.

- [ ] After a successful analysis, check the log viewer for these entries (in order):
  1. `Analysis started — Pages: N`
  2. `LLM request — Agent: analyzer, Prompt length: X chars`
  3. `LLM response — Agent: analyzer, ... Elapsed: Y ms`
  4. `Analysis completed — Patterns found: ..., Custom controls proposed: N, Elapsed: Z ms`
- [ ] Check the JSON log file on disk. The same entries are present with structured properties.

---

## 5.2 — Custom Control Generation

### UAT-5.2.1 — Generate a Single Custom Control

Generate a custom control from an approved proposal.

- [ ] After approving at least one control proposal, open Site → Manage Controls.
- [ ] Click "Generate Pending". The LLM generates code for each approved-but-not-yet-generated control.
- [ ] The log viewer shows `LLM request — Agent: generator` and `LLM response — Agent: generator` entries for each control being generated.
- [ ] After generation completes, a log entry appears: `Generation — Control: {name}`.
- [ ] The generated control appears in the Controls list with its name, namespace, and DOM signature.

### UAT-5.2.2 — View Generated Control Code

Verify the generated code is viewable.

- [ ] In the Manage Controls view, select a generated control from the list.
- [ ] The code preview panel shows the full C# source code for the control.
- [ ] The code contains: a `sealed class` declaration, `ContainerBase` inheritance, expression-bodied properties, and Locator-based element access.
- [ ] The code uses `file-scoped namespace` syntax.

### UAT-5.2.3 — Control Stored in Registry

Verify the generated control is persisted in the SQLite database.

- [ ] After generating a control, close and relaunch the Scraper.
- [ ] Open Site → Manage Controls. The previously generated control still appears in the list with the same code.
- [ ] The sidebar's Controls section also shows the control name.

### UAT-5.2.4 — Skills System — SKILL.md Generation

Verify that skills files are created for future LLM sessions.

- [ ] After generating controls, check the logs for `SkillService` entries.
- [ ] On first launch, a `brinell-conventions` SKILL.md was created (check logs).
- [ ] After generating site-specific controls, a `{site}-controls` SKILL.md was created with the custom control definitions.

### UAT-5.2.5 — Regenerate a Control

Verify that an existing control can be regenerated.

- [ ] In Manage Controls, select an existing control and click "Regenerate".
- [ ] The LLM generates new code for the control. The code preview updates.
- [ ] The new code replaces the old code in the registry (verify by restarting the app).

### UAT-5.2.6 — Validation Retry on Generation Failure

Verify that the auto-retry logic kicks in when generated code has syntax errors.

- [ ] (This is hard to trigger intentionally. Check the logs after any generation run.)
- [ ] If a generated control had Roslyn syntax errors, the log should show: `Generated control has errors, retrying — Name: {name}, Errors: N`.
- [ ] The retry produces a corrected version. If both retries fail, the control is still stored but with validation warnings.

---

## 5.3 — Page Object Generation

### UAT-5.3.1 — Generate a Single Page Object

Generate a page object from a recorded snapshot.

- [ ] With approved custom controls already generated, navigate to a recorded page and trigger page generation (via the Generation view or batch command).
- [ ] The log viewer shows: `Generation — Page: {ClassName}, Custom controls available: {names}`.
- [ ] After generation, a `PageGenerationResult` appears with: ClassName, Namespace, MainCode, Validation result.

### UAT-5.3.2 — Generated Code Quality

Inspect the generated page object code for correctness.

- [ ] The code contains a `sealed class` with `HtmlPageObjectBase<T>` inheritance.
- [ ] Each actionable element (input, button, select, textarea, link) has an expression-bodied property.
- [ ] Control types are appropriate: `TextInputControl` for text inputs, `ButtonControl` for buttons, `SelectControl` for selects, etc.
- [ ] Locators follow the preference order: `ByText` > `ByDataTestId` > `ByAriaLabel` > `ById` > `ByCss`.
- [ ] If the page contained patterns matching a custom control, the custom control type is used instead of a generic control type.
- [ ] The code compiles when pasted into a real Brinell project (manual check).

### UAT-5.3.3 — Batch Generation

Generate page objects for multiple pages at once.

- [ ] Trigger batch generation for all recorded pages (or a selection).
- [ ] Progress updates appear: "Generating 1/N...", "Generating 2/N...", etc.
- [ ] After completion, the status shows: "Complete — X succeeded, Y failed".
- [ ] The log shows a summary: `Generation batch — Completed: X, Failed: Y`.

### UAT-5.3.4 — Container Group Generation

Verify that container groups (repeated sections) produce separate ContainerBase classes.

- [ ] Record a page that has repeating row/card/list-item structures (e.g. a table or card grid).
- [ ] Generate the page object. The LLM response should include multiple `csharp` code blocks.
- [ ] The first code block is the main PageObject. Subsequent blocks are ContainerBase classes for repeated structures.

---

## 5.4 — Code Validation (Roslyn)

### UAT-5.4.1 — Valid Code Passes

Verify that correctly generated code passes validation.

- [ ] After generating a page object, check its `Validation.IsValid` is `true` (no error indicator in the UI, or check logs).
- [ ] The `Errors` list is empty. `Warnings` may be present (e.g. ByCss usage).

### UAT-5.4.2 — ByCss Warnings

Verify that `ByCss` locators produce warnings.

- [ ] If a generated page uses `Locator.ByCss(...)` for any element, a warning appears: "ByCss is a last-resort locator. Consider ByText, ByDataTestId, or ByAriaLabel instead."
- [ ] The warning includes the line number where `ByCss` was used.
- [ ] The code is still marked as valid (warnings ≠ errors).

### UAT-5.4.3 — Unknown Control Type Warning

Verify that unrecognized control types produce warnings.

- [ ] If the LLM generates a control type that is not in the built-in list or the custom control registry, a warning appears: "Unknown control type: '{TypeName}'."
- [ ] The code is still marked as valid.

### UAT-5.4.4 — Auto-Retry on Syntax Errors

Verify that syntax errors trigger an automatic retry prompt.

- [ ] (Check logs after generation runs.) If code had missing braces or invalid expressions, the log shows:
  1. `Code validation failed (attempt 1/2), retrying — Errors: N`
  2. A second `LLM request — Agent: generator` with the error feedback prompt
  3. Either a successful retry or `Code validation failed after 2 retries`.

---

## 5.5 — Corpus Query Tools (LLM Tool Calls)

### UAT-5.5.1 — Tool Invocations During Analysis

Verify that the LLM calls corpus tools during analysis.

- [ ] During an analysis run, check the log viewer for tool-related entries (the Copilot SDK logs tool invocations).
- [ ] The LLM should call `list_recorded_pages` to discover available pages.
- [ ] The LLM should call `get_page_snapshot` for at least some pages to inspect their DOM.
- [ ] The LLM may call `search_corpus` to find repeated patterns.
- [ ] The LLM may call `get_generated_controls` to check for existing custom controls.

### UAT-5.5.2 — Tool Invocations During Generation

Verify that the LLM can call corpus tools during page generation.

- [ ] During a page generation run, check if the LLM called any corpus tools (it may or may not, depending on the prompt). Tool calls appear in the log.

---

## 5.6 — End-to-End Workflow

### UAT-5.6.1 — Full Pipeline: Record → Analyze → Generate Controls → Generate Pages

Run the complete Phase 5 workflow from start to finish.

1. [ ] Create a new site and record at least 5 pages with varied content (forms, tables, navigation menus, repeated widgets).
2. [ ] Click 🔬 Analyze. Wait for analysis to complete. Review the proposed controls.
3. [ ] Approve the controls that look correct. Reject any that are noise.
4. [ ] Open Site → Manage Controls. Click "Generate Pending". Wait for all controls to be generated. Review the code.
5. [ ] Trigger batch page generation for all recorded pages. Wait for completion.
6. [ ] Review the generated page objects:
   - Do they use the correct custom controls where applicable?
   - Do they have sensible property names?
   - Do locators use the preferred strategy (ByText > ByDataTestId > etc.)?
   - Does the code compile (paste into a Brinell project)?
7. [ ] Close and relaunch the app. Verify all generated controls are still in the registry and the corpus is intact.

### UAT-5.6.2 — Iterative Refinement

Verify that re-running analysis after generating controls produces improved results.

- [ ] After generating custom controls, run analysis again on the same corpus.
- [ ] The LLM now sees existing custom controls via `get_generated_controls`. It may propose fewer new controls or refine existing proposals.
- [ ] Generating pages after this second analysis should use the updated custom controls.

---

## 5.7 — Error Handling & Edge Cases

### UAT-5.7.1 — Network Interruption During LLM Call

- [ ] Start an analysis or generation run. Disconnect the network mid-request.
- [ ] An error appears in the status text and log. The application does not crash.
- [ ] Reconnect the network. The next analysis/generation run succeeds.

### UAT-5.7.2 — Cancellation

- [ ] Start an analysis run. If a cancel button is available, click it.
- [ ] The operation stops gracefully. The status text reflects the cancellation.

### UAT-5.7.3 — Very Large Corpus

- [ ] Record 20+ pages with complex DOM structures (100+ elements each).
- [ ] Run analysis. Verify it completes without timing out (may take 1–2 minutes).
- [ ] Run batch generation. Verify all pages are processed sequentially without memory issues.

### UAT-5.7.4 — Empty LLM Response

- [ ] (Hard to trigger intentionally.) If the LLM returns an empty response, check that:
  - Analysis returns zero proposals (not a crash)
  - Generation reports a failure ("No C# code blocks in LLM response") in the log and validation result
