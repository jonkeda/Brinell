# The bUnit Head

A Blazor suite can run its page objects, controls and feature files without a browser: bUnit
renders the app's components in the test process, and Brinell drives them through the same
`IHtmlTestContext`. A run picks its head with `BRINELL_HEAD`; the browser stays the judge at every
gate. The decision is [AD-030](../../.docs/decisions/ad-030-a-blazor-suite-runs-without-a-browser.md).

## When To Use It

During development, to run the tests a change reaches in seconds. Brinell's Blazor suite takes
2 s on bUnit and 39 s in a browser, with the same 35 tests; a call costs about 0.2 ms instead of
about 40 ms.

At a gate, run the browser head. bUnit has no layout, no style sheets and no script, so a page a
style sheet hides, or a handler bound in JavaScript, can pass on bUnit and fail in a browser.

## Running It

```bash
BRINELL_HEAD=bunit dotnet test testsnew/Brinell.Blazor.UITests
```

Unset, or any value but `bunit`, is the browser. In PowerShell: `$env:BRINELL_HEAD = 'bunit'`.

## Giving A Suite A bUnit Head

1. Reference `Brinell.Html.Bunit` and the app's project (its assembly, not only its build).
2. Make the context where the suite makes its browser context, by `WebHeads.IsBunit`:

```csharp
_context = WebHeads.IsBunit
    ? new BunitTestContext(new BunitTestContextOptions
    {
        RootComponent = typeof(Routes),        // the app's router, so pages open by address
        ConfigureServices = services =>         // the app's own services
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<DataService>();
        },
    })
    : await BlazorTestContext.CreateAsync(options);
```

A suite on `HtmlTestFixtureBase` overrides `CreateBunitContext()` instead. Page objects and
scenarios need no change. Addresses are under `http://localhost`.

## What The Context Gives

| Option | Meaning |
| --- | --- |
| `RootComponent` | What is rendered: the app's `Routes`, or one component on its own |
| `ConfigureServices` | The app's services, registered before the root renders |
| `BaseUrl` | Where the context starts; empty is the root |
| `User`, `Roles` | Signed in through bUnit's test authorization; signed out by default |
| `JsInterop`, `ConfigureJs` | Loose (every script call answered with a default) or Strict, and planned answers |

- **Sign-in:** the test authorization replaces the app's own provider, so arrange the account rather
  than signing in; Identity's sign-in pages are Razor Pages and do not render.
- **Readiness:** the context reads the layout's `brinellBusy.set` calls, so `BlazorPageObjectBase`'s
  busy signal works as in a browser.
- **A "screenshot"** on this head is the rendered markup, saved as `.html`.
- **Results:** a scenario's `__result.json` says `"head": "bunit"`; the call log is `calls-bunit-….csv`.

## What Needs A Browser

Mark a test `[BrowserFact("why")]` and a scenario `@browser`: on the bUnit head they are skipped with
the reason, so the run still lists them.

| Needs a browser | Why |
| --- | --- |
| Layout and style: `Location`, `Size`, `Rect`, `GetCssValue`, `ClickAt`, `Hover`, `Swipe` | No layout engine |
| Script: `Evaluate`, a handler bound in JavaScript | No JavaScript engine |
| Frames: `FrameScope` | An iframe is a document only a browser loads |
| Recordings and narrated videos | No pixels |
| XPath and accessible-name locators | Not supported; use a test id |

A browser-only member ends its call at once with `BrowserOnlyException` naming the member, rather than
waiting out the budget.

## What Behaves Differently

- **Visible** means no `hidden` attribute and no inline `display:none` or `visibility:hidden` on the
  element or its parents. A class that hides through a style sheet still counts as visible.
- **Click** runs the element's handler; a submit button without one submits its form; a link
  navigates. A disabled control gets no click.
- **A handler that throws** ends the call with `AppUnavailableException`, as a browser's circuit ends;
  a throw after the handler's first `await` is reported on the next look at the page.
- **`Fill`** raises the input event where the element listens to one, then the change event; with
  `TextInputMethod.Keys`, `SendKeys` also raises key events per character.
