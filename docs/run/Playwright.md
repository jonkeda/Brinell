# Run Playwright Tests

Working directory: Bruinet root.

## Build

```powershell
dotnet build srcnew\Bruinet.Html.Playwright\Bruinet.Html.Playwright.csproj -v:minimal /nr:false
```

## Install Browsers

After building a Playwright test project, run the generated install script from
that project's output folder:

```powershell
pwsh bin\Debug\net10.0\playwright.ps1 install
```

## Run Tests

```powershell
dotnet test testsnew\Bruinet.Html.UITests\Bruinet.Html.UITests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinet.Blazor.UITests\Bruinet.Blazor.UITests.csproj -v:minimal /nr:false
```
