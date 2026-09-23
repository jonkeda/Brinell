# Run WinForms Tests

Working directory: Brinell root.

## Build

```powershell
dotnet build srcnew\Brinell.WinForms\Brinell.WinForms.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build samples\Brinell.Samples.WinForms.App\Brinell.Samples.WinForms.App.csproj -f net10.0-windows -v:minimal /nr:false
```

## Unit Tests

```powershell
dotnet test testsnew\Brinell.WinForms.Tests\Brinell.WinForms.Tests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

## UI Tests

```powershell
dotnet test testsnew\Brinell.WinForms.UITests\Brinell.WinForms.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```
