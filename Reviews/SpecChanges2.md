# Proposed Specification Changes - Part 2

**Review Date:** January 2, 2026
**Status:** Under Review
**Purpose:** Additional specification changes based on implementation analysis

---

## 1. Container Support

### FR-PROP-012: Container-Scoped Control Search

**Current State:**

Brinell implementations already support container-scoped element searching:

```csharp
// MAUI ControlBase - Container support already implemented
protected ControlBase(AppiumTestContext context, IPageObject? page, 
    AppiumElement? container, string automationId)

// WPF ControlBase - Container support already implemented  
protected ControlBase(FlaUITestContext context, IPageObject? page,
    AutomationElement? container, string automationId)

// WinForms - Same pattern as WPF
public ComboBoxControl(FlaUITestContext context, IPageObject? page, 
    AutomationElement container, string automationId)
```

**Used for:**

- Controls inside ListView/CollectionView items
- Repeating templates (data templates)
- Nested control hierarchies
- Scoping search to improve performance

**Formatted Requirement:**

> **FR-002.5 (New): Container-Scoped Control Objects**
>
> **Priority:** SHOULD
>
> Platform control base classes SHOULD support container-scoped element searching:
>
> 1. All control base classes SHOULD accept an optional container parameter
> 2. When container is specified, element search MUST be scoped to descendants of that container
> 3. When container is null, element search MUST search from the application/window root
>
> **Use Cases:**
>
> - Controls inside list item templates
> - Controls within data-bound repeaters
> - Performance optimization for large UI trees
>
> **Example Pattern:**
>
> ```csharp
> // Root-level control (no container)
> var saveButton = new ButtonControl(context, page, "SaveButton");
>
> // Control inside a list item (with container)
> var listItem = collectionView.GetItemContainer(0);
> var deleteButton = new ButtonControl(context, page, listItem, "DeleteButton");
> ```

**Analysis:**

| Aspect               | Assessment                                  |
| -------------------- | ------------------------------------------- |
| Already Implemented? | ✅ Yes - MAUI, WPF, WinForms                |
| In Current Spec?     | ❌ No - Not mentioned                       |
| Value                | High - Critical for list/collection testing |
| Breaking Change?     | ❌ No - Optional parameter                  |

**Recommendation:** ✅ **ACCEPT** - Documents existing, valuable functionality.

**Tasklist:**

- [X] Accept FR-PROP-012: Add container support to FR-002
- [X] Accept FR-PROP-012: Add to SPEC-003 (Control Objects)
- [X] Accept FR-PROP-012: Define as MUST for all platforms
- [ ] Reject FR-PROP-012 (implicit/obvious)

---

## 2. Async/Await Architectural Decision

### AD-PROP-009: Synchronous Control Operations

**Current Implementation:**

All Brinell platforms implement **synchronous** control operations:

```csharp
// All control methods are sync
public virtual void Click() { ... }
public virtual bool WaitVisible(bool expected = true, int? timeoutMs = null) { ... }
public virtual void CheckEnabled(bool expected = true, int? timeoutMs = null) { ... }
public virtual string GetText() { ... }
```

**Only async in setup/teardown:**

```csharp
// UITestBase implements IAsyncLifetime for xUnit
public class UITestBase<TContext> : IAsyncLifetime
{
    public virtual Task InitializeAsync() { ... }
    public virtual Task DisposeAsync() { ... }
}
```

**Formatted Architectural Decision:**

> **AD-009: Synchronous Control Operations**
>
> ### Context
>
> UI automation operations can be implemented as sync or async. Modern C# favors async, but UI automation drivers are typically synchronous.
>
> ### Decision
>
> **Control operations are synchronous.** Test base classes implement `IAsyncLifetime` for async setup/teardown.
>
> - `Click()`, `Tap()`, `Enter()` - Sync
> - `WaitVisible()`, `WaitEnabled()` - Sync (polling internally)
> - `GetText()`, `IsEnabled()` - Sync
> - `InitializeAsync()`, `DisposeAsync()` - Async (xUnit lifecycle)
>
> ### Rationale
>
> **Benefits:**
>
> 1. Matches native driver APIs (FlaUI, Selenium are sync)
> 2. Simpler test code (no await on every operation)
> 3. Polling waits block naturally (no Task.Delay overhead)
> 4. Easier debugging (sync stack traces)
>
> **Drawbacks:**
>
> 1. Cannot run control operations in parallel
> 2. Blocks thread during waits
> 3. Modern C# prefers async patterns
>
> ### Alternatives Considered
>
> **Alternative 1: Async-First Design**
>
> ```csharp
> await button.ClickAsync();
> await textBox.WaitVisibleAsync();
> ```
>
> **Rejected:** Every test line needs await, drivers are sync anyway.
>
> **Alternative 2: Dual Sync/Async APIs**
>
> ```csharp
> button.Click();        // Sync
> await button.ClickAsync();  // Async
> ```
>
> **Rejected:** Double API surface, maintenance burden, no real benefit.
>
> ### Status
>
> **ADOPTED** - Current implementation, should be documented.

**Analysis:**

| Aspect               | Assessment                         |
| -------------------- | ---------------------------------- |
| Already Implemented? | ✅ Yes - All platforms are sync    |
| In Current Spec?     | ❌ No - Not documented             |
| Important?           | ✅ Yes - Fundamental design choice |
| Controversial?       | ⚠️ Some prefer async-first       |

**Recommendation:** ✅ **ACCEPT** - Document the deliberate choice.

**Functional Requirement:**

> **FR-005.5 (New): Synchronous Operation Model**
>
> **Priority:** MUST
>
> Control and page object operations MUST be synchronous:
>
> - Action methods (`Click`, `Enter`, `Select`) - Synchronous
> - Wait methods (`WaitVisible`, `WaitEnabled`) - Synchronous with internal polling
> - Is methods (`IsVisible`, `IsEnabled`) - Synchronous immediate check
> - Get/Set methods (`GetText`, `SetText`) - Synchronous
>
> Test base classes SHOULD implement `IAsyncLifetime` for async test setup/teardown.
>
> **Rationale:** Native automation drivers are synchronous. Async wrappers add overhead without benefit.

**Tasklist:**

- [X] Accept AD-PROP-009: Add AD-009 to DES-001 (Architectural Decisions)
- [X] Accept AD-PROP-009: Add FR-005.5 to REQ-001 (Functional Requirements)
- [X] Accept AD-PROP-009: Document in SPEC-003 and SPEC-004
- [ ] Modify AD-PROP-009: Add async variants as FUTURE consideration
- [ ] Reject AD-PROP-009 (leave undocumented)

---

## 3. Update Naming to Brinell

### SPEC-CHANGE-001: Project Naming Convention

**Current Spec (SPEC-001):**

```
Oravey.UITestFramework.Core
Oravey.UITestFramework.Wpf
Oravey.UITestFramework.Maui
Oravey.UITestFramework.Html
```

**Actual Implementation:**

```
Brinell.Core
Brinell.Wpf
Brinell.Maui
Brinell.Html
Brinell.Html.Playwright
Brinell.Stride
Brinell.WinForms
Brinell.Testing
Brinell.Mocking
```

**Required Updates:**

| Document | Section       | Old Name                          | New Name               |
| -------- | ------------- | --------------------------------- | ---------------------- |
| SPEC-001 | §3.1 Diagram | Oravey.UITestFramework.*          | Brinell.*              |
| SPEC-001 | §3.1.2       | Oravey.UITestFramework.Core       | Brinell.Core           |
| SPEC-001 | §3.2         | Oravey.UITestFramework.{Platform} | Brinell.{Platform}     |
| SPEC-001 | §5.2         | Project references                | Brinell.{Platform}     |
| SPEC-001 | §5.3         | Folder structure                  | Brinell.sln, Brinell.* |
| DES-001  | Examples      | using Oravey.UITestFramework      | using Brinell          |

**Recommendation:** ✅ **ACCEPT** - Critical documentation accuracy.

**Tasklist:**

- [X] Accept SPEC-CHANGE-001: Update SPEC-001 project names to Brinell.*
- [X] Accept SPEC-CHANGE-001: Update DES-001 code examples
- [X] Accept SPEC-CHANGE-001: Update REQ-001 if any references
- [X] Accept SPEC-CHANGE-001: Update all sample code in specs

---

## 4. Additional Proposed Changes (Based on Implementation)

### FR-PROP-013: BusyPageBase Pattern

**Current Implementation:**

Both MAUI and WPF implement `BusyPageBase` for IsBusy tracking:

```csharp
// MAUI - BusyPageBase
public abstract class BusyPageBase : PageBase
{
    protected virtual string? BusyIndicatorId => null;
  
    public virtual bool IsBusy() { ... }
    public override bool IsReady() => IsDisplayed() && !IsBusy();
    public virtual bool WaitForNotBusy(int? timeoutMs = null) { ... }
}

// WPF - BusyPageBase  
public abstract class BusyPageBase : PageBase
{
    public abstract bool IsBusy();
    public bool IsNotBusy() => !IsBusy();
    public override bool IsReady() => IsDisplayed() && !IsBusy();
}
```

**Formatted Requirement:**

> **FR-005.4.1 (Amendment): BusyPageBase Pattern**
>
> **Priority:** SHOULD
>
> Platform implementations SHOULD provide a `BusyPageBase` class:
>
> | Method                      | Description                                        |
> | --------------------------- | -------------------------------------------------- |
> | `IsBusy()`                | Returns true if page is showing loading/busy state |
> | `IsNotBusy()`             | Returns true if page is not busy                   |
> | `WaitForNotBusy(timeout)` | Waits for busy state to clear                      |
> | `IsReady()` override      | Returns `IsDisplayed() && !IsBusy()`             |
>
> **Implementation Options:**
>
> 1. Override `BusyIndicatorId` property (element-based)
> 2. Override `IsBusy()` method (custom logic)

**Analysis:**

| Aspect       | Assessment                                   |
| ------------ | -------------------------------------------- |
| Implemented? | ✅ MAUI, ✅ WPF, ❌ HTML                     |
| In Spec?     | ⚠️ FR-005.4 mentions it, SPEC-001 shows it |
| Consistency  | ⚠️ HTML missing implementation             |

**Recommendation:** ✅ **ACCEPT** - Make explicit in REQ-001.

**Tasklist:**

- [X] Accept FR-PROP-013: Add explicit BusyPageBase requirement
- [X] Accept FR-PROP-013: Add to HTML platform
- [ ] Reject FR-PROP-013 (already implied by FR-005.4)

---

### FR-PROP-014: Gesture Support for Mobile Platforms

**Current Implementation (MAUI):**

```csharp
// ControlBase gestures
public virtual void Tap() { ... }
public virtual void Click() => Tap();
public virtual void DoubleTap() { ... }
public virtual void LongPress(int durationMs = 1000) { ... }
public virtual void Swipe(SwipeDirection direction, int distance = 200) { ... }
public virtual void SwipeLeft(int distance = 200) { ... }
public virtual void SwipeRight(int distance = 200) { ... }
public virtual void SwipeUp(int distance = 200) { ... }
public virtual void SwipeDown(int distance = 200) { ... }
```

**Formatted Requirement:**

> **FR-007.2.1 (Amendment): Mobile Gesture Support**
>
> **Priority:** MUST (for mobile platforms)
>
> Mobile platform implementations (MAUI for Android/iOS) MUST support:
>
> | Gesture    | Method                         | Description                               |
> | ---------- | ------------------------------ | ----------------------------------------- |
> | Tap        | `Tap()`                      | Single tap/touch                          |
> | Double Tap | `DoubleTap()`                | Two taps in quick succession              |
> | Long Press | `LongPress(durationMs)`      | Extended press with configurable duration |
> | Swipe      | `Swipe(direction, distance)` | Directional swipe gesture                 |
>
> **Platform Mapping:**
>
> - `Click()` SHOULD alias to `Tap()` for mobile
> - Desktop platforms MAY implement gestures as no-ops or throw NotSupportedException
>
> **SwipeDirection Enum:**
>
> - `Left`, `Right`, `Up`, `Down`

**Analysis:**

| Aspect       | Assessment                                  |
| ------------ | ------------------------------------------- |
| Implemented? | ✅ MAUI complete                            |
| In Spec?     | ⚠️ Mentioned in FR-007.2 but not detailed |
| Value        | High - Core mobile testing capability       |

**Recommendation:** ✅ **ACCEPT** - Document existing capability.

**Tasklist:**

- [X] Accept FR-PROP-014: Add gesture details to FR-007.2
- [X] Accept FR-PROP-014: Add to SPEC-007 platform specifications
- [ ] Reject FR-PROP-014 (too detailed for requirements)

---

### FR-PROP-015: Scroll-to-Element Support

**Current Implementation (MAUI):**

```csharp
// ScrollViewControl
public bool ScrollToElement(string automationId, int maxAttempts = 10)
{
    // Scrolls until element is found or max attempts reached
}

public void ScrollToTop() { ... }
public void ScrollToBottom() { ... }

// ItemsControlBase
public void ScrollUp() { ... }
public void ScrollDown() { ... }
```

**Formatted Requirement:**

> **FR-002.6 (New): Scroll-to-Element Support**
>
> **Priority:** SHOULD
>
> Scrollable container controls SHOULD support scrolling to make elements visible:
>
> | Method                            | Description                             |
> | --------------------------------- | --------------------------------------- |
> | `ScrollToElement(automationId)` | Scroll until element with ID is visible |
> | `ScrollToTop()`                 | Scroll to top of content                |
> | `ScrollToBottom()`              | Scroll to bottom of content             |
> | `ScrollUp(distance)`            | Scroll up by distance                   |
> | `ScrollDown(distance)`          | Scroll down by distance                 |
>
> **Behavior:**
>
> - `ScrollToElement` SHOULD retry up to configurable max attempts
> - SHOULD return false if element not found after max attempts (not throw)

**Analysis:**

| Aspect       | Assessment                         |
| ------------ | ---------------------------------- |
| Implemented? | ✅ MAUI, ⚠️ WPF partial, ❌ HTML |
| In Spec?     | ❌ Not mentioned                   |
| Value        | High - Essential for long lists    |

**Recommendation:** ✅ **ACCEPT** - Common requirement.

**Tasklist:**

- [X] Accept FR-PROP-015: Add scroll support to FR-002 or new section
- [X] Accept FR-PROP-015: Standardize across all platforms
- [ ] Reject FR-PROP-015 (platform-specific detail)

---

### FR-PROP-016: Visual Validation Support

**Current Implementation (WPF only):**

```csharp
// Brinell.Wpf/VisualValidation/
public class ScreenshotCapture { ... }
public class ValidationReport { ... }
```

**Formatted Requirement:**

> **FR-OPT-003.1 (Amendment): Visual Validation Implementation**
>
> **Priority:** MAY
>
> Platforms MAY provide visual validation capabilities:
>
> | Feature                 | Description                             |
> | ----------------------- | --------------------------------------- |
> | Screenshot capture      | Capture element or page screenshots     |
> | Baseline comparison     | Compare against baseline images         |
> | Difference highlighting | Highlight visual differences            |
> | Threshold tolerance     | Configurable pixel difference tolerance |
> | Report generation       | Generate HTML/Markdown reports          |

**Analysis:**

| Aspect       | Assessment                  |
| ------------ | --------------------------- |
| Implemented? | ⚠️ WPF only (partial)     |
| In Spec?     | ✅ FR-OPT-003 (as optional) |
| Value        | Medium - Nice to have       |

**Recommendation:** 🟡 **KEEP AS-IS** - Already optional, WPF has start.

**Tasklist:**

- [X] Accept FR-PROP-016: Expand FR-OPT-003 with implementation details
- [ ] Reject FR-PROP-016 (keep optional and vague)

---

### AD-PROP-010: Platform-Specific Extension Methods

**Current Pattern (Observed):**

Each platform adds platform-specific methods beyond interfaces:

```csharp
// MAUI - Platform-specific methods
public virtual void Swipe(SwipeDirection direction, int distance) { ... }
public virtual void LongPress(int durationMs) { ... }

// WPF - Platform-specific methods  
public AutomationPatterns GetAutomationPatterns() { ... }

// HTML - Platform-specific methods
public string GetAttribute(string name) { ... }
public string GetCssProperty(string name) { ... }
```

**Formatted Architectural Decision:**

> **AD-010: Platform Extension Points**
>
> ### Decision
>
> Platform implementations MAY add methods beyond interface contracts for platform-specific capabilities.
>
> ### Guidelines
>
> 1. Core interfaces define cross-platform contract
> 2. Platform base classes MAY add platform-specific methods
> 3. Platform-specific methods MUST be documented
> 4. Tests using platform-specific methods are not portable
>
> ### Examples
>
> | Platform | Extension Methods                                             |
> | -------- | ------------------------------------------------------------- |
> | MAUI     | `Swipe()`, `LongPress()`, `DoubleTap()`                 |
> | WPF      | `GetAutomationPatterns()`                                   |
> | HTML     | `GetAttribute()`, `GetCssProperty()`, `ExecuteScript()` |

**Recommendation:** ✅ **ACCEPT** - Documents existing pattern.

**Tasklist:**

- [X] Accept AD-PROP-010: Add AD-010 to DES-001
- [ ] Reject AD-PROP-010 (obvious from AD-003)

---

### FR-PROP-017: Expanded Platform Scope

**Current Implementation:**

| Platform          | Project                 | Status         |
| ----------------- | ----------------------- | -------------- |
| WPF               | Brinell.Wpf             | ✅ Implemented |
| MAUI              | Brinell.Maui            | ✅ Implemented |
| HTML/Selenium     | Brinell.Html            | ✅ Implemented |
| HTML/Playwright   | Brinell.Html.Playwright | ✅ Implemented |
| WinForms          | Brinell.WinForms        | ✅ Implemented |
| Stride            | Brinell.Stride          | ✅ Implemented |
| Mocking           | Brinell.Mocking         | ✅ Implemented |
| Testing Utilities | Brinell.Testing         | ✅ Implemented |

**Current Spec (REQ-001 §2):**

```
- Windows desktop applications (WPF)
- Cross-platform desktop and mobile applications (MAUI)
- Web applications (HTML/JavaScript in browsers)
```

**Proposed Update:**

> **FR-001 Scope (Amendment):**
>
> The framework SHALL support automated testing of:
>
> - Windows desktop applications (WPF) - via FlaUI/UIA3
> - Windows Forms applications (WinForms) - via FlaUI/UIA3
> - Cross-platform desktop and mobile applications (MAUI) - via Appium
> - Web applications (HTML) - via Selenium WebDriver
> - Web applications (Blazor) - via Playwright
> - 3D game applications (Stride Engine) - via named pipes
>
> Supporting projects:
>
> - Test utilities (Brinell.Testing) - Database fixtures, test helpers
> - Mocking support (Brinell.Mocking) - API mocking integration

**Recommendation:** ✅ **ACCEPT** - Align spec with implementation.

**Tasklist:**

- [X] Accept FR-PROP-017: Update REQ-001 §2 Scope
- [X] Accept FR-PROP-017: Update FR-001 platform list
- [X] Accept FR-PROP-017: Update SPEC-001 project list

---
