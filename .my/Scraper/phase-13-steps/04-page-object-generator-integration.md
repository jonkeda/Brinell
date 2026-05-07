# Step 13.4 — PageObject Generator Integration

## Objective

Adapt the existing `PageGenerationService` to be ControlObject-aware: match page DOM elements against registered ControlObjects and emit references to them in generated PageObject code, falling back to inline `ContainerBase` classes for page-specific patterns.

## Dependencies

- Step 13.2 (controls in registry)
- Step 13.3 (site-controls skill loaded in generator agent)
- Step 13.5 (`ControlObjectMatcher`)
- Existing `PageGenerationService`, `CodeBlockParser`, `CodeValidator`

## Implementation

### Files

- Update: `Services/PageGenerationService.cs`
- Update: `Services/PromptBuilder.cs` (improved `BuildPageObjectPrompt`)
- Update: `Services/CodeValidator.cs` (validate against registry)

### Method contract

```csharp
public async Task<PageGenerationResult> GeneratePageAsync(
    DomSnapshot snapshot,
    string targetNamespace,
    LocatorReport? locatorReport,
    List<ControlGroupSuggestion>? containerGroups = null,
    CancellationToken ct = default)
```

### Flow

```
1. actionable = FilterActionable(snapshot.Elements)
2. containerGroups ??= _detector.Detect(snapshot.RootElement)
3. registeredControls = _registry.GetControlsAsync(snapshot.SiteId)
4. matches = _matcher.MatchAll(snapshot, registeredControls)   // Step 13.5
5. prompt = PromptBuilder.BuildPageObjectPrompt(
       snapshot, actionable, containerGroups, matches,
       registeredControls, locatorReport, targetNamespace)
6. response = ICopilotService.GenerateAsync(prompt)
7. blocks = CodeBlockParser.ExtractCSharpBlocks(response)
       mainCode = blocks[0]
       containerCodes = blocks[1..]
8. validation = CodeValidator.ValidateWithRegistry(
       mainCode, registeredControls, containerCodes)
9. if validation.HasErrors:
       retry once with error feedback
10. return new PageGenerationResult {
        ClassName, Namespace, MainCode, ContainerCodes,
        UsedControlObjects = matches.Distinct(),
        Validation, GeneratedAt = UtcNow
    }
```

### Prompt changes

`BuildPageObjectPrompt` (per Phase 13.6 spec):

- Page metadata (URL, title, element count, snapshot ID)
- Actionable elements: compact DOM snippet (truncate to N elements; paginate if needed)
- **Available custom ControlObjects**: list `name`, `signature`, properties from registry
- **Pre-computed matches**: for each match, "Use `{ControlName}` for element `{xpath}`"
- Container group suggestions for unmatched groups
- Locator preference order (ByText > ByDataTestId > ByAriaLabel > ById > ByCss)
- Type whitelist: built-in controls + custom controls from registry only

### Validation rules

`CodeValidator.ValidateWithRegistry`:

| Check | Severity |
|---|---|
| Roslyn syntax | Error |
| Type resolution against (built-ins ∪ registry) | Error if unresolved |
| `Locator.ByCss` usage | Warning |
| Property name uniqueness within class | Error |
| Class derives from `HtmlPageObjectBase<Self>` | Error |
| Inline containers derive from `ContainerBase<TParent, TScope>` | Error |

### Retry policy

- Max 1 retry; append validation errors to prompt with explicit "fix these errors" instruction.
- On second failure, return result with `Validation.HasErrors=true` and `Status=Error` for UI display.

## Checklist

- [ ] `GeneratePageAsync` accepts `LocatorReport` and uses ControlObject registry
- [ ] `PromptBuilder.BuildPageObjectPrompt` includes available controls + matches
- [ ] `ControlObjectMatcher` consulted before LLM call
- [ ] `CodeValidator.ValidateWithRegistry` resolves types against built-ins + registry
- [ ] One retry on validation errors with feedback prompt
- [ ] `PageGenerationResult` carries main code + container codes + used controls + validation
- [ ] Logging tracks elapsed, prompt length, retry count
