# Run HTML Tests

Working directory: Bruinet root.

## Build

```powershell
dotnet build srcnew\Bruinet.Html\Bruinet.Html.csproj -v:minimal /nr:false
dotnet build srcnew\Bruinet.Html.Playwright\Bruinet.Html.Playwright.csproj -v:minimal /nr:false
```

## Unit Coverage

```powershell
dotnet test testsnew\Bruinet.Html.Tests\Bruinet.Html.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinet.Blazor.Tests\Bruinet.Blazor.Tests.csproj -v:minimal /nr:false
```

`Bruinet.Html.Tests` holds the call-model tests: calls, scope readiness, confirmations,
collections, and the Playwright error mapping.

## Generate Controls

Html and Blazor controls are `.tpl.cs` templates and generated `.gen.cs` files, as on MAUI. After
changing a template:

```powershell
tools\Scripts\CreateHtml.Bat
```

## UI Tests

Install Playwright's Chromium once (after a build, from the Bruinet root):

```powershell
pwsh testsnew\Bruinet.Html.UITests\bin\Debug\net10.0\playwright.ps1 install chromium
```

Then:

```powershell
dotnet test testsnew\Bruinet.Html.UITests\Bruinet.Html.UITests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinet.Blazor.UITests\Bruinet.Blazor.UITests.csproj -v:minimal /nr:false
```

Nothing needs to be running: the tests build the Blazor sample and start it on a free port
(`Bruinet.Html.Testing.WebAppHost`). Set `BLAZOR_APP_URL` to use a sample that is already
running instead. The Todo web head has its own UI tests:
`dotnet test samples\Todo\tests\Bruinet.Samples.Todo.Web.UITests` (hermetic; add
`BRUINET_UAT_LIVE_API=1` for the live tier).

## UAT

```powershell
dotnet test testsnew\Bruinet.Html.UITests\Bruinet.Html.UITests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinet.Blazor.UITests\Bruinet.Blazor.UITests.csproj -v:minimal /nr:false
dotnet test samples\Todo\tests\Bruinet.Samples.Todo.Web.Uat -v:minimal /nr:false
```

The first two start the Blazor sample themselves. The third runs the Todo scenario files tagged
`@web` against the Todo web head, including a scenario with two devices on one backend.
