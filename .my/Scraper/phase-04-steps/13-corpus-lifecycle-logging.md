# Step — Corpus Lifecycle Logging

## Objective

Log corpus open/create events with site name, page count, and last recording date.

## Dependencies

- Phase 3 logging framework (step 3.1)

## Implementation

### Logger category

`"Brinell.Scraper.Corpus"`

### Corpus lifecycle

```csharp
_logger.LogInformation(
    "Corpus opened — Site: {SiteName}, Pages: {PageCount}, Last recorded: {LastRecordingDate}",
    siteName, pageCount, lastRecordingDate);
```

## Checklist

- [ ] Corpus open/create logged with site name, page count, last recording date
