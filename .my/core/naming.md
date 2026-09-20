# Common Core layer: naming

The full rename table. What replaces what, and what stays.

## Rule

- **`ICore` prefix** for every shared interface that used to be generic on `TElement`.
- **`Core` prefix** for shared classes and enums that are new or renamed.
- **No prefix change** for types that already worked across every stack: `Locator`,
  `LocatorStrategy`, `BrinellException`, `ElementNotFoundException`, `AssertionException`,
  `WaitTimeoutException`, `TimeoutSettings`, `ITestLogger`, `LogResult`, `TextInputMethod`,
  `IDiagnosticDriver`, `IScreenshotService`, generator attributes.
- **Per-stack interfaces keep their `I<Stack>*` names** (`IMauiElement`, `IHtmlElement`,
  `IWpfElement`, `IWinFormsElement`, `INativeAndroidElement`, `IStrideElement`). They extend
  the `ICore*` counterpart and add only what that stack does uniquely.

## Interfaces

| Existing (`Brinell.Core.Interfaces`) | New (`Brinell.Core.Automation`) | Notes |
| --- | --- | --- |
| `IElement<TSelf>` | `ICoreElement` | Non-generic; the `TSelf` was only for fluent returns and the calls layer does not need it (?C3) |
| `IElementScope` + `IElementScope<TElement>` | `ICoreScope` (+ `ICoreScope<TSelf>` for fluent) | One interface; the element type is `ICoreElement` (?C4) |
| `IPageObject` + `IPageObject<TElement>` | `ICorePage` (+ `ICorePage<TSelf>` for fluent) | Same simplification |
| `ITestContext` (non-generic) | `ITestContext` | **Unchanged** - it never had `TElement` |
| `ITestContext<TElement>` | `ICoreTestContext` | Extends `ITestContext` and `ICoreScope` |
| `IDriver<TElement>` | `ICoreDriver` | Non-generic; element type is `ICoreElement` |
| `IContainerControl<TElement>` | `ICoreContainer` (+ `ICoreContainer<TSelf>`) | |
| `IContainerObject<TElement>` | (merged with `ICoreContainer`) | The two Core interfaces are collapsed |
| `IItemContainer<TElement>` | `ICoreItem<TCollection>` | |
| `ICollectionObject<TElement, TItem>` | `ICoreCollection<TItem>` | |
| `IElementObject<TScope>` | `ICoreElementObject<TScope>` | |
| `IControlObject<TScope>` | `ICoreControlObject<TScope>` | |
| `IPagedScope<TPage, TElement>` | (deleted in step 10) | Nothing uses it after every stack moves; ?C7 |

## Capability interfaces (?C2)

All under `Brinell.Core.Automation`. Members inside unchanged.

| Existing | New |
| --- | --- |
| `IClickableControlObject<TScope>` | `ICoreClickable<TScope>` |
| `IEditableTextControlObject<TScope>` | `ICoreEditableText<TScope>` |
| `IToggleControlObject<TScope>` | `ICoreToggle<TScope>` |
| `IRangeControlObject<TScope>` | `ICoreRange<TScope>` |
| `IScrollableControlObject<TScope>` | `ICoreScrollable<TScope>` |
| `IExpandableControlObject<TScope>` | `ICoreExpandable<TScope>` |
| `IFocusableControlObject<TScope>` | `ICoreFocusable<TScope>` |
| `IPressableControl<TScope>` | `ICorePressable<TScope>` |
| `IProgressControlObject<TScope>` | `ICoreProgress<TScope>` |
| `IRefreshableControlObject<TScope>` | `ICoreRefreshable<TScope>` |
| `ISelectorControlObject<TScope>` | `ICoreSelector<TScope>` |
| `ISwipeableControlObject<TScope>` | `ICoreSwipeable<TScope>` |
| `ITabControlObject<TScope>` | `ICoreTab<TScope>` |
| `ITextControlObject<TScope>` | `ICoreText<TScope>` |
| `IDateControlObject<TScope>` | `ICoreDate<TScope>` |
| `ITimeControlObject<TScope>` | `ICoreTime<TScope>` |
| `IRangePatternElement` | `ICoreRangePatternElement` |

## Readiness

| Existing | New |
| --- | --- |
| `PageReadinessSnapshot` (`Brinell.Core.Interfaces`) | `CoreScopeReadiness` (`Brinell.Core.Automation`) |
| `PageReadinessState` | `CoreScopeReadinessState`, with extra states `NotLoaded`, `ContentNotReady`, `ItemChanged` (all from MAUI's proven design) |
| `BusySignalPolicy` | `CoreBusySignalPolicy` |
| default `ProbeReadiness()` on `IPageObject` | Deleted with `IPageObject` in step 10 |

## Exceptions

| Existing (`Brinell.Core.Exceptions`) | New (`Brinell.Core.Automation.Exceptions`) | Fate |
| --- | --- | --- |
| `BrinellException` | (unchanged) | Kept |
| `ElementNotFoundException` | (unchanged) | Kept |
| `AssertionException` | (unchanged) | Kept |
| `WaitTimeoutException` | (unchanged) | Kept |
| `LocatorNotSupportedException` | (unchanged) | Kept |
| `PhysicalInputRefusedException` | (unchanged) | Kept |
| `PageLoadException` | `CoreScopeNotReadyException` | Old removed in step 10 (?C8) |
| (new) | `CoreStaleElementException` | New |
| (new) | `CoreElementNotReadyException` | New |
| (new) | `CoreAppUnavailableException` | New |

The MAUI-side exceptions `StaleElementException`, `ElementNotReadyException`,
`ScopeNotReadyException`, `AppUnavailableException` (in `Brinell.Maui.Exceptions`) become type
aliases or subclasses of the `Core*` set in step 4 of `plan.md`.

## Calls layer

Type names stay the same as MAUI's; the namespace disambiguates.

| MAUI name | Core name | Namespace |
| --- | --- | --- |
| `ControlCall` | `ControlCall` | `Brinell.Core.Automation.Calls` |
| `Poller` | `Poller` | `Brinell.Core.Automation.Calls` |
| `Deadline` | `Deadline` | `Brinell.Core.Automation.Calls` |
| `AttemptContext` | `AttemptContext` | `Brinell.Core.Automation.Calls` |
| `Observation` | `Observation` | `Brinell.Core.Automation.Calls` |
| `ObservationLog` | `ObservationLog` | `Brinell.Core.Automation.Calls` |
| `ScopeGate` | `ScopeGate` | `Brinell.Core.Automation.Calls` |
| `Confirmer` | `Confirmer` | `Brinell.Core.Automation.Calls` |
| `Confirmation<T>` | `Confirmation<T>` | `Brinell.Core.Automation.Calls` |

MAUI's copies in `srcnew/Brinell.Maui/Calls/*.cs` are deleted in step 4 of `plan.md`.

## Base classes

| MAUI | Core (renamed) |
| --- | --- |
| `ViewBase<TScope>` | `CoreViewBase<TScope>` |
| `RootedScopeBase<TSelf, TSetResult>` | `CoreRootedScopeBase<TSelf, TSetResult>` |
| `AppRoot` | `CoreAppRoot` |
| `PageObjectBase<TSelf>` | `CorePageObjectBase<TSelf>` |
| `ContainerObjectBase<TParent, TSelf>` | `CoreContainerObjectBase<TParent, TSelf>` |
| `CollectionObjectBase<TParent, TSelf, TItem>` | `CoreCollectionObjectBase<TParent, TSelf, TItem>` |
| `ItemObjectBase<TCollection, TSelf>` | `CoreItemObjectBase<TCollection, TSelf>` |
| `ComponentObjectBase<TScope, TSelf>` | `CoreComponentObjectBase<TScope, TSelf>` |
| `ObjectBase` | (deleted; nothing in the new stack extends it) |

Per-stack base classes (`MauiPageObjectBase<TSelf>`, `HtmlPageObjectBase<TSelf>`, etc.) become
thin subclasses of the Core base, adding only what that stack overrides. If nothing needs
overriding, the stack's page objects inherit the Core base directly.

## Per-stack extension interfaces

Names do not change. Only their parent interfaces do:

| Stack | Element | Driver | Scope | Page | Context |
| --- | --- | --- | --- | --- | --- |
| MAUI | `IMauiElement : ICoreElement` | `IMauiDriver : ICoreDriver` | `IMauiElementScope : ICoreScope` | `IMauiPage : ICorePage` | `IMauiTestContext : ICoreTestContext` |
| Html | `IHtmlElement : ICoreElement` | `IHtmlDriver : ICoreDriver` | `IHtmlElementScope : ICoreScope` | `IHtmlPage : ICorePage` | `IHtmlTestContext : ICoreTestContext` |
| WPF | `IWpfElement : ICoreElement` | `IWpfDriver : ICoreDriver` | `IWpfElementScope : ICoreScope` | `IWpfPage : ICorePage` | `IWpfTestContext : ICoreTestContext` |
| WinForms | `IWinFormsElement : ICoreElement` | `IWinFormsDriver : ICoreDriver` | `IWinFormsElementScope : ICoreScope` | `IWinFormsPage : ICorePage` | `IWinFormsTestContext : ICoreTestContext` |
| NativeAndroid | `INativeAndroidElement : ICoreElement` | `INativeAndroidDriver : ICoreDriver` | `INativeAndroidElementScope : ICoreScope` | `INativeAndroidPage : ICorePage` | `INativeAndroidTestContext : ICoreTestContext` |
| Stride | `IStrideElement : ICoreElement` | `IStrideDriver : ICoreDriver` | `IStrideElementScope : ICoreScope` | `IStridePage : ICorePage` | `IStrideTestContext : ICoreTestContext` |

WPF, WinForms, NativeAndroid and Stride do not have per-stack interfaces today - they still
use `IElement<TElement>` directly. Their migration adds the per-stack interfaces alongside
switching to `ICore*`.

## Non-renames

For record. These types were considered and left alone.

| Type | Reason kept |
| --- | --- |
| `Locator`, `LocatorStrategy` | Not stack-specific; already shared |
| `BrinellException` and its non-page subclasses | Shared, and the type identity matters for `catch` |
| `TimeoutSettings`, `ITestLogger`, `LogResult`, `NullTestLogger` | Runtime services; already shared |
| `TextInputMethod` | An input-mode enum; shared |
| `IDiagnosticDriver`, `IScreenshotService` | Cross-cutting services; shared |
| `AbsenceTolerantAttribute`, `SkipGenerationAttribute`, `GenerateComparisonsAttribute`, `FluentReturnAttribute` | Generator inputs; shared |
| `TestPageAttribute` (in `Brinell.Core.Composition`) | UAT discovery marker; kept while `IPageObject` and `ICorePage` are both accepted (?C7) |
| `ITestContext` (non-generic) | Already the shared surface (`design.md` 4, ?C6) |
