# Test 5.1 — Corpus Query Tools Tests

**Covers:** Step 5.3 — `CorpusTools` (search_corpus, get_page_snapshot, find_similar_elements, get_generated_controls, list_recorded_pages)

**File:** `Brinell.Scraper.Tests/Services/CorpusToolsTests.cs`

## Test Inventory (8 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `SearchCorpus_ReturnsMatchingElements` | Seed corpus with 3 pages containing `<input>` elements; `search_corpus("input")` returns all matching elements with page context |
| 2 | `SearchCorpus_FilterByTag_ReturnsOnlyMatchingTag` | Seed corpus with `<input>`, `<button>`, `<select>` elements; `search_corpus("", tag: "button")` returns only `<button>` elements |
| 3 | `SearchCorpus_NoResults_ReturnsEmpty` | `search_corpus("nonexistent-element")` returns empty formatted string or "no results" message |
| 4 | `GetPageSnapshot_ReturnsFormattedDom` | Seed snapshot with nested DOM tree; `get_page_snapshot(snapshotId)` returns formatted HTML-like output with URL, title, element count, and indented elements |
| 5 | `GetPageSnapshot_InvalidId_ReturnsNotFound` | `get_page_snapshot(999)` returns "No snapshot found" message |
| 6 | `FindSimilarElements_ReturnsCrossPageMatches` | Seed 5 pages, 3 of which contain `div.date-picker`; `find_similar_elements("div.date-picker", minCount: 2)` returns matches from those 3 pages |
| 7 | `GetGeneratedControls_ReturnsRegisteredControls` | Register 2 custom controls in registry; `get_generated_controls()` returns formatted list with names, DOM signatures, namespaces |
| 8 | `ListRecordedPages_ReturnsMarkdownTable` | Seed 4 snapshots; `list_recorded_pages()` returns a markdown table with ID, page name, URL, element count, captured date columns |

## Notes

- Use in-memory SQLite for `CorpusService` and `ControlRegistry`.
- Pre-seed the database with realistic test data (multi-level DOM trees, various element types).
- `CorpusTools.Initialize()` must be called with the test services before running handlers.
- Verify output formatting: HTML-like element output includes only non-null attributes.
- For `find_similar_elements`, verify the `minCount` threshold is respected.
- `get_generated_controls` with empty registry should return "No custom controls" message.
- All handlers are async — use `await` in tests.
