# Requirements Document: Scope Control Refactor

## Introduction

This specification proposes a refactoring of `MauiContainerBase` and `MauiPageObjectBase` to share a common `ScopeBase` (or similar) base class. Both classes implement scoped element finding and control factory methods, leading to code duplication and inconsistent patterns.

The key insight is that both pages and containers are **scopes** - they provide:
1. Element finding within their boundaries
2. A `Page` reference for fluent chaining
3. Factory methods for creating child controls
4. Access to test context

By extracting this common behavior into a base class, controls can accept `TScope : IScopeBase` instead of `TPage : IPageObject`, allowing controls to return their immediate scope rather than always jumping to the page level.

## Current Design Issues

### Code Duplication
- Factory methods (`Button()`, `Entry()`, `Container()`, `Control()`) are duplicated in both classes
- `Poll()` helper is duplicated between `MauiPageObjectBase` and `MauiControlBase`
- Element finding implementation patterns are similar

### Generic Parameter Inflexibility
- `MauiControlBase<TPage>` requires `TPage : IPageObject`
- This means container child controls always return the page, not the container
- Cannot have container-scoped fluent chaining (e.g., `container.Button("X").Click().Entry("Y").Enter("text")`)

### Inheritance Gap
- `MauiPageObjectBase` and `MauiContainerBase` don't share a common base
- No way to write code that works with "any scope"

## Alignment with Product Vision

This refactor aligns with the **Interface-Based Design** and **Code Reuse** principles from tech.md. It reduces duplication, creates cleaner abstractions, and enables more flexible fluent chaining patterns.

## Requirements

### Requirement 1: Common Scope Base Class

**User Story:** As a framework developer, I want pages and containers to share a common base class, so that I can eliminate code duplication and ensure consistent behavior.

#### Acceptance Criteria

1. WHEN a scope class (page or container) is created THEN it SHALL inherit from a common base providing element finding and factory methods
2. WHEN factory methods are called on any scope THEN they SHALL return controls typed to that scope
3. WHEN the `Poll()` helper is needed THEN it SHALL be available from the common base

### Requirement 2: Flexible Generic Parameter

**User Story:** As a framework developer, I want controls to accept a scope type parameter instead of page type, so that controls can return their containing scope for more granular fluent chaining.

#### Acceptance Criteria

1. WHEN a control is created with `TScope : IScopeBase` THEN it SHALL use `TScope` as its fluent return type
2. IF `TScope` is a page THEN fluent methods SHALL return the page (current behavior)
3. IF `TScope` is a container THEN fluent methods SHALL return the container
4. WHEN a scope provides access to its parent page THEN there SHALL be a way to navigate up the scope hierarchy

### Requirement 3: Virtual Methods for Customization

**User Story:** As a framework developer, I want key scope methods to be virtual, so that pages and containers can override behavior when needed.

#### Acceptance Criteria

1. WHEN element finding methods are defined in the scope base THEN they SHALL be virtual
2. WHEN factory methods are defined in the scope base THEN they SHALL be virtual
3. WHEN `Poll()` or other helper methods are defined THEN they SHALL be virtual

### Requirement 4: Backward Compatibility

**User Story:** As a test writer, I want my existing page objects and controls to continue working, so that I don't have to rewrite my tests.

#### Acceptance Criteria

1. WHEN a page object extends `MauiPageObjectBase<TSelf>` THEN it SHALL continue to work unchanged
2. WHEN a control uses `MauiControlBase<TPage>` THEN it SHALL continue to work unchanged  
3. WHEN existing tests run after the refactor THEN they SHALL pass without modification

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: Each class should have clear responsibility (scope vs. control vs. page)
- **DRY Principle**: Factory methods and helpers should exist in one place only
- **Open/Closed Principle**: Base class should be open for extension via virtual methods

### Performance
- No performance regression from additional inheritance layer
- Element finding should have same performance characteristics

### Usability
- Test writers should have clear guidance on when to use page-scoped vs container-scoped controls
- IDE auto-complete should work correctly with new type parameters

## Scope

### In Scope
- Create common base class for pages and containers
- Update `MauiControlBase` to use scope type parameter
- Make key methods virtual
- Migrate factory methods to base class

### Out of Scope
- Changes to Core interfaces (can be done in follow-up)
- Changes to other platforms (Blazor, WPF, etc.)
- Adding new control types
