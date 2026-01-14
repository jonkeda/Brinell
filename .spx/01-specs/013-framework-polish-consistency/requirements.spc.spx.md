# Requirements Document: Framework Polish & Consistency

## Introduction

This specification addresses three consistency and architecture issues in the Brinell framework:

1. **Exception Classes in Wrong Location** - Generic exceptions (`ElementNotFoundException`, `AssertionException`, `PageLoadException`) are currently defined within platform-specific files but should be in Brinell.Core since they're framework-wide concepts.

2. **Assert/Check Methods Break Fluent Chaining** - All `Check*` and `Assert*` methods currently return `void`, breaking the fluent API pattern. They should return `TPage` for consistency with action methods.

3. **Dead Code in DefaultLocatorStrategy** - `MauiContainerBase.DefaultLocatorStrategy` has a ternary that returns the same value for both branches, indicating either dead code or incomplete implementation.

## Alignment with Product Vision

This aligns with the **Is/Wait/Check/Assert Pattern** standard from tech.md which requires consistent state verification API. It also supports the **Interface-Based Design** principle by moving shared concepts to Brinell.Core where they belong.

## Requirements

### Requirement 1: Centralize Exception Classes

**User Story:** As a framework developer, I want generic exception classes defined in Brinell.Core, so that all platforms can use the same exception types without duplicating definitions.

#### Acceptance Criteria

1. WHEN an exception represents a framework-wide concept (element not found, assertion failed, page load failed) THEN the exception class SHALL be defined in `Brinell.Core.Exceptions` namespace
2. WHEN platform code throws these exceptions THEN it SHALL reference the exceptions from Brinell.Core
3. WHEN exceptions are moved THEN existing exception definitions in platform projects SHALL be removed to prevent ambiguity

#### Scope

**Exceptions to move:**
- `ElementNotFoundException` (currently in `MauiTestContext.cs`)
- `AssertionException` (currently in `MauiControlBase.cs`)
- `PageLoadException` (currently in `MauiPageObjectBase.cs`)

**Target location:** `srcnew/Brinell.Core/Exceptions/`

### Requirement 2: Fluent Assert/Check Methods

**User Story:** As a test writer, I want `Assert*` and `Check*` methods to return the page instance, so that I can chain assertions with other operations in a fluent style.

#### Acceptance Criteria

1. WHEN any `Assert*` method completes successfully THEN it SHALL return `TPage` to enable fluent chaining
2. WHEN any `Check*` method completes THEN it SHALL return `TPage` to enable fluent chaining
3. WHEN an assertion fails (throws) THEN no return value is needed since the exception stops execution
4. WHEN interfaces define assertion/check methods THEN they SHALL specify return type as `TPage` instead of `void`

#### Affected Methods

**MauiControlBase:**
- `AssertExists` → returns `TPage`
- `AssertVisible` → returns `TPage`
- `AssertEnabled` → returns `TPage`
- `AssertText` → returns `TPage`
- `AssertTextContains` → returns `TPage`

**MauiButtonControl:**
- `AssertClickable` → returns `TPage`

**MauiEntryControl:**
- `AssertTextMatches` → returns `TPage`
- `AssertPlaceholder` → returns `TPage`
- `AssertReadOnly` → returns `TPage`

**MauiPageObjectBase:**
- `AssertLoaded` → returns `TSelf`
- `AssertTitle` → returns `TSelf`

**Note:** Page object asserts return `TSelf` (the page type), not `TPage`.

### Requirement 3: Fix DefaultLocatorStrategy Dead Code

**User Story:** As a framework developer, I want `DefaultLocatorStrategy` to either have meaningful conditional logic or be simplified, so that the code is maintainable and its intent is clear.

#### Acceptance Criteria

1. WHEN `DefaultLocatorStrategy` has identical values for both ternary branches THEN it SHALL be simplified to a direct return
2. IF the conditional was intended for future use THEN a TODO comment SHALL be added OR the condition SHALL be removed entirely

#### Current Code (problematic)
```csharp
public LocatorStrategy DefaultLocatorStrategy => Context.Timeouts != null 
    ? LocatorStrategy.AutomationId 
    : LocatorStrategy.AutomationId;
```

#### Expected Behavior
```csharp
public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
```

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: Exception classes should be in a dedicated `Exceptions` folder in Brinell.Core
- **Minimal Dependencies**: Platform projects reference Core exceptions, no duplication
- **Consistent API**: All fluent methods maintain the same return pattern

### Backward Compatibility
- Existing test code that uses `void` return values will continue to work (just ignore return value)
- Exception types are semantically identical, just moved

### Performance
- No performance impact - these are simple changes

### Maintainability
- Centralized exceptions reduce maintenance burden across platforms
- Fluent API consistency makes the framework easier to learn and use
