# sUser Acceptance Tests - Phase 12 Wireup 08d (Sidebar Selection Actions and Right Sidebar UX)

Manual test scenarios for phase-12-08 updates introduced after 08a/08b/08c:

- Selection-only behavior for Corpus and This Session lists
- Explicit action buttons (Navigate, Remove, Clear All)
- Resizable right sidebar on Scraping tab
- Non-empty fallback content when Inspector is hidden

## Prerequisites

- Windows 10/11 with .NET 10 runtime
- WebView2 runtime installed
- At least 1 site with existing corpus pages
- At least 2 captured recordings in This Session
- Scraping tab opened for an active site

---

## W8d.1 - Corpus List Selection and Actions

### UAT-W8d.1.1 - Corpus Row Click Selects Only

- [X] Open Scraping tab with at least one corpus page visible.
- [X] Click a corpus row once.
- [X] Row is selected/highlighted.
- [X] Browser does not navigate on row click.

### UAT-W8d.1.2 - Navigate Button Uses Selected Corpus Row

- [X] Select a corpus row.
- [X] Click `Navigate` in the Corpus actions.
- [X] Browser address updates to selected row URL.
- [X] Browser navigates only after button click.

### UAT-W8d.1.3 - Remove Button Uses Selected Corpus Row

- [X] Select a corpus row.
- [X] Click `Remove` in the Corpus actions.
- [X] Confirm deletion when prompted.
- [X] Selected corpus page is removed from corpus list.

### UAT-W8d.1.4 - Corpus Actions Disabled Without Selection

- [X] Ensure no corpus row is selected.
- [X] Verify `Navigate` is disabled.
- [X] Verify `Remove` is disabled.

---

## W8d.2 - Recordings List Selection and Actions

### UAT-W8d.2.1 - Recording Row Click Selects Only

- [X] Ensure This Session has at least one recording.
- [X] Click a recording row.
- [X] Row is selected/highlighted.
- [X] Browser does not navigate on row click.

### UAT-W8d.2.2 - Navigate Button Uses Selected Recording

- [X] Select a recording row.
- [X] Click `Navigate` in This Session actions.
- [X] Browser navigates to selected recording URL.

### UAT-W8d.2.3 - Remove Button Removes Selected Recording

- [X] Select a recording row.
- [X] Click `Remove` in This Session actions.
- [X] Selected recording is removed from This Session list.
- [X] Session summary updates accordingly.

### UAT-W8d.2.4 - Clear All Removes All Recordings

- [X] Ensure This Session has 2 or more recordings.
- [X] Click `Clear All`.
- [X] Confirm clear action when prompted.
- [X] All recordings are removed from This Session list.
- [X] Session summary becomes `No pages captured yet`.

### UAT-W8d.2.5 - Recording Actions Disabled Correctly

- [X] Ensure no recording row is selected.
- [X] Verify recording `Navigate` and `Remove` are disabled.
- [X] Ensure This Session is empty.
- [X] Verify `Clear All` is disabled.

---

## W8d.3 - Navigation Safety Guard

### UAT-W8d.3.1 - Cross-Site Selection Requires Confirmation

- [ ] Set current browser host to site A.
- [ ] Select corpus or recording item with URL on site B.
- [ ] Click `Navigate`.
- [ ] Confirmation dialog is shown.
- [ ] Choose `No` and verify navigation does not occur.

### UAT-W8d.3.2 - Cross-Site Confirmation Yes Navigates

- [ ] Repeat cross-site navigate flow.
- [ ] Choose `Yes` on confirmation dialog.
- [ ] Verify navigation proceeds to selected URL.

---

## W8d.4 - Right Sidebar Resize and Fallback Content

### UAT-W8d.4.1 - Right Sidebar Is Resizable on Scraping Tab

- [ ] On Scraping tab, drag the splitter left/right between browser and right sidebar.
- [ ] Right sidebar width changes while dragging.
- [ ] Sidebar cannot be resized below usable minimum width.

### UAT-W8d.4.2 - Inspector Hidden Shows Non-Empty Fallback

- [ ] Turn off/hide inspector state.
- [ ] Right sidebar remains visible.
- [ ] `Sidebar Actions` panel is visible.
- [ ] Selected corpus/recording summary text is visible.

### UAT-W8d.4.3 - Fallback Actions Work While Inspector Hidden

- [ ] Hide inspector.
- [ ] Select an item in left session panel.
- [ ] Use right fallback `Navigate` and `Remove` actions.
- [ ] Actions execute correctly without showing inspector.

---

## W8d.5 - Layout and Usability Polish Checks

### UAT-W8d.5.1 - Action Buttons Are Consistent

- [X] Verify action button size, spacing, and alignment are visually consistent.
- [X] Verify wrapping behavior is acceptable at narrower sidebar widths.

### UAT-W8d.5.2 - Section Cards Are Readable

- [X] Verify This Session and Corpus card grouping is clear.
- [X] Verify text remains readable at common DPI/scaling settings.

---

## Sign-off

| Section                                | Tester | Date | Result |
| -------------------------------------- | ------ | ---- | ------ |
| W8d.1 Corpus Selection and Actions     |        |      |        |
| W8d.2 Recordings Selection and Actions |        |      |        |
| W8d.3 Navigation Safety Guard          |        |      |        |
| W8d.4 Right Sidebar UX                 |        |      |        |
| W8d.5 Layout and Usability             |        |      |        |
