# SPX-028: Testsnew Multi-Platform Validation

**Status:** Active

## Goal

Ensure `testsnew/` infrastructure supports running UI tests against both Appium and FlaUI drivers, and across Windows/Android platforms.

## Driver Factory Configuration

`MauiDriverFactory` selects driver via environment:

| Variable | Values | Purpose |
|----------|--------|---------|
| `BRINELL_DRIVER` | `Appium`, `FlaUI` | Which driver adapter |
| `APPIUM_PLATFORM` | `windows`, `android`, `ios` | Target platform |
| `APPIUM_DEVICE_NAME` | e.g., `emulator-5554` | Device identifier |
| `APPIUM_APP_PATH` | Path to AUT | Application under test |

## Test Matrix

| Driver | Platform | Status |
|--------|----------|--------|
| FlaUI | Windows | Working |
| Appium | Windows | Working (WinAppDriver) |
| Appium | Android | Not yet tested |
| Appium | iOS | Not yet tested |

## Validation Checklist

- [ ] Tests pass with FlaUI on Windows
- [ ] Tests pass with Appium/WinAppDriver on Windows
- [ ] Driver switching via env var works
- [ ] Screenshot capture works on both drivers
- [ ] Timeout configuration respected by both drivers
