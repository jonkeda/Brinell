# Run HTML Tests

Working directory: Brinell root.

## Build

```powershell
dotnet build srcnew\Brinell.Html\Brinell.Html.csproj -v:minimal /nr:false
dotnet build srcnew\Brinell.Html.Playwright\Brinell.Html.Playwright.csproj -v:minimal /nr:false
```

## Unit Coverage

```powershell
dotnet test testsnew\Brinell.Blazor.Tests\Brinell.Blazor.Tests.csproj -v:minimal /nr:false
```

`Brinell.Html.Tests` is currently a compile-only placeholder and is not a test
suite.

## UI Tests

```powershell
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false
```

UI tests may require a running sample app or fixture-managed host.
