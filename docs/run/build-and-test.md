# Build And Test

Working directory: Bruinet root.

## Framework Build

```powershell
dotnet build Bruinet.sln -v:minimal /nr:false
```

## Focused Unit Tests

```powershell
dotnet test tests\Bruinet.Core.Tests\Bruinet.Core.Tests.csproj -v:minimal /nr:false
dotnet test tests\Bruinet.Maui.Tests\Bruinet.Maui.Tests.csproj -v:minimal /nr:false
dotnet test tests\Bruinet.Uat.Tests\Bruinet.Uat.Tests.csproj -v:minimal /nr:false
```

## Windows Platform Build

```powershell
dotnet build src\Bruinet.Maui.FlaUI\Bruinet.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build src\Bruinet.Wpf\Bruinet.Wpf.csproj -f net10.0-windows -v:minimal /nr:false
dotnet build src\Bruinet.WinForms\Bruinet.WinForms.csproj -f net10.0-windows -v:minimal /nr:false
```

## Notes

`Bruinet.sln` holds every project in the repository, in folders by area; `SolutionTests` in
`Bruinet.Core.Tests` fails when a project is missing from it (scratch projects under `.my/` are
left out). The two
Visual Studio extension projects are in it but not built by it: they need Visual Studio's
MSBuild, and build from `tools\Bruinet.Bdd.VisualStudio\Bruinet.Bdd.VisualStudio.slnx`.
