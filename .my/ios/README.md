# iOS: the routes written on Windows, to run on a Mac

The Appium driver now has iOS routes for everything the Android work added. They were written on
Windows from UIKit's documented tree and have never run against a simulator. This page is the run
that checks them: what to start, which tests prove each route, and what to change when one is wrong.

## What was written

| Route | Where | What it assumes |
| --- | --- | --- |
| At the navigation root | `AppiumMauiDriver.IsAtNavigationRoot` | a pushed page shows a button named `BackButton` |
| Picker, date and time | `AppiumMauiElement.Wheels.cs` | tapping the field shows `XCUIElementTypePickerWheel`s and a `Done` button; a date's wheels read a month name, a day and a 4-digit year; a time's an hour, a minute and AM/PM |
| Alerts | `AppiumMauiDriver.ReadIosAlert` | an `XCUIElementTypeAlert`, title then message as static texts, the cancel the first button |
| Shell's tabs and flyout | `AppiumMauiDriver.IosShellChrome`, `AppiumMauiElement.OpenFlyout` | tabs are buttons in an `XCUIElementTypeTabBar`; the flyout is a table of cells; its opener is a navigation-bar button named `OK`, `Menu` or `FlyoutIcon`; a tap at the screen's right edge closes it |
| IndicatorView | `IndicatorView.ReadPageControl` | a `XCUIElementTypePageIndicator` whose value reads `page 2 of 5` |
| Session settings | `MauiDriverFactory.SessionDefaults` (2026-10-22, M1) | `waitForQuiescence` false and `settings[waitForIdleTimeout]` 1 (seconds on XCUITest) keep an animating page from costing each command its 10 s; check a command on a page with an ActivityIndicator returns at once |
| The Shell app | `samples/Brinell.Samples.Maui.ShellApp` (`net10.0-ios`), `ShellFixture.GetDefaultAppPath` | built like the hub app, into `bin/Debug/net10.0-ios/iossimulator-<arch>/` |

## Run it

Set up as [docs/run/maui-ios.md](../../docs/run/maui-ios.md) says, building both apps. Then one class
at a time, in this order, since each later class leans on the routes before it:

```bash
dotnet test testsnew/Brinell.Maui.UITests.Mobile --filter "FullyQualifiedName~.ButtonTests"
dotnet test testsnew/Brinell.Maui.UITests.Mobile --filter "FullyQualifiedName~.PickerTests"
dotnet test testsnew/Brinell.Maui.UITests.Mobile --filter "FullyQualifiedName~.DatePickerTests|FullyQualifiedName~.TimePickerTests"
dotnet test testsnew/Brinell.Maui.UITests.Mobile --filter "FullyQualifiedName~AlertReadTests"
dotnet test testsnew/Brinell.Maui.UITests.Mobile --filter "FullyQualifiedName~ShellTabTests|FullyQualifiedName~ShellFlyoutTests|FullyQualifiedName~ShellStackTests"
dotnet test testsnew/Brinell.Maui.UITests.Mobile --filter "FullyQualifiedName~.IndicatorViewTests"
dotnet test testsnew/Brinell.Maui.UITests.Mobile
```

The first sign that the navigation root is right: `ButtonTests` should not spend about 10 s per
test recovering in `MauiFixture.ReturnToHub`. Before this change every test paid that.

## When a route is wrong

Dump the screen's tree with the route's page open and compare it with the table above:

```bash
curl -s http://127.0.0.1:4723/session/<id>/source > tree.xml
```

The session id is in Appium's log. Fix the locator or value in the file the table names, and
remove its "not yet run on a simulator" remark once it passes. Then write what was measured
(names, values, timings) into [docs/run/maui-ios.md](../../docs/run/maui-ios.md) and delete this topic.

## Still open after this

- RatingView, slider and Expander use `./*` child paths measured on UiAutomator2; XCUITest roots an
  element's XPath differently, so these may need their own path on iOS.
- MediaElement, CarouselView and DrawingView fail on Android too; leave them until Android passes.
