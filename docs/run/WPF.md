# Run WPF Tests

Working directory: Bruinell root.

## Build

```powershell
dotnet build srcnew\Bruinell.Wpf\Bruinell.Wpf.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build samples\Bruinell.Samples.Wpf.App\Bruinell.Samples.Wpf.App.csproj -f net10.0-windows -v:minimal /nr:false
```

## Unit Tests

```powershell
dotnet test testsnew\Bruinell.Wpf.Tests\Bruinell.Wpf.Tests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

## UI Tests

```powershell
dotnet test testsnew\Bruinell.Wpf.UITests\Bruinell.Wpf.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```
