# RCA-014: IFrame Page Transitions Not Recorded (UAT-4.7)

**Reported:** 2026-05-04
**Severity:** High
**Component:** `Services/RecordingService.cs`, `Services/PageTransitionDetector.cs`
**UAT Reference:** UAT-4.7 — Recording Mode

---

## Symptoms

During a recording session, when a page inside an iframe transitions to a different URL (e.g. a multi-step form, an embedded application, or an OAuth flow within an iframe), the transition is not detected and no DOM capture is triggered. Only top-level page navigations are recorded. The iframe content changes silently without appearing in the "This Session" sidebar list.

## Root Cause

The page transition detection is only wired up to the top-level `CoreWebView2.NavigationCompleted` and `CoreWebView2.HistoryChanged` events. These events fire only for the main frame. Navigations that occur inside iframes (whether full navigations or SPA-style `pushState`/`replaceState` within the iframe) do not trigger these top-level events.

The `PageTransitionDetector` monitors:

- `NavigationCompleted` — only fires for the main frame
- `HistoryChanged` — only fires for main frame history changes
- The injected `MutationObserver` / `popstate` listener — only injected into the top-level document

None of these mechanisms observe navigation within child frames.

## Expected Behavior

- When an iframe navigates to a new URL (full navigation), a page transition event should fire and a DOM capture should be triggered.
- When an iframe performs a client-side navigation (`pushState`/`replaceState`), the SPA transition detector should catch it.
- Each iframe navigation should appear in the "This Session" sidebar with a notation indicating it occurred within an iframe (e.g. `[iframe] Page Name`).

## Fix

1. Subscribe to `CoreWebView2.FrameCreated` to track all child frames.
2. For each `CoreWebView2Frame`, listen for `NavigationCompleted` to detect full iframe navigations.
3. Inject the SPA transition detection script (`popstate` + `MutationObserver`) into each iframe's document via `CoreWebView2Frame.ExecuteScriptAsync`.
4. When an iframe navigation is detected, trigger a DOM capture that includes the updated iframe content and log it with an `[iframe]` prefix.
5. Apply the same 2-second deduplication window to iframe transitions to prevent capture floods.

## Verification

- [X] Start recording. Navigate to a page with an iframe that has internal navigation (e.g. multi-step form). Click through steps inside the iframe. Each step transition is captured and appears in the "This Session" list.
- [X] Verify captured pages include the updated iframe DOM content, not stale content from the initial load.
- [X] Verify deduplication: rapidly navigating inside the iframe does not produce duplicate captures within a 2-second window.
- [X] Verify top-level navigation recording is unaffected — navigating the main page still captures as before.
