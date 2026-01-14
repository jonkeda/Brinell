# Requirements Document: Appium Abstraction Layer

## Introduction

This specification defines abstraction layer interfaces and implementations that wrap the Appium library's concrete classes (`AppiumDriver`, `AppiumElement`) to enable proper unit testing with mocking frameworks like Moq.

### Problem Statement

The Appium.WebDriver library provides concrete classes (`AppiumDriver`, `AppiumElement`) that:
- Lack parameterless constructors required by Moq's Castle.DynamicProxy
- Require active WebDriver connections to instantiate
- Cannot be subclassed meaningfully for testing purposes

This makes unit testing of Brinell.Maui components impossible without integration test infrastructure (running Appium server).

### Proposed Solution

Create thin wrapper interfaces and implementations that:
- Abstract over `AppiumDriver` and `AppiumElement`
- Can be easily mocked for unit testing
- Have minimal performance overhead in production
- Maintain the full functionality of the underlying Appium types

## Alignment with Product Vision

This feature directly supports Brinell's core values:

| Product Goal | How This Feature Supports It |
|--------------|------------------------------|
| Testability | Enables true unit testing of framework code without external dependencies |
| Maintainability | Clean separation of concerns between abstraction and implementation |
| Reliability | Better test coverage leads to more reliable framework |
| Self-Contained Platforms | Keeps Appium-specific abstractions within Brinell.Maui |

## Requirements

### Requirement 1: Element Wrapper Interface

**User Story:** As a framework developer, I want an interface that wraps `AppiumElement` functionality, so that I can mock element interactions in unit tests.

#### Acceptance Criteria

1. WHEN the framework needs to interact with a UI element THEN `IMauiElement` interface SHALL provide methods for all common element operations (Click, SendKeys, Clear, GetAttribute, GetText)
2. WHEN a test needs to verify element state THEN `IMauiElement` SHALL expose properties for Displayed, Enabled, Selected, and location/size
3. WHEN the framework needs to find child elements THEN `IMauiElement` SHALL provide FindElement and FindElements methods that return `IMauiElement` instances
4. WHEN the abstraction wraps a real `AppiumElement` THEN the implementation SHALL delegate all calls directly to the underlying element with minimal overhead

### Requirement 2: Driver Wrapper Interface

**User Story:** As a framework developer, I want an interface that wraps `AppiumDriver` functionality, so that I can mock driver interactions in unit tests.

#### Acceptance Criteria

1. WHEN the framework needs to find elements at the driver level THEN `IMauiDriver` interface SHALL provide FindElement/FindElements methods returning `IMauiElement` instances
2. WHEN the framework needs driver capabilities THEN `IMauiDriver` SHALL expose session management, context switching, and screenshot capabilities
3. WHEN the framework needs to execute scripts or touch actions THEN `IMauiDriver` SHALL provide appropriate method abstractions
4. IF the underlying driver connection fails THEN `IMauiDriver` implementations SHALL propagate exceptions appropriately

### Requirement 3: Seamless Integration with Existing Code

**User Story:** As a framework developer, I want the abstractions to integrate seamlessly with existing `MauiControlBase` and `MauiPageObjectBase` code, so that I don't have to rewrite the entire framework.

#### Acceptance Criteria

1. WHEN updating `IMauiTestContext` THEN it SHALL expose `IMauiDriver` instead of `AppiumDriver`
2. WHEN updating `IElementScope<TElement>` for MAUI THEN `TElement` SHALL be `IMauiElement` instead of `AppiumElement`
3. WHEN migrating existing code THEN changes SHALL be confined to Brinell.Maui project only, NOT affecting Brinell.Core interfaces
4. WHEN the abstraction is introduced THEN backward compatibility with existing test code SHALL NOT be required (breaking change acceptable for new codebase)

### Requirement 4: Factory Pattern for Element Creation

**User Story:** As a framework developer, I want a factory that creates `IMauiElement` wrappers from `AppiumElement` instances, so that element finding operations return the correct abstraction type.

#### Acceptance Criteria

1. WHEN `AppiumDriver.FindElement` returns an `AppiumElement` THEN the factory SHALL wrap it in an `IMauiElement` implementation
2. WHEN `AppiumElement.FindElement` returns a child element THEN the factory SHALL wrap it in an `IMauiElement` implementation
3. WHEN an element operation returns null THEN the factory SHALL return null (not throw)
4. WHEN creating mock elements for tests THEN `IMauiElement` SHALL be directly mockable without special factory configuration

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility Principle**: Wrapper interfaces handle only delegation, no business logic
- **Modular Design**: Element wrapper and driver wrapper are independent components
- **Dependency Management**: Only Brinell.Maui depends on these abstractions; Brinell.Core unchanged
- **Clear Interfaces**: Thin interfaces exposing only what Brinell actually uses from Appium

### Performance

- Wrapper overhead SHALL be negligible (direct delegation only)
- No additional allocations beyond the wrapper instance itself
- No reflection or dynamic invocation in hot paths

### Testability

- All wrapper interfaces SHALL be mockable with Moq (interface-based)
- Mock setup SHALL be straightforward without complex initialization
- Test infrastructure SHALL work without Appium server connection

### Maintainability

- Wrapper interfaces SHALL be updated when Appium library updates add new functionality
- Implementation SHALL use explicit interface implementation where appropriate to keep public API clean

## Scope

### In Scope

- `IMauiElement` interface wrapping `AppiumElement`
- `IMauiDriver` interface wrapping `AppiumDriver`
- `MauiElement` implementation class
- `MauiDriver` implementation class
- Updates to `IMauiTestContext` to use new abstractions
- Updates to `MauiControlBase` and related classes to use `IMauiElement`
- Updates to `MauiPageObjectBase` to use new abstractions
- Updates to unit tests to use mockable interfaces

### Out of Scope

- Changes to Brinell.Core interfaces (these remain platform-agnostic)
- Wrapping every possible Appium feature (only wrap what Brinell uses)
- Multi-touch gesture abstractions (can be added later if needed)
- Screenshot abstractions (already handled separately)

## Open Questions

1. Should `IMauiElement` expose the underlying `AppiumElement` for advanced scenarios, or should it be fully opaque?
   - **Recommendation**: Expose via explicit interface for escape hatch, but discourage use

2. Should we use the same approach for other platforms (FlaUI, Selenium, Playwright)?
   - **Recommendation**: Evaluate after MAUI implementation succeeds; each platform has different mocking challenges
