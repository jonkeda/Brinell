# 130_007 Platform Support

## quality PlatformSupport

- **attribute**: Compatibility
- **requirement**: Framework supports required operating systems and .NET versions
- **priority**: high

---

## Description

This requirement specifies the platforms and runtime versions the framework must support, ensuring broad compatibility across development and CI/CD environments.

---

## Sub-Requirements

### NFR-COMPAT-001.1: Operating Systems

- Windows platform MUST support Windows 10 and later
- Web platform MUST support modern browsers (Chrome, Firefox, Edge, Safari)
- Mobile platforms MUST support current and previous major OS versions

### NFR-COMPAT-001.2: .NET Versions

- Framework MUST support .NET 8.0 or later
- Framework SHOULD support LTS .NET versions
- Framework MUST clearly document minimum .NET version

---

## Platform Matrix

### MAUI Platform

| Target | Minimum Version | Automation |
|--------|-----------------|------------|
| Windows | Windows 10 | WinAppDriver |
| Android | API 24+ | Appium |
| iOS | iOS 14+ | Appium |
| macOS | macOS 12+ | Appium |

### Blazor Platform

| Browser | Minimum Version | Notes |
|---------|-----------------|-------|
| Chrome | Latest - 2 | Primary target |
| Firefox | Latest - 2 | Secondary |
| Edge | Latest - 2 | Chromium-based |
| Safari | Latest - 1 | WebKit differences |

### WPF Platform

| Target | Minimum Version |
|--------|-----------------|
| Windows | Windows 10 |
| .NET | .NET 8.0+ |

---

## .NET Version Support

| .NET Version | Support Level | End of Support |
|--------------|---------------|----------------|
| .NET 8.0 | Fully Supported | Nov 2026 |
| .NET 9.0 | Fully Supported | May 2026 |

---

## Related

- [FR-001 Multi-Platform Support](../120_functional/120_001_MultiPlatformSupport.spx.md)
- [NFR-COMPAT-002 Automation Libraries](130_008_AutomationLibraries.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-COMPAT-001
