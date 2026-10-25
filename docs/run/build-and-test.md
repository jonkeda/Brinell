# Build And Test

Working directory: Bruinell root.

## Framework Build

```powershell
dotnet build Bruinell.sln -v:minimal /nr:false
```

## Focused Unit Tests

```powershell
dotnet test testsnew\Bruinell.Core.Tests\Bruinell.Core.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinell.Maui.Tests\Bruinell.Maui.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Bruinell.Uat.Tests\Bruinell.Uat.Tests.csproj -v:minimal /nr:false
```

## Windows Platform Build

```powershell
dotnet build srcnew\Bruinell.Maui.FlaUI\Bruinell.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build srcnew\Bruinell.Wpf\Bruinell.Wpf.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build srcnew\Bruinell.WinForms\Bruinell.WinForms.csproj -f net10.0-windows -v:minimal /nr:false
```

## Notes

`Bruinell.sln` holds every project in the repository, in folders by area; `SolutionTests` in
`Bruinell.Core.Tests` fails when a project is missing from it (scratch projects under `.my/` are
left out). The two
Visual Studio extension projects are in it but not built by it: they need Visual Studio's
MSBuild, and build from `tools\Bruinell.Bdd.VisualStudio\Bruinell.Bdd.VisualStudio.slnx`.
