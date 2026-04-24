# User Acceptance Tests — Phases 1–4

Manual test scenarios to verify end-to-end functionality of the Brinell Scraper through Phase 4. Run these after each milestone build.

**Prerequisites:** Windows 10/11 with .NET 10 runtime, internet access for WebView2 navigation, and a test website with forms, tables, lists, and navigation (e.g. https://the-internet.herokuapp.com or any internal site).

---

## Phase 1 — WPF Shell & Embedded Browser

### UAT-1.1 — Application Startup

Launch `Brinell.Scraper.exe` and verify the initial state of the application before any site is selected.

- [X] The window opens at 1280×800, centered on screen, with the title "Brinell Scraper".
- [X] The start screen shows the site selector view with a list of existing site corpora (or an empty list on first run) and a "New Site..." button.
- [X] The navigation toolbar (◀ ▶ ↻, address bar, Inspect/Record/Analyze buttons) is not visible — it only appears after selecting a site.
- [X] The status bar is visible at the bottom of the window.

### UAT-1.2 — Create New Site Corpus

Create a new site corpus from the start screen and verify the app transitions into the browser workspace.

- [X] Click "New Site..." and confirm the New Site dialog opens with fields for site name, start URL, namespace, output path, and URL aliases.
- [X] Enter a site name (e.g. "Test Site") and a start URL (e.g. "https://the-internet.herokuapp.com"), then click Create. The dialog closes and the site appears in the list.
- [X] The site is auto-selected and the browser view activates — WebView2 loads the start URL, the toolbar becomes visible, and the sidebar shows the site name with "0 pages · 0 controls".

### UAT-1.3 — Browser Navigation

With a site open, exercise the full navigation workflow in the embedded WebView2 browser.

- [X] Type a new URL in the address bar and press Enter. The browser navigates to that URL, the address bar updates to reflect the final URL, and the status bar shows it.
- [X] Click the ◀ Back button and confirm the browser goes back. The button disables when there is no more history.
- [X] Click the ▶ Forward button and confirm the browser goes forward. The button disables when there is no more forward history.
- [X] Click ↻ Refresh and confirm the page reloads.
- [X] Click the "Go" button and confirm it navigates to whatever URL is in the address bar.
- [X] Type an invalid URL (e.g. "not-a-url") and press Enter. The status bar shows a navigation error and the application does not crash.
- [X] Type a URL without a scheme (e.g. "example.com") and press Enter. The browser auto-prepends `https://` and navigates successfully.

### UAT-1.4 — Session Persistence

Verify that WebView2 cookies and session state survive across application restarts.

- [X] Navigate to a site that requires login and log in successfully inside WebView2.
- [X] Close the application, relaunch it, and select the same site. The user should still be logged in without needing to re-enter credentials.

### UAT-1.5 — Site Selector & Switching

Verify switching between sites and that site data persists.

- [X] With a site open, click Site → Switch Site. The site selector view reappears.
- [X] Select a different site (or create a new one). The browser navigates to the new site's start URL and the sidebar updates.
- [X] Confirm the previously created site still appears in the list and its data is intact. Sites persist across application restarts.
- [ ] Double-click a site in the list. It opens immediately without needing to click the "Open" button.

### UAT-1.6 — Window Chrome

Verify the status bar, loading indicator, developer tools, and layout resizing.

- [X] While navigating, a loading indicator (progress bar) appears in the status bar. It disappears after navigation completes and the status bar shows the current URL.
- [X] Click the F12 (Dev Tools) toolbar button. The WebView2 Developer Tools window opens.
- [X] Drag the grid splitter between the sidebar and browser. The sidebar width changes and the browser content area adjusts accordingly.

### UAT-1.7 — New Window Handling

Verify that links which would normally open in a new tab or window are captured inside the Scraper.

- [ ] Navigate to a page with a link that has `target="_blank"`. Click the link. The page opens inside the current browser pane — no external browser window is launched.
- [ ] Navigate to a page that calls `window.open()` via JavaScript. The new content loads inside the Scraper browser instead of spawning a new window.

### UAT-1.8 — Edit Site

Verify that an existing site's properties can be edited after creation.

- [X] On the site selector screen, click the Edit button next to an existing site. The Edit Site dialog opens pre-filled with the site's current name, URL, namespace, output path, and aliases.
- [X] Change the site name and click Save. The list updates to show the new name. Reopening the edit dialog confirms the change persisted.

---

## Phase 3 — Logging

### UAT-3.1 — In-App Log Viewer

Toggle the log viewer on and off, verify log entries appear in real time, and test filtering and clearing.

- [ ] Click View → Logs. The log viewer panel appears at the bottom of the window with a level filter dropdown and a Clear button.
- [X] Log entries appear with four columns: timestamp (HH:mm:ss.fff), colored log level, source name, and message text.
- [X] Navigate to a URL. New log entries appear in real time showing "Navigating to {url}" and "Navigation completed".
- [X] Change the level filter to "Warning". Debug and Information entries disappear; only Warning and Error entries remain.
- [X] Change the filter to "Error". Only Error-level entries are visible. Change it back to "Debug" and all entries reappear.
- [X] Click "Clear". All log entries are removed from the viewer.
- [X] Uncheck View → Logs. The log viewer panel collapses and hides.
- [ ] Drag the grid splitter above the log viewer panel. The panel height changes and the browser/content area adjusts accordingly.

### UAT-3.2 — File Logging

Verify that structured log files are written to disk.

- [X] After navigating to several pages, open the `logs/` folder next to the executable. A daily rolling JSON log file should exist with a name like `scraper-20260422.json`.
- [X] Open the log file and confirm it contains structured JSON entries with timestamp, level, message template, and properties.

### UAT-3.3 — Log Level Colors

Verify that each log level is visually distinct in the log viewer.

- [X] Debug-level entries appear in gray text.
- [X] Information-level entries appear in dark blue text.
- [X] Warning-level entries (e.g. triggered by navigating to an invalid URL) appear in dark orange text.
- [X] Error-level entries appear in red text.

---

## Phase 4A — DOM Inspection & Recording

### UAT-4.1 — DOM Snapshot Capture

Navigate to a content-rich page and trigger a DOM capture to verify the snapshot pipeline works end to end.

- [X] Navigate to a page with forms, inputs, and visible text content. The page loads fully.
- [X] Click 🔍 Inspect to activate inspect mode and trigger a DOM capture.
- [X] Check the log viewer for a capture entry: "DOM capture — URL: {url}, Elements: {count}, Size: {bytes} bytes, Elapsed: {ms} ms".
- [X] Verify the elapsed time is under 2 seconds for pages with up to 5,000 elements.

### UAT-4.2 — Element Highlight Overlay

With inspect mode active, verify that hovering and clicking elements in the browser produces the expected visual overlays and locator suggestions.

- [X] Hover over an element in the browser. A blue border with a light blue background overlay appears around the hovered element.
- [X] A tooltip appears below the element showing: tag name, id (if any), aria-label (if any), type (if any), and a suggested locator string (e.g. `Locator.ByText("Email:")`).
- [X] Move the mouse to a different element. The overlay and tooltip follow the cursor to the new element.
- [X] Hover over an element with a `data-testid` attribute. The locator suggestion reads `Locator.ByDataTestId("...")`.
- [X] Hover over an element with an `aria-label` attribute. The locator suggestion reads `Locator.ByAriaLabel("...")`.
- [X] Ctrl+click an element. It gets a persistent green border indicating selection. Ctrl+click the same element again and the green border is removed.
- [X] Toggle Inspect mode off. All overlays, tooltips, and green selection borders are removed from the page.

### UAT-4.2a — iFrame Overlay Support

Verify that the highlight overlay works inside iframes.

- [ ] Navigate to a page containing one or more iframes. Enable inspect mode.
- [ ] Hover over an element inside an iframe. The overlay appears within the iframe and the tooltip shows `[iframe]` as a prefix before the tag info.
- [ ] Ctrl+click an element inside an iframe. It receives a persistent green selection border. The selection coordinates map correctly to page-level position (no offset errors).
- [ ] Navigate to a different page and back. The iframe overlay is re-injected correctly on return.

### UAT-4.2b — Locator Suggestion Priority

Verify the tooltip shows the correct locator strategy based on the element’s attributes.

- [ ] Hover over an element with a `data-testid` attribute. The locator suggestion reads `Locator.ByDataTestId("...")` (highest priority).
- [ ] Hover over an element with a stable `id` (no GUIDs, long numbers, or dynamic suffixes). The locator suggestion reads `Locator.ById("...")`. Hover over an element with a dynamic-looking `id` (e.g. containing a GUID or `_123`). The locator does NOT suggest `ById` — it falls through to a lower-priority strategy.
- [ ] Hover over an `<input>` that has an associated `<label>` element. The locator suggestion reads `Locator.ByLabel("...")` with the label text.
- [ ] Hover over an element with an `aria-label` attribute. The locator suggestion reads `Locator.ByAriaLabel("...")`.
- [ ] Hover over an element with none of the above. The locator falls back to a minimal CSS selector.

### UAT-4.3 — DOM Tree View

Verify that the DOM tree panel renders the captured element hierarchy and supports filtering and browser sync.

- [X] With inspect mode active, the DOM tree panel appears showing the full DOM hierarchy starting from `<html>`. Each node displays its tag name with id and class attributes color-coded.
- [X] Expand a node to see its children. Collapse it to hide them.
- [ ] Type a filter term (e.g. "input") in the filter box. The tree filters to show only matching elements along with their ancestor path. Clear the filter and the full tree is restored.
- [ ] Hover over a tree node. The corresponding element highlights in the browser.

### UAT-4.3a — Inspect Persistence Across Navigation

Verify that inspect mode survives page navigation without requiring the user to toggle it off and on again.

- [ ] Enable inspect mode on a page. Navigate to a different URL. After the new page loads, the overlay is re-injected automatically — hovering over elements still shows the blue highlight and tooltip.
- [ ] The DOM tree panel refreshes to show the new page’s element hierarchy. The inspector status bar still shows the element count.
- [ ] The 🔍 Inspect toggle button remains in the checked/active state throughout.

### UAT-4.4 — Multi-Select Mode

Verify bulk selection commands and that selection count is tracked accurately.

- [X] In inspect mode, Ctrl+click several elements in the browser. Each selected element gets a green persistent border and the selection count updates.
- [ ] Click "Select All Forms". All `<input>`, `<select>`, `<textarea>`, and `<button>` elements become selected. The DOM tree filters to show only these element types with their ancestor path.
- [ ] The status bar shows the selection count and total element count (e.g. "12 selected │ DOM: 342 elements").
- [ ] Click "Clear Selection". All selections are removed, the count drops to 0, and the DOM tree restores to the full unfiltered hierarchy.
- [ ] Click "Select All Inputs". Only `<input>` elements are selected and the DOM tree filters to show only inputs.

### UAT-4.5 — Auto-Detect Control Groups

Navigate to pages with specific HTML structures and verify the detector identifies control group candidates.

- [ ] Navigate to a page containing a `<form>`, a `<table>` with `<thead>` and `<tbody>`, a `<nav>`, and a `<ul>` with 3+ `<li>` items. After capture, the suggestions list shows FormContainer, TableContainer, NavigationContainer, and ListContainer entries with references to the matched elements.
- [ ] Navigate to a page with a `<fieldset>` containing a `<legend>`. The detector finds a FieldsetContainer named from the legend text.
- [ ] Navigate to a page with a `<div role="dialog">`. The detector finds a RoleContainer for "dialog".

### UAT-4.6 — SPA Page Transition Detection

Verify that client-side navigations in single-page applications are detected without full page reloads.

- [ ] Navigate to a SPA. Click a link that triggers a client-side navigation (no full page reload). The transition is detected and a log entry "Page transition detected: {url}" appears.
- [ ] Rapidly click between two SPA routes. Only one transition event fires per URL within a 2-second window (deduplication prevents floods).

### UAT-4.7 — Recording Mode

Start a recording session, navigate through multiple pages, and verify capture, pause/resume, and stop behavior.

**Start recording:**

- [X] Click ⏺ Record. The ⏺ button is replaced by ⏹ (Stop) and ⏸ (Pause) buttons. A red 3px border appears around the browser area. The status bar shows "Recording..." in red text.
- [ ] The sidebar shows a "This Session" section header above the "Corpus Pages" section.

**Capture pages:**

- [ ] Navigate to 3 different pages. Each page transition triggers an automatic DOM capture. The "This Session" sidebar section lists each newly captured page with a 🆕 icon.
- [ ] Navigate to the same URL twice within 2 seconds. Only one capture is recorded (deduplication).
- [ ] Click a page in the "This Session" sidebar list. The browser navigates to that page's URL.

**Pause and resume:**

- [ ] Click ⏸ Pause. The ⏸ button changes to ▶ (Resume). The status bar shows "Paused". Navigate to a new page — no capture occurs and the "This Session" list does not grow.
- [ ] Click ▶ Resume. The next navigation triggers capture again and the status bar returns to showing the session count.

**Stop recording:**

- [ ] Click ⏹ Stop. Recording stops: the red border disappears, the toolbar returns to showing the ⏺ button, and the "This Session" section disappears from the sidebar.
- [ ] A prompt appears: "{N} pages captured. Analyze corpus now?" with Yes/No buttons.
- [ ] If no pages were captured during the session (e.g. recording was started and immediately stopped), no prompt appears.

**Log verification:**

- [ ] Check the log viewer for entries: "Recording started", "Page captured: {name} ({url})" for each page, and "Recording stopped. N pages captured".

**Fresh session:**

- [ ] Start a new recording after having stopped a previous one. The "This Session" list is empty — no stale pages from the previous session appear.

---

## Phase 4B — Corpus Management

### UAT-4.8 — SQLite Corpus Store

Verify that recorded snapshots persist in the SQLite database across application restarts.

- [ ] Record several pages to the corpus and verify the recording completes without error.
- [ ] Close the application, relaunch it, and select the same site. The previously recorded pages are still present.
- [ ] Check the log viewer for a corpus store entry: "Corpus store — Site: {id}, Page: {name}, Elements: {count}, Size: {bytes} bytes".

### UAT-4.9 — Corpus Browser

Open the corpus browser and verify sorting, filtering, selection, and action buttons.

- [ ] Click Site → Browse Corpus. The corpus browser view appears with a DataGrid showing all recorded pages with columns: Page Name, URL, Recorded date, Element count, and Size.
- [ ] Click a column header to sort by that column. Type a filter term in the search box and verify the grid narrows to matching pages by name or URL.
- [ ] Select a page row. The action buttons (View Snapshot, View Diff, Re-record, Delete Page) become enabled. Click "View Snapshot" and confirm the snapshot detail view loads.

### UAT-4.10 — Snapshot Diff

Record two versions of the same page and verify the diff view categorizes changes correctly.

- [ ] Record a page, modify it (e.g. add or remove elements by navigating to a different state), then re-record. Two snapshots now exist for the same page.
- [ ] Select the page in the Corpus Browser and click "View Diff". The diff view shows added elements in green, removed elements in red, and changed elements in yellow.
- [ ] Verify that added elements appear in the Added list, removed elements in the Removed list, changed attributes in the Changed list, and unchanged elements are counted separately.

### UAT-4.11 — Export/Import Snapshots

Export a snapshot to JSON, inspect the file, and import it back.

- [ ] Record a page and export its snapshot. A JSON file is saved with the filename pattern `{site}-{page}-{timestamp}.json`.
- [ ] Open the JSON file and confirm it contains indented, camelCase JSON with all DomSnapshot fields: siteName, pageName, pageUrl, pageTitle, capturedAt, rootElement with nested children, and selectedElements.
- [ ] Import the JSON file into a different site corpus. The snapshot is deserialized and stored correctly.
- [ ] Import an invalid JSON file. An error message is shown and the application does not crash.

---

## Cross-Cutting Scenarios

### UAT-X.1 — Error Resilience

Verify the app handles failures gracefully without crashing.

- [ ] Navigate to a page that fails to load (e.g. an unreachable host). An error message appears in the status bar and the app remains functional.
- [ ] Trigger a DOM capture on a page with no content. The capture completes with minimal elements and no crash.
- [ ] Close the WebView2 DevTools window while inspect mode is active. The app continues normally.

### UAT-X.2 — Performance

Verify acceptable performance under load.

- [ ] Navigate to a complex page with 3000+ elements. DOM capture completes in under 2 seconds.
- [ ] Open the log viewer with 500+ entries. Scrolling is smooth with no jank (virtualization is active).
- [ ] Open the Corpus Browser with 50+ recorded pages. The grid loads and scrolls without lag.

### UAT-X.3 — Data Persistence

Verify that all user data survives application restarts.

- [ ] Create 2 sites, record pages to each, close the app, and relaunch. Both sites appear in the selector with correct page counts.
- [ ] Verify that previous session log files exist in the `logs/` folder.

### UAT-X.4 — Sidebar Layout

Verify the sidebar displays correctly in both normal and recording modes.

- [ ] Select a site. The sidebar shows the site name as a bold header, corpus stats below (e.g. "0 pages · 0 controls"), a "Corpus Pages" section, and a "Controls" section.
- [ ] When not recording, no "This Session" section is visible and no recording indicator appears.
- [ ] Switch to a different site. The sidebar header, stats, and page lists update to reflect the new site. Any stale session data is cleared.

### UAT-X.5 — Inspector Panel Resize

Verify the inspector panel resizes correctly without whitespace.

- [ ] Enable inspect mode. Drag the grid splitter between the browser and the inspector panel. Both areas resize smoothly with no whitespace gaps.
- [ ] The inspector panel width is set to 300px by default. After resizing, the new width is maintained until inspect mode is toggled off.
