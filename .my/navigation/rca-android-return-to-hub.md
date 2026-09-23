# RCA: Android UI tests never return to the hub after the first test

Date: 2026-09-17. Emulator `Brinell_Perf` (emulator-5554), Appium UiAutomator2,
`Brinell.Maui.UITests.Mobile` via `run-android-tests.ps1`.

## Symptom

In any Android run of more than one test class instance, the first test passes and every later
test fails in its constructor:

```
System.InvalidOperationException : Could not get back to the hub, so the next page cannot be
opened. This is a navigation failure, not a fault in whatever is opened next.
  already at the navigation root
   at MauiFixture.ReturnToHub()  MauiFixture.cs:212
   at MauiFixture.Open(SamplePage page)
```

Measured:

| Run | Result |
| --- | --- |
| CommunityToolkit filter (33 tests), working tree | 1 passed, 32 failed |
| `Control=Switch\|Control=CheckBox` (10), working tree | 1 passed, 9 failed |
| Same filter, **HEAD** (changes stashed, APK rebuilt 18:03) | 1 passed, 9 failed, same message |

So it is pre-existing and not related to the CommunityToolkit work or the MAUI 10.0.90 bump.

## Root cause

`MauiFixture.ReturnToHub` asks the driver whether it is already at the root, and only presses
back when it is not:

```csharp
if (Context.Driver.IsAtNavigationRoot())
    attempts.Add("already at the navigation root");   // nothing is done
else
    Context.Driver.NavigateBack();
```

`IMauiDriver.IsAtNavigationRoot()` defaults to `NavigationDepth() <= 1`, and `NavigationDepth()`
defaults to `1`. `FlaUIMauiDriver` overrides both through the bridge's `GetState("NavigationDepth")`.
**`AppiumMauiDriver` overrides neither**, so on Android and iOS the answer is always "at the root",
whatever is on screen. The fixture skips the back press, then waits for a hub that is under the
pushed page, and throws.

The first test passes only because the app starts on the hub.

### When it broke

Commit `6f713f5` ("gestures and dates", 2026-09-11). Before it the fixture called
`TryNavigateBack()` (default `false` on Appium) and, when that declined and the hub was not
loaded, clicked the `BackToHub` toolbar item - which works on Android. The commit replaced that
with `IsAtNavigationRoot()`, whose default (`true`, later `NavigationDepth() => 1`) reads as a
statement of fact rather than "unknown". The Windows suite exercises only the FlaUI override, so
nothing caught it; there is no Android CI run.

## Evidence from the device

Appium page source, `com.brinell.samples.maui` (probe script, not a test):

| Screen | Relevant nodes |
| --- | --- |
| Hub | `PageHub`, `PageHubTitle`, `Open_*` buttons. **No** navigation button. |
| After opening Toggle | `android.widget.ImageButton content-desc='Navigate up'`, `Button content-desc='BackToHub'`, `ToggleTestPage` |
| After Appium `back` | `PageHub` again |

So Android can answer the question honestly: a `NavigationPage` with a page above the root shows
the toolbar's "Navigate up" button; the root does not. And the platform back action pops.

## Fix

`AppiumMauiDriver` (Android):

- `IsAtNavigationRoot()`: true when no toolbar "Navigate up" button is present.
- `NavigationDepth()`: 1 at the root, otherwise 2 - a lower bound. The toolbar shows whether there
  is something to pop, not how much; callers unwinding several pages loop on
  `IsAtNavigationRoot()`.
- `NavigateBack()`: refuses with `BrinellException` at the root instead of sending back, which on
  the root page would close the app. Otherwise sends the platform back.

iOS keeps the interface defaults and is not claimed fixed: its navigation bar has not been mapped.

Caveat: "Navigate up" is Android's English content description
(`abc_action_bar_up_description`); a device in another language needs its string.

## Verification (hub app)

`Control=Switch|Control=CheckBox` on Android after the fix: **10/10** (was 1/10).

One run in between failed from the third test on with "instrumentation process is not running".
Not this bug: an Appium session orphaned by killing a test process mid-run hit its 300 s
`newCommandTimeout`, and its teardown force-stopped the shared UiAutomator2 server under the live
session (Appium log: `Shutting down because we waited 300 seconds` on the old session id, then
`am force-stop io.appium.uiautomator2.server`). **After killing a test run, restart Appium before
the next one.**

## Other apps

| App / suite | Uses `IsAtNavigationRoot` | Result |
| --- | --- | --- |
| `Brinell.Samples.Maui.App` / `MauiFixture` | yes | fixed above |
| `Brinell.Samples.Maui.ShellApp` / `ShellFixture` | no | **0/15 -> 11/15**, two separate root causes below |
| `Brinell.Maui.Uat.Tests` | - | Windows-only (`net10.0-windows`), not run on Android |
| `NavigationRecovery` (Brinell.Maui.Testing) | no - calls `NavigateBack` directly | would have closed the app at root on Android; the refusal covers it |

### Shell app, root cause 2: the Shell's id was published on Windows only

`ShellSamplePage.Name` is `AppShell`, and every lookup under the page is scoped to that root.
`AppShell.PublishAutomationIdToThePlatformView` copies the Shell's `AutomationId` to its platform
view under `#if WINDOWS` only (Stage G step 32 fixed exactly this, for one platform). On Android no
node carried `AppShell`: the first test failed with `Page 'AppShell' ... MissingRoot`, left the
drawer open, and the rest could not find the hamburger.

Fix: the Android branch sets `ViewIdResourceName` (what UiAutomator2 reports as `resource-id`)
through an accessibility delegate on the platform view. That view is a `DrawerLayout`, which has
its own delegate, so the wrapper forwards every member to it. Measured: the `DrawerLayout` now
reports `com.brinell.samples.shell:id/AppShell`.

### Shell app, root cause 3: the Android chrome hosts were above the Shell

With the root found, every tab and flyout collection still materialized **0 items**.
`AndroidShellChrome` rooted both hosts at `android:id/content`, which is the Shell's *parent*
(`content > LinearLayout > DrawerLayout#AppShell`), and a page scope searches only down.

Fix: hosts named by what they hold, beneath the Shell, measured through Appium:

| Host | Locator | Found |
| --- | --- | --- |
| Tabs | `//android.view.ViewGroup[android.widget.FrameLayout[@content-desc!='']]` | Home, Controls, Detail, Status |
| Flyout | `//androidx.recyclerview.widget.RecyclerView[.//android.view.ViewGroup[@content-desc!='']]` | Main, Settings, Profile, Reports, History, Downloads, About |

### Shell app, the last four failures

| Test(s) | Root cause | Fix |
| --- | --- | --- |
| `ShellFlyoutVerbTests` (2) | `AppiumMauiElement.IsFlyoutOpen` answered null on Android, which these tests cannot accept, and `OpenFlyout` tapped the hamburger unconditionally. While the drawer is open Android renames that button (content-desc `OK`), so opening an open drawer threw and left it open for the next test. `CloseFlyout` sent back unconditionally, which on a shut drawer leaves the page. | `IsFlyoutOpen` = the drawer's items are in the tree (they leave it while shut). Open and Close are no-ops in the requested state and wait for the new one. |
| `Flyout_LastItem_IsReachable` | `ShellSamplePage.IsLoaded` was "the tab strip exists". A flyout item without tabs shows none on Android (Windows keeps an empty host), so on Profile/About the page was never loaded and every scoped lookup was refused: the drawer "had 0 items". Appium log: the tab-host lookup polled 404 until timeout. | Loaded = tab strip **or** a flyout page title. |
| `Shell_FixtureReset_ClearsAPushedPage` | Race in `ShellFixture.ReturnToShellRoot`: it pressed the sub-page's back button and immediately asked again whether the sub-page was pushed. The pop animates, the sub-page was still in the tree, and the second press found no button (Appium log: back button 200, sub-page still present 224 ms later, back button 404). Intermittent - it passed once by luck. | Wait for the sub-page to be gone after each press. |

`ShellSamplePage` also stops caching its root (`CacheContainerRoot => false`). Added while chasing
the race above and kept as cheap insurance; the race, not the cache, was the cause.

**Result:** Shell app on Android **15/15, twice in a row**.

### Environment note: a degraded emulator looks like a product bug

After several hours of runs one Shell run failed from the first test. Logcat showed
`ANR in com.brinell.samples.shell: failed to complete startup`, the app at 186% CPU, the
emulator at 7.6 of 8 GB with swap in use and the sensors HAL at 60% CPU. A cold emulator restart
(`-no-snapshot`) plus an Appium restart cleared it; tests also went from ~20 s to ~3 s each.
