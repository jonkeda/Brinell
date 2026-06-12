# Build And Test

Working directory: Brinell root.

## Framework Build

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
```

## Focused Unit Tests

```powershell
dotnet test testsnew\Brinell.Core.Tests\Brinell.Core.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Uat.Tests\Brinell.Uat.Tests.csproj -v:minimal /nr:false
```

## Windows Platform Build

```powershell
dotnet build srcnew\Brinell.Maui.FlaUI\Brinell.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build srcnew\Brinell.Wpf\Brinell.Wpf.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build srcnew\Brinell.WinForms\Brinell.WinForms.csproj -f net10.0-windows -v:minimal /nr:false
```

## Notes

Use `srcnew\Brinell.sln` as the broad active compile check. It is not a complete
inventory. The top-level `Brinell.sln` includes a different project slice plus
tools and may fail for tool-specific restore policies even when the framework
projects build.
