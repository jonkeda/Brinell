# Step — Generation Batch Logging

## Objective

Log generation batch stats: queued/completed/failed counts and total token usage.

## Dependencies

- Phase 3 logging framework (step 3.1)

## Implementation

### Logger category

`"Brinell.Scraper.Generator"`

### Generation batch tracking

```csharp
_logger.LogInformation(
    "Generation batch — Queued: {QueuedCount}, Completed: {CompletedCount}, " +
    "Failed: {FailedCount}, Total tokens: {TotalTokens}",
    queuedCount, completedCount, failedCount, totalTokens);
```

## Checklist

- [ ] Generation batch logged with queued/completed/failed counts and total tokens
