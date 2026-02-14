# 221 Foundation Module Index

## Module Overview

| Property | Value |
|----------|-------|
| **Module Code** | FND |
| **Module Name** | Foundation |
| **Purpose** | Cross-cutting concerns for the Brinell framework |
| **Package** | Brinell.Core |

---

## Description

The Foundation module defines cross-cutting concerns that apply across all platform implementations. These foundational capabilities ensure consistent behavior for logging, configuration, exception handling, and timeout management throughout the framework.

Foundation concerns are implemented in `Brinell.Core` and consumed by all platform packages (`Brinell.Maui`, `Brinell.Blazor`, `Brinell.Wpf`).

---

## Documents

| Document | Title | Description |
|----------|-------|-------------|
| [221_001](221_001_Logging.spx.md) | Logging | Test logging contracts and CSV output |
| [221_002](221_002_Configuration.spx.md) | Configuration | Test configuration and settings management |
| [221_003](221_003_ExceptionHandling.spx.md) | Exception Handling | Framework exception types and error reporting |
| [221_004](221_004_Timeout.spx.md) | Timeout Management | Timeout strategies and configuration |

---

## Cross-Cutting Concerns Checklist

| Concern | Status | Document |
|---------|--------|----------|
| Logging | ✅ Implemented | [221_001](221_001_Logging.spx.md) |
| Configuration | ✅ Implemented | [221_002](221_002_Configuration.spx.md) |
| Exception Handling | ✅ Implemented | [221_003](221_003_ExceptionHandling.spx.md) |
| Timeout Management | ✅ Implemented | [221_004](221_004_Timeout.spx.md) |
| Security | N/A | Credentials handled at test level |
| Localization | N/A | Not required for test framework |

---

## Package Structure

```
Brinell.Core/
├── Logging/
│   ├── ITestLogger.cs           # Logging contract
│   ├── CsvTestLogger.cs         # CSV implementation
│   ├── LogResult.cs             # Result enumeration
│   └── LoggingExtensions.cs     # Extension methods
├── Configuration/
│   └── UITestConfiguration.cs   # Configuration classes
├── Exceptions/
│   ├── ElementNotFoundException.cs
│   ├── UITestTimeoutException.cs
│   ├── AssertionException.cs
│   ├── CheckFailedException.cs
│   ├── InvalidStateException.cs
│   ├── PageNotDisplayedException.cs
│   └── PageNotReadyException.cs
└── ...
```

---

## Dependency Flow

```
┌─────────────────────────────────────────────────┐
│                  Test Projects                   │
│     (Brinell.Samples.Maui.UITests, etc.)        │
└─────────────────────┬───────────────────────────┘
                      │ uses
                      ▼
┌─────────────────────────────────────────────────┐
│              Platform Packages                   │
│   Brinell.Maui │ Brinell.Blazor │ Brinell.Wpf  │
└─────────────────────┬───────────────────────────┘
                      │ references
                      ▼
┌─────────────────────────────────────────────────┐
│               Brinell.Core                       │
│  ┌──────────┐ ┌────────────┐ ┌──────────────┐  │
│  │ Logging  │ │   Config   │ │  Exceptions  │  │
│  └──────────┘ └────────────┘ └──────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## Requirements Traceability

| Requirement | Document | Description |
|-------------|----------|-------------|
| FR-006 | [221_001](221_001_Logging.spx.md) | Logging and Diagnostics |
| FR-010 | [221_003](221_003_ExceptionHandling.spx.md) | Error Handling |
| NFR-USE-002 | [221_003](221_003_ExceptionHandling.spx.md) | Error Messages |
| NFR-USE-003 | [221_001](221_001_Logging.spx.md) | Debugging Support |
| NFR-REL-003 | [221_004](221_004_Timeout.spx.md) | Test Execution Timeout |

---

## Related Documents

- [211 Modules](../211_Modules/211_INDEX.md) - Core module definitions
- [220 External](../220_External/220_INDEX.md) - External dependencies
- [133_002 Error Messages](../../100_requirements/133_usability/133_002_ErrorMessages.spx.md)
- [133_003 Debugging Support](../../100_requirements/133_usability/133_003_DebuggingSupport.spx.md)
