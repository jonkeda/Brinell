# User Acceptance Tests — Phase 12 Wireup 02 (Control Objects: Analyze Corpus)

Manual test scenarios to verify `AnalyzeCorpusAsync` wiring in `ControlObjectsTabViewModel`: orchestrator call, busy-state behavior, status messaging, list reload, cancellation handling, and exception handling.

**Prerequisites:**

- Windows 10/11 with .NET 10 runtime
- At least 1 site with recorded snapshots
- Control Objects tab accessible in Workspace
- `PipelineOrchestrator` registered and injected
- For auth-specific tests: Copilot not signed in or invalid token

---

## W2.1 — Analyze Command Availability

### UAT-W2.1.1 — Enabled When Ready

- [ ] Open a site and navigate to **Control Objects**.
- [ ] Verify **Analyze Corpus** is enabled when not running and orchestrator is available.

### UAT-W2.1.2 — Disabled While Busy

- [ ] Click **Analyze Corpus**.
- [ ] While analysis is running, verify **Analyze Corpus** is disabled.
- [ ] Verify generation/regeneration actions are also effectively blocked by busy guard.

---

## W2.2 — Busy State and Status Message

### UAT-W2.2.1 — Start State

- [ ] Click **Analyze Corpus**.
- [ ] Status text changes to: `Analyzing corpus for control objects…`.
- [ ] `IsBusy` transitions to `true` (can be validated via UI disabled state).

### UAT-W2.2.2 — Completion State

- [ ] Allow analysis to complete.
- [ ] Status text changes to: `Analysis complete — {N} proposals found.`
- [ ] `IsBusy` returns to `false`.

---

## W2.3 — Orchestrator Integration

### UAT-W2.3.1 — Analyze Call Executes

- [ ] Trigger **Analyze Corpus**.
- [ ] In logs, verify analyze pipeline start/end entries from orchestrator/analyzer for the active site.
- [ ] Verify proposal count in logs matches the status message count.

### UAT-W2.3.2 — LoadControlObjects Reloads

- [ ] Before running, note current Control Objects list count.
- [ ] Run **Analyze Corpus**.
- [ ] After success, verify the list is reloaded and reflects latest persisted analysis proposals/merged generated controls.

---

## W2.4 — Cancellation Handling

### UAT-W2.4.1 — Cancel During Analysis

- [ ] Start **Analyze Corpus**.
- [ ] Trigger cancellation (navigate away/close operation path that cancels command token).
- [ ] Verify status becomes `Analysis cancelled.`
- [ ] Verify no crash and UI returns to interactive state (`IsBusy = false`).

---

## W2.5 — Error Handling

### UAT-W2.5.1 — General Exception Surface

- [ ] Force an analyze failure (e.g. temporary service misconfiguration or dependency failure).
- [ ] Verify status becomes `Analysis failed: {message}`.
- [ ] Verify error is logged with site id context.
- [ ] Verify `IsBusy` resets to `false` in failure path.

### UAT-W2.5.2 — Auth Required Path

- [ ] Run with auth unavailable/expired such that analyze fails through auth path.
- [ ] Verify operation fails gracefully (no unhandled exception dialog).
- [ ] Verify final status indicates failure and UI is usable after failure.

---

## W2.6 — Regression / Re-entry

### UAT-W2.6.1 — Repeat Analyze

- [ ] Run **Analyze Corpus** twice in a row.
- [ ] Verify second run starts normally after first completes.
- [ ] Verify status updates correctly on both runs and list reflects latest analysis each time.

### UAT-W2.6.2 — No Duplicate Execution While Busy

- [ ] Double-click **Analyze Corpus** rapidly.
- [ ] Verify only one run executes (single set of start/end logs for a single invocation window).

---

## Sign-off

| Section                                 | Tester | Date | Result |
| --------------------------------------- | ------ | ---- | ------ |
| W2.1 — Analyze Command Availability     |        |      |        |
| W2.2 — Busy State and Status Message    |        |      |        |
| W2.3 — Orchestrator Integration         |        |      |        |
| W2.4 — Cancellation Handling            |        |      |        |
| W2.5 — Error Handling                   |        |      |        |
| W2.6 — Regression / Re-entry            |        |      |        |
