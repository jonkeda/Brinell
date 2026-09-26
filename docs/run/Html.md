# Run HTML Tests

Working directory: Brinell root.

## Build

```powershell
dotnet build srcnew\Brinell.Html\Brinell.Html.csproj -v:minimal /nr:false
dotnet build srcnew\Brinell.Html.Playwright\Brinell.Html.Playwright.csproj -v:minimal /nr:false
```

## Unit Coverage

```powershell
dotnet test testsnew\Brinell.Html.Tests\Brinell.Html.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Blazor.Tests\Brinell.Blazor.Tests.csproj -v:minimal /nr:false
```

`Brinell.Html.Tests` holds the call-model tests: calls, scope readiness, confirmations,
collections, and the Playwright error mapping.

## UI Tests

Install Playwright's Chromium once (after a build, from the Brinell root):

```powershell
pwsh testsnew\Brinell.Html.UITests\bin\Debug\net10.0\playwright.ps1 install chromium
```

Then:

```powershell
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Blazor.UITests\Brinell.Blazor.UITests.csproj -v:minimal /nr:false
```

Nothing needs to be running: the tests build the Blazor sample and start it on a free port
(`Brinell.Html.Testing.WebAppHost`). Set `BLAZOR_APP_URL` to use a sample that is already
running instead. The Todo web head has its own UI tests:
`dotnet test samples\Todo\tests\Brinell.Samples.Todo.Web.UITests` (hermetic; add
`BRINELL_UAT_LIVE_API=1` for the live tier).
