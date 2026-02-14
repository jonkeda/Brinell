# Context & Infrastructure Classes

**Source of truth:** `srcnew/Brinell.Core/`, `srcnew/Brinell.Maui/Context/`

## TestContext

`ITestContext<TElement>` provides test execution context:
- Element finding (via `IElementScope<TElement>`)
- Navigation (`NavigateTo`, `NavigateBack`, `Refresh`)
- Screenshots (`TakeScreenshot`, `SaveScreenshot`)
- State reset (`ResetAppState`)
- Timeout configuration via `TimeoutSettings`
- Logging via `ITestLogger`

MAUI implementation: `MauiTestContext<IMauiElement>` wrapping `IMauiDriver`.

## TimeoutSettings

| Setting | Default | Purpose |
|---------|---------|---------|
| `DefaultTimeoutMs` | 10000 | Element wait timeout |
| `PageLoadTimeoutMs` | 30000 | Page load timeout |
| `PollingIntervalMs` | 200 | Polling frequency |

## ScreenshotService

`IScreenshotService` captures screenshots:
- On-demand via `Capture()`
- On test failure via `CaptureOnFailure()`
- Configurable output directory and naming
