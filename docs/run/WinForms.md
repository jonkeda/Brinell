# Run WinForms Tests

Working directory: Bruinell root.

## Build

```powershell
dotnet build srcnew\Bruinell.WinForms\Bruinell.WinForms.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build samples\Bruinell.Samples.WinForms.App\Bruinell.Samples.WinForms.App.csproj -f net10.0-windows -v:minimal /nr:false
```

## Unit Tests

```powershell
dotnet test testsnew\Bruinell.WinForms.Tests\Bruinell.WinForms.Tests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

## UI Tests

```powershell
dotnet test testsnew\Bruinell.WinForms.UITests\Bruinell.WinForms.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```
