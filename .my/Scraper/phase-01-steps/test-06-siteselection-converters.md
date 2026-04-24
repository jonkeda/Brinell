# Test 1.6 — SiteSelectionViewModel & Converters

## Covers

Step 07 — `SiteSelectionViewModel` (site loading, selection events)
Step 03 — `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`

## SiteSelectionViewModel Tests (6)

| Test | Description |
|------|-------------|
| `LoadSites_PopulatesSitesCollection` | Sites loaded from DB |
| `SelectSiteCommand_FiresSiteSelected` | Event fires with SiteInfo |
| `NewSiteCommand_FiresNewSiteRequested` | Event fires |
| `Sites_IsEmpty_WhenNoSitesInDb` | Empty collection |
| `SelectedSite_InitiallyNull` | No selection |
| `SiteSelected_IncludesCorrectSiteInfo` | Event arg matches selected |

## Converter Tests (4)

| Test | Description |
|------|-------------|
| `BoolToVisibility_True_ReturnsVisible` | true → Visible |
| `BoolToVisibility_False_ReturnsCollapsed` | false → Collapsed |
| `InverseBoolToVisibility_True_ReturnsCollapsed` | true → Collapsed |
| `InverseBoolToVisibility_False_ReturnsVisible` | false → Visible |

## Implementation Notes

- Mock `CorpusDatabase` with NSubstitute — return canned `SiteInfo` lists
- For converters, call `Convert()` directly and assert against `Visibility` enum values
- Test `ConvertBack()` as well for completeness
