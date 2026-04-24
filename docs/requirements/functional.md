# Functional Requirements

**Version:** 3.1 | **Status:** Active

## FR-001: Multi-Platform Support

- Support MAUI (.NET MAUI), Blazor (Playwright), WPF (FlaUI), WinForms (FlaUI), Stride (custom)
- Each platform has own project: `Brinell.{Platform}`
- Core interfaces shared via `Brinell.Core`
- Platform-specific drivers isolated in separate packages (e.g., `Brinell.Maui.Appium`)

## FR-002: Control Object Pattern

- Unified interface hierarchy rooted at `IControlObject<TScope>`
- Capability interfaces: `IClickableControlObject`, `ITextControlObject`, `IToggleControlObject`, etc.
- Container scoping: `IContainerControl<TElement>` for scoped element search within containers
- Controls expose properties via interfaces, not concrete classes
- Platform controls inherit from platform-specific base classes

### FR-002.5: Unified Interface Hierarchy

| Category | Interfaces |
|----------|-----------|
| Foundation | `IControlObject<TScope>`, `IClickableControlObject<TScope>`, `IFocusableControlObject<TScope>` |
| Text | `ITextControlObject<TScope>`, `IEditableTextControlObject<TScope>` |
| Selection | `ISelectorControlObject<TScope>`, `IToggleControlObject<TScope>`, `ITabControlObject<TScope>`, `IExpandableControlObject<TScope>` |
| Range | `IRangeControlObject<TScope>`, `IProgressControlObject<TScope>` |
| DateTime | `IDateControlObject<TScope>`, `ITimeControlObject<TScope>` |
| Scrolling | `IScrollableControlObject<TScope>`, `ISwipeableControlObject<TScope>`, `IRefreshableControlObject<TScope>` |

### FR-002.6: Container Scoping

- Controls scoped within containers search only within container bounds
- Containers own a root element; child controls search from that root
- Support nested containers, list containers, and indexed access
- `MauiListControl<TScope, TItem>` pattern for typed collection items

## FR-003: Page Object Pattern

- `IPageObject` represents a page/screen/view
- Page lifecycle: `IsLoaded()` → `WaitLoaded()` → `AssertLoaded()`
- Controls declared as properties on page objects
- Pages provide screenshot capability

## FR-004: State Verification

### 4-Tier Method Pattern (Is/Wait/Assert)

| Tier | Method | Returns | On Failure |
|------|--------|---------|------------|
| Query | `Is*()` | `bool?` (null = not found) | Never throws |
| Wait | `Wait*()` | `bool` | Returns false on timeout |
| Assert | `Assert*()` | `TScope` (fluent) | Throws `AssertionException` |

- `Assert*` calls the corresponding `Wait*` internally
- All `Wait*` methods accept optional `timeoutMs` parameter
- Null expected values = skip (no-op, return immediately)

## FR-005: Waiting & Synchronization

- Synchronous model — no async/await in test code (MAUI)
- Polling-based waits with configurable intervals (100-250ms)
- Default timeouts configurable via `TimeoutSettings`
- Never use `Thread.Sleep` — always wait for a condition

## FR-006: Logging

- Structured CSV logging per test
- Log levels: Trace, Debug, Info, Warning, Error
- Screenshot on test failure (configurable)
- Log file per test run with isolation

## FR-007: Locator Strategy

- `Locator` value object with `LocatorStrategy` enum (14 strategies)
- `Locator.ByAutomationId()`, `Locator.ByXPath()`, `Locator.ByCss()`, etc.
- Platform-specific strategy mapping (AutomationId → AccessibilityId on mobile)
- Implicit conversion from `string` for backward compatibility

## FR-008: Exception Hierarchy

| Exception | When |
|-----------|------|
| `BrinellException` | Base for all framework exceptions |
| `ElementNotFoundException` | Element not found within timeout |
| `WaitTimeoutException` | Wait condition not met within timeout |
| `AssertionException` | Assertion failed (has Expected/Actual) |
| `LocatorNotSupportedException` | Strategy not supported by current driver |
| `PageLoadException` | Page failed to load |

## FR-009: Extensibility

- Third-party control packages (e.g., `Brinell.Maui.CommunityToolkit`)
- Custom control objects via interface implementation
- Driver abstraction allows swapping automation backends

## FR-010: Test Isolation

- Each test gets fresh context
- No shared mutable state between tests
- Parallel test execution support (with driver isolation)

## FR-011: Constraints

- **No FluentAssertions** — use xUnit `Assert` only
- .NET 8+ required
- Dependencies licensed under MIT/Apache-2.0/BSD only
