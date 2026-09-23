# Step 12.W.8 — Unit Test Plan (Recording Sidebar Actions)

## Scope

This plan covers unit tests for Step 12.W.8a, 8b, and 8c in the active tabbed workspace flow:

- Sidebar/session panel population from corpus
- Recording session capture updates in real time
- Stop-recording analyze prompt flow and explicit transfer-to-corpus behavior
- Recording visual state signals exposed through view models

Out of scope:

- WPF visual rendering details (Border trigger paint, ListView visuals)
- MessageBox UI rendering itself (only interaction outcomes)
- WebView2 runtime internals (frame creation, browser process)

---

## Test Project Target

- Project: tools/Brinell.Scraper.Tests
- Framework: xUnit
- Mocking: NSubstitute
- Assertions: FluentAssertions (optional but recommended)

---

## Components Under Test

| Component | Focus |
|---|---|
| SessionPanelViewModel | Corpus/session state, session summary text, recording mode state |
| ScrapingTabViewModel | Recording event wiring, transition capture flow, stop/prompt/transfer logic |
| RecordingViewModel | Dedup and recording lifecycle contract used by Step 12.W.8 |

---

## Test Matrix

### A. SessionPanelViewModel (10 tests)

| ID | Test | Verifies |
|---|---|---|
| U8-A01 | Load_SetsSiteContext | SiteId and SiteHeader are set during load |
| U8-A02 | Load_PopulatesCorpusPages | CorpusPages mirrors CorpusService page list |
| U8-A03 | Load_PopulatesControls | Controls mirrors IControlRegistry list |
| U8-A04 | Load_UpdatesCorpusStats | CorpusStats shows X pages and Y controls |
| U8-A05 | SyncRecordedPages_AddsNewIcons | RecordedPages entries use new-page icon |
| U8-A06 | SessionSummary_WhenEmpty | Summary is No pages captured yet |
| U8-A07 | SessionSummary_WhenRecording | Summary is +N new · T total |
| U8-A08 | SessionSummary_WhenStopped | Summary is N captured this session |
| U8-A09 | NavigateToPage_InvokesCallback | Navigate callback receives selected URL |
| U8-A10 | SessionSummary_UpdatesOnCollectionChange | Property change emitted on recorded/corpus mutations |

### B. ScrapingTabViewModel Recording Capture (14 tests)

| ID | Test | Verifies |
|---|---|---|
| U8-B01 | Constructor_SyncsSessionFromRecording | Session panel mirrors existing session snapshots |
| U8-B02 | Constructor_SetsNavigateCallback | URL click routes through browser navigate command |
| U8-B03 | RecordingStarted_SetsSessionRecordingTrue | Session.IsRecording becomes true |
| U8-B04 | RecordingStarted_StartsSpaDetector_WhenWebViewAvailable | Detector starts and subscribes transition handler |
| U8-B05 | RecordingStarted_NoWebView_DoesNotThrow | Safe when WebView not initialized |
| U8-B06 | RecordingStopped_SetsSessionRecordingFalse | Session.IsRecording becomes false |
| U8-B07 | RecordingStopped_StopsSpaDetector_WhenActive | Detector stops and handler unsubscribes |
| U8-B08 | NavigationSucceeded_WhenRecording_CapturesAndTransitions | Top-level navigation capture path adds snapshot |
| U8-B09 | NavigationSucceeded_WhenNotRecording_SkipsCapture | No capture outside recording |
| U8-B10 | IFrameNavigationSucceeded_UsesIFrameTitlePrefix | Captured page name starts with iframe marker |
| U8-B11 | SpaTransitionDetected_UsesProvidedUrlForDedup | OnPageTransition receives transition URL from detector |
| U8-B12 | CaptureTransition_UsesTrackedFrames | Capture uses highlight tracked frames argument |
| U8-B13 | SessionSnapshotsChanged_ResyncsSessionList | Session panel list refreshes when snapshots change |
| U8-B14 | Dispose_UnsubscribesAllEvents | No residual handlers after dispose |

### C. Stop + Analyze Prompt Flow (12 tests)

| ID | Test | Verifies |
|---|---|---|
| U8-C01 | AnalyzePrompt_NoSnapshots_NoPrompt | Prompt path exits when count is zero |
| U8-C02 | AnalyzePrompt_Yes_TransfersAllSnapshots | All session snapshots persisted to corpus |
| U8-C03 | AnalyzePrompt_Yes_RefreshesSessionCorpus | Session.Load called to refresh corpus list |
| U8-C04 | AnalyzePrompt_Yes_ClearsRecordingSnapshots | Recording.ClearSnapshots called after transfer |
| U8-C05 | AnalyzePrompt_Yes_LeavesRecordingStopped | No implicit resume after transfer |
| U8-C06 | AnalyzePrompt_No_KeepsSnapshots | Session snapshots remain for review |
| U8-C07 | AnalyzePrompt_No_DoesNotStoreCorpus | No corpus writes on No path |
| U8-C08 | AnalyzePrompt_MessageIncludesCapturedCount | Prompt text contains count |
| U8-C09 | AnalyzeSession_InvalidSiteId_NoTransfer | Guard on invalid site context |
| U8-C10 | AnalyzeSession_EmptySnapshots_NoTransfer | Guard when no snapshots remain |
| U8-C11 | StopRecording_DoesNotClearImmediately | Stop alone keeps This Session data |
| U8-C12 | StopRecording_RemovesRecordingVisualStateSignal | Session.IsRecording false after stop |

### D. RecordingViewModel Contract Checks (6 tests)

| ID | Test | Verifies |
|---|---|---|
| U8-D01 | OnPageTransition_DedupsSameUrlWithin2Seconds | Duplicate URL within window is skipped |
| U8-D02 | OnPageTransition_AllowsSameUrlAfterWindow | Capture accepted after dedup window |
| U8-D03 | OnPageTransition_BlockedWhenPaused | Pause prevents capture |
| U8-D04 | StartRecording_FiresRecordingStarted | Event contract for listeners |
| U8-D05 | StopRecording_FiresRecordingStoppedAndPrompt | Stop and prompt events fired |
| U8-D06 | StopRecording_DoesNotClearSnapshotsByItself | Explicit transfer required to clear |

Total planned: 42 tests

---

## Test Seams and Fakes

Because MessageBox and WebView2 are static/runtime-coupled, use lightweight seams:

- Introduce an IMessageBoxService abstraction for deterministic Yes/No tests in ScrapingTabViewModel.
- Keep BrowserViewModel.GetCoreWebView2 as injectable delegate and substitute with stub object for capture tests.
- Use fake transition detector wrapper or test subclass to assert start/stop and handler attach behavior.

If production seams are not added yet, classify those tests as Pending until seam extraction lands.

---

## Suggested File Layout

- tests/Brinell.Scraper.Tests/ViewModels/Tabs/SessionPanelViewModelTests.cs
- tests/Brinell.Scraper.Tests/ViewModels/Tabs/ScrapingTabViewModelTests.cs
- tests/Brinell.Scraper.Tests/ViewModels/RecordingViewModelStep8Tests.cs
- tests/Brinell.Scraper.Tests/TestDoubles/FakeMessageBoxService.cs
- tests/Brinell.Scraper.Tests/TestDoubles/FakePageTransitionDetector.cs

---

## Exit Criteria

- All non-pending tests in A-D pass
- No regressions in existing RecordingViewModel tests
- New tests prove:
  - No automatic session wipe on stop
  - Yes transfers and clears
  - No preserves This Session
  - Capture paths for top-level, SPA, and iframe events are wired
