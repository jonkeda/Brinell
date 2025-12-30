# UI Test Framework Documentation

**Version:** 3.0  
**Last Updated:** December 2025  
**Framework Stack:** FlaUI (WPF) + Appium (MAUI/Mobile) + Selenium (Web) + xUnit

---

## Overview

The UI Test Framework provides a multi-platform test automation solution with consistent patterns across Windows (WPF), MAUI (cross-platform), and Web (HTML) applications.

### Key Features

- **Multi-platform support** - Single test framework for WPF, MAUI, and Web
- **Interface-based architecture** - Core defines contracts, platforms implement
- **Native driver access** - Direct FlaUI/Appium/Selenium access (no adapters)
- **Wait/Check/Is/Assert pattern** - Four-tier state verification
- **Page Object pattern** - Encapsulated page structure and behavior
- **IsBusy tracking** - Automatic page readiness detection
- **Structured CSV logging** - Machine-parseable test logs

---

## Documentation Index

### Getting Started
1. **[Quick Start Guide](01-quick-start.md)** - Get running in 5 minutes
2. **[Framework Overview](02-framework-overview.md)** - Architecture and design principles

### Core Concepts
3. **[Architecture](03-architecture.md)** - Component relationships and layers
4. **[Control Objects](04-control-objects.md)** - Control hierarchy and patterns
5. **[Page Objects](05-page-objects.md)** - Page encapsulation and navigation
6. **[Wait/Check/Is/Assert Pattern](06-wait-check-assert.md)** - State verification methods

### Platform-Specific
7. **[WPF Platform (FlaUI)](07-wpf-platform.md)** - Windows desktop automation
8. **[MAUI Platform (Appium)](08-maui-platform.md)** - Cross-platform and mobile
9. **[Web Platform (Selenium)](09-web-platform.md)** - Browser automation

### Advanced Topics
10. **[IsBusy State Tracking](10-isbusy-tracking.md)** - Page readiness detection
11. **[Multi-Platform Testing](11-multi-platform.md)** - Write once, run anywhere
12. **[Best Practices](12-best-practices.md)** - Guidelines for maintainable tests
13. **[Troubleshooting](13-troubleshooting.md)** - Common issues and solutions

### Reference
14. **[API Reference](14-api-reference.md)** - Complete interface documentation
15. **[Test Writing Guide](15-test-writing-guide.md)** - Quick reference for writing tests

---

## Version History

### Version 3.0 (December 2025)
- **Core = interfaces only** - No base classes or adapters in Core
- **Platform-specific base classes** - Each platform fully self-contained
- **Navigation returns void** - Tests manage page object lifecycle
- **Direct driver access** - No adapter layer, native performance

### Version 2.0 (December 2025)
- Assert pattern with full logging
- IsBusy state tracking
- CSV structured logging
- WireMock API mocking
- Selenium web support

### Version 1.0 (November 2025)
- Initial framework with FlaUI and Appium
- Page Object and Control Object patterns
- Basic multi-platform support

---

## Quick Links

- **[Installation](01-quick-start.md#installation)**
- **[Writing Your First Test](01-quick-start.md#first-test)**
- **[Common Patterns](15-test-writing-guide.md)**
- **[Troubleshooting](13-troubleshooting.md)**

---

## Framework Stack

| Component | Version | Purpose |
|-----------|---------|---------|
| FlaUI | 4.0.0 | WPF automation via UI Automation |
| Appium.WebDriver | 8.0.0 | MAUI/Mobile automation |
| Selenium.WebDriver | 4.27.0 | Web browser automation |
| xUnit | 2.9.x | Test framework |
| FluentAssertions | 6.x | Assertion library |

---

## Support

For issues, questions, or contributions, please refer to:
- **Issues:** Report problems or request features
- **Discussions:** Ask questions and share ideas
- **Contributing:** See contribution guidelines

---

*Documentation for Oravey UI Test Framework v3.0*
