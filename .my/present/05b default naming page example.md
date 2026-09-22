# Default Naming Page Example

Status: Delivered — inferred names verified against `UatNameInference` (2026-09-21)
Date: 2026-07-07, revised 2026-09-21
Area: `srcnew/Brinell.Uat/UatNameInference.cs`
Related:

- [Simple page and tests example](05%20simple%20page%20and%20tests%20example.md) — the same example with `[UatName]`
- [The Gherkin language, formalised](../uat/attribute-catalog/01-grammar.md) §3 — the lexicon
- [Runner code binding](03%20runner%20code%20binding.md) §Page And Control Names

This document shows a UAT runner example where the PageObject does not use `[UatName]`.

The runner discovers page and control names from default naming rules:

- `SettingsPage` becomes `Settings`.
- `DisplayNameEntry` becomes `Display Name`.
- `EmailNotificationsSwitch` becomes `Email Notifications`.
- `SaveButton` becomes `Save`.
- `StatusMessageLabel` becomes `Status Message`.

No aliases are used.

## Example App Page

Example MAUI XAML:

```xml
<ContentPage
    x:Class="Example.App.Pages.SettingsPage"
    AutomationId="SettingsPage">

    <VerticalStackLayout Padding="24" Spacing="12">
        <Label
            AutomationId="SettingsTitleLabel"
            Text="Settings" />

        <Entry
            AutomationId="DisplayNameEntry"
            Placeholder="Display name" />

        <Switch
            AutomationId="EmailNotificationsSwitch" />

        <Button
            AutomationId="SaveButton"
            Text="Save"
            Command="{Binding SaveCommand}" />

        <Label
            AutomationId="StatusMessageLabel"
            Text="{Binding StatusMessage}" />
    </VerticalStackLayout>
</ContentPage>
```

## PageObject Without UAT Names

```csharp
using Brinell.Maui;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Controls.Text;
using Brinell.Maui.Controls.Toggle;
using Brinell.Maui.Pages;

namespace Example.App.UITests.Pages;

public sealed class SettingsPage : PageObjectBase<SettingsPage>
{
    public SettingsPage(IMauiTestContext context)
        : base(context)
    {
    }

    public override string Name => "SettingsPage";

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return SettingsTitleLabel.IsExists();
    }

    public Label<SettingsPage> SettingsTitleLabel => new(this, "SettingsTitleLabel");

    public Entry<SettingsPage> DisplayNameEntry => new(this, "DisplayNameEntry");

    public Switch<SettingsPage> EmailNotificationsSwitch => new(this, "EmailNotificationsSwitch");

    public Button<SettingsPage> SaveButton => new(this, "SaveButton");

    public Label<SettingsPage> StatusMessageLabel => new(this, "StatusMessageLabel");
}
```

There are no `[UatName]` attributes. The runner must infer the authoring names from the class and property names.

## Inferred Names

Discovery result:

```text
Page:
  SettingsPage -> Settings

Controls:
  Settings.SettingsTitleLabel -> Settings Title
  Settings.DisplayNameEntry -> Display Name
  Settings.EmailNotificationsSwitch -> Email Notifications
  Settings.SaveButton -> Save
  Settings.StatusMessageLabel -> Status Message
```

Suffixes removed in this example:

```text
Page
Entry
Switch
Button
Label
```

Words split:

```text
DisplayName -> Display Name
EmailNotifications -> Email Notifications
StatusMessage -> Status Message
```

> **Verified (2026-09)** against `UatNameInference`. Every name above is what the
> shipped inference produces. Two rules of the implementation are worth knowing,
> because they are what make `StatusMessageLabel` land on `Status Message`:
>
> - **Exactly one suffix is stripped**, not all of them. `Message` is itself a
>   known suffix, but stripping stops after `Label`.
> - **The first match in the suffix list wins**, and the list is ordered:
>   `Page, Button, Input, Entry, Field, TextBox, CheckBox, Checkbox, Switch,
>   Toggle, Picker, Dropdown, List, Grid, Label, Display, Message, Text, Control`.
>
> A suffix is also only stripped when something would be left — a control named
> exactly `Label` keeps its name rather than becoming empty.

## UAT Config

`uat.config.md` enables name inference:

```md
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | Appium |
| AppPath | ../../samples/Example.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Example.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | ../Example.App.UITests/bin/Debug/net10.0-windows7.0/Example.App.UITests.dll |
| Controls | ../../srcnew/Brinell.Maui/bin/Debug/net10.0/Brinell.Maui.dll |
| Commands | ../../srcnew/Brinell.Uat/bin/Debug/net10.0/Brinell.Uat.dll |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | false |
| AllowNameInference | true |
```

`RequireExplicitUatAttributes` is `false` because this example intentionally relies on inferred names. Both fields are real `UatDiscoveryOptions` properties, and `AllowNameInference` defaults to `true`.

> **Revised (2026-09):** `Adapter` and `AppBootstrap` are not config fields — see
> [05](05%20simple%20page%20and%20tests%20example.md) §UAT Config.

## UAT Markdown

`settings.uat.md`:

```md
# UAT: Settings

## Metadata

| Field | Value |
| --- | --- |
| App | Example.Maui |
| Area | Settings |
| Target | MAUI |
| Tags | smoke, settings |

@smoke @settings
## Scenario: Save display name

Given I am on the Settings page
When I enter "Ada Lovelace" into Display Name
And I check Email Notifications
And I tap Save
Then I should see "Settings saved"
```

The UAT wording uses inferred names:

```text
Settings
Display Name
Email Notifications
Save
```

## Conventional Brinell Test

```csharp
using Brinell.Maui.UITests;
using Example.App.UITests.Pages;
using Xunit;

namespace Example.App.UITests.Tests;

public sealed class SettingsPageTests : MauiTestBase
{
    [Fact]
    public void SaveDisplayName()
    {
        var settings = GetPage<SettingsPage>();

        settings.AssertLoaded(true);
        settings.DisplayNameEntry.SetText("Ada Lovelace");
        settings.EmailNotificationsSwitch.Check();
        settings.SaveButton.Click();
        settings.StatusMessageLabel.AssertText("Settings saved");
    }
}
```

This proves the PageObject itself works before the UAT runner uses name inference.

## UAT Binding Test

Binding is asserted through `UatSpecFormatTestBase`, exactly as in
[05](05%20simple%20page%20and%20tests%20example.md):

```csharp
using Brinell.Uat;
using Xunit;

namespace Example.App.UITests.Uat;

public sealed class SettingsUatSpecTests : UatSpecFormatTestBase
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    [Theory]
    [MemberData(nameof(ScenarioFiles))]
    public void UatFile_BindsThroughCatalog(string filePath) =>
        AssertUatFileBindsThroughCatalog(filePath);
}
```

The *names* themselves are asserted directly against the inference, which is a
cheaper and more precise test than going through a bind:

```csharp
[Theory]
[InlineData("SettingsPage", "Settings")]
[InlineData("DisplayNameEntry", "Display Name")]
[InlineData("EmailNotificationsSwitch", "Email Notifications")]
[InlineData("SaveButton", "Save")]
[InlineData("StatusMessageLabel", "Status Message")]
public void InferredName_MatchesExpected(string identifier, string expected) =>
    Assert.Equal(expected, UatNameInference.FromIdentifier(identifier));
```

Together these verify that default naming rules are enough to bind the UAT file.

> **Superseded (2026-09):** the original asserted on
> `result.Invocations` with `ResolvedPage` / `ResolvedControl`, using
> `UatConfig.Load` and `UatDiscovery.BuildCatalog`. None of those exist. The
> deeper problem is that a *binding* test cannot see resolved names at all: the
> binder matches a phrase and captures `{control}` as a **string**, and the
> lookup from that string to a page member happens later, inside the handler. So
> a bind proves the phrase matched; only a run proves the name resolved — or a
> direct `UatNameInference` assertion, as above.

## UAT Execution Test

```csharp
using Brinell.Uat;
using Xunit;

namespace Example.App.UITests.Uat;

[Collection(ExampleUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "MAUI")]
public sealed class SettingsUatScenarioTests : UatScenarioTestBase<ExampleFixture>
{
    public SettingsUatScenarioTests(ExampleFixture fixture)
        : base(fixture)
    {
    }

    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath) => RunUatFileAsync(filePath);

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "MAUI", Fixture: "Appium");
}
```

> **Superseded (2026-09):** as in [05](05%20simple%20page%20and%20tests%20example.md),
> there is no `UatScenarioRunner`, `ThrowIfFailed()` or `result.Passed`.
> Execution goes through `UatScenarioTestBase<TFixture>.RunUatFileAsync`, as a
> theory over the scenario folder.

## Resolved Execution Plan

```text
1. Builtin.Page.Open                    (engine-owned)
   step: Given I am on the Settings page
   inferred page name: Settings
   page object: SettingsPage

2. Builtin.Control.Enter                (IEditableTextControlObject.Enter)
   step: When I enter "Ada Lovelace" into Display Name
   inferred control name: Display Name
   control object: DisplayNameEntry
   invoke: Enter("Ada Lovelace")

3. Builtin.Control.Check                (IToggleControlObject.Check)
   step: And I check Email Notifications
   inferred control name: Email Notifications
   control object: EmailNotificationsSwitch
   invoke: Check()

4. Builtin.Control.Tap                  (IClickableControlObject.Click)
   step: And I tap Save
   inferred control name: Save
   control object: SaveButton
   invoke: Click()

5. Builtin.Page.AssertTextVisible       (engine-owned)
   step: Then I should see "Settings saved"
   page object: SettingsPage
```

> **Revised (2026-09):** the command names on the left were invented groupings
> (`InputCommands`, `ToggleCommands`, `InteractionCommands`, `AssertCommands`).
> Real command ids are `Builtin.Page.*` for the three engine verbs and
> `Builtin.Control.*` for the fifteen discovered ones, and the id suffix comes
> from the `[UatStep]` on the interface method named in brackets.

## When Default Names Are Enough

Default names are enough when:

- PageObject class names are user-facing enough.
- Control property names are user-facing enough.
- Suffix stripping produces natural words.
- There is no duplicate control name within a page.

## When To Add `[UatName]`

Add `[UatName]` when the inferred name is awkward or wrong.

Example:

```text
SubmitUserPreferencesButton -> Submit User Preferences
```

Better:

```csharp
[UatName("Save")]
public Button<SettingsPage> SubmitUserPreferencesButton => Button("SaveButton");
```

The first version should support inferred names and single-name overrides, but not aliases.
