# User Acceptance Tests — Phase 12 (UI Redesign: Start Page & Tabbed Workspace)

Manual test scenarios to verify the new two-screen architecture: a dedicated **Start Page** for site management and a **Tabbed Workspace** with six purpose-built tabs (Scraping, Control Objects, Page Objects, Corpus, Log, Settings).

**Prerequisites:**
- Windows 10/11 with .NET 10 runtime
- WebView2 runtime installed
- A clean `%LOCALAPPDATA%\Brinell.Scraper\` (or backup the existing `settings.json` and `sites/`) for the first-launch tests
- For tests that exercise existing data: at least 2 sites with recorded pages, and 1 site with control proposals + generated controls (use Phases 1–5/13 to populate)

---

## 12.1 — Start Page

### UAT-12.1.1 — First Launch Shows Start Page

Verify the Start Page is the first screen on a fresh install.

- [ ] Delete (or rename) `%LOCALAPPDATA%\Brinell.Scraper\settings.json`. Launch the Scraper.
- [ ] The window opens directly into the Start Page — no toolbar, no sidebar, no log pane visible.
- [ ] The header shows "Brinell Scraper" with the app icon.
- [ ] A search box is visible above the site list.
- [ ] A "+ New" button is visible in the recent-sites header row.
- [ ] If no sites exist yet, the recent-sites area shows an empty state (no cards) without throwing.
- [ ] A footer row shows "⚙ Settings" on the left and the version string (e.g. `v1.0.0-beta`) on the right.

### UAT-12.1.2 — Site Cards Render Correctly

Verify each existing site is rendered as a card with the expected metadata.

- [ ] With 3+ existing sites, launch the app. The Start Page shows one card per site.
- [ ] Each card displays: site name (bold), URL (truncated to domain), page count + " pages", control count + " controls", last-opened relative time (e.g. "2 days ago", "today", "never").
- [ ] Each card has three actions: **Open**, **⚙** (settings), **🗑** (delete).
- [ ] Cards wrap into multiple rows when the window width is reduced.

### UAT-12.1.3 — Search Filters Sites in Real Time

- [ ] Type part of a site name into the search box. The card list filters as you type.
- [ ] Clearing the search box restores the full list.
- [ ] Search is case-insensitive and also matches against the URL field.

### UAT-12.1.4 — Open Site Navigates to Workspace

- [ ] Click **Open** on a site card. The window content swaps to the Tabbed Workspace for that site.
- [ ] The Start Page is no longer visible.
- [ ] The site's `LastOpenedAt` updates (return to Start Page later and confirm "moments ago" or "today").

### UAT-12.1.5 — New Site Wizard

- [ ] Click **+ New**. A new-site dialog/flow opens.
- [ ] Provide a name and start URL, then confirm. A new card appears on the Start Page.
- [ ] Open the new site — it navigates to the Workspace with an empty corpus.

### UAT-12.1.6 — Delete Site

- [ ] Click 🗑 on a site card. A confirmation dialog appears (so deletes are not silent).
- [ ] Confirm. The card disappears from the list.
- [ ] Restart the app — the site remains deleted (deletion was persisted).
- [ ] Cancelling the dialog leaves the site untouched.

### UAT-12.1.7 — Site Settings Shortcut

- [ ] Click the **⚙** action on a site card. The site's per-site settings open (either inline dialog or a navigation that returns to Start Page on close).
- [ ] Editing the site name updates the card text after save.

### UAT-12.1.8 — Footer Settings & Version

- [ ] Click "⚙ Settings" in the footer. The standalone Settings tab opens (without a site context).
- [ ] Closing/back returns to the Start Page.
- [ ] The version string in the bottom-right matches the assembly version.

---

## 12.2 — Workspace Shell

### UAT-12.2.1 — Tab Bar Layout

- [ ] Open any site. The workspace shows a horizontal tab bar at the top with six tabs: **Scraping**, **Control Objects**, **Page Objects**, **Corpus**, **Log**, **Settings** (in this order).
- [ ] The current site name is shown somewhere in the workspace chrome (header, breadcrumb, or tab strip).
- [ ] A "Back to Start" / home affordance is visible and clicking it returns to the Start Page.

### UAT-12.2.2 — Tab Switching Preserves State

- [ ] Open the Scraping tab and navigate to a URL in the WebView.
- [ ] Switch to Control Objects, then back to Scraping. The previous URL is still loaded — the WebView was not torn down.
- [ ] Switch to Log, then back to Scraping. State preserved.

### UAT-12.2.3 — Default Tab on Open

- [ ] Open a site that has zero recorded pages. The default tab is **Scraping**.
- [ ] Open a site that already has recordings. The default tab can be **Scraping** (acceptable) — verify no exceptions in Log tab.

### UAT-12.2.4 — Workspace Disposes on Site Switch

- [ ] Open Site A, navigate the WebView, then return to Start Page and open Site B.
- [ ] Site A's WebView should be disposed (no double WebView2 process visible in Task Manager — only Site B's).
- [ ] No exceptions appear in the Log tab.

---

## 12.3 — Scraping Tab

### UAT-12.3.1 — Layout

- [ ] On the Scraping tab, the layout is roughly: address bar + nav buttons at top, the WebView fills the main area, an inspector / session panel is visible on a side or bottom.
- [ ] No log viewer is embedded in this tab — the log lives in its own tab.

### UAT-12.3.2 — Recording Workflow

- [ ] Navigate to a URL. Click **Record** (or equivalent). A snapshot is captured.
- [ ] The session panel updates with the new page entry.
- [ ] Switch to the Corpus tab — the page appears there too.

### UAT-12.3.3 — Inspector

- [ ] With a page loaded, click an element in the WebView (or hover/inspect mode).
- [ ] The inspector shows the element's selector, attributes, and DOM ancestor chain.
- [ ] Clearing/changing the selection updates the inspector without errors.

---

## 12.4 — Control Objects Tab

### UAT-12.4.1 — Layout

- [ ] Switch to the Control Objects tab. It shows a list/grid of detected and generated control objects for the current site.
- [ ] Toolbar/actions include at minimum: **Analyze Corpus**, **Generate All Pending** (or **Approve All & Generate**), **Regenerate Selected**.
- [ ] When no analysis has run yet, the list is empty and a hint message is shown.

### UAT-12.4.2 — Status Indicators

- [ ] After an analysis pass (Phase 13.1), each row shows the proposal status (Pending / Approved / Rejected) and generation status (NotGenerated / Generated / Failed).
- [ ] Approving a proposal updates the status indicator immediately.

### UAT-12.4.3 — Property Drilldown

- [ ] Select a generated control. A detail/properties pane shows the control's child properties (name, type, locator).
- [ ] No exceptions are thrown when selecting controls without generated code yet.

> Note: Wiring the actual `PipelineOrchestrator` calls to the Analyze / Generate buttons is consumer-side work flagged for a follow-up. Treat command bodies as best-effort in this UAT round.

---

## 12.5 — Page Objects Tab

### UAT-12.5.1 — Layout

- [ ] Switch to the Page Objects tab. It lists all recorded pages for the current site, each with a generation status.
- [ ] Toolbar actions include **Generate All** and **Regenerate Selected**.

### UAT-12.5.2 — Per-Page Properties

- [ ] Select a page that has been generated. The detail pane shows its property list (name, type, css), and references to any control objects it uses.
- [ ] Validation entries (warnings / errors) from `CodeValidator` are shown if present.

---

## 12.6 — Corpus Tab

### UAT-12.6.1 — Layout

- [ ] Switch to the Corpus tab. Recorded pages are shown grouped (by URL path or recording session).
- [ ] Each row shows: page title, URL, snapshot count, last-recorded time.

### UAT-12.6.2 — Snapshot History

- [ ] Expand a page row (or click). All snapshot versions for that page are listed with timestamp and a way to view/diff.
- [ ] Selecting a snapshot loads its details (HTML preview or property list) without errors.

### UAT-12.6.3 — Delete Snapshot / Page

- [ ] Delete a single snapshot — confirmation prompt appears, then the row updates.
- [ ] Delete an entire page — confirmation prompt; FK-cascading removes its snapshots in the database.

---

## 12.7 — Log Tab

### UAT-12.7.1 — Layout & Live Updates

- [ ] Switch to the Log tab. A grid/list of log entries is visible with columns for timestamp, level, source/category, and message.
- [ ] Trigger an action on another tab (e.g. navigate the WebView). Switch back — new entries appear at the bottom (or top) of the log.

### UAT-12.7.2 — Default Filter Level

- [ ] On first open of the Log tab in a session, the level filter defaults to **Trace** ("All").
- [ ] Lowering the filter to **Information** hides Trace and Debug entries; the visible count drops.
- [ ] Setting back to **Trace** restores all entries.

### UAT-12.7.3 — Counters

- [ ] The tab shows a `Total: N / Filtered: M` counter (or similar). M ≤ N at all times.
- [ ] Changing the filter updates `Filtered`. New entries arriving update both.

### UAT-12.7.4 — Search

- [ ] Type a term into the search/filter box. The list narrows to entries whose message (or category) contains the term, case-insensitively.
- [ ] Clearing the search restores the full filtered list.

### UAT-12.7.5 — Auto-Scroll

- [ ] With auto-scroll enabled and the user not scrolled up, new entries scroll into view automatically.
- [ ] Scrolling up manually pauses auto-scroll. Scrolling back to the bottom resumes it (or there's an explicit toggle that the user can enable).

### UAT-12.7.6 — Export

- [ ] Click **Export**. A save dialog appears with a default filename (timestamped).
- [ ] Save. The exported file (text or JSON) contains the currently filtered entries with their full timestamp, level, category and message.

---

## 12.8 — Settings Tab

### UAT-12.8.1 — Site-Scoped vs Global

- [ ] Open Settings from inside a workspace — site-scoped settings are editable.
- [ ] Open Settings from the Start Page footer — only global / standalone settings are editable; site-specific options are hidden or disabled.

### UAT-12.8.2 — Persistence

- [ ] Change a global setting (e.g. AnalyzerModel name). Close and relaunch the app.
- [ ] Open Settings — the changed value persists. Verify by inspecting `%LOCALAPPDATA%\Brinell.Scraper\settings.json` that the value matches.

### UAT-12.8.3 — Defaults

- [ ] Delete `settings.json` and relaunch. Settings opens with sensible defaults (no nulls, no exceptions).

---

## 12.9 — Shell Navigation & End-to-End

### UAT-12.9.1 — Round Trip: Start → Workspace → Start

- [ ] From the Start Page, open a site. Workspace appears.
- [ ] Click "Back to Start" / home. Start Page reappears with the site's `LastOpenedAt` updated.
- [ ] Open a different site. Workspace appears for the new site (no leakage from previous site — corpus tab shows the new site's pages, not the old).

### UAT-12.9.2 — No Modal Dead-Ends

- [ ] Walk through every tab on a site with no data. No tab throws or shows a permanent spinner; each shows an empty state.

### UAT-12.9.3 — Clean Shutdown

- [ ] With the app open in the workspace, close the window.
- [ ] In Task Manager, no orphaned `Brinell.Scraper.exe` or its WebView2 helpers remain after a few seconds.

### UAT-12.9.4 — Settings Available Without Site

- [ ] From the Start Page footer, open Settings. Without selecting a site, the settings tab/page must still load and allow editing global values.

### UAT-12.9.5 — Reload Resilience

- [ ] In the workspace, force-reload the WebView in the Scraping tab. The app remains responsive.
- [ ] Switch tabs and back — no exceptions surface in the Log tab.

---

## Sign-off

| Section                        | Tester | Date | Result |
|--------------------------------|--------|------|--------|
| 12.1 Start Page                |        |      |        |
| 12.2 Workspace Shell           |        |      |        |
| 12.3 Scraping Tab              |        |      |        |
| 12.4 Control Objects Tab       |        |      |        |
| 12.5 Page Objects Tab          |        |      |        |
| 12.6 Corpus Tab                |        |      |        |
| 12.7 Log Tab                   |        |      |        |
| 12.8 Settings Tab              |        |      |        |
| 12.9 Shell Navigation & E2E    |        |      |        |
