# 130_008 Automation Libraries

## quality AutomationLibraries

- **attribute**: Compatibility
- **requirement**: Framework integrates with standard automation libraries
- **priority**: high

---

## Description

This requirement specifies the automation library versions and protocols the framework must support for each platform.

---

## Sub-Requirements

### NFR-COMPAT-002.1: FlaUI

- WPF platform MUST use FlaUI 4.0 or later
- WPF platform MUST support UI Automation 3

### NFR-COMPAT-002.2: Appium

- MAUI platform MUST use Appium WebDriver 8.0 or later
- MAUI platform MUST support W3C WebDriver protocol

### NFR-COMPAT-002.3: Selenium

- Web platform MUST use Selenium WebDriver 4.0 or later
- Web platform MUST support W3C WebDriver protocol

---

## Library Versions

| Platform | Library | Minimum Version | Protocol |
|----------|---------|-----------------|----------|
| WPF | FlaUI.Core | 4.0+ | UI Automation |
| WPF | FlaUI.UIA3 | 4.0+ | UIA3 |
| MAUI | Appium.WebDriver | 8.0+ | W3C WebDriver |
| Blazor | Selenium.WebDriver | 4.0+ | W3C WebDriver |

---

## Driver Requirements

### WinAppDriver (MAUI Windows)

```
Version: 1.2.1 or later
Installation: Windows App Certification Kit
Developer Mode: Required
```

### Appium Server (MAUI Mobile)

```
Version: 2.0+ (with UIAutomator2/XCUITest)
Node.js: 18.0+
Drivers: uiautomator2, xcuitest
```

### Selenium WebDriver (Blazor)

```
ChromeDriver: Match Chrome version
GeckoDriver: Match Firefox version
EdgeDriver: Match Edge version
```

---

## W3C WebDriver Compliance

All web-based automation must use W3C WebDriver protocol:

- Standard element location strategies
- Standard capability negotiation
- Standard action sequences
- Standard error responses

---

## Related

- [FR-007 Platform Automation](../120_functional/120_007_PlatformAutomation.spx.md)
- [NFR-COMPAT-001 Platform Support](130_007_PlatformSupport.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-COMPAT-002
