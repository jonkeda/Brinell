# Simple Page And Tests Example

This document shows a small end-to-end example for the UAT runner design.

It includes:

- A simple MAUI page.
- A Brinell PageObject.
- A custom ControlObject.
- A `uat.config.md`.
- A UAT Markdown file.
- Conventional Brinell tests.
- UAT runner binding/execution tests.

The code is illustrative. Names may shift when the actual UAT runner packages are created.

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

    [UatAction("enter")]
    [UatPhrase("I enter {value} into {control}")]
    public TScope EnterPassword(string? password, int? timeoutMs = null)
    {
        return SetText(password, timeoutMs);
    }

    [UatPhrase("I should see {control} is masked")]
    public TScope AssertMasked(string? message = null, int? timeoutMs = null)
    {
        var isPassword = GetAttribute("IsPassword")
            ?? GetAttribute("isPassword")
            ?? GetAttribute("Password");

        if (!string.Equals(isPassword, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new AssertionException(message ?? "Expected password entry to be masked.");
        }

        return ContainingScope;
    }
}
```

The important parts:

- The class derives from an existing Brinell control.
- `[UatAction("enter")]` lets generic enter commands bind to it.
- `[UatPhrase]` exposes a custom assertion phrase.
- The UAT file still uses readable language, not method calls.

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
    public Label<LoginPage> LoginTitle => Label("LoginTitle");

    [UatName("User name")]
    public Entry<LoginPage> UserNameEntry => Entry("UserNameEntry");

    [UatName("Password")]
    public PasswordEntry<LoginPage> PasswordEntry => new(this, "PasswordEntry");

    [UatName("Sign in")]
    public Button<LoginPage> SignInButton => Button("SignInButton");

    [UatName("Result message")]
    public Label<LoginPage> ResultMessage => Label("ResultMessage");

    [UatPhrase("I sign in with credentials")]
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

Custom page command:
  I sign in with credentials -> LoginPage.SignInWithCredentialsAsync

Custom control command:
  I should see {control} is masked -> PasswordEntry.AssertMasked
```

## UAT Config

Example `uat.config.md` in the UAT folder:

```md
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Adapter | FlaUI |
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
| RequireExplicitUatAttributes | true |
| AllowNameInference | true |
```

The runner uses this config to know which assemblies to scan for pages, controls, and custom commands.

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
Then I should see Password is masked
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

This test checks parsing and binding without launching the real app.

```csharp
using Xunit;

namespace Example.App.UITests.Uat;

public sealed class LoginUatBindingTests
{
    [Fact]
    public void LoginUat_BindsAllSteps()
    {
        var config = UatConfig.Load("uat.config.md");
        var catalog = UatDiscovery.BuildCatalog(config);
        var document = UatMarkdownParser.ParseFile("login.uat.md");

        var result = UatBinder.Bind(document, catalog);

        Assert.True(result.Success, result.FormatErrors());
        Assert.Contains(result.Invocations, x =>
            x.MatchedPattern == "I sign in with credentials" &&
            x.ResolvedPage?.Name == "Login");
        Assert.Contains(result.Invocations, x =>
            x.MatchedPattern == "I should see {control} is masked" &&
            x.ResolvedControl?.Name == "Password");
    }
}
```

This kind of test catches:

- Misspelled UAT phrases.
- Missing `[UatName]` attributes.
- Missing command assemblies.
- Ambiguous command patterns.
- Table shape mismatches.

## UAT Runner Execution Test

This test runs the UAT through the runner.

```csharp
using Xunit;

namespace Example.App.UITests.Uat;

public sealed class LoginUatExecutionTests : MauiTestBase
{
    [Fact]
    public async Task LoginUat_RunAllScenarios()
    {
        var config = UatConfig.Load("uat.config.md");
        var catalog = UatDiscovery.BuildCatalog(config);
        var document = UatMarkdownParser.ParseFile("login.uat.md");
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

The UAT runner uses the same PageObject and ControlObject code as the conventional tests.

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
Then I should see Password is masked
```

The runner resolves:

```text
Matched pattern:
  I should see {control} is masked

Handler:
  PasswordEntry.AssertMasked

Page:
  LoginPage

Control:
  PasswordEntry
```

## Minimal File Layout

```text
Example.App.UITests/
  Controls/
    PasswordEntry.cs
  Pages/
    LoginPage.cs
  Uat/
    uat.config.md
    login.uat.md
  Tests/
    LoginPageTests.cs
    LoginUatBindingTests.cs
    LoginUatExecutionTests.cs
```

## Key Point

The runner does not need a large command language to start.

The first version can combine:

- A tiny built-in command set.
- PageObject and ControlObject discovery.
- `[UatName]` for canonical names.
- `[UatPhrase]` for project-specific language.
- `[UatAction]` for reusable control capabilities.
- A folder-level `uat.config.md` for assembly discovery.

That is enough for a readable UAT file to drive real Brinell PageObjects and custom ControlObjects.
