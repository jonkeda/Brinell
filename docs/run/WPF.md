# Run WPF Tests

Working directory: Bruinet root.

## Build

```powershell
dotnet build srcnew\Bruinet.Wpf\Bruinet.Wpf.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build samples\Bruinet.Samples.Wpf.App\Bruinet.Samples.Wpf.App.csproj -f net10.0-windows -v:minimal /nr:false
```

## Unit Tests

```powershell
dotnet test testsnew\Bruinet.Wpf.Tests\Bruinet.Wpf.Tests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

## UI Tests

```powershell
dotnet test testsnew\Bruinet.Wpf.UITests\Bruinet.Wpf.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```
