# User Acceptance Tests — Phase 12 Wireup 08 (Recording Sidebar Actions)

Manual test scenarios for phase-12-08:

- 08a Sidebar corpus/session panel behavior
- 08b Recording session list updates and red browser border
- 08c Stop recording analyze prompt and explicit transfer flow

## Prerequisites

- Windows 10/11 with .NET 10 runtime
- WebView2 runtime installed
- At least 1 site with existing corpus pages
- A target web app/page where top-level navigation, SPA transitions, and iframe transitions can be triggered
- Optional: a site with cross-origin iframe content for extended validation

---

## W8.1 — Sidebar Corpus Population (08a)

### UAT-W8.1.1 — Session Panel Loads Site Header and Corpus Stats

- [X] Open a site from Start Page.
- [X] Scraping tab shows session panel with current site header.
- [X] Corpus stats line shows page count and control count.
- [ ] Values match data shown on Corpus and Control Objects tabs.

### UAT-W8.1.2 — Corpus Pages List Is Click-Navigable

- [ ] In session panel, click a page listed under Corpus.
- [ ] Browser address updates to the page URL.
- [ ] Browser navigates to that URL.
- [ ] No exceptions appear in Log tab.

### UAT-W8.1.3 — Session Section Starts Empty on Fresh Workspace Load

- [X] Open a site that has existing corpus pages but no active recording session.
- [X] This Session list is empty.
- [X] Session summary reads No pages captured yet.

---

## W8.2 — Recording Session and Red Border (08b)

### UAT-W8.2.1 — Start Recording Enables Recording Mode UI

- [X] In Scraping tab, click Start Recording.
- [X] Red 3px border appears around browser host area.
- [X] Session panel shows Recording label.
- [X] Session summary reflects recording mode.

### UAT-W8.2.2 — Top-Level Navigation Captures Into This Session

- [X] While recording, navigate to a different page URL.
- [X] A new entry appears in This Session list.
- [X] Entry shows new-page icon.
- [X] Session summary count increments.

### UAT-W8.2.3 — SPA Transition Captures Into This Session

- [ ] While recording, trigger an in-app SPA route transition without full reload.
- [ ] A new This Session entry appears after transition settles.
- [ ] No duplicate flood occurs for a single route change.

### UAT-W8.2.4 — IFrame Navigation Captures With Prefix

- [X] While recording, trigger navigation inside an iframe.
- [X] A new This Session entry appears.
- [X] Entry name includes iframe prefix.

### UAT-W8.2.5 — Duplicate URL Within Dedup Window Is Suppressed

- [X] While recording, trigger same URL transition twice quickly (within about 2 seconds).
- [X] Only one new session entry is added.
- [X] Recording status does not over-count duplicates.

### UAT-W8.2.6 — No Capture When Not Recording

- [X] Stop recording.
- [X] Perform navigation and SPA activity.
- [X] No additional This Session entries are added while stopped.

---

## W8.3 — Stop and Analyze Prompt (08c)

### UAT-W8.3.1 — Stop Removes Recording Visual State Without Clearing Session

- [X] Start recording and capture at least 2 pages.
- [X] Click Stop Recording.
- [X] Red border disappears immediately.
- [X] This Session entries remain visible.

### UAT-W8.3.2 — Prompt Appears Only When Captures Exist

- [X] Stop recording with one or more captured pages.
- [X] Prompt appears with captured page count.
- [X] Prompt title indicates recording completion.

### UAT-W8.3.3 — Prompt Not Shown for Empty Session

- [X] Start recording and stop immediately with zero captures.
- [X] No analyze/transfer prompt appears.

### UAT-W8.3.4 — No Path Preserves Session for Review

- [X] Capture pages, stop recording, and choose No in prompt.
- [X] This Session list remains intact.
- [X] Corpus list is unchanged.
- [X] User can review or resume recording.

### UAT-W8.3.5 — Yes Path Transfers and Clears Session

- [X] Capture pages, stop recording, and choose Yes in prompt.
- [X] Captured session pages are added to corpus.
- [X] Corpus stats/list refreshes.
- [X] This Session list clears after transfer.

### UAT-W8.3.6 — Transfer Includes IFrame-Rich Captures

- [X] Record at least one page involving iframe transitions.
- [X] Choose Yes on prompt.
- [X] Transferred corpus snapshots remain usable in downstream tabs.

---

## W8.4 — Regression and Stability

### UAT-W8.4.1 — Session Panel Navigation Still Works After Transfer

- [X] Complete a Yes transfer flow.
- [X] Click newly transferred corpus page from session panel.
- [X] Browser navigates correctly.

### UAT-W8.4.2 — Workspace Remains Stable Across Repeated Record Cycles

- [X] Run three cycles: start, capture, stop, choose No then Yes on final cycle.
- [X] No stale event behavior (double captures, duplicate prompts, or missing prompts).
- [X] No unhandled exceptions in Log tab.

### UAT-W8.4.3 — Back to Start and Reopen Site Does Not Leak Prior Session

- [X] Capture pages and stop with No (session still visible).
- [X] Return to Start page and reopen the same site.
- [X] Session data starts clean for new workspace instance.
- [X] Corpus persists prior transferred pages only.

---

## Sign-off

| Section                           | Tester | Date | Result |
| --------------------------------- | ------ | ---- | ------ |
| W8.1 Sidebar Corpus Population    |        |      |        |
| W8.2 Recording Session and Border |        |      |        |
| W8.3 Stop and Analyze Prompt      |        |      |        |
| W8.4 Regression and Stability     |        |      |        |
