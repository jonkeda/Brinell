# Concerns and Technical Debt

**Analysis Date:** 2026-03-02

## Critical Issues

### Vulnerability: WireMock.Net transitive dependency

- **Package**: `System.Linq.Dynamic.Core` (transitive via `WireMock.Net` 1.6.10)
- **Severity**: Medium — dynamic LINQ expression evaluation can execute arbitrary code if input is unsanitized
- **Status**: Suppressed with `<NoWarn>$(NoWarn);NU1903</NoWarn>` in `Brinell.Mocking.csproj`
- **Impact**: Low if `Brinell.Mocking` is used only in test environments (intended use). High if deployed to production infrastructure with untrusted input.
- **Mitigation**: Review WireMock.Net release notes for a patched version; upgrade when available.

---

## Architecture Deviations

### MobileBy.AccessibilityId implemented as XPath

- **Location**: `srcnew/Brinell.Maui.Appium/` — `MauiLocatorConverter.cs` or similar
- **Deviation**: `LocatorStrategy.AutomationId` is resolved to an XPath expression (`//*[@content-desc='' or @accessibility-id='']`) instead of Appium-native `MobileBy.AccessibilityId()`
- **Documented in**: `srcnew/explanation/DEVIATIONS-Phase1.md`
- **Risk**: XPath locators are slower due to full-tree traversal; platform-specific quirks (e.g., Android content-desc vs iOS accessibility-id) may cause cross-platform failures
- **Resolution**: Replace XPath with `MobileBy.AccessibilityId()` for the automationId strategy; XPath can remain as a fallback for complex queries

### Covariance workaround (interface return types)

- **Deviation**: Some fluent interfaces return `IControlObject` instead of the more specific `TScope` in places where C# covariance constraints prevent typed return
- **Impact**: Occasional cast required in test code
- **Resolution**: Tracked as planned improvement — no immediate action required

---

## Incomplete Implementations

### Brinell.Mocking — stub only

- **Files**: `srcnew/Brinell.Mocking/MockApiServer.cs`, `srcnew/Brinell.Mocking/ApiStubBuilder.cs`
- **Status**: Contain only placeholder comments (`// Placeholder for MockApiServer`)
- **Impact**: Any test that imports `Brinell.Mocking` for API stubbing will compile but fail at runtime
- **Resolution**: Implement WireMock.Net wrapper before `Brinell.Mocking` is used in any test

---

## Technical Debt

### Unused package declarations

In `Directory.Packages.props` (root), the following packages are declared but not referenced by any `srcnew/` project:

- `Microsoft.EntityFrameworkCore` 10.0.0 + `Microsoft.EntityFrameworkCore.Sqlite`
- `Serilog` (no version visible — likely a leftover entry)

These entries do not cause build failures but bloat the version management file and may confuse future maintainers. Remove or document intent.

### Legacy src/ and tests/ artifact directories

- `src/` and `tests/` directories exist on the local filesystem but contain only `bin/` and `obj/` build artifact subdirectories — no source files
- Git ignores them (only `bin/`/`obj/` contents remain, which `.gitignore` excludes)
- These are invisible to git and harmless but waste disk space
- **Cleanup**: `Remove-Item -Recurse -Force src, tests` from workspace root is safe

### MAUI tab navigation flakiness

- **Location**: `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` — `NavigateToContainerDemo()`
- **Symptom**: WinUI tab clicks can be flaky on Windows MAUI — retry logic with explicit comment
- **Impact**: Occasional test flakiness in container demo tests on Windows
- **Resolution**: Investigate WinUI tab click timing; apply `WaitVisible` after tab click with appropriate timeout instead of arbitrary retry

### Appium Android activity detection

- **Location**: `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` — capability building
- **Issue**: `appPackage`/`appActivity` are intentionally not specified (let Appium extract from APK). Some Appium versions or custom launchers fail to detect the activity automatically.
- **Resolution**: Document this assumption explicitly in fixture; add fallback `appActivity` env var if detection fails in CI

---

## Known Fragile Areas

| Area | Risk | Notes |
|------|------|-------|
| `MauiLocatorConverter` XPath | Medium | Slower lookups; platform differences in attribute names |
| `Brinell.Mocking` | High | Stub only — will fail at runtime if used |
| Tab navigation (WinUI) | Low-Medium | Known flakiness acknowledged in code comment |
| Android activity auto-detect | Low | May break with certain Appium/device configuration |
| `System.Linq.Dynamic.Core` | Medium | Vulnerability suppressed; awaiting fix in WireMock.Net |

---

## Resolved Issues (for reference)

The following issues were identified in `review.md` and confirmed fixed in the current codebase:

- **Editor used `new` keyword instead of `override`** → Fixed (`protected override` in Editor)
- **Empty catch blocks** in DatePicker, TimePicker, ContainerBase → Replaced with narrowed `catch (WebDriverException)` or `catch (ElementNotFoundException)`
- **Poll method bare catch** swallowing final-check exceptions → Fixed (bare catch removed)
- **ContainerBase missing factory methods** → Fixed (all control type factories added)
- **CollectionView/CarouselView not scrollable** → Fixed (now extend `ScrollableControlBase`)
- **Grid not usable as container scope** → Fixed (`Grid<TParent,TSelf>` double-generic variant added)
- **Menu/Toolbar item scope wrong** → Fixed (`RunWithElement` scoped search)

---

*Concerns analysis: 2026-03-02*
