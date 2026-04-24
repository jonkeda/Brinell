# Step 4.11 — Export/Import DOM Snapshots

## Objective

Serialize DOM snapshots to JSON for sharing and re-importing into the corpus.

## Dependencies

- Step 4.1 (DomSnapshot model)
- Step 4.8 (CorpusService for import storage)

## Implementation

### Export

- Serialize via `System.Text.Json` with `WriteIndented = true`, `JsonNamingPolicy.CamelCase`.
- Default filename: `{site}-{page}-{timestamp}.json`.
- Single snapshot export from Corpus Browser or inspector.
- Batch export: entire site corpus as a folder of JSON files.

### Import

- Load JSON file, deserialize to `DomSnapshot`.
- Store into corpus via `CorpusService.StoreSnapshotAsync()`.
- Batch import: select a folder of JSON files.
- Validate JSON structure before importing.

### UI

- Export button in Corpus Browser toolbar and inspector panel.
- Import via `File → Import Snapshot` or drag-and-drop.

## Checklist

- [ ] Single snapshot exports as indented JSON with camelCase naming
- [ ] Filename follows `{site}-{page}-{timestamp}.json` convention
- [ ] Batch export of entire site corpus works
- [ ] Import deserializes JSON and stores via CorpusService
- [ ] Batch import from folder works
- [ ] Invalid JSON is rejected with clear error message
