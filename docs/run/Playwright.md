# Run Playwright Tests

Working directory: Brinell root.

## Build

```powershell
dotnet build srcnew\Brinell.Html.Playwright\Brinell.Html.Playwright.csproj -v:minimal /nr:false
```

## Install Browsers

After building a Playwright test project, run the generated install script from
that project's output folder:

```powershell
pwsh bin\Debug\net10.0\playwright.ps1 install
```

## Run Tests

```powershell
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Blazor.UITests\Brinell.Blazor.UITests.csproj -v:minimal /nr:false
```
