# MAUI Platform Guide

Brinell supports MAUI through shared MAUI controls plus driver adapters.

## Projects

- `srcnew/Brinell.Maui`
- `srcnew/Brinell.Maui.Appium`
- `srcnew/Brinell.Maui.FlaUI`
- `srcnew/Brinell.Maui.CommunityToolkit`
- `testsnew/Brinell.Maui.Tests`
- `testsnew/Brinell.Maui.UITests`
- `testsnew/Brinell.Maui.Uat.Tests`

## Driver Choices

| Driver | Use when |
| --- | --- |
| FlaUI | Windows MAUI desktop automation |
| Appium | Android, iOS, or Appium-backed Windows automation |

## Rules

- Prefer automation IDs and semantic control APIs.
- Wait for page readiness after navigation.
- Keep Appium capability setup in fixtures/options.
- Keep pointer input disabled unless testing gesture-only surfaces.

## Run

See [MAUI Run Guide](../run/MAUI.md) and
[MAUI Android Run Guide](../run/maui-android.md).
