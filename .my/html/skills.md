# Skills and instruction files

Docs that ship with `plan.md` step 9. Nothing here is new content invented for Html; each file
below is a targeted adaptation of what MAUI already has. Where the MAUI file is right for Html
too, this document says "reuse verbatim" instead of writing a new one.

## New: `.github/skills/html-control/SKILL.md`

Modelled on [`.github/skills/maui-control/SKILL.md`](../../.github/skills/maui-control/SKILL.md).

The workflow is the same shape (choose the kind, probe the platform, add the sample, write the
template, generate and build, prove it). The differences below are what the Html skill's
`SKILL.md` and `references/*.md` restate.

### Frontmatter

```yaml
---
name: html-control
description: Create or extend a Brinell Html ControlObject - a simple control, a container,
  a component with named parts, or a collection with rows - in the .tpl.cs / .gen.cs generator
  format. Use when asked to add, write, or extend a control object, container, component,
  collection, or part for an HTML or Blazor control, or when a Html UI test needs a control
  member that does not exist yet.
---
```

### Differences from `maui-control`

1. **Platform probe.** No FlaUI / UI Automation walk. The probe is a Playwright
   `page.pause()` in a scratch script, or DevTools' Elements panel, and captures: the tag
   (`button`, `input`, `select`, ...), the ARIA role, the `data-testid`, and the attributes the
   control's Core methods read (`disabled`, `aria-disabled`, `aria-checked`, `value`,
   `data-busy`).

2. **Sample element.** An element in `samples/Brinell.Samples.Blazor.App/Components/Pages/` with
   a `data-testid` and a status label that shows the visible effect of each action
   (`sample-app.md` item 1).

3. **Routes reference (`references/routes.md`).** For Html, the route order is:

   | Route | When | How |
   | --- | --- | --- |
   | DOM property or attribute | Reading state (`Enabled`, `Checked`, `Value`, `Text`) | `element.GetDomProperty(...)` or `element.GetAttribute(...)` |
   | ARIA role/state | Reading a semantic state that has no HTML attribute (`aria-expanded`, `aria-selected`) | `element.GetAttribute("aria-...")` |
   | DOM action | Writing (`Click`, `Fill`, `Check`, `SelectOption`, `Focus`) | `element.Click()`, `element.Fill(...)` |
   | Evaluate | A composite read the driver has no primitive for | `element.Evaluate<T>("...")` |
   | Pointer input | A gesture the driver's action verbs cannot express | `element.Swipe(...)`, `element.Hover()` |

   Every route goes through a `Live<T>` wrapper so `TargetClosedError` becomes
   `AppUnavailableException` (`design.md` section 5). `Evaluate` is the last resort, not the
   first.

4. **Generator contract (`references/generator-contract.md`).** Reused unchanged from MAUI.
   `Is*Core` -> `IsX / WaitX / AssertX`, `Get*Core` -> `GetX / WaitX / AssertX`,
   `Set*Core(element, T? value, int? timeoutMs = null)` -> `SetX`, any other `*Core` -> an
   action returning the scope. `[GenerateComparisons]`, `[AbsenceTolerant]`,
   `[SkipGeneration]` as-is.

5. **Simple control, container, component, collection references** parallel the MAUI ones but
   with Html base classes: `ViewBase<TScope>`, `ContainerObjectBase<TParent, TSelf>`,
   `ComponentObjectBase<TScope, Foo<TScope>>`, `CollectionObjectBase<...>` +
   `ItemObjectBase<...>`. Only the class names change; the rules do not.

6. **Locators.** Prefer `Locator.ByAutomationId("data-testid-value")` for elements the sample
   controls. `Locator.ByCss(...)` for CSS-only selectors. The `IsRawSelector` heuristic in
   `ControlBase` still exists but is a fallback; a control's constructor should pick the
   locator kind explicitly.

7. **What goes into `.tpl.cs` for a Blazor-specific control.** Same rules as MAUI: behaviour
   in `protected virtual *Core` methods, timeouts through `int? timeoutMs = null`, the
   generator writes the public members. A Blazor control that binds `oninput` needs `Fill`
   (which triggers `input`), not `SendKeys` (which triggers `keydown`/`keyup` and may not fire
   the binding).

### File layout

```
.github/skills/html-control/
├── SKILL.md
└── references/
    ├── simple-control.md
    ├── container.md
    ├── component.md
    ├── collection.md
    ├── generator-contract.md    (may be a copy-with-diff of the MAUI one)
    └── routes.md                (Html-specific route table above)
```

## New: `.github/skills/html-ui-test/SKILL.md`

Modelled on [`.github/skills/maui-ui-test/SKILL.md`](../../.github/skills/maui-ui-test/SKILL.md).

Same rules: tests go through page objects and control objects; page objects own page structure;
controls own repeated interaction. No `Locator` construction in a test method; no direct
`IHtmlElement` in a test; no `TryFindElement` or `FindElements` in a test.

### Frontmatter

```yaml
---
name: html-ui-test
description: Write or fix Brinell Html UI tests and the page objects, app containers and
  collections they use, going only through page objects and ControlObjects - never the driver,
  IHtmlElement, or Find* calls. Use when asked to add, write, extend, or fix an Html UI test, a
  test page object, or a test for a Blazor or plain HTML control in testsnew/Brinell.Html.UITests,
  or a control unit test in testsnew/Brinell.Html.Tests.
---
```

### Forbidden APIs

Same shape as MAUI's `references/forbidden-apis.md`, adapted:

- no `Context.Driver` (unless the test is a driver / bridge framework test);
- no `IHtmlElement` variables, parameters, returns or members;
- no `FindElement` / `FindElements` / `TryFindElement`, `ContainerRoot`, `PageRoot`;
- no `Locator` construction in a test method;
- no `Playwright` types (`IPage`, `ILocator`, `IElementHandle`, `IFrame`) in a test.

The one exception is the driver and bridge framework tests in
`testsnew/Brinell.Html.Tests/Driver/`, which exist to prove the driver itself.

### The forbidden-fixture rule

Same as MAUI: fixture constructors are for runtime setup (browser launch, base URL, seed data).
Page ownership belongs on the test class or in page-object composition. No ad-hoc cleanup or
back-navigation loops inside a test method.

### Synchronization rules

Same as MAUI's, restated for Html:

- No arbitrary sleeps or longer waits to fix a test.
- Wait for concrete UI state: page loaded, element visible/enabled/selected/gone, text or
  value changed, `data-busy = "false"`, a request observed by Playwright's `RouteAsync`.

### File layout

```
.github/skills/html-ui-test/
├── SKILL.md
└── references/
    ├── forbidden-apis.md
    ├── page-objects.md
    ├── control-usage.md
    └── fixtures.md
```

## Update: `.github/skills/convert-control/SKILL.md`

Extend its scope to Html: add a paragraph saying the same rules apply to
`srcnew/Brinell.Html/Controls/**` and `srcnew/Brinell.Blazor/Controls/**`. The generator
contract does not change; the base classes to inherit from do (`ViewBase<TScope>` instead of
`ControlObjectBase<TScope>` etc., matching `design.md` section 8).

No new skill needed. One paragraph in the existing skill covers it.

## Update: `AGENTS.md`

Two changes.

1. In the "Read First" list, add:

   > - Html and Blazor: [docs/architecture/html.md](../../docs/architecture/html.md) (new,
   >   ships in step 9)

2. In the "Skills" section, add:

   > - [html-control](.github/skills/html-control/SKILL.md) - create or extend a Brinell Html
   >   ControlObject.
   > - [html-ui-test](.github/skills/html-ui-test/SKILL.md) - write Brinell Html UI tests and
   >   page objects.

3. In "Never In Tests", add the Html-side rules from the forbidden APIs section above (mirrored
   from MAUI).

4. In "Verification", add:

   ```powershell
   dotnet test testsnew\Brinell.Html.Tests\Brinell.Html.Tests.csproj -v:minimal /nr:false
   dotnet build srcnew\Brinell.Html.Playwright\Brinell.Html.Playwright.csproj -v:minimal /nr:false
   ```

## Update: `.github/copilot-instructions.md`

Two changes.

1. In "Skills", add the two new skills to the list (`html-control`, `html-ui-test`).

2. Under "Build And Test", add the Html test project commands (as above).

The architecture rules, synchronization rules, UI interaction rules and error-handling rules
all apply to Html as-is: Html is a Brinell stack, and the rules were never MAUI-specific in the
first place.

## Update: `docs/architecture/`

A new file `docs/architecture/html.md` mirroring `docs/architecture/structure.md`'s treatment of
MAUI: layers, driver, the frame scope, the Blazor busy signal, the sample project. Ships with
`plan.md` step 9.

## Update: `docs/architecture/testing.md`

If this file discusses per-stack testing conventions, add a Html section that names:

- the sample app (`Brinell.Samples.Blazor.App`);
- the test projects (`Brinell.Html.Tests`, `Brinell.Html.Uat.Tests`, `Brinell.Html.UITests`,
  `Brinell.Blazor.*.Tests`);
- the fixture (`BlazorSampleTestBase`) and the pattern for launching Playwright once per class.

## Update: `docs/controls/index.md`

Add an "Html controls" section pointing at the Blazor and plain-Html controls, mirroring the
MAUI section.

## Update: `CHANGELOG.md`

Ships in step 9. Lists everything itemized in `../stale-readiness/design.md` section 10 (the
Removed / Renamed lists), adapted for Html - the Core interfaces Html no longer implements, the
timeouts removed, the exception replacements, the `catch { }` sites deleted.

## Update: `docs/specs/`

If there is a spec that covers Brinell.Html's contract with tests, its status changes to
`superseded` and a new spec is written for the new interfaces. If there is no such spec, one is
written (status: `active` at merge, `implemented` when `plan.md` step 9 is done).

## Order

The docs land in `plan.md` step 9, together, so no partial state is documented. Skills are the
priority: the `maui-control` and `maui-ui-test` skills prevented a whole class of mistake while
MAUI was being changed, and the same holds for Html.
