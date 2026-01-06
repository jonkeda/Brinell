# functional ScreenshotEvidence
- **id**: FR-502
- **title**: Screenshot and Evidence Collection
- **priority**: high
- **status**: draft
- **category**: Logging and Evidence

The framework must provide screenshot and evidence collection capabilities for debugging and test reporting.

## capabilities

### AutomaticScreenshots
- **id**: FR-502.1
- **title**: Automatic screenshot on failure

The framework must capture screenshots automatically on:
- Assertion failure
- Unhandled exception
- Test timeout
- Element not found (configurable)

Automatic capture enabled by default, configurable.

### ManualScreenshots
- **id**: FR-502.2
- **title**: Manual screenshot API

Tests must be able to capture screenshots manually:
```
// Pseudocode
context.CaptureScreenshot("before_submit")
control.CaptureScreenshot("highlighted_element")
page.CaptureScreenshot("full_form")
```

Manual capture for documentation or debugging.

### ScreenshotNaming
- **id**: FR-502.3
- **title**: Screenshot naming convention

Screenshots must have meaningful names:
- Pattern: `{TestClass}_{TestMethod}_{Timestamp}_{Description}.{ext}`
- Description from capture call or event type
- Timestamp ensures uniqueness
- Extension based on format

Examples:
```
LoginTests_ValidLogin_20260106_143022_before_submit.png
LoginTests_ValidLogin_20260106_143025_assertion_failed.png
```

### ScreenshotStorage
- **id**: FR-502.4
- **title**: Screenshot storage configuration

Configurable storage settings:

| Setting | Description |
|---------|-------------|
| OutputDirectory | Directory for screenshots |
| Format | Image format (PNG, JPEG) |
| Quality | JPEG quality (if applicable) |
| RetentionDays | Days to keep old screenshots |
| MaxCount | Maximum screenshots per test |

### ElementScreenshots
- **id**: FR-502.5
- **title**: Element-specific screenshots

Controls must support element screenshots:
- Capture only the element region
- Crop to element bounds
- Include small margin around element
- Fall back to full page if crop fails

### FullPageScreenshots
- **id**: FR-502.6
- **title**: Full page vs viewport screenshots

Web platform must support both:

| Type | Description |
|------|-------------|
| Viewport | Current visible area only |
| FullPage | Entire page including scrolled content |

Configuration or per-capture parameter.

### VideoRecording
- **id**: FR-502.7
- **title**: Video recording support (optional)

Where platform supports:
- Record test execution as video
- Start/stop recording API
- Automatic recording on failure
- File naming similar to screenshots

Supported on: Web (Playwright), Mobile (Appium)

### EvidenceAttachment
- **id**: FR-502.8
- **title**: Evidence attachment to test results

Evidence must be attachable to test results:
- Screenshot paths returned from capture
- Integration with test result formats (TRX, JUnit XML)
- Linkable from test reports
- Queryable list of captures per test

---

## relationships

- Triggered by [FR-302 Assertions](120_302_Assertions.spx.md) failures
- Triggered by [FR-600 Exceptions](120_600_ExceptionStrategy.spx.md)
- Configuration via [FR-401 Configuration](120_401_Configuration.spx.md)
- Lifecycle managed by [FR-400 Test Context](120_400_TestContext.spx.md)

---

## constraints

- Screenshot capture must not throw exceptions (log and continue)
- Screenshots must be saved even if test crashes immediately after
- Large screenshots must not cause memory issues
- Screenshot capture must not significantly slow tests
