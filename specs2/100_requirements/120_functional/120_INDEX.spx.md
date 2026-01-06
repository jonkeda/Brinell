# functional Requirements Index V2
- **id**: 120-INDEX
- **version**: 2.0
- **status**: draft
- **last-updated**: 2026-01-06

This index organizes the functional requirements for the Brinell UI Testing Framework into logical categories. Requirements are language-agnostic and describe **what** the framework must do, not **how** it implements.

## categories

### Category A: Platform and Technology (120_0xx)
Requirements related to supported platforms and automation technologies.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_010_PlatformSupport](120_010_PlatformSupport.spx.md) | FR-010 | Supported Platforms and Technologies | High |
| [120_011_DriverAbstraction](120_011_DriverAbstraction.spx.md) | FR-011 | Driver Abstraction Layer | High |

### Category B: Object Model (120_1xx)
Requirements for the core object model: controls, pages, and containers.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_100_ControlObject](120_100_ControlObject.spx.md) | FR-100 | Control Object Model | High |
| [120_101_PageObject](120_101_PageObject.spx.md) | FR-101 | Page Object Model | High |
| [120_102_ContainerObject](120_102_ContainerObject.spx.md) | FR-102 | Container Object Model | High |
| [120_103_InterfaceHierarchy](120_103_InterfaceHierarchy.spx.md) | FR-103 | Interface Hierarchy | High |

### Category C: Element Location (120_2xx)
Requirements for finding and identifying UI elements.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_200_ElementLocation](120_200_ElementLocation.spx.md) | FR-200 | Element Location Strategies | High |

### Category D: State and Verification (120_3xx)
Requirements for state checking, waiting, and assertions.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_300_StateVerification](120_300_StateVerification.spx.md) | FR-300 | State Verification Methods | High |
| [120_301_WaitingSynchronization](120_301_WaitingSynchronization.spx.md) | FR-301 | Waiting and Synchronization | High |
| [120_302_Assertions](120_302_Assertions.spx.md) | FR-302 | Assertion Methods | High |

### Category E: Execution Context (120_4xx)
Requirements for test context, configuration, and lifecycle.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_400_TestContext](120_400_TestContext.spx.md) | FR-400 | Test Context Management | High |
| [120_401_Configuration](120_401_Configuration.spx.md) | FR-401 | Configuration System | High |
| [120_402_TimeoutHandling](120_402_TimeoutHandling.spx.md) | FR-402 | Timeout Handling | High |

### Category F: Logging and Evidence (120_5xx)
Requirements for logging, diagnostics, and evidence collection.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_500_Logging](120_500_Logging.spx.md) | FR-500 | Logging System | High |
| [120_501_LogFileManagement](120_501_LogFileManagement.spx.md) | FR-501 | Log File Management | High |
| [120_502_ScreenshotEvidence](120_502_ScreenshotEvidence.spx.md) | FR-502 | Screenshot and Evidence | High |

### Category G: Error Handling (120_6xx)
Requirements for exceptions and error management.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_600_ExceptionStrategy](120_600_ExceptionStrategy.spx.md) | FR-600 | Exception Strategy | High |
| [120_601_RetryHandling](120_601_RetryHandling.spx.md) | FR-601 | Retry and Recovery | Medium |

### Category H: Test Execution (120_7xx)
Requirements for test isolation and execution patterns.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_700_TestIsolation](120_700_TestIsolation.spx.md) | FR-700 | Test Isolation | High |
| [120_701_AsyncSupport](120_701_AsyncSupport.spx.md) | FR-701 | Asynchronous Operation Support | High |

### Category I: Extensibility (120_8xx)
Requirements for extension and customization.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_800_Extensibility](120_800_Extensibility.spx.md) | FR-800 | Extensibility Model | Medium |

### Category J: Compliance (120_9xx)
Requirements for licensing and accessibility.

| File | ID | Title | Priority |
|------|-----|-------|----------|
| [120_900_Licensing](120_900_Licensing.spx.md) | FR-900 | Dependency Licensing | Medium |
| [120_901_Accessibility](120_901_Accessibility.spx.md) | FR-901 | Accessibility Support | Low |

---

## migration from V1

| V1 File | V2 File(s) | Notes |
|---------|-----------|-------|
| FR-001 MultiPlatformSupport | FR-010 PlatformSupport | Consolidated |
| FR-002 ControlObjectPattern | FR-100, FR-103, FR-200 | Split into focused requirements |
| FR-003 PageObjectPattern | FR-101 PageObject | Streamlined |
| FR-004 StateVerification | FR-300 StateVerification | Renamed |
| FR-005 WaitingSynchronization | FR-301 WaitingSynchronization | Renamed |
| FR-006 LoggingDiagnostics | FR-500 Logging | Split from file management |
| FR-007 PlatformAutomation | FR-010 PlatformSupport | Merged |
| FR-008 Extensibility | FR-800 Extensibility | Renamed |
| FR-009 TestIsolation | FR-700 TestIsolation | Renamed |
| FR-010 ErrorHandling | FR-600, FR-601 | Split exceptions from retry |
| FR-011 DependencyLicensing | FR-900 Licensing | Renamed |
| FR-012 ContainerPattern | FR-102 ContainerObject | Renamed |
| FR-013 AsyncPattern | FR-701 AsyncSupport | Renamed |
| FR-014 ConfigurationSettings | FR-401 Configuration | Renamed |
| FR-015 ScreenshotEvidence | FR-502 ScreenshotEvidence | Renamed |
| FR-016 TestContextLifecycle | FR-400 TestContext | Renamed |
| FR-017 AccessibilityTesting | FR-901 Accessibility | Renamed |
| FR-018 DriverAdapterPattern | FR-011 DriverAbstraction | Renamed |
| FR-019 MethodLoggingPattern | FR-500 Logging | Merged into logging |
| FR-020 LogFileManagement | FR-501 LogFileManagement | Renamed |
| FR-021 NullableParameterHandling | FR-100 ControlObject | Merged into control behavior |
| FR-022 TimeoutHandling | FR-402 TimeoutHandling | Renamed |
| FR-023 ExceptionStrategy | FR-600 ExceptionStrategy | Renamed |

---

## design principles

### Language Independence
- Requirements describe behavior, not implementation
- Examples use pseudocode or diagrams, not language-specific syntax
- Avoid references to specific language features (generics notation, nullable types)

### No Duplication
- Each requirement appears in exactly one location
- Cross-references used when concepts relate
- Single source of truth for each capability

### Non-Contradictory
- Timeout hierarchy defined in one place (FR-402)
- Method behavior patterns defined consistently (FR-300)
- Exception types defined once (FR-600)

### Proper Functional Requirements
- Focus on **what** the system must do
- Measurable and testable
- Clear acceptance criteria
- Traceable to goals and non-functional requirements
