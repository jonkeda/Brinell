# What MAUI does without a platform

Facts from spike tests run under xUnit, plain `net10.0`, against MAUI 10.0.110 (2026-10-25). Each
one decided part of MUnit's design.

| Finding | Consequence for MUnit |
| --- | --- |
| `new Button()`, layouts, `SetBinding`, XAML (XamlC) all work on plain `net10.0` | No fake rendering layer; MAUI's own controls and binding engine are reused as they are |
| A view model's `PropertyChanged` throws "not instantiated on a thread with a dispatcher" | MUnit installs a `DispatcherProvider` once per process; `IsDispatchRequired` is false, so bindings apply at once |
| `IView.Measure` returns 0x0 without handlers | No layout in V1; `Visible` is `IsVisible` up the ancestors. Fake handlers are Phase 6 |
| `NavigationPage.PushAsync` and `PopAsync`, `PushModalAsync` and `Shell.GoToAsync("route")` complete with no handler | Navigation is MAUI's own; MUnit only reads the stacks |
| `SendAppearing` does nothing unless the page's `Window` has an `Application` parent | Each render is a `Window` opened through `Application.AddWindow` (internal); after that MAUI raises Appearing and Disappearing on push, pop and modal by itself |
| `new Application()` makes itself `Application.Current` | MUnit puts the previous one back, under a lock |
| `Shell.Current` reads `Application.Current.Windows` | `Application.Current` is the context's app while MUnit runs its work (an option, on by default) |
| `Button.SendClicked` skips a disabled button; `IMenuItemController.Activate`, `Entry.SendCompleted` and `Page.SendAppearing` are public | Interactions call MAUI's own entry points; MUnit also refuses hidden or disabled targets with a clear error |
| `TapGestureRecognizer.SendTapped(View, Func<IElement, Point?>)` is internal | Called by reflection from `MauiInternals`, the one place MUnit touches internals |
| A binding is kept in internal `BindableObject.GetContext(p).Bindings` | `BeBoundTo` reads it by reflection, in `MauiInternals` too |
| `CollectionView` has no item views without a handler | MUnit realizes every item through its template (no virtualization) and keeps each row per item |
