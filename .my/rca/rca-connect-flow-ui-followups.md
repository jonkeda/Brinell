# RCA: Connect Flow Follow-up (Auto Auth Works, UX Regressions Remain)

Date: 2026-05-08  
Scope: Brinell.Scraper Settings > GitHub Copilot section

## Symptoms Reported

1. Auto auth flow now starts and can complete.
2. Error message still shown after flow.
3. Refresh button appears missing.
4. Message text is too long.
5. Button should be to the left of the message.
6. Need validation of model naming.

## Root Causes

1. Failure status used raw CLI exception text.
- The UI displayed unbounded `LastInitError` strings from `session.create` failures.
- This produced very long status messages and made success/failure transitions hard to read.

2. Copilot action layout prioritized status text over controls.
- In the Copilot section, status text was shown before the sign-in action.
- This made the primary action less prominent and did not match the desired left-to-right flow.

3. Refresh affordance existed only as a small icon in the Models group header.
- Users looked for refresh in the Copilot connection area and perceived it as missing.

4. Fallback model seed list drifted from current naming used in adjacent projects.
- Prior list mixed older IDs and omitted some current aliases.
- This increased chance of stale/manual values diverging from expected model identifiers.

## Implemented Fixes

1. Short failure status formatting
- Added a status formatter that maps long/raw errors to concise messages.
- Session creation/auth failures now show short actionable text.
- Long unknown errors are trimmed with ellipsis.

2. Copilot action bar re-ordered
- "Sign in to GitHub" button remains in Copilot section and is now before status text.
- Added explicit "Refresh models" button in the same action bar.

3. Status text constrained in UI
- Added width + ellipsis trimming in XAML.
- Full status remains available via tooltip.

4. Fallback model names updated
- Updated fallback list to align with current Oravey-side naming patterns:
  - gpt-4.1
  - gpt-4.1-mini
  - gpt-4.1-nano
  - o4-mini
  - o3-mini
  - claude-haiku-4.5
  - claude-sonnet-4.6
  - claude-opus-4.6

## Model Naming Validation Notes

- Cross-check source used: Oravey settings list (current local source of truth in this workspace).
- Online docs check was inconclusive for strict SDK session model ID matrix; authoritative runtime source remains `ListModelsAsync()` in authenticated context.
- Product behavior should prefer runtime-discovered list whenever available.

## Remaining Risk

- Model availability is account/entitlement-dependent. Even syntactically correct IDs can fail per-user.
- If login succeeds but entitlement is missing, session creation can still fail.
