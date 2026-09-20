# Sample app updates

The Blazor Server app at `samples/Brinell.Samples.Blazor.App` is the Html sample. It plays the
role `Brinell.Samples.Maui.App` does for MAUI: it is what the UI tests launch, and it is where
the "known bugs" for R0 proofs live.

Changes below are numbered; `plan.md` step 8 lists which items land with which step.

## 1. A common status label

Every interactive page grows a small status region at the top:

```razor
<div data-testid="page-status"
     data-status-code="@_status.Code"
     data-status-detail="@_status.Detail">
    @_status.Detail
</div>
```

`_status` starts as `Code = "Ready"`. Handlers update it. It is analogous to the MAUI sample's
"status label that shows the visible effect of each action" (from `.github/skills/maui-control/SKILL.md`).
UI tests read `data-status-code` to assert the visible effect of an action.

**Ships with:** step 2.

## 2. The R0 toolbar-cancel analog on Login

Give the login page a controllable "the Login command never re-enables" bug, behind a query
string:

- `?login-bug=none` (default): normal behaviour.
- `?login-bug=stuck-disabled`: after the first click, `CanLogIn` stays false forever, even
  after the field values change.
- `?login-bug=stuck-enabled`: the button reports `disabled` in the DOM but Playwright's
  actionability check still finds it clickable.

The test for R0 (`plan.md` step 8) launches with `?login-bug=stuck-disabled` and asserts:

- `Click` throws `ElementNotReadyException(NotReadyReason.Disabled)` within `timeoutMs: 2000`;
- the message names `data-testid='login-btn'`;
- `Invoke` is called `Times.Once` on a mocked click sink.

Restoring `?login-bug=none` puts the page back to normal for the rest of the suite.

**Ships with:** step 2 (the switch), step 4 (the calls layer), step 8 (the R0 proof run).

## 3. A page-level busy signal

A one-file JS module at `samples/Brinell.Samples.Blazor.App/wwwroot/brinell-busy.js`, loaded
from the sample host's layout, wraps `blazorInvoke` (Blazor's client-side entry) so that
`document.body.dataset.busy = 'true'` while any interactive render is running, and `'false'`
after. The exact hook depends on the Blazor version and is a sample-app implementation detail;
the contract is:

- `<body data-busy="true">` while a Blazor call is in flight, `"false"` otherwise;
- the attribute is written before the call starts and cleared after the call resolves, so a
  test can see it between clicks.

`HtmlPageObjectBase` reads this attribute in `ProbeReadiness` (design 7.2). The alternative -
absence of a spinner class - only works for pages that have one.

**Ships with:** step 5 (scope readiness).

## 4. A virtualized list with row recycling

A new page `/todos` with:

- an in-memory `List<TodoItem>` of ~200 items;
- Blazor `<Virtualize>` for the list;
- **each row has `data-item-key="@item.Id"`** and `data-testid="todo-row"`;
- each row has an inline `<button data-testid="delete-btn">Delete</button>` and a
  `<span data-testid="title">@item.Title</span>`;
- a top toolbar with `Add`, `Reload` and `Recycle` buttons. `Recycle` swaps the order of items
  after a short delay, so the same DOM node hosts a different item.

The test for R0 (`plan.md` step 7) picks row 3, presses `Recycle`, waits for the swap, then
asks the row scope to `Click Delete` and asserts:

- the call fails with `StaleElementException`;
- the message says `row 3 now shows another item`;
- **no other item was deleted** (the model check).

**Ships with:** step 7 (collections).

## 5. A component that re-renders every N ms

A new page `/nervous` with:

- a component whose root has a `[key]` attribute that flips every 100 ms, so Blazor tears down
  and rebuilds it;
- a `data-testid="counter-value"` that counts stable renders (not re-renders).

The test for near-misses (`plan.md` step 6) opens `/nervous` and asserts:

- a `GetText` on `counter-value` **passes** (the framework re-resolves and reads);
- the log contains a `LogExit(..., LogResult.Warning, ...)` entry starting with `near-miss:`;
- the warning message lists at least 3 replacements.

**Ships with:** step 6 (`Confirm` + near-miss).

## 6. A `StateContainer` analog

A page `/state` that shows one of four states, controllable through the query string:

- `?state=loading` (a spinner region);
- `?state=empty` (an empty message);
- `?state=loaded` (the content, with a `data-testid="content-body"`);
- `?state=error` (an error region).

Used to test container `ProbeContentReadiness` (`design.md` 7.2). The container reads the
current state and reports `ContentNotReady` for `loading`, `Empty` (or `Ready` with a detail)
for `empty`, `Ready` for `loaded`, and a `Detail = "error"` observation for `error`.

**Ships with:** step 5.

## 7. A frame page

A page `/frames` with two `<iframe>`s: one same-origin (`/frames/inner`) and one cross-origin
(a data URL). Used to test `FrameScope` (`design.md` 7.5, `differences-from-maui.md` item 4).

**Ships with:** step 5.

## 8. Test data and reset

Every mutating page (`/todos`, `/state`) exposes a hidden endpoint or a top-of-page button
`data-testid="reset"` that puts the page back to its seed state. The UI test fixture calls it
between tests instead of navigating and starting a new session, matching the MAUI sample's
`ResetAppState`.

**Ships with:** step 7.

## 9. No `AutomationId` outside `data-testid`

Blazor Server does not have an `AutomationId` property in the MAUI sense. The convention used
today (`data-testid`) stays. Every new element added by this project uses `data-testid`; the
`ResolveLocator` heuristic in `ControlBase.IsRawSelector` is not touched (?A7).

## 10. What does not go into the sample

- No screenshot golden-files. Playwright can, but this project's proofs are functional.
- No visual regression tests.
- No custom Playwright bindings.
- No mobile-emulation profiles beyond the ones a Playwright config already covers.

## Verification the sample changes work

For each item above, a matching test in `testsnew/Brinell.Html.UITests/Tests/Sample/`:

```powershell
dotnet build samples\Brinell.Samples.Blazor.App\Brinell.Samples.Blazor.App.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj `
    --filter "FullyQualifiedName~Tests.Sample" -v:minimal /nr:false
```

The sample tests are what fails first when the design's rules do not hold. They are not the
whole `Brinell.Html.UITests` suite; the fuller suite runs in `plan.md` step 8.
