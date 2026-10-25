# Run HTML Tests

Working directory: Bruinell root.

## Build

```powershell
dotnet build srcnew\Bruinell.Html\Bruinell.Html.csproj -v:minimal /nr:false
dotnet build srcnew\Bruinell.Html.Playwright\Bruinell.Html.Playwright.csproj -v:minimal /nr:false
```

## Unit Coverage

```powershell
dotnet test testsnew\Bruinell.Html.Tests\Bruinell.Html.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinell.Blazor.Tests\Bruinell.Blazor.Tests.csproj -v:minimal /nr:false
```

`Bruinell.Html.Tests` holds the call-model tests: calls, scope readiness, confirmations,
collections, and the Playwright error mapping.

## Generate Controls

Html and Blazor controls are `.tpl.cs` templates and generated `.gen.cs` files, as on MAUI. After
changing a template:

```powershell
tools\Scripts\CreateHtml.Bat
```

## UI Tests

Install Playwright's Chromium once (after a build, from the Bruinell root):

```powershell
pwsh testsnew\Bruinell.Html.UITests\bin\Debug\net10.0\playwright.ps1 install chromium
```

Then:

```powershell
dotnet test testsnew\Bruinell.Html.UITests\Bruinell.Html.UITests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinell.Blazor.UITests\Bruinell.Blazor.UITests.csproj -v:minimal /nr:false
```

Nothing needs to be running: the tests build the Blazor sample and start it on a free port
(`Bruinell.Html.Testing.WebAppHost`). Set `BLAZOR_APP_URL` to use a sample that is already
running instead. The Todo web head has its own UI tests:
`dotnet test samples\Todo\tests\Bruinell.Samples.Todo.Web.UITests` (hermetic; add
`BRUINELL_UAT_LIVE_API=1` for the live tier).

## UAT

```powershell
dotnet test testsnew\Bruinell.Html.UITests\Bruinell.Html.UITests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinell.Blazor.UITests\Bruinell.Blazor.UITests.csproj -v:minimal /nr:false
dotnet test samples\Todo\tests\Bruinell.Samples.Todo.Web.Uat -v:minimal /nr:false
```

The first two start the Blazor sample themselves. The third runs the Todo scenario files tagged
`@web` against the Todo web head, including a scenario with two devices on one backend.
