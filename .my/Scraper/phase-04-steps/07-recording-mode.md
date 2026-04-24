# Step 4.7 — Recording Mode

## Objective

Navigate through a site, capture each page automatically to the corpus. Recording only stores DOM snapshots — no LLM generation during recording.

## Dependencies

- Step 4.1 (DOM capture)
- Step 4.6 (SPA page transition detection)
- Step 4.8 (corpus store for persistence)
- Phase 1 (⏺ Record toolbar button)

## Implementation

### Recording flow

```csharp
private bool _isRecording;
private readonly List<DomSnapshot> _sessionSnapshots = [];

private async void OnPageTransitionDetected(string url)
{
    if (!_isRecording) return;

    await WaitForStableState();

    var snapshot = await _domCaptureService.CaptureAsync(_webView.CoreWebView2);
    snapshot.SiteName = _activeSite.Name;
    snapshot.PageName = InferPageName(url, snapshot.PageTitle);

    await _corpusService.StoreSnapshotAsync(_activeSite, snapshot);
    _sessionSnapshots.Add(snapshot);

    RecordingStatus = $"+{_sessionSnapshots.Count} new │ {_activeSite.TotalPages} total";
}
```

### Controls

- **Start** (⏺): Set `_isRecording = true`, show red border around browser, attach transition detector.
- **Pause** (⏸): Temporarily stop capturing without ending the session.
- **Stop** (⏹): End recording. Prompt: "Analyze corpus now?" → if yes, switch to Analysis view.

### UI behavior

- ⏺ becomes ⏹ (stop) + ⏸ (pause) during recording.
- Red border around browser indicates active recording.
- Sidebar splits into "This Session" (new pages) and "Previous" (existing corpus pages).
- Re-recording a known page overwrites its snapshot (old version kept in history for diffing).
- Duplicate transitions filtered (same URL within 2-second window).

## Checklist

- [ ] Start/pause/stop recording controls work
- [ ] Red border shown during active recording
- [ ] Each page transition triggers automatic DOM capture
- [ ] Sidebar separates "This Session" from "Previous" pages
- [ ] Re-recording overwrites latest snapshot (old kept in history)
- [ ] Duplicate transitions within 2 seconds are filtered
- [ ] Stop prompts "Analyze corpus now?"
