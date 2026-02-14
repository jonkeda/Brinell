# Review: srcnew Implementation vs .specs Specifications

**Date:** February 14, 2026  
**Scope:** All 12 projects in `srcnew/` reviewed against all documents in `.specs/`  
**Verdict:** Implementation is **substantially conformant** with significant progress, but has critical anti-pattern violations and several gaps requiring attention.

---

## Executive Summary

| Area | PASS | DEVIATION | MISSING | EXTRA | CONCERN |
|------|------|-----------|---------|-------|---------|
| Core Interfaces (001-INTERFACES) | 134 | 1 | 1 | 5 | — |
| Architecture (ARCH-001/002/003) | 20 | 5 | 4 | — | 5 |
| MAUI Controls (classes/) | 42 | 9 | 4 | 12 | — |
| **Total** | **196** | **15** | **9** | **17** | **5** |

**Overall conformance rate:** ~89% PASS (196/224 checkpoints)

---

## 1. Core Interfaces Conformance

**Source:** `.specs/controls/001-INTERFACES.md`  
**Implementation:** `srcnew/Brinell.Core/Interfaces/`

### Verdict: Excellent (134/140 = 96%)

All 16 control interfaces match the spec exactly in method signatures, return types, nullable skip patterns, and inheritance hierarchy:

- **IControlObject\<TScope\>** — 14/14 methods PASS
- **IClickableControlObject\<TScope\>** — 8/8 methods PASS
- **IFocusableControlObject\<TScope\>** — 5/5 methods PASS
- **ITextControlObject\<TScope\>** — 6/6 methods PASS
- **IEditableTextControlObject\<TScope\>** — 10/10 methods PASS
- **ISelectorControlObject\<TScope\>** — 13/13 methods PASS
- **IToggleControlObject\<TScope\>** — 7/7 methods PASS
- **ITabControlObject\<TScope\>** — 4/4 members PASS
- **IExpandableControlObject\<TScope\>** — 6/6 methods PASS
- **IRangeControlObject\<TScope\>** — 9/9 methods PASS
- **IProgressControlObject\<TScope\>** — 6/6 methods PASS
- **IDateControlObject\<TScope\>** — 4/4 methods PASS
- **ITimeControlObject\<TScope\>** — 4/4 methods PASS
- **IScrollableControlObject\<TScope\>** — 10/10 methods PASS
- **ISwipeableControlObject\<TScope\>** — 5/5 methods PASS
- **IRefreshableControlObject\<TScope\>** — 4/4 methods PASS

Infrastructure interfaces (IElement, IDriver, IPageObject, ITestContext, IContainerControl, IElementScope, IRangePatternElement) all PASS.

### Deviations

| # | Item | Detail |
|---|------|--------|
| DEV-1 | `IElement<TSelf>.SendKeys()` | Adds `TextInputMethod method` parameter not in spec. Spec shows bare `SendKeys(string)`, impl is `SendKeys(string, TextInputMethod)` |

### Missing

| # | Item | Detail |
|---|------|--------|
| MIS-1 | Implicit `string → Locator` conversion | LOCATOR.md spec states implicit string→Locator conversion (treated as AutomationId). Not implemented. Requires `public static implicit operator Locator(string) => ByAutomationId(value)` |

### Extras (not in spec, but useful)

| # | Item | Location |
|---|------|----------|
| 1 | `IScreenshotService` interface | Brinell.Core/Interfaces/ |
| 2 | `IDiagnosticDriver` interface | Brinell.Core/Interfaces/ |
| 3 | `IElement<TSelf>.GetAttribute(string)` method | IElement.cs |
| 4 | `IRangePatternElement.SupportsRangeValue` property | IRangePatternElement.cs |

### Locator & Exception Conformance

- **LocatorStrategy enum:** 14/14 values match exactly
- **Locator class:** All factory methods and composition methods match
- **Exception hierarchy:** All 6 exceptions match in class hierarchy and properties

---

## 2. Architecture Conformance

**Source:** `.specs/architecture/ARCH-001-core-architecture.md`, `ARCH-002-decisions.md`, `ARCH-003-project-structure.md`  
**Implementation:** `srcnew/` (build infrastructure and project structure)

### Verdict: Good with concerns (20/29 = 69%)

### PASS (20 items)

| # | Item | Detail |
|---|------|--------|
| 1 | TFM multi-targeting | `net8.0;net9.0;net10.0` matches spec |
| 2 | C# language version | `latest` (C# 13) |
| 3 | Nullable reference types | Enabled |
| 4 | Implicit usings | Enabled |
| 5 | Treat warnings as errors | Enabled |
| 6 | Versioning | `0.1.0` pre-release matches |
| 7 | Central Package Management | Enabled |
| 8 | Package versions | xunit 2.9.3, FlaUI 5.0.0, Appium 8.0.1, Playwright 1.50.0, Stride 4.3.0.2507 — all match |
| 9 | Platform isolation | No cross-references between Maui↔Blazor↔Wpf↔WinForms↔Stride |
| 10 | Project naming | All use `Brinell.` prefix per ARCH-003 |
| 11 | All 12 projects present | Core, Maui, Maui.Appium, Maui.FlaUI, Maui.CommunityToolkit, Blazor, Html, Wpf, WinForms, Stride, Automation, Mocking |
| 12 | Core folder structure | Has Abstractions/, Configuration/, Exceptions/, Interfaces/, Locators/, Logging/, Services/, Utilities/ |
| 13 | Maui folder structure | Has Context/, Controls/, Interfaces/, Pages/, Testing/ |
| 14 | Separate Technology Adapters (AD-004) | Maui.Appium and Maui.FlaUI are separate projects |
| 15 | MauiDriverFactory reflection loading | Dynamic assembly loading avoids hard dependencies |
| 16 | WaitHelper polling pattern | Stopwatch + condition + polling — no WebDriverWait dependency |
| 17 | No FluentAssertions (AD-009) | Banned via MSBuild target in testsnew/Directory.Build.props |
| 18 | Test project structure | All expected test projects in testsnew/ |
| 19 | Wpf/WinForms Windows-only TFMs | Override to `net*-windows` |
| 20 | SourceLink configured | Deterministic CI builds |

### Deviations

| # | Item | Spec Says | Actual |
|---|------|-----------|--------|
| DEV-1 | **Core references xUnit** | Core has zero platform/library references | Brinell.Core.csproj includes `xunit.extensibility.core` for `ScreenshotTestAttribute` |
| DEV-2 | **Maui references Appium directly** | Platform layer = Core + platform SDK only; automation libs in Technology layer | Brinell.Maui.csproj has `Appium.WebDriver` for `StaleElementReferenceException` catch |
| DEV-3 | **`BRINELL_DRIVER` env var not implemented** | ARCH-001: `BRINELL_DRIVER=Appium\|FlaUI` for swappable drivers | Uses `APPIUM_PLATFORM` instead; auto-selected by platform (Windows→FlaUI, Android→Appium) |
| DEV-4 | **Missing `Wrappers/` folder, extra `Enums/`** | ARCH-003 specifies `Wrappers/` in Maui | `Wrappers/` absent; `Enums/` exists but unspecified |
| DEV-5 | **Brinell.Automation has no Core reference** | Technology adapter should depend on Core | Only references Stride.Engine/UI; also targets only `net10.0` instead of multi-target |

### Missing Dependencies

| # | Package | Spec Version | Status |
|---|---------|-------------|--------|
| 1 | AutoFixture | 4.18.1 | Not in Directory.Packages.props |
| 2 | Bogus | 35.5.1 | Not in Directory.Packages.props |
| 3 | Serilog | 4.1.0 | Not in Directory.Packages.props |
| 4 | Microsoft.EntityFrameworkCore | 10.0.0 | Not in Directory.Packages.props |

---

## 3. MAUI Controls Conformance

**Source:** `.specs/controls/classes/` (14 documents)  
**Implementation:** `srcnew/Brinell.Maui/Controls/`

### Verdict: Good with critical deviations (42/67 = 63%)

### Foundation Layer

| Component | Status | Notes |
|-----------|--------|-------|
| `MauiObjectBase` | PASS | Poll/Context engine present |
| `MauiControlBase<TScope>` | PASS | Full Is/Wait/Assert triad implemented |
| `MauiPageObjectBase<TSelf>` | DEVIATION | Spec names it `MauiPageBase<TElement>`, impl uses CRTP pattern `MauiPageObjectBase<TSelf>` — functionally equivalent |
| All 9 intermediate bases | PASS | Clickable, Toggle, Range, Selector, Scrollable, Expandable, Focusable, Swipeable, Refreshable |
| `Run`/`RunWithElement` location | DEVIATION | Spec places these in `MauiObjectBase`; impl places them in `MauiControlBase<TScope>` |

### Concrete Controls

| Control | Spec | Status | Notes |
|---------|------|--------|-------|
| MauiButtonControl | INPUT.md | PASS | Full IClickableControlObject |
| MauiImageButtonControl | INPUT.md | PASS + EXTRA | Extra: GetSource, IsPressed, GetAspect |
| MauiEntryControl | INPUT.md | DEVIATION | `Enter()` does NOT clear first (spec says it should) |
| MauiEditorControl | INPUT.md | PASS | Inherits MauiEntryControl; FlaUI clear fallback |
| MauiSearchBarControl | INPUT.md | PASS + EXTRA | Extra: Search(), SubmitSearch() |
| MauiLabelControl | DISPLAY.md | PASS | Read-only text |
| MauiImageControl | DISPLAY.md | PASS | Read-only visibility |
| MauiCheckBoxControl | TOGGLE.md | DEVIATION | Missing IClickableControlObject interface |
| MauiSwitchControl | TOGGLE.md | DEVIATION + EXTRA | Missing IClickableControlObject; Extra: IsOn/TurnOn/TurnOff aliases |
| MauiRadioButtonControl | TOGGLE.md | DEVIATION + EXTRA | Missing IClickableControlObject; Extra: Select/IsSelected aliases |
| MauiSliderControl | RANGE.md | PASS + EXTRA | Extra: SlideToPercentage, GetPercentage |
| MauiStepperControl | RANGE.md | PASS + EXTRA | Extra: IncrementBy, DecrementBy, SetToMinimum/Maximum |
| MauiProgressBarControl | RANGE.md | PASS | IProgressControlObject |
| MauiActivityIndicatorControl | RANGE.md | PASS | IProgressControlObject |
| MauiPickerControl | SELECTION.md | PASS + EXTRA | Extra: GetTitle, AssertTitle |
| MauiTabControl | SELECTION.md | PASS | ITabControlObject with XPath fallback |
| MauiFlyoutItemControl | NAVIGATION.md | PASS | Click-based navigation |
| MauiDatePickerControl | DATETIME.md | PASS | IDateControlObject |
| MauiTimePickerControl | DATETIME.md | PASS | ITimeControlObject |
| MauiScrollViewControl | CONTAINER.md | PASS | IScrollableControlObject |
| MauiSwipeViewControl | CONTAINER.md | PASS | ISwipeableControlObject |
| MauiRefreshViewControl | CONTAINER.md | PASS | IRefreshableControlObject |
| MauiExpanderControl | CONTAINER.md | PASS | IExpandableControlObject |
| MauiContainerBase | CONTAINER.md | DEVIATION | Spec: `MauiContainerBase<TScope>` / Impl: `MauiContainerBase<TParent, TSelf>` |
| MauiCollectionViewControl | COLLECTION.md | DEVIATION | Extends MauiListControl, not MauiScrollableControlBase — missing IScrollableControlObject |
| MauiListViewControl | COLLECTION.md | DEVIATION | Same as CollectionView |
| MauiListControl | COLLECTION.md | PASS | Generic typed collection with item factory |
| MauiToolbarControl | — | EXTRA | Not in spec; provides toolbar item access |
| MauiMenuControl | — | EXTRA | Not in spec; provides menu interaction |
| MauiWebViewControl | MEDIA.md | PASS | Basic existence/visibility |
| MauiMediaElementControl | MEDIA.md | PASS | Basic existence/visibility |

### Missing Controls (specified but not implemented)

| # | Control | Spec Source | Purpose |
|---|---------|------------|---------|
| 1 | `MauiSpanControl` | INPUT.md | Read-only text for Span elements |
| 2 | `MauiTabBarControl` | NAVIGATION.md | Shell TabBar navigation |
| 3 | `MauiNavigationPageControl` | NAVIGATION.md | NavigationPage back/forward |
| 4 | `MauiFrameControl` | CONTAINER.md | Frame/Border container |

---

## 4. Critical Issues

### CRITICAL-1: Thread.Sleep Usage (36 instances)

**Severity:** HIGH — Directly violates `.github/copilot-instructions.md` anti-pattern #1: *"NEVER use Thread.Sleep — not in test code, not in framework code, not anywhere."*

| File | Count | Nature |
|------|-------|--------|
| MauiSliderControl.cs | 8 | Arbitrary waits (10ms–50ms) after slider operations |
| MauiToggleControlBase.cs | 5 | Arbitrary waits (50ms–100ms) after toggle clicks |
| MauiListControl.cs | 5 | Arbitrary waits (100ms) after list operations |
| MauiControlBase.cs | 4 | Polling waits |
| WaitHelper.cs | 4 | Polling interval delay (arguable — this IS the polling mechanism) |
| FlaUIMauiElement.cs | 4 | Arbitrary waits (100ms) + gesture delays |
| FlaUIMauiDriver.cs | 2 | Arbitrary waits (100ms) |
| Others (MauiObjectBase, MauiTestContext, AppiumMauiElement, MauiScrollableControlBase) | 4 | Mixed polling + arbitrary |

**WaitHelper's 4 uses are defensible** (it IS the polling infrastructure). The remaining **32 instances are anti-pattern violations** that should use condition-based polling.

### CRITICAL-2: Empty Catch Blocks in WaitHelper

**Severity:** HIGH — Violates anti-pattern #3: *"NEVER Use Empty Catch Blocks"*

`WaitHelper.cs` has 8 empty catch blocks (`catch { }`). These hide failures during polling and make debugging impossible. Should at minimum constrain exception types or log.

### CRITICAL-3: xUnit Dependency Leak in Brinell.Core

**Severity:** MEDIUM — Brinell.Core's reference to `xunit.extensibility.core` (without `PrivateAssets="All"`) causes every consumer of the NuGet package to transitively pull in xUnit, even non-test projects. Violates the "zero dependencies" principle from ARCH-001.

### CRITICAL-4: Toggle Controls Missing IClickableControlObject

**Severity:** MEDIUM — Spec says CheckBox, Switch, and RadioButton implement both `IToggleControlObject` and `IClickableControlObject`. Implementation only has `IToggleControlObject`. Users cannot call `Click()`, `DoubleClick()`, `Hover()`, `LongPress()` on toggle controls.

### CRITICAL-5: Enter() Does Not Clear First

**Severity:** MEDIUM — Spec (INPUT.md) states: *"Enter() clears existing text first, then inputs."* Implementation's `EnterCore` only calls `element.SendKeys(text)` without clearing. Only `SetText()` clears. This behavioral difference breaks spec contract and may confuse test writers expecting `Enter()` to produce a clean input state.

### CRITICAL-6: Collection Controls Missing IScrollableControlObject

**Severity:** LOW-MEDIUM — `MauiCollectionViewControl` and `MauiListViewControl` extend `MauiListControl` instead of `MauiScrollableControlBase`. They have ScrollToTop/ScrollToEnd helpers via `MauiListControl` but don't formally implement `IScrollableControlObject`, breaking polymorphic usage.

---

## 5. Active Specs Status vs Implementation

| Spec | Target | Status |
|------|--------|--------|
| SPEC-015 (Element Lookup Optimization) | Reduce FindElement calls from 53→1-3 | ✅ Implemented in RunWithElement/PollWithElement |
| SPEC-017 (TabView Migration) | Migrate to CommunityToolkit TabView | ⏭️ Superseded by SPEC-023 (TabbedPage) |
| SPEC-017b (Container Testing) | 25 tests across 5 test suites | 🔄 Active — container pattern implemented |
| SPEC-023 (TabbedPage Automation) | XPath fallback for Windows MAUI #3996 | ✅ Complete — all navigation + container tests passing |
| SPEC-025 (MAUI Control UITests) | 33-task comprehensive test plan | 🔲 Draft — 0/33 tasks complete |
| SPEC-026 (UI Test Fixes) | 90%+ pass rate from 68% | 🔄 Active — ScrollIntoView, slider, toggle fixes |
| SPEC-029 (FlaUI Windows Fixes) | 85%+ pass rate from 65.5% | 🔄 In Progress — 15/22 tasks done |
| PLAN-android-testing | Android emulator CI setup | 🔲 Draft |
| SPEC-scrollintoview-android | Android scroll strategy | 🔄 Active analysis |

---

## 6. Platform Maturity Summary

| Platform | Project(s) | Status | Implementation Level |
|----------|-----------|--------|---------------------|
| **MAUI** | Brinell.Maui, .Appium, .FlaUI, .CommunityToolkit | **Production-ready** | ~55 .cs files, 30+ controls, 2 driver implementations |
| **Blazor** | Brinell.Blazor | **Placeholder** | Stub files only |
| **HTML** | Brinell.Html | **Placeholder** | Stub files only |
| **WPF** | Brinell.Wpf | **Placeholder** | Stub files only |
| **WinForms** | Brinell.WinForms | **Placeholder** | Stub files only |
| **Stride** | Brinell.Stride, Brinell.Automation | **Placeholder** | Stub files only |
| **Mocking** | Brinell.Mocking | **Placeholder** | Stub files only |
| **Core** | Brinell.Core | **Complete** | ~40 .cs files, all 25 interfaces, full infrastructure |

---

## 7. Documented Deviations

`srcnew/explanation/DEVIATIONS-Phase1.md` documents 8 intentional deviations:

1. Locator strategy — platform-agnostic in Core (no MobileBy)
2. IElementScope — not covariant (C# generic limits)
3. Nullable bool state — `IsVisible()`/`IsEnabled()` return `bool?`
4. Capability interfaces — standalone, not requiring IControlObject inheritance
5. WaitHelper — simple Stopwatch polling (no WebDriverWait dependency)
6. Logger — string-based methods (simplified from structured logging)
7. Platform pages — extend base with platform-specific methods
8. MobileBy — implemented as XPath covering content-desc, accessibility-id, AutomationId

These are reasonable engineering decisions. However, the deviations document does **not** cover the critical issues found in this review (Thread.Sleep, empty catches, Enter() behavior, missing interfaces).

---

## 8. Internal Spec Inconsistency

ARCH-001 Layer Rules define Platform layer dependencies as *"Core + platform SDK"*, placing automation libraries in the Technology layer. But ARCH-003 Module Boundaries explicitly allows `Brinell.Maui → Core + Appium.WebDriver`. The implementation follows ARCH-003, which means the clean four-layer separation is not fully realized. **The specs should be reconciled.**

---

## 9. Recommendations (Priority Order)

### P0 — Must Fix

1. **Replace all 32 arbitrary `Thread.Sleep` calls** with condition-based polling using `WaitHelper.WaitFor()` or equivalent
2. **Replace 8 empty catch blocks** in WaitHelper with typed exception handling or at minimum constrain to known transient exception types
3. **Add `IClickableControlObject` to toggle controls** (CheckBox, Switch, RadioButton) to match spec

### P1 — Should Fix

4. **Fix `Enter()` to clear first** or update spec to document current behavior — the contract must be consistent
5. **Fix Brinell.Core xUnit leak** — either add `PrivateAssets="All"` to the xunit reference, or move `ScreenshotTestAttribute` to a separate Brinell.Testing package
6. **Implement implicit `string → Locator` conversion** — simple one-line addition specified in LOCATOR.md
7. **Add `IScrollableControlObject` to collection controls** or update spec to reflect current design

### P2 — Should Address

8. **Implement `BRINELL_DRIVER` env var** or update ARCH-001 to document platform-based auto-selection
9. **Add missing controls:** MauiSpanControl, MauiFrameControl (MauiTabBarControl and MauiNavigationPageControl may be deferred if TabbedPage is the adopted pattern)
10. **Reconcile ARCH-001 vs ARCH-003** layer rule inconsistency regarding Appium.WebDriver in Brinell.Maui
11. **Update DEVIATIONS-Phase1.md** to document newly discovered deviations

### P3 — Nice to Have

12. **Add missing optional dependencies** (AutoFixture, Bogus, Serilog, EF Core) if planned for use
13. **Add Extras to spec** — IScreenshotService, IDiagnosticDriver, and control-specific helper methods should be documented
14. **Brinell.Maui.CommunityToolkit** — bring back under central package management

---

## 10. Conclusion

The srcnew implementation demonstrates **strong architectural discipline** and **excellent interface fidelity** (96% pass rate on Core interfaces). The MAUI platform is substantially complete with 30+ control implementations and two working driver backends.

The primary concerns are **code quality anti-patterns** (Thread.Sleep, empty catches) rather than architectural or API design gaps. These anti-patterns are well-documented in the project's own copilot-instructions.md but have not yet been addressed in the implementation.

The gap between spec and implementation is **manageable** — most deviations are minor naming or structural differences, and the missing controls are low-traffic edge cases. The critical fixes (P0) should be prioritized to maintain the framework's reliability guarantees.
