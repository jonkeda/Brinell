# Testing & Mockability Guide

**Source of truth:** `testsnew/`

## Mockability Strategy

### MAUI (Appium/FlaUI)

Appium's `AppiumDriver` has non-virtual members — cannot mock directly. Solution:

- `IMauiDriver` / `IMauiElement` interfaces wrap driver/element
- Unit tests mock `IMauiDriver` and `IMauiElement`
- `MauiDriverFactory` allows injecting mock drivers

### Blazor (Playwright)

Playwright's `IPage`/`ILocator` are interfaces — mock directly with Moq/NSubstitute.

## Test Organization

```
testsnew/
├── Brinell.Core.Tests/           # Core interface/locator/exception tests
├── Brinell.Maui.Tests/           # MAUI control unit tests (mocked driver)
├── Brinell.Maui.UITests/         # MAUI integration tests (real Appium/FlaUI)
├── Brinell.Blazor.Tests/         # Blazor unit tests
├── Brinell.Blazor.UITests/       # Blazor integration tests
└── ...per platform
```

## Coverage Targets

| Layer | Line | Branch | Method |
|-------|------|--------|--------|
| Core | 90% | 85% | 95% |
| Platform | 85% | 80% | 90% |

## Test Patterns

- Unit tests: mock driver, verify method calls and return values
- UI tests: real automation driver against sample app
- No `Thread.Sleep` — use framework `Wait*`/`Assert*` methods
- No FluentAssertions — use xUnit `Assert` only
- Controls as page object properties, initialized in constructor
