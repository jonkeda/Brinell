# 203.003b Technology Layer

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

The Technology layer defines how external automation technologies (Appium, Selenium, Playwright) are integrated into the Brinell framework. This layer sits at the **boundary** between Brinell and the underlying automation drivers.

## Purpose

- Provide driver abstraction over different automation libraries
- Isolate technology changes from test code
- Manage configuration and connection lifecycle
- Translate automation exceptions to Brinell exceptions

## Supported Technologies

| Technology | Use Case | Package |
|------------|----------|---------|
| Appium | MAUI (Android, iOS, Windows, Mac) | Appium.WebDriver |
| Appium | WPF (WinAppDriver) | Appium.WebDriver |
| Selenium | Blazor (all browsers) | Selenium.WebDriver |
| Playwright | Blazor (alternative) | Microsoft.Playwright |

## Locator Strategies

| Technology | Default Strategy | Default Attribute |
|------------|------------------|-------------------|
| Appium (MAUI) | AutomationId | AutomationId |
| Appium (WPF) | AutomationId | AutomationProperties.AutomationId |
| Selenium | TestId | data-testid |
| Playwright | TestId | data-testid |

## Lifecycle Modes

### Session-Per-Run (Default)

Driver and application stay open for entire test run. Faster, shared state.

### Session-Per-Test

New driver and application for each test. Isolated state, slower.

## Design Rules

1. Automation library types are never in public API
2. TestContext manages driver lifecycle
3. Controls use Locator abstraction (not hardcoded strings)
4. Exceptions are translated to Brinell types
5. Configuration is externalized

## Validation

- [ ] Automation types not exposed publicly
- [ ] TestContext handles lifecycle
- [ ] Exceptions properly translated
- [ ] Lifecycle mode is configurable

---

## Related Documents

- [Platform Layer](203_002b_PlatformLayer.spx.md)
- [220 External Systems](../220_External/220_INDEX.md)
