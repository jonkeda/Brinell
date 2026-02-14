# Exception Classes

**Source of truth:** `srcnew/Brinell.Core/Exceptions/`

## Hierarchy

```
Exception
├── BrinellException (framework root)
│   ├── AssertionException (assertion failed)
│   ├── ElementNotFoundException (element not found)
│   ├── WaitTimeoutException (wait timed out)
│   └── LocatorNotSupportedException (strategy not supported by driver)
└── PageLoadException (page load failed)
```

## Usage

| Exception | Thrown When | Key Info |
|-----------|-----------|----------|
| `AssertionException` | `Assert*()` methods fail | `Expected`, `Actual`, `ControlLocator` |
| `ElementNotFoundException` | Element not found within timeout | `LocatorInfo` (Locator), `LocatorString` |
| `WaitTimeoutException` | `Wait*()` exceeds timeout | `TimeoutMs`, `Condition` |
| `LocatorNotSupportedException` | Driver doesn't support requested strategy | `Strategy`, `DriverName` |
| `PageLoadException` | Page fails to load within timeout | — |
