# Playwright Platform Guide

Use Playwright when web tests need reliable auto-waiting, browser tracing, and
modern browser automation.

## Projects

- `src/Bruinet.Html`
- `src/Bruinet.Html.Playwright`
- `src/Bruinet.Blazor`
- `tests/Bruinet.Blazor.Tests`
- `tests/Bruinet.Html.UITests` (the UI tests and the UAT suite)
- `tests/Bruinet.Blazor.UITests` (the UI tests and the UAT suite)
- `samples/Todo/tests/Bruinet.Samples.Todo.Web.UITests`, `samples/Todo/tests/Bruinet.Samples.Todo.Web.Uat`

## Rules

- Keep browser and page lifecycle in fixtures.
- Keep selectors inside page objects and controls.
- Wait through Bruinet members (`WaitX`, `AssertX`), never with fixed delays. A member is one
  synchronous call with one budget, as on MAUI; there is no async API.
- Html controls have MAUI's members and names (`AssertTextContains`, `SetText`, `Enter`,
  `SelectByText`, `AssertSelectedText`), so a test or a UAT scenario reads the same on both.
- Save traces under the shared artifact layout when enabled.

## Run

See [Playwright Run Guide](../run/Playwright.md).
