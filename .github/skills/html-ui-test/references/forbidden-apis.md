# Forbidden in Html tests

In test methods, page objects' public members, app containers and test helpers - even for a
quick fix:

| Forbidden | Use instead |
| --- | --- |
| `Context.Driver`, any `IHtmlDriver` member | A page-object or control member |
| `IHtmlElement` variables, parameters, returns or members | A control, container or row object |
| `FindElement`, `FindElements`, `TryFindElement`, `ContainerRoot`, `PageRoot` | A control member; `Try*` members for "now" |
| `Locator` construction in a test method | A page-object property |
| Playwright types: `IPage`, `ILocator`, `IElementHandle`, `IFrame`, `IBrowser` | Page objects; `FrameScope` for frames |
| bUnit and AngleSharp types: `BunitContext`, `IRenderedComponent`, `cut.Find`, `IElement`; `BunitTestContext` members beyond the fixture | Page objects; the fixture alone makes the context |
| `Thread.Sleep`, `Task.Delay`, a `while` loop that polls | A waiting `Assert*`/`Wait*` member |

Why: every lookup and wait inside a control is one call with one budget (the call model,
`.docs/contracts/call-model.md`). Code that reaches past it waits on its own clock, reads the
page at a moment nothing waited for, and turns an app bug into a flaky test.

The one exception is the drivers' own tests, which exist to prove the driver:
`testsnew/Bruinell.Html.Tests` (`PlaywrightErrorMappingTests` and the pins) and
`testsnew/Bruinell.Html.Bunit.Tests` (`MemberTests`, `ContextTests`).
