# Playwright Platform Guide

Use Playwright when web tests need reliable auto-waiting, browser tracing, and
modern browser automation.

## Projects

- `srcnew/Brinell.Html`
- `srcnew/Brinell.Html.Playwright`
- `srcnew/Brinell.Blazor`
- `testsnew/Brinell.Html.Tests`
- `testsnew/Brinell.Html.UITests`
- `testsnew/Brinell.Blazor.UITests`

## Rules

- Keep browser and page lifecycle in fixtures.
- Keep selectors inside page objects and controls.
- Prefer Playwright waits and Brinell state checks over fixed delays.
- Save traces under the shared artifact layout when enabled.

## Run

See [Playwright Run Guide](../run/Playwright.md).
