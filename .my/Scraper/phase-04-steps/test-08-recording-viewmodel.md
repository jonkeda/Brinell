# Test 4.8 — RecordingViewModel Tests

**Covers:** Step 4.7 — `RecordingViewModel` (recording mode state management, page transition capture, deduplication)

**File:** `Brinell.Scraper.Tests/ViewModels/RecordingViewModelTests.cs`

## Test Inventory (7 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `StartRecording_SetsIsRecordingTrue` | After calling `StartRecordingCommand.Execute()`, `IsRecording == true` |
| 2 | `StopRecording_SetsIsRecordingFalse` | After starting and then stopping, `IsRecording == false` |
| 3 | `PauseRecording_SetsIsPausedTrue` | After starting and then pausing, `IsPaused == true` and `IsRecording` remains true |
| 4 | `OnPageTransition_CapturesSnapshot` | When recording and a page transition occurs, `SessionSnapshots.Count` increments by 1 |
| 5 | `OnPageTransition_SkipsDuplicateWithin2Seconds` | Two transitions to the same URL within 2 seconds — only the first is captured (`SessionSnapshots.Count == 1`) |
| 6 | `SessionSnapshots_TracksNewPages` | After 3 page transitions to different URLs, `SessionSnapshots.Count == 3` |
| 7 | `StopRecording_FiresAnalyzePrompt` | After stopping recording, `AnalyzePromptRequested` event (or callback) fires so the UI can show "Analyze corpus now?" |

## Notes

- `RecordingViewModel` depends on `DomCaptureService` and `CorpusService` — mock both with NSubstitute.
  - `DomCaptureService` mock returns a pre-built `DomSnapshot` from `CaptureAsync()`.
  - `CorpusService` mock accepts `StoreSnapshotAsync()` calls without side effects.
- `OnPageTransition(string url)` is the entry point for page transition events — called by the SPA transition detector or navigation events.
- Test 5 (dedup): call `OnPageTransition("https://example.com/page1")` twice in quick succession — second call should be filtered.
- `SessionSnapshots` is a `List<DomSnapshot>` or `ObservableCollection<DomSnapshot>` tracking pages captured in the current recording session.
- `IsRecording`, `IsPaused` should fire `PropertyChanged` — verify via event subscription.
- `RecordingStatus` string property updates to `"+N new │ M total"` after each capture — could add an additional test if desired.
- `ILogger<RecordingViewModel>` dependency can use `NullLogger<RecordingViewModel>.Instance`.
- Mock setup example:
  ```csharp
  var captureService = Substitute.For<IDomCaptureService>();
  captureService.CaptureAsync(Arg.Any<CoreWebView2>()).Returns(testSnapshot);

  var corpusService = Substitute.For<ICorpusService>();
  ```
- If `CaptureAsync` requires a `CoreWebView2` parameter that can't be mocked, the ViewModel should use an abstraction (`IDomCaptureService`) or the test should call `OnPageTransition` which internally handles the capture.
- No WPF dispatcher dependency — state management is on the calling thread.
