# Plan — Presenter UI refresh: themes, icons, toolkit Expander, app icon

Status: In progress — WP1, WP2 and WP3 delivered; WP4 open
Date: 2026-09-21
Assets: screenshots and the glyph verification sheet in [assets/](assets/)
Area: `srcnew/Brinell.Presenter/`, `testsnew/Brinell.Presenter.Uat.Tests/`
Related:

- [Presenter current UI and execution design](../10%20presenter%20tabbed%20tree%20redesign.md)
- [Presenter development plan](../09%20brinell%20presenter%20development%20plan.md)
- [AD-008 — gestures and semantic actions go through UI Automation](../../../.docs/decisions/ad-008-gestures-and-semantic-actions-go-through-ui-automation.md)
- [AD-005 — physical input is opt-in](../../../.docs/decisions/ad-005-physical-input-is-opt-in.md)
- `samples/Brinell.Samples.Maui.App/Automation/ToolkitVerbSink.cs`
- `srcnew/Brinell.Maui.CommunityToolkit/Controls/Views/Expander.tpl.cs`

---

## 0. What this plan covers

Four requested changes to `Brinell.Presenter`:

1. A light and a black theme with high visibility, plus a toggle button in the top right.
2. Most buttons become icons.
3. The CommunityToolkit `Expander` replaces the hand-rolled expand/collapse pairs.
4. The app gets an icon.

All four touch the same two files — `Views/PresenterPage.xaml` and
`ViewModels/PresenterShellViewModel.cs` — so they are sequenced to avoid
colliding edits, not because they depend on each other's behaviour.

## 1. Ground truth this plan was written against

| Fact | Source |
| --- | --- |
| Presenter is Windows-only, unpackaged, self-contained WinApp SDK | `Brinell.Presenter.csproj` — one TFM, `WindowsPackageType=None` |
| It has **no** `Resources/` folder at all — no icon, no splash, no styles | file listing of `srcnew/Brinell.Presenter/` |
| Palette is six `Color` keys inline in `App.xaml`, consumed as `StaticResource` | `App.xaml`, `Views/PresenterPage.xaml` |
| Status glyph colours are hard-coded in C#, bound as `{Binding IconColor}` | `ViewModels/UatStatusPresentation.cs`, `UatWorkspaceNodeViewModel.IconColor` |
| The VM is constructed by hand in `App.CreateWindow`; `MauiProgram` registers nothing | `App.xaml.cs:21-33`, `MauiProgram.cs` |
| Presenter UAT tests drive the **real .exe** through FlaUI / UI Automation | `PresenterFixture.GetDefaultAppPath` |
| Every test locator is an **AutomationId**; none assert on button text | `testsnew/Brinell.Presenter.Uat.Tests/PageObjects/PresenterPage.cs` |
| `CommunityToolkit.Maui` 15.0.1 is already version-pinned centrally, unreferenced by the Presenter | `Directory.Packages.props` |
| The toolkit `Expander` publishes **no** UI Automation pattern on Windows | `Expander.tpl.cs` remarks; `.my/communitytoolkit/probe.md` |

## 2. The two constraints every work package must respect

**K1 — AutomationIds are the test contract.** Icons, themes and Expanders may
change text, colour and visual tree freely. They may not change, drop or
re-parent any of these ids:

```
PresenterRoot            OpenFolderButton        OpenRecentButton
ReloadButton             ValidateButton          RunButton
StopButton               NextButton              DelayMillisecondsInput
DelayMillisecondsLabel   TreeTabButton           ConfigTabButton
DiagnosticsTabButton     DiscoveryTabButton      CommandCatalogTabButton
WorkspaceTree            RecentFoldersList       SelectionExpander
StatusSummaryLabel       WorkspaceSummaryLabel   SelectionDetailsText
WorkspaceTreeText        AllWorkspaceTreeText    StepListText
ExecutionTimingText      RunScopeText            RecentFoldersText
AutPlacementText         WorkspaceConfigText     DiagnosticsText
DiscoveryText            CommandCatalogText
WorkspaceNode_*          WorkspaceNodeToggle_*
```

**K2 — the hidden text mirrors stay plain ASCII.** `WorkspaceTreeText`,
`AllWorkspaceTreeText`, `StepListText` and friends are 1px-tall labels whose
text is what tests assert against (`PresenterWorkspaceTreeTests`, the
`.uat.md` scenarios). They are fed by
`UatWorkspaceNodeViewModel.DisplayText`, which today concatenates
`ExpansionText` and `Icon`. **Splitting the visual glyph from the mirror text
is the single most load-bearing detail in this plan** — see WP2.3.

---

## WP1 — Light and black themes with a top-right toggle

### 1.1 Approach

Use `AppThemeBinding` + `Application.UserAppTheme`, not a swapped
`ResourceDictionary`. `AppThemeBinding` re-evaluates when the effective theme
changes, so a single assignment repaints the window; `StaticResource` does not,
and `DynamicResource` + dictionary swapping needs a second mechanism for the
C#-side colours anyway.

Three tiers of theme, persisted:

| `PresenterTheme` | Meaning |
| --- | --- |
| `System` (default) | follow Windows |
| `Light` | force light |
| `Dark` | force black |

The toggle button cycles `Light → Dark → System`, or — if a two-state button is
preferred — flips `Light ↔ Dark` and never returns to `System` once pressed.
**Pick one and say so in the code comment;** a tri-state button with no visible
state indicator is the kind of thing that reads as a bug.

### 1.2 Files

**New — `Resources/Styles/Colors.xaml`**: light and dark values for every
palette role, one `Color` key per `<role><Light|Dark>` pair, then one
`AppThemeBinding`-backed `Style` per control type so call sites stay short.

Proposed palette (contrast targets in §1.4):

| Role | Light | Dark |
| --- | --- | --- |
| `PresenterBackground` | `#F6F7F9` | `#0D0D0F` |
| `PresenterPanel` | `#FFFFFF` | `#17181C` |
| `PresenterBorder` | `#D9DEE7` | `#3A3D45` |
| `PresenterText` | `#1F2937` | `#F2F3F5` |
| `PresenterMuted` | `#667085` | `#A6ADBA` |
| `PresenterAccent` | `#2563EB` | `#7AA5FF` |
| `PresenterSelected` (new, tab strip) | `#E3EAFB` | `#243049` |

**Edit — `App.xaml`**: merge `Colors.xaml`; replace the six literal `Color`
entries; add implicit `Style`s for `Label`, `Button`, `Entry`, `Border` that
reference the theme pairs. Add an explicit `Entry` style — `Entry` on WinUI
draws from WinUI theme brushes, and a dark page with a light entry is the most
likely visible miss.

**Edit — `Views/PresenterPage.xaml`**: every `{StaticResource PresenterX}`
becomes the themed equivalent. There are ~20 such references; several are on
`Border.Stroke`, `BackgroundColor` and `Label.TextColor`.

**Edit — `ViewModels/UatStatusPresentation.cs`** — the real work. `Color(status)`
returns one fixed colour used for the status glyph in every tree row. `#16A34A`
on `#0D0D0F` and `#94A3B8` on `#FFFFFF` are both below AA. Two options:

- **A (recommended)** — pick values that clear 4.5:1 against *both* the light
  panel `#FFFFFF` and the dark panel `#17181C`. Starting candidates, to be
  measured, not assumed: pass `#1F9D55`→`#3DD68C`, fail `#DC2626`→`#FF6B6B`,
  run `#2563EB`→`#7AA5FF`, skip/cancel `#5B6472`→`#9AA3B2`, wait `#6B7280`→`#9CA3AF`.
  That means `Color(status)` takes the current `AppTheme`, so it stops being a
  static-only helper: give it `Color(string status, AppTheme theme)` and have
  `UatWorkspaceNodeViewModel.IconColor` re-raise `PropertyChanged` on theme change.
- **B** — return a resource *key* and resolve it in XAML through a converter.
  Cleaner separation, more moving parts, and the tree is a `CollectionView`
  template so the converter runs per row.

Take A. Note the consequence: the shell VM must broadcast a theme change to
every `UatWorkspaceNodeViewModel` so bound colours refresh. Simplest honest
route is `Application.Current.RequestedThemeChanged` → shell → walk
`_allWorkspaceNodes` → `OnPropertyChanged(nameof(IconColor))`.

**Edit — `Services/PresenterUserSettings.cs`**: add `public PresenterTheme Theme { get; set; } = PresenterTheme.System;`
plus a new `PresenterTheme` enum. `PresenterUserSettingsService` needs no change
(it round-trips the whole object), but add a round-trip case to
`Services/PresenterUserSettingsServiceTests.cs` — including that an older
settings file with no `Theme` member loads as `System`.

**Edit — `ViewModels/PresenterShellViewModel.cs`**: `ToggleThemeCommand`,
`ThemeGlyph`, `ThemeDescription`, `CurrentTheme`; apply on construction and on
each toggle; persist through `_settingsService`.

**Edit — `App.xaml.cs`**: apply the persisted theme before the window is shown,
so the app does not flash light before turning black.

### 1.3 The toggle button

The top action row becomes:

```xml
<Grid Grid.Row="0" ColumnDefinitions="Auto,*,Auto">
    <HorizontalStackLayout Grid.Column="0" Spacing="6"> ...Open/v/Reload/Validate... </HorizontalStackLayout>
    <Button Grid.Column="2"
            AutomationId="ThemeToggleButton"
            Text="{Binding ThemeGlyph}"
            SemanticProperties.Description="{Binding ThemeDescription}"
            ToolTipProperties.Text="{Binding ThemeDescription}" />
</Grid>
```

`ThemeDescription` reads "Switch to dark theme" / "Switch to light theme" /
"Follow system theme" — that string is both the tooltip and the UIA name, so an
icon-only button is still findable and still passes the accessibility audit.

### 1.4 Definition of done

- Every text/background pair in both themes measures **≥ 4.5:1**; 1px borders
  and the status glyphs measure **≥ 3:1** against their panel.
- Toggling repaints the whole window with no restart, including the `Entry`,
  the `CollectionView` rows and the tab strip.
- The choice survives a restart.
- Screenshots of both themes land in `.my/present/fix/assets/`.

### 1.5 Known risk

WinUI's native theme is set on the root `FrameworkElement`, and MAUI's mapping
of `UserAppTheme` onto it has historically been the weak spot. If the `Entry`,
scrollbars or the window chrome stay light after the toggle, the fallback is to
set `Microsoft.UI.Xaml.FrameworkElement.RequestedTheme` on the window's root
content from the `#if WINDOWS` block already present in `App.xaml.cs` next to
`PlacePresenterWindow`. **Verify this early** — it decides whether WP1 is an
afternoon or a day.

---

## WP2 — Icon buttons

### 2.1 Icon source

Use glyphs from **Segoe Fluent Icons** with **Segoe MDL2 Assets** as the
fallback family, set as `Button.Text` + `FontFamily`. Reasons: the app is
Windows-only, both fonts ship with the OS, nothing has to be bundled or
registered, and glyphs recolour with `TextColor` — which WP1 needs. A
`FontImageSource` on `Button.ImageSource` is the alternative; it does not
recolour as cleanly per theme and adds a second code path for nothing here.

Register the family once in `App.xaml` as `PresenterIconFont` so a later switch
to a bundled font (if these ever need to run non-Windows) is one line.

**Every codepoint below must be checked against the official Segoe Fluent Icons
list before use — they are proposals, not verified values:**

> **Verified on delivery.** Each candidate was checked for presence in both
> `SegoeIcons.ttf` and `segmdl2.ttf` and then rendered and eyeballed — see
> [assets/wp2-glyph-verification.png](assets/wp2-glyph-verification.png). All were
> present in both families. **One proposal was wrong: `E8A9` is a four-square tiles
> icon, not a tree** — the Tree tab ships `F168`, an indented hierarchy. Everything
> else below was confirmed as described. The shipped values live in
> `ViewModels/PresenterIcons.cs`, one name per meaning.

| Button | Meaning | Candidate |
| --- | --- | --- |
| `OpenFolderButton` | open folder | `E838` |
| `OpenRecentButton` | chevron down / up | `E70D` / `E70E` |
| `ReloadButton` | refresh | `E72C` |
| `ValidateButton` | check | `E73E` |
| `RunButton` | play | `E768` |
| `StopButton` | stop | `E71A` |
| `NextButton` | step forward | `E893` |
| `ThemeToggleButton` | sun / moon | `E706` / `E708` |
| `TreeTabButton` | tree/list | `E8A9` |
| `ConfigTabButton` | settings | `E713` |
| `DiagnosticsTabButton` | diagnostic | `E9D9` |
| `DiscoveryTabButton` | search | `E721` |
| `CommandCatalogTabButton` | list | `E8FD` |
| tree row toggle | chevron right / down | `E76C` / `E70D` |

### 2.2 The tab strip is the actual win

In the current screenshot the fifth tab ("Command Catalog") is **clipped off the
right edge** — the Presenter is designed to sit at a quarter of the screen width
and five word-labelled tabs do not fit. Icon tabs fix a visible defect, not just
a style preference.

That retires the `[Brackets]` selection convention:

- Delete `TreeTabText`, `ConfigTabText`, `DiagnosticsTabText`, `DiscoveryTabText`,
  `CommandCatalogTabText` and the five `OnPropertyChanged` calls in the
  `SelectedTab` setter (`PresenterShellViewModel.cs:335-339`, `354-362`).
- Keep the five `IsXTabSelected` booleans — they already exist and already drive
  `IsVisible` on the panels.
- Bind the selected tab's `BackgroundColor` to `PresenterSelected` via a
  `BoolToColor` converter or a `DataTrigger`, so selection is visible without text.
- Give each tab `ToolTipProperties.Text` and `SemanticProperties.Description`
  equal to its old label ("Tree", "Config", …).

Grep confirms nothing asserts on the bracketed strings, so this is safe under K1.

### 2.3 Tree row chevrons — the mirror split

`UatWorkspaceNodeViewModel` currently has:

```csharp
public string ExpansionText => CanExpand ? (IsExpanded ? "v" : ">") : string.Empty;
public string DisplayText => $"{new string(' ', Depth * 2)}{ExpansionText.PadRight(1)} {Icon} {Name}";
```

`ExpansionText` is bound to the visible row button **and** folded into
`DisplayText`, which feeds the hidden `WorkspaceTreeText` mirror the tests read.
Turning `ExpansionText` into a private-use-area glyph would push that glyph into
the assertion text.

Split them:

- `ExpansionGlyph` — the Segoe codepoint, bound to the row button. New.
- `ExpansionText` — keeps returning `"v"` / `">"` / `""`, used **only** by
  `DisplayText`. Same for `Icon`: the row `Label` gets a glyph property, while
  `DisplayText` keeps `[F]`, `[MD]`, `[C]`, `[S]` and the `✓ ▶ ✕ → ■ ○` status
  characters it produces today.
- Both raise `PropertyChanged` wherever `ExpansionText` does now
  (`UatWorkspaceNodeViewModel.cs:80`, `:138`, `AddChild`).

Do the same for the recent-folders rows: `RecentFolderViewModel.DisplayText`
feeds the `RecentFoldersText` mirror.

### 2.4 What keeps its text

- `DelayMillisecondsLabel` ("ms") and the "Delay" label — they label an input.
- `StatusSummaryLabel`, `WorkspaceSummaryLabel`, every panel body.
- All seven hidden mirror labels.
- Recent-folder rows — they are paths; a path is its own label.

### 2.5 Definition of done

- No AutomationId changed.
- Every icon-only button carries a `SemanticProperties.Description` and a tooltip.
- All five tabs, plus the theme button, fit on one row at quarter-screen width.
- `accessibility-audit.md` from a UI run reports no unnamed interactive element.
- `Brinell.Presenter.Uat.Tests` passes unchanged — **no test edit should be
  needed for WP2.** If one is, K1 was broken.

---

## WP3 — CommunityToolkit `Expander`

### 3.1 What gets replaced

Exactly two hand-rolled pairs:

| Today | Location |
| --- | --- |
| `OpenRecentButton` + `IsVisible`-bound `Border` (recent folders) | `PresenterPage.xaml:19-57` |
| `SelectionExpander` button + `IsVisible`-bound `Border` (selection details) | `PresenterPage.xaml:213-232` |

### 3.2 What does **not** get replaced

The workspace tree. It is a flat, indented `CollectionView` whose rows are
flattened from a node graph, with a hidden text mirror derived from the same
flattening. Nested per-row `Expander`s would replace virtualised flat rows with
a nested visual tree, break the mirror, and break `TreeToggle(kind, name)`.
Out of scope, deliberately.

### 3.3 Project wiring

- `Brinell.Presenter.csproj`: `<PackageReference Include="CommunityToolkit.Maui" />`
  (version is already central — do not add one).
- `MauiProgram.cs`: `.UseMauiCommunityToolkit()`.
- **`MauiProgram.CreateMauiApp` is currently not doing DI for the Presenter** —
  `App.CreateWindow` news up the whole VM graph by hand. Leave that alone; this
  plan is not a DI refactor.
- `PresenterPage.xaml`: `xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"`.

### 3.4 The automation problem, and the answer

Per `Expander.tpl.cs` and the toolkit probe: **the toolkit `Expander` publishes
no ExpandCollapse and no Invoke pattern on Windows, and its header tap is a
toolkit-internal handler the bridge cannot bind.** The Presenter's own UAT tests
drive the real .exe through UI Automation. So a naïve swap makes both expanders
untappable by a test.

Two routes:

**Route A — header holds a real `Button` (recommended).**

```xml
<toolkit:Expander IsExpanded="{Binding IsSelectionExpanded, Mode=TwoWay}">
    <toolkit:Expander.Header>
        <Button AutomationId="SelectionExpander"
                Command="{Binding ToggleSelectionCommand}"
                Text="Selection"
                ... />
    </toolkit:Expander.Header>
    ...content...
</toolkit:Expander>
```

The VM keeps `IsSelectionExpanded` / `IsRecentFoldersExpanded` and their toggle
commands exactly as they are. `Button` is natively invokable through UIA, the
AutomationId is unchanged, and the existing page object needs no edit. Cost:
tapping the header *chrome* (as opposed to the button) is not test-reachable —
acceptable, because no test does that today.

`SelectionExpanderText` ("Selection ^" / "Selection v") collapses to a static
"Selection" label plus the WP2 chevron glyph driven by `IsSelectionExpanded`.

**Route B — the full gesture bridge.** Reference
`samples/Brinell.Maui.AppSupport`, call `UseBrinellGestureBridge()`, add a
`PresenterVerbSink` modelled on `ToolkitVerbSink` (`Tap` → flip `IsExpanded`,
`GetState("IsExpanded")`), declare `uia:GestureAutomation.Verbs="Tap,GetState"`
on each `Expander`, and open both `BRINELL_UIA_BRIDGE` gates (compile define +
env var) from `PresenterFixture`. This is what unlocks
`Brinell.Maui.CommunityToolkit.Controls.Views.Expander<T>` as a control object
against the Presenter.

**Take Route A.** Route B makes a `srcnew/` product project depend on a
`samples/` project and adds three gates to get right, to buy header-tap coverage
nothing currently needs. Record Route B here as the route to take *if and when*
someone wants `Expander<T>` coverage of the Presenter itself.

> **Route B now has a plan:** [01 — Presenter gesture bridge](../plan/01-presenter-gesture-bridge.md).
> It corrects two things above, both measured: it is **one** gate, not three
> (`FlaUIMauiDriver` already sets `BRINELL_UIA_BRIDGE=1`, and Debug defines the
> constant, so `PresenterFixture` needs no change), and **no test or `.uat.md`
> scenario touches `OpenRecentButton` or `SelectionExpander` today** — so Route B
> would break nothing. Route A remains the right call for this WP if the bridge is
> not yet in; if it is, prefer Route B. Neither choice changes an AutomationId.

### 3.5 Definition of done

- Both panels expand and collapse, and remember state across a reload the same
  way they do today (`IsRecentFoldersExpanded` is force-collapsed on folder
  open — `PresenterShellViewModel.cs:538`; keep that).
- `SelectionExpander` and `OpenRecentButton` still resolve and still click.
- No `srcnew → samples` project reference was added.

---

## WP4 — App icon

### 4.1 Files

- `Resources/AppIcon/appicon.svg` — background/base.
- `Resources/AppIcon/appiconfg.svg` — foreground glyph.
- `Brinell.Presenter.csproj`:

```xml
<ItemGroup>
  <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#2563EB" />
  <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#2563EB" BaseSize="128,128" />
</ItemGroup>
```

Splash is optional — it is listed because the sample app pairs them and because
the Presenter currently shows an unbranded blank window while WinUI starts.

### 4.2 Design

The Presenter is the UAT *runner*: a play triangle over the Brinell mark, or a
Brinell indenter dot, in `PresenterAccent` `#2563EB` so the icon and the accent
agree. It must read at 16px in the taskbar — one shape, no text.

### 4.3 The unpackaged caveat

`WindowsPackageType=None`. For unpackaged WinUI apps the taskbar / Alt-Tab /
title-bar icon does **not** always come from `<MauiIcon>` the way it does for
packaged ones. Verify by building and running, and checking all three surfaces.
If the title-bar icon is missing, the fallback is an explicit `<ApplicationIcon>`
pointing at a generated `.ico`, or setting the window icon from the existing
`#if WINDOWS` block in `App.xaml.cs` via `AppWindow.SetIcon`. Budget for this;
do not assume the item group alone finishes the job.

### 4.4 Definition of done

Icon visible in: the `.exe` in Explorer, the taskbar, Alt-Tab, and the window
title bar. Screenshot into `.my/present/fix/assets/`.

---

## 5. Sequence

1. **WP1** first, and the WinUI native-theme risk (§1.5) first within it — it
   decides how much of the rest is worth styling.
2. **WP2** second. It edits the same rows WP1 just restyled; doing it after
   means one pass over `PresenterPage.xaml`, not two.
3. **WP3** third. It restructures two of those rows; doing it last means it
   inherits finished styling instead of being restyled twice.
4. **WP4** any time — it touches only the csproj and new files.

WP4 can run in parallel with any of the others. WP1–WP3 should not.

## 6. Verification

From `Brinell/`:

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
dotnet build srcnew\Brinell.Presenter\Brinell.Presenter.csproj -f net10.0-windows10.0.19041.0 -v:minimal /nr:false
dotnet test testsnew\Brinell.Presenter.Uat.Tests\Brinell.Presenter.Uat.Tests.csproj -v:minimal /nr:false
```

The Presenter test project builds the sample AUT as a pre-build step, and the
suite launches the real Presenter .exe — so **rebuild the Presenter before
running the tests**, and run only one UI test process at a time.

Per-WP gate:

| WP | Passes when |
| --- | --- |
| 1 | both themes measured at AA; toggle repaints live; choice survives restart |
| 2 | no AutomationId moved; no test edited; all tabs fit at quarter width |
| 3 | both expanders still click by their existing ids; no `srcnew → samples` reference |
| 4 | icon on all four Windows surfaces |

Manual check, all four: launch the Presenter, load
`testsnew/Brinell.Maui.Uat.Tests`, expand a tree node, run a scenario, and
confirm the status glyphs are legible in **both** themes while running.

## 7. Out of scope

- Moving the Presenter onto DI (`App.CreateWindow` hand-builds the VM graph).
- Turning the workspace tree into nested Expanders (§3.2).
- Route B / the gesture bridge in the Presenter (§3.4) — now planned separately in
  [01 — Presenter gesture bridge](../plan/01-presenter-gesture-bridge.md); still out of
  scope *here*.
- Any change to `Brinell.Maui.CommunityToolkit` — its `Expander<T>` control
  object already exists and is not needed by Route A.
- Non-Windows heads. The Presenter is Windows-only and this plan keeps it so.

## 8. Docs to update when this lands

- `.my/present/10 presenter tabbed tree redesign.md` — its `markui` mock shows
  `[Open][v] [Reload] [Validate]` and `[[Tree]]--[Config]--...`; both
  conventions are retired by WP2.
- `docs/README.md` only if a new active guide is added — this plan does not add one.
