# Simple Page And Tests Example

Status: Delivered — example code corrected against the shipped API (2026-09-21)
Date: 2026-07-07, revised 2026-09-21
Area: `srcnew/Brinell.Uat`, `testsnew/Brinell.*.Uat.Tests`
Related:

- [Default naming page example](05b%20default%20naming%20page%20example.md) — the same example without `[UatName]`
- [Runner code binding](03%20runner%20code%20binding.md) — the binding model
- [UAT phrases and flows](../../docs/guides/uat-phrases-and-flows.md)

This document shows a small end-to-end example for the UAT runner design.

It includes:

- A simple MAUI page.
- A Brinell PageObject.
- A custom ControlObject.
- A `uat.config.md`.
- A UAT Markdown file.
- Conventional Brinell tests.
- UAT runner binding/execution tests.

> **Revised (2026-09):** this example was written before the runner existed, and
> its original note — "the code is illustrative, names may shift" — turned out to
> be right: several names did shift. The snippets below have been corrected
> against the shipped API, so they are now meant to be read as real code. The
> largest change is that a custom *control* verb is declared with `[UatStep]`
> rather than `[UatAction]` + `[UatPhrase]`; `[UatPhrase]` remains correct for
> page and phrase-class methods. For a live version of this example, see
> `testsnew/Brinell.Maui.Uat.Tests`.

## Example App Page

The example app has a `LoginPage` with:

- User name entry.
- Password entry.
- Sign in button.
- Result message label.

Example MAUI XAML:

```xml
<ContentPage
    x:Class="Example.App.Pages.LoginPage"
    AutomationId="LoginPage">

    <VerticalStackLayout Padding="24" Spacing="12">
        <Label
            AutomationId="LoginTitle"
            Text="Sign in" />

        <Entry
            AutomationId="UserNameEntry"
            Placeholder="User name" />

        <Entry
            AutomationId="PasswordEntry"
            Placeholder="Password"
            IsPassword="True" />

        <Button
            AutomationId="SignInButton"
            Text="Sign in"
            Command="{Binding SignInCommand}" />

        <Label
            AutomationId="ResultMessage"
            Text="{Binding ResultMessage}" />
    </VerticalStackLayout>
</ContentPage>
```

## Custom ControlObject

`PasswordEntry` is a custom control object. It wraps normal text entry behavior, but exposes password-specific UAT behavior and assertions.

```csharp
using Brinell.Maui;
using Brinell.Maui.Controls.Text;

namespace Example.App.UITests.Controls;

public sealed class PasswordEntry<TScope> : Entry<TScope>
    where TScope : IMauiScope<TScope>
{
    public PasswordEntry(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    public TScope EnterPassword(string? password, int? timeoutMs = null)
    {
        return SetText(password, timeoutMs);
    }

    [UatStep(UatEffectiveStepKeyword.Then, "{control} should be masked",
             CommandId = "Password.AssertMasked")]
    public TScope AssertMasked(string? message = null, int? timeoutMs = null)
    {
        var isPassword = GetAttribute("IsPassword", null)
            ?? GetAttribute("isPassword", null)
            ?? GetAttribute("Password", null);

        if (!string.Equals(isPassword, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new AssertionException(message ?? "Expected password entry to be masked.");
        }

        return ContainingScope;
    }
}
```

The important parts:

- The class derives from an existing Brinell control, so it **inherits the
  built-in phrases** — `I enter {value} into {control}`, `I set {control} to
  {value}` and `I clear {control}` all already bind to it, because `Entry<TScope>`
  implements `IEditableTextControlObject<TScope>` and that is where those phrases
  are declared.
- `[UatStep]` adds one phrase the built-in set does not have. It is discovered on
  the same pass as the Core verbs, because the runtime scans the control property
  types it finds during page discovery alongside `Brinell.Core`.
- The UAT file still uses readable language, not method calls.

> **Superseded (2026-09).** The original showed two attributes that would both be
> wrong today:
>
> - `[UatAction("enter")]` + `[UatPhrase("I enter {value} into {control}")]` on
>   `EnterPassword`, to make the generic enter command bind. This is now
>   unnecessary *and* harmful: the phrase is inherited from the interface, so
>   re-declaring it is a duplicate — discovery would either de-dup it or report
>   `UATD003`. The method is kept here as a plain convenience wrapper.
> - `[UatPhrase]` on a control method. `[UatPhrase]` is discovered on root types,
>   page types and `[UatPhraseClass]` types — **not** on controls. A custom
>   control verb uses `[UatStep]`, exactly like a built-in one. See
>   `CustomControlVerb_OnAppControl_IsDiscovered` in
>   `testsnew/Brinell.Uat.Tests/UatCatalogParityTests.cs`.
>
> The phrase was also reworded from `I should see {control} is masked` to
> `{control} should be masked`, to match the `Then {control} should …` shape every
> built-in assertion uses.

## PageObject

```csharp
using Brinell.Maui;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Controls.Text;
using Brinell.Maui.Pages;
using Example.App.UITests.Controls;

namespace Example.App.UITests.Pages;

[UatName("Login")]
public sealed class LoginPage : PageObjectBase<LoginPage>
{
    public LoginPage(IMauiTestContext context)
        : base(context)
    {
    }

    public override string Name => "LoginPage";

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return LoginTitle.IsExists();
    }

    [UatName("Title")]
    public Label<LoginPage> LoginTitle => new(this, "LoginTitle");

    [UatName("User name")]
    public Entry<LoginPage> UserNameEntry => new(this, "UserNameEntry");

    [UatName("Password")]
    public PasswordEntry<LoginPage> PasswordEntry => new(this, "PasswordEntry");

    [UatName("Sign in")]
    public Button<LoginPage> SignInButton => new(this, "SignInButton");

    [UatName("Result message")]
    public Label<LoginPage> ResultMessage => new(this, "ResultMessage");

    [UatPhrase(UatEffectiveStepKeyword.When, "I sign in with credentials")]
    public Task SignInWithCredentialsAsync(
        UatTable table,
        CancellationToken cancellationToken = default)
    {
        var userName = table.GetValue("User name");
        var password = table.GetValue("Password");

        UserNameEntry.SetText(userName);
        PasswordEntry.EnterPassword(password);
        SignInButton.Click();

        return Task.CompletedTask;
    }
}
```

Discovery result:

```text
Page:
  Login -> LoginPage

Controls:
  Login.Title -> LoginTitle
  Login.User name -> UserNameEntry
  Login.Password -> PasswordEntry
  Login.Sign in -> SignInButton
  Login.Result message -> ResultMessage

Custom page command (from [UatPhrase]):
  I sign in with credentials -> LoginPage.SignInWithCredentialsAsync

Custom control command (from [UatStep]):
  {control} should be masked -> PasswordEntry.AssertMasked

Inherited control commands (from [UatStep] on Brinell.Core interfaces):
  I tap {control}, I enter {value} into {control}, I set {control} to {value},
  I clear {control}, {control} should contain {value}, ... (15 in all)
```

Note what the page does *not* have to declare: every built-in verb the controls
support is already bound, because the phrases live on the interfaces those
controls implement. Discovery adds only the two custom phrases.

Two attribute forms are in play, and the split is worth holding on to:

| Attribute | Goes on | Use it for |
| --- | --- | --- |
| `[UatStep]` | a control method | a verb that acts on one control, e.g. `{control} should be masked` |
| `[UatPhrase]` | a page, root or `[UatPhraseClass]` method | anything wider than one control, e.g. a multi-field form step |

## UAT Config

Example `uat.config.md` in the UAT folder:

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
| RequireExplicitUatAttributes | true |
| AllowNameInference | true |
```

The runner uses this config to know which assemblies to scan for pages, controls, and custom commands.

> **Revised (2026-09):** `Adapter` and `AppBootstrap` are not config fields. The
> shipped `Runtime` section uses `Target`, `Fixture`, `AppPath` and
> `WorkingDirectory`, and `Assemblies` entries are paths to built `.dll` files
> rather than logical assembly names. The file also accepts optional `Reporting`,
> `Settings` and `Skip Rules` sections — see
> [03](03%20runner%20code%20binding.md) §`uat.config.md`.

## UAT Markdown

Example `login.uat.md`:

```md
# UAT: Login

## Metadata

| Field | Value |
| --- | --- |
| App | Example.Maui |
| Area | Authentication |
| Target | MAUI |
| Tags | smoke, login |

@smoke @login
## Scenario: Valid user can sign in

Given I am on the Login page
When I sign in with credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
Then I should see "Welcome Ada"

@login @validation
## Scenario: Password field is masked

Given I am on the Login page
Then Password should be masked
```

## Conventional Brinell Tests

These are normal code-first UI tests. They do not use the UAT Markdown runner.

```csharp
using Brinell.Maui.UITests;
using Example.App.UITests.Pages;
using Xunit;

namespace Example.App.UITests.Tests;

public sealed class LoginPageTests : MauiTestBase
{
    [Fact]
    public void ValidUserCanSignIn()
    {
        var login = GetPage<LoginPage>();

        login.AssertLoaded(true);
        login.UserNameEntry.SetText("ada@example.com");
        login.PasswordEntry.EnterPassword("correct-password");
        login.SignInButton.Click();
        login.ResultMessage.AssertText("Welcome Ada");
    }

    [Fact]
    public void PasswordFieldIsMasked()
    {
        var login = GetPage<LoginPage>();

        login.AssertLoaded(true);
        login.PasswordEntry.AssertMasked();
    }
}
```

These tests prove the PageObject and custom ControlObject work before the UAT runner is involved.

## UAT Runner Binding Test

This test checks parsing and binding without launching the real app. It derives
from `UatSpecFormatTestBase`, which supplies the parse-and-bind assertions:

```csharp
using Brinell.Uat;
using Xunit;

namespace Example.App.UITests.Uat;

public sealed class LoginUatSpecTests : UatSpecFormatTestBase
{
    // Pulls the runtime root in so custom [UatPhrase] methods are in the catalog.
    protected override Type? RuntimeRootType => typeof(ExampleUatFixture);

    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    [Theory]
    [MemberData(nameof(ScenarioFiles))]
    public void UatFile_ParsesWithRequiredMetadata(string filePath) =>
        AssertUatFileParsesAndContainsRequiredMetadata(filePath);

    [Theory]
    [MemberData(nameof(ScenarioFiles))]
    public void UatFile_BindsThroughCatalog(string filePath) =>
        AssertUatFileBindsThroughCatalog(filePath);

    [Fact]
    public void Config_Parses() => AssertUatConfigParses();
}
```

Under the covers that is `UatMarkdownParser.ParseFile` → `UatBinder.Bind` against
a catalog from `UatSpecCommandCatalog.CreateDefault()`, with failures formatted by
`UatDiagnosticsFormatter`. Writing it by hand is possible but rarely worth it.

This kind of test catches:

- Misspelled UAT phrases (`UATB001`).
- Ambiguous command patterns (`UATB002`).
- Table shape mismatches (`UATB003` / `UATB004`).
- Missing `[UatName]` attributes and duplicate names (`UATD001` / `UATD002`).

> **Superseded (2026-09):** the original snippet used four APIs that do not exist
> — `UatConfig.Load`, `UatDiscovery.BuildCatalog`, `result.FormatErrors()` and
> `result.Invocations` with `ResolvedPage` / `ResolvedControl`. The real names are
> `UatConfigParser.ParseFile`, a catalog from `UatSpecCommandCatalog.CreateDefault()`
> (there is no "build a catalog from config" call — discovery takes assemblies and
> options, via `UatDiscovery.Discover`), and `UatDiagnosticsFormatter.FormatDiagnostics`.
> Page and control resolution is not on the invocation at all: it happens inside
> the handler at execution time, so a binding test cannot assert on it.

## UAT Runner Execution Test

This test runs the UAT through the runner. It derives from
`UatScenarioTestBase<TFixture>`, which owns the parse → bind → execute pipeline,
the screenshot-on-failure evidence and the artifact output:

```csharp
using Brinell.Uat;
using Xunit;

namespace Example.App.UITests.Uat;

[Collection(ExampleUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "MAUI")]
public sealed class LoginUatScenarioTests : UatScenarioTestBase<ExampleFixture>
{
    public LoginUatScenarioTests(ExampleFixture fixture)
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

One theory over `Scenarios/**/*.uat.md`, so adding a scenario file adds a test
case with no code change. The UAT runner uses the same PageObject and
ControlObject code as the conventional tests.

For the diagnostics case, the same base class offers
`RunExpectedFailureUatFileAsync(filePath, expectedFragments...)`, which asserts a
scenario fails *and* that the message names the missing control and the available
ones — the pattern
[08](08%20uat%20diagnostics%20and%20config%20hardening.md) asked for.

> **Superseded (2026-09):** there is no `UatScenarioRunner` taking an app session,
> no `ThrowIfFailed()`, and no `result.Passed` / `FormatFailures()`. Nor is a UAT
> execution test written per file — it is a theory over the scenario folder.
> The live version of this is
> `testsnew/Brinell.Maui.Uat.Tests/Runtime/MauiUatScenarioTests.cs`.

## Resolved Execution Plan

For this UAT step:

```md
When I sign in with credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
```

The runner resolves:

```text
Matched pattern:
  I sign in with credentials

Handler:
  LoginPage.SignInWithCredentialsAsync

Page:
  LoginPage

Table:
  User name = ada@example.com
  Password = correct-password
```

For this UAT step:

```md
Then Password should be masked
```

The runner resolves:

```text
Matched pattern:
  {control} should be masked

Command id:
  Builtin.Password.AssertMasked

Handler:
  AssertMasked, invoked by name on the resolved control

Page:
  LoginPage (from CurrentPageName)

Control:
  "Password" -> LoginPage.PasswordEntry
```

## Minimal File Layout

This is the layout the shipped UAT projects use:

```text
Example.App.UITests/            # page objects and custom controls
  Controls/
    PasswordEntry.cs
  Pages/
    LoginPage.cs

Example.App.Uat.Tests/          # the UAT project itself
  uat.config.md
  Scenarios/
    login.uat.md
  ExpectedFailures/
    login-missing-control.uat.md
  Runtime/
    ExampleUatCollection.cs
    ExampleFixture.cs
    LoginUatScenarioTests.cs
  TestSettings/
    testsettings.json
```

The page objects stay in the UI-test assembly and are *referenced* by the UAT
project, which is why both conventional tests and UAT scenarios drive the same
code.

## Key Point

The runner does not need a large command language to start — and it turned out
not to need a large one at all. The built-in set is 18 phrases, unchanged across
six UI technologies.

What makes that enough:

- A small built-in command set, declared once on the `Brinell.Core` control
  interfaces and inherited by every control that implements them.
- PageObject and ControlObject discovery.
- `[UatName]` for canonical names.
- `[UatStep]` for a project's own control verbs.
- `[UatPhrase]` for project-specific language wider than one control.
- A folder-level `uat.config.md` for assembly discovery.

> **Revised (2026-09):** the original list credited `[UatAction]` with "reusable
> control capabilities". It marks a capability for discovery but contributes no
> phrase; `[UatStep]` is what makes a control verb reusable, and it is the entry
> this list was missing.

That is enough for a readable UAT file to drive real Brinell PageObjects and custom ControlObjects.
