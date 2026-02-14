# 220 External Dependencies

**Block Type:** IDX (Index)  
**ID:** 220  
**Title:** External Dependencies Index  
**Status:** Draft  
**Version:** 1.0

---

## Overview

This section documents the external libraries and frameworks that Brinell depends on for UI automation. Each platform package relies on specific automation drivers.

## Quick Reference

> **📋 Start here:** [220_000_ExternalDependencies.spx.md](220_000_ExternalDependencies.spx.md) — Consolidated overview with locator mappings, element properties, and version compatibility for all drivers.

## Documents

| ID | Title | Description |
|----|-------|-------------|
| [220.000](220_000_ExternalDependencies.spx.md) | **Overview** | Consolidated quick reference for all drivers |
| [220.001](220_001_Appium.spx.md) | Appium | Full documentation for MAUI (Android/iOS) |
| [220.002](220_002_Selenium.spx.md) | Selenium WebDriver | Full documentation for Blazor |
| [220.003](220_003_Playwright.spx.md) | Playwright | Full documentation (planned) |
| [220.004](220_004_FlaUI.spx.md) | FlaUI | Full documentation for WPF/WinForms |

## Platform Mapping

| Brinell Package | External Dependency | Target Platform |
|-----------------|---------------------|-----------------|
| Brinell.Maui | Appium | Android, iOS, Windows (MAUI) |
| Brinell.Blazor | Selenium WebDriver | Chrome, Firefox, Edge, Safari |
| Brinell.Blazor | Playwright (optional) | Chromium, Firefox, WebKit |
| Brinell.Wpf | FlaUI | WPF, WinForms, Win32, UWP |

## Dependency Philosophy

1. **Thin Wrappers** — Brinell wraps external drivers, not replaces them
2. **Version Isolation** — Driver versions managed via NuGet
3. **Abstraction** — Test code doesn't directly use driver APIs
4. **Escape Hatches** — Advanced users can access underlying drivers when needed

---

## Related Documents

- [211 Modules](../211_Modules/211_INDEX.md) — Module definitions using these dependencies
- [203.002 Platform Layer](../203_Layers/203_002_PlatformLayer.spx.md) — Platform abstraction layer
