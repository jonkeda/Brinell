# Common Core layer: migration

Per-stack playbook. Each stack's step in `plan.md` (steps 4-8) uses the shape below.

## Common shape

Every stack move has these six sub-steps:

1. **Baseline.** Record the passing/failing test counts for that stack. Never edited after.
2. **Per-stack extension interfaces.** Add `I<Stack>Element : ICoreElement`, `I<Stack>Driver`,
   `I<Stack>ElementScope`, `I<Stack>Page`, `I<Stack>TestContext`. If the stack already has
   them (MAUI, Html), change their base to `ICore*`.
3. **Switch the stack's control base.** `<Stack>ViewBase<TScope>` extends
   `CoreViewBase<TScope>` (or if nothing stack-specific remains, is removed).
4. **Switch the stack's page/container/collection bases** to the `Core*` bases.
5. **Delete the stack's private calls layer.** Use `Brinell.Core.Automation.Calls`.
6. **Prove it.** The stack's tests green against the baseline; no new failures.

## MAUI (plan step 4)

MAUI already stands alone (`../stale-readiness/plan.md` step 1 done). Its move to `ICore*` is
mechanical.

Sub-steps:

1. Baseline: MAUI's step 8 numbers from stale-readiness.
2. `IMauiElement`, `IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext` change
   their base from `IElement<IMauiElement>`, `IDriver<IMauiElement>` etc. (which MAUI stopped
   implementing in stale-readiness step 1) to `ICoreElement`, `ICoreDriver`, `ICoreScope`,
   `ICorePage`, `ICoreTestContext`.
3. Members that already live on both (`Click`, `SendKeys`, `Text`, ...) move up to `ICore*`
   and disappear from `IMaui*`.
4. `MauiViewBase<TScope>` becomes `CoreViewBase<TScope>` (renamed), or a subclass with only
   MAUI-specific helpers.
5. `Brinell.Maui/Calls/*.cs` deleted; call sites `using Brinell.Core.Automation.Calls;`.
6. `Brinell.Maui.Exceptions.*` become subclasses of `Core*Exception` (a caught
   `StaleElementException` still catches). Or use type aliases (`using StaleElementException =
   Brinell.Core.Automation.Exceptions.CoreStaleElementException;` in `GlobalUsings.cs`).
7. **Verify.** `Brinell.Maui.Tests`, `Brinell.Maui.Uat.Tests`, `Brinell.Maui.UITests` full
   Windows run; the Android baseline subset; `Brinell.Presenter.Uat.Tests`.

**Skills:** `.github/skills/maui-control/references/*` and `.github/skills/maui-ui-test/`
update their type references from `IMauiElement` to `ICoreElement` where the member is the
Core one, and keep `IMauiElement` where the member is MAUI-specific.

**Expected diff size:** small. `srcnew/Brinell.Maui/Interfaces/*.cs`: base-change lines only.
`srcnew/Brinell.Maui/Controls/Base/*.cs`: rename `ViewBase` -> `CoreViewBase` (or a shim).
`srcnew/Brinell.Maui/Calls/*.cs`: deleted (~1,500 lines). Net removal.

## Html (plan step 5)

The Html plan (`../html/`) is still a draft. If Html has not started when this common-Core
step lands, Html's step 1 targets `ICoreElement` / `ICoreScope` / `ICorePage` /
`ICoreTestContext` directly, instead of first defining its own `IHtmlElement` and later
subclassing `ICoreElement`.

Sub-steps:

1. Baseline: Html plan's baseline.
2. Define `IHtmlElement : ICoreElement`, `IHtmlDriver : ICoreDriver`, `IHtmlElementScope :
   ICoreScope`, `IHtmlPage : ICorePage`, `IHtmlTestContext : ICoreTestContext`. Add only the
   Playwright-specific members (`Fill`, `SelectOption`, `Check`, `Focus`, `Evaluate`,
   `GetDomAttribute`, `GetDomProperty`, `InnerHtml`, `OuterHtml`).
3. `HtmlViewBase<TScope>` extends `CoreViewBase<TScope>`. `HtmlPageObjectBase<TSelf>`
   extends `CorePageObjectBase<TSelf>` and adds the URL check.
4. `PlaywrightHtmlElement : IHtmlElement`, `PlaywrightHtmlDriver : IHtmlDriver`,
   `PlaywrightTestContext : IHtmlTestContext`. `Live<T>` wrapper as `../html/design.md` 5.
5. Html's `Brinell.Html/Calls/*` is not created; Html uses
   `Brinell.Core.Automation.Calls` from the start.
6. `HtmlPageObjectBase.AssertLoaded` and `AssertTitle` throw `CoreScopeNotReadyException`
   instead of `PageLoadException`.
7. **Verify.** `Brinell.Html.Tests`, `Brinell.Html.Uat.Tests`, `Brinell.Blazor.Tests`,
   `Brinell.Blazor.Uat.Tests`, `Brinell.Html.UITests`, `Brinell.Blazor.UITests`.

**Skills:** the `html-control` and `html-ui-test` skills (in `../html/skills.md`) name
`ICoreElement` as the element type in their references from the start; the Playwright- and
Blazor-specific helpers are the `IHtmlElement` extension.

**Sample app:** unchanged from `../html/sample-app.md`.

## NativeAndroid (plan step 6)

NativeAndroid currently uses `Brinell.Core.Interfaces.IElement<TElement>` and friends directly
(no per-stack extension interfaces). Its move creates the per-stack interfaces at the same time
as it switches to `ICore*`.

Sub-steps:

1. Baseline: Range 6 passed / 16 failed, Picker 0 passed / 8 failed
   (memory: this is the known Android baseline).
2. Define `INativeAndroidElement : ICoreElement`, `INativeAndroidDriver : ICoreDriver`,
   `INativeAndroidElementScope : ICoreScope`, `INativeAndroidPage : ICorePage`,
   `INativeAndroidTestContext : ICoreTestContext`. Add Appium- and Android-specific members
   (`AppiumErrors` mapping, `Live` wrapper, session-gone handling).
3. `AppiumNativeAndroidElement`, `AppiumNativeAndroidDriver`, `AppiumTestContext` switch to
   the new interfaces. UIA-style `InstanceKey` from the Appium element id.
4. `NativeAndroidViewBase<TScope>` extends `CoreViewBase<TScope>`.
   `NativeAndroidPageObjectBase<TSelf>` extends `CorePageObjectBase<TSelf>`.
5. Calls layer: `Brinell.Core.Automation.Calls`.
6. `PageLoadException` → `CoreScopeNotReadyException` throughout.
7. **Verify.** `Brinell.NativeAndroid.Tests` against the baseline; the Android UI suite
   subset; the Appium adapter unit tests.

**Sample app:** any Android sample project used by the fixture. If none exists as a working
target, ship one; but that is scope for the NativeAndroid move-down slice, not this project.

## WPF and WinForms (plan step 7)

Both stacks currently use `Brinell.Core.Interfaces.IElement<TElement>` and Core's
`PageObjectBase`. Both use FlaUI. Their move is similar to NativeAndroid's.

Sub-steps:

1. Baseline for each: current passing/failing test counts. **WPF has 2 failing login tests
   before any change** (memory: WPF login baseline). WinForms baseline TBD.
2. Define `IWpfElement`, `IWpfDriver`, `IWpfElementScope`, `IWpfPage`, `IWpfTestContext`
   inheriting `ICore*`. Same for `IWinForms*`. Members added by each stack: FlaUI-specific
   (`UIA_E_ELEMENTNOTAVAILABLE` mapping, `InstanceKey` from the runtime id, chrome finds).
3. `FlaUIWpfElement : IWpfElement`, `FlaUIWpfDriver : IWpfDriver`,
   `FlaUIWpfTestContext : IWpfTestContext`. Same for the WinForms drivers.
4. `WpfViewBase<TScope>` extends `CoreViewBase<TScope>`. Same for
   `WinFormsViewBase<TScope>`.
5. Calls layer: `Brinell.Core.Automation.Calls`.
6. `PageLoadException` → `CoreScopeNotReadyException` throughout both stacks.
7. **Verify.** `Brinell.Wpf.Tests`, `Brinell.Wpf.UITests`, `Brinell.Wpf.Uat.Tests`,
   `Brinell.WinForms.Tests`, `Brinell.WinForms.UITests`, `Brinell.WinForms.Uat.Tests`.
   Against the recorded baseline; the 2 WPF login failures must still be exactly the same 2
   failures.

**Skills:** two new skills ship with this step, mirroring `maui-control` and `maui-ui-test`:
`.github/skills/wpf-control/`, `.github/skills/wpf-ui-test/`,
`.github/skills/winforms-control/`, `.github/skills/winforms-ui-test/`. Not built here; called
out in `plan.md` step 11.

## Stride (plan step 8)

Stride uses a pipe-based automation handshake through `Brinell.Stride.Automation`. The pipe
name is shared across every test fixture in the suite; orphaned Stride game processes block
new connections (memory: pipe orphan-kill rule).

Sub-steps:

1. Baseline: `Brinell.Stride.Tests`, `Brinell.Stride.UITests`, `Brinell.Stride.Uat.Tests`
   current passing/failing counts.
2. Define `IStrideElement : ICoreElement`, `IStrideDriver : ICoreDriver`,
   `IStrideElementScope : ICoreScope`, `IStridePage : ICorePage`,
   `IStrideTestContext : ICoreTestContext`. Add pipe-specific members: the handshake and the
   named pipe connection.
3. `StridePipeElement`, `StridePipeDriver`, `StridePipeTestContext` switch to the new
   interfaces.
4. `StrideViewBase<TScope>` extends `CoreViewBase<TScope>`.
5. Calls layer: `Brinell.Core.Automation.Calls`. `WaitHelper` polling in `PageObjectBase.IsReady`
   is replaced by `CoreScopeReadiness.ProbeReadiness`.
6. `PageLoadException` → `CoreScopeNotReadyException`.
7. **Verify.** With the orphan-kill script run first:

   ```powershell
   Get-Process -Name "Oravey2*" -EA SilentlyContinue | Stop-Process -Force
   Get-Process -Name "Stride*" -EA SilentlyContinue | Stop-Process -Force
   ```

   Then `Brinell.Stride.Tests` and one class of `Brinell.Stride.UITests` at a time, with
   `isBackground=false` and per-class timeouts of 120000 ms.

## UAT (plan step 9)

UAT discovery is one shared surface. Only `TestComposition` and `UatDiscovery` need to know
about the new page/element interfaces.

Sub-steps:

1. `Brinell.Core.Composition.TestComposition` recognizes `ICorePage` alongside `IPageObject`.
   Both are accepted while any stack still uses the old interface (?C7).
2. `Brinell.Uat.UatDiscovery`'s name-inference (if any) and its `IControlObject<>`
   recognition switch to `ICoreControlObject<>`.
3. **Verify.** Every stack's UAT test project. The number of pages discovered is unchanged.

## Delete (plan step 10)

Nothing built; only removed. In order:

1. `Brinell.Core.Interfaces.IElement<TSelf>` (delete file).
2. `IElementScope`, `IElementScope<TElement>` (delete file).
3. `IPageObject`, `IPageObject<TElement>` (delete file).
4. `ITestContext<TElement>` (edit `ITestContext.cs`, leaving the non-generic
   `ITestContext`).
5. `IDriver<TElement>` (delete file).
6. `IContainerControl<TElement>` (delete file).
7. `IContainerObject<TElement>`, `IItemContainer<TElement>`,
   `ICollectionObject<TElement, TItem>` (delete file).
8. `IElementObject<TScope>` (delete file).
9. `IControlObject<TScope>` (delete file).
10. Each old capability interface (`IClickableControlObject<TScope>`, ...) (delete each).
11. `PageReadinessSnapshot`, `PageReadinessState`, `BusySignalPolicy` (delete file).
12. `PageLoadException` (delete file).
13. `ElementGeometryExtensions`, `ElementScopeExtensions` (delete files; the geometry and
    scope helpers now live under `Brinell.Core.Automation.Helpers`).
14. `ControlObjectBase<TScope>` (delete file).
15. `ObjectBase` (delete file).

After each delete, `dotnet build srcnew\Brinell.sln`; any consumer that fails is a stack that
did not fully move, and the delete is reverted until that stack moves.

**Verify.** Full solution build; every stack's `Tests` and `Uat.Tests` project; grep finds no
occurrence of any deleted type.

## What is not migrated

- `TestPageAttribute` on `Brinell.Core.Composition`. Kept as-is; UAT uses it and it costs
  nothing.
- The generator (`Brinell.Generator`). R8 says the generator does not change. Its inputs
  (attributes, method conventions) are already stack-agnostic.
