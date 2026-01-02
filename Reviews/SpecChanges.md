# Proposed Specification Changes - Review

**Review Date:** January 2, 2026
**Status:** Under Review
**Purpose:** Evaluate proposed changes to REQ-001 and REQ-002 specifications

---

## Summary

This document reviews proposed specification changes, formats them as proper requirements, provides analysis and recommendations, and includes tasklists for decision-making.

---

## Functional Requirements (REQ-001 Changes)

---

### FR-PROP-001: Expand Platform Scope

**Original Proposal:** Add to Scope: Stride, WinForms, Blazor

**Formatted Requirement:**

> **FR-001.1 (Amendment): Extended Platform Support**
>
> The framework MUST support automated testing across the following platforms:
>
> - Windows desktop applications (WPF)
> - Windows Forms applications (WinForms) ← NEW
> - Cross-platform desktop and mobile applications (MAUI)
> - Web applications via Selenium (HTML/JavaScript)
> - Web applications via Playwright (Blazor) ← NEW
> - 3D game applications (Stride Engine) ← NEW

**Analysis:**

| Aspect                    | Assessment                           |
| ------------------------- | ------------------------------------ |
| Already Implemented?      | ✅ Yes - All three exist in `src/` |
| Aligns with Architecture? | ✅ Yes - Follows platform pattern    |
| Breaking Change?          | ❌ No - Additive                     |
| Documentation Impact      | Medium - Spec updates needed         |

**Recommendation:** ✅ **ACCEPT** - These platforms are already implemented. This is purely documentation alignment.

**Tasklist:**

- [X] Accept FR-PROP-001: Add Stride to REQ-001 scope
- [X] Accept FR-PROP-001: Add WinForms to REQ-001 scope
- [X] Accept FR-PROP-001: Add Blazor/Playwright to REQ-001 scope
- [ ] Reject FR-PROP-001 (keep spec minimal)

---

### FR-PROP-002: Multiple Test Driver Support

**Original Proposal:** Add that multiple test frameworks are supported - Selenium, Appium, Playwright

**Formatted Requirement:**

> **FR-007.3 (New): Multi-Driver Web Platform**
>
> The framework MUST support multiple automation drivers for web testing:
>
> - Selenium WebDriver for traditional web applications
> - Playwright for modern web applications and Blazor
> - Appium WebDriver for mobile web and hybrid applications
>
> Each driver MUST have its own platform implementation project.

**Analysis:**

| Aspect                    | Assessment                                                |
| ------------------------- | --------------------------------------------------------- |
| Already Implemented?      | ✅ Yes - Brinell.Html (Selenium), Brinell.Html.Playwright |
| Aligns with Architecture? | ✅ Yes - Separate platform projects                       |
| Breaking Change?          | ❌ No - Additive                                          |
| Value                     | High - Documents actual capability                        |

**Recommendation:** ✅ **ACCEPT** - Documents existing functionality.

**Tasklist:**

- [X] Accept FR-PROP-002: Document multi-driver support in FR-007
- [ ] Reject FR-PROP-002 (implicit from platform list)

---

### FR-PROP-003: Free License Requirement

**Original Proposal:** NuGet packages should be MIT or another free license

**Formatted Requirement:**

> **FR-011 (New): Dependency Licensing**
>
> **Priority:** MUST
>
> All framework dependencies MUST use OSI-approved open source licenses that:
>
> - Allow commercial use without fees
> - Do not require per-developer or per-seat licensing
> - Include at minimum: MIT, Apache 2.0, BSD, LGPL
>
> **Rationale:** Ensures framework can be used in commercial projects without licensing concerns.

**Analysis:**

| Aspect               | Assessment                                        |
| -------------------- | ------------------------------------------------- |
| Currently Compliant? | ⚠️ Partial - FluentAssertions changed licensing |
| Impact               | High - May require dependency changes             |
| Breaking Change?     | Potentially - If existing deps violate            |
| Enforcement          | Difficult - Licenses can change                   |

**Recommendation:** ✅ **ACCEPT WITH MODIFICATION**

Add as a SHOULD requirement with exceptions process:

> Dependencies SHOULD use permissive open source licenses. Commercial/paid dependencies MUST be documented and approved.

**Tasklist:**

- [ ] Accept FR-PROP-003 as MUST requirement (strict)
- [X] Accept FR-PROP-003 as SHOULD requirement (flexible)
- [ ] Accept FR-PROP-003 with explicit allowed licenses list
- [ ] Reject FR-PROP-003 (leave to consumer discretion)

---

### FR-PROP-004: FluentAssertions Prohibition

**Original Proposal:** FluentAssertions is forbidden (not free anymore)

**Formatted Requirement:**

> **FR-011.1 (New): Prohibited Dependencies**
>
> The framework MUST NOT depend on FluentAssertions library.
>
> **Rationale:** FluentAssertions adopted commercial licensing (post v6.x) requiring paid licenses for commercial use.
>
> **Alternatives:**
>
> - Built-in Assert methods on control objects
> - Shouldly (MIT license)
> - xUnit assertions
> - Custom assertion helpers

**Analysis:**

| Aspect              | Assessment                                            |
| ------------------- | ----------------------------------------------------- |
| Current Usage       | ⚠️ Mentioned in SPEC-001 3.3.3 as sample dependency |
| Impact              | Medium - Samples/docs need updates                    |
| Valid Concern?      | ✅ Yes - FluentAssertions 7.x has commercial license  |
| Alternatives Exist? | ✅ Yes - Multiple options                             |

**Recommendation:** ✅ **ACCEPT** - Valid licensing concern. Framework should not recommend paid dependencies.

**Additional Notes:**

- FluentAssertions 6.x is still MIT (last free version)
- Could allow 6.x with version pin, but risky
- Better to use built-in assertions per FR-PROP-010

**Tasklist:**

- [X] Accept FR-PROP-004: Prohibit FluentAssertions entirely
- [ ] Accept FR-PROP-004: Allow FluentAssertions ≤6.x only
- [ ] Accept FR-PROP-004: Remove from docs, don't prohibit
- [ ] Reject FR-PROP-004 (let users decide)

---

### FR-PROP-005: Single Interface Hierarchy

**Original Proposal:** There is one interface hierarchy for control objects

**Formatted Requirement:**

> **FR-002.5 (New): Unified Control Interface Hierarchy**
>
> **Priority:** MUST
>
> The framework MUST define a single, unified interface hierarchy for control objects in Core:
>
> ```
> IControlObject (base)
> ├── IClickableControl
> │   └── IContentControl
> ├── ITextControl
> ├── IToggleControl
> ├── ISelectorControl
> ├── IRangeControl
> ├── IItemsControl
> └── IContainerControl
> ```
>
> All platform implementations MUST implement these interfaces.
> Platform-specific interfaces MAY extend the core interfaces.

**Analysis:**

| Aspect               | Assessment                                            |
| -------------------- | ----------------------------------------------------- |
| Already Implemented? | ✅ Yes - Exists in Brinell.Core.Abstractions.Controls |
| Documented in Spec?  | ⚠️ Partial - Implied but not explicit               |
| Value                | High - Clarifies architecture                         |

**Recommendation:** ✅ **ACCEPT** - Makes existing design explicit.

**Tasklist:**

- [X] Accept FR-PROP-005: Add explicit interface hierarchy to spec
- [ ] Accept FR-PROP-005: Add diagram to SPEC-001
- [ ] Reject FR-PROP-005 (already implied)

---

### FR-PROP-006: Platform-Specific Implementation Stacks

**Original Proposal:** Technologies (HTML, MAUI, Stride, WPF, WinForms) each implement their own test stack based on interfaces

**Formatted Requirement:**

> **FR-001.3 (Clarification): Platform-Specific Implementation Stacks**
>
> **Priority:** MUST
>
> Each platform MUST provide a complete, self-contained implementation:
>
> | Platform        | Project                 | Driver      | Stack           |
> | --------------- | ----------------------- | ----------- | --------------- |
> | WPF             | Brinell.Wpf             | FlaUI/UIA3  | Windows-only    |
> | WinForms        | Brinell.WinForms        | FlaUI/UIA3  | Windows-only    |
> | MAUI            | Brinell.Maui            | Appium      | Cross-platform  |
> | HTML/Selenium   | Brinell.Html            | Selenium    | Cross-platform  |
> | HTML/Playwright | Brinell.Html.Playwright | Playwright  | Cross-platform  |
> | Stride          | Brinell.Stride          | Named Pipes | Custom protocol |
>
> Platforms MUST NOT share implementation code (only interfaces).

**Analysis:**

| Aspect                  | Assessment                                   |
| ----------------------- | -------------------------------------------- |
| Already Implemented?    | ✅ Yes                                       |
| Documented?             | ⚠️ Partial - SPEC-001 mentions 3 platforms |
| Clarifies Architecture? | ✅ Yes                                       |

**Recommendation:** ✅ **ACCEPT** - Explicit is better than implicit.

**Tasklist:**

- [X] Accept FR-PROP-006: Add platform table to SPEC-001
- [ ] Accept FR-PROP-006: Update FR-001.3 in REQ-001
- [ ] Reject FR-PROP-006 (covered by existing spec)

---

### FR-PROP-007: Overridable Timeouts on All Methods

**Original Proposal:** Each action/get/set method should have an overridable timeout in milliseconds. Default from context.

**Formatted Requirement:**

> **FR-005.2 (Amendment): Per-Operation Timeout Override**
>
> **Priority:** MUST
>
> ALL control and page object methods that involve waiting MUST accept an optional timeout parameter:
>
> ```csharp
> // Pattern for all Wait/Check/Action methods
> void Click(int? timeoutMs = null);
> bool WaitVisible(bool expected = true, int? timeoutMs = null);
> void CheckEnabled(bool expected = true, int? timeoutMs = null);
> string GetText(int? timeoutMs = null);  // If involves wait
> void SetText(string value, int? timeoutMs = null);
> ```
>
> When `timeoutMs` is null, the method MUST use `ITestContext.DefaultTimeoutMs`.
>
> **Rationale:** Enables fine-grained control for slow operations without changing global settings.

**Analysis:**

| Aspect                | Assessment                                            |
| --------------------- | ----------------------------------------------------- |
| Already Implemented?  | ⚠️ Partial - Wait/Check have it, some actions don't |
| Breaking Change?      | ❌ No - Optional parameter                            |
| Value                 | High - Flexibility for edge cases                     |
| Implementation Effort | Medium - Audit all methods                            |

**Recommendation:** ✅ **ACCEPT** - Important for real-world usage.

**Note:** Currently `Click()`, `Enter()`, `GetText()` don't have timeouts - they rely on precondition waits.

**Tasklist:**

- [X] Accept FR-PROP-007: All methods get timeout parameter
- [ ] Accept FR-PROP-007: Only action methods (Click, Enter, etc.)
- [ ] Accept FR-PROP-007: Document current partial implementation as sufficient
- [ ] Reject FR-PROP-007 (current Wait/Check pattern is enough)

---

### FR-PROP-008: 80% Standard Control Coverage

**Original Proposal:** 80% of common standard controls should be supported out of the box

**Formatted Requirement:**

> **FR-002.4 (Amendment): Standard Control Coverage**
>
> **Priority:** SHOULD
>
> Each platform implementation SHOULD support at least 80% of the platform's standard control types:
>
> | Category     | Example Controls                     |
> | ------------ | ------------------------------------ |
> | Text Display | Label, TextBlock                     |
> | Text Input   | TextBox, Entry, Editor, PasswordBox  |
> | Buttons      | Button, ImageButton, ToggleButton    |
> | Selection    | CheckBox, RadioButton, Switch        |
> | Lists        | ListBox, ListView, ComboBox, Picker  |
> | Containers   | Panel, Frame, ScrollView, TabControl |
> | Range        | Slider, ProgressBar, Stepper         |
> | Date/Time    | DatePicker, TimePicker               |
> | Navigation   | Menu, NavigationView, Shell          |
>
> **Measurement:** Coverage = (Implemented Controls / Platform Standard Controls) × 100

**Analysis:**

| Aspect           | Assessment                                |
| ---------------- | ----------------------------------------- |
| Current Coverage | WPF: ~70%, MAUI: ~85%, HTML: ~60%         |
| Measurable?      | ⚠️ Difficult - "standard" is subjective |
| Value            | Medium - Sets expectations                |

**Recommendation:** 🟡 **ACCEPT WITH MODIFICATION**

Change to specific control lists per platform rather than percentage:

> Each platform MUST support the controls listed in Appendix A.

**Tasklist:**

- [ ] Accept FR-PROP-008: 80% as stated
- [X] Accept FR-PROP-008: Define explicit control lists per platform
- [ ] Accept FR-PROP-008: As SHOULD (not MUST)
- [ ] Reject FR-PROP-008 (too prescriptive)

---

### FR-PROP-009: Separate Projects for Control Libraries

**Original Proposal:** For control libraries, separate projects are created that define control objects

**Formatted Requirement:**

> **FR-008.4 (New): Third-Party Control Library Support**
>
> **Priority:** MAY
>
> For third-party control libraries (e.g., Telerik, DevExpress, Syncfusion), separate NuGet packages MAY be created:
>
> ```
> Brinell.Wpf.Telerik       - Telerik WPF controls
> Brinell.Wpf.DevExpress    - DevExpress WPF controls
> Brinell.Maui.Syncfusion   - Syncfusion MAUI controls
> ```
>
> These packages:
>
> - MUST reference the base platform package
> - MUST follow the same interface patterns
> - SHOULD be maintained separately from core framework
> - MAY be community-contributed

**Analysis:**

| Aspect               | Assessment                     |
| -------------------- | ------------------------------ |
| Already Implemented? | ❌ No - Only standard controls |
| Value                | High - Extensibility story     |
| Scope Creep Risk     | ⚠️ Could expand maintenance  |
| Best Practice        | ✅ Yes - Clean separation      |

**Recommendation:** ✅ **ACCEPT as MAY** - Good extensibility pattern without commitment.

**Tasklist:**

- [ ] Accept FR-PROP-009: As MAY (optional capability)
- [X] Accept FR-PROP-009: As SHOULD (recommended pattern)
- [ ] Accept FR-PROP-009: Document pattern only, no requirement
- [ ] Reject FR-PROP-009 (out of scope)

---

### FR-PROP-010: Use Control Assert Methods

**Original Proposal:** Test assertions should use the assert methods on control objects if available

**Formatted Requirement:**

> **FR-004.5 (New): Prefer Control Object Assertions**
>
> **Priority:** SHOULD
>
> Test code SHOULD prefer using control object assertion methods over external assertion libraries:
>
> ```csharp
> // ✅ PREFERRED - Uses control's Assert method
> loginButton.AssertEnabled();
> usernameField.AssertTextEquals("admin");
>
> // ❌ DISCOURAGED - External library
> loginButton.IsEnabled().Should().BeTrue();  // FluentAssertions
> Assert.True(loginButton.IsEnabled());        // xUnit
> ```
>
> **Rationale:**
>
> - Control assertions include automatic logging
> - Control assertions capture screenshots on failure
> - Control assertions provide better error messages with context
> - Removes dependency on external assertion libraries

**Analysis:**

| Aspect               | Assessment                               |
| -------------------- | ---------------------------------------- |
| Already Implemented? | ✅ Yes - Assert* methods exist           |
| Documented?          | ⚠️ Not as best practice                |
| Value                | High - Consistency, logging, screenshots |
| Enforcement          | ❌ Cannot enforce in code                |

**Recommendation:** ✅ **ACCEPT** - Aligns with built-in assertion pattern and addresses FR-PROP-004.

**Tasklist:**

- [X] Accept FR-PROP-010: As SHOULD (best practice)
- [ ] Accept FR-PROP-010: As MUST (strict requirement)
- [ ] Accept FR-PROP-010: Document in best practices only
- [ ] Reject FR-PROP-010 (let users choose)

---

### FR-PROP-011: Timeout Failures Must Throw

**Original Proposal:** If a final timeout fails it should throw an exception and stop the test. No silent continues.

**Formatted Requirement:**

> **FR-010.4 (New): Fail-Fast on Timeout**
>
> **Priority:** MUST
>
> When a timeout expires during a Check* or Action method:
>
> - The method MUST throw an exception immediately
> - The method MUST NOT return false and continue
> - The method MUST NOT silently swallow the timeout
> - The exception MUST include: element ID, timeout value, and current state
>
> ```csharp
> // Check* methods - MUST throw on timeout
> button.CheckVisible();  // Throws CheckFailedException if timeout
>
> // Wait* methods - MAY return false (caller decides)
> if (!button.WaitVisible()) 
> {
>     // Caller handles failure
> }
> ```
>
> **Distinction:**
>
> - `Wait*` methods return bool (polling, caller decides)
> - `Check*` methods throw on failure (preconditions)
> - `Assert*` methods throw on failure (test assertions)
> - Action methods throw on failure (Click, Enter, etc.)

**Analysis:**

| Aspect               | Assessment                        |
| -------------------- | --------------------------------- |
| Already Implemented? | ✅ Yes - Check/Assert throw       |
| Documented?          | ⚠️ Partial - In SPEC-005        |
| Critical?            | ✅ Yes - Prevents silent failures |

**Recommendation:** ✅ **ACCEPT** - Critical for test reliability.

**Tasklist:**

- [X] Accept FR-PROP-011: Explicit fail-fast requirement
- [X] Accept FR-PROP-011: Clarify Wait vs Check vs Assert distinction
- [ ] Reject FR-PROP-011 (already implemented)

---

## Non-Functional Requirements (REQ-002 Changes)

---

### NFR-PROP-001: Test Timeout Prevention

**Original Proposal:** Tests should not be able to hang and automatically shutdown after configurable timeout. Normally 2 minutes.

**Formatted Requirement:**

> **NFR-REL-003 (New): Test Execution Timeout**
>
> **Priority:** SHOULD
>
> The framework SHOULD support test-level execution timeouts:
>
> | Setting               | Default        | Description                 |
> | --------------------- | -------------- | --------------------------- |
> | `TestTimeoutMs`     | 120000 (2 min) | Maximum test execution time |
> | `SetupTimeoutMs`    | 60000 (1 min)  | Maximum setup time          |
> | `TeardownTimeoutMs` | 30000 (30 sec) | Maximum teardown time       |
>
> When timeout is exceeded:
>
> - Test MUST be terminated
> - Application SHOULD be force-closed
> - Failure MUST be logged with timeout reason
> - Screenshot SHOULD be captured before termination
>
> **Implementation Options:**
>
> 1. xUnit `[Fact(Timeout = 120000)]` attribute
> 2. Custom test wrapper with CancellationToken
> 3. Process-level watchdog

**Analysis:**

| Aspect               | Assessment                        |
| -------------------- | --------------------------------- |
| Already Implemented? | ❌ No                             |
| Value                | High - Prevents CI/CD hangs       |
| Difficulty           | Medium - xUnit has native support |
| Scope                | Test runner level, not framework  |

**Recommendation:** ✅ **ACCEPT WITH CLARIFICATION**

This is partially a test runner concern. Recommend:

1. Document xUnit timeout usage
2. Provide timeout wrapper in test base classes
3. Framework timeouts (element waits) are separate from test timeouts

**Tasklist:**

- [X] Accept NFR-PROP-001: As framework requirement
- [ ] Accept NFR-PROP-001: Document as best practice only
- [X] Accept NFR-PROP-001: Add timeout support to UITestBase
- [ ] Accept NFR-PROP-001: Use xUnit native timeout only
- [ ] Reject NFR-PROP-001 (runner responsibility)

---

---
