# 133_002 Error Messages

## usability ErrorMessages

- **title**: Actionable and Consistent Error Messages
- **requirement**: Error messages clearly indicate problems and suggest solutions
- **priority**: high

---

## Description

This requirement ensures that when tests fail, developers get clear, actionable error messages that help them quickly identify and fix issues.

---

## Sub-Requirements

### NFR-USE-002.1: Actionable Messages

- Error messages MUST clearly indicate what went wrong
- Error messages MUST include relevant context (element ID, timeout, etc.)
- Error messages SHOULD suggest potential solutions

### NFR-USE-002.2: Error Message Format

- Error messages MUST be consistent across platforms
- Error messages MUST include stack traces for debugging
- Error messages MUST distinguish between framework errors and application errors

---

## Acceptance Criteria

- All assertion failures include element identification
- Timeout errors include configured and actual timeout values
- Messages reviewed for consistency across platforms

---

## Error Message Examples

### Good Error Messages

```
Element 'LoginButton' not found within 5000ms timeout.
Searched by: AutomationId='LoginButton'
Container: LoginPage
Suggestions:
- Verify the AutomationId is correctly set in the application
- Check if the element is visible and enabled
- Consider increasing the timeout if the element loads slowly
```

```
Assertion failed: Text does not match expected value.
Control: UsernameLabel (AutomationId='UsernameLabel')
Expected: "Hello, John"
Actual: "Hello, Jane"
```

### Bad Error Messages

```
Element not found.
```

```
Assertion failed.
```

---

## Message Categories

| Category | Prefix | Example |
|----------|--------|---------|
| Element Not Found | ElementNotFoundException | Control 'X' not found |
| Timeout | TimeoutException | Operation timed out after Xms |
| Assertion | AssertionException | Expected X but got Y |
| Framework | BrinellException | Framework configuration error |
| Application | ApplicationException | Application crashed or unresponsive |

---

## Related

- [FR-010 Error Handling](../120_functional/120_010_ErrorHandling.spx.md)
- [G-006 Debug Friendly](../110_goal/110_006_DebugFriendly.spx.md)
- [NFR-USE-003 Debugging Support](133_003_DebuggingSupport.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-USE-002
