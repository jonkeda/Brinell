# Quick Start

This page gets a Brinell developer from a clean checkout to a compiled framework
and a first page-object style test.

## Build The Framework

Working directory: Brinell root.

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
```

Use `srcnew\Brinell.sln` as the broad active compile check. It covers many
source, sample, and test projects, but it is not a complete project inventory.
The top-level `Brinell.sln` includes a different slice plus tools and can fail
for tool-specific restore rules.

## Add Packages

For package consumers:

```powershell
dotnet add package Brinell.Core
dotnet add package Brinell.Maui
dotnet add package Brinell.Maui.FlaUI
dotnet add package Brinell.Html.Playwright
dotnet add package Brinell.Wpf
dotnet add package Brinell.WinForms
dotnet add package Brinell.Mocking
dotnet add package Brinell.Uat
```

For repo-local tests, prefer project references to `srcnew/*` projects.

## First Page Object

Page objects should expose user intent and hide locator plumbing.

```csharp
using Brinell.Core.Locators;
using Brinell.Maui.Controls.Text;
using Brinell.Maui.Pages;

public sealed class LoginPage : PageObjectBase<LoginPage>
{
    public LoginPage(MauiTestContext context)
        : base(context, "Login")
    {
    }

    public Entry Username => Get<Entry>(Locator.ByAutomationId("UsernameEntry"));
    public Entry Password => Get<Entry>(Locator.ByAutomationId("PasswordEntry"));
    public Button SignIn => Get<Button>(Locator.ByAutomationId("SignInButton"));

    public void SignInAs(string username, string password)
    {
        Username.SetText(username);
        Password.SetText(password);
        SignIn.Click();
    }
}
```

Use the concrete platform page-object base and controls that match the test
project you are writing. Keep repeated interaction behavior in Brinell controls,
not in test methods.

## First Test Shape

```csharp
using Xunit;

public sealed class LoginTests
{
    private readonly AppFixture _fixture;

    public LoginTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ValidCredentials_ShowHomePage()
    {
        var login = _fixture.OpenLoginPage();

        login.SignInAs("user@example.test", "password");

        var home = _fixture.CurrentHomePage();
        Assert.True(home.IsVisible(), "Home page should be visible after sign in.");
    }
}
```

Rules:

- Use xUnit `Assert`.
- Do not add FluentAssertions.
- Wait for concrete UI state.
- Do not add arbitrary sleeps to fix timing.
- Use semantic control operations such as `SetText`, `Click`, `SelectItem`, and
  `WaitReady`.

## Next Steps

- Read [Framework Overview](framework-overview.md).
- Read [Test Writing](../guides/test-writing.md).
- Use [Build And Test](../run/build-and-test.md) for common commands.
