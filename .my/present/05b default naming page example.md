# Default Naming Page Example

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

    public Label<SettingsPage> SettingsTitleLabel => Label("SettingsTitleLabel");

    public Entry<SettingsPage> DisplayNameEntry => Entry("DisplayNameEntry");

    public Switch<SettingsPage> EmailNotificationsSwitch => Switch("EmailNotificationsSwitch");

    public Button<SettingsPage> SaveButton => Button("SaveButton");

    public Label<SettingsPage> StatusMessageLabel => Label("StatusMessageLabel");
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

Suffixes removed:

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

## UAT Config

`uat.config.md` enables name inference:

```md
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Adapter | CrossPlatformMaui |
| AppBootstrap | Example.App.UITests.ExampleMauiBootstrap |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Example.App.UITests |
| Controls | Example.App.UITests |
| Commands | Example.App.UITests |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | false |
| AllowNameInference | true |
```

`RequireExplicitUatAttributes` is `false` because this example intentionally relies on inferred names.

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

```csharp
using Xunit;

namespace Example.App.UITests.Uat;

public sealed class SettingsUatBindingTests
{
    [Fact]
    public void SettingsUat_BindsWithInferredNames()
    {
        var config = UatConfig.Load("uat.config.md");
        var catalog = UatDiscovery.BuildCatalog(config);
        var document = UatMarkdownParser.ParseFile("settings.uat.md");

        var result = UatBinder.Bind(document, catalog);

        Assert.True(result.Success, result.FormatErrors());

        Assert.Contains(result.Invocations, x =>
            x.MatchedPattern == "I am on the {page} page" &&
            x.ResolvedPage?.Name == "Settings");

        Assert.Contains(result.Invocations, x =>
            x.MatchedPattern == "I enter {value} into {control}" &&
            x.ResolvedControl?.Name == "Display Name");

        Assert.Contains(result.Invocations, x =>
            x.MatchedPattern == "I check {control}" &&
            x.ResolvedControl?.Name == "Email Notifications");

        Assert.Contains(result.Invocations, x =>
            x.MatchedPattern == "I tap {control}" &&
            x.ResolvedControl?.Name == "Save");
    }
}
```

This test verifies that default naming rules are enough to bind the UAT file.

## UAT Execution Test

```csharp
using Xunit;

namespace Example.App.UITests.Uat;

public sealed class SettingsUatExecutionTests : MauiTestBase
{
    [Fact]
    public async Task SettingsUat_RunScenario()
    {
        var config = UatConfig.Load("uat.config.md");
        var catalog = UatDiscovery.BuildCatalog(config);
        var document = UatMarkdownParser.ParseFile("settings.uat.md");
        var plan = UatBinder.Bind(document, catalog).ThrowIfFailed();

        var runner = new UatScenarioRunner(
            AppSession,
            catalog,
            Diagnostics);

        var result = await runner.RunAsync(plan);

        Assert.True(result.Passed, result.FormatFailures());
    }
}
```

## Resolved Execution Plan

```text
1. PageCommands.AssertPageOpen
   step: Given I am on the Settings page
   inferred page name: Settings
   page object: SettingsPage

2. InputCommands.EnterText
   step: When I enter "Ada Lovelace" into Display Name
   inferred control name: Display Name
   control object: DisplayNameEntry
   method: Entry.SetText

3. ToggleCommands.Check
   step: And I check Email Notifications
   inferred control name: Email Notifications
   control object: EmailNotificationsSwitch
   method: Switch.Check

4. InteractionCommands.Tap
   step: And I tap Save
   inferred control name: Save
   control object: SaveButton
   method: Button.Click

5. AssertCommands.AssertTextVisible
   step: Then I should see "Settings saved"
   page object: SettingsPage
```

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
