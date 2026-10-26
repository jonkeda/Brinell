# What MAUI does without a platform

Facts from spike tests run under xUnit, plain `net10.0`, against MAUI 10.0.110 (2026-10-25). Each
one decided part of MauiUnit's design.

| Finding | Consequence for MauiUnit |
| --- | --- |
| `new Button()`, layouts, `SetBinding`, XAML (XamlC) all work on plain `net10.0` | No fake rendering layer; MAUI's own controls and binding engine are reused as they are |
| A view model's `PropertyChanged` throws "not instantiated on a thread with a dispatcher" | MauiUnit installs a `DispatcherProvider` once per process; `IsDispatchRequired` is false, so bindings apply at once |
| `IView.Measure` returns 0x0 without handlers | No layout in V1; `Visible` is `IsVisible` up the ancestors. Fake handlers are Phase 6 |
| `NavigationPage.PushAsync` and `PopAsync`, `PushModalAsync` and `Shell.GoToAsync("route")` complete with no handler | Navigation is MAUI's own; MauiUnit only reads the stacks |
| `SendAppearing` does nothing unless the page's `Window` has an `Application` parent | Each render is a `Window` opened through `Application.AddWindow` (internal); after that MAUI raises Appearing and Disappearing on push, pop and modal by itself |
| `new Application()` makes itself `Application.Current` | MauiUnit puts the previous one back, under a lock |
| `Shell.Current` reads `Application.Current.Windows` | `Application.Current` is the context's app while MauiUnit runs its work (an option, on by default) |
| `Button.SendClicked` skips a disabled button; `IMenuItemController.Activate`, `Entry.SendCompleted` and `Page.SendAppearing` are public | Interactions call MAUI's own entry points; MauiUnit also refuses hidden or disabled targets with a clear error |
| `TapGestureRecognizer.SendTapped(View, Func<IElement, Point?>)` is internal | Called by reflection from `MauiInternals`, the one place MauiUnit touches internals |
| A binding is kept in internal `BindableObject.GetContext(p).Bindings` | `BeBoundTo` reads it by reflection, in `MauiInternals` too |
| `CollectionView` has no item views without a handler | MauiUnit realizes every item through its template (no virtualization) and keeps each row per item |
| A Shell builds a `ContentTemplate` page only when the platform asks, with `ActivatorUtilities` over the nearest handler's services, and `FindMauiContext` stops at the Window | MauiUnit asks for the page (`IShellContentController.GetOrCreateContent`), and a Shell root gets a no-op `HeadlessHandler` carrying the test's services. A `NavigationPage` must never get one: with a handler, a push waits for the platform to report it finished |
| A Shell section's `NavigationStack` starts with null | Every walk over a stack filters nulls |
| `DisplayAlertAsync`, `DisplayPromptAsync` and `DisplayActionSheetAsync` never complete without a platform: the page keeps a closure in its private `_pendingActions`, holding MAUI's public `AlertArguments`, `PromptArguments` or `ActionSheetArguments` | MauiUnit takes the request from there (`MauiInternals.TakeDialogRequests`) and shows a `TestDialog` over the page; its buttons call the arguments' own `SetResult`, so the app's await resumes |
| `VisualElement.Focus()` needs a handler; `IsFocused` is read-only through the internal `IsFocusedPropertyKey`, and setting it raises `Focused` and `Unfocused` | `SetFocus` and `ClearFocus` set it through the key, unfocusing what had focus in the window first |
| `SwipeGestureRecognizer.SendSwiped` is public; `SwipeView.Open` without a handler only announces itself | Swipes run the recognizer as the platform does; a SwipeView is opened, but nothing visible changes in memory |
| Picker resolves `SelectedIndex` through equality: two equal items select the first | MauiUnit refuses to pick an item equal to an earlier one, as the Windows bridge does |
| The device trees leave an `IsVisible="False"` element out, but keep an off-screen one (Windows) | The head's lookups drop declared-hidden elements and keep off-screen carousel cards |
| A Bruinet control that misses sweeps by asking `AppElement.TryFindByScrolling` | On an in-memory head nothing is off screen, so that must answer null; answering with a lookup turned a container's miss into an app-wide hit |
| In Debug, MAUI 10 parses XAML at run time, and the parser knows only the XAML namespaces of loaded assemblies; on a device `UseMauiCommunityToolkit()` loads the toolkit first | A fixture loads what its app's builder would before the first page parses (Todo: the toolkit's assembly); XamlC in the headless project could not resolve the toolkit namespace either |
| Any animation (`FadeToAsync`, the toolkit's `StateContainer`) asks the element's services for an `IAnimationManager` and throws without one | MauiUnit registers MAUI's own `AnimationManager` over a ticker whose `SystemEnabled` is false, so every animation ends at once at its final value |
| A `Window`'s parent is its `Application` | A walk up from an element stops at the window, not at the top |
| An app that waits on something real (a backend, a file handshake) waits in wall time, and its continuations run only when the test lets MauiUnit's queue run | The head's timeouts are the device's; a test-side wait that does not go through the driver must let the app run (`whileWaiting`); between tests the app is let settle until it is quiet |
| In one process, the app's polling reads meet the test's file replaces (`File.Move` with overwrite fails while a handle is open) | The writers retry briefly; a swallowed failure had left a test waiting out a reset that was done |
