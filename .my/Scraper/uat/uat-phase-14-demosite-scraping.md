# User Acceptance Tests — Phase 14 Demo Site Scraping

Manual test scenarios to validate scraper behavior against the static multi-page demo site in Phase 14.

Scope:

- Multi-page navigation scraping (no SPA)
- Form and table element detection
- Repeated control pattern detection
- IFrame extraction (single-level and nested)
- Custom web component detection (`demo-pill`, `demo-counter`, `demo-collapse`)

**Demo Site Root:**

- tools/Brinell.Scraper.TestSite

**Prerequisites:**

- Windows 10/11 with .NET 10 runtime
- Brinell.Scraper app builds and launches
- Demo site files exist under `.my/Scraper/phase-14-DemoSite`
- A new test site is configured in scraper with Start URL pointing to the local demo site
- Existing corpus for that test site is empty before first run (recommended)

---

## W14.1 — Site Bootstrapping

### UAT-W14.1.1 — Open Demo Site Home

- [X] Create or open a scraper site named Phase14 Demo.
  Action: Open Start Page, click + New Site (or edit existing), set Name to Phase14 Demo.
  Expected: Site card exists and opens without errors.
- [X] Set Start URL to the local file URL for the home page.
  Action: Use file:///... path to the demo home page.
  Expected: URL is accepted and saved in site config.
- [X] Navigate to Scraping tab and load home page.
  Action: Click Open on the site card and wait for browser load.
  Expected: Browser shows the demo home document.
- [X] Verify title/header rendered.
  Action: Inspect page top header.
  Expected: Header text shows Demo Shop Portal.

### UAT-W14.1.2 — Basic Crawlability

- [X] Verify scraper can identify top navigation links.
  Action: Capture a snapshot on Home and inspect discovered links/elements.
  Expected: Home, Catalog, Checkout, and Support navigation entries are detected.
- [X] Verify page metadata is captured.
  Action: Open captured snapshot details.
  Expected: Title, URL, and capture time are present.

---

## W14.2 — Home Page Scraping

Target page:

- `.my/Scraper/phase-14-DemoSite/index.html`

### UAT-W14.2.1 — Main Controls Detected

- [X] Detect heading and welcome panel controls.
  Action: On Home page, capture snapshot and open DOM tree/element list.
  Expected: A heading node and a welcome panel container are present as separate detectable controls.
- [X] Detect quick login form fields.
  Action: Filter elements by login form area.
  Expected: Email input home-email, password input home-password, and sign-in button home-login are detected with correct control types.
- [X] Detect welcome action button.
  Action: Locate button with test id welcome-action in capture results.
  Expected: Button exists and is mapped as clickable/actionable control.

### UAT-W14.2.2 — Custom Components Detected

- [X] Detect demo-pill instances on page.
  Action: Inspect Home page nodes for custom tag names.
  Expected: One or more demo-pill elements are captured in DOM output.
- [X] Verify repeated pill badges are grouped consistently.
  Action: Run analyze/group detection for Home page snapshot.
  Expected: Repeated badge-like controls are either grouped together or given equivalent signatures.

### UAT-W14.2.3 — Home IFrame Detected

- [X] Detect home iframe element.
  Action: Capture Home snapshot and search for iframe.
  Expected: iframe home-announcements-frame is present.
- [ ] Scrape iframe DOM content.
  Action: Expand iframe context/frame capture results.
  Expected: frame-title heading and announcement-list content are present in extracted frame DOM.
- [ ] Ensure iframe content is linked to parent snapshot.
  Action: Review stored snapshot/frame association metadata.
  Expected: Frame content is attached to the Home page snapshot, not stored as unrelated page.

---

## W14.3 — Catalog Page Scraping

Target page:

- `.my/Scraper/phase-14-DemoSite/pages/catalog.html`

### UAT-W14.3.1 — Table and Repeating Rows

- [ ] Detect product table with headers and body rows.
  Action: Navigate to Catalog and capture snapshot.
  Expected: product-table is detected with row/column structure preserved.
- [ ] Detect repeated row action buttons.
  Action: Locate add buttons by ids add-p100, add-p200, add-p300.
  Expected: All three buttons are present and distinct.
- [ ] Verify row structure grouping consistency.
  Action: Run analysis/group detection on Catalog snapshot.
  Expected: Similar row patterns produce consistent grouping/signature behavior.

### UAT-W14.3.2 — Filter Controls

- [ ] Detect select controls for filtering.
  Action: Inspect Catalog filter panel controls.
  Expected: category-filter and price-filter are identified as select/dropdown controls.
- [ ] Detect custom counter component.
  Action: Locate results-page-counter element in DOM capture.
  Expected: demo-counter component is captured and visible in inspector output.

### UAT-W14.3.3 — Catalog IFrame

- [ ] Detect catalog profile iframe.
  Action: Inspect Catalog snapshot for frame node.
  Expected: iframe catalog-profile-frame exists.
- [ ] Scrape iframe controls.
  Action: Open frame extraction result.
  Expected: profile-frame-heading, profile-id, profile-tier, and profile-save are all present.

---

## W14.4 — Checkout Page Scraping

Target page:

- `.my/Scraper/phase-14-DemoSite/pages/checkout.html`

### UAT-W14.4.1 — Shipping Form

- [ ] Detect shipping fields.
  Action: Navigate to Checkout and capture snapshot.
  Expected: ship-name, ship-street, ship-city, and ship-zip are detected as text inputs.
- [ ] Detect save shipping button.
  Action: Locate save-shipping control in checkout form.
  Expected: save-shipping is present and recognized as a button.

### UAT-W14.4.2 — Summary and Custom Collapse

- [ ] Detect summary card and list content.
  Action: Inspect order summary area in capture results.
  Expected: Summary container and list items are visible in DOM extraction.
- [ ] Detect custom collapse component.
  Action: Locate demo-collapse element in checkout snapshot.
  Expected: Collapsible custom component is detected.
- [ ] Detect coupon controls inside collapse.
  Action: Inspect coupon section nodes.
  Expected: coupon-code input and apply-coupon button are detected.

### UAT-W14.4.3 — Payment IFrame

- [ ] Detect checkout payment iframe.
  Action: Inspect Checkout snapshot for frame elements.
  Expected: checkout-payment-frame is present.
- [ ] Scrape payment controls in frame.
  Action: Traverse frame capture output.
  Expected: card-name, card-number, card-expiry, card-cvv, card-type, and pay-now are detected.

---

## W14.5 — Support Page and Nested IFrame Scraping

Target page:

- `.my/Scraper/phase-14-DemoSite/pages/support.html`

### UAT-W14.5.1 — Support Form and FAQ

- [ ] Detect support ticket form controls.
  Action: Navigate to Support and capture snapshot.
  Expected: ticket-topic, ticket-description, and submit-ticket are all present.
- [ ] Detect FAQ collapsible components.
  Action: Inspect FAQ area in support snapshot.
  Expected: demo-collapse components and their summary labels are captured.

### UAT-W14.5.2 — Nested IFrame Level 1

- [ ] Detect outer nested iframe.
  Action: Inspect Support snapshot for frame nodes.
  Expected: support-nested-frame is detected.
- [ ] Scrape level-1 frame content.
  Action: Open support-nested-frame extraction.
  Expected: nested-host-title and nested-inner-frame are both present.

### UAT-W14.5.3 — Nested IFrame Level 2

- [ ] Traverse into nested inner frame.
  Action: Continue from level-1 frame into nested-inner-frame.
  Expected: Inner frame DOM is accessible.
- [ ] Scrape inner content.
  Action: Inspect inner frame nodes.
  Expected: nested-inner-title and nested-inner-table are captured.
- [ ] Verify traversal is stable.
  Action: Check logs during nested frame capture.
  Expected: No iframe traversal exception or abort is logged.

---

## W14.6 — Cross-Page Corpus Validation

### UAT-W14.6.1 — Snapshot Presence

- [ ] Confirm snapshots are stored for all four pages.
  Action: Open Corpus tab after capturing each page.
  Expected: Home, Catalog, Checkout, and Support each appear with at least one snapshot.
- [ ] Confirm element counts are populated.
  Action: Inspect snapshot metrics per page.
  Expected: Element count is greater than zero for each captured snapshot.

### UAT-W14.6.2 — Distinct Page Count

- [ ] Verify distinct page count equals 4.
  Action: Return to Start Page/site summary after full run.
  Expected: Page count reflects four distinct demo pages.

### UAT-W14.6.3 — Re-open and Reload

- [ ] Restart and reopen the same test site.
  Action: Close app, relaunch, open Phase14 Demo.
  Expected: Site opens without data loss.
- [ ] Confirm corpus persistence.
  Action: Open Corpus tab.
  Expected: Previously captured pages and snapshots are still present.
- [ ] Confirm reconciliation correctness.
  Action: Trigger refresh in Corpus tab.
  Expected: No previously captured page disappears unexpectedly.

---

## W14.7 — Control/Object Generation Readiness

### UAT-W14.7.1 — Analyze Corpus on Demo Site

- [ ] Run analyze from Control Objects tab.
  Action: Click Analyze Corpus after full page capture.
  Expected: Analysis completes with non-empty proposal list.
- [ ] Verify proposal relevance.
  Action: Inspect proposal names/signatures.
  Expected: Proposed controls include repeated patterns from product rows/buttons, form groups, and repeated custom wrapper areas.

### UAT-W14.7.2 — Page Objects Coverage

- [ ] Run page object generation for all pages.
  Action: Use Generate All in Page Objects tab.
  Expected: Generation attempts run for each captured page.
- [ ] Verify status progression.
  Action: Inspect per-page status values after generation.
  Expected: All four pages transition out of NotGenerated into Generated or Error.
- [ ] Verify iframe tolerance.
  Action: Review pages containing frames.
  Expected: Frame-containing pages do not fail only because iframes exist.

---

## W14.8 — Negative/Edge Cases

### UAT-W14.8.1 — Missing Frame Source Handling

- [ ] Temporarily break one iframe src and reload page.
  Action: Edit one frame source to an invalid file and navigate back to that page.
  Expected: Parent page still renders and can be captured.
- [ ] Verify parent capture resilience.
  Action: Capture snapshot after frame break.
  Expected: Snapshot succeeds for parent page without application crash.
- [ ] Verify frame-scoped error reporting.
  Action: Inspect logs around capture time.
  Expected: Error is logged as frame-specific and does not abort full page capture.

### UAT-W14.8.2 — Rapid Navigation

- [ ] Perform rapid navigation sequence.
  Action: Navigate quickly Home -> Catalog -> Checkout -> Support.
  Expected: Browser updates correctly without lock-up.
- [ ] Capture snapshots during transitions.
  Action: Trigger captures while navigating quickly.
  Expected: Captures are recorded without duplicate corruption.
- [ ] Verify URL-to-snapshot mapping.
  Action: Open Corpus entries after run.
  Expected: Each snapshot remains associated with its correct page URL.

---

## Sign-off

| Section                               | Tester | Date | Result |
| ------------------------------------- | ------ | ---- | ------ |
| W14.1 — Site Bootstrapping           |        |      |        |
| W14.2 — Home Page Scraping           |        |      |        |
| W14.3 — Catalog Page Scraping        |        |      |        |
| W14.4 — Checkout Page Scraping       |        |      |        |
| W14.5 — Support + Nested IFrame      |        |      |        |
| W14.6 — Cross-Page Corpus Validation |        |      |        |
| W14.7 — Generation Readiness         |        |      |        |
| W14.8 — Negative/Edge Cases          |        |      |        |
