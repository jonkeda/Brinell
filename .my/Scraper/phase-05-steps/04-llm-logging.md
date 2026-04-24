# Step 3.4 — LLM Request/Response Logging

## Objective

Log every LLM call for both the Analyzer and Generator agents with model, token usage, and timing.

## Dependencies

- Step 3.1 (logging framework)

## Implementation

### Logger categories

Two-model strategy — dedicated categories per agent:

- `"Brinell.Scraper.Analyzer"` — analysis calls (cheaper model)
- `"Brinell.Scraper.Generator"` — generation calls (smarter model)
- `"Brinell.Scraper.Llm"` — shared LLM plumbing

### Request/response logging (both agents)

```csharp
_logger.LogInformation(
    "LLM request — Agent: {Agent}, Model: {Model}, Prompt length: {PromptLength} chars",
    agent, model, prompt.Length);

_logger.LogInformation(
    "LLM response — Agent: {Agent}, Model: {Model}, Response length: {ResponseLength} chars, " +
    "Tokens: {PromptTokens}+{CompletionTokens}={TotalTokens}, Elapsed: {ElapsedMs} ms",
    agent, model, response.Length, usage.PromptTokens, usage.CompletionTokens,
    usage.TotalTokens, stopwatch.ElapsedMilliseconds);
```

### Agent-specific logging

**Analyzer:**
```csharp
_logger.LogInformation(
    "Analysis — Pages analyzed: {PageCount}, Patterns detected: {PatternCount}, " +
    "Custom controls proposed: {ControlCount}",
    pageCount, patternCount, controlCount);
```

**Generator:**
```csharp
_logger.LogInformation(
    "Generation — Page: {PageName}, Custom controls used: {ControlNames}",
    pageName, string.Join(", ", controlNames));
```

### Prompt logging levels

- `Debug` — prompt text truncated to first 500 chars
- `Trace` — full prompt and response for diagnostics
- `Error` — exception with agent name, model, prompt length

### Session tracking

Track cumulative token usage per session per agent (surfaced in status bar or log viewer).

## Checklist

- [ ] Every LLM call logged with agent, model, prompt length, response length, tokens, elapsed time
- [ ] Analyzer and Generator use separate logger categories
- [ ] Prompt text logged at Debug (truncated) and Trace (full) levels
- [ ] Errors logged with exception, agent, model, prompt length
- [ ] Cumulative session token tracking works
