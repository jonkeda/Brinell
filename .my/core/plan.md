# Common Core layer: plan

The work plan and its status. What to build is in [design.md](design.md); the rename table is
in [naming.md](naming.md); per-stack playbooks are in [migration.md](migration.md).

Status: **draft**, 2026-09-20; updated 2026-09-27. **L1 done** (2026-09-26): the leaf layer (the
exceptions, `ScopeReadiness`, the calls, `NearMissSettings`, `CallBudget`) is in Core under
`Bruinet.Core.Calls`, `.Exceptions` and `.Scopes`, with no copies left in MAUI or Html (AD-010;
the review that planned it is [at 7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/core/review-what-can-move-now.md)). That did
step 3. **Next: L2**, then steps 0-2 and 4-5. Html landed on its own `IHtml*` interfaces, so
step 5 is now the Html twin of step 4.

The MAUI and Html designs this plan cites as `../stale-readiness/` and `../html/` are at
[7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/); their rules are the [call-model contract](../../.docs/contracts/call-model.md).

## 1. Goal

Bring the shape MAUI proved (`../stale-readiness/`, done 2026-09-19) into `Bruinet.Core` under
**new names**, so every stack can reuse it without touching the old `IElement<T>` /
`IElementScope<T>` / `IPageObject<T>` / `ITestContext<T>` / `IDriver<T>` interfaces until each
stack is ready to move.

**Same behaviour, different names.** The rules R0-R9 do not change. This project is a shape
project: it creates `ICoreElement`, `ICoreDriver`, `ICoreScope`, `ICorePage`, `ICoreTestContext`,
the calls layer and the four exceptions in `Bruinet.Core.Automation` (or `Bruinet.Core.Common`,
see ?C1), and lets each stack switch from its own bespoke interfaces to those.

## 2. Steps

Working rules:

- **The old Core interfaces stay in place** until every stack has moved. Nothing in
  `srcnew/Bruinet.Core/Interfaces/*.cs` is deleted while the migration is in flight
  ([coexistence.md](coexistence.md)).
- **One stack at a time.** A stack's move is its own step; it does not spill into another
  stack.
- **MAUI first, but only its interface hierarchy.** MAUI already stands alone
  (`IMauiElement`, etc.). Making its interfaces inherit from `ICore*` is small and mechanical -
  it proves the shape lands before the other stacks start.
- **Rebuild the sample apps** when their control bases change. Same rule as MAUI and Html.

Status values: `todo`, `doing`, `done`, `blocked`.

| Step | Status | Contents | Done when |
| --- | --- | --- | --- |
| **L2. Busy signal** | todo | One Core busy-signal type for both meanings of a missing sentinel: Core's `BusySignalPolicy` (`Disabled`, `Required`; missing is a configuration error) and Html's `BusySignal` (`Ignore`, `Optional`, `Required`; missing means not loaded yet, because a Blazor app sets it when it becomes interactive). A third value, or a flag for what `missing` means. Decide it with the pages | `BusySignalPolicy` and `BusySignal` are one type |
| **0. Decide the home** | todo | Decide **?C1** (`Bruinet.Core.Automation` namespace vs. a new `Bruinet.Common` project). Decide **?C2** (do the capability interfaces (`IClickable`, `IEditableText`, ...) get `Core` prefixes too, or stay in `Bruinet.Core.Interfaces` because the generator uses them). Write both decisions into `design.md` and this plan | Both decisions written, and neither the plan nor the design has any `?C1`/`?C2` left |
| **1. Land `ICoreElement` and `ICoreScope`** | todo | Add `ICoreElement`, `ICoreScope`, the extension helpers (`CoreElementExtensions`, `CoreScopeExtensions`), and `Bruinet.Core.Automation` (or the new project). No stack uses them yet. The existing `IElement<T>` and `IElementScope<T>` are **unchanged** | Solution builds. All existing tests still pass. `srcnew/Bruinet.Core/Interfaces/*.cs` is untouched |
| **2. Land `ICoreDriver` and `ICoreTestContext`** | todo | Add the driver and context. Add `ICorePage`, `ICoreContainer`, `ICoreCollection<TItem>` and `ICoreItem<TCollection>`. Add `CoreScopeReadiness` and `CoreScopeReadinessState`. Add the four exceptions (`CoreStaleElementException`, `CoreElementNotReadyException`, `CoreScopeNotReadyException`, `CoreAppUnavailableException`) | Solution builds; MAUI's `IMauiElement` does **not** yet inherit `ICoreElement` |
| **3. Land the calls layer** | done (L1, 2026-09-26) | `Bruinet.Core.Automation.Calls/`: `ControlCall`, `Poller`, `Deadline`, `AttemptContext`, `Observation`, `ObservationLog`, `Confirmer`, `Confirmation<T>` copied from MAUI's `Bruinet.Maui/Calls/`, retyped on `ICoreTestContext`. `internal` today; opened selectively as MAUI and other stacks reference them | Solution builds; MAUI's `Bruinet.Maui/Calls/` still compiles and its callers still work |
| **4. MAUI inherits `ICore*`** | todo | `IMauiElement : ICoreElement`, `IMauiDriver : ICoreDriver`, `IMauiElementScope : ICoreScope`, `IMauiPage : ICorePage`, `IMauiTestContext : ICoreTestContext`. Anything on the MAUI interfaces that has a `ICore*` equivalent moves up (kept on `ICore*`, removed from `IMaui*`). The MAUI exceptions become type aliases or subclasses of `Core*` exceptions. MAUI's calls layer becomes a thin using of `Bruinet.Core.Automation.Calls`; the copies in `Bruinet.Maui/Calls/` are deleted | `Bruinet.Maui.Tests`, `Bruinet.Maui.Uat.Tests`, `Bruinet.Maui.UITests` (a chosen subset, then full) green with no behaviour change; grep finds no `Bruinet.Maui.Calls` type outside `Bruinet.Maui` |
| **5. Html inherits `ICore*`** | todo | As step 4, for Html: `IHtmlElement : ICoreElement`, `IHtmlDriver`, `IHtmlElementScope`, `IHtmlTestContext` on their `ICore*` bases. Before the bases move, close the R8 gap in Html's `ControlBase`: a shared base must offer the signatures the generator emits | Html on `ICore*`; `Bruinet.Html.Tests`, `Bruinet.Html.Uat.Tests`, `Bruinet.Blazor.*.Tests` green |
| **6. NativeAndroid** | todo | **Now the shape of step 4** (2026-10-07): NativeAndroid stands alone on its own `INativeAndroid*` interfaces, the Core calls layer and generated controls, as MAUI and Html do ([`../androidnative/`](../androidnative/README.md)). Its move to `ICore*` is the same mechanical change as MAUI's: change the interfaces' base to `ICore*`, move the shared members up | `Bruinet.NativeAndroid.Tests` (66) and `Bruinet.NativeAndroid.UITests` (30, on the emulator) green against the `androidnative/` baselines; no regression |
| **7. WPF and WinForms** | todo | Same as step 6. FlaUI's `UIA_E_ELEMENTNOTAVAILABLE`, `InstanceKey` from the runtime id, and both stacks' `PageObjectBase` `IsReady(int?)` handling reused. `PageLoadException` from these stacks becomes `CoreScopeNotReadyException`. **WPF baseline** (memory: 2 failing login tests before any change) recorded first | `Bruinet.Wpf.Tests`, `Bruinet.WinForms.Tests` green against baseline; no new regressions |
| **8. Stride** | todo | Same as step 6. The pipe-based `Bruinet.Stride.Automation` handshake and the shared pipe name (memory: pipe orphan-kill rule) stay. `PageObjectBase.IsReady` moves from `WaitHelper` polling to `CoreScopeReadiness` | `Bruinet.Stride.Tests` green against baseline |
| **9. UAT flip** | todo | `Bruinet.Uat`'s `UatDiscovery` and `Bruinet.Core.Composition`'s `TestComposition` recognise `ICorePage` in addition to the old `IPageObject`. When the last stack has moved, the recognition of `IPageObject` is removed (?C7) | UAT tests green against baseline for every stack |
| **10. Delete the old shapes** | todo | This step is the delete-only follow-up. `IElement<T>`, `IElementScope`/`IElementScope<T>`, `IPageObject`/`IPageObject<T>`, `IDriver<T>`, `ITestContext<T>`, `IContainerControl<T>`, `IContainerObject<T>`, `ItemContainer<T>`, `ICollectionObject<T, TItem>`, `IPagedScope<TPage, TElement>`, `ControlObjectBase<TScope>`, `PageReadinessSnapshot`/`PageReadinessState`, `BusySignalPolicy`, `PageLoadException` are removed. Anything with no consumer left goes | Solution builds; grep finds no reference to any deleted type; every stack green |
| **11. Write it down** | todo | AD-012 (or next free number) - "one shape across stacks, with `ICore*` as the base". The two per-stack skills (`maui-control` / `maui-ui-test`) and the ones added by the Html plan (`html-control` / `html-ui-test`) get their control-base names updated to `ICoreControlObject<TScope>` (or whatever ?C2 settles on). `AGENTS.md`, `.github/copilot-instructions.md`, `docs/architecture/` refreshed; `CHANGELOG.md` lists the deletes | Docs merged |

### 2.1 Step notes

Filled in as steps finish, same shape as `../stale-readiness/plan.md` section 2.1.

## 3. Open decisions

Deferred until step 0 decides them. Kept here so review does not re-ask them.

| # | Question | Default |
| --- | --- | --- |
| ?C1 | Where does the new layer live: a new namespace `Bruinet.Core.Automation` under `srcnew/Bruinet.Core`, or a new project `Bruinet.Common` referenced by every stack | `Bruinet.Core.Automation` under the existing project. A new project means every csproj gets a `ProjectReference`, and Core-consumers upstream get a new dependency. A namespace is cheaper and reversible |
| ?C2 | Do the capability interfaces (`IClickableControlObject<TScope>`, `IEditableTextControlObject<TScope>`, `IToggleControlObject<TScope>`, ...) get `Core` prefixes too, or stay under their existing names | **Prefix them.** They become `ICoreClickable<TScope>`, `ICoreEditableText<TScope>`, `ICoreToggle<TScope>` etc., under `Bruinet.Core.Automation`. The generator's discovery matches by convention, not by exact type name (`../stale-readiness/plan.md` 2.1 step 1 shows UAT discovery goes through `[TestPage]` attribute-based recognition, not name matching). Revisit if the generator turns out to hard-code names |
| ?C3 | `ICoreElement` as generic (`ICoreElement<TSelf>`) or non-generic | **Non-generic.** MAUI dropped the `TSelf` type parameter when it went to `IMauiElement`, and the calls layer never needed it. The old `IElement<TSelf>` had the parameter for fluent returns only, which the calls layer no longer uses |
| ?C4 | `ICoreScope` typed on element (`ICoreScope<TElement>`) or not | **Not typed on element.** Every scope returns `ICoreElement`. Per-stack scopes (`IMauiElementScope`, `IHtmlElementScope`) type on their per-stack element type, and the platform-specific member set is *there*, not on `ICoreScope`. This is exactly the split MAUI accepted in `../stale-readiness/design.md` 4.2 |
| ?C5 | `ICoreDriver` includes `IDiagnosticDriver`, or does that stay separate | `ICoreDriver : IDiagnosticDriver, IDisposable` (as MAUI's `IMauiDriver` does today). Nothing else consumes `IDiagnosticDriver` on its own |
| ?C6 | `ICoreTestContext` extends non-generic `ITestContext` (the Core one that already exists), or the whole context moves too | **Extends the existing `ITestContext`.** `ITestContext` is a non-generic timeouts + logger + navigation + screenshots + reset surface that stacks already share. It never had the `TElement` type parameter (that lives on `ITestContext<TElement>`). Keeping it means the migration is only the *scope* side; the *context* side is unchanged |
| ?C7 | UAT `TestComposition` recognizing both `IPageObject` (old) and `ICorePage` (new) - how long does the both-recognition path last | Ends with step 10 (the delete). If a test class is annotated `[TestPage]` and inherits either interface, it is picked up. Same for `IElementObject` and `ICoreElementObject`. Removed the moment the old interfaces go |
| ?C8 | Does the `Core*` exception set replace `Bruinet.Core.Exceptions.PageLoadException` outright, or add itself alongside | **Add alongside**, delete `PageLoadException` in step 10. Reasoning: the WPF, WinForms and Stride PageObjectBases still throw `PageLoadException` until their stack moves (steps 7-8); a delete-first breaks them for weeks. `ElementNotFoundException`, `AssertionException`, `WaitTimeoutException` and `BruinetException` stay - they are shared |
| ?C9 | The old `PageReadinessSnapshot` / `PageReadinessState` and `BusySignalPolicy` in `IPageObject`'s default `ProbeReadiness` - keep, deprecate, delete | Keep in place until step 10. The default is only used by stacks that have not moved yet; once every stack has moved, none of them use it |

## 4. Baselines

TODO. Recorded before step 4 starts and updated before each stack's step, as a *per-stack*
line: the passing/failing test counts to compare against. No baseline is edited after it is
written.

- `Bruinet.Maui.Tests`, `Bruinet.Maui.UITests` (Windows and Android subsets): the numbers from
  `../stale-readiness/plan.md` step 8.
- `Bruinet.Html.Tests`, `Bruinet.Html.UITests`: from `../html/plan.md` baseline (still to be
  recorded).
- `Bruinet.Wpf.Tests`, `Bruinet.Wpf.UITests`: including the 2 known-failing login tests.
- `Bruinet.WinForms.Tests`, `Bruinet.WinForms.UITests`.
- `Bruinet.NativeAndroid.Tests`: 66 passed (2026-10-07, after `../androidnative/`). "Range 6/16, Picker 0/8", once noted here, are MAUI Android UI numbers, not NativeAndroid's.
- `Bruinet.Stride.Tests`, `Bruinet.Stride.UITests`.
- `Bruinet.Uat.Tests`, per-stack UAT suites.

## 5. Verification

Commands are from the Bruinet root.

```powershell
dotnet build srcnew\Bruinet.sln -v:minimal /nr:false
dotnet test testsnew\Bruinet.Core.Tests\Bruinet.Core.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinet.Uat.Tests\Bruinet.Uat.Tests.csproj -v:minimal /nr:false
```

Per-stack test commands live in the migration playbook for that stack (see
[migration.md](migration.md)).

**Match the scope to the change.** Same rule as `AGENTS.md`. A step that only adds interfaces
(steps 1-3) needs a broad build and the Core-only tests, no UI suite. A step that moves a stack
needs that stack's test project + one UI area first, then wider suites, then the full suite at
the end of the step.

## 6. Not in scope

- **Rewriting Core's non-interface types** (`Locator`, `LocatorStrategy`, `BruinetException`,
  `TimeoutSettings`, `ITestLogger`, `LogResult`, `TextInputMethod`). None of them need renaming;
  they are already the shared surface.
- **Behaviour changes.** The rules R0-R9 are the ones MAUI proved. Anything that changes what a
  call *does* on any stack is a separate project.
- **A new capability set.** `IClickableControlObject<TScope>` etc. keep their behavioural
  contract; only the *name* changes (?C2). Adding a new capability is out.
- **Renaming the runtime abstractions** (`IDiagnosticDriver`, `IScreenshotService`,
  `AbsenceTolerantAttribute`, `SkipGenerationAttribute`, `GenerateComparisonsAttribute`,
  `FluentReturnAttribute`). Not renamed - nothing in them belongs to a *stack*.
