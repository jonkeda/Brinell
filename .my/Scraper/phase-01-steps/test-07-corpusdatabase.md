# Test 1.7 — CorpusDatabase Tests

## Covers

Step 01, 07 — `CorpusDatabase` (SQLite CRUD for sites)

## Tests (6)

| Test | Description |
|------|-------------|
| `EnsureCreated_CreatesSitesTable` | Table exists after init |
| `CreateSite_InsertsAndReturnsId` | Site persisted, ID > 0 |
| `GetAllSites_ReturnsAllSites` | All sites returned |
| `GetAllSites_ReturnsEmpty_WhenNoSites` | Empty list |
| `TouchSite_UpdatesLastOpenedAt` | Timestamp updated |
| `CreateSite_WithAliases_StoresCorrectly` | Pipe-delimited aliases round-trip |

## Implementation Notes

- Use in-memory SQLite: `Data Source=:memory:` — connection must stay open for the test's lifetime
- Create a fresh `CorpusDatabase` instance per test pointing to the in-memory DB
- Verify `CreateSite` returns a valid ID and `GetAllSites` retrieves the same data
- For aliases test, create a site with `UrlAliases = ["https://a.com", "https://b.com"]`, retrieve it, verify the list
- `TouchSite` test: create a site, note `LastOpenedAt`, call `TouchSite`, verify timestamp changed
