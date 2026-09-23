# Step 13.2 — ControlObject Generator Integration

## Objective

Wire the existing `ControlGenerationService` into the analyzer pipeline so that approved `ControlProposal` items become persisted `GeneratedControl` rows, with retry-on-validation-error and auto skill regeneration.

## Dependencies

- Step 13.1 (ControlObject Analyzer producing approved proposals)
- Existing `ControlGenerationService`, `IControlRegistry`, `CodeValidator`, `CodeBlockParser`
- Step 13.3 (skill auto-generation runs after this)

## Implementation

### Files

- Update: `Services/ControlGenerationService.cs` (ensure `GenerateAllApprovedAsync` exists)
- Update: `Services/PromptBuilder.cs` (control generation prompt — should already exist, verify retry-with-error path)
- New: `Services/ControlGenerationOptions.cs` if not present

### Method contract

```csharp
public async Task<List<GeneratedControl>> GenerateAllApprovedAsync(
    List<ControlProposal> approvedProposals,
    string targetNamespace,
    LocatorReport? locatorReport,
    CancellationToken ct = default)
{
    var results = new List<GeneratedControl>();
    foreach (var p in approvedProposals.Where(x => x.IsApproved))
    {
        var generated = await GenerateOneAsync(p, targetNamespace, locatorReport, ct);
        if (generated != null)
        {
            await _registry.StoreControlAsync(generated, ct);
            results.Add(generated);
        }
    }
    return results;
}
```

### Single-proposal flow

```
1. prompt = PromptBuilder.BuildControlObjectPrompt(proposal, namespace, locatorReport)
2. response = ICopilotService.GenerateAsync(prompt)
3. blocks = CodeBlockParser.ExtractCSharpBlocks(response)
4. code = blocks[0]
5. validation = CodeValidator.Validate(code, registry: null /* no custom types yet */)
6. if validation.HasErrors:
       retryPrompt = prompt + "\n\nPrevious attempt had errors:\n" + validation.Errors
       response2 = ICopilotService.GenerateAsync(retryPrompt)
       code = CodeBlockParser.ExtractCSharpBlocks(response2)[0]
       validation = CodeValidator.Validate(code, null)
       if validation.HasErrors → mark proposal failed, continue
7. return new GeneratedControl {
       Name, Namespace, Code, DomSignature = proposal.DomSignature,
       Confidence = proposal.Confidence, CreatedAt = UtcNow,
       SuggestedProperties = proposal.SuggestedProperties
   }
```

### Validation rules at this stage

- Roslyn syntax pass.
- All control type references must resolve to **built-in** types (TextInputControl, ButtonControl, etc.) — custom types not yet generated.
- Locator method calls validated; emit Warning (not Error) on `Locator.ByCss` usage.

### Retry policy

- Max 1 retry per proposal.
- On second failure, set `proposal.GenerationStatus = Failed` and surface error in `ControlObjectsTabViewModel` for manual edit.

### Logging

- `Generating control {Name} (signature={DomSignature}, confidence={Confidence})`
- On retry: `Retry 1 for {Name}: {ErrorSummary}`
- On success: `Generated control {Name} ({CodeLength} chars) in {Elapsed} ms`

### Post-generation

- After `GenerateAllApprovedAsync` returns, the `PipelineOrchestrator` (Step 13.4) invokes `SkillService.GenerateSiteControlsSkillAsync(siteId)` (Step 13.3).

## Checklist

- [ ] `GenerateAllApprovedAsync` iterates approved proposals only
- [ ] Single-proposal flow includes one retry on validation errors
- [ ] Built-in control types validated at this stage
- [ ] `ByCss` produces warning, not error
- [ ] Each successful result persisted via `IControlRegistry.StoreControlAsync`
- [ ] Failed proposals marked with status (no exception propagation)
- [ ] Logging matches conventions from Phase 5 (LLM logging step)
