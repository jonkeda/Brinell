# 221_003 Exception Handling

## foundation ExceptionHandling

- **title**: Framework Exception Types and Error Reporting
- **package**: Brinell.Core.Exceptions
- **purpose**: Consistent exception hierarchy with actionable error messages

---

## Description

The Exception Handling foundation defines a hierarchy of exception types that provide clear, actionable error messages when test operations fail. All exceptions include contextual information (AutomationId, operation, state) to facilitate debugging.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Exception Hierarchy

```
System.Exception
├── ElementNotFoundException     # Element not found in UI tree
├── UITestTimeoutException       # Operation timed out
├── AssertionException           # Assert* method failed (includes waiting)
├── InvalidStateException        # Control in wrong state
├── PageNotDisplayedException    # Expected page not visible
└── PageNotReadyException        # Page not fully loaded
```

---

## 2. Exception Types

### 2.1 ElementNotFoundException

Thrown when a UI element cannot be located.

```csharp
public class ElementNotFoundException : Exception
{
    public ElementNotFoundException(string message)
        : base(message) { }
    
    public ElementNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Usage Context:**
- Element search by AutomationId fails
- Locator does not match any element
- Scoped search within container finds nothing

**Example Message:**
```
Element 'LoginButton' not found within 5000ms timeout.
Searched by: AutomationId='LoginButton'
Container: LoginPage
Suggestions:
- Verify the AutomationId is correctly set in the application
- Check if the element is visible and enabled
- Consider increasing the timeout if the element loads slowly
```

### 2.2 UITestTimeoutException

Thrown when an operation exceeds its timeout.

```csharp
public class UITestTimeoutException : Exception
{
    public string? AutomationId { get; }
    public int TimeoutMs { get; }
    public string? Operation { get; }
    public string? CurrentState { get; }
    
    public UITestTimeoutException(string message) 
        : base(message) { }
    
    public UITestTimeoutException(
        string message, 
        string automationId, 
        int timeoutMs, 
        string? operation = null,
        string? currentState = null)
        : base(FormatMessage(message, automationId, timeoutMs, operation, currentState))
    {
        AutomationId = automationId;
        TimeoutMs = timeoutMs;
        Operation = operation;
        CurrentState = currentState;
    }
    
    public UITestTimeoutException(string message, Exception innerException)
        : base(message, innerException) { }
    
    private static string FormatMessage(
        string message, 
        string automationId, 
        int timeoutMs, 
        string? operation, 
        string? currentState)
    {
        var formatted = $"{message} [AutomationId: {automationId}, Timeout: {timeoutMs}ms";
        if (!string.IsNullOrEmpty(operation))
            formatted += $", Operation: {operation}";
        if (!string.IsNullOrEmpty(currentState))
            formatted += $", CurrentState: {currentState}";
        formatted += "]";
        return formatted;
    }
}
```

**Usage Context:**
- WaitVisible, WaitExists, WaitEnabled timeout
- Element find with timeout fails
- Page load timeout

### 2.3 AssertionException

Thrown when an Assert* method fails (after waiting for the condition).

```csharp
public class AssertionException : Exception
{
    public string? AutomationId { get; }
    public string? AssertionType { get; }
    
    public AssertionException(string message) 
        : base(message) { }
    
    public AssertionException(string message, Exception inner) 
        : base(message, inner) { }
    
    public AssertionException(string message, string? automationId, string? assertionType = null) 
        : base(message)
    {
        AutomationId = automationId;
        AssertionType = assertionType;
    }
}
```

**Usage Context:**
- AssertTextEquals finds mismatched text after waiting
- AssertExists finds element still missing after timeout
- AssertVisible finds element still hidden after timeout
- AssertEnabled finds element still disabled after timeout

> **Note:** Assert methods now include waiting (consolidating the previous Wait+Check patterns). They wait for the expected condition and throw `AssertionException` if the timeout is reached without the condition being met.

### 2.4 InvalidStateException

Thrown when a control is in an invalid state for the requested operation.

```csharp
public class InvalidStateException : Exception
{
    public string? AutomationId { get; }
    public string? CurrentState { get; }
    public string? ExpectedState { get; }
    public string? Operation { get; }
    
    public InvalidStateException(string message) 
        : base(message) { }
    
    public InvalidStateException(
        string message, 
        string automationId, 
        string currentState, 
        string expectedState,
        string? operation = null)
        : base(FormatMessage(message, automationId, currentState, expectedState, operation))
    {
        AutomationId = automationId;
        CurrentState = currentState;
        ExpectedState = expectedState;
        Operation = operation;
    }
    
    private static string FormatMessage(
        string message, 
        string automationId, 
        string currentState, 
        string expectedState,
        string? operation)
    {
        var formatted = $"{message} [AutomationId: {automationId}, Current: {currentState}, Expected: {expectedState}";
        if (!string.IsNullOrEmpty(operation))
            formatted += $", Operation: {operation}";
        formatted += "]";
        return formatted;
    }
}
```

**Usage Context:**
- Click on disabled button
- Enter text in readonly field
- Select item from empty picker

### 2.5 PageNotDisplayedException

Thrown when an expected page is not visible.

```csharp
public class PageNotDisplayedException : Exception
{
    public string? PageName { get; }
    
    public PageNotDisplayedException(string message) 
        : base(message) { }
    
    public PageNotDisplayedException(string message, string pageName) 
        : base($"{message} [Page: {pageName}]")
    {
        PageName = pageName;
    }
}
```

**Usage Context:**
- Navigation expected to reach page but didn't
- Page verification after action fails

### 2.6 PageNotReadyException

Thrown when a page is visible but not fully loaded.

```csharp
public class PageNotReadyException : Exception
{
    public string? PageName { get; }
    public string? MissingElement { get; }
    
    public PageNotReadyException(string message) 
        : base(message) { }
    
    public PageNotReadyException(string message, string pageName, string? missingElement = null) 
        : base($"{message} [Page: {pageName}, Missing: {missingElement ?? "unknown"}]")
    {
        PageName = pageName;
        MissingElement = missingElement;
    }
}
```

**Usage Context:**
- WaitForPage() times out
- Expected control not ready after page appears

---

## 3. Exception Categories

| Category | Exception | Thrown By | Meaning |
|----------|-----------|-----------|---------|
| **Element** | ElementNotFoundException | FindElement | Element not in UI tree |
| **Timeout** | UITestTimeoutException | Wait*, internal | Operation exceeded time limit |
| **Assertion** | AssertionException | Assert* | Condition not met after waiting |
| **State** | InvalidStateException | Click, Enter, etc. | Control in wrong state |
| **Page** | PageNotDisplayedException | Page verification | Expected page not shown |
| **Page** | PageNotReadyException | WaitForPage | Page not fully loaded |

---

## 4. Error Message Guidelines

### 4.1 Message Structure

All error messages should include:

1. **What failed** - Clear description of the failure
2. **Context** - AutomationId, page name, operation
3. **Values** - Expected vs actual (for assertions)
4. **Suggestions** - Actionable debugging hints

### 4.2 Good Error Message Example

```
Assertion failed: Text does not match expected value.
Control: WelcomeLabel (AutomationId='WelcomeLabel')
Page: HomePage
Expected: "Hello, John"
Actual: "Hello, Jane"

Suggestions:
- Verify the logged-in user is correct
- Check if the label updates asynchronously (use WaitText instead)
- Review test data setup
```

### 4.3 Bad Error Message Example

```
Assertion failed.
```

---

## 5. Exception Handling Patterns

### 5.1 Control Method Pattern

```csharp
public void Click(int? timeoutMs = null)
{
    var element = FindElement();
    
    if (!element.Enabled)
    {
        throw new InvalidStateException(
            "Cannot click disabled element",
            AutomationId,
            "Disabled",
            "Enabled",
            "Click");
    }
    
    try
    {
        element.Click();
    }
    catch (StaleElementReferenceException ex)
    {
        throw new ElementNotFoundException(
            $"Element '{AutomationId}' became stale during click operation",
            ex);
    }
}
```

### 5.2 Wait Method Pattern

```csharp
public bool WaitVisible(bool? visible, int? timeoutMs = null)
{
    if (visible == null) return true;  // Nullable skip pattern
    
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    var stopwatch = Stopwatch.StartNew();
    
    while (stopwatch.ElapsedMilliseconds < timeout)
    {
        if (IsVisible() == visible)
            return true;
        Thread.Sleep(PollingIntervalMs);
    }
    
    // Return false for Wait methods (don't throw)
    return false;
}

public void AssertVisible(bool? visible, string? message = null, int? timeoutMs = null)
{
    if (visible == null) return;  // Nullable skip pattern
    
    if (!WaitVisible(visible, timeoutMs))
    {
        throw new AssertionException(
            message ?? $"Element '{Locator}' visibility did not become {visible}",
            Locator.Value,
            "AssertVisible");
    }
}
```

### 5.3 Assert Method Pattern

```csharp
public void AssertTextEquals(string? expected, string? message = null, int? timeoutMs = null)
{
    if (expected == null) return;  // Nullable skip pattern
    
    // Wait for text to match, then assert
    if (!WaitText(expected, timeoutMs))
    {
        var actual = GetText();
        throw new AssertionException(
            message ?? $"Expected text '{expected}' but found '{actual}'",
            Locator.Value,
            "AssertTextEquals");
    }
}
```

---

## 6. Exception Logging Integration

Exceptions should be logged when caught at boundary points:

```csharp
public void PerformAction(Action action, string actionName)
{
    try
    {
        action();
        _logger.LogAction(_testName, _page.Name, AutomationId, actionName);
    }
    catch (Exception ex)
    {
        _logger.LogError(_testName, _page.Name, AutomationId, actionName, ex);
        throw;
    }
}
```

---

## 7. Rethrowing and Wrapping

### 7.1 Preserving Original Exception

```csharp
catch (NoSuchElementException ex)
{
    // Wrap platform exception in framework exception
    throw new ElementNotFoundException(
        $"Element '{AutomationId}' not found",
        ex);  // Preserve inner exception
}
```

### 7.2 Adding Context

```csharp
catch (ElementNotFoundException ex)
{
    // Add page context and rethrow
    throw new ElementNotFoundException(
        $"{ex.Message} on page '{_page.Name}'",
        ex);
}
```

---

## 8. Validation Rules

The Exception Handling foundation is valid when:

- [ ] All framework exceptions inherit from System.Exception
- [ ] Exceptions include AutomationId/Locator property where applicable
- [ ] Error messages are actionable (what, context, suggestion)
- [ ] Assert* methods wait then throw AssertionException on timeout
- [ ] Wait* methods return bool (don't throw on timeout)
- [ ] Platform exceptions are wrapped in framework exceptions
- [ ] Inner exceptions are preserved for debugging

---

## Related Documents

- [221_001 Logging](221_001_Logging.spx.md)
- [133_002 Error Messages](../../100_requirements/133_usability/133_002_ErrorMessages.spx.md)
- [211_001 Interfaces](../211_Modules/211_001_Interfaces.spx.md)
