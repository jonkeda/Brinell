# Tasks Document

## Task Format

Each task should follow this structure:
- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields
- _Prompt provides AI guidance for implementing the task

---

## Phase 1: MAUI Interfaces

### [x] 1. Create IMauiElementScope Interface
- **File:** `srcnew/Brinell.Maui/Interfaces/IMauiElementScope.cs`
- **Purpose:** Define MAUI-specific element scope with context access
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/IElementScope.cs`
- **_Requirements:** R1 (Element Scope Abstraction)
- **_Prompt:** Role: C# Interface Designer specializing in UI test frameworks | Task: Create IMauiElementScope interface extending IElementScope<AppiumElement> with a Context property returning IMauiTestContext, following design.spc.spx.md Component 1 | Restrictions: Do not add methods beyond IElementScope contract and Context property, maintain interface segregation | Success: Interface compiles, extends IElementScope<AppiumElement>, exposes IMauiTestContext Context property

### [x] 2. Create IMauiTestContext Interface
- **File:** `srcnew/Brinell.Maui/Interfaces/IMauiTestContext.cs`
- **Purpose:** Define MAUI test context with Appium driver access
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/ITestContext.cs`, `srcnew/Brinell.Core/Interfaces/IElementScope.cs`
- **_Requirements:** R1, R6 (Element Scope, Page Scope)
- **_Prompt:** Role: C# Interface Designer for test automation | Task: Create IMauiTestContext interface extending both ITestContext<AppiumElement> and IMauiElementScope, adding AppiumDriver Driver property per design.spc.spx.md Component 2 | Restrictions: Keep interface minimal, do not duplicate parent interface members, self-reference Context to return this | Success: Interface compiles, inherits from both ITestContext<AppiumElement> and IMauiElementScope, exposes AppiumDriver property

---

## Phase 2: Locator Extensions

### [x] 3. Create LocatorExtensions Utility
- **File:** `srcnew/Brinell.Maui/Extensions/LocatorExtensions.cs`
- **Purpose:** Convert Brinell Locator to Appium By selector
- **_Leverage:** `srcnew/Brinell.Core/Locators/Locator.cs`
- **_Requirements:** R1 (Element Scope - element finding)
- **_Prompt:** Role: C# Extension Methods Developer for Appium integration | Task: Create static LocatorExtensions class with ToBy() extension method converting Locator to OpenQA.Selenium.By, handling all LocatorStrategy values (AutomationId, XPath, Name, ClassName, AccessibilityId) | Restrictions: Use switch expression, throw ArgumentOutOfRangeException for unknown strategies, do not handle Parent locator chaining (simple conversion only) | Success: Extension converts all LocatorStrategy values to correct By selectors, compiles against Appium.WebDriver package

---

## Phase 3: Test Context

### [x] 4. Create MauiTestContextOptions
- **File:** `srcnew/Brinell.Maui/Context/MauiTestContextOptions.cs`
- **Purpose:** Configuration options for MAUI test context
- **_Leverage:** `srcnew/Brinell.Core/Configuration/TimeoutSettings.cs`
- **_Requirements:** R6 (Page Scope - configurable timeouts)
- **_Prompt:** Role: C# Developer specializing in configuration patterns | Task: Create MauiTestContextOptions class with properties: Uri AppiumServerUri, AppiumOptions AppiumOptions, TimeoutSettings? Timeouts, ITestLogger? Logger per design.spc.spx.md Data Models | Restrictions: Use init-only setters, provide sensible defaults for Timeouts, do not add validation logic in the options class | Success: Class compiles with all properties, nullable properties properly annotated

### [x] 5. Create MauiTestContext Implementation
- **File:** `srcnew/Brinell.Maui/Context/MauiTestContext.cs`
- **Purpose:** Concrete MAUI test context with Appium driver management
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/ITestContext.cs`, `srcnew/Brinell.Maui/Extensions/LocatorExtensions.cs`
- **_Requirements:** R1, R6 (Element Scope, Page Scope)
- **_Prompt:** Role: Senior C# Developer specializing in Appium WebDriver integration | Task: Create MauiTestContext class implementing IMauiTestContext with: constructor accepting MauiTestContextOptions, AppiumDriver initialization, TryFindElement using LocatorExtensions.ToBy(), IDisposable for driver cleanup per design.spc.spx.md Component 3 | Restrictions: Do not cache elements at context level, use try-catch returning null for TryFindElement, implement Context property returning this | Success: Class creates AppiumDriver, implements all interface members, properly disposes driver, TryFindElement returns null on not found

---

## Phase 4: Control Base Classes

### [x] 6. Create MauiControlBase
- **File:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose:** Base class for all MAUI controls with Is/Wait/Assert pattern
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/IControlObject.cs`, `srcnew/Brinell.Maui/Interfaces/IMauiElementScope.cs`
- **_Requirements:** R2 (IControlObject Base Interface)
- **_Prompt:** Role: Senior C# Developer specializing in UI test framework design | Task: Create MauiControlBase implementing IControlObject with constructor(IMauiElementScope scope, Locator locator), implement all Is*/Wait*/Assert* methods per requirements.spc.spx.md R2 acceptance criteria: IsExists immediate check, IsVisible/IsEnabled return null when element missing, Wait methods poll with timeout, Assert methods throw AssertionException with message | Restrictions: Do not cache element references, use scope.TryFindElement for every state check, follow nullable skip pattern (null params skip operation), keep element find logic in protected method | Success: All IControlObject methods implemented, null safety respected, Is methods return null for missing elements, assertions include custom message

### [x] 7. Create MauiContainerBase
- **File:** `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`
- **Purpose:** Base class for container controls that scope child element searches
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/IContainerControl.cs`, `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **_Requirements:** R5 (Container/View as Element Scope)
- **_Prompt:** Role: Senior C# Developer for test automation frameworks | Task: Create MauiContainerBase extending MauiControlBase, implementing IContainerControl<AppiumElement> and IMauiElementScope, with: lazy ContainerRoot property, TryFindElement scoped to container root, InvalidateCache method per design.spc.spx.md Component 6 and requirements.spc.spx.md R5 | Restrictions: Cache container root but re-find on stale, search within container using element.FindElement not driver, expose Context from parent scope | Success: Container searches scoped to its root element, caching works with invalidation, implements IMauiElementScope for child control creation

---

## Phase 5: Page Object Base

### [x] 8. Create MauiPageObjectBase
- **File:** `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`
- **Purpose:** Base class for MAUI page objects
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/IPageObject.cs`, `srcnew/Brinell.Maui/Interfaces/IMauiElementScope.cs`
- **_Requirements:** R6 (Page as Element Scope)
- **_Prompt:** Role: Senior C# Developer specializing in Page Object pattern | Task: Create abstract MauiPageObjectBase implementing IPageObject<AppiumElement> and IMauiElementScope with: constructor(IMauiTestContext context), TryFindElement delegating to context, abstract IsLoaded method, WaitLoaded/AssertLoaded methods per design.spc.spx.md Component 5 and requirements.spc.spx.md R6 | Restrictions: Pages do not search for specific element (delegate to context for driver root), do not cache any elements, provide protected property for context access | Success: Page delegates element finding to context, IsLoaded is abstract for subclass implementation, wait/assert load methods work correctly

---

## Phase 6: Concrete Controls

### [x] 9. Create MauiButtonControl
- **File:** `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- **Purpose:** MAUI Button control with click capability
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs`, `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **_Requirements:** R3, R8 (IClickableControl, MAUI Button)
- **_Prompt:** Role: C# Developer for UI test automation | Task: Create MauiButtonControl extending MauiControlBase, implementing IClickableControlObject with: Click waits for clickable then calls element.Click(), DoubleClick performs two clicks, IsClickable returns visible AND enabled per requirements.spc.spx.md R3 and R8 | Restrictions: Click on disabled element does nothing (no throw), wait for visibility before click with timeout, use element.Click() not JavaScript click | Success: Click waits for element to be clickable, disabled element click is no-op, GetText returns element.Text, DoubleClick works

### [x] 10. Create MauiEntryControl
- **File:** `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
- **Purpose:** MAUI Entry control with text input capability
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/IEditableTextControlObject.cs`, `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **_Requirements:** R4, R9 (IEditableTextControl, MAUI Entry)
- **_Prompt:** Role: C# Developer for UI test automation | Task: Create MauiEntryControl extending MauiControlBase, implementing IEditableTextControlObject with: Enter appends via element.SendKeys, Clear uses element.Clear, SetText does Clear+Enter, GetPlaceholder gets hint attribute per requirements.spc.spx.md R4 and R9 | Restrictions: Enter on null text skips (nullable pattern), Enter on disabled element does nothing (no throw), use Appium SendKeys not JavaScript | Success: All text operations work, nullable skip pattern honored, GetPlaceholder returns correct attribute, SetText clears before entering

---

## Phase 7: Cleanup and Verification

### [x] 11. Remove Placeholder Files
- **Files:** 
  - `srcnew/Brinell.Maui/Controls/Placeholder.cs`
  - `srcnew/Brinell.Maui/Context/Placeholder.cs`
  - `srcnew/Brinell.Maui/Pages/Placeholder.cs`
- **Purpose:** Clean up placeholder files now that real implementations exist
- **_Leverage:** N/A
- **_Requirements:** N/A (cleanup)
- **_Prompt:** Role: DevOps Engineer | Task: Delete the three Placeholder.cs files from Controls, Context, and Pages folders | Restrictions: Only delete Placeholder.cs files, do not modify any other files | Success: Placeholder files removed, project still compiles

### [x] 12. Verify Project Compilation
- **Files:** `srcnew/Brinell.Maui/Brinell.Maui.csproj`
- **Purpose:** Ensure all new files compile together without errors
- **_Leverage:** All files created in tasks 1-10
- **_Requirements:** All
- **_Prompt:** Role: Build Engineer | Task: Run dotnet build on Brinell.Maui project, verify no compilation errors, check all interfaces are implemented correctly | Restrictions: Do not modify code, only verify compilation | Success: Project builds successfully with no errors or warnings

---

## Phase 8: Unit Tests

### [ ] 13. Create MauiControlBase Unit Tests
- **File:** `testsnew/Brinell.Maui.Tests/Controls/MauiControlBaseTests.cs`
- **Purpose:** Test Is/Wait/Assert methods with mocked elements
- **_Leverage:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **_Requirements:** R2
- **_Prompt:** Role: Test Engineer specializing in unit testing with mocking | Task: Create unit tests for MauiControlBase testing: IsExists returns true/false correctly, IsVisible returns null when element missing, WaitExists polls and times out, AssertExists throws with message, nullable skip pattern works | Restrictions: Mock IMauiElementScope and AppiumElement, do not test Appium directly, use xUnit and Moq | Success: Tests cover all state methods, null scenarios tested, timeout behavior verified

### [ ] 14. Create MauiButtonControl Unit Tests
- **File:** `testsnew/Brinell.Maui.Tests/Controls/MauiButtonControlTests.cs`
- **Purpose:** Test click behavior with mocked Appium
- **_Leverage:** `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- **_Requirements:** R3, R8
- **_Prompt:** Role: Test Engineer for UI test frameworks | Task: Create unit tests for MauiButtonControl testing: Click waits then clicks, Click on disabled is no-op, DoubleClick calls click twice, IsClickable checks visible AND enabled | Restrictions: Mock element, do not need real Appium, verify element.Click called | Success: All click scenarios tested, disabled behavior verified, double-click verified

### [ ] 15. Create MauiEntryControl Unit Tests
- **File:** `testsnew/Brinell.Maui.Tests/Controls/MauiEntryControlTests.cs`
- **Purpose:** Test text entry with mocked Appium
- **_Leverage:** `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
- **_Requirements:** R4, R9
- **_Prompt:** Role: Test Engineer for UI test frameworks | Task: Create unit tests for MauiEntryControl testing: Enter calls SendKeys, Clear calls Clear, SetText calls Clear then SendKeys, Enter with null skips, GetPlaceholder returns attribute | Restrictions: Mock element, verify Appium method calls | Success: All text operations tested, nullable skip verified, placeholder retrieval tested

### [ ] 16. Create MauiContainerBase Unit Tests
- **File:** `testsnew/Brinell.Maui.Tests/Controls/MauiContainerBaseTests.cs`
- **Purpose:** Test scoped element finding
- **_Leverage:** `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`
- **_Requirements:** R5
- **_Prompt:** Role: Test Engineer specializing in scoping tests | Task: Create unit tests for MauiContainerBase testing: TryFindElement searches within container root, child controls use container scope, InvalidateCache clears cached root, nested containers scope correctly | Restrictions: Mock elements to verify FindElement called on container not driver | Success: Scoping verified, cache invalidation works, nested scenarios tested

---

## Summary

| Phase | Tasks | Files Created |
|-------|-------|---------------|
| 1. Interfaces | 1-2 | 2 interface files |
| 2. Extensions | 3 | 1 utility file |
| 3. Context | 4-5 | 2 context files |
| 4. Control Base | 6-7 | 2 base class files |
| 5. Page Base | 8 | 1 page base file |
| 6. Controls | 9-10 | 2 control files |
| 7. Cleanup | 11-12 | 0 (deletions + verify) |
| 8. Tests | 13-16 | 4 test files |

**Total: 16 tasks creating 10 implementation files + 4 test files**
