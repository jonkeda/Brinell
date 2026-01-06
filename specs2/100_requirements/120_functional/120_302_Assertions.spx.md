# functional Assertions
- **id**: FR-302
- **title**: Assertion Methods
- **priority**: high
- **status**: draft
- **category**: State and Verification

The framework must provide assertion methods for test verification that integrate with logging and error handling.

## capabilities

### AssertionMethodPattern
- **id**: FR-302.1
- **title**: Assertion method pattern

All assertion methods must follow this pattern:

1. **Log attempt** - Record that assertion is being made
2. **Check precondition** - Verify element is in testable state
3. **Get actual value** - Retrieve current value
4. **Compare values** - Compare expected with actual
5. **Log result** - Record success or failure
6. **Throw on failure** - Raise AssertionException with details

### StateAssertions
- **id**: FR-302.2
- **title**: State assertion methods

Controls must provide state assertions:

| Method | Verifies |
|--------|----------|
| AssertExists | Element exists in UI tree |
| AssertNotExists | Element does not exist |
| AssertVisible | Element is visible |
| AssertNotVisible | Element is not visible |
| AssertEnabled | Element is enabled |
| AssertDisabled | Element is disabled |
| AssertClickable | Element is clickable (visible and enabled) |

### ValueAssertions
- **id**: FR-302.3
- **title**: Value assertion methods

Controls must provide value assertions:

| Method | Verifies |
|--------|----------|
| AssertText | Text equals expected |
| AssertTextContains | Text contains substring |
| AssertTextMatches | Text matches pattern |
| AssertValue | Value equals expected |
| AssertAttribute | Attribute equals expected |

### CollectionAssertions
- **id**: FR-302.4
- **title**: Collection assertion methods

Collection controls must provide:

| Method | Verifies |
|--------|----------|
| AssertItemCount | Item count equals expected |
| AssertContainsItem | Collection contains item |
| AssertNotContainsItem | Collection does not contain item |
| AssertEmpty | Collection has no items |
| AssertNotEmpty | Collection has items |

### ToggleAssertions
- **id**: FR-302.5
- **title**: Toggle assertion methods

Toggle controls must provide:

| Method | Verifies |
|--------|----------|
| AssertChecked | Control is checked/on |
| AssertUnchecked | Control is unchecked/off |

### SelectionAssertions
- **id**: FR-302.6
- **title**: Selection assertion methods

Selector controls must provide:

| Method | Verifies |
|--------|----------|
| AssertSelected | Specific item is selected |
| AssertSelectedIndex | Item at index is selected |
| AssertNoSelection | No item is selected |

### AssertionMessages
- **id**: FR-302.7
- **title**: Assertion message parameter

All assertion methods must accept an optional message parameter:
- Included in exception if assertion fails
- Included in log output
- Provides test-specific context

When omitted, framework provides default message.

### NullExpectedValue
- **id**: FR-302.8
- **title**: Null expected value handling

When expected value is null:
- Assertion is skipped (no action)
- No logging occurs
- Method returns immediately

This enables conditional assertions:
```
// Pseudocode - only asserts if expectedValue is not null
control.AssertText(expectedValue)
```

### AssertionExceptionContent
- **id**: FR-302.9
- **title**: Assertion exception content

When assertion fails, exception must include:
- Control identifier (locator)
- Expected value
- Actual value
- Custom message (if provided)
- Page context
- Timestamp
- Screenshot path (if captured)

---

## relationships

- Implements patterns from [FR-300 State Verification](120_300_StateVerification.spx.md)
- Throws exceptions per [FR-600 Exception Strategy](120_600_ExceptionStrategy.spx.md)
- Logs per [FR-500 Logging](120_500_Logging.spx.md)
- Screenshots per [FR-502 Screenshot Evidence](120_502_ScreenshotEvidence.spx.md)

---

## constraints

- Assertions must log before throwing
- Assertions must not modify element state
- Assertions must use Check* internally for preconditions
- Assertion messages must be human-readable
