# Moving the call model down to Core: first draft

Status: **draft**, 2026-09-19. Written in step 9 of [plan.md](plan.md). This is a separate project
(design 4.5, decision X1: it starts after step 9, as its own plan). Nothing here is decided yet.

## 1. Where things stand

MAUI runs the new call model on its own interfaces, and Core is unchanged (design R9, AD-010).
Step 8 proved the model on MAUI:
- Windows: the full suite green.
- Todo: 5/5 without the toolbar workaround.
- R0 holds: TOD.04.4 still fails against the re-introduced bug.
- Android: no worse than its baseline.

The other stacks still use Core's shapes. Rough count of uses of the Core interfaces that MAUI
left (`IElement<T>`, `IElementScope`, `IPageObject`, `IDriver<T>`, `ControlObjectBase`,
`ITestContext<T>`, `IContainerObject<T>`, `IContainerControl<T>`, `WaitHelper`,
`PageLoadException`), counted with grep on 2026-09-19:

| Stack | Uses | Files in the project |
| --- | ---: | ---: |
| `Brinell.Wpf` | 37 | 36 |
| `Brinell.WinForms` | 31 | 40 |
| `Brinell.Html` | 16 | 49 |
| `Brinell.Stride` | 13 | 35 |
| `Brinell.NativeAndroid` | 11 | 74 |
| `Brinell.Blazor` | 2 | 25 |
| `Brinell.Html.Playwright` | 1 | 3 |

Core itself also uses them: `TestComposition` and `Brinell.Uat`'s `UatDiscovery` find pages
through `IPageObject`, and `ControlObjectBase` is typed on `IElementScope`.

## 2. What moves to Core

As **replacements, not additions**: at the end, Core has one element, one driver, one scope and
one page contract.

1. **Interfaces:**
   - the shapes of `IMauiElement`, `IMauiDriver`, `IMauiElementScope`, `IMauiPage` and
     `IMauiTestContext`, without their MAUI-only members;
   - `MauiElementExtensions` merges back into `ElementGeometryExtensions` and
     `ElementScopeExtensions`.
   - Candidates to stay in MAUI: `Platform`, `ShellChrome`, `TryFindByScrolling`, the bridge
     verbs.
2. **Exceptions:** `StaleElementException`, `ElementNotReadyException` (with `NotReadyReason`),
   `ScopeNotReadyException`, `AppUnavailableException`. `PageLoadException` goes with the last
   stack that throws it.
3. **Readiness:** `ScopeReadiness` and `ScopeReadinessState`, replacing `PageReadinessSnapshot`
   and `PageReadinessState`. `BusySignalPolicy` stays Core's.
4. **Calls layer**, when a second stack needs it:
   - `ControlCall`, `Poller`, `Deadline`, `AttemptContext`, `Observation`, `ObservationLog`,
     `ScopeGate`, `Confirmer`;
   - `Confirmation<T>`, which is public today in `Brinell.Maui.Controls.Base`.
   - They are internal to MAUI today, so moving them is a visibility decision, not a rewrite.
5. **The `ActOnce` contract** (step 8): an `ElementNotReadyException` from a Core method means it
   did not act. This belongs in Core's control documentation, with AD-009.

## 3. Order

The design gives the order: NativeAndroid, then WPF and WinForms, then Html (Playwright, Blazor),
then Stride. Notes per stack, from what is known now:

| Stack | What carries over | Known work |
| --- | --- | --- |
| **NativeAndroid** | The Appium driver side: `AppiumErrors`, the `Live` wrapper, session-gone handling | `AndroidContainerBase.IsReady` asks its parent and then the root: the chain exists, as booleans. It becomes `ProbeReadiness`. |
| **WPF, WinForms** | The FlaUI side: `FlaUIErrors` (`UIA_E_ELEMENTNOTAVAILABLE`), `InstanceKey` from the runtime id | Their `PageObjectBase` throws `PageLoadException` and has `IsReady(int?)`. The WPF login baseline has 2 failing tests before any change (memory: WPF login baseline). Measure it before moving. |
| **Html, Playwright, Blazor** | The Calls layer as is | X2: `Brinell.Html`'s `RunDoWithElement` retries actions. That is an R0 violation, fixed when Html moves, not before. `ControlBase` gates on `ContainingScope.IsReady(timeout)`. |
| **Stride** | The Calls layer | `PageObjectBase.IsReady` polls with `WaitHelper`. |

Each stack's move is a step like MAUI's steps 1-7, with a step 8 of its own: prove it with its
suite, a repeat run, and an R0 proof where the sample app has a known, fixed bug to put back.

## 4. Left over from the MAUI work

Things the MAUI steps did not do, which the move down should settle:

- **Root renames** (design 7.3), left out of step 5:
  - `FindContainerRootElement` → `TryFindRootElement`;
  - `ContainerRoot` / `TryGetContainerRoot` → `Root` / `TryGetRoot`;
  - `CacheContainerRoot` → `CacheRoot`;
  - `IsCachedRootValid` → `IsRootUsable`.
  Done once, in Core's naming, they avoid a second rename.
- **`GetAttribute(name, timeoutMs)`** on MAUI containers keeps an unused `timeoutMs`, because
  Core's `IControlObject` declares it. Drop it from the Core interface when it moves.
- **Page waits** keep `PageLoad` as their default budget; design 7.5 said `DefaultWait`. That is
  a decision for Core's page contract.
- **`IsExists()` / `IsVisible()`** on a control no longer consult the page (design 7.4). Other
  stacks' controls do; decide once for all stacks.
- **The generator** (R8) did not change. Its nested-unit-of-work warning now names `Confirm`.
  Moving down may let it target Core's control base directly.

## 5. Risks

- **UAT discovery.** `TestComposition` finds pages by `[TestPage]` or `IPageObject`. MAUI pages
  carry `[TestPage]` now. When the page interface moves, composition switches to it and the
  attribute can go. Run UAT for every stack after the switch.
- **Timing.** Every stack's calls become one budget. Tests that relied on nested waits adding up
  may need explicit `timeoutMs`, as the MAUI long-list search did (Q9). Record which tests take
  more than 5 s before each move, as step 2 did.
- **Baselines.** Several suites fail before any change (WPF login, Android Range and Picker,
  the MAUI UAT and Presenter UAT scenarios). Record each stack's baseline before its move.

## 6. Open questions for the move-down plan

1. Does the Calls layer become public API in Core, or stay internal to each stack, shared as
   source?
2. Does Core get one `ViewBase`-like control base, or does each stack keep its own base over
   Core's calls layer?
3. Should `BRINELL_CALL_LOG` (MAUI only today) become a Core feature for every stack's context?
