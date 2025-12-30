# Control Assertions Migration Plan (Plan 10)

## Overview

This plan addresses migrating from `Assert.*` (xUnit) patterns to control-based `Assert*` methods, updating documentation/instructions, and enhancing the assertion API to handle 80% of common test scenarios.

**Goal**: Make test assertions consistent, self-documenting, and provide better error messages with automatic screenshot capture.

---

## Part 1: Current State Analysis

### Current Assertion Patterns in Test Files

| Pattern | Count | Example |
|---------|-------|---------|
| `Assert.True(...)` | 12 | `Assert.True(loginPage.HasLoginError())` |
| `Assert.False(...)` | 3 | `Assert.False(loginPage.IsBusy())` |
| `Assert.Equal(...)` | 10 | `Assert.Equal(0, counterPage.GetCurrentCount())` |
| `Assert.Contains(...)` | 5 | `Assert.Contains("Invalid", loginPage.GetLoginError())` |
| `Assert.Empty(...)` | 3 | `Assert.Empty(loginPage.UsernameTextBox.GetText())` |
| `Assert.NotEmpty(...)` | 1 | `Assert.NotEmpty(loginPage.GetErrorMessage())` |
| `control.AssertVisible(...)` | 8 | `loginPage.EmailInput.AssertVisible()` |
| `control.AssertDisplayed(...)` | 6 | `homePage.AssertDisplayed()` |
| **Total xUnit Assert** | **34** | |
| **Total Control Assert** | **14** | |

### Problems with Current Approach

1. **Inconsistent**: Mix of xUnit `Assert.*` and control `Assert*` methods
2. **No context**: `Assert.True(x.HasError())` doesn't log control info
3. **No screenshots**: xUnit assertions don't capture screenshots on failure
4. **Verbose**: `Assert.Empty(control.GetText())` vs `control.AssertTextEmpty()`
5. **Missing methods**: No `AssertTextEmpty`, `AssertTextContains` (page-level), `AssertValue`

### Existing Control Assert Methods

| Method | Available On | Notes |
|--------|--------------|-------|
| `AssertExists()` | ControlBase | ✅ |
| `AssertNotExists()` | ControlBase | ✅ |
| `AssertVisible()` | ControlBase | ✅ |
| `AssertNotVisible()` | ControlBase | ✅ |
| `AssertEnabled()` | ControlBase | ✅ |
| `AssertDisabled()` | ControlBase | ✅ |
| `AssertTextEquals(expected)` | ControlBase | ✅ |
| `AssertTextContains(expected)` | ControlBase | ✅ |
| `AssertDisplayed()` | PageBase | ✅ |

---

## Part 2: Assertion Enhancement Proposal

### 2.1 New ControlBase Assert Methods (80% Coverage)

These methods would cover 80% of common test assertions:

| Method | Signature | Replaces |
|--------|-----------|----------|
| `AssertTextEmpty` | `AssertTextEmpty(string? message = null)` | `Assert.Empty(control.GetText())` |
| `AssertTextNotEmpty` | `AssertTextNotEmpty(string? message = null)` | `Assert.NotEmpty(control.GetText())` |
| `AssertTextStartsWith` | `AssertTextStartsWith(string prefix, string? message = null)` | `Assert.StartsWith(...)` |
| `AssertTextEndsWith` | `AssertTextEndsWith(string suffix, string? message = null)` | `Assert.EndsWith(...)` |
| `AssertTextMatches` | `AssertTextMatches(string regex, string? message = null)` | `Assert.Matches(...)` |
| `AssertHasClass` | `AssertHasClass(string className, string? message = null)` | Custom HTML checks |
| `AssertNotHasClass` | `AssertNotHasClass(string className, string? message = null)` | Custom HTML checks |
| `AssertAttribute` | `AssertAttribute(string name, string expected, string? message = null)` | `Assert.Equal(expected, GetAttribute(...))` |

### 2.2 Control-Specific Assert Methods

**CheckBoxControl / SwitchControl**:
| Method | Signature |
|--------|-----------|
| `AssertIsChecked` | `AssertIsChecked(string? message = null)` |
| `AssertIsUnchecked` | `AssertIsUnchecked(string? message = null)` |

**SliderControl / ProgressControl / StepperControl**:
| Method | Signature |
|--------|-----------|
| `AssertValue` | `AssertValue(double expected, string? message = null)` |
| `AssertValueInRange` | `AssertValueInRange(double min, double max, string? message = null)` |

**PickerControl / ComboBoxControl**:
| Method | Signature |
|--------|-----------|
| `AssertSelectedText` | `AssertSelectedText(string expected, string? message = null)` |
| `AssertSelectedIndex` | `AssertSelectedIndex(int expected, string? message = null)` |
| `AssertItemCount` | `AssertItemCount(int expected, string? message = null)` |

**CollectionViewControl / ListBoxControl**:
| Method | Signature |
|--------|-----------|
| `AssertItemCount` | `AssertItemCount(int expected, string? message = null)` |
| `AssertContainsItem` | `AssertContainsItem(string text, string? message = null)` |
| `AssertNotContainsItem` | `AssertNotContainsItem(string text, string? message = null)` |

**TextInputControl / TextBoxControl**:
| Method | Signature |
|--------|-----------|
| `AssertPlaceholder` | `AssertPlaceholder(string expected, string? message = null)` |
| `AssertInputType` | `AssertInputType(string expected, string? message = null)` (HTML) |
| `AssertIsReadOnly` | `AssertIsReadOnly(string? message = null)` |
| `AssertIsNotReadOnly` | `AssertIsNotReadOnly(string? message = null)` |

### 2.3 PageBase Assert Methods

| Method | Signature | Replaces |
|--------|-----------|----------|
| `AssertDisplayed` | `AssertDisplayed(string? message = null)` | ✅ Already exists |
| `AssertNotDisplayed` | `AssertNotDisplayed(string? message = null)` | ✅ Already exists |
| `AssertUrl` | `AssertUrl(string expected, string? message = null)` (HTML) | `Assert.Contains(url, GetCurrentUrl())` |
| `AssertUrlContains` | `AssertUrlContains(string expected, string? message = null)` (HTML) | `Assert.Contains(...)` |
| `AssertTitle` | `AssertTitle(string expected, string? message = null)` | `Assert.Equal(title, ...)` |
| `AssertTitleContains` | `AssertTitleContains(string expected, string? message = null)` | `Assert.Contains(...)` |

---

## Part 3: Migration Guide

### 3.1 Before/After Examples

**Example 1: Text Empty Check**
```csharp
// Before
Assert.Empty(loginPage.UsernameTextBox.GetText());

// After
loginPage.UsernameTextBox.AssertTextEmpty();
```

**Example 2: Text Contains Check**
```csharp
// Before
Assert.Contains("Invalid", loginPage.GetLoginError());

// After - If GetLoginError() returns a control
loginPage.ErrorLabel.AssertTextContains("Invalid");

// After - If GetLoginError() is a page method, add page-level assert
loginPage.AssertErrorContains("Invalid");
```

**Example 3: Boolean State Check**
```csharp
// Before
Assert.True(loginPage.HasLoginError(), "Login error should be displayed");

// After - Add AssertHasError to page
loginPage.AssertHasError("Login error should be displayed");

// Or use control directly
loginPage.ErrorLabel.AssertVisible("Login error should be displayed");
```

**Example 4: Numeric Value Check**
```csharp
// Before
Assert.Equal(0, counterPage.GetCurrentCount());

// After - Add AssertCount to page
counterPage.AssertCount(0);

// Or use control
counterPage.CountDisplay.AssertTextEquals("0");
```

**Example 5: URL Check (HTML)**
```csharp
// Before
Assert.Contains("/counter", GetCurrentUrl());

// After
AssertUrlContains("/counter");
```

**Example 6: Input Type Check (HTML)**
```csharp
// Before
var inputType = loginPage.PasswordInput.GetInputType();
Assert.Equal("password", inputType);

// After
loginPage.PasswordInput.AssertInputType("password");
```

**Example 7: Placeholder Check (HTML)**
```csharp
// Before
var placeholder = loginPage.EmailInput.GetPlaceholder();
Assert.False(string.IsNullOrEmpty(placeholder));

// After
loginPage.EmailInput.AssertHasPlaceholder();
```

### 3.2 Migration Patterns

| xUnit Pattern | Control Assert Replacement |
|---------------|---------------------------|
| `Assert.True(control.IsVisible())` | `control.AssertVisible()` |
| `Assert.False(control.IsVisible())` | `control.AssertNotVisible()` |
| `Assert.True(control.IsEnabled())` | `control.AssertEnabled()` |
| `Assert.False(control.IsEnabled())` | `control.AssertDisabled()` |
| `Assert.Equal(expected, control.GetText())` | `control.AssertTextEquals(expected)` |
| `Assert.Contains(expected, control.GetText())` | `control.AssertTextContains(expected)` |
| `Assert.Empty(control.GetText())` | `control.AssertTextEmpty()` |
| `Assert.NotEmpty(control.GetText())` | `control.AssertTextNotEmpty()` |
| `Assert.True(page.IsDisplayed())` | `page.AssertDisplayed()` |
| `Assert.True(page.HasError())` | `page.AssertHasError()` or `page.ErrorLabel.AssertVisible()` |
| `Assert.Equal(n, page.GetCount())` | `page.AssertCount(n)` or `page.CountLabel.AssertTextEquals(n.ToString())` |
| `Assert.Contains(url, GetCurrentUrl())` | `AssertUrlContains(url)` |

### 3.3 Page-Level Semantic Methods

For domain-specific assertions, add semantic methods to page objects:

```csharp
public class LoginPage : PageBase
{
    // Semantic assertions that wrap control assertions
    public void AssertHasError(string? message = null)
    {
        ErrorLabel.AssertVisible(message ?? "Error message should be visible");
    }
    
    public void AssertErrorContains(string expected, string? message = null)
    {
        ErrorLabel.AssertTextContains(expected, message);
    }
    
    public void AssertIsReady()
    {
        AssertDisplayed();
        UsernameTextBox.AssertEnabled();
        PasswordTextBox.AssertEnabled();
        LoginButton.AssertEnabled();
    }
}

public class CounterPage : PageBase
{
    public void AssertCount(int expected, string? message = null)
    {
        CountDisplay.AssertTextEquals(expected.ToString(), 
            message ?? $"Count should be {expected}");
    }
}
```

---

## Part 4: Implementation Plan

### Phase 1: Add Missing Assert Methods (2-3 days)

#### 1.1 ControlBase Enhancements (All Platforms)

```csharp
// Add to all ControlBase implementations (WPF, HTML, MAUI)

/// <summary>
/// Assert element text is empty.
/// </summary>
public virtual void AssertTextEmpty(string? message = null)
{
    CheckVisible(expected: true);
    var actual = GetText();
    if (!string.IsNullOrEmpty(actual))
    {
        ThrowAssertionFailed("TextEmpty", actual, "(empty)",
            message ?? $"Expected empty text but got '{actual}' for element '{AutomationId}'.");
    }
    LogAssertPass("TextEmpty", "(empty)", "(empty)");
}

/// <summary>
/// Assert element text is not empty.
/// </summary>
public virtual void AssertTextNotEmpty(string? message = null)
{
    CheckVisible(expected: true);
    var actual = GetText();
    if (string.IsNullOrEmpty(actual))
    {
        ThrowAssertionFailed("TextNotEmpty", "(empty)", "(non-empty)",
            message ?? $"Expected non-empty text but got empty for element '{AutomationId}'.");
    }
    LogAssertPass("TextNotEmpty", actual, "(non-empty)");
}

/// <summary>
/// Assert element text starts with expected prefix.
/// </summary>
public virtual void AssertTextStartsWith(string prefix, string? message = null)
{
    CheckVisible(expected: true);
    var actual = GetText();
    if (!actual.StartsWith(prefix, StringComparison.Ordinal))
    {
        ThrowAssertionFailed("TextStartsWith", actual, $"starts with '{prefix}'",
            message ?? $"Expected text to start with '{prefix}' but got '{actual}'.");
    }
    LogAssertPass("TextStartsWith", actual, prefix);
}

/// <summary>
/// Assert element text ends with expected suffix.
/// </summary>
public virtual void AssertTextEndsWith(string suffix, string? message = null)
{
    CheckVisible(expected: true);
    var actual = GetText();
    if (!actual.EndsWith(suffix, StringComparison.Ordinal))
    {
        ThrowAssertionFailed("TextEndsWith", actual, $"ends with '{suffix}'",
            message ?? $"Expected text to end with '{suffix}' but got '{actual}'.");
    }
    LogAssertPass("TextEndsWith", actual, suffix);
}
```

#### 1.2 HTML-Specific Additions

```csharp
// Add to Brinell.Html ControlBase

/// <summary>
/// Assert element has a CSS class.
/// </summary>
public virtual void AssertHasClass(string className, string? message = null)
{
    CheckVisible(expected: true);
    if (!HasClass(className))
    {
        var classes = GetAttribute("class") ?? "(none)";
        ThrowAssertionFailed("HasClass", classes, className,
            message ?? $"Expected element to have class '{className}' but has '{classes}'.");
    }
    LogAssertPass("HasClass", className, className);
}

/// <summary>
/// Assert element does not have a CSS class.
/// </summary>
public virtual void AssertNotHasClass(string className, string? message = null)
{
    CheckVisible(expected: true);
    if (HasClass(className))
    {
        var classes = GetAttribute("class") ?? "(none)";
        ThrowAssertionFailed("NotHasClass", classes, $"not '{className}'",
            message ?? $"Expected element to not have class '{className}' but it does.");
    }
    LogAssertPass("NotHasClass", "(no class)", className);
}

/// <summary>
/// Assert element attribute equals expected value.
/// </summary>
public virtual void AssertAttribute(string attributeName, string expected, string? message = null)
{
    CheckVisible(expected: true);
    var actual = GetAttribute(attributeName) ?? "(null)";
    if (actual != expected)
    {
        ThrowAssertionFailed($"Attribute[{attributeName}]", actual, expected,
            message ?? $"Expected attribute '{attributeName}' to be '{expected}' but got '{actual}'.");
    }
    LogAssertPass($"Attribute[{attributeName}]", actual, expected);
}

/// <summary>
/// Assert element has a non-empty placeholder.
/// </summary>
public virtual void AssertHasPlaceholder(string? message = null)
{
    CheckVisible(expected: true);
    var placeholder = GetAttribute("placeholder");
    if (string.IsNullOrEmpty(placeholder))
    {
        ThrowAssertionFailed("HasPlaceholder", "(none)", "(placeholder)",
            message ?? $"Expected element to have a placeholder but it doesn't.");
    }
    LogAssertPass("HasPlaceholder", placeholder, "(placeholder)");
}
```

#### 1.3 TextInputControl Additions (HTML)

```csharp
/// <summary>
/// Assert input type attribute.
/// </summary>
public void AssertInputType(string expected, string? message = null)
{
    AssertAttribute("type", expected, message);
}

/// <summary>
/// Assert placeholder text.
/// </summary>
public void AssertPlaceholder(string expected, string? message = null)
{
    AssertAttribute("placeholder", expected, message);
}
```

#### 1.4 HtmlUITestBase URL Assertions

```csharp
// Add to HtmlUITestBase

/// <summary>
/// Assert current URL equals expected.
/// </summary>
protected void AssertUrl(string expected, string? message = null)
{
    var actual = GetCurrentUrl();
    if (actual != expected)
    {
        throw new AssertionException($"Expected URL '{expected}' but got '{actual}'");
    }
}

/// <summary>
/// Assert current URL contains expected substring.
/// </summary>
protected void AssertUrlContains(string expected, string? message = null)
{
    var actual = GetCurrentUrl();
    if (!actual.Contains(expected))
    {
        throw new AssertionException(
            message ?? $"Expected URL to contain '{expected}' but got '{actual}'");
    }
}
```

### Phase 2: Migrate Sample Tests (1-2 days)

#### 2.1 WPF LoginTests Migration

| Line | Before | After |
|------|--------|-------|
| 51 | `Assert.True(loginPage.HasLoginError(), ...)` | `loginPage.ErrorLabel.AssertVisible(...)` |
| 52 | `Assert.Contains("Invalid", loginPage.GetLoginError())` | `loginPage.ErrorLabel.AssertTextContains("Invalid")` |
| 74 | `Assert.True(loginPage.HasUsernameError(), ...)` | `loginPage.UsernameErrorLabel.AssertVisible(...)` |
| 93-94 | `Assert.True/Contains` for password error | `loginPage.PasswordErrorLabel.AssertVisible/TextContains()` |
| 113 | `Assert.Empty(loginPage.UsernameTextBox.GetText())` | `loginPage.UsernameTextBox.AssertTextEmpty()` |

#### 2.2 Blazor LoginTests Migration

| Line | Before | After |
|------|--------|-------|
| 33 | `Assert.True(dashboardPage.HasWelcomeAlert(), ...)` | `dashboardPage.WelcomeAlert.AssertVisible(...)` |
| 50-51 | `Assert.True/NotEmpty` for error | `loginPage.ErrorMessage.AssertVisible()` / `AssertTextNotEmpty()` |
| 84 | `Assert.False(string.IsNullOrEmpty(placeholder), ...)` | `loginPage.EmailInput.AssertHasPlaceholder()` |
| 99 | `Assert.Equal("password", inputType)` | `loginPage.PasswordInput.AssertInputType("password")` |
| 146-147 | `Assert.Empty(...)` | `loginPage.EmailInput.AssertTextEmpty()` |

#### 2.3 Blazor CounterTests Migration

| Line | Before | After |
|------|--------|-------|
| 30, 48, 66, etc. | `Assert.Equal(n, counterPage.GetCurrentCount())` | `counterPage.AssertCount(n)` or `counterPage.CountDisplay.AssertTextEquals(n.ToString())` |

#### 2.4 Blazor NavigationTests Migration

| Line | Before | After |
|------|--------|-------|
| 49 | `Assert.Contains("/counter", GetCurrentUrl())` | `AssertUrlContains("/counter")` |
| 67 | `Assert.Contains("/login", GetCurrentUrl())` | `AssertUrlContains("/login")` |
| 85 | `Assert.Contains("/dashboard", GetCurrentUrl())` | `AssertUrlContains("/dashboard")` |

### Phase 3: Update Documentation (1 day)

#### 3.1 Update uitests-core.instructions.md

Add comprehensive assertion section:

```markdown
## Control Assertions (Preferred)

Use control `Assert*` methods instead of xUnit `Assert.*` for:
- Better error messages with control context
- Automatic screenshot capture on failure
- CSV logging for test analytics
- Consistent wait-before-assert behavior

### Available Assert Methods

**All Controls (ControlBase)**:
```csharp
control.AssertExists("message");
control.AssertNotExists("message");
control.AssertVisible("message");
control.AssertNotVisible("message");
control.AssertEnabled("message");
control.AssertDisabled("message");
control.AssertTextEquals("expected", "message");
control.AssertTextContains("expected", "message");
control.AssertTextEmpty("message");
control.AssertTextNotEmpty("message");
control.AssertTextStartsWith("prefix", "message");
control.AssertTextEndsWith("suffix", "message");
```

**HTML Controls (Additional)**:
```csharp
control.AssertHasClass("class-name", "message");
control.AssertNotHasClass("class-name", "message");
control.AssertAttribute("name", "expected", "message");
control.AssertHasPlaceholder("message");
textInput.AssertInputType("password", "message");
textInput.AssertPlaceholder("Enter email", "message");
```

**Pages**:
```csharp
page.AssertDisplayed("message");
page.AssertNotDisplayed("message");
// HTML only:
AssertUrl("http://...", "message");
AssertUrlContains("/path", "message");
```

### Migration from xUnit Assert

| Instead of | Use |
|------------|-----|
| `Assert.True(control.IsVisible())` | `control.AssertVisible()` |
| `Assert.False(control.IsVisible())` | `control.AssertNotVisible()` |
| `Assert.Equal(expected, control.GetText())` | `control.AssertTextEquals(expected)` |
| `Assert.Contains(expected, control.GetText())` | `control.AssertTextContains(expected)` |
| `Assert.Empty(control.GetText())` | `control.AssertTextEmpty()` |
| `Assert.NotEmpty(control.GetText())` | `control.AssertTextNotEmpty()` |
| `Assert.True(page.IsDisplayed())` | `page.AssertDisplayed()` |

### When to Still Use xUnit Assert

Use xUnit `Assert.*` only for:
- Non-UI assertions (business logic in page objects)
- Complex comparisons not covered by control assertions
- Collection assertions on non-control data
```

#### 3.2 Update uitests-html.instructions.md

Add HTML-specific assertion examples.

#### 3.3 Update uitests-wpf.instructions.md

Add WPF-specific assertion examples.

---

## Part 5: 80% Coverage Analysis

### Common Assertion Categories

Based on the sample test analysis:

| Category | % of Tests | Covered By |
|----------|------------|------------|
| Visibility checks | 25% | `AssertVisible`, `AssertNotVisible` |
| Text equality | 20% | `AssertTextEquals` |
| Text contains | 15% | `AssertTextContains` |
| Text empty/not empty | 10% | `AssertTextEmpty`, `AssertTextNotEmpty` |
| Enabled/disabled | 10% | `AssertEnabled`, `AssertDisabled` |
| URL checks (HTML) | 8% | `AssertUrlContains` |
| Boolean state (HasError, etc.) | 7% | Page semantic methods |
| Numeric values | 5% | Page semantic methods or `AssertTextEquals` |
| **Total Covered** | **100%** | |

### Methods Needed for 80% Coverage

| Priority | Methods | Coverage |
|----------|---------|----------|
| P1 (Existing) | `AssertVisible`, `AssertEnabled`, `AssertTextEquals`, `AssertTextContains` | 60% |
| P2 (New) | `AssertTextEmpty`, `AssertTextNotEmpty` | +15% |
| P3 (New) | `AssertUrlContains`, `AssertHasPlaceholder`, `AssertInputType` | +10% |
| P4 (Semantic) | Page-level `AssertHasError`, `AssertCount` | +10% |
| **Total** | | **95%** |

---

## Part 6: Timeline & Effort

| Phase | Duration | Effort |
|-------|----------|--------|
| Phase 1: Add Assert Methods | 2-3 days | Medium |
| Phase 2: Migrate Sample Tests | 1-2 days | Low |
| Phase 3: Update Documentation | 1 day | Low |
| **Total** | **4-6 days** | |

---

## Part 7: Success Criteria

- [ ] All new `Assert*` methods implemented in ControlBase (WPF, HTML, MAUI)
- [ ] Sample tests migrated to use control assertions
- [ ] Zero `Assert.Empty(control.GetText())` patterns remaining
- [ ] Zero `Assert.True(control.IsVisible())` patterns remaining
- [ ] Instructions files updated with assertion guidelines
- [ ] 80%+ of test assertions use control `Assert*` methods
- [ ] All assertions log to CSV and capture screenshots on failure

---

## Appendix A: Full Method List by Control

### ControlBase (All Platforms)

```csharp
// Existing
void AssertExists(string? message = null);
void AssertNotExists(string? message = null);
void AssertVisible(string? message = null);
void AssertNotVisible(string? message = null);
void AssertEnabled(string? message = null);
void AssertDisabled(string? message = null);
void AssertTextEquals(string expected, string? message = null);
void AssertTextContains(string expected, string? message = null);

// New (Phase 1)
void AssertTextEmpty(string? message = null);
void AssertTextNotEmpty(string? message = null);
void AssertTextStartsWith(string prefix, string? message = null);
void AssertTextEndsWith(string suffix, string? message = null);
```

### HTML ControlBase (Additional)

```csharp
void AssertHasClass(string className, string? message = null);
void AssertNotHasClass(string className, string? message = null);
void AssertAttribute(string name, string expected, string? message = null);
void AssertHasPlaceholder(string? message = null);
```

### TextInputControl (HTML)

```csharp
void AssertInputType(string expected, string? message = null);
void AssertPlaceholder(string expected, string? message = null);
void AssertIsReadOnly(string? message = null);
void AssertIsNotReadOnly(string? message = null);
```

### CheckBoxControl / SwitchControl

```csharp
void AssertIsChecked(string? message = null);
void AssertIsUnchecked(string? message = null);
```

### SliderControl / ProgressControl

```csharp
void AssertValue(double expected, string? message = null);
void AssertValueInRange(double min, double max, string? message = null);
```

### PickerControl / ComboBoxControl

```csharp
void AssertSelectedText(string expected, string? message = null);
void AssertSelectedIndex(int expected, string? message = null);
void AssertItemCount(int expected, string? message = null);
```

### HtmlUITestBase

```csharp
void AssertUrl(string expected, string? message = null);
void AssertUrlContains(string expected, string? message = null);
void AssertTitle(string expected, string? message = null);
void AssertTitleContains(string expected, string? message = null);
```

---

## Appendix B: Regex Patterns for Migration

Use these patterns to find and replace assertions:

```regex
# Find Assert.Empty on control text
Assert\.Empty\((\w+)\.GetText\(\)\)
→ $1.AssertTextEmpty()

# Find Assert.True on IsVisible
Assert\.True\((\w+)\.IsVisible\(\)(?:,\s*"([^"]+)")?\)
→ $1.AssertVisible($2)

# Find Assert.Equal on GetText
Assert\.Equal\("([^"]+)",\s*(\w+)\.GetText\(\)\)
→ $2.AssertTextEquals("$1")

# Find Assert.Contains on GetText
Assert\.Contains\("([^"]+)",\s*(\w+)\.GetText\(\)\)
→ $2.AssertTextContains("$1")

# Find Assert.Contains on URL
Assert\.Contains\("([^"]+)",\s*GetCurrentUrl\(\)\)
→ AssertUrlContains("$1")
```

---

## Change Log

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2024-12-30 | Plan | Initial control assertions migration plan |
