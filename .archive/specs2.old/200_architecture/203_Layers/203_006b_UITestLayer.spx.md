# 203.006b UI Test Layer

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

The UI Test layer contains **end-to-end tests** that verify real applications using the Brinell framework. This is where test writers create Page Objects and test scenarios.

## Purpose

- Test real applications end-to-end
- Demonstrate framework usage patterns
- Validate complete workflows
- Serve as example implementations

## Packages

```
Brinell.Samples.Maui.UITests/
├── Pages/            # Page Object implementations
├── Tests/            # Test classes organized by feature
├── Fixtures/         # Test fixtures and setup
└── Utilities/        # Test-specific helpers

Brinell.Samples.Blazor.UITests/
├── Pages/            # Page Object implementations
├── Tests/            # Test classes organized by feature
├── Fixtures/         # Test fixtures and setup
└── Utilities/        # Test-specific helpers
```

## Dependencies

- Brinell platform package (Brinell.Maui or Brinell.Blazor)
- Test framework (xUnit, NUnit, or MSTest)
- Sample application to test

## Dependents

- None (top of dependency chain)

## Design Rules

1. Uses Page Object pattern for all UI interaction
2. Tests are organized by feature or user flow
3. No direct automation API usage (only Brinell interfaces)
4. Tests should be readable as documentation
5. Follow test naming conventions

## Test Organization

```
Tests/
├── LoginTests.cs           # Login feature tests
├── NavigationTests.cs      # Navigation and routing tests
├── FormTests.cs            # Form input and validation tests
├── DataDisplayTests.cs     # Data grid and list tests
└── SettingsTests.cs        # Settings and preferences tests
```

## Validation

- [ ] All UI interaction through Page Objects
- [ ] No direct Appium/Selenium API calls
- [ ] Tests are independent (no ordering)
- [ ] Clear assertion messages

---

## Related Documents

- [Integration Test Layer](203_005b_IntegrationTestLayer.spx.md)
- [Platform Layer](203_002b_PlatformLayer.spx.md)
- [Test Writing Guide](../../../docs/15-test-writing-guide.md)
