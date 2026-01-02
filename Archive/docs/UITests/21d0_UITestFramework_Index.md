# Oravey UI Testing Framework - Documentation Index

**Status:** Active  
**Date:** December 2025  
**Framework:** FlaUI (WPF) + Appium (MAUI) + Selenium (HTML) + xUnit  
**Core Library:** Oravey.UITestFramework.Core  
**Version:** 3.0

---

## Document Index

| Document | Topic | Code Examples | Version |
|----------|-------|---------------|---------|
| [21d1](21d1_Overview.md) | Overview | [Examples](21d1_Overview_CodeExamples.md) | 2.0 |
| [21d2](21d2_Architecture.md) | Architecture | [Examples](21d2_Architecture_CodeExamples.md) | **3.0** |
| [21d3](21d3_CoreFramework.md) | Core Framework | [Examples](21d3_CoreFramework_CodeExamples.md) | **3.0** |
| [21d4](21d4_PlatformImplementations.md) | Platform Implementations | [Examples](21d4_PlatformImplementations_CodeExamples.md) | **3.0** |
| [21d5](21d5_MultiPlatformSupport.md) | Multi-Platform Support | [Examples](21d5_MultiPlatformSupport_CodeExamples.md) | **3.0** |
| [21d6](21d6_ControlObjectHierarchy.md) | ControlObject Hierarchy | [Examples](21d6_ControlObjectHierarchy_CodeExamples.md) | **3.0** |
| [21d7](21d7_WaitCheckIsAssertPattern.md) | Wait/Check/Is/Assert Pattern | [Examples](21d7_WaitCheckIsAssertPattern_CodeExamples.md) | **3.0** |
| [21d8](21d8_IsBusyStateTracking.md) | IsBusy-Based State Tracking | [Examples](21d8_IsBusyStateTracking_CodeExamples.md) | **3.0** |
| [21d9](21d9_PageObjectPattern.md) | Page Object Pattern | [Examples](21d9_PageObjectPattern_CodeExamples.md) | **3.0** |
| [21d10](21d10_WireMockApiMocking.md) | WireMock API Mocking | [Examples](21d10_WireMockApiMocking_CodeExamples.md) | 2.0 |
| [21d11](21d11_CloudProviderSupport.md) | Cloud Provider Support | [Examples](21d11_CloudProviderSupport_CodeExamples.md) | 2.0 |
| [21d12](21d12_StandardizedLogging.md) | Standardized Logging | [Examples](21d12_StandardizedLogging_CodeExamples.md) | 2.0 |
| [21d13](21d13_ApplicationUITestProjects.md) | Application UITest Projects | [Examples](21d13_ApplicationUITestProjects_CodeExamples.md) | 2.0 |
| [21d14](21d14_TestCategories.md) | Test Categories | [Examples](21d14_TestCategories_CodeExamples.md) | 2.0 |
| [21d15](21d15_RunningTests.md) | Running Tests | [Examples](21d15_RunningTests_CodeExamples.md) | 2.0 |
| [21d16](21d16_BestPractices.md) | Best Practices | [Examples](21d16_BestPractices_CodeExamples.md) | **3.0** |
| [21d17](21d17_Troubleshooting.md) | Troubleshooting | [Examples](21d17_Troubleshooting_CodeExamples.md) | 2.0 |

---

## Key Design Changes (v3)

### From v2 to v3

| Change | Before (v2) | After (v3) |
|--------|-------------|------------|
| Core project | Interfaces + base classes + adapters | **Interfaces only** |
| Driver abstraction | IDriverAdapter, IElementAdapter | **Native driver access** |
| Base class location | Shared in Core | **Platform-specific** |
| ControlObject hierarchy | Shared inheritance tree | **Per-platform hierarchy** |
| Navigation return type | Returns target page | **Returns void** |
| Page creation | Inside navigation method | **Test creates target page** |
| ITestContext | Element operations | **No element operations** |

### Benefits of v3

- **Simpler Core**: Pure interface contracts, no implementation details
- **Native Performance**: Direct driver access without adapter overhead
- **Platform Flexibility**: Each platform can optimize independently
- **Clearer Ownership**: Tests own page lifecycle explicitly
- **Better Debugging**: Direct stack traces to native drivers

---

## Quick Start

1. Read [Overview](21d1_Overview.md) for framework stack
2. Understand [Architecture](21d2_Architecture.md) layers
3. Learn [Wait/Check/Is/Assert Pattern](21d7_WaitCheckIsAssertPattern.md)
4. See [Best Practices](21d16_BestPractices.md) for guidelines

---

## Version History

| Version | Date | Key Changes |
|---------|------|-------------|
| 3.0 | December 2025 | Core = interfaces only, platform-specific base classes, navigation returns void |
| 2.0 | December 2025 | Assert pattern, IsBusy tracking, CSV logging, WireMock, Selenium |
| 1.0 | November 2025 | Initial framework with FlaUI and Appium |

---

*Document Version: 3.0*  
*Last Updated: December 2025*
