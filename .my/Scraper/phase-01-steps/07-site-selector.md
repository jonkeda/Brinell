# Step 1.7 — Start Screen / Site Selector

## Objective

On launch, show a site selector view where the user creates or selects a site corpus before the browser appears.

## Dependencies

- Step 1.1 (project structure)
- Step 1.2 (MVVM foundation)
- Step 1.3 (main window layout to transition into)
- NuGet: `Microsoft.Data.Sqlite` (corpus storage)

## Implementation

### Site selector view

On launch, show a **site selector** view (either a separate `Window` or the initial `ActiveView` in `MainWindow`) instead of going directly to the browser.

The site selector displays:
- A list of existing site corpuses (name, URL, last-opened date, page/control counts)
- A **"New Site"** button that opens a dialog

### New Site dialog

Collects:
- **Site name** — display name (e.g. "Exact Online")
- **Start URL** — the base URL to open in the browser
- **URL aliases** — additional URL patterns for regional variants (e.g. `start.exactonline.nl`, `.be`, `.de`) that share the same control set
- **Namespace** — C# namespace for generated code (e.g. `ExactOnline`)
- **Output path** — where generated Brinell page objects are written

### Storage

Per-site settings are stored in the **corpus SQLite database** (`Data/` folder).

### Navigation flow

1. App starts → site selector view
2. User picks an existing site or creates a new one
3. App transitions to main window layout (sidebar + browser)
4. "Site → Switch Site" menu item returns to the site selector

## Checklist

- [ ] App launches into site selector (not directly into browser)
- [ ] Existing corpuses listed with name, URL, last-opened date, counts
- [ ] "New Site" button opens a dialog
- [ ] Dialog validates required fields (name, start URL)
- [ ] After selection, main window shows sidebar + browser at the start URL
- [ ] "Switch Site" menu item returns to site selector
- [ ] Site settings persisted in SQLite
