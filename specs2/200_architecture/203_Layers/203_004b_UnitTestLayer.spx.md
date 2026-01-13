# 203.004b Unit Test Layer

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

The Unit Test layer contains tests that verify framework components in **isolation** using mocks and stubs. No actual UI automation occurs at this layer.

## Purpose

- Test framework logic without external dependencies
- Verify interface contracts and base class behavior
- Fast feedback during development
- High code coverage of framework internals

## Packages

```
Brinell.Core.Tests/
├── Interfaces/       # Interface contract tests
├── Exceptions/       # Exception behavior tests
├── Configuration/    # Configuration validation tests
├── Logging/          # Logger implementation tests
├── Timeout/          # Timeout and wait logic tests
├── Retry/            # Retry policy tests
└── Assertions/       # Assertion helper tests

Brinell.Maui.Tests/
├── Controls/         # Control implementation tests (mocked context)
├── Base/             # Base class behavior tests
└── Context/          # TestContext tests (mocked driver)

Brinell.Blazor.Tests/
├── Controls/         # Control implementation tests (mocked context)
├── Base/             # Base class behavior tests
└── Context/          # TestContext tests (mocked driver)
```

## Dependencies

- Framework package being tested
- Test framework (xUnit, NUnit, or MSTest)
- Mocking library (Moq, NSubstitute)

## Dependents

- CI/CD pipelines
- Pull request checks

## Design Rules

1. No real drivers or UI automation
2. All external dependencies are mocked
3. Tests are fast (< 1 second each)
4. Each test is independent
5. Naming: `[Method]_[Scenario]_[Expected]`

## Validation

- [ ] No automation library usage
- [ ] All tests run without external services
- [ ] Tests complete in under 1 minute total
- [ ] No test interdependencies

---

## Related Documents

- [Core Layer](203_001b_CoreLayer.spx.md)
- [Platform Layer](203_002b_PlatformLayer.spx.md)
- [Integration Test Layer](203_005b_IntegrationTestLayer.spx.md)
