# Run WPF Tests

Working directory: Brinell root.

## Build

```powershell
dotnet build srcnew\Brinell.Wpf\Brinell.Wpf.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build samples\Brinell.Samples.Wpf.App\Brinell.Samples.Wpf.App.csproj -f net10.0-windows -v:minimal /nr:false
```

## Unit Tests

```powershell
dotnet test testsnew\Brinell.Wpf.Tests\Brinell.Wpf.Tests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

## UI Tests

```powershell
dotnet test testsnew\Brinell.Wpf.UITests\Brinell.Wpf.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```
