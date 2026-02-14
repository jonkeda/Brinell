# Architectural Decisions

**Version:** 3.2 | **Status:** Active

## AD-001: Clean Architecture with Interface Segregation

**Decision:** Core layer contains only interfaces and value types. No implementations.

**Rationale:** Enables platform libraries to implement interfaces without coupling to each other. Test code depends on interfaces, not implementations.

**Consequence:** Each platform project is independent; adding a platform requires zero changes to Core.

## AD-002: Interface-First Design

**Decision:** Define all control capabilities as interfaces before any implementation.

**Rationale:** Interfaces define the test-writer's API contract. Implementations are platform-specific details.

**Consequence:** 25 interfaces in `Brinell.Core/Interfaces/`, each representing a single capability.

## AD-003: Platform-Specific Base Classes

**Decision:** Each platform has its own base class hierarchy, not shared abstract classes.

**Rationale:** Platforms differ fundamentally (sync vs async, Appium vs Playwright, element models). Shared base classes leak abstractions.

**Consequence:** `MauiControlBase`, `BlazorControlBase`, etc. are independent hierarchies implementing Core interfaces.

## AD-004: Separate Technology Adapters

**Decision:** Driver implementations (Appium, FlaUI, Playwright) are in separate NuGet packages.

**Rationale:** Tests should choose their driver at configuration time, not compile time. Enables driver swapping for CI vs local.

**Consequence:** `Brinell.Maui.Appium` and `Brinell.Maui.FlaUI` are separate projects with `MauiDriverFactory` as the selector.

## AD-005: Synchronous API for MAUI

**Decision:** MAUI control API is synchronous. Blazor may be async.

**Rationale:** Appium operations are inherently synchronous. Async wrappers add complexity without benefit. Test code reads better without `await`.

**Consequence:** All MAUI `Is*/Wait*/Assert*` methods are synchronous. Blazor interfaces may introduce `Task<>`-returning variants.

## AD-006: Nullable Skip Pattern

**Decision:** `null` expected values in Wait/Assert methods mean "skip this check."

**Rationale:** Enables data-driven tests where some assertions are conditional without if-branching.

**Consequence:** Every Wait/Assert parameter is nullable. Methods must check for null before operating.

## AD-007: Fluent TScope Return

**Decision:** Action and assertion methods return `TScope` (the scope/page type) for chaining.

**Rationale:** Enables fluent test code: `page.UserName.Enter("test").Password.Enter("pass").Submit.Click()`

**Consequence:** Generic `TScope` type parameter threads through all interfaces and base classes.

## AD-008: Locator Value Object

**Decision:** Use `Locator` immutable value object instead of raw strings for element identification.

**Rationale:** Supports multiple strategies (AutomationId, XPath, CSS, etc.) with platform-specific mapping.

**Consequence:** All element-finding methods accept `Locator`. Implicit `string → Locator` conversion for convenience.

## AD-009: No FluentAssertions

**Decision:** Use xUnit `Assert` exclusively. No FluentAssertions dependency.

**Rationale:** Licensing concerns. The framework's own `Assert*` methods provide fluent assertions for control state.

**Consequence:** Custom assertion methods on controls replace FluentAssertions patterns.

## AD-010: CSV Structured Logging

**Decision:** Log to structured CSV files, one per test.

**Rationale:** Easy to parse, diff, and aggregate. No dependency on logging frameworks.

**Consequence:** `ITestLogger` writes CSV rows with timestamp, level, control, action, and outcome.
