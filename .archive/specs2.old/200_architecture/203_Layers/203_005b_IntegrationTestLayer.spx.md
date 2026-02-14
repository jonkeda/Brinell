# 203.005b Integration Test Layer

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

The Integration Test layer contains tests that verify framework components work correctly with **real automation drivers** but may use simplified test applications or harnesses.

## Purpose

- Verify driver integration works correctly
- Test element finding and waiting behavior
- Validate exception translation
- Confirm lifecycle management

## Packages

```
Brinell.Maui.IntegrationTests/
├── Context/          # Real Appium driver tests
├── ElementFinding/   # Locator strategy tests
├── Waiting/          # Wait behavior with real elements
└── Lifecycle/        # Session management tests

Brinell.Blazor.IntegrationTests/
├── Context/          # Real Selenium/Playwright driver tests
├── ElementFinding/   # Locator strategy tests
├── Waiting/          # Wait behavior with real elements
└── Lifecycle/        # Session management tests
```

## Dependencies

- Framework package being tested
- Test framework (xUnit, NUnit, or MSTest)
- Real automation SDK
- Test harness application (simple test app)

## Dependents

- CI/CD pipelines (may require special runners)
- Release validation

## Design Rules

1. Uses real drivers (Appium, Selenium, Playwright)
2. May use simplified test applications
3. Tests can be slower (seconds per test)
4. Tests may require specific environment setup
5. Document environment requirements

## Validation

- [ ] Driver setup documented
- [ ] Environment requirements listed
- [ ] Tests can run in CI with proper setup
- [ ] Failures have clear error messages

---

## Related Documents

- [Unit Test Layer](203_004b_UnitTestLayer.spx.md)
- [UI Test Layer](203_006b_UITestLayer.spx.md)
- [Technology Layer](203_003b_TechnologyLayer.spx.md)
