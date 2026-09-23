# Phase 10 — Automated (Integration) Tests

## Goal

End-to-end integration tests that verify the full corpus-based pipeline: WebView2 → DOM capture → SQLite corpus → analyzer agent → custom control generation → page generation → Roslyn compilation.

## Tasks

### 10.1 — Integration test: WebView2 loads page → DOM capture returns valid snapshot

Host a local test HTML page (embedded resource or local file served via a minimal HTTP listener), load it in WebView2, execute the DOM capture JavaScript, and assert the returned snapshot contains expected elements.

**Implementation details:**

- Create a set of test HTML pages under `TestData/Pages/` as embedded resources (e.g., `simple-form.html`, `table-with-controls.html`, `nested-components.html`).
- Use a test fixture class `WebView2TestFixture` that:
  - Creates a hidden WPF `Window` hosting a `WebView2` control.
  - Manages `CoreWebView2` initialization (`EnsureCoreWebView2Async`).
  - Provides `NavigateToEmbeddedPage(string resourceName)` and `RunDomCaptureAsync()` helpers.
  - Disposes the window and WebView2 on teardown.
- WebView2 requires an STA thread — the fixture must marshal test execution onto an STA `Dispatcher` or use `STAThreadAttribute` with a custom xUnit synchronization context.
- Assertions:
  - Snapshot is non-null and non-empty.
  - Snapshot contains expected top-level element tags (`<form>`, `<table>`, etc.).
  - Element attributes (`id`, `name`, `class`, `type`) are captured.
  - Nested children are present at expected depth.
  - Elements with `aria-*` attributes and `data-testid` are captured.
- **Corpus round-trip:** After capture, store the snapshot in the SQLite corpus via `ICorpusStore.SavePageAsync()`. Retrieve it back via `ICorpusStore.GetPageAsync()` and assert the retrieved snapshot matches the original (element count, attributes, structure). This verifies serialization/deserialization fidelity.

### 10.2 — Integration test: Corpus → analyzer agent → pattern proposals

Build a test corpus with multiple pages, run the analysis pipeline through the analyzer agent (mock), and verify the proposals identify expected patterns from the test pages.

**Implementation details:**

- Create a test corpus in a temp SQLite database with 3–4 test pages (`simple-form.html`, `table-with-controls.html`, `nested-components.html`, `repeated-card-list.html`).
- DOM snapshots are loaded from `TestData/Snapshots/` and stored in the corpus via `ICorpusStore`.
- Run the analyzer agent via `IAnalyzerAgent.AnalyzeCorpusAsync(corpus)` using the mock analyzer.
- **Mock analyzer** returns structured JSON with control proposals (e.g., `CardControl` from repeated `.card` elements, `DataTableControl` from `<table>` patterns).
- **Live analyzer path (opt-in):** Tests marked with `[Trait("Category", "LiveLLM")]` call the real analyzer agent.
- Assertions:
  - Proposals are non-empty.
  - At least one proposal identifies the repeated card pattern from `repeated-card-list.html`.
  - At least one proposal identifies the table pattern from `table-with-controls.html`.
  - Each proposal has a name, description, and list of matched pages.
  - Proposals reference valid page IDs from the corpus.

### 10.3 — Integration test: custom controls + page objects → write to project → `dotnet build` succeeds

Generate both custom controls and page objects, write to a temp project with `Controls/` and `Pages/` subfolders, run `dotnet build`, and assert exit code 0.

**Implementation details:**

- Create a temp directory per test run via `Path.GetTempPath()` + `Path.GetRandomFileName()`.
- Scaffold a minimal `.csproj` that references the required Brinell NuGet packages (or project references for local dev):
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="Brinell.Html" Version="*" />
    </ItemGroup>
  </Project>
  ```
- Generate custom controls via `IGeneratorAgent.GenerateControlsAsync(approvedProposals)` (mock generator returns valid C# `ControlObject` classes).
- Generate page objects via `IGeneratorAgent.GeneratePagesAsync(corpus, controls)` (mock generator returns valid C# `PageObject` classes that reference the custom controls).
- Write custom control `.cs` files into `Controls/` subfolder.
- Write page object `.cs` files into `Pages/` subfolder.
- Assert the project structure:
  - `Controls/` contains at least one `.cs` file.
  - `Pages/` contains at least one `.cs` file.
  - Page objects contain `using` statements or references to the custom control types.
- Run `dotnet build` via `Process.Start` with captured stdout/stderr.
- Assert exit code is 0 and stderr contains no error lines.
- Clean up the temp directory in the test teardown (`IDisposable` or `IAsyncLifetime`).
- Consider a shared `TempProjectFixture` that handles scaffold + cleanup to reduce boilerplate.

### 10.4 — Integration test: corpus-based re-record & diff detects changes

Record a page to the corpus, modify the test HTML, re-record, and verify the diff algorithm correctly identifies added/removed/changed elements using corpus-stored snapshots.

**Implementation details:**

- Record `simple-form.html` (3 inputs: name, email, phone) to the corpus via WebView2 capture → `ICorpusStore.SavePageAsync()`.
- Modify the test HTML: remove phone, add address, change email's type.
- Re-record the modified page to the corpus (same URL/page ID, new version).
- Retrieve both versions from the corpus: `ICorpusStore.GetPageVersionsAsync(pageId)`.
- Run the diff service: `var diff = diffService.Compare(versionBefore, versionAfter)`.
- Assert:
  - `diff.Added` contains the address element.
  - `diff.Removed` contains the phone element.
  - `diff.Changed` contains the email element with the type change detail.
  - `diff.Unchanged` contains the name element.
- Test edge cases:
  - First recording (no previous version — all elements are "added").
  - Identical re-record (no changes).
  - Elements reordered but not changed (should not appear as changed).

### 10.5 — Smoke test: full corpus-based pipeline end-to-end

Full end-to-end pipeline test covering the entire corpus-based workflow. This is the critical smoke test that validates the complete pipeline.

**Implementation details:**

- Use two self-contained test HTML pages (`smoke-login-page.html` with form inputs/buttons, `smoke-dashboard-page.html` with tables/cards/navigation).
- Pipeline steps executed in sequence:
  1. **Navigate & Record:** Load `smoke-login-page.html` in WebView2, run DOM capture, store in SQLite corpus.
  2. **Record second page:** Load `smoke-dashboard-page.html`, capture, store in corpus (building a multi-page corpus).
  3. **Analyze:** Run analyzer agent (mock) on the corpus → receive pattern proposals (e.g., `CardControl` from dashboard cards).
  4. **Approve:** Auto-approve all proposed controls.
  5. **Generate controls:** Run generator agent (mock) → receive custom control C# code.
  6. **Generate pages:** Run generator agent (mock) with corpus + custom controls → receive page object C# code that references custom controls.
  7. **Write to project:** Write to temp project with `Controls/` and `Pages/` subfolders.
  8. **Build:** Run `dotnet build`, assert exit code 0.
  9. **Verify structure:** Assert `Controls/` has at least one `.cs` file, `Pages/` has two `.cs` files (one per recorded page).
- This test is marked `[Trait("Category", "Smoke")]` for easy filtering.
- Timeout: allow up to 60 seconds for live LLM variant; mock variant should complete in < 10 seconds.
- On failure, capture and log: the corpus state, the analyzer/generator prompts sent, the agent responses, and any Roslyn diagnostics.

### 10.6 — Integration test: Copilot SDK custom tools

Verify that corpus-related custom tools register correctly and return expected data from a test corpus.

**Implementation details:**

- Create a test corpus with 2–3 pages stored in SQLite.
- Register the custom tools with a test LLM session: `search_corpus`, `get_page_snapshot`, `list_pages`, `get_page_diff`.
- Invoke each tool programmatically and assert results:
  - `search_corpus("form")` → returns pages containing form elements.
  - `get_page_snapshot(pageId)` → returns the full snapshot for the given page.
  - `list_pages()` → returns all page IDs and URLs in the corpus.
  - `get_page_diff(pageId)` → returns diff between versions (if multiple versions exist).
- Verify tool schemas are valid JSON Schema and match expected parameter/return types.
- **Mock LLM session test:** Send a mock LLM response that references tool results (e.g., "Based on the snapshot from `get_page_snapshot`, I found 3 form inputs..."). Verify the tool invocation round-trip works: LLM requests tool → tool executes → result sent back to LLM → LLM produces final response.

### 10.7 — Integration test: incremental re-generation

Verify that re-generation after a page change only regenerates the changed page, skipping unchanged pages.

**Implementation details:**

- Record 3 test pages to the corpus: `page-a.html`, `page-b.html`, `page-c.html`.
- Run the full pipeline: analyze → approve → generate controls → generate pages. All 3 pages produce page objects.
- Save the generated output (file contents + timestamps or hashes).
- Modify `page-b.html` (add a new input field), re-record to corpus.
- Run incremental re-generation via `IPipelineOrchestrator.RegenerateChangedAsync(corpus)`.
- Assert:
  - `page-b` page object was regenerated (content differs from original).
  - `page-a` and `page-c` page objects were **not** regenerated (content unchanged, skipped).
  - The pipeline reports which pages were regenerated and which were skipped.
- Verify the updated project still compiles: `dotnet build` succeeds after incremental re-generation.

## Test Infrastructure

### Framework

- **xUnit** as the test framework, consistent with all other Brinell test projects.
- Tests live in `testsnew/Brinell.Scraper.Tests/` following the existing convention.

### Test HTML Pages

- Stored as embedded resources in the test assembly under `TestData/Pages/`.
- Alternatively, served via a local `HttpListener` on a random port for tests that need real HTTP navigation.
- Pages should be minimal but representative — avoid external dependencies (no CDN links, inline all CSS/JS).

### LLM Mocking Strategy

- **Two-model architecture:** The pipeline uses separate agents — an **analyzer agent** (identifies patterns, proposes custom controls) and a **generator agent** (produces C# code). Each agent may use a different model configuration.
- **`MockAnalyzerAgent : IAnalyzerAgent`**: Returns pre-recorded structured JSON responses with control proposals. Keyed by corpus hash or test name. Stored as `.proposals.json` files under `TestData/MockResponses/`.
- **`MockGeneratorAgent : IGeneratorAgent`**: Returns pre-recorded C# code responses for both custom controls and page objects. Stored as `.cs.txt` files under `TestData/MockResponses/`.
- **Deterministic tests** (default): Use mock agents. Fast, offline, no API cost.
- **Live LLM tests**: Use real agent implementations with GitHub Copilot SDK. Marked with `[Trait("Category", "LiveLLM")]`, excluded from CI by default via test filter. Verify that analyzer and generator agents receive their respective model configurations (e.g., different model names, temperatures).
- Mock responses are updated manually when prompt format or proposal schema changes.

### Temp Directory Management

- Each test that generates files creates a unique temp directory.
- Cleanup via `IAsyncLifetime.DisposeAsync()` or a shared `TempDirectoryFixture`.
- On test failure, optionally preserve the temp directory and log its path for debugging (controlled by env var `BRINELL_KEEP_TEST_OUTPUT=1`).

### WebView2 in Test Context

- WebView2 requires an STA thread with a message pump.
- Options:
  1. Custom xUnit `SynchronizationContext` that runs tests on an STA thread.
  2. `WebView2TestFixture` that creates a `Dispatcher` and marshals calls.
  3. Use `Microsoft.Web.WebView2.DevToolsProtocolExtension` for headless-like control if available.
- The `WebView2TestFixture` is shared via xUnit `IClassFixture<>` to avoid reinitializing WebView2 per test.

## Acceptance Criteria

- [ ] All integration tests pass with mock agents in CI.
- [ ] Live LLM tests pass when run manually with valid API credentials.
- [ ] WebView2 tests initialize and tear down cleanly without orphaned processes.
- [ ] Temp directories are cleaned up after test runs (unless `BRINELL_KEEP_TEST_OUTPUT` is set).
- [ ] Corpus round-trip: snapshots stored and retrieved from SQLite match the originals (10.1).
- [ ] Analysis pipeline: mock analyzer returns proposals that correctly reference test corpus pages (10.2).
- [ ] Custom control + page generation: generated project with `Controls/` and `Pages/` compiles (10.3).
- [ ] Corpus-based diff: re-recorded pages produce correct diffs from corpus versions (10.4).
- [ ] Smoke test (10.5) completes the full corpus-based pipeline: record → analyze → approve → generate controls → generate pages → build.
- [ ] Copilot SDK custom tools return correct data from test corpus (10.6).
- [ ] Incremental re-generation only regenerates changed pages, skips unchanged (10.7).
- [ ] Test HTML pages are self-contained with no external dependencies.
- [ ] Tests run on Windows (WebView2 requirement) — CI agent must have WebView2 runtime installed.
- [ ] No test takes longer than 60 seconds (mock path < 10 seconds).

## Dependencies

- **Phase 3** (DOM Capture) — DOM capture JavaScript must be functional.
- **Phase 4** (Corpus) — `ICorpusStore`, SQLite storage, page versioning must be implemented.
- **Phase 5** (Analysis + Generation) — `IAnalyzerAgent`, `IGeneratorAgent`, Copilot SDK custom tools, two-model configuration.
- **Phase 8** (Pipeline Orchestrator) — `IPipelineOrchestrator`, incremental re-generation, diff service.
- **WebView2 Runtime** — Must be installed on the test machine.
- **xUnit** + `Microsoft.NET.Test.Sdk` — Test framework packages.
- **Roslyn** (`Microsoft.CodeAnalysis.CSharp`) — For in-process compilation verification.
