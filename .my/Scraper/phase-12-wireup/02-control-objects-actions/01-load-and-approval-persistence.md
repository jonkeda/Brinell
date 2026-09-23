# Step 12.W.1 — Load Proposals & Persist Approval State

## Objective

Wire `LoadControlObjects` to merge generated controls from `IControlRegistry` with proposal data from `CorpusService.GetCurrentAnalysisResult`, and persist `Approve`/`Reject` actions to the corpus store so approval state survives app restart.

## Dependencies

- `CorpusService.GetCurrentAnalysisResult(siteId)` — returns `AnalysisResult?` containing JSON with proposals array
- `CorpusService.UpdateProposalApproval(siteId, proposalName, isApproved)` — persists approval flag
- `IControlRegistry.GetAllControls()` — returns generated controls
- Existing `ControlObjectItemViewModel` (or equivalent row VM) with `Name`, `Status`, `IsGenerated` properties

## Implementation

### Files

- **Modify**: `ControlObjectsTabViewModel.cs` — update `LoadControlObjects`, `Approve`, `Reject`
- **Modify** (if needed): `ControlObjectItemViewModel.cs` — ensure `Status` enum covers `Pending`, `Approved`, `Rejected`, `Generated`

### Code sketch

```csharp
// ControlObjectsTabViewModel.cs

private async Task LoadControlObjects(string siteId)
{
    var generated = _controlRegistry.GetAllControls();
    var analysisResult = _corpusService.GetCurrentAnalysisResult(siteId);

    var items = new List<ControlObjectItemViewModel>();

    // Parse proposals from analysis result
    if (analysisResult?.Json is not null)
    {
        var proposals = JsonSerializer.Deserialize<ControlObjectAnalysisResult>(analysisResult.Json);
        foreach (var proposal in proposals.Proposals)
        {
            var matchingGenerated = generated.FirstOrDefault(g =>
                string.Equals(g.Name, proposal.Name, StringComparison.OrdinalIgnoreCase));

            var status = matchingGenerated is not null
                ? ControlObjectStatus.Generated
                : proposal.IsApproved switch
                {
                    true => ControlObjectStatus.Approved,
                    false => ControlObjectStatus.Rejected,
                    null => ControlObjectStatus.Pending
                };

            items.Add(new ControlObjectItemViewModel
            {
                Name = proposal.Name,
                Description = proposal.Description,
                Status = status,
                IsGenerated = matchingGenerated is not null,
                Proposal = proposal
            });
        }
    }

    // Add any generated controls that have no matching proposal
    foreach (var gen in generated)
    {
        if (items.All(i => !string.Equals(i.Name, gen.Name, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(new ControlObjectItemViewModel
            {
                Name = gen.Name,
                Status = ControlObjectStatus.Generated,
                IsGenerated = true
            });
        }
    }

    ControlObjects = new ObservableCollection<ControlObjectItemViewModel>(items);
}

private void Approve(ControlObjectItemViewModel item)
{
    item.Status = ControlObjectStatus.Approved;
    _corpusService.UpdateProposalApproval(_siteId, item.Name, isApproved: true);
    _logger.LogInformation("Approved proposal: {Name}", item.Name);
}

private void Reject(ControlObjectItemViewModel item)
{
    item.Status = ControlObjectStatus.Rejected;
    _corpusService.UpdateProposalApproval(_siteId, item.Name, isApproved: false);
    _logger.LogInformation("Rejected proposal: {Name}", item.Name);
}
```

### Behavior

- On tab load (or site change), `LoadControlObjects` fetches both generated controls and the current analysis result.
- Proposals without a matching generated control display as Pending, Approved, or Rejected based on persisted `IsApproved` value.
- Proposals with a matching generated control display as Generated regardless of approval flag.
- Generated controls with no proposal appear at the end of the list marked Generated.
- `Approve` sets in-memory status AND persists via `UpdateProposalApproval`.
- `Reject` sets in-memory status AND persists via `UpdateProposalApproval`.
- If `GetCurrentAnalysisResult` returns null (no analysis run yet), only generated controls appear.

## Checklist

- [ ] `LoadControlObjects` calls `CorpusService.GetCurrentAnalysisResult(siteId)`
- [ ] Proposals deserialized from analysis JSON
- [ ] Merge logic matches proposals to generated controls by name (case-insensitive)
- [ ] Status enum has `Pending`, `Approved`, `Rejected`, `Generated` values
- [ ] `Approve` calls `UpdateProposalApproval(siteId, name, true)`
- [ ] `Reject` calls `UpdateProposalApproval(siteId, name, false)`
- [ ] Null analysis result handled gracefully (empty proposals list)
- [ ] UI updates after approve/reject (PropertyChanged on Status)
