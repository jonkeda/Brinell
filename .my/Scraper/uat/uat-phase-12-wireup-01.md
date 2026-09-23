# User Acceptance Tests — Phase 12 Wireup 01 (Start Page Actions)

Manual test scenarios to verify the four start-page action wire-ups: **New Site** dialog, **Edit Site** dialog, **Delete Site** confirmation, and **Open Site with Corpus Page URL**.

**Prerequisites:**

- Windows 10/11 with .NET 10 runtime
- At least 2 existing sites in the corpus database (one with recorded pages)
- Phase 12 Start Page UI functional (site cards render, events exist but may be unsubscribed)

---

## W1 — New Site Dialog

### UAT-W1.1 — Dialog Opens from Start Page

- [X] On the Start Page, click **+ New**. A modal "New Site" dialog appears centered over the main window.
- [X] The dialog contains fields: **Name**, **Start URL**, **Namespace**, **Output Path** (with Browse button), and **OK** / **Cancel** buttons.
- [X] The dialog cannot be resized. It sizes to content height with a fixed width (~480px).

### UAT-W1.2 — Name Validation

- [X] Leave the Name field empty and click **OK**. The dialog does not close; focus moves to the Name field.
- [ ] Enter a name (e.g. "Test Site") — no error indicator appears for the Name field.

### UAT-W1.3 — URL Validation

- [X] Enter a valid name but leave Start URL empty. Click **OK**. The dialog does not close; a red error message "Must be a valid absolute URL." appears below the URL field and focus moves to the URL field.
- [ ] Enter an invalid URL (e.g. "not-a-url"). Click **OK**. The same red error message appears.
  This is actually a valid url. All HTTP/HTTPS should be allowed and any text is also ok
- [ ] Enter a URL with an unsupported scheme (e.g. "ftp://example.com"). Click **OK**. The error message appears (only `http` and `https` are accepted).
  Error: this is accepted
- [X] Enter a valid URL (e.g. "https://example.com"). Click **OK**. The dialog closes successfully.

### UAT-W1.4 — Namespace Auto-Fill

- [X] Open the New Site dialog. Type "My Test Site" in the Name field.
- [ ] The Namespace field auto-fills with a sanitized value (e.g. "MyTestSite") — only `[A-Za-z0-9_]` characters, no spaces or special characters.
- [ ] Clear the Namespace field manually, then type more in the Name field. The auto-fill resumes (it only fills when Namespace is empty).

### UAT-W1.5 — Browse Button

- [X] Click **Browse…** next to the Output Path field. A folder picker dialog opens.
- [X] Select a folder and confirm. The Output Path field is populated with the selected folder path.
- [X] Cancel the folder picker. The Output Path field remains unchanged.

### UAT-W1.6 — Site Persisted and Card Appears

- [X] Fill in Name ("UAT New Site") and Start URL ("https://example.com"), then click **OK**.
- [X] The dialog closes. A new site card appears on the Start Page with the name "UAT New Site" and URL "example.com".
- [X] Close and relaunch the application. The new site still appears on the Start Page (persisted to database).

### UAT-W1.7 — Cancel Has No Side Effects

- [X] Open the New Site dialog, fill in all fields, then click **Cancel** (or close the dialog via the X button).
- [X] No new site card appears on the Start Page. No database row was created.

---

## W2 — Edit Site Dialog

### UAT-W2.1 — Dialog Opens Pre-Populated

- [ ] On the Start Page, click the **⚙** (edit/settings) action on an existing site card. The "Edit Site" dialog opens.
- [ ] The dialog title reads **"Edit Site"** (not "New Site").
- [ ] All fields are pre-populated with the site's current values: Name, Start URL, Namespace, and Output Path.

### UAT-W2.2 — Namespace Auto-Fill Suppressed

- [ ] In the Edit Site dialog, modify the Name field. The Namespace field does **not** auto-fill — it retains the existing value.
- [ ] This is because `IsEditMode` is `true`, suppressing the auto-fill behavior.

### UAT-W2.3 — Validation Rules Match New Site

- [ ] Clear the Name field and click **OK**. The dialog does not close; focus moves to the Name field.
- [ ] Enter an invalid URL and click **OK**. The red URL error message appears.
- [ ] Validation behavior is identical to the New Site dialog.

### UAT-W2.4 — Changes Persisted

- [ ] Change the site name from "Old Name" to "Updated Name" and click **OK**.
- [ ] The dialog closes. The site card on the Start Page immediately shows "Updated Name".
- [ ] Close and relaunch the application. The site still shows "Updated Name" (changes persisted via `CorpusDatabase.UpdateSite`).

### UAT-W2.5 — URL Change Persisted

- [ ] Edit a site and change the Start URL to a different valid URL. Click **OK**.
- [ ] The site card updates to show the new domain. Opening the site navigates to the new URL.

### UAT-W2.6 — Aliases Preserved

- [ ] Edit a site that has existing URL aliases. Change the name and click **OK**.
- [ ] The site's aliases are preserved (not cleared or overwritten). Verify by re-opening the edit dialog or checking the database.

### UAT-W2.7 — Cancel Discards Changes

- [ ] Open the Edit Site dialog, modify several fields, then click **Cancel**.
- [ ] The site card remains unchanged on the Start Page. No database update occurred.

---

## W3 — Delete Site Confirmation

### UAT-W3.1 — Confirmation Dialog Appears

- [X] On the Start Page, click the **🗑** (delete) action on a site card.
- [X] A message box appears with the text `Delete "{SiteName}" and all its data?`, a "Confirm Delete" title, Yes/No buttons, and a warning icon.

### UAT-W3.2 — Confirm Deletes Site

- [X] Click **Yes** on the confirmation dialog.
- [X] The site card disappears from the Start Page immediately.
- [X] Close and relaunch the application. The site is no longer listed (deletion persisted).

### UAT-W3.3 — Cancel Preserves Site

- [X] Click **No** on the confirmation dialog (or close the message box).
- [X] The site card remains on the Start Page. No data was deleted.

### UAT-W3.4 — Delete Removes All Site Data

- [ ] Delete a site that has recorded pages, generated controls, and analysis results.
- [ ] The site and all associated data are removed from the database (cascading delete).
- [ ] No orphan rows remain in related tables (snapshots, elements, controls, page objects).

### UAT-W3.5 — Delete Last Site Shows Empty State

- [ ] If only one site exists, delete it.
- [ ] The Start Page shows the empty state (no cards) without throwing an exception.

---

## W4 — Open Site with Corpus Page URL

### UAT-W4.1 — Event Wiring Exists

- [ ] On the Start Page, a site card with recorded pages shows a list of recent corpus pages (page titles as hyperlinks or clickable entries).
- [ ] Clicking a corpus page entry triggers the `SiteOpenWithUrlRequested` event (no error, no unhandled event).

### UAT-W4.2 — Workspace Opens to Specific URL

- [ ] Click a corpus page entry (e.g. "Login Page — https://example.com/login") on a site card.
- [ ] The Start Page transitions to the Tabbed Workspace for that site.
- [ ] The Scraping tab is active (tab index 0).
- [ ] The browser navigates to the corpus page's URL (e.g. "https://example.com/login"), **not** the site's default Start URL.

### UAT-W4.3 — Address Bar Shows Page URL

- [ ] After the workspace opens via a corpus page click, the address bar displays the page URL.
- [ ] The status bar shows the page URL after navigation completes.

### UAT-W4.4 — Normal Open Still Uses Start URL

- [ ] Click the regular **Open** button on the same site card (not a corpus page link).
- [ ] The workspace opens and the browser navigates to the site's configured Start URL (not a corpus page URL).
- [ ] This confirms the two navigation paths are independent.

### UAT-W4.5 — Invalid or Missing URL Handled Gracefully

- [ ] If a corpus page entry has a blank or null URL, clicking it does nothing (the `OnOpenCorpusPage` guard returns early).
- [ ] No exception is thrown. The Start Page remains visible.

### UAT-W4.6 — Workspace Disposes Correctly

- [ ] Open a site via a corpus page URL. Return to the Start Page. Open a different site normally.
- [ ] The previous workspace is disposed (no duplicate WebView2 processes in Task Manager).
- [ ] The new site loads correctly with its own Start URL.

---

## Cross-Cutting

### UAT-CC.1 — Event Subscriptions in ShowStartPage

- [ ] Verify that `ShowStartPage` subscribes to all four events: `NewSiteRequested`, `EditSiteRequested`, `DeleteSiteConfirmRequested`, and `SiteOpenWithUrlRequested`.
- [ ] Verify that returning to the Start Page after a workspace session re-subscribes correctly (no duplicate subscriptions causing double-fires).

### UAT-CC.2 — Event Unsubscription on Dispose

- [ ] Open a site (leaving the Start Page). The Start Page event handlers are unsubscribed in `DisposeStart()`.
- [ ] Return to the Start Page. Events are re-subscribed. Actions work correctly on the first click (no stale handlers, no missing handlers).

### UAT-CC.3 — No Regression on Existing Flows

- [ ] Open a site via the **Open** button → workspace loads (existing `SiteSelected` flow unchanged).
- [ ] Click **⚙ Settings** in the footer → settings open (existing `SettingsRequested` flow unchanged).
- [ ] Search filtering on the Start Page still works.
- [ ] Site cards still render with correct metadata (page count, control count, last-opened time).

---

## Sign-off

| Section                              | Tester | Date | Result |
| ------------------------------------ | ------ | ---- | ------ |
| W1 — New Site Dialog                |        |      |        |
| W2 — Edit Site Dialog               |        |      |        |
| W3 — Delete Site Confirmation       |        |      |        |
| W4 — Open Site with Corpus Page URL |        |      |        |
| Cross-Cutting                        |        |      |        |
