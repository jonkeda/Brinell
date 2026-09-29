# Screen capture: the Windows capture API, and one recorder API for every platform

Status: **design**, 2026-09-29. Serves step 1 of [plan.md](plan.md) (record the app's window).

## 1. Windows.Graphics.Capture

The API Windows itself uses for the Snipping Tool's video and Teams' window sharing: capture of one
window or one monitor, from the compositor (DWM), as GPU frames.

### The pieces

| Type | What it is |
| --- | --- |
| `GraphicsCaptureItem` | What is captured: a window or a monitor. From an `HWND` or `HMONITOR` through the COM interop `IGraphicsCaptureItemInterop.CreateForWindow` / `CreateForMonitor` (or a picker, which we do not want). `Size` and a `Closed` event. |
| `Direct3D11CaptureFramePool` | A pool of frame buffers on a Direct3D 11 device. `CreateFreeThreaded` raises `FrameArrived` on a worker thread (no UI thread needed). `Recreate` when the item's size changes. |
| `GraphicsCaptureSession` | Started with `StartCapture()`. `IsCursorCaptureEnabled` (off: the cursor is not ours to show), `IsBorderRequired` (off: no yellow border; Windows 11, after `GraphicsCaptureAccess.RequestAccessAsync(Borderless)`), `MinUpdateInterval` (Windows 11: caps the frame rate). |
| `Direct3D11CaptureFrame` | One frame: an `IDirect3DSurface`, its `ContentSize` and a `SystemRelativeTime` (a QPC time, the clock to line frames up with the step timeline). |

```text
HWND ─► IGraphicsCaptureItemInterop.CreateForWindow ─► GraphicsCaptureItem
D3D11 device ─► Direct3D11CaptureFramePool.CreateFreeThreaded(device, B8G8R8A8, 2, size)
pool.CreateCaptureSession(item) ─► session.StartCapture()
pool.FrameArrived ─► TryGetNextFrame() ─► surface + SystemRelativeTime ─► encoder
```

### Encoding

The frames are textures; something has to make a video of them:

| Route | How | For us |
| --- | --- | --- |
| **Media Foundation** `IMFSinkWriter` | H.264 in MP4 from D3D11 textures, hardware encoder, no copy to the CPU | the right end state; the most code |
| **`Windows.Media.Transcoding`** (`MediaStreamSource` → `MediaTranscoder`) | WinRT, frames handed over as samples | less code, works from .NET; what the `SimpleRecorder` sample does |
| **ffmpeg over a pipe** | copy each frame to the CPU, write raw BGRA to `ffmpeg -f rawvideo` | simplest, and ffmpeg is there anyway to add the voice (plan Q1); costs a copy per frame |

Start with **ffmpeg over a pipe** (the spike), keep the encoder behind the recorder so Media
Foundation can replace it.

### What it can and cannot do

- **Needs** Windows 10 1903 or later for a window (1803 for a monitor); the Presenter's TFM
  (`net10.0-windows10.0.19041.0`) has the projections. D3D11 device creation needs a little interop
  (`CreateDirect3D11DeviceFromDXGIDevice`), or a library (Vortice.Windows).
- **Captures a covered window**: the frames come from the window's own surface, so a window behind
  another still records correctly. That suits test runs, where the app is kept behind (QuietWindow).
- **A minimized window produces no frames.** A window off every monitor does (the "offscreen" test
  placement) as long as it is not minimized.
- **Follows the window**: moving it changes nothing; resizing changes `ContentSize`, and the pool is
  recreated at the new size (the encoder scales, or starts a new segment).
- **Yellow border** on Windows 10 around the captured window; on Windows 11 it can be turned off
  once the process has asked for borderless access.
- **Black content**: protected video (DRM), and windows that exclude themselves from capture
  (`SetWindowDisplayAffinity`).
- **Not input** (AD-005): it reads what the compositor already draws; no focus, no foreground.

### The alternatives on Windows

| API | What it captures | Why not first |
| --- | --- | --- |
| DXGI Desktop Duplication (ffmpeg `ddagrab`) | a whole monitor | a covered or offscreen window is not in it; crop by rectangle only |
| GDI `BitBlt` / `PrintWindow` (ffmpeg `gdigrab`) | a window or a region | slow, and WinUI and WebView content often comes out black with `BitBlt`; `PrintWindow` with full content is too slow for 30 fps |
| Libraries: ScreenRecorderLib, OBS | either of the above | fine for a tool, heavy for a test dependency |

## 2. One recorder API for every platform

The narrator records whatever the scenario drives: a MAUI window on Windows, a WPF or WinForms
window, a browser page, an Android device, an iOS simulator, a Stride window. One API, a
recorder per platform, chosen by what the test context says it runs.

### The API (`Brinell.Recording`, net10.0, no platform types)

```csharp
public interface IScreenRecorder : IAsyncDisposable
{
    /// <summary>Whether this recorder can record the target.</summary>
    bool CanRecord(RecordingTarget target);

    /// <summary>Starts recording; the frames are timed on the recording clock.</summary>
    Task<IRecording> StartAsync(RecordingTarget target, RecordingOptions options, CancellationToken cancellationToken);
}

public interface IRecording : IAsyncDisposable
{
    /// <summary>When the first frame was taken, on the shared clock (Stopwatch ticks).</summary>
    long StartedAt { get; }

    /// <summary>Stops, finishes the file, and says what was recorded.</summary>
    Task<RecordingResult> StopAsync(CancellationToken cancellationToken);
}

public abstract record RecordingTarget
{
    public sealed record Window(nint Handle) : RecordingTarget;              // Windows: MAUI, WPF, WinForms, Stride
    public sealed record Monitor(nint Handle, Rectangle? Crop) : RecordingTarget;
    public sealed record BrowserPage(object Page) : RecordingTarget;        // a Playwright IPage, kept opaque
    public sealed record AndroidDevice(string Serial) : RecordingTarget;
    public sealed record IosSimulator(string Udid) : RecordingTarget;
}

public sealed record RecordingOptions(string OutputPath, int FramesPerSecond = 30, bool Cursor = false);

public sealed record RecordingResult(string Path, TimeSpan Duration, Size Size, long StartedAt, IReadOnlyList<string> Warnings);
```

- **One clock.** `StartedAt` and the step timeline use the same `Stopwatch` ticks, so a step's time
  in `timeline.json` is its time in the video. Every recorder converts its own frame times
  (`SystemRelativeTime`, a device's clock) to it.
- **Where the target comes from.** Each platform's test context answers "what am I showing":
  `IRecordable.RecordingTarget` on `IMauiTestContext` (the window handle on Windows, the device
  serial on Android), `IHtmlTestContext` (the page), and the WPF, WinForms and Stride contexts
  (the window). The runner asks the context, then the registry for a recorder that `CanRecord` it.
- **No platform types in `Brinell.Core`** (AGENTS.md): the targets are handles, strings and an
  opaque page. So `RecordingTarget` and `IRecordable` live in `Brinell.Core.Recording`, where every
  context can implement it; the recorders live in `Brinell.Recording` and the platform packages.
- **Discovery.** A platform package declares its recorder with `[assembly: ScreenRecorder(typeof(...))]`;
  `ScreenRecorders.Discover()` loads the known packages from the output folder, so a runner that
  cannot reference a Windows-only package still finds its recorder.

### The recorders

| Recorder | Target | How | Limits |
| --- | --- | --- | --- |
| `WindowsCaptureRecorder` (`Brinell.Recording.Windows`) | `Window`, `Monitor` | Windows.Graphics.Capture, section 1 | Windows 10 1903+; no frames while minimized |
| `PlaywrightRecorder` (`Brinell.Html.Playwright`) | `BrowserPage` | Chromium: CDP `Page.startScreencast` (frames on demand, can start mid-run); others: the context's `recordVideo` (from the context's start, trimmed to `StartedAt`) | a headed browser under Presenter is a `Window` too: the window recorder is the fallback |
| `AndroidRecorder` (`Brinell.Maui.Appium`) | `AndroidDevice` | `adb shell screenrecord`, restarted every 3 minutes (its limit) and joined; or scrcpy's `--record` when present | device clock: aligned by the first frame's time as adb reports it |
| `IosSimulatorRecorder` | `IosSimulator` | `xcrun simctl io <udid> recordVideo`; stopped with SIGINT | macOS only; a real device is out of scope |
| `FfmpegRegionRecorder` | `Monitor` | ffmpeg `gdigrab` (any ffmpeg build; `ddagrab` needs ffmpeg 6 and an output index, not an area) | the last resort: records whatever covers the area |

### What the narrator does with it

`BRINELL_RECORD=1` → before the first step, the runner resolves the target and starts the
recording; each step writes its times on the shared clock; after the last step it stops and hands
`RecordingResult` and `timeline.json` to the composer (plan step 1). A target that no recorder can
record is a warning in the report, not a failed run.

## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | **Done.** `Brinell.Recording`: the API, the clock, the registry; `FfmpegRegionRecorder` as the first recorder. | A unit test records a region for two seconds and reads the MP4's duration (runs where ffmpeg is installed, skipped elsewhere). |
| 2 | **Done.** `WindowsCaptureRecorder` over an ffmpeg pipe; `FrameVideo` turns frames-on-change into a steady frame rate. | A window behind an always-on-top window records its own pixels (`WindowsCaptureRecorderTests`); the MP4 test runs where ffmpeg is installed. |
| 3 | **Done** for MAUI (Windows), WPF, WinForms, Html and Blazor. Stride's context talks to the game over a channel and does not know its window yet. | Each context names its target. |
| 4 | `PlaywrightRecorder` (screencast). | A Blazor scenario records headless. |
| 5 | `AndroidRecorder`. | A TOD scenario on the emulator records past 3 minutes. |
| 6 | Media Foundation encoder for the window recorder, if the pipe costs too much. | 30 fps at 1920x1080 without dropped frames. |
