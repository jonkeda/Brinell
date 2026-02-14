# Non-Functional Requirements

**Version:** 3.1 | **Status:** Active

## NFR-PERF: Performance

| Metric | Target |
|--------|--------|
| Control action (click, enter, toggle) | < 1 second |
| Element finding | < 100ms (cached), < 500ms (first) |
| Polling interval | 100-250ms |
| Page load detection | < 5 seconds |

- No busy-waiting — use polling with backoff
- Element caching where safe (invalidate on navigation)

## NFR-REL: Reliability

- Deterministic test results — same input = same outcome
- Driver crash recovery: detect and report, don't hang
- Scroll-to-element before interaction (off-screen elements)
- Retry at driver level only, not at test level

### Timeout Defaults

| Scope | Default |
|-------|---------|
| Test timeout | 120 seconds |
| Setup timeout | 60 seconds |
| Teardown timeout | 30 seconds |
| Element wait | 10 seconds |
| Page load | 30 seconds |

## NFR-MAINT: Maintainability

- Separation of concerns: interfaces in Core, implementations in platform projects
- One control class per file
- XML documentation on public APIs
- Consistent naming: `I{Capability}ControlObject<TScope>` for interfaces

## NFR-USE: Usability

- Intuitive API — discoverable via IntelliSense
- Actionable error messages with locator details and timeout values
- Fluent API for chaining assertions
- Minimal boilerplate in test code

## NFR-COMPAT: Compatibility

| Dependency | Minimum Version |
|------------|----------------|
| .NET | 8.0 |
| Appium .NET Client | 5.x |
| FlaUI | 4.x |
| Playwright | 1.40+ |
| xUnit | 2.6+ |

## NFR-SEC: Security

- No hardcoded credentials in source or config
- No secrets in log output
- Credential injection via environment variables only

## NFR-EXT: Extensibility

- Plugin model for custom control types
- Driver abstraction for new automation backends
- Platform-specific packages without Core changes
