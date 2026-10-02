# Build And Test

Working directory: Brinell root.

## Framework Build

```powershell
dotnet build Brinell.sln -v:minimal /nr:false
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

`Brinell.sln` holds every project in the repository, in folders by area. The two
Visual Studio extension projects are in it but not built by it: they need Visual Studio's
MSBuild, and build from `tools\Brinell.Bdd.VisualStudio\Brinell.Bdd.VisualStudio.slnx`.
